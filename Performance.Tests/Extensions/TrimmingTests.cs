using System;

using Performance.Extensions;
using Xunit;

namespace Performance.Extensions.UnitTests;


/// <summary>
/// Unit tests for the <see cref="Trimming"/> class.
/// </summary>
public partial class TrimmingTests
{
    /// <summary>
    /// Verifies that Trim returns the original Memory when it is empty.
    /// </summary>
    [Fact]
    public void Trim_EmptyMemory_ReturnsOriginal()
    {
        // Arrange
        Memory<byte> memory = Memory<byte>.Empty;

        // Act
        Memory<byte> result = memory.Trim();

        // Assert
        Assert.True(result.IsEmpty);
        Assert.Equal(0, result.Length);
    }

    /// <summary>
    /// Verifies that Trim returns a zero-length slice when the memory contains only whitespace bytes.
    /// Tests various combinations of ASCII whitespace (space, tab, CR, LF).
    /// </summary>
    /// <param name="bytes">The whitespace-only byte array.</param>
    [Theory]
    [InlineData(new byte[] { 0x20 })]                           // Single space
    [InlineData(new byte[] { 0x09 })]                           // Single tab
    [InlineData(new byte[] { 0x0D })]                           // Single CR
    [InlineData(new byte[] { 0x0A })]                           // Single LF
    [InlineData(new byte[] { 0x20, 0x20, 0x20 })]              // Multiple spaces
    [InlineData(new byte[] { 0x09, 0x09 })]                     // Multiple tabs
    [InlineData(new byte[] { 0x0D, 0x0A })]                     // CR+LF
    [InlineData(new byte[] { 0x20, 0x09, 0x0D, 0x0A })]        // All whitespace types
    [InlineData(new byte[] { 0x20, 0x09, 0x20, 0x0A, 0x0D, 0x09 })] // Mixed whitespace
    public void Trim_AllWhitespace_ReturnsZeroLengthSlice(byte[] bytes)
    {
        // Arrange
        Memory<byte> memory = new Memory<byte>(bytes);

        // Act
        Memory<byte> result = memory.Trim();

        // Assert
        Assert.Equal(0, result.Length);
    }

    /// <summary>
    /// Verifies that Trim returns the original content when there is no whitespace to trim.
    /// </summary>
    /// <param name="bytes">The byte array with no whitespace.</param>
    /// <param name="expected">The expected result bytes.</param>
    [Theory]
    [InlineData(new byte[] { 0x41 }, new byte[] { 0x41 })]                     // Single 'A'
    [InlineData(new byte[] { 0x48, 0x69 }, new byte[] { 0x48, 0x69 })]        // "Hi"
    [InlineData(new byte[] { 0x41, 0x42, 0x43 }, new byte[] { 0x41, 0x42, 0x43 })] // "ABC"
    public void Trim_NoWhitespace_ReturnsOriginal(byte[] bytes, byte[] expected)
    {
        // Arrange
        Memory<byte> memory = new Memory<byte>(bytes);

        // Act
        Memory<byte> result = memory.Trim();

        // Assert
        Assert.Equal(expected.Length, result.Length);
        Assert.True(result.Span.SequenceEqual(expected));
    }

    /// <summary>
    /// Verifies that Trim removes leading whitespace correctly.
    /// </summary>
    /// <param name="bytes">The input byte array with leading whitespace.</param>
    /// <param name="expected">The expected result bytes after trimming.</param>
    [Theory]
    [InlineData(new byte[] { 0x20, 0x41 }, new byte[] { 0x41 })]                      // " A"
    [InlineData(new byte[] { 0x09, 0x48, 0x69 }, new byte[] { 0x48, 0x69 })]         // "\tHi"
    [InlineData(new byte[] { 0x0D, 0x0A, 0x41 }, new byte[] { 0x41 })]               // "\r\nA"
    [InlineData(new byte[] { 0x20, 0x20, 0x09, 0x41, 0x42 }, new byte[] { 0x41, 0x42 })] // "  \tAB"
    public void Trim_LeadingWhitespace_RemovesLeading(byte[] bytes, byte[] expected)
    {
        // Arrange
        Memory<byte> memory = new Memory<byte>(bytes);

        // Act
        Memory<byte> result = memory.Trim();

        // Assert
        Assert.Equal(expected.Length, result.Length);
        Assert.True(result.Span.SequenceEqual(expected));
    }

    /// <summary>
    /// Verifies that Trim removes trailing whitespace correctly.
    /// </summary>
    /// <param name="bytes">The input byte array with trailing whitespace.</param>
    /// <param name="expected">The expected result bytes after trimming.</param>
    [Theory]
    [InlineData(new byte[] { 0x41, 0x20 }, new byte[] { 0x41 })]                      // "A "
    [InlineData(new byte[] { 0x48, 0x69, 0x09 }, new byte[] { 0x48, 0x69 })]         // "Hi\t"
    [InlineData(new byte[] { 0x41, 0x0D, 0x0A }, new byte[] { 0x41 })]               // "A\r\n"
    [InlineData(new byte[] { 0x41, 0x42, 0x20, 0x20, 0x09 }, new byte[] { 0x41, 0x42 })] // "AB  \t"
    public void Trim_TrailingWhitespace_RemovesTrailing(byte[] bytes, byte[] expected)
    {
        // Arrange
        Memory<byte> memory = new Memory<byte>(bytes);

        // Act
        Memory<byte> result = memory.Trim();

        // Assert
        Assert.Equal(expected.Length, result.Length);
        Assert.True(result.Span.SequenceEqual(expected));
    }

    /// <summary>
    /// Verifies that Trim removes both leading and trailing whitespace while preserving internal content.
    /// </summary>
    /// <param name="bytes">The input byte array with leading and trailing whitespace.</param>
    /// <param name="expected">The expected result bytes after trimming.</param>
    [Theory]
    [InlineData(new byte[] { 0x20, 0x41, 0x20 }, new byte[] { 0x41 })]                // " A "
    [InlineData(new byte[] { 0x09, 0x48, 0x69, 0x09 }, new byte[] { 0x48, 0x69 })]   // "\tHi\t"
    [InlineData(new byte[] { 0x20, 0x0D, 0x41, 0x0A, 0x20 }, new byte[] { 0x41 })]   // " \rA\n " - CR and LF are also trimmed as whitespace
    [InlineData(new byte[] { 0x20, 0x20, 0x41, 0x20, 0x42, 0x20, 0x20 }, new byte[] { 0x41, 0x20, 0x42 })] // "  A B  " - internal space preserved
    [InlineData(new byte[] { 0x0D, 0x0A, 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x20, 0x57, 0x6F, 0x72, 0x6C, 0x64, 0x0D, 0x0A },
                new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x20, 0x57, 0x6F, 0x72, 0x6C, 0x64 })] // "\r\nHello World\r\n"
    public void Trim_LeadingAndTrailingWhitespace_RemovesBothEnds(byte[] bytes, byte[] expected)
    {
        // Arrange
        Memory<byte> memory = new Memory<byte>(bytes);

        // Act
        Memory<byte> result = memory.Trim();

        // Assert
        Assert.Equal(expected.Length, result.Length);
        Assert.True(result.Span.SequenceEqual(expected));
    }

    /// <summary>
    /// Verifies that Trim handles single-byte content correctly.
    /// </summary>
    [Fact]
    public void Trim_SingleNonWhitespaceByte_ReturnsOriginal()
    {
        // Arrange
        Memory<byte> memory = new Memory<byte>(new byte[] { 0x58 }); // 'X'

        // Act
        Memory<byte> result = memory.Trim();

        // Assert
        Assert.Equal(1, result.Length);
        Assert.Equal(0x58, result.Span[0]);
    }

    /// <summary>
    /// Verifies that Trim correctly handles content that contains all four whitespace types at boundaries
    /// but preserves non-whitespace bytes.
    /// </summary>
    [Fact]
    public void Trim_AllWhitespaceTypesAtBoundaries_TrimsCorrectly()
    {
        // Arrange - space, tab, CR, LF at start; content in middle; space, tab, CR, LF at end
        byte[] bytes = new byte[] { 0x20, 0x09, 0x0D, 0x0A, 0x41, 0x42, 0x43, 0x20, 0x09, 0x0D, 0x0A };
        byte[] expected = new byte[] { 0x41, 0x42, 0x43 }; // "ABC"
        Memory<byte> memory = new Memory<byte>(bytes);

        // Act
        Memory<byte> result = memory.Trim();

        // Assert
        Assert.Equal(expected.Length, result.Length);
        Assert.True(result.Span.SequenceEqual(expected));
    }

    /// <summary>
    /// Verifies that Trim returns a proper slice of the original memory,
    /// and modifications to the original affect the slice.
    /// </summary>
    [Fact]
    public void Trim_ReturnsSliceOfOriginalMemory_SharesBackingStore()
    {
        // Arrange
        byte[] backingArray = new byte[] { 0x20, 0x41, 0x42, 0x20 }; // " AB "
        Memory<byte> memory = new Memory<byte>(backingArray);

        // Act
        Memory<byte> result = memory.Trim();

        // Assert - verify the result is "AB"
        Assert.Equal(2, result.Length);
        Assert.Equal(0x41, result.Span[0]);
        Assert.Equal(0x42, result.Span[1]);

        // Modify the backing array and verify the slice sees the change
        backingArray[1] = 0x58; // Change 'A' to 'X'
        Assert.Equal(0x58, result.Span[0]);
    }

    /// <summary>
    /// Verifies that Trim handles larger buffers correctly.
    /// </summary>
    [Fact]
    public void Trim_LargeBuffer_TrimsCorrectly()
    {
        // Arrange - large content with whitespace padding
        byte[] content = new byte[100];
        for (int i = 0; i < 100; i++)
        {
            content[i] = 0x41; // Fill with 'A'
        }

        byte[] bytes = new byte[110];
        Array.Fill(bytes, (byte)0x20, 0, 5);     // 5 leading spaces
        Array.Copy(content, 0, bytes, 5, 100);   // 100 'A's
        Array.Fill(bytes, (byte)0x09, 105, 5);   // 5 trailing tabs

        Memory<byte> memory = new Memory<byte>(bytes);

        // Act
        Memory<byte> result = memory.Trim();

        // Assert
        Assert.Equal(100, result.Length);
        Assert.True(result.Span.ToArray().All(b => b == 0x41));
    }

    /// <summary>
    /// Verifies that Trim handles non-ASCII bytes correctly (they should not be treated as whitespace).
    /// </summary>
    [Fact]
    public void Trim_NonAsciiBytes_NotTreatedAsWhitespace()
    {
        // Arrange - bytes outside the ASCII whitespace set (0x20, 0x09, 0x0D, 0x0A)
        byte[] bytes = new byte[] { 0x20, 0xA0, 0x41, 0xA0, 0x20 }; // space, non-breaking space (0xA0), 'A', non-breaking space, space
        byte[] expected = new byte[] { 0xA0, 0x41, 0xA0 }; // Non-breaking spaces are NOT trimmed
        Memory<byte> memory = new Memory<byte>(bytes);

        // Act
        Memory<byte> result = memory.Trim();

        // Assert
        Assert.Equal(expected.Length, result.Length);
        Assert.True(result.Span.SequenceEqual(expected));
    }

    /// <summary>
    /// Verifies that Trim correctly handles edge case where only first and last bytes are non-whitespace.
    /// </summary>
    [Fact]
    public void Trim_OnlyFirstAndLastNonWhitespace_PreservesAll()
    {
        // Arrange
        byte[] bytes = new byte[] { 0x41, 0x20, 0x09, 0x0D, 0x0A, 0x42 }; // "A \t\r\nB"
        byte[] expected = new byte[] { 0x41, 0x20, 0x09, 0x0D, 0x0A, 0x42 }; // All preserved
        Memory<byte> memory = new Memory<byte>(bytes);

        // Act
        Memory<byte> result = memory.Trim();

        // Assert
        Assert.Equal(expected.Length, result.Length);
        Assert.True(result.Span.SequenceEqual(expected));
    }

    /// <summary>
    /// Verifies that TrimStart returns an empty span when the input is empty.
    /// </summary>
    [Fact]
    public void TrimStart_EmptySpan_ReturnsEmptySpan()
    {
        // Arrange
        ReadOnlySpan<byte> span = ReadOnlySpan<byte>.Empty;

        // Act
        ReadOnlySpan<byte> result = span.TrimStart();

        // Assert
        Assert.True(result.IsEmpty);
    }

    /// <summary>
    /// Verifies that TrimStart returns an empty span when the input contains only whitespace.
    /// Tests various whitespace characters: space (0x20), tab (0x09), CR (0x0D), LF (0x0A).
    /// </summary>
    /// <param name="whitespaceBytes">The whitespace bytes to test.</param>
    [Theory]
    [InlineData(new byte[] { 0x20 })] // Single space
    [InlineData(new byte[] { 0x09 })] // Single tab
    [InlineData(new byte[] { 0x0D })] // Single CR
    [InlineData(new byte[] { 0x0A })] // Single LF
    [InlineData(new byte[] { 0x20, 0x20 })] // Multiple spaces
    [InlineData(new byte[] { 0x09, 0x09, 0x09 })] // Multiple tabs
    [InlineData(new byte[] { 0x20, 0x09, 0x0D, 0x0A })] // Mixed whitespace
    [InlineData(new byte[] { 0x0A, 0x0D, 0x09, 0x20 })] // Mixed whitespace reversed
    [InlineData(new byte[] { 0x20, 0x20, 0x09, 0x09, 0x0D, 0x0D, 0x0A, 0x0A })] // Many mixed
    public void TrimStart_AllWhitespace_ReturnsEmptySpan(byte[] whitespaceBytes)
    {
        // Arrange
        ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(whitespaceBytes);

        // Act
        ReadOnlySpan<byte> result = span.TrimStart();

        // Assert
        Assert.True(result.IsEmpty);
    }

    /// <summary>
    /// Verifies that TrimStart returns the original span when there is no leading whitespace.
    /// </summary>
    /// <param name="input">The input bytes without leading whitespace.</param>
    /// <param name="expectedLength">The expected length of the result.</param>
    [Theory]
    [InlineData(new byte[] { 0x41 }, 1)] // Single 'A'
    [InlineData(new byte[] { 0x41, 0x42 }, 2)] // 'AB'
    [InlineData(new byte[] { 0x41, 0x42, 0x43 }, 3)] // 'ABC'
    [InlineData(new byte[] { 0x41, 0x20 }, 2)] // 'A' + trailing space
    [InlineData(new byte[] { 0x41, 0x09, 0x0D, 0x0A }, 4)] // 'A' + trailing whitespace
    [InlineData(new byte[] { 0x41, 0x42, 0x20, 0x20 }, 4)] // 'AB' + trailing spaces
    [InlineData(new byte[] { 0x00 }, 1)] // Null byte (non-whitespace)
    [InlineData(new byte[] { 0xFF }, 1)] // 0xFF (non-whitespace)
    public void TrimStart_NoLeadingWhitespace_ReturnsOriginalSpan(byte[] input, int expectedLength)
    {
        // Arrange
        ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(input);

        // Act
        ReadOnlySpan<byte> result = span.TrimStart();

        // Assert
        Assert.Equal(expectedLength, result.Length);
        Assert.True(span.SequenceEqual(result));
    }

    /// <summary>
    /// Verifies that TrimStart correctly removes leading whitespace and returns the trimmed slice.
    /// </summary>
    /// <param name="input">The input bytes with leading whitespace.</param>
    /// <param name="expectedFirstByte">The expected first byte after trimming.</param>
    /// <param name="expectedLength">The expected length after trimming.</param>
    [Theory]
    [InlineData(new byte[] { 0x20, 0x41 }, 0x41, 1)] // Space + 'A'
    [InlineData(new byte[] { 0x09, 0x41 }, 0x41, 1)] // Tab + 'A'
    [InlineData(new byte[] { 0x0D, 0x41 }, 0x41, 1)] // CR + 'A'
    [InlineData(new byte[] { 0x0A, 0x41 }, 0x41, 1)] // LF + 'A'
    [InlineData(new byte[] { 0x20, 0x20, 0x41 }, 0x41, 1)] // Two spaces + 'A'
    [InlineData(new byte[] { 0x20, 0x09, 0x41 }, 0x41, 1)] // Space + Tab + 'A'
    [InlineData(new byte[] { 0x20, 0x09, 0x0D, 0x0A, 0x41 }, 0x41, 1)] // All whitespace types + 'A'
    [InlineData(new byte[] { 0x20, 0x41, 0x42 }, 0x41, 2)] // Space + 'AB'
    [InlineData(new byte[] { 0x20, 0x20, 0x41, 0x42, 0x43 }, 0x41, 3)] // Spaces + 'ABC'
    [InlineData(new byte[] { 0x20, 0x41, 0x20 }, 0x41, 2)] // Space + 'A' + trailing space
    [InlineData(new byte[] { 0x09, 0x09, 0x41, 0x42, 0x09, 0x09 }, 0x41, 4)] // Tabs + 'AB' + trailing tabs
    public void TrimStart_LeadingWhitespace_RemovesLeadingWhitespaceOnly(byte[] input, byte expectedFirstByte, int expectedLength)
    {
        // Arrange
        ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(input);

        // Act
        ReadOnlySpan<byte> result = span.TrimStart();

        // Assert
        Assert.Equal(expectedLength, result.Length);
        Assert.Equal(expectedFirstByte, result[0]);
    }

    /// <summary>
    /// Verifies that TrimStart correctly handles a large span with leading whitespace.
    /// </summary>
    [Fact]
    public void TrimStart_LargeSpanWithLeadingWhitespace_RemovesLeadingWhitespace()
    {
        // Arrange
        byte[] data = new byte[10000];
        for (int i = 0; i < 100; i++)
        {
            data[i] = 0x20; // Leading spaces
        }
        for (int i = 100; i < 10000; i++)
        {
            data[i] = 0x41; // 'A' characters
        }
        ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(data);

        // Act
        ReadOnlySpan<byte> result = span.TrimStart();

        // Assert
        Assert.Equal(9900, result.Length);
        Assert.Equal(0x41, result[0]);
    }

    /// <summary>
    /// Verifies that TrimStart preserves trailing whitespace when only leading whitespace is present.
    /// </summary>
    [Fact]
    public void TrimStart_LeadingAndTrailingWhitespace_PreservesTrailingWhitespace()
    {
        // Arrange
        byte[] data = new byte[] { 0x20, 0x09, 0x41, 0x42, 0x43, 0x0D, 0x0A };
        ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(data);

        // Act
        ReadOnlySpan<byte> result = span.TrimStart();

        // Assert
        Assert.Equal(5, result.Length);
        Assert.Equal(0x41, result[0]); // 'A'
        Assert.Equal(0x42, result[1]); // 'B'
        Assert.Equal(0x43, result[2]); // 'C'
        Assert.Equal(0x0D, result[3]); // CR preserved
        Assert.Equal(0x0A, result[4]); // LF preserved
    }

    /// <summary>
    /// Verifies that TrimStart handles boundary values for byte data correctly.
    /// Tests bytes at extreme values (0x00, 0xFF) that are not whitespace.
    /// </summary>
    [Theory]
    [InlineData(new byte[] { 0x20, 0x00 }, 0x00)] // Space + null byte
    [InlineData(new byte[] { 0x09, 0xFF }, 0xFF)] // Tab + 0xFF
    [InlineData(new byte[] { 0x0D, 0x0A, 0x01 }, 0x01)] // CR LF + 0x01
    public void TrimStart_LeadingWhitespaceWithBoundaryBytes_RemovesWhitespaceCorrectly(byte[] input, byte expectedFirstByte)
    {
        // Arrange
        ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(input);

        // Act
        ReadOnlySpan<byte> result = span.TrimStart();

        // Assert
        Assert.Equal(expectedFirstByte, result[0]);
    }

    /// <summary>
    /// Verifies that TrimStart does not treat non-whitespace control characters as whitespace.
    /// Only ASCII whitespace (0x20, 0x09, 0x0D, 0x0A) should be trimmed.
    /// </summary>
    [Theory]
    [InlineData(new byte[] { 0x00 })] // Null
    [InlineData(new byte[] { 0x01 })] // SOH
    [InlineData(new byte[] { 0x08 })] // Backspace
    [InlineData(new byte[] { 0x0B })] // Vertical tab
    [InlineData(new byte[] { 0x0C })] // Form feed
    [InlineData(new byte[] { 0x1F })] // Unit separator
    public void TrimStart_NonWhitespaceControlCharacters_ReturnsOriginalSpan(byte[] input)
    {
        // Arrange
        ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(input);

        // Act
        ReadOnlySpan<byte> result = span.TrimStart();

        // Assert
        Assert.Equal(input.Length, result.Length);
        Assert.True(span.SequenceEqual(result));
    }

    /// <summary>
    /// Verifies that TrimStart correctly handles spans with maximum consecutive whitespace characters.
    /// </summary>
    [Fact]
    public void TrimStart_ManyConsecutiveWhitespaceBytes_ReturnsEmptySpan()
    {
        // Arrange
        byte[] data = new byte[1000];
        for (int i = 0; i < 1000; i++)
        {
            data[i] = (byte)(0x20 + (i % 4) switch { 0 => 0, 1 => 0x09 - 0x20, 2 => 0x0D - 0x20, _ => 0x0A - 0x20 });
        }
        ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(data);

        // Act
        ReadOnlySpan<byte> result = span.TrimStart();

        // Assert
        Assert.True(result.IsEmpty);
    }

    /// <summary>
    /// Tests that Trim returns the original span when the input is empty.
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
        Assert.Equal(0, result.Length);
    }

    /// <summary>
    /// Tests that Trim returns an empty span when the input contains only whitespace characters.
    /// </summary>
    /// <param name="input">Byte array containing only ASCII whitespace characters.</param>
    [Theory]
    [MemberData(nameof(AllWhitespaceTestCases))]
    public void Trim_AllWhitespace_ReturnsEmptySpan(byte[] input)
    {
        // Arrange
        ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(input);

        // Act
        ReadOnlySpan<byte> result = span.Trim();

        // Assert
        Assert.True(result.IsEmpty);
        Assert.Equal(0, result.Length);
    }

    /// <summary>
    /// Tests that Trim returns the original span when there is no whitespace to trim.
    /// </summary>
    /// <param name="input">Byte array with no leading or trailing whitespace.</param>
    /// <param name="expected">Expected result bytes.</param>
    [Theory]
    [MemberData(nameof(NoWhitespaceTestCases))]
    public void Trim_NoWhitespace_ReturnsOriginalSpan(byte[] input, byte[] expected)
    {
        // Arrange
        ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(input);

        // Act
        ReadOnlySpan<byte> result = span.Trim();

        // Assert
        Assert.Equal(expected.Length, result.Length);
        Assert.True(result.SequenceEqual(expected));
    }

    /// <summary>
    /// Tests that Trim correctly removes leading ASCII whitespace characters.
    /// </summary>
    /// <param name="input">Byte array with leading whitespace.</param>
    /// <param name="expected">Expected result bytes after trimming.</param>
    [Theory]
    [MemberData(nameof(LeadingWhitespaceTestCases))]
    public void Trim_LeadingWhitespace_TrimsLeading(byte[] input, byte[] expected)
    {
        // Arrange
        ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(input);

        // Act
        ReadOnlySpan<byte> result = span.Trim();

        // Assert
        Assert.Equal(expected.Length, result.Length);
        Assert.True(result.SequenceEqual(expected));
    }

    /// <summary>
    /// Tests that Trim correctly removes trailing ASCII whitespace characters.
    /// </summary>
    /// <param name="input">Byte array with trailing whitespace.</param>
    /// <param name="expected">Expected result bytes after trimming.</param>
    [Theory]
    [MemberData(nameof(TrailingWhitespaceTestCases))]
    public void Trim_TrailingWhitespace_TrimsTrailing(byte[] input, byte[] expected)
    {
        // Arrange
        ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(input);

        // Act
        ReadOnlySpan<byte> result = span.Trim();

        // Assert
        Assert.Equal(expected.Length, result.Length);
        Assert.True(result.SequenceEqual(expected));
    }

    /// <summary>
    /// Tests that Trim correctly removes both leading and trailing ASCII whitespace characters.
    /// </summary>
    /// <param name="input">Byte array with both leading and trailing whitespace.</param>
    /// <param name="expected">Expected result bytes after trimming.</param>
    [Theory]
    [MemberData(nameof(BothSidesWhitespaceTestCases))]
    public void Trim_BothSidesWhitespace_TrimsBothSides(byte[] input, byte[] expected)
    {
        // Arrange
        ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(input);

        // Act
        ReadOnlySpan<byte> result = span.Trim();

        // Assert
        Assert.Equal(expected.Length, result.Length);
        Assert.True(result.SequenceEqual(expected));
    }

    /// <summary>
    /// Tests that Trim preserves internal whitespace characters while trimming leading and trailing whitespace.
    /// </summary>
    /// <param name="input">Byte array with internal whitespace.</param>
    /// <param name="expected">Expected result bytes with internal whitespace preserved.</param>
    [Theory]
    [MemberData(nameof(InternalWhitespaceTestCases))]
    public void Trim_InternalWhitespace_PreservesInternal(byte[] input, byte[] expected)
    {
        // Arrange
        ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(input);

        // Act
        ReadOnlySpan<byte> result = span.Trim();

        // Assert
        Assert.Equal(expected.Length, result.Length);
        Assert.True(result.SequenceEqual(expected));
    }

    /// <summary>
    /// Tests that Trim handles single byte inputs correctly.
    /// </summary>
    /// <param name="input">Single byte array.</param>
    /// <param name="expected">Expected result bytes.</param>
    [Theory]
    [MemberData(nameof(SingleByteTestCases))]
    public void Trim_SingleByte_HandlesCorrectly(byte[] input, byte[] expected)
    {
        // Arrange
        ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(input);

        // Act
        ReadOnlySpan<byte> result = span.Trim();

        // Assert
        Assert.Equal(expected.Length, result.Length);
        if (expected.Length > 0)
        {
            Assert.True(result.SequenceEqual(expected));
        }
    }

    /// <summary>
    /// Tests that Trim does not trim non-ASCII bytes (values above 0x7F).
    /// </summary>
    /// <param name="input">Byte array containing non-ASCII bytes.</param>
    /// <param name="expected">Expected result bytes.</param>
    [Theory]
    [MemberData(nameof(NonAsciiTestCases))]
    public void Trim_NonAsciiBytes_DoesNotTrim(byte[] input, byte[] expected)
    {
        // Arrange
        ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(input);

        // Act
        ReadOnlySpan<byte> result = span.Trim();

        // Assert
        Assert.Equal(expected.Length, result.Length);
        Assert.True(result.SequenceEqual(expected));
    }

    /// <summary>
    /// Tests that Trim handles all four types of ASCII whitespace (space, tab, CR, LF) correctly.
    /// </summary>
    [Fact]
    public void Trim_AllWhitespaceTypes_TrimsAllTypes()
    {
        // Arrange - 0x20 (space), 0x09 (tab), 0x0D (CR), 0x0A (LF)
        ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(new byte[] { 0x20, 0x09, 0x0D, 0x0A, 65, 66, 67, 0x20, 0x09, 0x0D, 0x0A });
        byte[] expected = new byte[] { 65, 66, 67 }; // "ABC"

        // Act
        ReadOnlySpan<byte> result = span.Trim();

        // Assert
        Assert.Equal(expected.Length, result.Length);
        Assert.True(result.SequenceEqual(expected));
    }

    public static TheoryData<byte[]> AllWhitespaceTestCases()
    {
        return new TheoryData<byte[]>
        {
            new byte[] { 0x20 }, // Single space
            new byte[] { 0x09 }, // Single tab
            new byte[] { 0x0D }, // Single CR
            new byte[] { 0x0A }, // Single LF
            new byte[] { 0x20, 0x20, 0x20 }, // Multiple spaces
            new byte[] { 0x09, 0x09 }, // Multiple tabs
            new byte[] { 0x0D, 0x0A }, // CR + LF
            new byte[] { 0x20, 0x09, 0x0D, 0x0A }, // All types mixed
            new byte[] { 0x20, 0x09, 0x0D, 0x0A, 0x20, 0x09 } // Longer mixed
        };
    }

    public static TheoryData<byte[], byte[]> NoWhitespaceTestCases()
    {
        return new TheoryData<byte[], byte[]>
        {
            { new byte[] { 65 }, new byte[] { 65 } }, // Single 'A'
            { new byte[] { 65, 66, 67 }, new byte[] { 65, 66, 67 } }, // "ABC"
            { new byte[] { 48, 49, 50 }, new byte[] { 48, 49, 50 } }, // "012"
            { new byte[] { 0x21 }, new byte[] { 0x21 } }, // '!' (just after space in ASCII)
            { new byte[] { 0x7E }, new byte[] { 0x7E } } // '~' (high ASCII printable)
        };
    }

    public static TheoryData<byte[], byte[]> LeadingWhitespaceTestCases()
    {
        return new TheoryData<byte[], byte[]>
        {
            { new byte[] { 0x20, 65 }, new byte[] { 65 } }, // " A"
            { new byte[] { 0x09, 65, 66 }, new byte[] { 65, 66 } }, // "\tAB"
            { new byte[] { 0x0D, 0x0A, 65, 66, 67 }, new byte[] { 65, 66, 67 } }, // "\r\nABC"
            { new byte[] { 0x20, 0x20, 0x09, 65 }, new byte[] { 65 } }, // "  \tA"
            { new byte[] { 0x20, 0x09, 0x0D, 0x0A, 65, 66 }, new byte[] { 65, 66 } } // " \t\r\nAB"
        };
    }

    public static TheoryData<byte[], byte[]> TrailingWhitespaceTestCases()
    {
        return new TheoryData<byte[], byte[]>
        {
            { new byte[] { 65, 0x20 }, new byte[] { 65 } }, // "A "
            { new byte[] { 65, 66, 0x09 }, new byte[] { 65, 66 } }, // "AB\t"
            { new byte[] { 65, 66, 67, 0x0D, 0x0A }, new byte[] { 65, 66, 67 } }, // "ABC\r\n"
            { new byte[] { 65, 0x20, 0x20, 0x09 }, new byte[] { 65 } }, // "A  \t"
            { new byte[] { 65, 66, 0x20, 0x09, 0x0D, 0x0A }, new byte[] { 65, 66 } } // "AB \t\r\n"
        };
    }

    public static TheoryData<byte[], byte[]> BothSidesWhitespaceTestCases()
    {
        return new TheoryData<byte[], byte[]>
        {
            { new byte[] { 0x20, 65, 0x20 }, new byte[] { 65 } }, // " A "
            { new byte[] { 0x09, 65, 66, 0x09 }, new byte[] { 65, 66 } }, // "\tAB\t"
            { new byte[] { 0x0D, 0x0A, 65, 66, 67, 0x0D, 0x0A }, new byte[] { 65, 66, 67 } }, // "\r\nABC\r\n"
            { new byte[] { 0x20, 0x09, 65, 0x0D, 0x0A }, new byte[] { 65 } }, // " \tA\r\n"
            { new byte[] { 0x20, 0x20, 65, 66, 0x09, 0x09 }, new byte[] { 65, 66 } } // "  AB\t\t"
        };
    }

    public static TheoryData<byte[], byte[]> InternalWhitespaceTestCases()
    {
        return new TheoryData<byte[], byte[]>
        {
            { new byte[] { 65, 0x20, 66 }, new byte[] { 65, 0x20, 66 } }, // "A B"
            { new byte[] { 65, 0x09, 66, 0x20, 67 }, new byte[] { 65, 0x09, 66, 0x20, 67 } }, // "A\tB C"
            { new byte[] { 0x20, 65, 0x20, 66, 0x20 }, new byte[] { 65, 0x20, 66 } }, // " A B "
            { new byte[] { 0x09, 65, 0x0D, 0x0A, 66, 0x09 }, new byte[] { 65, 0x0D, 0x0A, 66 } }, // "\tA\r\nB\t"
            { new byte[] { 65, 0x20, 0x20, 66 }, new byte[] { 65, 0x20, 0x20, 66 } } // "A  B"
        };
    }

    public static TheoryData<byte[], byte[]> SingleByteTestCases()
    {
        return new TheoryData<byte[], byte[]>
        {
            { new byte[] { 0x20 }, Array.Empty<byte>() }, // Single space -> empty
            { new byte[] { 0x09 }, Array.Empty<byte>() }, // Single tab -> empty
            { new byte[] { 0x0D }, Array.Empty<byte>() }, // Single CR -> empty
            { new byte[] { 0x0A }, Array.Empty<byte>() }, // Single LF -> empty
            { new byte[] { 65 }, new byte[] { 65 } }, // Single 'A' -> 'A'
            { new byte[] { 0x00 }, new byte[] { 0x00 } }, // Null byte -> preserved
            { new byte[] { 0xFF }, new byte[] { 0xFF } } // High byte -> preserved
        };
    }

    public static TheoryData<byte[], byte[]> NonAsciiTestCases()
    {
        return new TheoryData<byte[], byte[]>
        {
            { new byte[] { 0xFF }, new byte[] { 0xFF } }, // High byte alone
            { new byte[] { 0x80, 0x90, 0xA0 }, new byte[] { 0x80, 0x90, 0xA0 } }, // Multiple non-ASCII
            { new byte[] { 0x20, 0xFF, 0x20 }, new byte[] { 0xFF } }, // Non-ASCII with whitespace
            { new byte[] { 0xC3, 0xA9 }, new byte[] { 0xC3, 0xA9 } }, // UTF-8 é (should not trim)
            { new byte[] { 0x09, 0xFF, 0x20 }, new byte[] { 0xFF } }, // Non-ASCII with surrounding whitespace
            { new byte[] { 0x00 }, new byte[] { 0x00 } } // Null byte is not whitespace
        };
    }
}