using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Performance.Enumerators;

public ref struct WhitespaceSplitEnumerator
{
    private static readonly SearchValues<char> WsAscii = SearchValues.Create(" \t\r\n\f");

    private readonly ReadOnlySpan<char> _span;
    private int _index;

    public WhitespaceSplitEnumerator(ReadOnlySpan<char> span)
    {
        _span = span;
        _index = 0;
        Current = default;
    }

    public ReadOnlySpan<char> Current { get; private set; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly WhitespaceSplitEnumerator GetEnumerator() => this;

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsAsciiWs(char c) => c is ' ' or '\t' or '\r' or '\n' or '\f';
}
