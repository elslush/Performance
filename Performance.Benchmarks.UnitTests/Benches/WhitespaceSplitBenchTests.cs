using System;

using BenchmarkDotNet.Attributes;
using Performance.Benchmarks.Benches;
using Performance.Benchmarks.Whitespace;
using Xunit;

namespace Performance.Benchmarks.Benches.UnitTests;


/// <summary>
/// Unit tests for the <see cref="WhitespaceSplitBench"/> class.
/// </summary>
public class WhitespaceSplitBenchTests
{
    /// <summary>
    /// Tests that OriginalEnumerator returns zero for an empty string.
    /// Input: Length = 0, WhitespaceRatio = 0.0
    /// Expected: Returns 0 (no tokens to count).
    /// </summary>
    [Fact]
    public void OriginalEnumerator_EmptyString_ReturnsZero()
    {
        // Arrange
        var bench = new WhitespaceSplitBench
        {
            Length = 0,
            WhitespaceRatio = 0.0
        };
        bench.Setup();

        // Act
        int result = bench.OriginalEnumerator();

        // Assert
        Assert.Equal(0, result);
    }

    /// <summary>
    /// Tests that OriginalEnumerator returns zero for a string with only whitespace.
    /// Input: Length = 100, WhitespaceRatio = 1.0 (all whitespace)
    /// Expected: Returns 0 (no non-whitespace tokens).
    /// </summary>
    [Fact]
    public void OriginalEnumerator_OnlyWhitespace_ReturnsZero()
    {
        // Arrange
        var bench = new WhitespaceSplitBench
        {
            Length = 100,
            WhitespaceRatio = 1.0
        };
        bench.Setup();

        // Act
        int result = bench.OriginalEnumerator();

        // Assert
        Assert.Equal(0, result);
    }

    /// <summary>
    /// Tests that OriginalEnumerator counts all characters when there is no whitespace.
    /// Input: Length = 100, WhitespaceRatio = 0.0 (no whitespace)
    /// Expected: Returns a positive value equal to or close to the length.
    /// </summary>
    [Fact]
    public void OriginalEnumerator_NoWhitespace_ReturnsPositiveValue()
    {
        // Arrange
        var bench = new WhitespaceSplitBench
        {
            Length = 100,
            WhitespaceRatio = 0.0
        };
        bench.Setup();

        // Act
        int result = bench.OriginalEnumerator();

        // Assert
        Assert.True(result > 0, "Expected positive accumulated length for non-whitespace text");
        Assert.True(result <= 100, "Accumulated length should not exceed total length");
    }

    /// <summary>
    /// Tests that OriginalEnumerator correctly handles mixed whitespace and non-whitespace content.
    /// Input: Length = 1000, WhitespaceRatio = 0.5 (50% whitespace)
    /// Expected: Returns a positive value less than the total length.
    /// </summary>
    [Fact]
    public void OriginalEnumerator_MixedContent_ReturnsPositiveValue()
    {
        // Arrange
        var bench = new WhitespaceSplitBench
        {
            Length = 1000,
            WhitespaceRatio = 0.5
        };
        bench.Setup();

        // Act
        int result = bench.OriginalEnumerator();

        // Assert
        Assert.True(result > 0, "Expected positive accumulated length for mixed content");
        Assert.True(result <= 1000, "Accumulated length should not exceed total length");
    }

    /// <summary>
    /// Tests that OriginalEnumerator handles various whitespace ratios correctly.
    /// Input: Various combinations of Length and WhitespaceRatio
    /// Expected: Returns non-negative values that don't exceed the input length.
    /// </summary>
    [Theory]
    [InlineData(1, 0.0)]
    [InlineData(1, 1.0)]
    [InlineData(10, 0.1)]
    [InlineData(10, 0.9)]
    [InlineData(100, 0.3)]
    [InlineData(1000, 0.5)]
    [InlineData(10000, 0.1)]
    public void OriginalEnumerator_VariousInputs_ReturnsValidResult(int length, double whitespaceRatio)
    {
        // Arrange
        var bench = new WhitespaceSplitBench
        {
            Length = length,
            WhitespaceRatio = whitespaceRatio
        };
        bench.Setup();

        // Act
        int result = bench.OriginalEnumerator();

        // Assert
        Assert.True(result >= 0, $"Result should be non-negative for Length={length}, WhitespaceRatio={whitespaceRatio}");
        Assert.True(result <= length, $"Result should not exceed input length for Length={length}, WhitespaceRatio={whitespaceRatio}");
    }

    /// <summary>
    /// Tests that OriginalEnumerator handles small strings correctly.
    /// Input: Length = 1
    /// Expected: Returns a valid non-negative result.
    /// </summary>
    [Fact]
    public void OriginalEnumerator_SingleCharacter_ReturnsValidResult()
    {
        // Arrange
        var bench = new WhitespaceSplitBench
        {
            Length = 1,
            WhitespaceRatio = 0.0
        };
        bench.Setup();

        // Act
        int result = bench.OriginalEnumerator();

        // Assert
        Assert.True(result >= 0, "Result should be non-negative");
        Assert.True(result <= 1, "Result should not exceed length of 1");
    }

    /// <summary>
    /// Tests that OriginalEnumerator handles large strings correctly.
    /// Input: Length = 100000
    /// Expected: Returns a valid non-negative result without throwing exceptions.
    /// </summary>
    [Fact]
    public void OriginalEnumerator_LargeString_ReturnsValidResult()
    {
        // Arrange
        var bench = new WhitespaceSplitBench
        {
            Length = 100000,
            WhitespaceRatio = 0.3
        };
        bench.Setup();

        // Act
        int result = bench.OriginalEnumerator();

        // Assert
        Assert.True(result >= 0, "Result should be non-negative for large strings");
        Assert.True(result <= 100000, "Result should not exceed input length");
    }

    /// <summary>
    /// Tests that OriginalEnumerator returns consistent results when called multiple times.
    /// Input: Length = 100, WhitespaceRatio = 0.5
    /// Expected: Returns the same result on each call (deterministic).
    /// </summary>
    [Fact]
    public void OriginalEnumerator_CalledMultipleTimes_ReturnsConsistentResults()
    {
        // Arrange
        var bench = new WhitespaceSplitBench
        {
            Length = 100,
            WhitespaceRatio = 0.5
        };
        bench.Setup();

        // Act
        int result1 = bench.OriginalEnumerator();
        int result2 = bench.OriginalEnumerator();
        int result3 = bench.OriginalEnumerator();

        // Assert
        Assert.Equal(result1, result2);
        Assert.Equal(result2, result3);
    }

    /// <summary>
    /// Tests that OriginalEnumerator handles boundary whitespace ratio values.
    /// Input: WhitespaceRatio at boundaries (0.0, 1.0)
    /// Expected: Returns valid results for extreme ratios.
    /// </summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void OriginalEnumerator_BoundaryWhitespaceRatios_ReturnsValidResult(double whitespaceRatio)
    {
        // Arrange
        var bench = new WhitespaceSplitBench
        {
            Length = 50,
            WhitespaceRatio = whitespaceRatio
        };
        bench.Setup();

        // Act
        int result = bench.OriginalEnumerator();

        // Assert
        Assert.True(result >= 0, $"Result should be non-negative for WhitespaceRatio={whitespaceRatio}");
        Assert.True(result <= 50, $"Result should not exceed length for WhitespaceRatio={whitespaceRatio}");
    }

    /// <summary>
    /// Tests that OptimizedEnumerator returns zero when Length is zero,
    /// resulting in an empty text string.
    /// </summary>
    [Fact]
    public void OptimizedEnumerator_LengthZero_ReturnsZero()
    {
        // Arrange
        var bench = new WhitespaceSplitBench
        {
            Length = 0,
            WhitespaceRatio = 0.10
        };
        bench.Setup();

        // Act
        int result = bench.OptimizedEnumerator();

        // Assert
        Assert.Equal(0, result);
    }

    /// <summary>
    /// Tests that OptimizedEnumerator returns a non-negative value
    /// with standard benchmark parameters.
    /// </summary>
    [Fact]
    public void OptimizedEnumerator_StandardParameters_ReturnsNonNegative()
    {
        // Arrange
        var bench = new WhitespaceSplitBench
        {
            Length = 1000,
            WhitespaceRatio = 0.10
        };
        bench.Setup();

        // Act
        int result = bench.OptimizedEnumerator();

        // Assert
        Assert.True(result >= 0);
        Assert.True(result <= bench.Length);
    }

    /// <summary>
    /// Tests that OptimizedEnumerator handles high whitespace ratio,
    /// where most of the text is whitespace characters.
    /// </summary>
    [Fact]
    public void OptimizedEnumerator_HighWhitespaceRatio_ReturnsReducedLength()
    {
        // Arrange
        var bench = new WhitespaceSplitBench
        {
            Length = 1000,
            WhitespaceRatio = 0.90
        };
        bench.Setup();

        // Act
        int result = bench.OptimizedEnumerator();

        // Assert
        Assert.True(result >= 0);
        Assert.True(result < bench.Length);
    }

    /// <summary>
    /// Tests that OptimizedEnumerator handles low whitespace ratio,
    /// where most of the text consists of non-whitespace tokens.
    /// </summary>
    [Fact]
    public void OptimizedEnumerator_LowWhitespaceRatio_ReturnsHigherLength()
    {
        // Arrange
        var bench = new WhitespaceSplitBench
        {
            Length = 1000,
            WhitespaceRatio = 0.01
        };
        bench.Setup();

        // Act
        int result = bench.OptimizedEnumerator();

        // Assert
        Assert.True(result >= 0);
        Assert.True(result <= bench.Length);
    }

    /// <summary>
    /// Tests that OptimizedEnumerator returns consistent results
    /// when called multiple times with the same setup.
    /// </summary>
    [Fact]
    public void OptimizedEnumerator_CalledMultipleTimes_ReturnsConsistentResults()
    {
        // Arrange
        var bench = new WhitespaceSplitBench
        {
            Length = 500,
            WhitespaceRatio = 0.25
        };
        bench.Setup();

        // Act
        int result1 = bench.OptimizedEnumerator();
        int result2 = bench.OptimizedEnumerator();
        int result3 = bench.OptimizedEnumerator();

        // Assert
        Assert.Equal(result1, result2);
        Assert.Equal(result2, result3);
    }

    /// <summary>
    /// Tests that OptimizedEnumerator handles very small Length values correctly.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void OptimizedEnumerator_SmallLength_ReturnsValidResult(int length)
    {
        // Arrange
        var bench = new WhitespaceSplitBench
        {
            Length = length,
            WhitespaceRatio = 0.10
        };
        bench.Setup();

        // Act
        int result = bench.OptimizedEnumerator();

        // Assert
        Assert.True(result >= 0);
        Assert.True(result <= length);
    }

    /// <summary>
    /// Tests that OptimizedEnumerator handles various whitespace ratios correctly.
    /// </summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(0.1)]
    [InlineData(0.5)]
    [InlineData(0.9)]
    [InlineData(1.0)]
    public void OptimizedEnumerator_VariousWhitespaceRatios_ReturnsValidResult(double ratio)
    {
        // Arrange
        var bench = new WhitespaceSplitBench
        {
            Length = 1000,
            WhitespaceRatio = ratio
        };
        bench.Setup();

        // Act
        int result = bench.OptimizedEnumerator();

        // Assert
        Assert.True(result >= 0);
        Assert.True(result <= bench.Length);
    }

    /// <summary>
    /// Tests that OptimizedEnumerator handles large Length values without issues.
    /// </summary>
    [Theory]
    [InlineData(10000)]
    [InlineData(100000)]
    public void OptimizedEnumerator_LargeLength_ReturnsValidResult(int length)
    {
        // Arrange
        var bench = new WhitespaceSplitBench
        {
            Length = length,
            WhitespaceRatio = 0.10
        };
        bench.Setup();

        // Act
        int result = bench.OptimizedEnumerator();

        // Assert
        Assert.True(result >= 0);
        Assert.True(result <= length);
    }

    /// <summary>
    /// Tests that OptimizedEnumerator produces deterministic results
    /// for the same Length and WhitespaceRatio parameters across different instances.
    /// </summary>
    [Fact]
    public void OptimizedEnumerator_SameParameters_ProducesDeterministicResults()
    {
        // Arrange
        var bench1 = new WhitespaceSplitBench
        {
            Length = 1000,
            WhitespaceRatio = 0.10
        };
        bench1.Setup();

        var bench2 = new WhitespaceSplitBench
        {
            Length = 1000,
            WhitespaceRatio = 0.10
        };
        bench2.Setup();

        // Act
        int result1 = bench1.OptimizedEnumerator();
        int result2 = bench2.OptimizedEnumerator();

        // Assert
        Assert.Equal(result1, result2);
    }

    /// <summary>
    /// Tests that Setup completes successfully with valid Length and WhitespaceRatio values.
    /// Verifies that the method executes without throwing exceptions for various boundary and normal input combinations.
    /// </summary>
    /// <param name="length">The Length field value to test.</param>
    /// <param name="whitespaceRatio">The WhitespaceRatio field value to test.</param>
    [Theory]
    [InlineData(0, 0.0)]
    [InlineData(1, 0.0)]
    [InlineData(1, 0.5)]
    [InlineData(10, 0.1)]
    [InlineData(100, 0.5)]
    [InlineData(1000, 0.1)]
    [InlineData(1000, 0.0)]
    [InlineData(1000, 1.0)]
    [InlineData(10000, 0.3)]
    [InlineData(1, 1.0)]
    [InlineData(50, -0.5)]
    [InlineData(50, 1.5)]
    [InlineData(50, 2.0)]
    public void Setup_ValidLengthAndWhitespaceRatio_CompletesSuccessfully(int length, double whitespaceRatio)
    {
        // Arrange
        var bench = new WhitespaceSplitBench
        {
            Length = length,
            WhitespaceRatio = whitespaceRatio
        };

        // Act & Assert
        var exception = Record.Exception(() => bench.Setup());
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that Setup completes successfully when WhitespaceRatio is NaN.
    /// Verifies that the method handles NaN gracefully without throwing exceptions.
    /// Expected behavior: NaN comparisons are always false, so only words are generated.
    /// </summary>
    [Fact]
    public void Setup_WhitespaceRatioIsNaN_CompletesSuccessfully()
    {
        // Arrange
        var bench = new WhitespaceSplitBench
        {
            Length = 100,
            WhitespaceRatio = double.NaN
        };

        // Act & Assert
        var exception = Record.Exception(() => bench.Setup());
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that Setup completes successfully when WhitespaceRatio is PositiveInfinity.
    /// Verifies that the method handles PositiveInfinity gracefully without throwing exceptions.
    /// Expected behavior: All whitespace characters are generated.
    /// </summary>
    [Fact]
    public void Setup_WhitespaceRatioIsPositiveInfinity_CompletesSuccessfully()
    {
        // Arrange
        var bench = new WhitespaceSplitBench
        {
            Length = 100,
            WhitespaceRatio = double.PositiveInfinity
        };

        // Act & Assert
        var exception = Record.Exception(() => bench.Setup());
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that Setup completes successfully when WhitespaceRatio is NegativeInfinity.
    /// Verifies that the method handles NegativeInfinity gracefully without throwing exceptions.
    /// Expected behavior: Only words are generated, no whitespace.
    /// </summary>
    [Fact]
    public void Setup_WhitespaceRatioIsNegativeInfinity_CompletesSuccessfully()
    {
        // Arrange
        var bench = new WhitespaceSplitBench
        {
            Length = 100,
            WhitespaceRatio = double.NegativeInfinity
        };

        // Act & Assert
        var exception = Record.Exception(() => bench.Setup());
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that Setup throws ArgumentOutOfRangeException when Length is negative.
    /// Verifies that invalid negative length values are properly rejected.
    /// Expected exception: ArgumentOutOfRangeException from StringBuilder constructor.
    /// </summary>
    /// <param name="length">The negative Length field value to test.</param>
    [Theory]
    [InlineData(-1)]
    [InlineData(-10)]
    [InlineData(-1000)]
    [InlineData(int.MinValue)]
    public void Setup_NegativeLength_ThrowsArgumentOutOfRangeException(int length)
    {
        // Arrange
        var bench = new WhitespaceSplitBench
        {
            Length = length,
            WhitespaceRatio = 0.1
        };

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => bench.Setup());
    }

    /// <summary>
    /// Tests that Setup handles maximum integer length value.
    /// Verifies behavior when Length is int.MaxValue, which may cause overflow or OutOfMemoryException.
    /// Expected exception: Either OverflowException or OutOfMemoryException due to StringBuilder capacity constraints.
    /// </summary>
    [Fact]
    public void Setup_MaximumLength_ThrowsException()
    {
        // Arrange
        var bench = new WhitespaceSplitBench
        {
            Length = int.MaxValue,
            WhitespaceRatio = 0.1
        };

        // Act & Assert
        // This will throw either OverflowException (from int.MaxValue + 64) or OutOfMemoryException
        var exception = Assert.ThrowsAny<Exception>(() => bench.Setup());
        Assert.True(
            exception is OverflowException || exception is OutOfMemoryException || exception is ArgumentOutOfRangeException,
            $"Expected OverflowException, OutOfMemoryException, or ArgumentOutOfRangeException, but got {exception.GetType().Name}");
    }

    /// <summary>
    /// Tests that Setup handles near-maximum integer length values.
    /// Verifies behavior when Length is close to int.MaxValue, which may cause overflow.
    /// Expected exception: OverflowException or OutOfMemoryException due to StringBuilder capacity calculation (length + 64).
    /// </summary>
    [Theory]
    [InlineData(int.MaxValue - 1)]
    [InlineData(int.MaxValue - 63)]
    public void Setup_NearMaximumLength_ThrowsException(int length)
    {
        // Arrange
        var bench = new WhitespaceSplitBench
        {
            Length = length,
            WhitespaceRatio = 0.1
        };

        // Act & Assert
        // This will throw either OverflowException or OutOfMemoryException
        var exception = Assert.ThrowsAny<Exception>(() => bench.Setup());
        Assert.True(
            exception is OverflowException || exception is OutOfMemoryException || exception is ArgumentOutOfRangeException,
            $"Expected OverflowException, OutOfMemoryException, or ArgumentOutOfRangeException, but got {exception.GetType().Name}");
    }
}
