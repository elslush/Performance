using Performance.Enumerators;
using System;
using System.Collections.Generic;
using System.Text;

namespace Performance.Tests;

public sealed class WhitespaceSplitEnumeratorTests
{
    [Fact]
    public void Empty_And_WhitespaceOnly_Produce_No_Tokens()
    {
        Assert.Empty(Tokens(""));
        Assert.Empty(Tokens(" "));
        Assert.Empty(Tokens("\t\r\n\f"));
        Assert.Empty(Tokens("\u00A0\u2003\u202F\u3000")); // NBSP, EM SPACE, NNBSP, IDEOGRAPHIC SPACE
    }

    [Fact]
    public void Simple_Ascii_Cases()
    {
        EqualSeq(new[] { "one" }, Tokens("   one   "));
        EqualSeq(new[] { "one", "two" }, Tokens("one two"));
        EqualSeq(new[] { "one", "two", "three" }, Tokens("  one   two   three  "));
    }

    [Fact]
    public void Unicode_Whitespace_Simple()
    {
        var input = "\u00A0alpha\u2003beta\u202Fgamma\u3000delta"; // NBSP, EM SPACE, NNBSP, IDEOGRAPHIC SPACE
        EqualSeq(new[] { "alpha", "beta", "gamma", "delta" }, Tokens(input));
    }

    [Fact]
    public void Mixed_Ascii_Unicode_Leading_Trailing()
    {
        var input = "\t\u00A0  alpha\u2003beta  \u202F  ";
        EqualSeq(new[] { "alpha", "beta" }, Tokens(input));
    }

    [Fact]
    public void Consecutive_Whitespace_Delimits_Once()
    {
        var input = "one\u2003\u2003\u2003two"; // multiple EM SPACE
        EqualSeq(["one", "two"], Tokens(input));
    }

    [Fact]
    public void No_Whitespace_All_In_One_Token()
    {
        EqualSeq(["onetwo"], Tokens("onetwo"));
    }

    [Fact]
    public void Single_Token_With_Unicode_Whitespace_Around()
    {
        var input = "\u202F\u202Fomega\u00A0";
        EqualSeq(new[] { "omega" }, Tokens(input));
    }

    [Fact]
    public void Handles_Large_Input()
    {
        var words = Enumerable.Repeat("alpha", 10_000);
        var input = string.Join("\u2003", words); // EM SPACE as separator
        var result = Tokens(input);
        Assert.Equal(10_000, result.Count);
        // spot-check a few positions
        Assert.Equal("alpha", result[0]);
        Assert.Equal("alpha", result[1234]);
        Assert.Equal("alpha", result[^1]);
    }

    [Fact]
    public void Fuzz_Equals_Reference_Splitter_On_Random_Text()
    {
        var seed = 12345;
        var rng = new Random(seed);
        var asciiWs = new[] { ' ', '\t', '\r', '\n', '\f' };
        var uniWs = new[] { '\u00A0', '\u2003', '\u202F', '\u3000' };
        var words = new[] { "alpha", "beta", "gamma", "delta", "epsilon", "zeta", "eta", "theta", "iota", "kappa" };

        var sb = new StringBuilder(capacity: 16_384);
        for (int i = 0; i < 5000; i++)
        {
            if (rng.NextDouble() < 0.35)
            {
                int n = 1 + rng.Next(4);
                for (int j = 0; j < n; j++)
                    sb.Append(rng.NextDouble() < 0.75 ? asciiWs[rng.Next(asciiWs.Length)] : uniWs[rng.Next(uniWs.Length)]);
            }
            else
            {
                sb.Append(words[rng.Next(words.Length)]);
            }
        }
        var input = sb.ToString();

        var expected = ReferenceSplit(input);
        var actual = Tokens(input);

        EqualSeq(expected, actual);
    }

    // ----------------- Helpers -----------------

    private static List<string> Tokens(string s)
    {
        var list = new List<string>();
        foreach (var tok in new WhitespaceSplitEnumerator(s.AsSpan()))
            list.Add(tok.ToString());
        return list;
    }

    /// <summary>
    /// Reference splitter: Unicode-correct char-based implementation.
    /// Mirrors the intended semantics (split on char.IsWhiteSpace; consume runs).
    /// </summary>
    private static List<string> ReferenceSplit(string s)
    {
        var result = new List<string>();
        int i = 0;
        while (i < s.Length)
        {
            while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
            if (i >= s.Length) break;

            int start = i;
            while (i < s.Length && !char.IsWhiteSpace(s[i])) i++;

            result.Add(s.AsSpan(start, i - start).ToString());

            while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
        }
        return result;
    }

    private static void EqualSeq(IReadOnlyList<string> expected, IReadOnlyList<string> actual)
    {
        Assert.Equal(expected.Count, actual.Count);
        for (int i = 0; i < expected.Count; i++)
            Assert.Equal(expected[i], actual[i]);
    }
}
