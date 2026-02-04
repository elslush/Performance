using System;

using Performance.Benchmarks.Original;
using Xunit;

namespace Performance.Benchmarks.Original.UnitTests;


/// <summary>
/// Unit tests for the <see cref="OriginalTrimming"/> class.
/// </summary>
public class OriginalTrimmingTests
{
    /// <summary>
    /// Tests that Trim returns an empty span when the input span is empty.
    /// </summary>
    [Fact]
    public void Trim_EmptySpan_ReturnsEmptySpan()
    {
        // Arrange
        ReadOnlySpan<byte> span = ReadOnlySpan<byte>.Empty;

        // Act
        ReadOnlySpan<byte> result = span.Trim();

        // Assert
        Assert.True(result.IsEmpty);
    }

    /// <summary>
    /// Tests that Trim returns the original span when there is no whitespace.
    /// </summary>
    [Fact]
    public void Trim_NoWhitespace_ReturnsOriginalSpan()
    {
        // Arrange
        byte[] input = new byte[] { 0x41, 0x42, 0x43 }; // "ABC"
        ReadOnlySpan<byte> span = input;

        // Act
        ReadOnlySpan<byte> result = span.Trim();

        // Assert
        Assert.Equal(3, result.Length);
        Assert.True(span.SequenceEqual(result));
    }

    /// <summary>
    /// Tests that Trim removes leading whitespace correctly.
    /// </summary>
    /// <param name="leadingWhitespace">The leading whitespace bytes to test.</param>
    /// <param name="content">The non-whitespace content bytes.</param>
    [Theory]
    [InlineData(new byte[] { 0x20 }, new byte[] { 0x41, 0x42 })] // Space
    [InlineData(new byte[] { 0x09 }, new byte[] { 0x41, 0x42 })] // Tab
    [InlineData(new byte[] { 0x0D }, new byte[] { 0x41, 0x42 })] // CR
    [InlineData(new byte[] { 0x0A }, new byte[] { 0x41, 0x42 })] // LF
    [InlineData(new byte[] { 0x20, 0x09 }, new byte[] { 0x41 })] // Space + Tab
    [InlineData(new byte[] { 0x20, 0x09, 0x0D, 0x0A }, new byte[] { 0x41 })] // All whitespace types
    public void Trim_LeadingWhitespace_RemovesWhitespace(byte[] leadingWhitespace, byte[] content)
    {
        // Arrange
        byte[] input = new byte[leadingWhitespace.Length + content.Length];
        Array.Copy(leadingWhitespace, 0, input, 0, leadingWhitespace.Length);
        Array.Copy(content, 0, input, leadingWhitespace.Length, content.Length);
        ReadOnlySpan<byte> span = input;

        // Act
        ReadOnlySpan<byte> result = span.Trim();

        // Assert
        Assert.Equal(content.Length, result.Length);
        Assert.True(result.SequenceEqual(content));
    }

    /// <summary>
    /// Tests that Trim removes trailing whitespace correctly.
    /// </summary>
    /// <param name="content">The non-whitespace content bytes.</param>
    /// <param name="trailingWhitespace">The trailing whitespace bytes to test.</param>
    [Theory]
    [InlineData(new byte[] { 0x41, 0x42 }, new byte[] { 0x20 })] // Space
    [InlineData(new byte[] { 0x41, 0x42 }, new byte[] { 0x09 })] // Tab
    [InlineData(new byte[] { 0x41, 0x42 }, new byte[] { 0x0D })] // CR
    [InlineData(new byte[] { 0x41, 0x42 }, new byte[] { 0x0A })] // LF
    [InlineData(new byte[] { 0x41 }, new byte[] { 0x20, 0x09 })] // Space + Tab
    [InlineData(new byte[] { 0x41 }, new byte[] { 0x20, 0x09, 0x0D, 0x0A })] // All whitespace types
    public void Trim_TrailingWhitespace_RemovesWhitespace(byte[] content, byte[] trailingWhitespace)
    {
        // Arrange
        byte[] input = new byte[content.Length + trailingWhitespace.Length];
        Array.Copy(content, 0, input, 0, content.Length);
        Array.Copy(trailingWhitespace, 0, input, content.Length, trailingWhitespace.Length);
        ReadOnlySpan<byte> span = input;

        // Act
        ReadOnlySpan<byte> result = span.Trim();

        // Assert
        Assert.Equal(content.Length, result.Length);
        Assert.True(result.SequenceEqual(content));
    }

    /// <summary>
    /// Tests that Trim removes both leading and trailing whitespace correctly.
    /// </summary>
    /// <param name="leadingWhitespace">The leading whitespace bytes.</param>
    /// <param name="content">The non-whitespace content bytes.</param>
    /// <param name="trailingWhitespace">The trailing whitespace bytes.</param>
    [Theory]
    [InlineData(new byte[] { 0x20 }, new byte[] { 0x41 }, new byte[] { 0x20 })]
    [InlineData(new byte[] { 0x09 }, new byte[] { 0x41, 0x42 }, new byte[] { 0x0A })]
    [InlineData(new byte[] { 0x20, 0x09 }, new byte[] { 0x41 }, new byte[] { 0x0D, 0x0A })]
    [InlineData(new byte[] { 0x20, 0x09, 0x0D, 0x0A }, new byte[] { 0x41, 0x42, 0x43 }, new byte[] { 0x0A, 0x0D, 0x09, 0x20 })]
    public void Trim_LeadingAndTrailingWhitespace_RemovesBoth(byte[] leadingWhitespace, byte[] content, byte[] trailingWhitespace)
    {
        // Arrange
        byte[] input = new byte[leadingWhitespace.Length + content.Length + trailingWhitespace.Length];
        Array.Copy(leadingWhitespace, 0, input, 0, leadingWhitespace.Length);
        Array.Copy(content, 0, input, leadingWhitespace.Length, content.Length);
        Array.Copy(trailingWhitespace, 0, input, leadingWhitespace.Length + content.Length, trailingWhitespace.Length);
        ReadOnlySpan<byte> span = input;

        // Act
        ReadOnlySpan<byte> result = span.Trim();

        // Assert
        Assert.Equal(content.Length, result.Length);
        Assert.True(result.SequenceEqual(content));
    }

    /// <summary>
    /// Tests that Trim returns an empty span when the input contains only whitespace.
    /// </summary>
    /// <param name="whitespaceOnly">A span containing only whitespace bytes.</param>
    [Theory]
    [InlineData(new byte[] { 0x20 })]
    [InlineData(new byte[] { 0x09 })]
    [InlineData(new byte[] { 0x0D })]
    [InlineData(new byte[] { 0x0A })]
    [InlineData(new byte[] { 0x20, 0x20, 0x20 })]
    [InlineData(new byte[] { 0x20, 0x09, 0x0D, 0x0A })]
    [InlineData(new byte[] { 0x09, 0x20, 0x0A, 0x0D, 0x20 })]
    public void Trim_OnlyWhitespace_ReturnsEmptySpan(byte[] whitespaceOnly)
    {
        // Arrange
        ReadOnlySpan<byte> span = whitespaceOnly;

        // Act
        ReadOnlySpan<byte> result = span.Trim();

        // Assert
        Assert.Equal(0, result.Length);
    }

    /// <summary>
    /// Tests that Trim preserves whitespace in the middle of the content.
    /// </summary>
    [Fact]
    public void Trim_WhitespaceInMiddle_PreservesMiddleWhitespace()
    {
        // Arrange
        byte[] input = new byte[] { 0x41, 0x20, 0x42 }; // "A B"
        ReadOnlySpan<byte> span = input;

        // Act
        ReadOnlySpan<byte> result = span.Trim();

        // Assert
        Assert.Equal(3, result.Length);
        Assert.True(span.SequenceEqual(result));
    }

    /// <summary>
    /// Tests that Trim correctly handles whitespace in the middle with leading and trailing whitespace.
    /// </summary>
    [Fact]
    public void Trim_WhitespaceInMiddleWithLeadingAndTrailing_PreservesMiddleWhitespace()
    {
        // Arrange
        byte[] input = new byte[] { 0x20, 0x41, 0x20, 0x42, 0x09 }; // " A B\t"
        byte[] expected = new byte[] { 0x41, 0x20, 0x42 }; // "A B"
        ReadOnlySpan<byte> span = input;

        // Act
        ReadOnlySpan<byte> result = span.Trim();

        // Assert
        Assert.Equal(3, result.Length);
        Assert.True(result.SequenceEqual(expected));
    }

    /// <summary>
    /// Tests that Trim correctly handles a single non-whitespace byte.
    /// </summary>
    [Fact]
    public void Trim_SingleNonWhitespaceByte_ReturnsOriginalSpan()
    {
        // Arrange
        byte[] input = new byte[] { 0x41 }; // "A"
        ReadOnlySpan<byte> span = input;

        // Act
        ReadOnlySpan<byte> result = span.Trim();

        // Assert
        Assert.Equal(1, result.Length);
        Assert.Equal(0x41, result[0]);
    }

    /// <summary>
    /// Tests that Trim correctly handles a single whitespace byte.
    /// </summary>
    /// <param name="whitespaceByte">The single whitespace byte to test.</param>
    [Theory]
    [InlineData((byte)0x20)]
    [InlineData((byte)0x09)]
    [InlineData((byte)0x0D)]
    [InlineData((byte)0x0A)]
    public void Trim_SingleWhitespaceByte_ReturnsEmptySpan(byte whitespaceByte)
    {
        // Arrange
        byte[] input = new byte[] { whitespaceByte };
        ReadOnlySpan<byte> span = input;

        // Act
        ReadOnlySpan<byte> result = span.Trim();

        // Assert
        Assert.Equal(0, result.Length);
    }

    /// <summary>
    /// Tests that Trim correctly handles two non-whitespace bytes.
    /// </summary>
    [Fact]
    public void Trim_TwoNonWhitespaceBytes_ReturnsOriginalSpan()
    {
        // Arrange
        byte[] input = new byte[] { 0x41, 0x42 }; // "AB"
        ReadOnlySpan<byte> span = input;

        // Act
        ReadOnlySpan<byte> result = span.Trim();

        // Assert
        Assert.Equal(2, result.Length);
        Assert.True(span.SequenceEqual(result));
    }

    /// <summary>
    /// Tests that Trim correctly handles two whitespace bytes.
    /// </summary>
    [Fact]
    public void Trim_TwoWhitespaceBytes_ReturnsEmptySpan()
    {
        // Arrange
        byte[] input = new byte[] { 0x20, 0x09 };
        ReadOnlySpan<byte> span = input;

        // Act
        ReadOnlySpan<byte> result = span.Trim();

        // Assert
        Assert.Equal(0, result.Length);
    }

    /// <summary>
    /// Tests that Trim correctly handles multiple consecutive leading whitespace bytes.
    /// </summary>
    [Fact]
    public void Trim_MultipleConsecutiveLeadingWhitespace_RemovesAll()
    {
        // Arrange
        byte[] input = new byte[] { 0x20, 0x20, 0x20, 0x41, 0x42 }; // "   AB"
        byte[] expected = new byte[] { 0x41, 0x42 }; // "AB"
        ReadOnlySpan<byte> span = input;

        // Act
        ReadOnlySpan<byte> result = span.Trim();

        // Assert
        Assert.Equal(2, result.Length);
        Assert.True(result.SequenceEqual(expected));
    }

    /// <summary>
    /// Tests that Trim correctly handles multiple consecutive trailing whitespace bytes.
    /// </summary>
    [Fact]
    public void Trim_MultipleConsecutiveTrailingWhitespace_RemovesAll()
    {
        // Arrange
        byte[] input = new byte[] { 0x41, 0x42, 0x09, 0x09, 0x09 }; // "AB\t\t\t"
        byte[] expected = new byte[] { 0x41, 0x42 }; // "AB"
        ReadOnlySpan<byte> span = input;

        // Act
        ReadOnlySpan<byte> result = span.Trim();

        // Assert
        Assert.Equal(2, result.Length);
        Assert.True(result.SequenceEqual(expected));
    }

    /// <summary>
    /// Tests that Trim correctly handles bytes that are not considered whitespace.
    /// </summary>
    [Fact]
    public void Trim_NonWhitespaceBytes_ReturnsOriginalSpan()
    {
        // Arrange
        byte[] input = new byte[] { 0x00, 0x01, 0x1F, 0x21, 0xFF }; // Various non-whitespace bytes
        ReadOnlySpan<byte> span = input;

        // Act
        ReadOnlySpan<byte> result = span.Trim();

        // Assert
        Assert.Equal(input.Length, result.Length);
        Assert.True(span.SequenceEqual(result));
    }

    /// <summary>
    /// Tests that Trim correctly handles a mix of all whitespace types around content.
    /// </summary>
    [Fact]
    public void Trim_MixedWhitespaceTypes_RemovesAllWhitespace()
    {
        // Arrange
        byte[] input = new byte[] { 0x0A, 0x0D, 0x09, 0x20, 0x41, 0x42, 0x43, 0x20, 0x09, 0x0D, 0x0A }; // "\n\r\t ABC \t\r\n"
        byte[] expected = new byte[] { 0x41, 0x42, 0x43 }; // "ABC"
        ReadOnlySpan<byte> span = input;

        // Act
        ReadOnlySpan<byte> result = span.Trim();

        // Assert
        Assert.Equal(3, result.Length);
        Assert.True(result.SequenceEqual(expected));
    }

    /// <summary>
    /// Tests that TrimStart returns an empty span when given an empty input span.
    /// </summary>
    [Fact]
    public void TrimStart_EmptySpan_ReturnsEmptySpan()
    {
        // Arrange
        ReadOnlySpan<byte> emptySpan = ReadOnlySpan<byte>.Empty;

        // Act
        ReadOnlySpan<byte> result = emptySpan.TrimStart();

        // Assert
        Assert.True(result.IsEmpty);
    }

    /// <summary>
    /// Tests that TrimStart returns the original span when there is no leading whitespace.
    /// </summary>
    [Fact]
    public void TrimStart_NoLeadingWhitespace_ReturnsOriginalSpan()
    {
        // Arrange
        byte[] data = new byte[] { 0x41, 0x42, 0x43 }; // "ABC"
        ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(data);

        // Act
        ReadOnlySpan<byte> result = span.TrimStart();

        // Assert
        Assert.Equal(3, result.Length);
        Assert.True(span.SequenceEqual(result));
    }

    /// <summary>
    /// Tests that TrimStart removes leading space bytes (0x20).
    /// Input: spaces followed by non-whitespace bytes.
    /// Expected: Non-whitespace bytes without leading spaces.
    /// </summary>
    [Fact]
    public void TrimStart_LeadingSpaces_RemovesSpaces()
    {
        // Arrange
        byte[] data = new byte[] { 0x20, 0x20, 0x41, 0x42 }; // "  AB"
        ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(data);

        // Act
        ReadOnlySpan<byte> result = span.TrimStart();

        // Assert
        Assert.Equal(2, result.Length);
        Assert.Equal(0x41, result[0]);
        Assert.Equal(0x42, result[1]);
    }

    /// <summary>
    /// Tests that TrimStart removes leading tab bytes (0x09).
    /// Input: tabs followed by non-whitespace bytes.
    /// Expected: Non-whitespace bytes without leading tabs.
    /// </summary>
    [Fact]
    public void TrimStart_LeadingTabs_RemovesTabs()
    {
        // Arrange
        byte[] data = new byte[] { 0x09, 0x09, 0x41, 0x42 }; // "\t\tAB"
        ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(data);

        // Act
        ReadOnlySpan<byte> result = span.TrimStart();

        // Assert
        Assert.Equal(2, result.Length);
        Assert.Equal(0x41, result[0]);
        Assert.Equal(0x42, result[1]);
    }

    /// <summary>
    /// Tests that TrimStart removes leading carriage return bytes (0x0D).
    /// Input: CR followed by non-whitespace bytes.
    /// Expected: Non-whitespace bytes without leading CR.
    /// </summary>
    [Fact]
    public void TrimStart_LeadingCarriageReturn_RemovesCarriageReturn()
    {
        // Arrange
        byte[] data = new byte[] { 0x0D, 0x0D, 0x41 }; // "\r\rA"
        ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(data);

        // Act
        ReadOnlySpan<byte> result = span.TrimStart();

        // Assert
        Assert.Equal(1, result.Length);
        Assert.Equal(0x41, result[0]);
    }

    /// <summary>
    /// Tests that TrimStart removes leading line feed bytes (0x0A).
    /// Input: LF followed by non-whitespace bytes.
    /// Expected: Non-whitespace bytes without leading LF.
    /// </summary>
    [Fact]
    public void TrimStart_LeadingLineFeed_RemovesLineFeed()
    {
        // Arrange
        byte[] data = new byte[] { 0x0A, 0x0A, 0x41 }; // "\n\nA"
        ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(data);

        // Act
        ReadOnlySpan<byte> result = span.TrimStart();

        // Assert
        Assert.Equal(1, result.Length);
        Assert.Equal(0x41, result[0]);
    }

    /// <summary>
    /// Tests that TrimStart removes all types of leading whitespace bytes (space, tab, CR, LF).
    /// Input: mixed whitespace followed by non-whitespace bytes.
    /// Expected: Non-whitespace bytes without any leading whitespace.
    /// </summary>
    [Fact]
    public void TrimStart_MixedLeadingWhitespace_RemovesAllWhitespace()
    {
        // Arrange
        byte[] data = new byte[] { 0x20, 0x09, 0x0D, 0x0A, 0x20, 0x41, 0x42 }; // " \t\r\n AB"
        ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(data);

        // Act
        ReadOnlySpan<byte> result = span.TrimStart();

        // Assert
        Assert.Equal(2, result.Length);
        Assert.Equal(0x41, result[0]);
        Assert.Equal(0x42, result[1]);
    }

    /// <summary>
    /// Tests that TrimStart returns an empty span when the input contains only whitespace bytes.
    /// Input: only spaces, tabs, CR, and LF.
    /// Expected: Empty span.
    /// </summary>
    [Fact]
    public void TrimStart_OnlyWhitespace_ReturnsEmptySpan()
    {
        // Arrange
        byte[] data = new byte[] { 0x20, 0x09, 0x0D, 0x0A, 0x20, 0x09 }; // " \t\r\n \t"
        ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(data);

        // Act
        ReadOnlySpan<byte> result = span.TrimStart();

        // Assert
        Assert.True(result.IsEmpty);
    }

    /// <summary>
    /// Tests that TrimStart handles a single non-whitespace byte correctly.
    /// Input: single non-whitespace byte.
    /// Expected: Original span with single byte.
    /// </summary>
    [Fact]
    public void TrimStart_SingleNonWhitespaceByte_ReturnsOriginalSpan()
    {
        // Arrange
        byte[] data = new byte[] { 0x41 }; // "A"
        ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(data);

        // Act
        ReadOnlySpan<byte> result = span.TrimStart();

        // Assert
        Assert.Equal(1, result.Length);
        Assert.Equal(0x41, result[0]);
    }

    /// <summary>
    /// Tests that TrimStart returns an empty span when given a single whitespace byte.
    /// Input: single space byte.
    /// Expected: Empty span.
    /// </summary>
    [Fact]
    public void TrimStart_SingleWhitespaceByte_ReturnsEmptySpan()
    {
        // Arrange
        byte[] data = new byte[] { 0x20 }; // " "
        ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(data);

        // Act
        ReadOnlySpan<byte> result = span.TrimStart();

        // Assert
        Assert.True(result.IsEmpty);
    }

    /// <summary>
    /// Tests that TrimStart does not remove trailing whitespace.
    /// Input: non-whitespace followed by trailing whitespace.
    /// Expected: Original span including trailing whitespace.
    /// </summary>
    [Fact]
    public void TrimStart_TrailingWhitespaceOnly_ReturnsOriginalSpan()
    {
        // Arrange
        byte[] data = new byte[] { 0x41, 0x42, 0x20, 0x09 }; // "AB \t"
        ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(data);

        // Act
        ReadOnlySpan<byte> result = span.TrimStart();

        // Assert
        Assert.Equal(4, result.Length);
        Assert.True(span.SequenceEqual(result));
    }

    /// <summary>
    /// Tests that TrimStart does not affect whitespace in the middle of the span.
    /// Input: non-whitespace, then whitespace, then non-whitespace.
    /// Expected: Original span unchanged.
    /// </summary>
    [Fact]
    public void TrimStart_WhitespaceInMiddle_ReturnsOriginalSpan()
    {
        // Arrange
        byte[] data = new byte[] { 0x41, 0x20, 0x42 }; // "A B"
        ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(data);

        // Act
        ReadOnlySpan<byte> result = span.TrimStart();

        // Assert
        Assert.Equal(3, result.Length);
        Assert.True(span.SequenceEqual(result));
    }

    /// <summary>
    /// Tests that TrimStart correctly handles leading whitespace followed by non-whitespace and trailing whitespace.
    /// Input: leading whitespace, content, and trailing whitespace.
    /// Expected: Content and trailing whitespace without leading whitespace.
    /// </summary>
    [Fact]
    public void TrimStart_LeadingAndTrailingWhitespace_RemovesOnlyLeading()
    {
        // Arrange
        byte[] data = new byte[] { 0x20, 0x09, 0x41, 0x42, 0x20, 0x09 }; // " \tAB \t"
        ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(data);

        // Act
        ReadOnlySpan<byte> result = span.TrimStart();

        // Assert
        Assert.Equal(4, result.Length);
        Assert.Equal(0x41, result[0]);
        Assert.Equal(0x42, result[1]);
        Assert.Equal(0x20, result[2]);
        Assert.Equal(0x09, result[3]);
    }

    /// <summary>
    /// Tests that TrimStart handles non-standard bytes that are not whitespace correctly.
    /// Input: bytes outside the trim set.
    /// Expected: Original span unchanged.
    /// </summary>
    [Fact]
    public void TrimStart_NonWhitespaceBytes_ReturnsOriginalSpan()
    {
        // Arrange
        byte[] data = new byte[] { 0x00, 0x01, 0xFF, 0x7F }; // Various non-whitespace bytes
        ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(data);

        // Act
        ReadOnlySpan<byte> result = span.TrimStart();

        // Assert
        Assert.Equal(4, result.Length);
        Assert.True(span.SequenceEqual(result));
    }

    /// <summary>
    /// Tests that Trim returns the same empty memory when the input memory is empty.
    /// </summary>
    [Fact]
    public void Trim_EmptyMemory_ReturnsSameEmptyMemory()
    {
        // Arrange
        Memory<byte> emptyMemory = Memory<byte>.Empty;

        // Act
        Memory<byte> result = emptyMemory.Trim();

        // Assert
        Assert.True(result.IsEmpty);
    }

    /// <summary>
    /// Tests that Trim returns the same default memory when the input is a default Memory struct.
    /// </summary>
    [Fact]
    public void Trim_DefaultMemory_ReturnsSameDefaultMemory()
    {
        // Arrange
        Memory<byte> defaultMemory = default;

        // Act
        Memory<byte> result = defaultMemory.Trim();

        // Assert
        Assert.True(result.IsEmpty);
    }

    /// <summary>
    /// Tests that Trim removes leading and trailing whitespace bytes (space, tab, CR, LF).
    /// </summary>
    /// <param name="input">The input byte array containing whitespace.</param>
    /// <param name="expected">The expected trimmed byte array.</param>
    [Theory]
    [InlineData(new byte[] { 0x20, 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x20 }, new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F })] // " Hello " -> "Hello"
    [InlineData(new byte[] { 0x09, 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x09 }, new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F })] // "\tHello\t" -> "Hello"
    [InlineData(new byte[] { 0x0D, 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x0D }, new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F })] // "\rHello\r" -> "Hello"
    [InlineData(new byte[] { 0x0A, 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x0A }, new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F })] // "\nHello\n" -> "Hello"
    [InlineData(new byte[] { 0x20, 0x09, 0x0D, 0x0A, 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x20, 0x09, 0x0D, 0x0A }, new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F })] // Multiple whitespace -> "Hello"
    [InlineData(new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }, new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F })] // "Hello" -> "Hello" (no whitespace)
    [InlineData(new byte[] { 0x48, 0x65, 0x20, 0x6C, 0x6C, 0x6F }, new byte[] { 0x48, 0x65, 0x20, 0x6C, 0x6C, 0x6F })] // "He llo" -> "He llo" (whitespace in middle)
    public void Trim_MemoryWithWhitespace_TrimsCorrectly(byte[] input, byte[] expected)
    {
        // Arrange
        Memory<byte> memory = new Memory<byte>(input);

        // Act
        Memory<byte> result = memory.Trim();

        // Assert
        Assert.Equal(expected, result.ToArray());
    }

    /// <summary>
    /// Tests that Trim removes only leading whitespace bytes.
    /// </summary>
    [Fact]
    public void Trim_LeadingWhitespaceOnly_RemovesLeadingWhitespace()
    {
        // Arrange
        byte[] input = new byte[] { 0x20, 0x09, 0x48, 0x65, 0x6C, 0x6C, 0x6F }; // " \tHello"
        Memory<byte> memory = new Memory<byte>(input);

        // Act
        Memory<byte> result = memory.Trim();

        // Assert
        byte[] expected = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }; // "Hello"
        Assert.Equal(expected, result.ToArray());
    }

    /// <summary>
    /// Tests that Trim removes only trailing whitespace bytes.
    /// </summary>
    [Fact]
    public void Trim_TrailingWhitespaceOnly_RemovesTrailingWhitespace()
    {
        // Arrange
        byte[] input = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x20, 0x09 }; // "Hello \t"
        Memory<byte> memory = new Memory<byte>(input);

        // Act
        Memory<byte> result = memory.Trim();

        // Assert
        byte[] expected = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }; // "Hello"
        Assert.Equal(expected, result.ToArray());
    }

    /// <summary>
    /// Tests that Trim returns empty memory when input contains only whitespace bytes.
    /// </summary>
    [Fact]
    public void Trim_OnlyWhitespace_ReturnsEmptyMemory()
    {
        // Arrange
        byte[] input = new byte[] { 0x20, 0x09, 0x0D, 0x0A, 0x20 }; // All whitespace
        Memory<byte> memory = new Memory<byte>(input);

        // Act
        Memory<byte> result = memory.Trim();

        // Assert
        Assert.True(result.IsEmpty);
    }

    /// <summary>
    /// Tests that Trim handles a single non-whitespace byte correctly.
    /// </summary>
    [Fact]
    public void Trim_SingleNonWhitespaceByte_ReturnsSameByte()
    {
        // Arrange
        byte[] input = new byte[] { 0x48 }; // "H"
        Memory<byte> memory = new Memory<byte>(input);

        // Act
        Memory<byte> result = memory.Trim();

        // Assert
        Assert.Equal(input, result.ToArray());
    }

    /// <summary>
    /// Tests that Trim handles a single whitespace byte correctly.
    /// </summary>
    [Fact]
    public void Trim_SingleWhitespaceByte_ReturnsEmptyMemory()
    {
        // Arrange
        byte[] input = new byte[] { 0x20 }; // " "
        Memory<byte> memory = new Memory<byte>(input);

        // Act
        Memory<byte> result = memory.Trim();

        // Assert
        Assert.True(result.IsEmpty);
    }

    /// <summary>
    /// Tests that Trim correctly handles memory with only space characters.
    /// </summary>
    [Fact]
    public void Trim_OnlySpaces_ReturnsEmptyMemory()
    {
        // Arrange
        byte[] input = new byte[] { 0x20, 0x20, 0x20, 0x20 }; // "    "
        Memory<byte> memory = new Memory<byte>(input);

        // Act
        Memory<byte> result = memory.Trim();

        // Assert
        Assert.True(result.IsEmpty);
    }

    /// <summary>
    /// Tests that Trim correctly handles memory with only tab characters.
    /// </summary>
    [Fact]
    public void Trim_OnlyTabs_ReturnsEmptyMemory()
    {
        // Arrange
        byte[] input = new byte[] { 0x09, 0x09, 0x09 }; // "\t\t\t"
        Memory<byte> memory = new Memory<byte>(input);

        // Act
        Memory<byte> result = memory.Trim();

        // Assert
        Assert.True(result.IsEmpty);
    }

    /// <summary>
    /// Tests that Trim correctly handles memory with only carriage return characters.
    /// </summary>
    [Fact]
    public void Trim_OnlyCarriageReturns_ReturnsEmptyMemory()
    {
        // Arrange
        byte[] input = new byte[] { 0x0D, 0x0D }; // "\r\r"
        Memory<byte> memory = new Memory<byte>(input);

        // Act
        Memory<byte> result = memory.Trim();

        // Assert
        Assert.True(result.IsEmpty);
    }

    /// <summary>
    /// Tests that Trim correctly handles memory with only line feed characters.
    /// </summary>
    [Fact]
    public void Trim_OnlyLineFeeds_ReturnsEmptyMemory()
    {
        // Arrange
        byte[] input = new byte[] { 0x0A, 0x0A, 0x0A }; // "\n\n\n"
        Memory<byte> memory = new Memory<byte>(input);

        // Act
        Memory<byte> result = memory.Trim();

        // Assert
        Assert.True(result.IsEmpty);
    }

    /// <summary>
    /// Tests that Trim does not remove non-whitespace characters from the middle of the memory.
    /// </summary>
    [Fact]
    public void Trim_WhitespaceInMiddle_DoesNotRemoveMiddleWhitespace()
    {
        // Arrange
        byte[] input = new byte[] { 0x48, 0x65, 0x20, 0x20, 0x6C, 0x6F }; // "He  lo"
        Memory<byte> memory = new Memory<byte>(input);

        // Act
        Memory<byte> result = memory.Trim();

        // Assert
        Assert.Equal(input, result.ToArray());
    }

    /// <summary>
    /// Tests that Trim handles a large memory buffer with whitespace correctly.
    /// </summary>
    [Fact]
    public void Trim_LargeMemoryWithWhitespace_TrimsCorrectly()
    {
        // Arrange
        byte[] content = new byte[1000];
        for (int i = 0; i < 1000; i++)
        {
            content[i] = 0x41; // 'A'
        }
        byte[] input = new byte[1010];
        input[0] = 0x20;
        input[1] = 0x09;
        input[2] = 0x0D;
        input[3] = 0x0A;
        input[4] = 0x20;
        Array.Copy(content, 0, input, 5, 1000);
        input[1005] = 0x20;
        input[1006] = 0x09;
        input[1007] = 0x0D;
        input[1008] = 0x0A;
        input[1009] = 0x20;

        Memory<byte> memory = new Memory<byte>(input);

        // Act
        Memory<byte> result = memory.Trim();

        // Assert
        Assert.Equal(1000, result.Length);
        Assert.Equal(content, result.ToArray());
    }

    /// <summary>
    /// Tests that Trim handles bytes that are not whitespace but close to whitespace values.
    /// </summary>
    [Fact]
    public void Trim_NonWhitespaceBytes_DoesNotTrim()
    {
        // Arrange
        byte[] input = new byte[] { 0x21, 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x21 }; // "!Hello!" (0x21 is not whitespace)
        Memory<byte> memory = new Memory<byte>(input);

        // Act
        Memory<byte> result = memory.Trim();

        // Assert
        Assert.Equal(input, result.ToArray());
    }
}