using System;
using BenchmarkDotNet.Attributes;
using Performance.Benchmarks.Original;
using Performance.Extensions;

namespace Performance.Benchmarks.Benches;

[MemoryDiagnoser]
[HideColumns("Error", "StdDev", "Median")]
public class TrimmingBench
{
    [Params(32, 256, 4_096)]
    public int Length;

    [Params(0.0, 0.1, 0.5)]
    public double WhitespaceRatio;

    private byte[] _buffer = null!;

    [GlobalSetup]
    public void Setup() => _buffer = BuildBuffer();

    [Benchmark(Baseline = true, Description = "Old.Trim(memory)")]
    public int Old_TrimMemory()
    {
        return OriginalTrimming.Trim(_buffer.AsMemory()).Length;
    }

    [Benchmark(Description = "New.Trim(memory)")]
    public int New_TrimMemory()
    {
        return Trimming.Trim(_buffer.AsMemory()).Length;
    }

    [Benchmark(Description = "Old.TrimStart(span)")]
    public int Old_TrimStartSpan()
    {
        return OriginalTrimming.TrimStart(_buffer.AsSpan()).Length;
    }

    [Benchmark(Description = "New.TrimStart(span)")]
    public int New_TrimStartSpan()
    {
        return Trimming.TrimStart(_buffer.AsSpan()).Length;
    }

    private byte[] BuildBuffer()
    {
        // Spread whitespace across the start/end with a non-empty payload in the middle.
        int leading = (int)(Length * (WhitespaceRatio / 2.0));
        int trailing = leading;
        int payload = Length - leading - trailing;
        if (payload <= 0)
        {
            payload = 1;
            trailing = Math.Max(0, Length - leading - payload);
            if (trailing == 0 && leading > 0)
            {
                leading = Math.Max(0, Length - 1);
                payload = 1;
            }
        }

        var data = new byte[Length];

        for (int i = 0; i < leading; i++)
        {
            data[i] = i % 2 == 0 ? (byte)0x20 : (byte)0x09; // space / tab
        }

        for (int i = 0; i < payload; i++)
        {
            data[leading + i] = (byte)('A' + (i % 26));
        }

        for (int i = 0; i < trailing; i++)
        {
            data[leading + payload + i] = i % 2 == 0 ? (byte)0x0D : (byte)0x0A; // CR / LF
        }

        return data;
    }
}
