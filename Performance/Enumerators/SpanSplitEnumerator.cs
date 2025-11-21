using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Performance.Enumerators;

///
/// <summary>
/// A lightweight, allocation-free enumerator that splits a <see cref="ReadOnlySpan{T}"/>
/// by a single-element separator and yields each segment as a <see cref="ReadOnlySpan{T}"/>.
/// </summary>
/// <typeparam name="T">
/// Element type of the span. Must implement <see cref="IEquatable{T}"/> so
/// <see cref="MemoryExtensions.IndexOf"/> can locate the separator.
/// </typeparam>
/// <remarks>
/// <para>
/// - This is a <c>ref struct</c>; it lives on the stack and cannot be boxed, captured,
///   stored in fields on reference types, or used across async/yield boundaries.
/// </para>
/// <para>
/// - Matches the "enumerator pattern" (<c>GetEnumerator</c>, <c>MoveNext</c>, <c>Current</c>)
///   so it can be used directly in <c>foreach</c> without allocations.
/// </para>
/// <para>
/// - Consecutive separators yield empty segments. A trailing separator yields a final
///   empty segment. An empty input yields a single empty segment—consistent with many
///   split semantics.
/// </para>
/// <para>
/// - Time complexity: O(n). The implementation advances through the span and uses
///   <see cref="MemoryExtensions.IndexOf"/>  which is vectorized for primitives.</para>
/// </remarks>
/// <example>
/// <code>
/// ReadOnlySpan&lt;char&gt; s = "a,,b,".AsSpan();
/// foreach (var part in new SpanSplitEnumerator&lt;char&gt;(s, ','))
/// {
///     // parts: "a"  ""  "b"  ""
/// }
/// </code>
/// </example>
///
public ref struct SpanSplitEnumerator<T> where T : IEquatable<T>
{
    private readonly ReadOnlySpan<T> _span;
    private readonly T _separator;
    private int _currentIndex;
    private const int NoMatchFound = -1;
    private const int FirstElement = 0;
    private const int LastElement = -1;



    /// <summary>
    /// Initializes a new enumerator over <paramref name="span"/> splitting by <paramref name="separator"/>.
    /// </summary>
    public SpanSplitEnumerator(ReadOnlySpan<T> span, T separator)
    {
        _span = span;
        _separator = separator;
        _currentIndex = 0;
        Current = default;
    }

    /// <summary>
    /// The current segment produced by the most recent call to <see cref="MoveNext"/>.
    /// </summary>
    public ReadOnlySpan<T> Current { get; private set; }

    /// <summary>
    /// Returns the enumerator itself. Required for <c>foreach</c> pattern.
    /// </summary>
    public readonly SpanSplitEnumerator<T> GetEnumerator() => this;

    /// <summary>
    /// Advances to the next segment. Returns <c>true</c> if a segment is available;
    /// otherwise <c>false</c>.
    /// </summary>
    public bool MoveNext()
    {
        if (_currentIndex > _span.Length)
            return false;

        var remainingSpan = _span[_currentIndex..];
        int index = remainingSpan.IndexOf(_separator);
        if (index == NoMatchFound)
        {
            Current = remainingSpan;
            _currentIndex = _span.Length + 1; // Move past the end
            return true;
        }

        Current = remainingSpan.Slice(FirstElement, index);
        _currentIndex += index + 1;
        return true;
    }
}
