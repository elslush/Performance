using BenchmarkDotNet.Attributes;
using Performance.Benchmarks.Whitespace;
using Performance.Enumerators;
using System;
using System.Collections.Generic;
using System.Text;

namespace Performance.Benchmarks.Benches;

[MemoryDiagnoser]
[HideColumns("Error", "StdDev", "Median")]
public class WhitespaceSplitBench
{
    // --- Parameters ---
    [Params(1_000)]//, 10_000, 100_000
    public int Length;

    [Params(0.10)]//, 0.30, 0.50
    public double WhitespaceRatio;

    private string _text = string.Empty;

    [GlobalSetup]
    public void Setup()
    {
        _text = GenerateSampleText(Length, WhitespaceRatio);
    }

    // Baseline: original enumerator
    [Benchmark(Baseline = true)]
    public int OriginalEnumerator()
    {
        int acc = 0;
        var e = new OriginalWhitespaceSplitEnumerator(_text.AsSpan());
        foreach (var token in e)
            acc += token.Length;
        return acc;
    }

    // Optimized: SearchValues + IndexOfAny/Except
    [Benchmark]
    public int OptimizedEnumerator()
    {
        int acc = 0;
        var e = new WhitespaceSplitEnumerator(_text.AsSpan());
        foreach (var token in e)
            acc += token.Length;
        return acc;
    }

    // --- Helpers ---

    private static string GenerateSampleText(int length, double wsRatio)
    {
        // Build a predictable pseudo-random mix of ASCII words and whitespace.
        // Includes some unicode whitespace when UnicodeAware is true (handled by bench param in enumerator).
        var rng = new XorShift32(12345);
        ReadOnlySpan<string> words =
        [
            "alpha","beta","gamma","delta","epsilon","zeta","eta","theta",
            "iota","kappa","lambda","mu","nu","xi","omicron","pi","rho",
            "sigma","tau","upsilon","phi","chi","psi","omega"
        ];
        // A small set of whitespace chars (ASCII) + few Unicode; they’ll appear regardless,
        // but only the Unicode-aware mode will treat the Unicode ones as whitespace.
        var ws = new[] { ' ', '\t', '\r', '\n', '\f', '\u00A0', '\u2003', '\u202F' };

        var sb = new System.Text.StringBuilder(length + 64);
        while (sb.Length < length)
        {
            bool emitWs = rng.NextDouble() < wsRatio;
            if (emitWs)
            {
                int count = 1 + rng.Next(4);
                for (int i = 0; i < count; i++)
                    sb.Append(ws[rng.Next(ws.Length)]);
            }
            else
            {
                var w = words[rng.Next(words.Length)];
                sb.Append(w);
            }
        }
        return sb.ToString(0, length);
    }

    // Tiny fast PRNG to keep generation stable and dependency-free
    private struct XorShift32(uint seed)
    {
        private uint _s = seed == 0 ? 2463534242u : seed;
        public uint Next() { uint x = _s; x ^= x << 13; x ^= x >> 17; x ^= x << 5; _s = x; return x; }
        public int Next(int max) => (int)(Next() % (uint)max);
        public double NextDouble() => (Next() & 0xFFFFFF) / (double)0x1000000;
    }
}