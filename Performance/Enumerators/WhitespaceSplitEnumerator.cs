using System.Buffers;
using System.Runtime.CompilerServices;

namespace Performance.Enumerators;

/// <summary>
/// Allocation-free, stack-only enumerator that splits a <see cref="ReadOnlySpan{T}"/>
/// on **whitespace**, returning contiguous non-whitespace tokens as <see cref="ReadOnlySpan{T}"/> slices.
/// </summary>
/// <remarks>
/// <para>
/// - **Whitespace definition:** Unicode-correct (<see cref="char.IsWhiteSpace(char)"/>), with an ASCII fast path for
///   common separators (<c>' '</c>, <c>'\t'</c>, <c>'\r'</c>, <c>'\n'</c>, <c>'\f'</c>).
/// </para>
/// <para>
/// - **No empty tokens:** Consecutive whitespace is coalesced; leading/trailing whitespace is skipped.
/// </para>
/// <para>
/// - **Performance:** O(n). Uses <see cref="MemoryExtensions.IndexOfAny{T}(ReadOnlySpan{T}, SearchValues{T})"/>
///   for tight ASCII scanning, then falls back to <see cref="char.IsWhiteSpace(char)"/> only when needed.
/// </para>
/// <para>
/// - **ref struct:** Lives on the stack; cannot be boxed, captured, or used across await/yield boundaries.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// ReadOnlySpan&lt;char&gt; s = "  foo\tbar\u00A0baz  ".AsSpan(); // includes a non-breaking space
/// foreach (var token in new WhitespaceSplitEnumerator(s))
/// {
///     // tokens: "foo", "bar", "baz"
/// }
/// </code>
/// </example>
public ref struct WhitespaceSplitEnumerator
{
    private static readonly SearchValues<char> WsAscii = SearchValues.Create(" \t\r\n\f");

    private readonly ReadOnlySpan<char> _span;
    private int _index;

    /// <summary>Creates an enumerator over <paramref name="span"/> that yields non-whitespace tokens.</summary>
    public WhitespaceSplitEnumerator(ReadOnlySpan<char> span)
    {
        _span = span;
        _index = 0;
        Current = default;
    }

    /// <summary>The current token produced by the last successful <see cref="MoveNext"/>.</summary>
    public ReadOnlySpan<char> Current { get; private set; }

    /// <summary>Returns the enumerator itself (required for <c>foreach</c> pattern).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly WhitespaceSplitEnumerator GetEnumerator() => this;

    /// <summary>
    /// Advances to the next token. Returns <c>true</c> and sets <see cref="Current"/> if a token exists; otherwise <c>false</c>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public bool MoveNext()
    {
        var s = _span;
        int i = _index;

        // 1) Skip leading whitespace (ASCII fast path, Unicode-correct fallback)
        while (i < s.Length)
        {
            char c = s[i];
            if (c <= 0x7F)
            {
                if (!IsAsciiWs(c)) break;
                i++;
            }
            else
            {
                if (!char.IsWhiteSpace(c)) break;
                i++;
            }
        }
        if (i >= s.Length) { _index = s.Length; return false; }

        // 2) Find candidate token end via ASCII whitespace
        var rem = s[i..];
        int end = rem.IndexOfAny(WsAscii);

        if (end >= 0)
        {
            // 2a) Check ONLY the prefix [i, i+end) for earlier Unicode WS.
            //     If non-ASCII appears, test it with char.IsWhiteSpace; stop early if found.
            int k = 0;
            int limit = end;
            while (k < limit)
            {
                char c = rem[k];
                if (c > 0x7F)
                {
                    if (char.IsWhiteSpace(c))
                    {
                        end = k; // earlier Unicode WS -> cut here
                        break;
                    }
                }
                k++;
            }
        }
        else
        {
            // 2b) No ASCII WS ahead; scan Unicode-correctly until first WS
            int u = 0;
            while (u < rem.Length && !char.IsWhiteSpace(rem[u])) u++;
            end = u < rem.Length ? u : -1;
        }

        if (end < 0)
        {
            Current = rem;
            _index = s.Length;
            return true;
        }

        // Emit token
        Current = rem[..end];
        i += end;

        // 3) Consume trailing whitespace (ASCII fast path, Unicode fallback)
        while (i < s.Length)
        {
            char c = s[i];
            if (c <= 0x7F)
            {
                if (!IsAsciiWs(c)) break;
                i++;
            }
            else
            {
                if (!char.IsWhiteSpace(c)) break;
                i++;
            }
        }

        _index = i;
        return true;
    }

    // Small, branchless-ish helper for ASCII whitespace classification.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsAsciiWs(char c) => c is ' ' or '\t' or '\r' or '\n' or '\f';
}
