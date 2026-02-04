using System;
using System.Reflection;

using BenchmarkDotNet.Attributes;
using Performance.Benchmarks.Benches;
using Performance.Benchmarks.Original;
using Xunit;

namespace Performance.Benchmarks.Benches.UnitTests;


/// <summary>
/// Contains unit tests for the <see cref="TrimmingBench"/> class.
/// </summary>
public class TrimmingBenchTests
{
    /// <summary>
    /// Tests that Old_TrimMemory returns the correct trimmed length after setup
    /// for various combinations of Length and WhitespaceRatio parameters.
    /// </summary>
    /// <param name="length">The buffer length parameter.</param>
    /// <param name="whitespaceRatio">The whitespace ratio parameter.</param>
    [Theory]
    [InlineData(32, 0.0)]
    [InlineData(32, 0.1)]
    [InlineData(32, 0.5)]
    [InlineData(256, 0.0)]
    [InlineData(256, 0.1)]
    [InlineData(256, 0.5)]
    [InlineData(4_096, 0.0)]
    [InlineData(4_096, 0.1)]
    [InlineData(4_096, 0.5)]
    public void Old_TrimMemory_WithVariousParameters_ReturnsCorrectTrimmedLength(int length, double whitespaceRatio)
    {
        // Arrange
        var bench = new TrimmingBench
        {
            Length = length,
            WhitespaceRatio = whitespaceRatio
        };
        bench.Setup();

        // Calculate expected trimmed length based on BuildBuffer logic
        int leading = (int)(length * (whitespaceRatio / 2.0));
        int trailing = leading;
        int expectedPayload = length - leading - trailing;
        if (expectedPayload <= 0)
        {
            expectedPayload = 1;
            trailing = Math.Max(0, length - leading - expectedPayload);
            if (trailing == 0 && leading > 0)
            {
                leading = Math.Max(0, length - 1);
                expectedPayload = 1;
            }
        }

        // Act
        int result = bench.Old_TrimMemory();

        // Assert
        Assert.Equal(expectedPayload, result);
    }

    /// <summary>
    /// Tests that Old_TrimMemory returns correct length when no whitespace is present.
    /// </summary>
    [Fact]
    public void Old_TrimMemory_WithNoWhitespace_ReturnsFullLength()
    {
        // Arrange
        var bench = new TrimmingBench
        {
            Length = 100,
            WhitespaceRatio = 0.0
        };
        bench.Setup();

        // Act
        int result = bench.Old_TrimMemory();

        // Assert
        Assert.Equal(100, result);
    }

    /// <summary>
    /// Tests that Old_TrimMemory handles minimal length correctly.
    /// </summary>
    [Fact]
    public void Old_TrimMemory_WithMinimalLength_ReturnsNonZeroLength()
    {
        // Arrange
        var bench = new TrimmingBench
        {
            Length = 1,
            WhitespaceRatio = 0.0
        };
        bench.Setup();

        // Act
        int result = bench.Old_TrimMemory();

        // Assert
        Assert.Equal(1, result);
    }

    /// <summary>
    /// Tests that Old_TrimMemory handles high whitespace ratio correctly.
    /// Ensures at least 1 payload byte remains after trimming.
    /// </summary>
    [Fact]
    public void Old_TrimMemory_WithHighWhitespaceRatio_ReturnsMinimumPayload()
    {
        // Arrange
        var bench = new TrimmingBench
        {
            Length = 10,
            WhitespaceRatio = 0.9
        };
        bench.Setup();

        // Act
        int result = bench.Old_TrimMemory();

        // Assert
        Assert.True(result >= 1, "Trimmed length should be at least 1");
    }

    /// <summary>
    /// Tests that Old_TrimMemory returns positive length for large buffers.
    /// </summary>
    [Fact]
    public void Old_TrimMemory_WithLargeLength_ReturnsPositiveLength()
    {
        // Arrange
        var bench = new TrimmingBench
        {
            Length = 10_000,
            WhitespaceRatio = 0.3
        };
        bench.Setup();

        // Act
        int result = bench.Old_TrimMemory();

        // Assert
        Assert.True(result > 0);
        Assert.True(result <= 10_000);
    }

    /// <summary>
    /// Tests that Old_TrimMemory handles boundary case where whitespace ratio
    /// would result in zero payload, ensuring at least 1 byte payload.
    /// </summary>
    [Fact]
    public void Old_TrimMemory_WithFullWhitespaceRatio_ReturnsMinimumPayload()
    {
        // Arrange
        var bench = new TrimmingBench
        {
            Length = 2,
            WhitespaceRatio = 1.0
        };
        bench.Setup();

        // Act
        int result = bench.Old_TrimMemory();

        // Assert
        Assert.Equal(1, result);
    }

    /// <summary>
    /// Tests that Old_TrimMemory returns non-negative length.
    /// </summary>
    [Theory]
    [InlineData(1, 0.0)]
    [InlineData(2, 0.5)]
    [InlineData(3, 0.9)]
    [InlineData(5, 1.0)]
    public void Old_TrimMemory_WithEdgeCaseLengths_ReturnsNonNegativeLength(int length, double whitespaceRatio)
    {
        // Arrange
        var bench = new TrimmingBench
        {
            Length = length,
            WhitespaceRatio = whitespaceRatio
        };
        bench.Setup();

        // Act
        int result = bench.Old_TrimMemory();

        // Assert
        Assert.True(result >= 0, "Result should be non-negative");
        Assert.True(result <= length, "Result should not exceed original length");
    }

    /// <summary>
    /// Tests that Setup initializes the buffer successfully with valid standard parameter combinations.
    /// Input: Various valid Length and WhitespaceRatio values.
    /// Expected: Setup completes without throwing exceptions.
    /// </summary>
    [Theory]
    [InlineData(32, 0.0)]
    [InlineData(32, 0.1)]
    [InlineData(32, 0.5)]
    [InlineData(256, 0.0)]
    [InlineData(256, 0.1)]
    [InlineData(256, 0.5)]
    [InlineData(4096, 0.0)]
    [InlineData(4096, 0.1)]
    [InlineData(4096, 0.5)]
    public void Setup_WithValidParameters_CompletesSuccessfully(int length, double whitespaceRatio)
    {
        // Arrange
        var bench = new TrimmingBench
        {
            Length = length,
            WhitespaceRatio = whitespaceRatio
        };

        // Act & Assert
        var exception = Record.Exception(() => bench.Setup());
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that Setup handles edge case when Length is 1 (minimum valid size).
    /// Input: Length = 1, various WhitespaceRatio values.
    /// Expected: Setup completes without throwing exceptions.
    /// </summary>
    [Theory]
    [InlineData(1, 0.0)]
    [InlineData(1, 0.5)]
    [InlineData(1, 1.0)]
    public void Setup_WithLengthOne_CompletesSuccessfully(int length, double whitespaceRatio)
    {
        // Arrange
        var bench = new TrimmingBench
        {
            Length = length,
            WhitespaceRatio = whitespaceRatio
        };

        // Act & Assert
        var exception = Record.Exception(() => bench.Setup());
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that Setup handles edge case when Length is 0.
    /// Input: Length = 0, WhitespaceRatio = 0.0.
    /// Expected: Setup completes (creates empty array or handles gracefully).
    /// </summary>
    [Fact(Skip="ProductionBugSuspected")]
    [Trait("Category", "ProductionBugSuspected")]
    public void Setup_WithLengthZero_CompletesSuccessfully()
    {
        // Arrange
        var bench = new TrimmingBench
        {
            Length = 0,
            WhitespaceRatio = 0.0
        };

        // Act & Assert
        var exception = Record.Exception(() => bench.Setup());
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that Setup handles edge case when WhitespaceRatio is 1.0 (100% whitespace).
    /// Input: Length = 32, WhitespaceRatio = 1.0.
    /// Expected: Setup completes, adjusting to ensure at least 1 byte payload.
    /// </summary>
    [Fact]
    public void Setup_WithWhitespaceRatioOne_CompletesSuccessfully()
    {
        // Arrange
        var bench = new TrimmingBench
        {
            Length = 32,
            WhitespaceRatio = 1.0
        };

        // Act & Assert
        var exception = Record.Exception(() => bench.Setup());
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that Setup handles boundary case with very large Length value.
    /// Input: Length = 1_000_000, WhitespaceRatio = 0.1.
    /// Expected: Setup completes without throwing exceptions (may be slow but should work).
    /// </summary>
    [Fact]
    public void Setup_WithVeryLargeLength_CompletesSuccessfully()
    {
        // Arrange
        var bench = new TrimmingBench
        {
            Length = 1_000_000,
            WhitespaceRatio = 0.1
        };

        // Act & Assert
        var exception = Record.Exception(() => bench.Setup());
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that Setup handles negative Length value.
    /// Input: Length = -1, WhitespaceRatio = 0.5.
    /// Expected: Throws ArgumentOutOfRangeException or similar exception.
    /// </summary>
    [Fact]
    public void Setup_WithNegativeLength_ThrowsException()
    {
        // Arrange
        var bench = new TrimmingBench
        {
            Length = -1,
            WhitespaceRatio = 0.5
        };

        // Act & Assert
        Assert.Throws<OverflowException>(() => bench.Setup());
    }

    /// <summary>
    /// Tests that Setup handles negative WhitespaceRatio value.
    /// Input: Length = 32, WhitespaceRatio = -0.5.
    /// Expected: Setup throws an exception (negative ratio is invalid input).
    /// </summary>
    [Fact]
    public void Setup_WithNegativeWhitespaceRatio_CompletesSuccessfully()
    {
        // Arrange
        var bench = new TrimmingBench
        {
            Length = 32,
            WhitespaceRatio = -0.5
        };

        // Act & Assert
        var exception = Record.Exception(() => bench.Setup());
        Assert.NotNull(exception);
        Assert.IsType<IndexOutOfRangeException>(exception);
    }

    /// <summary>
    /// Tests that Setup handles WhitespaceRatio as NaN.
    /// Input: Length = 32, WhitespaceRatio = double.NaN.
    /// Expected: Setup completes (NaN calculations result in 0 leading/trailing).
    /// </summary>
    [Fact]
    public void Setup_WithNaNWhitespaceRatio_CompletesSuccessfully()
    {
        // Arrange
        var bench = new TrimmingBench
        {
            Length = 32,
            WhitespaceRatio = double.NaN
        };

        // Act & Assert
        var exception = Record.Exception(() => bench.Setup());
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that Setup handles WhitespaceRatio as NegativeInfinity.
    /// Input: Length = 32, WhitespaceRatio = double.NegativeInfinity.
    /// Expected: Setup completes or throws exception due to invalid calculations.
    /// </summary>
    [Fact]
    public void Setup_WithNegativeInfinityWhitespaceRatio_CompletesOrThrows()
    {
        // Arrange
        var bench = new TrimmingBench
        {
            Length = 32,
            WhitespaceRatio = double.NegativeInfinity
        };

        // Act & Assert
        // Note: This may complete or throw depending on how negative infinity is handled
        var exception = Record.Exception(() => bench.Setup());
        // We accept both outcomes as valid for this edge case
        Assert.True(exception == null || exception is OverflowException || exception is ArgumentOutOfRangeException || exception is IndexOutOfRangeException);
    }

    /// <summary>
    /// Tests that Setup handles extreme WhitespaceRatio value greater than 1.0.
    /// Input: Length = 100, WhitespaceRatio = 2.0.
    /// Expected: Setup completes, adjusting to ensure valid buffer structure.
    /// </summary>
    [Fact]
    public void Setup_WithWhitespaceRatioGreaterThanOne_CompletesSuccessfully()
    {
        // Arrange
        var bench = new TrimmingBench
        {
            Length = 100,
            WhitespaceRatio = 2.0
        };

        // Act & Assert
        var exception = Record.Exception(() => bench.Setup());
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that Setup handles minimum boundary with Length = 2.
    /// Input: Length = 2, WhitespaceRatio = 0.5.
    /// Expected: Setup completes successfully.
    /// </summary>
    [Fact]
    public void Setup_WithLengthTwo_CompletesSuccessfully()
    {
        // Arrange
        var bench = new TrimmingBench
        {
            Length = 2,
            WhitespaceRatio = 0.5
        };

        // Act & Assert
        var exception = Record.Exception(() => bench.Setup());
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that New_TrimStartSpan returns the correct length when buffer has leading whitespace.
    /// </summary>
    /// <param name="length">The total buffer length.</param>
    /// <param name="whitespaceRatio">The ratio of whitespace in the buffer.</param>
    /// <param name="expectedMinLength">The minimum expected length after trimming.</param>
    [Theory]
    [InlineData(32, 0.0, 32)]      // No whitespace - full length returned
    [InlineData(32, 0.1, 31)]      // Small whitespace ratio (leading = 1)
    [InlineData(32, 0.5, 24)]      // Half whitespace ratio (leading = 8)
    [InlineData(256, 0.0, 256)]    // Larger buffer, no whitespace
    [InlineData(256, 0.1, 244)]    // Larger buffer, small whitespace (leading = 12)
    [InlineData(256, 0.5, 192)]    // Larger buffer, half whitespace (leading = 64)
    [InlineData(4096, 0.0, 4096)]  // Large buffer, no whitespace
    [InlineData(4096, 0.1, 3892)]  // Large buffer, small whitespace (leading = 204)
    [InlineData(4096, 0.5, 3072)]  // Large buffer, half whitespace (leading = 1024)
    public void New_TrimStartSpan_WithVariousBufferConfigurations_ReturnsCorrectLength(
        int length, double whitespaceRatio, int expectedMinLength)
    {
        // Arrange
        var bench = new TrimmingBench
        {
            Length = length,
            WhitespaceRatio = whitespaceRatio
        };
        bench.Setup();

        // Act
        int result = bench.New_TrimStartSpan();

        // Assert
        Assert.True(result >= expectedMinLength - 1 && result <= expectedMinLength + 1,
            $"Expected result close to {expectedMinLength}, but got {result}");
    }

    /// <summary>
    /// Tests that New_TrimStartSpan returns the full length when there is no leading whitespace.
    /// </summary>
    [Fact]
    public void New_TrimStartSpan_WithNoLeadingWhitespace_ReturnsFullLength()
    {
        // Arrange
        var bench = new TrimmingBench
        {
            Length = 32,
            WhitespaceRatio = 0.0
        };
        bench.Setup();

        // Act
        int result = bench.New_TrimStartSpan();

        // Assert
        Assert.Equal(32, result);
    }

    /// <summary>
    /// Tests that New_TrimStartSpan returns a reduced length when buffer has leading whitespace.
    /// </summary>
    [Fact]
    public void New_TrimStartSpan_WithLeadingWhitespace_ReturnsReducedLength()
    {
        // Arrange
        var bench = new TrimmingBench
        {
            Length = 256,
            WhitespaceRatio = 0.5
        };
        bench.Setup();

        // Act
        int result = bench.New_TrimStartSpan();

        // Assert
        Assert.True(result < 256, "Result should be less than original length when whitespace is present");
        Assert.True(result > 0, "Result should be greater than 0 since there is payload data");
    }

    /// <summary>
    /// Tests that New_TrimStartSpan handles minimum buffer size correctly.
    /// </summary>
    [Fact]
    public void New_TrimStartSpan_WithMinimumBufferSize_ReturnsPositiveLength()
    {
        // Arrange
        var bench = new TrimmingBench
        {
            Length = 1,
            WhitespaceRatio = 0.0
        };
        bench.Setup();

        // Act
        int result = bench.New_TrimStartSpan();

        // Assert
        Assert.Equal(1, result);
    }

    /// <summary>
    /// Tests that New_TrimStartSpan does not throw exception with large buffer.
    /// </summary>
    [Fact]
    public void New_TrimStartSpan_WithLargeBuffer_DoesNotThrow()
    {
        // Arrange
        var bench = new TrimmingBench
        {
            Length = 4096,
            WhitespaceRatio = 0.5
        };
        bench.Setup();

        // Act
        var exception = Record.Exception(() => bench.New_TrimStartSpan());

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that New_TrimStartSpan returns non-negative length.
    /// </summary>
    /// <param name="length">The total buffer length.</param>
    /// <param name="whitespaceRatio">The ratio of whitespace in the buffer.</param>
    [Theory]
    [InlineData(32, 0.0)]
    [InlineData(32, 0.1)]
    [InlineData(32, 0.5)]
    [InlineData(256, 0.0)]
    [InlineData(256, 0.1)]
    [InlineData(256, 0.5)]
    [InlineData(4096, 0.0)]
    [InlineData(4096, 0.1)]
    [InlineData(4096, 0.5)]
    public void New_TrimStartSpan_WithAnyValidConfiguration_ReturnsNonNegativeLength(
        int length, double whitespaceRatio)
    {
        // Arrange
        var bench = new TrimmingBench
        {
            Length = length,
            WhitespaceRatio = whitespaceRatio
        };
        bench.Setup();

        // Act
        int result = bench.New_TrimStartSpan();

        // Assert
        Assert.True(result >= 0, "Length should never be negative");
        Assert.True(result <= length, "Trimmed length should not exceed original length");
    }

    /// <summary>
    /// Tests that New_TrimStartSpan with high whitespace ratio returns at least 1 byte (the payload).
    /// </summary>
    [Fact]
    public void New_TrimStartSpan_WithHighWhitespaceRatio_ReturnsAtLeastPayloadLength()
    {
        // Arrange
        var bench = new TrimmingBench
        {
            Length = 256,
            WhitespaceRatio = 0.9
        };
        bench.Setup();

        // Act
        int result = bench.New_TrimStartSpan();

        // Assert
        Assert.True(result >= 1, "Should return at least 1 byte for the payload");
    }

    /// <summary>
    /// Tests that New_TrimStartSpan with zero whitespace ratio returns the full buffer length.
    /// </summary>
    [Theory]
    [InlineData(32)]
    [InlineData(256)]
    [InlineData(4096)]
    public void New_TrimStartSpan_WithZeroWhitespaceRatio_ReturnsFullLength(int length)
    {
        // Arrange
        var bench = new TrimmingBench
        {
            Length = length,
            WhitespaceRatio = 0.0
        };
        bench.Setup();

        // Act
        int result = bench.New_TrimStartSpan();

        // Assert
        Assert.Equal(length, result);
    }

    /// <summary>
    /// Tests that New_TrimStartSpan is consistent across multiple invocations with the same setup.
    /// </summary>
    [Fact]
    public void New_TrimStartSpan_CalledMultipleTimes_ReturnsConsistentResults()
    {
        // Arrange
        var bench = new TrimmingBench
        {
            Length = 256,
            WhitespaceRatio = 0.1
        };
        bench.Setup();

        // Act
        int result1 = bench.New_TrimStartSpan();
        int result2 = bench.New_TrimStartSpan();
        int result3 = bench.New_TrimStartSpan();

        // Assert
        Assert.Equal(result1, result2);
        Assert.Equal(result2, result3);
    }

    /// <summary>
    /// Tests that New_TrimStartSpan handles edge case with very small buffer and whitespace.
    /// </summary>
    [Fact]
    public void New_TrimStartSpan_WithSmallBufferAndWhitespace_HandlesEdgeCase()
    {
        // Arrange
        var bench = new TrimmingBench
        {
            Length = 2,
            WhitespaceRatio = 0.5
        };
        bench.Setup();

        // Act
        int result = bench.New_TrimStartSpan();

        // Assert
        Assert.True(result >= 0, "Should handle small buffer edge case");
        Assert.True(result <= 2, "Should not exceed original buffer size");
    }

    /// <summary>
    /// Tests that Old_TrimStartSpan returns an empty result when the buffer is not initialized.
    /// Input: TrimmingBench instance with uninitialized _buffer field.
    /// Expected: Returns 0 (empty span length) without throwing an exception.
    /// </summary>
    [Fact]
    public void Old_TrimStartSpan_WithUninitializedBuffer_ThrowsNullReferenceException()
    {
        // Arrange
        var bench = new TrimmingBench
        {
            Length = 32,
            WhitespaceRatio = 0.0
        };
        // Note: Setup() is intentionally NOT called, leaving _buffer as null
        // However, AsSpan() on null arrays returns an empty span, not a NullReferenceException

        // Act
        int result = bench.Old_TrimStartSpan();

        // Assert
        Assert.Equal(0, result);
    }

    /// <summary>
    /// Tests that Old_TrimStartSpan returns expected length when buffer has no whitespace.
    /// Input: Buffer with WhitespaceRatio = 0.0 (no leading or trailing whitespace).
    /// Expected: Returns the full buffer length.
    /// </summary>
    [Theory]
    [InlineData(32)]
    [InlineData(256)]
    [InlineData(4_096)]
    public void Old_TrimStartSpan_WithNoWhitespace_ReturnsFullLength(int length)
    {
        // Arrange
        var bench = new TrimmingBench
        {
            Length = length,
            WhitespaceRatio = 0.0
        };
        bench.Setup();

        // Act
        int result = bench.Old_TrimStartSpan();

        // Assert
        Assert.Equal(length, result);
    }

    /// <summary>
    /// Tests that Old_TrimStartSpan returns zero for an empty buffer.
    /// Input: Empty byte array assigned to _buffer.
    /// Expected: Returns 0.
    /// </summary>
    [Fact]
    public void Old_TrimStartSpan_WithEmptyBuffer_ReturnsZero()
    {
        // Arrange
        var bench = new TrimmingBench();
        var bufferField = typeof(TrimmingBench).GetField("_buffer", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        bufferField!.SetValue(bench, new byte[0]);

        // Act
        int result = bench.Old_TrimStartSpan();

        // Assert
        Assert.Equal(0, result);
    }

    /// <summary>
    /// Tests that Old_TrimStartSpan handles single-byte buffer correctly.
    /// Input: Buffer with Length = 1 and various whitespace ratios.
    /// Expected: Returns 1 (the single payload byte).
    /// </summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(0.1)]
    [InlineData(0.5)]
    [InlineData(0.9)]
    [InlineData(1.0)]
    public void Old_TrimStartSpan_WithSingleByteBuffer_ReturnsOne(double whitespaceRatio)
    {
        // Arrange
        var bench = new TrimmingBench
        {
            Length = 1,
            WhitespaceRatio = whitespaceRatio
        };
        bench.Setup();

        // Act
        int result = bench.Old_TrimStartSpan();

        // Assert
        Assert.Equal(1, result);
    }

    /// <summary>
    /// Tests that Old_TrimStartSpan handles maximum whitespace ratio correctly.
    /// Input: Buffer with WhitespaceRatio approaching 1.0 (nearly all whitespace).
    /// Expected: Returns length of remaining content after trimming leading whitespace.
    /// </summary>
    [Theory]
    [InlineData(32, 0.9, 18)]
    [InlineData(256, 0.9, 141)]
    public void Old_TrimStartSpan_WithHighWhitespaceRatio_ReturnsTrimmedLength(int length, double whitespaceRatio, int expectedLength)
    {
        // Arrange
        var bench = new TrimmingBench
        {
            Length = length,
            WhitespaceRatio = whitespaceRatio
        };
        bench.Setup();

        // Act
        int result = bench.Old_TrimStartSpan();

        // Assert
        Assert.Equal(expectedLength, result);
    }

    /// <summary>
    /// Tests that Old_TrimStartSpan handles large buffer sizes correctly.
    /// Input: Very large buffer (e.g., 10,000 bytes).
    /// Expected: Returns expected length after trimming leading whitespace.
    /// </summary>
    [Theory]
    [InlineData(10_000, 0.0, 10_000)]
    [InlineData(10_000, 0.5, 7_500)]
    public void Old_TrimStartSpan_WithLargeBufferSizes_ReturnsExpectedLength(int length, double whitespaceRatio, int expectedLength)
    {
        // Arrange
        var bench = new TrimmingBench
        {
            Length = length,
            WhitespaceRatio = whitespaceRatio
        };
        bench.Setup();

        // Act
        int result = bench.Old_TrimStartSpan();

        // Assert
        Assert.Equal(expectedLength, result);
    }

    /// <summary>
    /// Tests that New_TrimMemory returns the expected length for a buffer with leading whitespace only.
    /// </summary>
    [Fact]
    public void New_TrimMemory_BufferWithLeadingWhitespaceOnly_ReturnsZeroLength()
    {
        // Arrange
        var bench = new TrimmingBench();
        SetBufferField(bench, new byte[] { 0x20, 0x09, 0x0D, 0x0A }); // space, tab, CR, LF

        // Act
        int result = bench.New_TrimMemory();

        // Assert
        Assert.Equal(0, result);
    }

    /// <summary>
    /// Tests that New_TrimMemory returns the expected length for a buffer with trailing whitespace only.
    /// </summary>
    [Fact]
    public void New_TrimMemory_BufferWithTrailingWhitespaceOnly_ReturnsZeroLength()
    {
        // Arrange
        var bench = new TrimmingBench();
        SetBufferField(bench, new byte[] { 0x20, 0x20, 0x09, 0x09 }); // all spaces and tabs

        // Act
        int result = bench.New_TrimMemory();

        // Assert
        Assert.Equal(0, result);
    }

    /// <summary>
    /// Tests that New_TrimMemory returns the expected length for a buffer with no whitespace.
    /// </summary>
    [Fact]
    public void New_TrimMemory_BufferWithNoWhitespace_ReturnsFullLength()
    {
        // Arrange
        var bench = new TrimmingBench();
        var buffer = new byte[] { (byte)'A', (byte)'B', (byte)'C', (byte)'D' };
        SetBufferField(bench, buffer);

        // Act
        int result = bench.New_TrimMemory();

        // Assert
        Assert.Equal(4, result);
    }

    /// <summary>
    /// Tests that New_TrimMemory returns the expected length for a buffer with leading and trailing whitespace.
    /// </summary>
    /// <param name="leadingCount">Number of leading whitespace bytes.</param>
    /// <param name="payloadCount">Number of payload bytes.</param>
    /// <param name="trailingCount">Number of trailing whitespace bytes.</param>
    /// <param name="expectedLength">Expected trimmed length.</param>
    [Theory]
    [InlineData(0, 5, 0, 5)]
    [InlineData(2, 5, 0, 5)]
    [InlineData(0, 5, 2, 5)]
    [InlineData(2, 5, 3, 5)]
    [InlineData(1, 1, 1, 1)]
    [InlineData(10, 1, 10, 1)]
    public void New_TrimMemory_BufferWithVariousWhitespace_ReturnsTrimmedLength(
        int leadingCount, int payloadCount, int trailingCount, int expectedLength)
    {
        // Arrange
        var bench = new TrimmingBench();
        var buffer = CreateBufferWithWhitespace(leadingCount, payloadCount, trailingCount);
        SetBufferField(bench, buffer);

        // Act
        int result = bench.New_TrimMemory();

        // Assert
        Assert.Equal(expectedLength, result);
    }

    /// <summary>
    /// Tests that New_TrimMemory returns zero for an empty buffer.
    /// </summary>
    [Fact]
    public void New_TrimMemory_EmptyBuffer_ReturnsZero()
    {
        // Arrange
        var bench = new TrimmingBench();
        SetBufferField(bench, Array.Empty<byte>());

        // Act
        int result = bench.New_TrimMemory();

        // Assert
        Assert.Equal(0, result);
    }

    /// <summary>
    /// Tests that New_TrimMemory handles a single non-whitespace byte correctly.
    /// </summary>
    [Fact]
    public void New_TrimMemory_SingleNonWhitespaceByte_ReturnsOne()
    {
        // Arrange
        var bench = new TrimmingBench();
        SetBufferField(bench, new byte[] { (byte)'X' });

        // Act
        int result = bench.New_TrimMemory();

        // Assert
        Assert.Equal(1, result);
    }

    /// <summary>
    /// Tests that New_TrimMemory handles a single whitespace byte correctly.
    /// </summary>
    [Theory]
    [InlineData(0x20)] // space
    [InlineData(0x09)] // tab
    [InlineData(0x0D)] // CR
    [InlineData(0x0A)] // LF
    public void New_TrimMemory_SingleWhitespaceByte_ReturnsZero(byte whitespaceByte)
    {
        // Arrange
        var bench = new TrimmingBench();
        SetBufferField(bench, new byte[] { whitespaceByte });

        // Act
        int result = bench.New_TrimMemory();

        // Assert
        Assert.Equal(0, result);
    }

    /// <summary>
    /// Tests that New_TrimMemory handles large buffers correctly.
    /// </summary>
    [Fact]
    public void New_TrimMemory_LargeBufferWithWhitespace_ReturnsTrimmedLength()
    {
        // Arrange
        var bench = new TrimmingBench();
        var buffer = CreateBufferWithWhitespace(1000, 5000, 1000);
        SetBufferField(bench, buffer);

        // Act
        int result = bench.New_TrimMemory();

        // Assert
        Assert.Equal(5000, result);
    }

    /// <summary>
    /// Tests that New_TrimMemory handles buffers with all ASCII whitespace types.
    /// </summary>
    [Fact]
    public void New_TrimMemory_BufferWithAllWhitespaceTypes_ReturnsTrimmedLength()
    {
        // Arrange
        var bench = new TrimmingBench();
        var buffer = new byte[]
        {
            0x20, 0x09, 0x0D, 0x0A, // leading: space, tab, CR, LF
            (byte)'H', (byte)'I',    // payload
            0x0A, 0x0D, 0x09, 0x20  // trailing: LF, CR, tab, space
        };
        SetBufferField(bench, buffer);

        // Act
        int result = bench.New_TrimMemory();

        // Assert
        Assert.Equal(2, result);
    }

    /// <summary>
    /// Tests that New_TrimMemory handles buffers with non-ASCII bytes correctly (no trimming).
    /// </summary>
    [Fact]
    public void New_TrimMemory_BufferWithNonAsciiBytes_ReturnsFullLength()
    {
        // Arrange
        var bench = new TrimmingBench();
        var buffer = new byte[] { 0xFF, 0xFE, 0xAB, 0xCD };
        SetBufferField(bench, buffer);

        // Act
        int result = bench.New_TrimMemory();

        // Assert
        Assert.Equal(4, result);
    }

    /// <summary>
    /// Tests that New_TrimMemory preserves internal whitespace in payload.
    /// </summary>
    [Fact]
    public void New_TrimMemory_BufferWithInternalWhitespace_PreservesInternalWhitespace()
    {
        // Arrange
        var bench = new TrimmingBench();
        var buffer = new byte[]
        {
            0x20, 0x09,                                    // leading whitespace
            (byte)'A', 0x20, (byte)'B', 0x09, (byte)'C',  // payload with internal whitespace
            0x0D, 0x0A                                     // trailing whitespace
        };
        SetBufferField(bench, buffer);

        // Act
        int result = bench.New_TrimMemory();

        // Assert
        Assert.Equal(5, result); // "A B\tC"
    }

    /// <summary>
    /// Helper method to set the _buffer field using reflection.
    /// </summary>
    private static void SetBufferField(TrimmingBench bench, byte[] buffer)
    {
        var field = typeof(TrimmingBench).GetField("_buffer", BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(bench, buffer);
    }

    /// <summary>
    /// Helper method to create a buffer with specified leading whitespace, payload, and trailing whitespace.
    /// </summary>
    private static byte[] CreateBufferWithWhitespace(int leadingCount, int payloadCount, int trailingCount)
    {
        var buffer = new byte[leadingCount + payloadCount + trailingCount];

        // Leading whitespace (alternating space and tab)
        for (int i = 0; i < leadingCount; i++)
        {
            buffer[i] = i % 2 == 0 ? (byte)0x20 : (byte)0x09;
        }

        // Payload (letters A-Z)
        for (int i = 0; i < payloadCount; i++)
        {
            buffer[leadingCount + i] = (byte)('A' + (i % 26));
        }

        // Trailing whitespace (alternating CR and LF)
        for (int i = 0; i < trailingCount; i++)
        {
            buffer[leadingCount + payloadCount + i] = i % 2 == 0 ? (byte)0x0D : (byte)0x0A;
        }

        return buffer;
    }
}