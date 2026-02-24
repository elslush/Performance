using System.Text;

using BenchmarkDotNet.Attributes;

using Performance.Enumerators;

namespace Performance.Benchmarks.Benches;

[MemoryDiagnoser]
[HideColumns("Error", "StdDev", "Median")]
public class SpanSplitEnumeratorBench
{
    [Params(128, 1_024, 8_192)]
    public int Length;

    [Params(0.05, 0.20)]
    public double SeparatorRatio;

    private string _input = string.Empty;

    [GlobalSetup]
    public void Setup()
    {
        _input = BuildInput(Length, SeparatorRatio);
    }

    [Benchmark(Baseline = true, Description = "SpanSplitEnumerator<char>")]
    public int SpanSplitEnumerator()
    {
        int total = 0;
        foreach (var segment in new SpanSplitEnumerator<char>(_input.AsSpan(), ','))
        {
            total += segment.Length;
        }

        return total;
    }

    [Benchmark(Description = "string.Split(',')")]
    public int StringSplit()
    {
        int total = 0;
        var parts = _input.Split(',');
        for (int i = 0; i < parts.Length; i++)
        {
            total += parts[i].Length;
        }

        return total;
    }

    [Benchmark(Description = "Manual IndexOf Loop")]
    public int ManualIndexOfLoop()
    {
        var remaining = _input.AsSpan();
        int total = 0;

        while (true)
        {
            int index = remaining.IndexOf(',');
            if (index < 0)
            {
                total += remaining.Length;
                break;
            }

            total += index;
            remaining = remaining[(index + 1)..];
        }

        return total;
    }

    private static string BuildInput(int length, double separatorRatio)
    {
        var rng = new XorShift32((uint)(length * 31 + (int)(separatorRatio * 10_000)));
        var sb = new StringBuilder(length);

        while (sb.Length < length)
        {
            if (rng.NextDouble() < separatorRatio)
            {
                sb.Append(',');
                continue;
            }

            sb.Append((char)('a' + rng.Next(26)));
        }

        return sb.ToString();
    }

    private struct XorShift32(uint seed)
    {
        private uint _state = seed == 0 ? 2463534242u : seed;

        public uint Next()
        {
            uint value = _state;
            value ^= value << 13;
            value ^= value >> 17;
            value ^= value << 5;
            _state = value;
            return value;
        }

        public int Next(int maxExclusive) => (int)(Next() % (uint)maxExclusive);

        public double NextDouble() => (Next() & 0xFFFFFF) / (double)0x1000000;
    }
}
