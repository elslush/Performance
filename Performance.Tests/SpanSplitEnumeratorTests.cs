using Performance.Enumerators;
using System;
using System.Collections.Generic;
using System.Text;

namespace Performance.Tests;

public sealed class SpanSplitEnumeratorTests
{
    // ---------- char tests ----------

    [Fact]
    public void Char_EmptyInput_YieldsSingleEmpty()
    {
        var segs = SplitChars(string.Empty, ',');
        EqualSeq(new[] { "" }, segs);
    }

    [Fact]
    public void Char_NoSeparator_ReturnsWholeSpan()
    {
        var segs = SplitChars("alpha", ',');
        EqualSeq(new[] { "alpha" }, segs);
    }

    [Fact]
    public void Char_LeadingSeparator_EmitsEmptyFirst()
    {
        var segs = SplitChars(",alpha,beta", ',');
        EqualSeq(new[] { "", "alpha", "beta" }, segs);
    }

    [Fact]
    public void Char_TrailingSeparator_EmitsEmptyLast()
    {
        var segs = SplitChars("alpha,beta,", ',');
        EqualSeq(new[] { "alpha", "beta", "" }, segs);
    }

    [Fact]
    public void Char_ConsecutiveSeparators_EmitEmptyBetween()
    {
        var segs = SplitChars("alpha,,beta,,,gamma", ',');
        EqualSeq(new[] { "alpha", "", "beta", "", "", "gamma" }, segs);
    }

    [Fact]
    public void Char_Unicode_Works()
    {
        var input = "α,β,γ,δ"; // Greek letters (still char code units)
        var segs = SplitChars(input, ',');
        EqualSeq(new[] { "α", "β", "γ", "δ" }, segs);
    }

    // ---------- byte tests ----------

    [Fact]
    public void Byte_BasicCases()
    {
        var data = new byte[] { 1, 2, 0, 3, 0, 0, 4 };
        var segs = SplitBytes(data, 0);
        EqualSeq(new[]
        {
            new byte[] { 1, 2 },
            new byte[] { 3 },
            Array.Empty<byte>(),
            new byte[] { 4 },
        }, segs);
    }

    [Fact]
    public void Byte_EmptyInput_YieldsSingleEmpty()
    {
        var segs = SplitBytes(Array.Empty<byte>(), 0);
        EqualSeq(new[] { Array.Empty<byte>() }, segs);
    }

    [Fact]
    public void Byte_NoSeparator_ReturnsWholeSpan()
    {
        var data = new byte[] { 10, 20, 30 };
        var segs = SplitBytes(data, 0);
        EqualSeq(new[] { new byte[] { 10, 20, 30 } }, segs);
    }

    // ---------- int tests ----------

    [Fact]
    public void Int_BasicCases()
    {
        var data = new[] { -1, 7, 7, 0, 42, 7, 7, 7, 9 };
        var segs = SplitInts(data, 7);
        EqualSeq(new[]
        {
            new[] { -1 },
            Array.Empty<int>(),
            new[] { 0, 42 },
            Array.Empty<int>(),
            Array.Empty<int>(),
            new[] { 9 },
        }, segs);
    }

    [Fact]
    public void Int_Leading_Trailing()
    {
        var data = new[] { 5, 1, 2, 3, 5 };
        var segs = SplitInts(data, 5);
        EqualSeq(new[]
        {
            Array.Empty<int>(),
            new[] { 1, 2, 3 },
            Array.Empty<int>(),
        }, segs);
    }

    // ---------- fuzz tests (char & byte) ----------

    [Fact]
    public void Char_Fuzz_Equals_Reference()
    {
        var seed = 12345;
        var rng = new Random(seed);

        for (int iter = 0; iter < 100; iter++)
        {
            char sep = RandomSeparatorChar(rng);
            string s = RandomCharPayload(rng, length: 2000, sep);

            var expected = ReferenceSplitChars(s, sep);
            var actual = SplitChars(s, sep);

            EqualSeq(expected, actual);
        }
    }

    [Fact]
    public void Byte_Fuzz_Equals_Reference()
    {
        var seed = 23456;
        var rng = new Random(seed);

        for (int iter = 0; iter < 100; iter++)
        {
            byte sep = (byte)rng.Next(0, 256);
            byte[] data = RandomBytePayload(rng, length: 6000, sep);

            var expected = ReferenceSplitBytes(data, sep);
            var actual = SplitBytes(data, sep);

            EqualSeq(expected, actual);
        }
    }

    // ---------- helpers: run enumerator & materialize segments ----------

    private static List<string> SplitChars(string s, char sep)
    {
        var list = new List<string>();
        foreach (var seg in new SpanSplitEnumerator<char>(s.AsSpan(), sep))
            list.Add(seg.ToString());
        return list;
    }

    private static List<byte[]> SplitBytes(ReadOnlySpan<byte> span, byte sep)
    {
        var list = new List<byte[]>();
        foreach (var seg in new SpanSplitEnumerator<byte>(span, sep))
            list.Add(seg.ToArray()); // ok to allocate in tests
        return list;
    }

    private static List<int[]> SplitInts(ReadOnlySpan<int> span, int sep)
    {
        var list = new List<int[]>();
        foreach (var seg in new SpanSplitEnumerator<int>(span, sep))
            list.Add(seg.ToArray());
        return list;
    }

    // ---------- helpers: reference splitters matching enumerator semantics ----------

    private static List<string> ReferenceSplitChars(string s, char sep)
    {
        var result = new List<string>();
        int i = 0;
        while (true)
        {
            if (i > s.Length) break;
            int idx = s.AsSpan(i).IndexOf(sep);
            if (idx == -1)
            {
                result.Add(s[i..]); // remainder (can be empty)
                break;
            }
            result.Add(s.Substring(i, idx)); // can be empty
            i += idx + 1;
        }
        return result;
    }

    private static List<byte[]> ReferenceSplitBytes(ReadOnlySpan<byte> span, byte sep)
    {
        var result = new List<byte[]>();
        int i = 0;
        while (true)
        {
            if (i > span.Length) break;
            int idx = span[i..].IndexOf(sep);
            if (idx == -1)
            {
                result.Add(span[i..].ToArray());
                break;
            }
            result.Add(span.Slice(i, idx).ToArray());
            i += idx + 1;
        }
        return result;
    }

    // ---------- helpers: equality for sequences of sequences ----------

    private static void EqualSeq(IReadOnlyList<string> expected, IReadOnlyList<string> actual)
    {
        Assert.Equal(expected.Count, actual.Count);
        for (int i = 0; i < expected.Count; i++)
            Assert.Equal(expected[i], actual[i]);
    }

    private static void EqualSeq(IReadOnlyList<byte[]> expected, IReadOnlyList<byte[]> actual)
    {
        Assert.Equal(expected.Count, actual.Count);
        for (int i = 0; i < expected.Count; i++)
            Assert.True(expected[i].AsSpan().SequenceEqual(actual[i]), $"Segment {i} differs");
    }

    private static void EqualSeq(IReadOnlyList<int[]> expected, IReadOnlyList<int[]> actual)
    {
        Assert.Equal(expected.Count, actual.Count);
        for (int i = 0; i < expected.Count; i++)
            Assert.True(expected[i].AsSpan().SequenceEqual(actual[i]), $"Segment {i} differs");
    }

    // ---------- helpers: fuzz data generation ----------

    private static char RandomSeparatorChar(Random rng)
    {
        // Avoid NUL to keep string ops simple; include punctuation & some letters
        const string candidates = ",;|:/.-_ \t\n\r\f=+*#@~";
        return candidates[rng.Next(candidates.Length)];
    }

    private static string RandomCharPayload(Random rng, int length, char sep)
    {
        var letters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789αβγδεζηθικλμνξοπρστυφχψω";
        var sb = new StringBuilder(length + 32);
        while (sb.Length < length)
        {
            if (rng.NextDouble() < 0.20)
            {
                // more likely to emit the chosen separator (including runs)
                int run = 1 + rng.Next(3);
                for (int j = 0; j < run && sb.Length < length; j++) sb.Append(sep);
            }
            else
            {
                int wl = 1 + rng.Next(8);
                for (int j = 0; j < wl && sb.Length < length; j++)
                    sb.Append(letters[rng.Next(letters.Length)]);
            }
        }
        return sb.ToString(0, length);
    }

    private static byte[] RandomBytePayload(Random rng, int length, byte sep)
    {
        var buf = new byte[length];
        for (int i = 0; i < length; i++)
        {
            if (rng.NextDouble() < 0.15)
                buf[i] = sep;
            else
                buf[i] = (byte)rng.Next(0, 256);
        }
        return buf;
    }
}
