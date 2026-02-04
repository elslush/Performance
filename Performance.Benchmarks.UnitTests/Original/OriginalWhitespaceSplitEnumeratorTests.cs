using System;

using Performance.Benchmarks.Whitespace;
using Xunit;

namespace Performance.Benchmarks.Whitespace.UnitTests;


/// <summary>
/// Unit tests for <see cref="OriginalWhitespaceSplitEnumerator"/>.
/// </summary>
public partial class OriginalWhitespaceSplitEnumeratorTests
{
    /// <summary>
    /// Tests that GetEnumerator returns a copy of the enumerator with the same initial state
    /// when called on a freshly constructed enumerator with an empty span.
    /// Expected: Returns enumerator with default state.
    /// </summary>
    [Fact]
    public void GetEnumerator_EmptySpan_ReturnsCopyWithSameState()
    {
        // Arrange
        ReadOnlySpan<char> emptySpan = ReadOnlySpan<char>.Empty;
        var enumerator = new OriginalWhitespaceSplitEnumerator(emptySpan);

        // Act
        var result = enumerator.GetEnumerator();

        // Assert
        Assert.Equal(default(ReadOnlySpan<char>), result.Current);
    }

    /// <summary>
    /// Tests that GetEnumerator returns a copy of the enumerator with the same initial state
    /// when called on a freshly constructed enumerator with a non-empty span.
    /// Expected: Returns enumerator with Current as default and can enumerate.
    /// </summary>
    [Theory]
    [InlineData("hello")]
    [InlineData("hello world")]
    [InlineData("a b c")]
    [InlineData("  spaces  ")]
    [InlineData("single")]
    [InlineData("   ")]
    [InlineData(" leading")]
    [InlineData("trailing ")]
    [InlineData("multiple   spaces   between")]
    public void GetEnumerator_FreshEnumerator_ReturnsCopyWithInitialState(string input)
    {
        // Arrange
        ReadOnlySpan<char> span = input.AsSpan();
        var enumerator = new OriginalWhitespaceSplitEnumerator(span);

        // Act
        var result = enumerator.GetEnumerator();

        // Assert
        Assert.Equal(default(ReadOnlySpan<char>), result.Current);
    }

    /// <summary>
    /// Tests that GetEnumerator returns a copy with the same state after MoveNext has been called.
    /// Expected: The returned enumerator has the same Current value as the original.
    /// </summary>
    [Theory]
    [InlineData("hello world", "hello")]
    [InlineData("first second third", "first")]
    [InlineData("  leading", "leading")]
    [InlineData("a", "a")]
    public void GetEnumerator_AfterMoveNext_ReturnsCopyWithSameState(string input, string expectedFirst)
    {
        // Arrange
        ReadOnlySpan<char> span = input.AsSpan();
        var enumerator = new OriginalWhitespaceSplitEnumerator(span);
        enumerator.MoveNext();

        // Act
        var result = enumerator.GetEnumerator();

        // Assert
        Assert.True(expectedFirst.AsSpan().SequenceEqual(result.Current));
    }

    /// <summary>
    /// Tests that the returned enumerator is an independent copy.
    /// Modifying the returned copy should not affect the original enumerator.
    /// Expected: Original enumerator state remains unchanged.
    /// </summary>
    [Fact]
    public void GetEnumerator_ModifyingReturnedCopy_DoesNotAffectOriginal()
    {
        // Arrange
        ReadOnlySpan<char> span = "first second third".AsSpan();
        var original = new OriginalWhitespaceSplitEnumerator(span);
        original.MoveNext();
        var originalCurrent = original.Current;

        // Act
        var copy = original.GetEnumerator();
        copy.MoveNext(); // Modify the copy

        // Assert - original should be unchanged
        Assert.True(originalCurrent.SequenceEqual(original.Current));
    }

    /// <summary>
    /// Tests that GetEnumerator can be called multiple times and each returns an independent copy.
    /// Expected: Each call returns a new copy that can be enumerated independently.
    /// </summary>
    [Fact]
    public void GetEnumerator_MultipleCalls_ReturnsIndependentCopies()
    {
        // Arrange
        ReadOnlySpan<char> span = "one two three".AsSpan();
        var enumerator = new OriginalWhitespaceSplitEnumerator(span);

        // Act
        var copy1 = enumerator.GetEnumerator();
        var copy2 = enumerator.GetEnumerator();

        copy1.MoveNext();
        copy2.MoveNext();
        copy2.MoveNext();

        // Assert - copies have different states
        Assert.True("one".AsSpan().SequenceEqual(copy1.Current));
        Assert.True("two".AsSpan().SequenceEqual(copy2.Current));
    }

    /// <summary>
    /// Tests that GetEnumerator works correctly when called on an enumerator that has reached the end.
    /// Expected: Returns a copy with the last state.
    /// </summary>
    [Fact]
    public void GetEnumerator_AfterEnumerationComplete_ReturnsCopyWithFinalState()
    {
        // Arrange
        ReadOnlySpan<char> span = "only".AsSpan();
        var enumerator = new OriginalWhitespaceSplitEnumerator(span);
        enumerator.MoveNext(); // Move to "only"
        var moved = enumerator.MoveNext(); // Try to move past end

        // Act
        var result = enumerator.GetEnumerator();

        // Assert
        Assert.False(moved);
        Assert.True("only".AsSpan().SequenceEqual(result.Current));
    }

    /// <summary>
    /// Tests GetEnumerator with a span containing only whitespace characters.
    /// Expected: Returns a copy with default Current state.
    /// </summary>
    [Theory]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    [InlineData(" \t\n\r")]
    public void GetEnumerator_WhitespaceOnlySpan_ReturnsCopyWithDefaultState(string input)
    {
        // Arrange
        ReadOnlySpan<char> span = input.AsSpan();
        var enumerator = new OriginalWhitespaceSplitEnumerator(span);

        // Act
        var result = enumerator.GetEnumerator();

        // Assert
        Assert.Equal(default(ReadOnlySpan<char>), result.Current);
    }

    /// <summary>
    /// Tests GetEnumerator with a very large span to ensure it handles boundary cases.
    /// Expected: Returns a copy that can enumerate the large span.
    /// </summary>
    [Fact]
    public void GetEnumerator_LargeSpan_ReturnsCopyWithSameState()
    {
        // Arrange
        string largeString = new string('a', 10000) + " " + new string('b', 10000);
        ReadOnlySpan<char> span = largeString.AsSpan();
        var enumerator = new OriginalWhitespaceSplitEnumerator(span);

        // Act
        var result = enumerator.GetEnumerator();

        // Assert
        Assert.Equal(default(ReadOnlySpan<char>), result.Current);

        // Verify it can enumerate
        var canMove = result.MoveNext();
        Assert.True(canMove);
    }

    /// <summary>
    /// Tests that the returned enumerator can be used in a foreach-like pattern.
    /// Expected: The enumerator works correctly for iteration.
    /// </summary>
    [Fact]
    public void GetEnumerator_CanBeUsedForIteration_EnumeratesCorrectly()
    {
        // Arrange
        ReadOnlySpan<char> span = "alpha beta gamma".AsSpan();
        var enumerator = new OriginalWhitespaceSplitEnumerator(span);
        var result = enumerator.GetEnumerator();

        // Act & Assert
        Assert.True(result.MoveNext());
        Assert.True("alpha".AsSpan().SequenceEqual(result.Current));

        Assert.True(result.MoveNext());
        Assert.True("beta".AsSpan().SequenceEqual(result.Current));

        Assert.True(result.MoveNext());
        Assert.True("gamma".AsSpan().SequenceEqual(result.Current));

        Assert.False(result.MoveNext());
    }

    /// <summary>
    /// Tests GetEnumerator with special characters in the span.
    /// Expected: Returns a copy that correctly handles special characters.
    /// </summary>
    [Theory]
    [InlineData("hello\tworld")]
    [InlineData("line1\nline2")]
    [InlineData("word1\r\nword2")]
    [InlineData("a\u00A0b")] // Non-breaking space
    public void GetEnumerator_SpecialWhitespaceCharacters_ReturnsCopyThatHandlesCorrectly(string input)
    {
        // Arrange
        ReadOnlySpan<char> span = input.AsSpan();
        var enumerator = new OriginalWhitespaceSplitEnumerator(span);

        // Act
        var result = enumerator.GetEnumerator();

        // Assert
        Assert.Equal(default(ReadOnlySpan<char>), result.Current);

        // Verify it can move
        var canMove = result.MoveNext();
        Assert.True(canMove);
    }

    /// <summary>
    /// Tests GetEnumerator with a single character span.
    /// Expected: Returns a copy that can enumerate the single character.
    /// </summary>
    [Theory]
    [InlineData("a")]
    [InlineData("Z")]
    [InlineData("0")]
    [InlineData("!")]
    public void GetEnumerator_SingleCharacter_ReturnsCopyWithCorrectState(string input)
    {
        // Arrange
        ReadOnlySpan<char> span = input.AsSpan();
        var enumerator = new OriginalWhitespaceSplitEnumerator(span);

        // Act
        var result = enumerator.GetEnumerator();

        // Assert
        Assert.Equal(default(ReadOnlySpan<char>), result.Current);
        Assert.True(result.MoveNext());
        Assert.True(input.AsSpan().SequenceEqual(result.Current));
    }

    /// <summary>
    /// Tests GetEnumerator after partial enumeration.
    /// Expected: Returns a copy reflecting the current mid-enumeration state.
    /// </summary>
    [Fact]
    public void GetEnumerator_PartialEnumeration_ReturnsCopyWithCurrentState()
    {
        // Arrange
        ReadOnlySpan<char> span = "one two three four".AsSpan();
        var enumerator = new OriginalWhitespaceSplitEnumerator(span);
        enumerator.MoveNext(); // "one"
        enumerator.MoveNext(); // "two"

        // Act
        var result = enumerator.GetEnumerator();

        // Assert
        Assert.True("two".AsSpan().SequenceEqual(result.Current));

        // Verify the copy can continue from where it was
        Assert.True(result.MoveNext());
        Assert.True("three".AsSpan().SequenceEqual(result.Current));
    }

    /// <summary>
    /// Tests that the constructor initializes the Current property to an empty span (default)
    /// when provided with various span inputs.
    /// </summary>
    /// <param name="input">The input string to create a span from.</param>
    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("hello world")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t\n\r")]
    [InlineData("test\tstring\nwith\rwhitespace")]
    [InlineData("!@#$%^&*()")]
    [InlineData("日本語")]
    public void Constructor_WithVariousSpans_InitializesCurrentToDefault(string input)
    {
        // Arrange
        ReadOnlySpan<char> span = input.AsSpan();

        // Act
        OriginalWhitespaceSplitEnumerator enumerator = new OriginalWhitespaceSplitEnumerator(span);

        // Assert
        Assert.True(enumerator.Current.IsEmpty);
        Assert.Equal(0, enumerator.Current.Length);
    }

    /// <summary>
    /// Tests that the constructor successfully initializes with an empty span
    /// and sets Current to an empty span.
    /// </summary>
    [Fact]
    public void Constructor_WithEmptySpan_InitializesCurrentToDefault()
    {
        // Arrange
        ReadOnlySpan<char> emptySpan = ReadOnlySpan<char>.Empty;

        // Act
        OriginalWhitespaceSplitEnumerator enumerator = new OriginalWhitespaceSplitEnumerator(emptySpan);

        // Assert
        Assert.True(enumerator.Current.IsEmpty);
        Assert.Equal(0, enumerator.Current.Length);
    }

    /// <summary>
    /// Tests that the constructor successfully initializes with a large span
    /// and sets Current to an empty span.
    /// </summary>
    [Fact]
    public void Constructor_WithLargeSpan_InitializesCurrentToDefault()
    {
        // Arrange
        string largeString = new string('x', 100000);
        ReadOnlySpan<char> largeSpan = largeString.AsSpan();

        // Act
        OriginalWhitespaceSplitEnumerator enumerator = new OriginalWhitespaceSplitEnumerator(largeSpan);

        // Assert
        Assert.True(enumerator.Current.IsEmpty);
        Assert.Equal(0, enumerator.Current.Length);
    }

    /// <summary>
    /// Tests that the constructor handles spans with control characters
    /// and initializes Current to an empty span.
    /// </summary>
    [Theory]
    [InlineData("\0")]
    [InlineData("\u0001\u0002\u0003")]
    [InlineData("text\0with\0nulls")]
    public void Constructor_WithControlCharacters_InitializesCurrentToDefault(string input)
    {
        // Arrange
        ReadOnlySpan<char> span = input.AsSpan();

        // Act
        OriginalWhitespaceSplitEnumerator enumerator = new OriginalWhitespaceSplitEnumerator(span);

        // Assert
        Assert.True(enumerator.Current.IsEmpty);
        Assert.Equal(0, enumerator.Current.Length);
    }

    /// <summary>
    /// Tests that the constructor handles spans with Unicode characters
    /// and initializes Current to an empty span.
    /// </summary>
    [Theory]
    [InlineData("😀")]
    [InlineData("🎉🎊🎈")]
    [InlineData("Hello 世界")]
    [InlineData("Привет мир")]
    [InlineData("مرحبا بالعالم")]
    public void Constructor_WithUnicodeCharacters_InitializesCurrentToDefault(string input)
    {
        // Arrange
        ReadOnlySpan<char> span = input.AsSpan();

        // Act
        OriginalWhitespaceSplitEnumerator enumerator = new OriginalWhitespaceSplitEnumerator(span);

        // Assert
        Assert.True(enumerator.Current.IsEmpty);
        Assert.Equal(0, enumerator.Current.Length);
    }

    /// <summary>
    /// Tests that MoveNext returns false when the span is empty.
    /// </summary>
    [Fact]
    public void MoveNext_EmptySpan_ReturnsFalse()
    {
        // Arrange
        ReadOnlySpan<char> span = ReadOnlySpan<char>.Empty;
        var enumerator = new OriginalWhitespaceSplitEnumerator(span);

        // Act
        bool result = enumerator.MoveNext();

        // Assert
        Assert.False(result);
        Assert.True(enumerator.Current.IsEmpty);
    }

    /// <summary>
    /// Tests that MoveNext returns false when the span contains only whitespace characters.
    /// </summary>
    /// <param name="input">The input string containing only whitespace.</param>
    [Theory]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("\r")]
    [InlineData("\r\n")]
    [InlineData(" \t\n\r")]
    [InlineData("     \t\t\t     ")]
    public void MoveNext_OnlyWhitespace_ReturnsFalse(string input)
    {
        // Arrange
        ReadOnlySpan<char> span = input.AsSpan();
        var enumerator = new OriginalWhitespaceSplitEnumerator(span);

        // Act
        bool result = enumerator.MoveNext();

        // Assert
        Assert.False(result);
        Assert.True(enumerator.Current.IsEmpty);
    }

    /// <summary>
    /// Tests that MoveNext returns true and sets Current correctly for a single non-whitespace character.
    /// </summary>
    /// <param name="input">The input string containing a single character.</param>
    [Theory]
    [InlineData("a")]
    [InlineData("Z")]
    [InlineData("0")]
    [InlineData("!")]
    [InlineData("@")]
    public void MoveNext_SingleNonWhitespaceChar_ReturnsTrueAndSetsCurrent(string input)
    {
        // Arrange
        ReadOnlySpan<char> span = input.AsSpan();
        var enumerator = new OriginalWhitespaceSplitEnumerator(span);

        // Act
        bool firstResult = enumerator.MoveNext();
        string currentToken = enumerator.Current.ToString();
        bool secondResult = enumerator.MoveNext();

        // Assert
        Assert.True(firstResult);
        Assert.Equal(input, currentToken);
        Assert.False(secondResult);
    }

    /// <summary>
    /// Tests that MoveNext correctly handles a single word with no whitespace.
    /// </summary>
    /// <param name="input">The input string containing a single word.</param>
    [Theory]
    [InlineData("hello")]
    [InlineData("WORLD")]
    [InlineData("test123")]
    [InlineData("a")]
    [InlineData("VeryLongWordWithNoSpaces")]
    public void MoveNext_SingleWord_ReturnsTrueOnceAndSetsCurrent(string input)
    {
        // Arrange
        ReadOnlySpan<char> span = input.AsSpan();
        var enumerator = new OriginalWhitespaceSplitEnumerator(span);

        // Act
        bool firstResult = enumerator.MoveNext();
        string currentToken = enumerator.Current.ToString();
        bool secondResult = enumerator.MoveNext();

        // Assert
        Assert.True(firstResult);
        Assert.Equal(input, currentToken);
        Assert.False(secondResult);
    }

    /// <summary>
    /// Tests that MoveNext correctly enumerates multiple words separated by single spaces.
    /// </summary>
    [Fact]
    public void MoveNext_MultipleWordsWithSingleSpaces_EnumeratesAllWords()
    {
        // Arrange
        ReadOnlySpan<char> span = "hello world test".AsSpan();
        var enumerator = new OriginalWhitespaceSplitEnumerator(span);

        // Act & Assert
        Assert.True(enumerator.MoveNext());
        Assert.Equal("hello", enumerator.Current.ToString());

        Assert.True(enumerator.MoveNext());
        Assert.Equal("world", enumerator.Current.ToString());

        Assert.True(enumerator.MoveNext());
        Assert.Equal("test", enumerator.Current.ToString());

        Assert.False(enumerator.MoveNext());
    }

    /// <summary>
    /// Tests that MoveNext correctly skips leading whitespace before the first token.
    /// </summary>
    /// <param name="input">The input string with leading whitespace.</param>
    /// <param name="expectedToken">The expected first token.</param>
    [Theory]
    [InlineData(" hello", "hello")]
    [InlineData("  hello", "hello")]
    [InlineData("\thello", "hello")]
    [InlineData("\n\rhello", "hello")]
    [InlineData("     word", "word")]
    public void MoveNext_LeadingWhitespace_SkipsAndFindsFirstToken(string input, string expectedToken)
    {
        // Arrange
        ReadOnlySpan<char> span = input.AsSpan();
        var enumerator = new OriginalWhitespaceSplitEnumerator(span);

        // Act
        bool result = enumerator.MoveNext();
        string currentToken = enumerator.Current.ToString();

        // Assert
        Assert.True(result);
        Assert.Equal(expectedToken, currentToken);
    }

    /// <summary>
    /// Tests that MoveNext correctly handles trailing whitespace after the last token.
    /// </summary>
    /// <param name="input">The input string with trailing whitespace.</param>
    /// <param name="expectedToken">The expected token.</param>
    [Theory]
    [InlineData("hello ", "hello")]
    [InlineData("hello  ", "hello")]
    [InlineData("hello\t", "hello")]
    [InlineData("hello\n", "hello")]
    [InlineData("word     ", "word")]
    public void MoveNext_TrailingWhitespace_FindsTokenAndReturnsFalseAfter(string input, string expectedToken)
    {
        // Arrange
        ReadOnlySpan<char> span = input.AsSpan();
        var enumerator = new OriginalWhitespaceSplitEnumerator(span);

        // Act
        bool firstResult = enumerator.MoveNext();
        string currentToken = enumerator.Current.ToString();
        bool secondResult = enumerator.MoveNext();

        // Assert
        Assert.True(firstResult);
        Assert.Equal(expectedToken, currentToken);
        Assert.False(secondResult);
    }

    /// <summary>
    /// Tests that MoveNext correctly handles multiple consecutive whitespace characters between tokens.
    /// </summary>
    [Fact]
    public void MoveNext_MultipleConsecutiveWhitespace_TreatsAsSingleSeparator()
    {
        // Arrange
        ReadOnlySpan<char> span = "hello     world".AsSpan();
        var enumerator = new OriginalWhitespaceSplitEnumerator(span);

        // Act & Assert
        Assert.True(enumerator.MoveNext());
        Assert.Equal("hello", enumerator.Current.ToString());

        Assert.True(enumerator.MoveNext());
        Assert.Equal("world", enumerator.Current.ToString());

        Assert.False(enumerator.MoveNext());
    }

    /// <summary>
    /// Tests that MoveNext correctly handles mixed types of whitespace characters.
    /// </summary>
    [Fact]
    public void MoveNext_MixedWhitespaceTypes_EnumeratesTokensCorrectly()
    {
        // Arrange
        ReadOnlySpan<char> span = "one\ttwo\nthree\rfour \t\n five".AsSpan();
        var enumerator = new OriginalWhitespaceSplitEnumerator(span);

        // Act & Assert
        Assert.True(enumerator.MoveNext());
        Assert.Equal("one", enumerator.Current.ToString());

        Assert.True(enumerator.MoveNext());
        Assert.Equal("two", enumerator.Current.ToString());

        Assert.True(enumerator.MoveNext());
        Assert.Equal("three", enumerator.Current.ToString());

        Assert.True(enumerator.MoveNext());
        Assert.Equal("four", enumerator.Current.ToString());

        Assert.True(enumerator.MoveNext());
        Assert.Equal("five", enumerator.Current.ToString());

        Assert.False(enumerator.MoveNext());
    }

    /// <summary>
    /// Tests that MoveNext handles leading, trailing, and multiple consecutive whitespace together.
    /// </summary>
    [Fact]
    public void MoveNext_LeadingTrailingAndMultipleWhitespace_EnumeratesCorrectly()
    {
        // Arrange
        ReadOnlySpan<char> span = "   first    second     third   ".AsSpan();
        var enumerator = new OriginalWhitespaceSplitEnumerator(span);

        // Act & Assert
        Assert.True(enumerator.MoveNext());
        Assert.Equal("first", enumerator.Current.ToString());

        Assert.True(enumerator.MoveNext());
        Assert.Equal("second", enumerator.Current.ToString());

        Assert.True(enumerator.MoveNext());
        Assert.Equal("third", enumerator.Current.ToString());

        Assert.False(enumerator.MoveNext());
    }

    /// <summary>
    /// Tests that MoveNext continues to return false after enumeration is complete.
    /// </summary>
    [Fact]
    public void MoveNext_AfterEnumerationComplete_ContinuesReturningFalse()
    {
        // Arrange
        ReadOnlySpan<char> span = "word".AsSpan();
        var enumerator = new OriginalWhitespaceSplitEnumerator(span);

        // Act
        enumerator.MoveNext(); // First call returns true
        enumerator.MoveNext(); // Second call returns false
        bool thirdResult = enumerator.MoveNext();
        bool fourthResult = enumerator.MoveNext();

        // Assert
        Assert.False(thirdResult);
        Assert.False(fourthResult);
    }

    /// <summary>
    /// Tests that MoveNext correctly handles a string with many tokens.
    /// </summary>
    [Fact]
    public void MoveNext_ManyTokens_EnumeratesAll()
    {
        // Arrange
        ReadOnlySpan<char> span = "a b c d e f g h i j".AsSpan();
        var enumerator = new OriginalWhitespaceSplitEnumerator(span);
        string[] expectedTokens = { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j" };

        // Act & Assert
        foreach (string expected in expectedTokens)
        {
            Assert.True(enumerator.MoveNext());
            Assert.Equal(expected, enumerator.Current.ToString());
        }

        Assert.False(enumerator.MoveNext());
    }

    /// <summary>
    /// Tests that MoveNext correctly handles tokens with special characters.
    /// </summary>
    [Fact]
    public void MoveNext_TokensWithSpecialCharacters_EnumeratesCorrectly()
    {
        // Arrange
        ReadOnlySpan<char> span = "hello! @world #test $money".AsSpan();
        var enumerator = new OriginalWhitespaceSplitEnumerator(span);

        // Act & Assert
        Assert.True(enumerator.MoveNext());
        Assert.Equal("hello!", enumerator.Current.ToString());

        Assert.True(enumerator.MoveNext());
        Assert.Equal("@world", enumerator.Current.ToString());

        Assert.True(enumerator.MoveNext());
        Assert.Equal("#test", enumerator.Current.ToString());

        Assert.True(enumerator.MoveNext());
        Assert.Equal("$money", enumerator.Current.ToString());

        Assert.False(enumerator.MoveNext());
    }

    /// <summary>
    /// Tests that MoveNext correctly handles Unicode whitespace characters.
    /// </summary>
    [Fact]
    public void MoveNext_UnicodeWhitespace_EnumeratesCorrectly()
    {
        // Arrange - Using various Unicode whitespace characters
        ReadOnlySpan<char> span = "hello\u00A0world\u2000test".AsSpan(); // \u00A0 = non-breaking space, \u2000 = en quad
        var enumerator = new OriginalWhitespaceSplitEnumerator(span);

        // Act & Assert
        Assert.True(enumerator.MoveNext());
        Assert.Equal("hello", enumerator.Current.ToString());

        Assert.True(enumerator.MoveNext());
        Assert.Equal("world", enumerator.Current.ToString());

        Assert.True(enumerator.MoveNext());
        Assert.Equal("test", enumerator.Current.ToString());

        Assert.False(enumerator.MoveNext());
    }

    /// <summary>
    /// Tests that Current remains at the last token after enumeration completes.
    /// </summary>
    [Fact]
    public void MoveNext_CurrentPropertyAfterEnumeration_RemainsAtLastToken()
    {
        // Arrange
        ReadOnlySpan<char> span = "first second".AsSpan();
        var enumerator = new OriginalWhitespaceSplitEnumerator(span);

        // Act
        enumerator.MoveNext(); // "first"
        enumerator.MoveNext(); // "second"
        string lastToken = enumerator.Current.ToString();
        enumerator.MoveNext(); // false
        string currentAfterFalse = enumerator.Current.ToString();

        // Assert
        Assert.Equal("second", lastToken);
        Assert.Equal("second", currentAfterFalse);
    }

    /// <summary>
    /// Tests that MoveNext handles a very long token correctly.
    /// </summary>
    [Fact]
    public void MoveNext_VeryLongToken_EnumeratesCorrectly()
    {
        // Arrange
        string longToken = new string('a', 10000);
        ReadOnlySpan<char> span = longToken.AsSpan();
        var enumerator = new OriginalWhitespaceSplitEnumerator(span);

        // Act
        bool result = enumerator.MoveNext();
        string currentToken = enumerator.Current.ToString();

        // Assert
        Assert.True(result);
        Assert.Equal(longToken, currentToken);
        Assert.False(enumerator.MoveNext());
    }

    /// <summary>
    /// Tests that MoveNext correctly handles alternating single characters and whitespace.
    /// </summary>
    [Fact]
    public void MoveNext_AlternatingSingleCharsAndWhitespace_EnumeratesCorrectly()
    {
        // Arrange
        ReadOnlySpan<char> span = "a b c d e".AsSpan();
        var enumerator = new OriginalWhitespaceSplitEnumerator(span);

        // Act & Assert
        Assert.True(enumerator.MoveNext());
        Assert.Equal("a", enumerator.Current.ToString());

        Assert.True(enumerator.MoveNext());
        Assert.Equal("b", enumerator.Current.ToString());

        Assert.True(enumerator.MoveNext());
        Assert.Equal("c", enumerator.Current.ToString());

        Assert.True(enumerator.MoveNext());
        Assert.Equal("d", enumerator.Current.ToString());

        Assert.True(enumerator.MoveNext());
        Assert.Equal("e", enumerator.Current.ToString());

        Assert.False(enumerator.MoveNext());
    }
}