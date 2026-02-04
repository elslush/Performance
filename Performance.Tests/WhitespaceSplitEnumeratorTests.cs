using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

using Performance.Enumerators;

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

    /// <summary>
    /// Verifies that the constructor initializes Current to an empty span (default)
    /// when provided with an empty input span.
    /// </summary>
    [Fact]
    public void Constructor_WithEmptySpan_InitializesCurrentToEmpty()
    {
        // Arrange
        ReadOnlySpan<char> emptySpan = ReadOnlySpan<char>.Empty;

        // Act
        var enumerator = new WhitespaceSplitEnumerator(emptySpan);

        // Assert
        Assert.True(enumerator.Current.IsEmpty);
        Assert.Equal(0, enumerator.Current.Length);
    }

    /// <summary>
    /// Verifies that the constructor initializes Current to an empty span (default)
    /// when provided with a non-empty input span, before MoveNext is called.
    /// </summary>
    [Fact]
    public void Constructor_WithNonEmptySpan_InitializesCurrentToEmpty()
    {
        // Arrange
        ReadOnlySpan<char> span = "hello world".AsSpan();

        // Act
        var enumerator = new WhitespaceSplitEnumerator(span);

        // Assert
        Assert.True(enumerator.Current.IsEmpty);
        Assert.Equal(0, enumerator.Current.Length);
    }

    /// <summary>
    /// Verifies that the constructor initializes Current to an empty span (default)
    /// when provided with a whitespace-only input span.
    /// </summary>
    [Fact]
    public void Constructor_WithWhitespaceOnlySpan_InitializesCurrentToEmpty()
    {
        // Arrange
        ReadOnlySpan<char> whitespaceSpan = "   \t\r\n\f   ".AsSpan();

        // Act
        var enumerator = new WhitespaceSplitEnumerator(whitespaceSpan);

        // Assert
        Assert.True(enumerator.Current.IsEmpty);
        Assert.Equal(0, enumerator.Current.Length);
    }

    /// <summary>
    /// Verifies that the constructor initializes Current to an empty span (default)
    /// when provided with a span containing Unicode whitespace characters.
    /// </summary>
    [Fact]
    public void Constructor_WithUnicodeWhitespace_InitializesCurrentToEmpty()
    {
        // Arrange
        ReadOnlySpan<char> unicodeSpan = "\u00A0\u2003\u202F\u3000".AsSpan(); // NBSP, EM SPACE, NNBSP, IDEOGRAPHIC SPACE

        // Act
        var enumerator = new WhitespaceSplitEnumerator(unicodeSpan);

        // Assert
        Assert.True(enumerator.Current.IsEmpty);
        Assert.Equal(0, enumerator.Current.Length);
    }

    /// <summary>
    /// Verifies that the constructor initializes Current to an empty span (default)
    /// when provided with a single character span.
    /// </summary>
    [Fact]
    public void Constructor_WithSingleCharacter_InitializesCurrentToEmpty()
    {
        // Arrange
        ReadOnlySpan<char> singleChar = "x".AsSpan();

        // Act
        var enumerator = new WhitespaceSplitEnumerator(singleChar);

        // Assert
        Assert.True(enumerator.Current.IsEmpty);
        Assert.Equal(0, enumerator.Current.Length);
    }

    /// <summary>
    /// Verifies that the constructor initializes Current to an empty span (default)
    /// when provided with a large input span.
    /// </summary>
    [Fact]
    public void Constructor_WithLargeSpan_InitializesCurrentToEmpty()
    {
        // Arrange
        string largeString = new string('a', 10000);
        ReadOnlySpan<char> largeSpan = largeString.AsSpan();

        // Act
        var enumerator = new WhitespaceSplitEnumerator(largeSpan);

        // Assert
        Assert.True(enumerator.Current.IsEmpty);
        Assert.Equal(0, enumerator.Current.Length);
    }

    /// <summary>
    /// Verifies that the constructor initializes Current to an empty span (default)
    /// when provided with a span containing special and control characters.
    /// </summary>
    [Fact]
    public void Constructor_WithSpecialCharacters_InitializesCurrentToEmpty()
    {
        // Arrange
        ReadOnlySpan<char> specialChars = "test\0\u0001\u001F\uFFFF".AsSpan();

        // Act
        var enumerator = new WhitespaceSplitEnumerator(specialChars);

        // Assert
        Assert.True(enumerator.Current.IsEmpty);
        Assert.Equal(0, enumerator.Current.Length);
    }

    /// <summary>
    /// Verifies that the constructor properly initializes the enumerator
    /// and allows GetEnumerator to be called, returning itself.
    /// </summary>
    [Fact]
    public void Constructor_GetEnumerator_ReturnsSelf()
    {
        // Arrange
        ReadOnlySpan<char> span = "test".AsSpan();
        var enumerator = new WhitespaceSplitEnumerator(span);

        // Act
        var result = enumerator.GetEnumerator();

        // Assert - verifying that GetEnumerator works after construction
        Assert.True(result.Current.IsEmpty);
    }

    /// <summary>
    /// Verifies that the constructor properly initializes the enumerator
    /// to a state where MoveNext can be called without throwing exceptions.
    /// </summary>
    [Fact]
    public void Constructor_AllowsMoveNextCall_WithoutException()
    {
        // Arrange
        ReadOnlySpan<char> span = "token".AsSpan();

        // Act
        var enumerator = new WhitespaceSplitEnumerator(span);
        var canMove = enumerator.MoveNext();

        // Assert - verifying that MoveNext works after construction
        Assert.True(canMove);
        Assert.False(enumerator.Current.IsEmpty);
    }

    /// <summary>
    /// Verifies that the constructor properly initializes the enumerator
    /// with a span containing mixed ASCII and Unicode content.
    /// </summary>
    [Fact]
    public void Constructor_WithMixedAsciiUnicode_InitializesCurrentToEmpty()
    {
        // Arrange
        ReadOnlySpan<char> mixedSpan = "hello\u00A0world\u2003test".AsSpan();

        // Act
        var enumerator = new WhitespaceSplitEnumerator(mixedSpan);

        // Assert
        Assert.True(enumerator.Current.IsEmpty);
        Assert.Equal(0, enumerator.Current.Length);
    }

    /// <summary>
    /// Tests that GetEnumerator returns a usable enumerator when called on a fresh instance.
    /// Verifies the enumerator can be explicitly obtained and iterated to produce expected tokens.
    /// </summary>
    [Fact]
    public void GetEnumerator_OnFreshEnumerator_ReturnsUsableEnumerator()
    {
        // Arrange
        var span = "one two three".AsSpan();
        var enumerator = new WhitespaceSplitEnumerator(span);

        // Act
        var result = enumerator.GetEnumerator();
        var tokens = new List<string>();
        while (result.MoveNext())
        {
            tokens.Add(result.Current.ToString());
        }

        // Assert
        Assert.Equal(3, tokens.Count);
        Assert.Equal("one", tokens[0]);
        Assert.Equal("two", tokens[1]);
        Assert.Equal("three", tokens[2]);
    }

    /// <summary>
    /// Tests that GetEnumerator returns a working enumerator for an empty span.
    /// Verifies that no tokens are produced when the input is empty.
    /// </summary>
    [Fact]
    public void GetEnumerator_OnEmptySpan_ReturnsEnumeratorWithNoTokens()
    {
        // Arrange
        var span = ReadOnlySpan<char>.Empty;
        var enumerator = new WhitespaceSplitEnumerator(span);

        // Act
        var result = enumerator.GetEnumerator();
        var tokens = new List<string>();
        while (result.MoveNext())
        {
            tokens.Add(result.Current.ToString());
        }

        // Assert
        Assert.Empty(tokens);
    }

    /// <summary>
    /// Tests that GetEnumerator returns a working enumerator for whitespace-only input.
    /// Verifies that no tokens are produced when the input contains only whitespace characters.
    /// </summary>
    [Fact]
    public void GetEnumerator_OnWhitespaceOnlySpan_ReturnsEnumeratorWithNoTokens()
    {
        // Arrange
        var span = "   \t\r\n  ".AsSpan();
        var enumerator = new WhitespaceSplitEnumerator(span);

        // Act
        var result = enumerator.GetEnumerator();
        var tokens = new List<string>();
        while (result.MoveNext())
        {
            tokens.Add(result.Current.ToString());
        }

        // Assert
        Assert.Empty(tokens);
    }

    /// <summary>
    /// Tests that multiple calls to GetEnumerator return independent enumerator copies.
    /// Since WhitespaceSplitEnumerator is a value type, each call should return an independent copy
    /// that can be iterated separately without affecting other copies.
    /// </summary>
    [Fact]
    public void GetEnumerator_MultipleCalls_ReturnsIndependentCopies()
    {
        // Arrange
        var span = "alpha beta gamma".AsSpan();
        var enumerator = new WhitespaceSplitEnumerator(span);

        // Act
        var enumerator1 = enumerator.GetEnumerator();
        var enumerator2 = enumerator.GetEnumerator();

        var tokens1 = new List<string>();
        while (enumerator1.MoveNext())
        {
            tokens1.Add(enumerator1.Current.ToString());
        }

        var tokens2 = new List<string>();
        while (enumerator2.MoveNext())
        {
            tokens2.Add(enumerator2.Current.ToString());
        }

        // Assert
        Assert.Equal(3, tokens1.Count);
        Assert.Equal(3, tokens2.Count);
        Assert.Equal("alpha", tokens1[0]);
        Assert.Equal("alpha", tokens2[0]);
        Assert.Equal("beta", tokens1[1]);
        Assert.Equal("beta", tokens2[1]);
        Assert.Equal("gamma", tokens1[2]);
        Assert.Equal("gamma", tokens2[2]);
    }

    /// <summary>
    /// Tests that GetEnumerator works correctly with Unicode whitespace characters.
    /// Verifies the enumerator properly handles non-ASCII whitespace like NBSP, EM SPACE, etc.
    /// </summary>
    [Fact]
    public void GetEnumerator_WithUnicodeWhitespace_ReturnsCorrectTokens()
    {
        // Arrange
        var span = "\u00A0foo\u2003bar\u202Fbaz\u3000".AsSpan(); // NBSP, EM SPACE, NNBSP, IDEOGRAPHIC SPACE
        var enumerator = new WhitespaceSplitEnumerator(span);

        // Act
        var result = enumerator.GetEnumerator();
        var tokens = new List<string>();
        while (result.MoveNext())
        {
            tokens.Add(result.Current.ToString());
        }

        // Assert
        Assert.Equal(3, tokens.Count);
        Assert.Equal("foo", tokens[0]);
        Assert.Equal("bar", tokens[1]);
        Assert.Equal("baz", tokens[2]);
    }

    /// <summary>
    /// Tests that GetEnumerator works with a span containing a single token with no whitespace.
    /// Verifies that the enumerator returns the entire span as one token.
    /// </summary>
    [Fact]
    public void GetEnumerator_WithSingleTokenNoWhitespace_ReturnsSingleToken()
    {
        // Arrange
        var span = "singletoken".AsSpan();
        var enumerator = new WhitespaceSplitEnumerator(span);

        // Act
        var result = enumerator.GetEnumerator();
        var tokens = new List<string>();
        while (result.MoveNext())
        {
            tokens.Add(result.Current.ToString());
        }

        // Assert
        Assert.Single(tokens);
        Assert.Equal("singletoken", tokens[0]);
    }

    /// <summary>
    /// Tests that GetEnumerator works with consecutive whitespace characters.
    /// Verifies that consecutive whitespace is treated as a single delimiter and no empty tokens are produced.
    /// </summary>
    [Fact]
    public void GetEnumerator_WithConsecutiveWhitespace_ProducesCorrectTokens()
    {
        // Arrange
        var span = "one     two\t\t\tthree".AsSpan();
        var enumerator = new WhitespaceSplitEnumerator(span);

        // Act
        var result = enumerator.GetEnumerator();
        var tokens = new List<string>();
        while (result.MoveNext())
        {
            tokens.Add(result.Current.ToString());
        }

        // Assert
        Assert.Equal(3, tokens.Count);
        Assert.Equal("one", tokens[0]);
        Assert.Equal("two", tokens[1]);
        Assert.Equal("three", tokens[2]);
    }

    /// <summary>
    /// Tests that GetEnumerator works with leading and trailing whitespace.
    /// Verifies that leading and trailing whitespace is properly skipped and does not produce empty tokens.
    /// </summary>
    [Fact]
    public void GetEnumerator_WithLeadingAndTrailingWhitespace_SkipsWhitespace()
    {
        // Arrange
        var span = "   \t\r\ntoken\n\r\t   ".AsSpan();
        var enumerator = new WhitespaceSplitEnumerator(span);

        // Act
        var result = enumerator.GetEnumerator();
        var tokens = new List<string>();
        while (result.MoveNext())
        {
            tokens.Add(result.Current.ToString());
        }

        // Assert
        Assert.Single(tokens);
        Assert.Equal("token", tokens[0]);
    }
}

