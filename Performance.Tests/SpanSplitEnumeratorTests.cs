using System;
using System.Collections.Generic;
using System.Text;

using Performance.Enumerators;

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

    /// <summary>
    /// Verifies that GetEnumerator returns a valid enumerator that can iterate through char segments.
    /// Tests with a simple char span containing separators.
    /// </summary>
    [Fact]
    public void GetEnumerator_ReturnsValidEnumerator_ForCharSpan()
    {
        // Arrange
        ReadOnlySpan<char> span = "a,b,c".AsSpan();
        var splitter = new SpanSplitEnumerator<char>(span, ',');

        // Act
        var enumerator = splitter.GetEnumerator();
        var results = new List<string>();
        while (enumerator.MoveNext())
        {
            results.Add(enumerator.Current.ToString());
        }

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal("a", results[0]);
        Assert.Equal("b", results[1]);
        Assert.Equal("c", results[2]);
    }

    /// <summary>
    /// Verifies that GetEnumerator returns a valid enumerator that can iterate through byte segments.
    /// Tests with a byte span containing separators.
    /// </summary>
    [Fact]
    public void GetEnumerator_ReturnsValidEnumerator_ForByteSpan()
    {
        // Arrange
        ReadOnlySpan<byte> span = new byte[] { 1, 0, 2, 0, 3 };
        var splitter = new SpanSplitEnumerator<byte>(span, 0);

        // Act
        var enumerator = splitter.GetEnumerator();
        var results = new List<byte[]>();
        while (enumerator.MoveNext())
        {
            results.Add(enumerator.Current.ToArray());
        }

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal(new byte[] { 1 }, results[0]);
        Assert.Equal(new byte[] { 2 }, results[1]);
        Assert.Equal(new byte[] { 3 }, results[2]);
    }

    /// <summary>
    /// Verifies that GetEnumerator returns a valid enumerator that can iterate through int segments.
    /// Tests with an int span containing separators.
    /// </summary>
    [Fact]
    public void GetEnumerator_ReturnsValidEnumerator_ForIntSpan()
    {
        // Arrange
        ReadOnlySpan<int> span = new int[] { 10, -1, 20, -1, 30 };
        var splitter = new SpanSplitEnumerator<int>(span, -1);

        // Act
        var enumerator = splitter.GetEnumerator();
        var results = new List<int[]>();
        while (enumerator.MoveNext())
        {
            results.Add(enumerator.Current.ToArray());
        }

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal(new int[] { 10 }, results[0]);
        Assert.Equal(new int[] { 20 }, results[1]);
        Assert.Equal(new int[] { 30 }, results[2]);
    }

    /// <summary>
    /// Verifies that calling GetEnumerator multiple times returns independent enumerator copies.
    /// Each enumerator should be able to iterate from the beginning independently.
    /// </summary>
    [Fact]
    public void GetEnumerator_MultipleCalls_ReturnIndependentCopies()
    {
        // Arrange
        ReadOnlySpan<char> span = "x,y".AsSpan();
        var splitter = new SpanSplitEnumerator<char>(span, ',');

        // Act
        var enumerator1 = splitter.GetEnumerator();
        var enumerator2 = splitter.GetEnumerator();

        var results1 = new List<string>();
        while (enumerator1.MoveNext())
        {
            results1.Add(enumerator1.Current.ToString());
        }

        var results2 = new List<string>();
        while (enumerator2.MoveNext())
        {
            results2.Add(enumerator2.Current.ToString());
        }

        // Assert
        Assert.Equal(2, results1.Count);
        Assert.Equal("x", results1[0]);
        Assert.Equal("y", results1[1]);
        Assert.Equal(2, results2.Count);
        Assert.Equal("x", results2[0]);
        Assert.Equal("y", results2[1]);
    }

    /// <summary>
    /// Verifies that GetEnumerator returns a valid enumerator for an empty span.
    /// Should yield a single empty segment.
    /// </summary>
    [Fact]
    public void GetEnumerator_WithEmptySpan_ReturnsValidEnumerator()
    {
        // Arrange
        ReadOnlySpan<char> span = "".AsSpan();
        var splitter = new SpanSplitEnumerator<char>(span, ',');

        // Act
        var enumerator = splitter.GetEnumerator();
        var results = new List<string>();
        while (enumerator.MoveNext())
        {
            results.Add(enumerator.Current.ToString());
        }

        // Assert
        Assert.Single(results);
        Assert.Equal("", results[0]);
    }

    /// <summary>
    /// Verifies that GetEnumerator returns a valid enumerator when no separator is present.
    /// Should yield the entire span as a single segment.
    /// </summary>
    [Fact]
    public void GetEnumerator_WithNoSeparator_ReturnsWholeSpan()
    {
        // Arrange
        ReadOnlySpan<char> span = "hello".AsSpan();
        var splitter = new SpanSplitEnumerator<char>(span, ',');

        // Act
        var enumerator = splitter.GetEnumerator();
        var results = new List<string>();
        while (enumerator.MoveNext())
        {
            results.Add(enumerator.Current.ToString());
        }

        // Assert
        Assert.Single(results);
        Assert.Equal("hello", results[0]);
    }

    /// <summary>
    /// Verifies that GetEnumerator can be used directly in a foreach loop (foreach pattern).
    /// Tests the idiomatic C# usage of the enumerator.
    /// </summary>
    [Fact]
    public void GetEnumerator_CanBeUsedInForeach()
    {
        // Arrange
        ReadOnlySpan<char> span = "a,b,c".AsSpan();
        var splitter = new SpanSplitEnumerator<char>(span, ',');

        // Act
        var results = new List<string>();
        foreach (var segment in splitter.GetEnumerator())
        {
            results.Add(segment.ToString());
        }

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal("a", results[0]);
        Assert.Equal("b", results[1]);
        Assert.Equal("c", results[2]);
    }

    /// <summary>
    /// Verifies that GetEnumerator returns a valid enumerator with leading separator.
    /// Should yield an empty segment first, then remaining segments.
    /// </summary>
    [Fact]
    public void GetEnumerator_WithLeadingSeparator_EmitsEmptyFirst()
    {
        // Arrange
        ReadOnlySpan<char> span = ",a,b".AsSpan();
        var splitter = new SpanSplitEnumerator<char>(span, ',');

        // Act
        var enumerator = splitter.GetEnumerator();
        var results = new List<string>();
        while (enumerator.MoveNext())
        {
            results.Add(enumerator.Current.ToString());
        }

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal("", results[0]);
        Assert.Equal("a", results[1]);
        Assert.Equal("b", results[2]);
    }

    /// <summary>
    /// Verifies that GetEnumerator returns a valid enumerator with trailing separator.
    /// Should yield segments followed by an empty segment last.
    /// </summary>
    [Fact]
    public void GetEnumerator_WithTrailingSeparator_EmitsEmptyLast()
    {
        // Arrange
        ReadOnlySpan<char> span = "a,b,".AsSpan();
        var splitter = new SpanSplitEnumerator<char>(span, ',');

        // Act
        var enumerator = splitter.GetEnumerator();
        var results = new List<string>();
        while (enumerator.MoveNext())
        {
            results.Add(enumerator.Current.ToString());
        }

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal("a", results[0]);
        Assert.Equal("b", results[1]);
        Assert.Equal("", results[2]);
    }

    /// <summary>
    /// Verifies that GetEnumerator returns a valid enumerator with consecutive separators.
    /// Should yield empty segments between consecutive separators.
    /// </summary>
    [Fact]
    public void GetEnumerator_WithConsecutiveSeparators_EmitsEmptyBetween()
    {
        // Arrange
        ReadOnlySpan<char> span = "a,,b".AsSpan();
        var splitter = new SpanSplitEnumerator<char>(span, ',');

        // Act
        var enumerator = splitter.GetEnumerator();
        var results = new List<string>();
        while (enumerator.MoveNext())
        {
            results.Add(enumerator.Current.ToString());
        }

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal("a", results[0]);
        Assert.Equal("", results[1]);
        Assert.Equal("b", results[2]);
    }

    /// <summary>
    /// Verifies that MoveNext returns true for the first call on an empty span,
    /// yielding a single empty segment, and returns false on subsequent calls.
    /// </summary>
    [Fact]
    public void MoveNext_EmptySpan_ReturnsTrueOnceThenFalse()
    {
        // Arrange
        ReadOnlySpan<char> span = ReadOnlySpan<char>.Empty;
        var enumerator = new SpanSplitEnumerator<char>(span, ',');

        // Act & Assert - First call should return true with empty segment
        Assert.True(enumerator.MoveNext());
        Assert.True(enumerator.Current.IsEmpty);

        // Act & Assert - Second call should return false
        Assert.False(enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that MoveNext returns true once for a span with no separator,
    /// yielding the entire span, then returns false on subsequent calls.
    /// </summary>
    [Fact]
    public void MoveNext_NoSeparator_ReturnsTrueOnceThenFalse()
    {
        // Arrange
        ReadOnlySpan<char> span = "abcdef".AsSpan();
        var enumerator = new SpanSplitEnumerator<char>(span, ',');

        // Act & Assert - First call returns true with entire span
        Assert.True(enumerator.MoveNext());
        Assert.Equal("abcdef", enumerator.Current.ToString());

        // Act & Assert - Second call returns false
        Assert.False(enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that MoveNext correctly processes a single separator,
    /// yielding two empty segments before returning false.
    /// </summary>
    [Fact]
    public void MoveNext_SingleSeparator_YieldsTwoEmptySegments()
    {
        // Arrange
        ReadOnlySpan<char> span = ",".AsSpan();
        var enumerator = new SpanSplitEnumerator<char>(span, ',');

        // Act & Assert - First segment (before separator)
        Assert.True(enumerator.MoveNext());
        Assert.True(enumerator.Current.IsEmpty);

        // Act & Assert - Second segment (after separator)
        Assert.True(enumerator.MoveNext());
        Assert.True(enumerator.Current.IsEmpty);

        // Act & Assert - No more segments
        Assert.False(enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that MoveNext correctly processes a leading separator,
    /// yielding an empty first segment followed by the remaining content.
    /// </summary>
    [Fact]
    public void MoveNext_LeadingSeparator_YieldsEmptyFirstSegment()
    {
        // Arrange
        ReadOnlySpan<char> span = ",abc".AsSpan();
        var enumerator = new SpanSplitEnumerator<char>(span, ',');

        // Act & Assert - First segment (empty before separator)
        Assert.True(enumerator.MoveNext());
        Assert.True(enumerator.Current.IsEmpty);

        // Act & Assert - Second segment (remaining content)
        Assert.True(enumerator.MoveNext());
        Assert.Equal("abc", enumerator.Current.ToString());

        // Act & Assert - No more segments
        Assert.False(enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that MoveNext correctly processes a trailing separator,
    /// yielding the content followed by an empty final segment.
    /// </summary>
    [Fact]
    public void MoveNext_TrailingSeparator_YieldsEmptyLastSegment()
    {
        // Arrange
        ReadOnlySpan<char> span = "abc,".AsSpan();
        var enumerator = new SpanSplitEnumerator<char>(span, ',');

        // Act & Assert - First segment
        Assert.True(enumerator.MoveNext());
        Assert.Equal("abc", enumerator.Current.ToString());

        // Act & Assert - Second segment (empty after trailing separator)
        Assert.True(enumerator.MoveNext());
        Assert.True(enumerator.Current.IsEmpty);

        // Act & Assert - No more segments
        Assert.False(enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that MoveNext correctly processes consecutive separators,
    /// yielding empty segments between them.
    /// </summary>
    [Fact]
    public void MoveNext_ConsecutiveSeparators_YieldsEmptySegments()
    {
        // Arrange
        ReadOnlySpan<char> span = "a,,b".AsSpan();
        var enumerator = new SpanSplitEnumerator<char>(span, ',');

        // Act & Assert - First segment "a"
        Assert.True(enumerator.MoveNext());
        Assert.Equal("a", enumerator.Current.ToString());

        // Act & Assert - Second segment (empty between separators)
        Assert.True(enumerator.MoveNext());
        Assert.True(enumerator.Current.IsEmpty);

        // Act & Assert - Third segment "b"
        Assert.True(enumerator.MoveNext());
        Assert.Equal("b", enumerator.Current.ToString());

        // Act & Assert - No more segments
        Assert.False(enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that MoveNext continues to return false after enumeration is exhausted,
    /// even when called multiple times.
    /// </summary>
    [Fact]
    public void MoveNext_CalledAfterExhaustion_ContinuesReturningFalse()
    {
        // Arrange
        ReadOnlySpan<char> span = "a".AsSpan();
        var enumerator = new SpanSplitEnumerator<char>(span, ',');

        // Act - Exhaust the enumeration
        Assert.True(enumerator.MoveNext());
        Assert.False(enumerator.MoveNext());

        // Assert - Multiple calls after exhaustion all return false
        Assert.False(enumerator.MoveNext());
        Assert.False(enumerator.MoveNext());
        Assert.False(enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that MoveNext correctly processes multiple segments separated by the separator,
    /// returning true for each segment and false when exhausted.
    /// </summary>
    [Fact]
    public void MoveNext_MultipleSegments_ReturnsCorrectSequence()
    {
        // Arrange
        ReadOnlySpan<char> span = "one,two,three".AsSpan();
        var enumerator = new SpanSplitEnumerator<char>(span, ',');

        // Act & Assert - First segment
        Assert.True(enumerator.MoveNext());
        Assert.Equal("one", enumerator.Current.ToString());

        // Act & Assert - Second segment
        Assert.True(enumerator.MoveNext());
        Assert.Equal("two", enumerator.Current.ToString());

        // Act & Assert - Third segment
        Assert.True(enumerator.MoveNext());
        Assert.Equal("three", enumerator.Current.ToString());

        // Act & Assert - No more segments
        Assert.False(enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that MoveNext works correctly with byte spans,
    /// splitting by a byte separator.
    /// </summary>
    [Fact]
    public void MoveNext_ByteSpan_SplitsCorrectly()
    {
        // Arrange
        ReadOnlySpan<byte> span = new byte[] { 1, 0, 2, 0, 3 };
        var enumerator = new SpanSplitEnumerator<byte>(span, 0);

        // Act & Assert - First segment [1]
        Assert.True(enumerator.MoveNext());
        Assert.True(enumerator.Current.SequenceEqual(new byte[] { 1 }));

        // Act & Assert - Second segment [2]
        Assert.True(enumerator.MoveNext());
        Assert.True(enumerator.Current.SequenceEqual(new byte[] { 2 }));

        // Act & Assert - Third segment [3]
        Assert.True(enumerator.MoveNext());
        Assert.True(enumerator.Current.SequenceEqual(new byte[] { 3 }));

        // Act & Assert - No more segments
        Assert.False(enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that MoveNext works correctly with int spans,
    /// splitting by an int separator including extreme values.
    /// </summary>
    [Fact]
    public void MoveNext_IntSpanWithExtremeValues_SplitsCorrectly()
    {
        // Arrange
        ReadOnlySpan<int> span = new int[] { int.MinValue, 0, int.MaxValue, 0, 42 };
        var enumerator = new SpanSplitEnumerator<int>(span, 0);

        // Act & Assert - First segment [int.MinValue]
        Assert.True(enumerator.MoveNext());
        Assert.True(enumerator.Current.SequenceEqual(new int[] { int.MinValue }));

        // Act & Assert - Second segment [int.MaxValue]
        Assert.True(enumerator.MoveNext());
        Assert.True(enumerator.Current.SequenceEqual(new int[] { int.MaxValue }));

        // Act & Assert - Third segment [42]
        Assert.True(enumerator.MoveNext());
        Assert.True(enumerator.Current.SequenceEqual(new int[] { 42 }));

        // Act & Assert - No more segments
        Assert.False(enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that MoveNext correctly handles a span containing only separators,
    /// yielding the appropriate number of empty segments.
    /// </summary>
    [Fact]
    public void MoveNext_OnlySeparators_YieldsEmptySegments()
    {
        // Arrange
        ReadOnlySpan<char> span = ",,,".AsSpan();
        var enumerator = new SpanSplitEnumerator<char>(span, ',');

        // Act & Assert - Four empty segments
        Assert.True(enumerator.MoveNext());
        Assert.True(enumerator.Current.IsEmpty);

        Assert.True(enumerator.MoveNext());
        Assert.True(enumerator.Current.IsEmpty);

        Assert.True(enumerator.MoveNext());
        Assert.True(enumerator.Current.IsEmpty);

        Assert.True(enumerator.MoveNext());
        Assert.True(enumerator.Current.IsEmpty);

        // Act & Assert - No more segments
        Assert.False(enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that MoveNext handles Unicode characters correctly,
    /// treating them as char elements in the span.
    /// </summary>
    [Fact]
    public void MoveNext_UnicodeCharacters_HandlesCorrectly()
    {
        // Arrange
        ReadOnlySpan<char> span = "α,β,γ".AsSpan();
        var enumerator = new SpanSplitEnumerator<char>(span, ',');

        // Act & Assert - First segment "α"
        Assert.True(enumerator.MoveNext());
        Assert.Equal("α", enumerator.Current.ToString());

        // Act & Assert - Second segment "β"
        Assert.True(enumerator.MoveNext());
        Assert.Equal("β", enumerator.Current.ToString());

        // Act & Assert - Third segment "γ"
        Assert.True(enumerator.MoveNext());
        Assert.Equal("γ", enumerator.Current.ToString());

        // Act & Assert - No more segments
        Assert.False(enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that MoveNext correctly processes a span with a single element
    /// that is not the separator.
    /// </summary>
    [Fact]
    public void MoveNext_SingleElementNotSeparator_ReturnsCorrectly()
    {
        // Arrange
        ReadOnlySpan<char> span = "x".AsSpan();
        var enumerator = new SpanSplitEnumerator<char>(span, ',');

        // Act & Assert - First call returns true with the element
        Assert.True(enumerator.MoveNext());
        Assert.Equal("x", enumerator.Current.ToString());

        // Act & Assert - Second call returns false
        Assert.False(enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that Current property maintains the correct value
    /// after MoveNext returns false.
    /// </summary>
    [Fact]
    public void MoveNext_CurrentAfterExhaustion_RetainsLastValue()
    {
        // Arrange
        ReadOnlySpan<char> span = "test".AsSpan();
        var enumerator = new SpanSplitEnumerator<char>(span, ',');

        // Act
        enumerator.MoveNext();
        var lastCurrent = enumerator.Current;
        enumerator.MoveNext(); // Returns false

        // Assert - Current should still reference the last valid segment
        Assert.Equal(lastCurrent.ToString(), enumerator.Current.ToString());
    }

    /// <summary>
    /// Tests that the constructor initializes successfully with an empty char span
    /// and sets Current to an empty span.
    /// </summary>
    [Fact]
    public void Constructor_EmptyCharSpan_InitializesSuccessfully()
    {
        // Arrange
        ReadOnlySpan<char> emptySpan = ReadOnlySpan<char>.Empty;
        char separator = ',';

        // Act
        var enumerator = new SpanSplitEnumerator<char>(emptySpan, separator);

        // Assert
        Assert.True(enumerator.Current.IsEmpty);
        Assert.Equal(0, enumerator.Current.Length);
    }

    /// <summary>
    /// Tests that the constructor initializes successfully with a non-empty char span
    /// and sets Current to an empty span.
    /// </summary>
    [Fact]
    public void Constructor_NonEmptyCharSpan_InitializesCurrentToEmpty()
    {
        // Arrange
        ReadOnlySpan<char> span = "a,b,c".AsSpan();
        char separator = ',';

        // Act
        var enumerator = new SpanSplitEnumerator<char>(span, separator);

        // Assert
        Assert.True(enumerator.Current.IsEmpty);
        Assert.Equal(0, enumerator.Current.Length);
    }

    /// <summary>
    /// Tests that the constructor accepts single element char span
    /// and initializes Current to empty.
    /// </summary>
    [Fact]
    public void Constructor_SingleCharSpan_InitializesSuccessfully()
    {
        // Arrange
        ReadOnlySpan<char> span = "x".AsSpan();
        char separator = ',';

        // Act
        var enumerator = new SpanSplitEnumerator<char>(span, separator);

        // Assert
        Assert.True(enumerator.Current.IsEmpty);
    }

    /// <summary>
    /// Tests constructor with various char separator values including default,
    /// whitespace, and special characters.
    /// </summary>
    /// <param name="separatorChar">The separator character to test.</param>
    [Theory]
    [InlineData('\0')]
    [InlineData(',')]
    [InlineData(' ')]
    [InlineData('\t')]
    [InlineData('\n')]
    [InlineData('\r')]
    [InlineData('|')]
    [InlineData('\u00A0')]
    [InlineData('\u2003')]
    public void Constructor_VariousCharSeparators_InitializesSuccessfully(char separatorChar)
    {
        // Arrange
        ReadOnlySpan<char> span = "test,data".AsSpan();

        // Act
        var enumerator = new SpanSplitEnumerator<char>(span, separatorChar);

        // Assert
        Assert.True(enumerator.Current.IsEmpty);
    }

    /// <summary>
    /// Tests that the constructor initializes successfully with an empty byte span
    /// and sets Current to an empty span.
    /// </summary>
    [Fact]
    public void Constructor_EmptyByteSpan_InitializesSuccessfully()
    {
        // Arrange
        ReadOnlySpan<byte> emptySpan = ReadOnlySpan<byte>.Empty;
        byte separator = 0;

        // Act
        var enumerator = new SpanSplitEnumerator<byte>(emptySpan, separator);

        // Assert
        Assert.True(enumerator.Current.IsEmpty);
        Assert.Equal(0, enumerator.Current.Length);
    }

    /// <summary>
    /// Tests that the constructor initializes successfully with a non-empty byte span
    /// and sets Current to an empty span.
    /// </summary>
    [Fact]
    public void Constructor_NonEmptyByteSpan_InitializesCurrentToEmpty()
    {
        // Arrange
        byte[] data = new byte[] { 1, 2, 3, 4, 5 };
        ReadOnlySpan<byte> span = data.AsSpan();
        byte separator = 3;

        // Act
        var enumerator = new SpanSplitEnumerator<byte>(span, separator);

        // Assert
        Assert.True(enumerator.Current.IsEmpty);
        Assert.Equal(0, enumerator.Current.Length);
    }

    /// <summary>
    /// Tests constructor with various byte separator boundary values.
    /// </summary>
    /// <param name="separator">The separator byte to test.</param>
    [Theory]
    [InlineData((byte)0)]
    [InlineData((byte)1)]
    [InlineData((byte)255)]
    [InlineData((byte)127)]
    public void Constructor_VariousByteSeparators_InitializesSuccessfully(byte separator)
    {
        // Arrange
        byte[] data = new byte[] { 10, 20, 30, 40 };
        ReadOnlySpan<byte> span = data.AsSpan();

        // Act
        var enumerator = new SpanSplitEnumerator<byte>(span, separator);

        // Assert
        Assert.True(enumerator.Current.IsEmpty);
    }

    /// <summary>
    /// Tests that the constructor initializes successfully with an empty int span
    /// and sets Current to an empty span.
    /// </summary>
    [Fact]
    public void Constructor_EmptyIntSpan_InitializesSuccessfully()
    {
        // Arrange
        ReadOnlySpan<int> emptySpan = ReadOnlySpan<int>.Empty;
        int separator = 0;

        // Act
        var enumerator = new SpanSplitEnumerator<int>(emptySpan, separator);

        // Assert
        Assert.True(enumerator.Current.IsEmpty);
        Assert.Equal(0, enumerator.Current.Length);
    }

    /// <summary>
    /// Tests that the constructor initializes successfully with a non-empty int span
    /// and sets Current to an empty span.
    /// </summary>
    [Fact]
    public void Constructor_NonEmptyIntSpan_InitializesCurrentToEmpty()
    {
        // Arrange
        int[] data = new int[] { 1, 2, 3, 4, 5 };
        ReadOnlySpan<int> span = data.AsSpan();
        int separator = 3;

        // Act
        var enumerator = new SpanSplitEnumerator<int>(span, separator);

        // Assert
        Assert.True(enumerator.Current.IsEmpty);
        Assert.Equal(0, enumerator.Current.Length);
    }

    /// <summary>
    /// Tests constructor with various int separator boundary values including
    /// minimum, maximum, zero, and negative values.
    /// </summary>
    /// <param name="separator">The separator int value to test.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    [InlineData(42)]
    [InlineData(-999)]
    public void Constructor_VariousIntSeparators_InitializesSuccessfully(int separator)
    {
        // Arrange
        int[] data = new int[] { 10, 20, 30, 40 };
        ReadOnlySpan<int> span = data.AsSpan();

        // Act
        var enumerator = new SpanSplitEnumerator<int>(span, separator);

        // Assert
        Assert.True(enumerator.Current.IsEmpty);
    }

    /// <summary>
    /// Tests that the constructor handles large char spans without issues
    /// and initializes Current to empty.
    /// </summary>
    [Fact]
    public void Constructor_LargeCharSpan_InitializesSuccessfully()
    {
        // Arrange
        string largeString = new string('x', 10000);
        ReadOnlySpan<char> span = largeString.AsSpan();
        char separator = ',';

        // Act
        var enumerator = new SpanSplitEnumerator<char>(span, separator);

        // Assert
        Assert.True(enumerator.Current.IsEmpty);
    }

    /// <summary>
    /// Tests that the constructor handles large byte spans without issues
    /// and initializes Current to empty.
    /// </summary>
    [Fact]
    public void Constructor_LargeByteSpan_InitializesSuccessfully()
    {
        // Arrange
        byte[] largeData = new byte[10000];
        Array.Fill(largeData, (byte)1);
        ReadOnlySpan<byte> span = largeData.AsSpan();
        byte separator = 0;

        // Act
        var enumerator = new SpanSplitEnumerator<byte>(span, separator);

        // Assert
        Assert.True(enumerator.Current.IsEmpty);
    }

    /// <summary>
    /// Tests that the constructor works when the span contains only the separator character.
    /// </summary>
    [Fact]
    public void Constructor_SpanContainsOnlySeparator_InitializesSuccessfully()
    {
        // Arrange
        ReadOnlySpan<char> span = ",".AsSpan();
        char separator = ',';

        // Act
        var enumerator = new SpanSplitEnumerator<char>(span, separator);

        // Assert
        Assert.True(enumerator.Current.IsEmpty);
    }

    /// <summary>
    /// Tests that the constructor works when the span contains multiple consecutive separators.
    /// </summary>
    [Fact]
    public void Constructor_SpanWithConsecutiveSeparators_InitializesSuccessfully()
    {
        // Arrange
        ReadOnlySpan<char> span = ",,,".AsSpan();
        char separator = ',';

        // Act
        var enumerator = new SpanSplitEnumerator<char>(span, separator);

        // Assert
        Assert.True(enumerator.Current.IsEmpty);
    }

    /// <summary>
    /// Tests that the constructor handles unicode char separators correctly.
    /// </summary>
    [Fact]
    public void Constructor_UnicodeSeparator_InitializesSuccessfully()
    {
        // Arrange
        ReadOnlySpan<char> span = "α•β•γ".AsSpan();
        char separator = '•';

        // Act
        var enumerator = new SpanSplitEnumerator<char>(span, separator);

        // Assert
        Assert.True(enumerator.Current.IsEmpty);
    }

    // -------- Single separator only --------

    [Fact]
    public void Char_SingleSeparatorOnly_YieldsTwoEmptySegments()
    {
        var segs = SplitChars(",", ',');
        EqualSeq(new[] { "", "" }, segs);
    }

    [Fact]
    public void Byte_SingleSeparatorOnly_YieldsTwoEmptySegments()
    {
        var segs = SplitBytes(new byte[] { 0 }, 0);
        EqualSeq(new[] { Array.Empty<byte>(), Array.Empty<byte>() }, segs);
    }

    // -------- All separators --------

    [Fact]
    public void Char_AllSeparators_YieldsOnlyEmptySegments()
    {
        var segs = SplitChars(",,,", ',');
        EqualSeq(new[] { "", "", "", "" }, segs);
    }

    // -------- Single element --------

    [Fact]
    public void Char_SingleChar_NoSeparator_YieldsSingleSegment()
    {
        var segs = SplitChars("x", ',');
        EqualSeq(new[] { "x" }, segs);
    }

    [Fact]
    public void Byte_SingleElement_NoSeparator_YieldsSingleSegment()
    {
        var segs = SplitBytes(new byte[] { 42 }, 0);
        EqualSeq(new[] { new byte[] { 42 } }, segs);
    }

    // -------- MoveNext after exhaustion --------

    [Fact]
    public void Char_MoveNext_AfterExhaustion_ReturnsFalse()
    {
        var e = new SpanSplitEnumerator<char>("a,b".AsSpan(), ',');
        while (e.MoveNext()) { }

        Assert.False(e.MoveNext());
        Assert.False(e.MoveNext());
    }

    [Fact]
    public void Byte_MoveNext_AfterExhaustion_ReturnsFalse()
    {
        var e = new SpanSplitEnumerator<byte>(new byte[] { 1, 0, 2 }, (byte)0);
        while (e.MoveNext()) { }

        Assert.False(e.MoveNext());
        Assert.False(e.MoveNext());
    }

    // -------- Large spans --------

    [Fact]
    public void Char_LargeSpan_SplitsCorrectly()
    {
        var words = string.Join(",", Enumerable.Range(0, 10_000).Select(i => i.ToString()));
        var segs = SplitChars(words, ',');

        Assert.Equal(10_000, segs.Count);
        Assert.Equal("0", segs[0]);
        Assert.Equal("9999", segs[^1]);
    }

    // -------- Separator equals element value --------

    [Fact]
    public void Int_SeparatorIsDefault_SplitsOnZero()
    {
        var data = new[] { 0, 0, 0 };
        var segs = SplitInts(data, 0);
        EqualSeq(new[]
        {
            Array.Empty<int>(),
            Array.Empty<int>(),
            Array.Empty<int>(),
            Array.Empty<int>(),
        }, segs);
    }
}