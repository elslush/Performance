using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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
    // Common ASCII whitespace set used by all trim helpers.
    private static readonly SearchValues<byte> Whitespace = SearchValues.Create(stackalloc byte[] { 0x20, 0x09, 0x0D, 0x0A });
    private const int DefaultChunkSize = 32;
    private const int ByteSize = 1;
    


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
        if (memory.IsEmpty) return memory;

        var span = memory.Span;
        var (start, length) = ComputeTrim(span);
        return memory.Slice(start, length);
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
        if (span.IsEmpty) return span;

        var (start, length) = ComputeTrim(span);
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
        if (span.IsEmpty) return span;

        int start = IndexOfFirstNonWhitespace(span);
        if (start < 0) return ReadOnlySpan<byte>.Empty; // all whitespace
        return span[start..];
    }

    // Compute start index and length of the trimmed slice.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (int start, int length) ComputeTrim(ReadOnlySpan<byte> span)
    {
        int start = IndexOfFirstNonWhitespace(span);
        if (start < 0) return (0, 0);              // all whitespace

        int end = LastIndexOfNonWhitespace(span);
        return (start, end - start + 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int IndexOfFirstNonWhitespace(ReadOnlySpan<byte> span)
    {
        int idx = span.IndexOfAnyExcept(Whitespace);
        return idx; // returns -1 if all bytes are in the whitespace set
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int LastIndexOfNonWhitespace(ReadOnlySpan<byte> span)
    {
        // Optimized search from end using bounds checking
        for (int i = span.Length - 1; i >= 0; i--)
        {
            if (!Whitespace.Contains(span[i]))
                return i;
        }
        return -1;
    }
}
