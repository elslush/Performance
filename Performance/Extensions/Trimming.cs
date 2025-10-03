using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Performance.Extensions;

/// <summary>
/// Trimming helpers for <see cref="Memory{T}"/> and
/// <see cref="ReadOnlySpan{T}"/> that remove leading/trailing ASCII
/// whitespace bytes (space, tab, CR, LF) without allocations.
/// </summary>
/// <remarks>
/// - Default trim set is ASCII: 0x20 (space), 0x09 (tab), 0x0D (CR), 0x0A (LF).  
/// - Span-based overloads return slices into the original buffer (no copying).  
/// - The <see cref="Memory{T}"/> overload delegates to a
///   byte-set overload (expected to exist elsewhere) to avoid duplicating logic.
/// </remarks>
public static class Trimming
{
    /// <summary>
    /// Trims leading and trailing ASCII whitespace from a <see cref="Memory{T}"/>.
    /// </summary>
    /// <param name="memory">The buffer to trim.</param>
    /// <returns>
    /// A <see cref="Memory{T}"/> view of <paramref name="memory"/> with
    /// leading and trailing ASCII whitespace removed. If empty, returns the input.
    /// </returns>
    public static Memory<byte> Trim(this Memory<byte> memory)
    {
        if (memory.IsEmpty)
        {
            return memory;
        }

        Span<byte> trimBytes = stackalloc byte[] { 0x20, 0x09, 0x0D, 0x0A };

        return memory.Trim(trimBytes);
    }

    /// <summary>
    /// Trims leading and trailing ASCII whitespace from a <see cref="ReadOnlySpan{T}"/>.
    /// </summary>
    /// <param name="span">The input bytes.</param>
    /// <returns>
    /// A slice of <paramref name="span"/> with leading/trailing ASCII whitespace removed.
    /// Returns the original span if it is empty.
    /// </returns>
    public static ReadOnlySpan<byte> Trim(this ReadOnlySpan<byte> span)
    {
        if (span.IsEmpty)
        {
            return span;
        }

        Span<byte> trimBytes = stackalloc byte[] { 0x20, 0x09, 0x0D, 0x0A };

        int start = ClampStart(span, trimBytes);
        int length = ClampEnd(span, start, trimBytes);
        return span.Slice(start, length);
    }

    /// <summary>
    /// Trims only the leading ASCII whitespace from a <see cref="ReadOnlySpan{T}"/>.
    /// </summary>
    /// <param name="span">The input bytes.</param>
    /// <returns>
    /// A slice of <paramref name="span"/> starting at the first non-ASCII-whitespace byte.
    /// Returns the original span if it is empty.
    /// </returns>
    public static ReadOnlySpan<byte> TrimStart(this ReadOnlySpan<byte> span)
    {
        if (span.IsEmpty)
        {
            return span;
        }

        Span<byte> trimBytes = stackalloc byte[] { 0x20, 0x09, 0x0D, 0x0A };

        int start = ClampStart(span, trimBytes);
        return span[start..];
    }

    /// <summary>
    /// Scans from the left until finding the first element not contained in <paramref name="trimElements"/>.
    /// </summary>
    /// <typeparam name="T">Element type, compared via <see cref="IEquatable{T}"/>.</typeparam>
    /// <param name="span">The span to scan.</param>
    /// <param name="trimElements">Set of elements to trim.</param>
    /// <returns>Index of first non-trim element; equals <c>span.Length</c> if all trimmed.</returns>
    private static int ClampStart<T>(ReadOnlySpan<T> span, ReadOnlySpan<T> trimElements) where T : IEquatable<T>?
    {
        int start = 0;
        for (; start < span.Length; start++)
        {
            if (!trimElements.Contains(span[start]))
            {
                break;
            }
        }

        return start;
    }

    /// <summary>
    /// Scans from the right to compute the length of the slice after trimming, given a left bound.
    /// </summary>
    /// <typeparam name="T">Element type, compared via <see cref="System.IEquatable{T}"/>.</typeparam>
    /// <param name="span">The span to scan.</param>
    /// <param name="start">The leftmost kept index (from <see cref="ClampStart{T}(System.ReadOnlySpan{T}, System.ReadOnlySpan{T})"/>).</param>
    /// <param name="trimElements">Set of elements to trim.</param>
    /// <returns>The length of the kept slice (may be 0).</returns>
    private static int ClampEnd<T>(ReadOnlySpan<T> span, int start, ReadOnlySpan<T> trimElements) where T : IEquatable<T>?
    {
        // Initially, start==len==0. If ClampStart trims all, start==len
        Debug.Assert((uint)start <= span.Length);

        int end = span.Length - 1;
        for (; end >= start; end--)
        {
            if (!trimElements.Contains(span[end]))
            {
                break;
            }
        }

        return end - start + 1;
    }
}
