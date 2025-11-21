using System;
using System.Diagnostics;

namespace Performance.Benchmarks.Original;

public static class OriginalTrimming
{
    public static Memory<byte> Trim(this Memory<byte> memory)
    {
        if (memory.IsEmpty)
        {
            return memory;
        }

        Span<byte> trimBytes = stackalloc byte[] { 0x20, 0x09, 0x0D, 0x0A };

        return memory.Trim(trimBytes);
    }

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

    private static int ClampEnd<T>(ReadOnlySpan<T> span, int start, ReadOnlySpan<T> trimElements) where T : IEquatable<T>?
    {
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
