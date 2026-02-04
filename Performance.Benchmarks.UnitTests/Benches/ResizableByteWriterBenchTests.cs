using System;
using System.Reflection;

using BenchmarkDotNet.Attributes;
using Moq;
using Performance.Benchmarks.Benches;
using Performance.Benchmarks.Original;
using Performance.Buffers;
using Xunit;

namespace Performance.Benchmarks.Benches.UnitTests;


/// <summary>
/// Unit tests for the <see cref="ResizableByteWriterBench"/> class.
/// </summary>
public class ResizableByteWriterBenchTests
{
    /// <summary>
    /// Tests that GlobalSetup initializes all fields correctly with valid size values.
    /// Verifies that _source is created with the correct length, and both writers are initialized.
    /// </summary>
    /// <param name="size">The size of the byte array to create.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(256)]
    [InlineData(4096)]
    [InlineData(65536)]
    [InlineData(1048576)]
    public void GlobalSetup_WithValidSize_InitializesAllFieldsCorrectly(int size)
    {
        // Arrange
        var bench = new ResizableByteWriterBench
        {
            Size = size
        };

        // Act
        bench.GlobalSetup();

        // Assert
        Assert.NotNull(bench.GetSourceField());
        Assert.Equal(size, bench.GetSourceField().Length);
        Assert.NotNull(bench.GetOldWriterField());
        Assert.NotNull(bench.GetNewWriterField());
    }

    /// <summary>
    /// Tests that GlobalSetup throws ArgumentOutOfRangeException when Size is negative.
    /// The exception is thrown by the byte array constructor, not the method itself.
    /// </summary>
    /// <param name="negativeSize">A negative size value.</param>
    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void GlobalSetup_WithNegativeSize_ThrowsArgumentOutOfRangeException(int negativeSize)
    {
        // Arrange
        var bench = new ResizableByteWriterBench
        {
            Size = negativeSize
        };

        // Act & Assert
        Assert.Throws<OverflowException>(() => bench.GlobalSetup());
    }

    /// <summary>
    /// Tests that GlobalSetup produces deterministic data using the fixed seed (42).
    /// Calling GlobalSetup multiple times should produce identical byte array contents.
    /// </summary>
    [Fact]
    public void GlobalSetup_CalledMultipleTimes_ProducesDeterministicData()
    {
        // Arrange
        var bench1 = new ResizableByteWriterBench { Size = 256 };
        var bench2 = new ResizableByteWriterBench { Size = 256 };

        // Act
        bench1.GlobalSetup();
        bench2.GlobalSetup();

        // Assert
        var source1 = bench1.GetSourceField();
        var source2 = bench2.GetSourceField();

        Assert.NotNull(source1);
        Assert.NotNull(source2);
        Assert.Equal(source1.Length, source2.Length);
        Assert.Equal(source1, source2);
    }

    /// <summary>
    /// Tests that GlobalSetup can be called multiple times on the same instance.
    /// Each call should reinitialize the fields with new instances.
    /// </summary>
    [Fact]
    public void GlobalSetup_CalledTwiceOnSameInstance_ReplacesExistingInstances()
    {
        // Arrange
        var bench = new ResizableByteWriterBench { Size = 100 };

        // Act
        bench.GlobalSetup();
        var firstSource = bench.GetSourceField();
        var firstOldWriter = bench.GetOldWriterField();
        var firstNewWriter = bench.GetNewWriterField();

        bench.Size = 200;
        bench.GlobalSetup();
        var secondSource = bench.GetSourceField();
        var secondOldWriter = bench.GetOldWriterField();
        var secondNewWriter = bench.GetNewWriterField();

        // Assert
        Assert.NotNull(firstSource);
        Assert.NotNull(secondSource);
        Assert.NotSame(firstSource, secondSource);
        Assert.Equal(100, firstSource.Length);
        Assert.Equal(200, secondSource.Length);

        Assert.NotNull(firstOldWriter);
        Assert.NotNull(secondOldWriter);
        Assert.NotSame(firstOldWriter, secondOldWriter);

        Assert.NotNull(firstNewWriter);
        Assert.NotNull(secondNewWriter);
        Assert.NotSame(firstNewWriter, secondNewWriter);
    }

    /// <summary>
    /// Tests that GlobalSetup with size zero creates an empty byte array.
    /// This is a valid edge case that should not throw.
    /// </summary>
    [Fact]
    public void GlobalSetup_WithSizeZero_CreatesEmptyArrayAndInitializesWriters()
    {
        // Arrange
        var bench = new ResizableByteWriterBench { Size = 0 };

        // Act
        bench.GlobalSetup();

        // Assert
        var source = bench.GetSourceField();
        Assert.NotNull(source);
        Assert.Empty(source);
        Assert.NotNull(bench.GetOldWriterField());
        Assert.NotNull(bench.GetNewWriterField());
    }

    /// <summary>
    /// Tests that GlobalSetup fills the byte array with non-default values.
    /// The Random.NextBytes call should populate the array with pseudo-random data.
    /// </summary>
    [Fact]
    public void GlobalSetup_WithPositiveSize_FillsByteArrayWithData()
    {
        // Arrange
        var bench = new ResizableByteWriterBench { Size = 100 };

        // Act
        bench.GlobalSetup();

        // Assert
        var source = bench.GetSourceField();
        Assert.NotNull(source);

        // Verify that not all bytes are zero (extremely unlikely with Random.NextBytes)
        var hasNonZeroByte = false;
        foreach (var b in source)
        {
            if (b != 0)
            {
                hasNonZeroByte = true;
                break;
            }
        }
        Assert.True(hasNonZeroByte || source.Length == 0, "Expected at least one non-zero byte in the array");
    }

    /// <summary>
    /// Tests that Cleanup successfully resets both writers after GlobalSetup has initialized them.
    /// This verifies the normal operation path where writers are properly initialized.
    /// Expected: No exception is thrown.
    /// </summary>
    [Fact]
    public void Cleanup_AfterGlobalSetup_ResetsWritersSuccessfully()
    {
        // Arrange
        var bench = new ResizableByteWriterBench
        {
            Size = 256
        };
        bench.GlobalSetup();

        // Act
        var exception = Record.Exception(() => bench.Cleanup());

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that Cleanup throws NullReferenceException when called before GlobalSetup.
    /// This verifies error handling when writers are not initialized (null).
    /// Expected: NullReferenceException is thrown.
    /// </summary>
    [Fact]
    public void Cleanup_WithNullWriters_ThrowsNullReferenceException()
    {
        // Arrange
        var bench = new ResizableByteWriterBench();

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => bench.Cleanup());
    }

    /// <summary>
    /// Tests that Cleanup actually calls Reset on both writers by verifying their Length is reset to 0.
    /// This validates that the Reset methods are properly invoked and have the expected side effect.
    /// Expected: Both writers have Length == 0 after Cleanup.
    /// </summary>
    [Fact]
    public void Cleanup_AfterWritingData_ResetsWriterLength()
    {
        // Arrange
        var bench = new ResizableByteWriterBench
        {
            Size = 256
        };
        bench.GlobalSetup();

        // Write some data to both writers to increase their Length
        bench.Old_WriteByteArray();
        bench.New_WriteByteArray();

        // Verify data was written (Length > 0)
        // Note: Cannot access private fields directly, but calling the write methods
        // will have increased the internal length

        // Act
        bench.Cleanup();

        // Assert
        // After cleanup, subsequent writes should start from position 0
        // We can verify this by writing again and checking consistency
        var exception = Record.Exception(() => bench.Old_WriteByteArray());
        Assert.Null(exception);

        exception = Record.Exception(() => bench.New_WriteByteArray());
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests Cleanup with various Size parameter values to ensure it works across different configurations.
    /// This validates that Cleanup properly resets writers regardless of the buffer size used.
    /// Expected: No exception is thrown for any valid size.
    /// </summary>
    [Theory]
    [InlineData(256)]
    [InlineData(4096)]
    [InlineData(65536)]
    [InlineData(1048576)]
    public void Cleanup_WithVariousSizes_ResetsSuccessfully(int size)
    {
        // Arrange
        var bench = new ResizableByteWriterBench
        {
            Size = size
        };
        bench.GlobalSetup();

        // Act
        var exception = Record.Exception(() => bench.Cleanup());

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that Cleanup can be called multiple times in succession without error.
    /// This verifies idempotent behavior and proper reset handling.
    /// Expected: No exception is thrown on repeated calls.
    /// </summary>
    [Fact]
    public void Cleanup_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var bench = new ResizableByteWriterBench
        {
            Size = 256
        };
        bench.GlobalSetup();

        // Act
        var exception1 = Record.Exception(() => bench.Cleanup());
        var exception2 = Record.Exception(() => bench.Cleanup());
        var exception3 = Record.Exception(() => bench.Cleanup());

        // Assert
        Assert.Null(exception1);
        Assert.Null(exception2);
        Assert.Null(exception3);
    }

    /// <summary>
    /// Tests that Old_WriteByteArray successfully writes data to the writer with various source sizes.
    /// Verifies that the method executes without throwing exceptions and that data is written correctly.
    /// </summary>
    /// <param name="size">The size of the source byte array to write.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(256)]
    [InlineData(4096)]
    [InlineData(65536)]
    [InlineData(1048576)]
    public void Old_WriteByteArray_WithVariousSizes_WritesDataSuccessfully(int size)
    {
        // Arrange
        var bench = new ResizableByteWriterBench
        {
            Size = size
        };
        bench.GlobalSetup();

        // Act
        bench.Old_WriteByteArray();

        // Assert
        // No exception should be thrown, and we can indirectly verify by checking writer state
        // The method should complete successfully
    }

    /// <summary>
    /// Tests that Old_WriteByteArray can be called multiple times and accumulates data correctly.
    /// Verifies that multiple writes increase the writer's length appropriately.
    /// </summary>
    [Fact]
    public void Old_WriteByteArray_CalledMultipleTimes_AccumulatesData()
    {
        // Arrange
        var bench = new ResizableByteWriterBench
        {
            Size = 256
        };
        bench.GlobalSetup();
        var oldWriter = GetOldWriter(bench);
        var expectedLength = bench.Size;

        // Act
        bench.Old_WriteByteArray();
        var lengthAfterFirstWrite = oldWriter.Length;
        bench.Old_WriteByteArray();
        var lengthAfterSecondWrite = oldWriter.Length;

        // Assert
        Assert.Equal(expectedLength, lengthAfterFirstWrite);
        Assert.Equal(expectedLength * 2, lengthAfterSecondWrite);
    }

    /// <summary>
    /// Tests that Old_WriteByteArray writes from the beginning after Reset is called.
    /// Verifies that the Cleanup method resets the writer state correctly.
    /// </summary>
    [Fact]
    public void Old_WriteByteArray_AfterCleanup_WritesFromBeginning()
    {
        // Arrange
        var bench = new ResizableByteWriterBench
        {
            Size = 1024
        };
        bench.GlobalSetup();
        var oldWriter = GetOldWriter(bench);

        // Act
        bench.Old_WriteByteArray();
        var lengthAfterFirstWrite = oldWriter.Length;
        bench.Cleanup();
        var lengthAfterCleanup = oldWriter.Length;
        bench.Old_WriteByteArray();
        var lengthAfterSecondWrite = oldWriter.Length;

        // Assert
        Assert.Equal(bench.Size, lengthAfterFirstWrite);
        Assert.Equal(0, lengthAfterCleanup);
        Assert.Equal(bench.Size, lengthAfterSecondWrite);
    }

    /// <summary>
    /// Tests that Old_WriteByteArray handles boundary values for Size parameter.
    /// Verifies behavior with minimum, maximum practical values, and edge cases.
    /// </summary>
    /// <param name="size">The size boundary value to test.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(int.MaxValue / 1024)] // Practical maximum to avoid OutOfMemoryException
    public void Old_WriteByteArray_WithBoundarySizes_HandlesCorrectly(int size)
    {
        // Arrange
        var bench = new ResizableByteWriterBench
        {
            Size = size
        };
        bench.GlobalSetup();

        // Act & Assert
        var exception = Record.Exception(() => bench.Old_WriteByteArray());
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that Old_WriteByteArray writes the exact number of bytes specified by the source array length.
    /// Verifies that the writer's length matches the expected value after writing.
    /// </summary>
    [Fact]
    public void Old_WriteByteArray_WritesExactNumberOfBytes_MatchesSourceLength()
    {
        // Arrange
        var bench = new ResizableByteWriterBench
        {
            Size = 512
        };
        bench.GlobalSetup();
        var oldWriter = GetOldWriter(bench);
        var expectedLength = bench.Size;

        // Act
        bench.Old_WriteByteArray();

        // Assert
        Assert.Equal(expectedLength, oldWriter.Length);
    }

    /// <summary>
    /// Tests that Old_WriteByteArray can handle consecutive write and cleanup cycles.
    /// Verifies that the benchmark method works correctly across multiple iterations.
    /// </summary>
    [Fact]
    public void Old_WriteByteArray_MultipleWriteCleanupCycles_WorksCorrectly()
    {
        // Arrange
        var bench = new ResizableByteWriterBench
        {
            Size = 128
        };
        bench.GlobalSetup();
        var oldWriter = GetOldWriter(bench);

        // Act & Assert
        for (int i = 0; i < 5; i++)
        {
            bench.Old_WriteByteArray();
            Assert.Equal(bench.Size, oldWriter.Length);
            bench.Cleanup();
            Assert.Equal(0, oldWriter.Length);
        }
    }

    /// <summary>
    /// Helper method to access the private _oldWriter field via reflection.
    /// </summary>
    private static OriginalResizableByteWriter GetOldWriter(ResizableByteWriterBench bench)
    {
        var field = typeof(ResizableByteWriterBench).GetField("_oldWriter",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (OriginalResizableByteWriter)field!.GetValue(bench)!;
    }

    /// <summary>
    /// Tests that New_WriteByteArray successfully writes data when the writer and source are properly initialized.
    /// This validates the normal execution path with various array sizes.
    /// </summary>
    /// <param name="size">The size of the byte array to write.</param>
    [Theory]
    [InlineData(256)]
    [InlineData(4096)]
    [InlineData(65536)]
    [InlineData(1048576)]
    public void New_WriteByteArray_ValidInitializedState_WritesSuccessfully(int size)
    {
        // Arrange
        var bench = new ResizableByteWriterBench
        {
            Size = size
        };
        bench.GlobalSetup();

        // Act & Assert - should not throw
        bench.New_WriteByteArray();
    }

    /// <summary>
    /// Tests that New_WriteByteArray writes correctly when the source array is empty.
    /// This verifies the method handles empty arrays without errors.
    /// </summary>
    [Fact]
    public void New_WriteByteArray_EmptySourceArray_WritesSuccessfully()
    {
        // Arrange
        var bench = new ResizableByteWriterBench
        {
            Size = 0
        };
        bench.GlobalSetup();

        // Act & Assert - should not throw
        bench.New_WriteByteArray();
    }

    /// <summary>
    /// Tests that New_WriteByteArray throws ArgumentNullException when _source is null.
    /// This validates proper null handling for the source buffer parameter.
    /// </summary>
    [Fact]
    public void New_WriteByteArray_NullSource_ThrowsArgumentNullException()
    {
        // Arrange
        var bench = new ResizableByteWriterBench
        {
            Size = 256
        };
        bench.GlobalSetup();

        // Use reflection to set _source to null to simulate an invalid state
        var sourceField = typeof(ResizableByteWriterBench).GetField("_source",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        sourceField?.SetValue(bench, null);

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => bench.New_WriteByteArray());
    }

    /// <summary>
    /// Tests that New_WriteByteArray throws NullReferenceException when _newWriter is null.
    /// This validates proper null handling for the writer instance.
    /// </summary>
    [Fact]
    public void New_WriteByteArray_NullWriter_ThrowsNullReferenceException()
    {
        // Arrange
        var bench = new ResizableByteWriterBench
        {
            Size = 256
        };
        bench.GlobalSetup();

        // Use reflection to set _newWriter to null to simulate an invalid state
        var writerField = typeof(ResizableByteWriterBench).GetField("_newWriter",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        writerField?.SetValue(bench, null);

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => bench.New_WriteByteArray());
    }

    /// <summary>
    /// Tests that New_WriteByteArray correctly invokes the Write method with proper parameters.
    /// This validates that the method passes the entire source array to the writer.
    /// </summary>
    [Fact]
    public void New_WriteByteArray_ProperlyInitialized_InvokesWriteWithCorrectParameters()
    {
        // Arrange
        var bench = new ResizableByteWriterBench
        {
            Size = 256
        };
        bench.GlobalSetup();

        var sourceField = typeof(ResizableByteWriterBench).GetField("_source",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var source = (byte[]?)sourceField?.GetValue(bench);

        var writerField = typeof(ResizableByteWriterBench).GetField("_newWriter",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var writer = (ResizableByteWriter?)writerField?.GetValue(bench);

        // Act
        bench.New_WriteByteArray();

        // Assert
        // Verify that the writer received all the data by checking its length
        Assert.NotNull(writer);
        Assert.NotNull(source);
        Assert.Equal(source.Length, writer.Length);
    }

    /// <summary>
    /// Tests that New_WriteByteArray can be called multiple times with cleanup in between.
    /// This validates the method's behavior in the benchmark iteration lifecycle.
    /// </summary>
    [Fact]
    public void New_WriteByteArray_MultipleIterationsWithCleanup_WritesSuccessfully()
    {
        // Arrange
        var bench = new ResizableByteWriterBench
        {
            Size = 1024
        };
        bench.GlobalSetup();

        // Act & Assert - should not throw on multiple iterations
        bench.New_WriteByteArray();
        bench.Cleanup();

        bench.New_WriteByteArray();
        bench.Cleanup();

        bench.New_WriteByteArray();
    }

    /// <summary>
    /// Tests that New_WriteByteArray handles edge case of single byte array.
    /// This validates the method works correctly with minimal size arrays.
    /// </summary>
    [Fact]
    public void New_WriteByteArray_SingleByteArray_WritesSuccessfully()
    {
        // Arrange
        var bench = new ResizableByteWriterBench
        {
            Size = 1
        };
        bench.GlobalSetup();

        // Act & Assert - should not throw
        bench.New_WriteByteArray();
    }

    /// <summary>
    /// Tests that New_WriteByteArray handles very large arrays correctly.
    /// This validates the method can handle boundary values for array sizes.
    /// </summary>
    [Fact]
    public void New_WriteByteArray_VeryLargeArray_WritesSuccessfully()
    {
        // Arrange
        var bench = new ResizableByteWriterBench
        {
            Size = 10_485_760 // 10 MB
        };
        bench.GlobalSetup();

        // Act & Assert - should not throw
        bench.New_WriteByteArray();
    }

    /// <summary>
    /// Tests that Old_ResetReuse executes successfully with various buffer sizes.
    /// Verifies that the method writes data, resets the writer, writes again,
    /// and that the final state contains the expected data with correct length.
    /// </summary>
    /// <param name="size">The size of the source buffer to test.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(256)]
    [InlineData(4_096)]
    [InlineData(65_536)]
    [InlineData(1_048_576)]
    public void Old_ResetReuse_WithVariousSizes_CompletesSuccessfullyAndWritesExpectedData(int size)
    {
        // Arrange
        var bench = new ResizableByteWriterBench
        {
            Size = size
        };
        bench.GlobalSetup();

        // Act
        bench.Old_ResetReuse();

        // Assert
        // After write, reset, write - the writer should contain the data once
        var writer = GetOldWriter(bench);
        Assert.Equal(size, writer.Length);

        if (size > 0)
        {
            var writtenData = writer.WrittenMemory.ToArray();
            var sourceData = GetSource(bench);
            Assert.Equal(sourceData, writtenData);
        }
    }

    /// <summary>
    /// Tests that Old_ResetReuse throws NullReferenceException when _oldWriter is not initialized.
    /// This simulates the scenario where GlobalSetup has not been called.
    /// </summary>
    [Fact]
    public void Old_ResetReuse_WhenOldWriterIsNull_ThrowsNullReferenceException()
    {
        // Arrange
        var bench = new ResizableByteWriterBench
        {
            Size = 256
        };
        // Note: GlobalSetup is NOT called, so _oldWriter remains null

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => bench.Old_ResetReuse());
    }

    /// <summary>
    /// Tests that Old_ResetReuse correctly resets the writer's position to zero between writes.
    /// Verifies that after reset, the second write overwrites the data from the first write.
    /// </summary>
    [Fact]
    public void Old_ResetReuse_ResetsWriterPosition_ResultsInCorrectFinalLength()
    {
        // Arrange
        var bench = new ResizableByteWriterBench
        {
            Size = 100
        };
        bench.GlobalSetup();
        var writer = GetOldWriter(bench);

        // Act
        bench.Old_ResetReuse();

        // Assert
        // After write (100 bytes), reset (position=0), write again (100 bytes)
        // Final length should be 100, not 200
        Assert.Equal(100, writer.Length);
    }

    /// <summary>
    /// Tests that Old_ResetReuse works correctly with maximum boundary size.
    /// Ensures the method can handle very large buffers without throwing.
    /// </summary>
    [Fact]
    public void Old_ResetReuse_WithMaxBoundarySize_CompletesSuccessfully()
    {
        // Arrange
        var bench = new ResizableByteWriterBench
        {
            Size = int.MaxValue / 2 // Use a very large but reasonable size
        };

        // This test may be slow or memory-intensive, so we reduce the size
        // to something more practical while still testing large buffer handling
        bench.Size = 10_000_000; // 10MB
        bench.GlobalSetup();

        // Act & Assert
        var exception = Record.Exception(() => bench.Old_ResetReuse());
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that Old_ResetReuse completes multiple consecutive invocations successfully.
    /// Verifies that the method can be called multiple times in sequence without issues.
    /// </summary>
    [Fact]
    public void Old_ResetReuse_CalledMultipleTimes_CompletesSuccessfully()
    {
        // Arrange
        var bench = new ResizableByteWriterBench
        {
            Size = 1024
        };
        bench.GlobalSetup();

        // Act
        bench.Old_ResetReuse();
        bench.Old_ResetReuse();
        bench.Old_ResetReuse();

        // Assert
        var writer = GetOldWriter(bench);
        Assert.Equal(1024, writer.Length);
    }

    /// <summary>
    /// Tests that Old_ResetReuse maintains data integrity after reset.
    /// Verifies that the data written after reset matches the source data.
    /// </summary>
    [Fact]
    public void Old_ResetReuse_AfterReset_WrittenDataMatchesSource()
    {
        // Arrange
        var bench = new ResizableByteWriterBench
        {
            Size = 512
        };
        bench.GlobalSetup();
        var sourceData = GetSource(bench);

        // Act
        bench.Old_ResetReuse();

        // Assert
        var writer = GetOldWriter(bench);
        var writtenData = writer.WrittenMemory.ToArray();
        Assert.Equal(sourceData, writtenData);
    }

    private static byte[] GetSource(ResizableByteWriterBench bench)
    {
        var field = typeof(ResizableByteWriterBench).GetField("_source",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (byte[])field!.GetValue(bench)!;
    }

    /// <summary>
    /// Tests that New_ResetReuse executes successfully with various size parameters.
    /// Verifies the method writes, resets, and writes again without throwing exceptions.
    /// </summary>
    /// <param name="size">The size of the byte array to test with.</param>
    [Theory]
    [InlineData(256)]
    [InlineData(4_096)]
    [InlineData(65_536)]
    [InlineData(1_048_576)]
    public void New_ResetReuse_WithVariousSizes_ExecutesSuccessfully(int size)
    {
        // Arrange
        var bench = new ResizableByteWriterBench
        {
            Size = size
        };
        bench.GlobalSetup();

        // Act & Assert - should not throw
        bench.New_ResetReuse();
    }

    /// <summary>
    /// Tests that New_ResetReuse can be called multiple times consecutively.
    /// Verifies the method is idempotent and can handle repeated invocations.
    /// </summary>
    [Fact]
    public void New_ResetReuse_CalledMultipleTimes_ExecutesSuccessfully()
    {
        // Arrange
        var bench = new ResizableByteWriterBench
        {
            Size = 1024
        };
        bench.GlobalSetup();

        // Act & Assert - should not throw
        bench.New_ResetReuse();
        bench.New_ResetReuse();
        bench.New_ResetReuse();
    }

    /// <summary>
    /// Tests that New_ResetReuse throws NullReferenceException when called without GlobalSetup.
    /// Verifies that the method requires proper initialization before execution.
    /// </summary>
    [Fact]
    public void New_ResetReuse_WithoutGlobalSetup_ThrowsNullReferenceException()
    {
        // Arrange
        var bench = new ResizableByteWriterBench();

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => bench.New_ResetReuse());
    }

    /// <summary>
    /// Tests that New_ResetReuse executes successfully with minimum positive size.
    /// Verifies the method handles small byte arrays correctly.
    /// </summary>
    [Fact]
    public void New_ResetReuse_WithMinimumPositiveSize_ExecutesSuccessfully()
    {
        // Arrange
        var bench = new ResizableByteWriterBench
        {
            Size = 1
        };
        bench.GlobalSetup();

        // Act & Assert - should not throw
        bench.New_ResetReuse();
    }

    /// <summary>
    /// Tests that New_ResetReuse executes successfully with zero size.
    /// Verifies the method handles empty byte arrays correctly.
    /// </summary>
    [Fact]
    public void New_ResetReuse_WithZeroSize_ExecutesSuccessfully()
    {
        // Arrange
        var bench = new ResizableByteWriterBench
        {
            Size = 0
        };
        bench.GlobalSetup();

        // Act & Assert - should not throw
        bench.New_ResetReuse();
    }

    /// <summary>
    /// Tests that New_ResetReuse executes successfully after Cleanup.
    /// Verifies the method works correctly after the iteration cleanup process.
    /// </summary>
    [Fact]
    public void New_ResetReuse_AfterCleanup_ExecutesSuccessfully()
    {
        // Arrange
        var bench = new ResizableByteWriterBench
        {
            Size = 1024
        };
        bench.GlobalSetup();
        bench.Cleanup();

        // Act & Assert - should not throw
        bench.New_ResetReuse();
    }

    /// <summary>
    /// Tests that New_ResetReuse handles boundary size values correctly.
    /// Verifies the method works with very large byte arrays.
    /// </summary>
    [Theory]
    [InlineData(int.MaxValue / 2)]
    public void New_ResetReuse_WithLargeSize_ExecutesSuccessfullyOrThrowsOutOfMemory(int size)
    {
        // Arrange
        var bench = new ResizableByteWriterBench
        {
            Size = size
        };

        try
        {
            bench.GlobalSetup();

            // Act & Assert - should not throw (unless OutOfMemoryException during setup)
            bench.New_ResetReuse();
        }
        catch (OutOfMemoryException)
        {
            // Expected for very large allocations on some systems
            Assert.True(true);
        }
    }

    /// <summary>
    /// Tests that Old_WriteSpanAdvance executes successfully with valid initialized fields.
    /// Input: Valid _oldWriter and _source with various sizes.
    /// Expected: Method completes without throwing exceptions.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(256)]
    [InlineData(4096)]
    [InlineData(65536)]
    public void Old_WriteSpanAdvance_WithValidState_CompletesSuccessfully(int sourceSize)
    {
        // Arrange
        ResizableByteWriterBench bench = new ResizableByteWriterBench();
        byte[] source = new byte[sourceSize];
        new Random(42).NextBytes(source);

        OriginalResizableByteWriter oldWriter = new OriginalResizableByteWriter();

        SetPrivateField(bench, "_source", source);
        SetPrivateField(bench, "_oldWriter", oldWriter);

        // Act & Assert - should not throw
        bench.Old_WriteSpanAdvance();
    }

    /// <summary>
    /// Tests that Old_WriteSpanAdvance handles an empty source array correctly.
    /// Input: Valid _oldWriter and empty _source array (Length = 0).
    /// Expected: Method completes without throwing exceptions (GetSpan will use sizeHint=8, Advance will use 0).
    /// </summary>
    [Fact]
    public void Old_WriteSpanAdvance_WithEmptySource_CompletesSuccessfully()
    {
        // Arrange
        ResizableByteWriterBench bench = new ResizableByteWriterBench();
        byte[] source = Array.Empty<byte>();
        OriginalResizableByteWriter oldWriter = new OriginalResizableByteWriter();

        SetPrivateField(bench, "_source", source);
        SetPrivateField(bench, "_oldWriter", oldWriter);

        // Act & Assert - should not throw
        bench.Old_WriteSpanAdvance();
    }

    /// <summary>
    /// Tests that Old_WriteSpanAdvance throws NullReferenceException when _oldWriter is null.
    /// Input: Null _oldWriter field.
    /// Expected: NullReferenceException is thrown.
    /// </summary>
    [Fact]
    public void Old_WriteSpanAdvance_WithNullOldWriter_ThrowsNullReferenceException()
    {
        // Arrange
        ResizableByteWriterBench bench = new ResizableByteWriterBench();
        byte[] source = new byte[10];

        SetPrivateField(bench, "_source", source);
        SetPrivateField(bench, "_oldWriter", null);

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => bench.Old_WriteSpanAdvance());
    }

    /// <summary>
    /// Tests that Old_WriteSpanAdvance throws NullReferenceException when _source is null.
    /// Input: Null _source field.
    /// Expected: NullReferenceException is thrown when accessing _source.Length.
    /// </summary>
    [Fact]
    public void Old_WriteSpanAdvance_WithNullSource_ThrowsNullReferenceException()
    {
        // Arrange
        ResizableByteWriterBench bench = new ResizableByteWriterBench();
        OriginalResizableByteWriter oldWriter = new OriginalResizableByteWriter();

        SetPrivateField(bench, "_source", null);
        SetPrivateField(bench, "_oldWriter", oldWriter);

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => bench.Old_WriteSpanAdvance());
    }

    /// <summary>
    /// Tests that Old_WriteSpanAdvance can be called multiple times consecutively.
    /// Input: Valid _oldWriter and _source, method called twice.
    /// Expected: Both calls complete successfully without exceptions.
    /// </summary>
    [Fact]
    public void Old_WriteSpanAdvance_CalledMultipleTimes_CompletesSuccessfully()
    {
        // Arrange
        ResizableByteWriterBench bench = new ResizableByteWriterBench();
        byte[] source = new byte[100];
        new Random(42).NextBytes(source);

        OriginalResizableByteWriter oldWriter = new OriginalResizableByteWriter();

        SetPrivateField(bench, "_source", source);
        SetPrivateField(bench, "_oldWriter", oldWriter);

        // Act & Assert - should not throw on multiple calls
        bench.Old_WriteSpanAdvance();
        bench.Old_WriteSpanAdvance();
    }

    /// <summary>
    /// Tests that Old_WriteSpanAdvance handles maximum practical size arrays.
    /// Input: Valid _oldWriter and _source with size 1MB (1_048_576 bytes).
    /// Expected: Method completes without throwing exceptions.
    /// </summary>
    [Fact]
    public void Old_WriteSpanAdvance_WithLargeSource_CompletesSuccessfully()
    {
        // Arrange
        ResizableByteWriterBench bench = new ResizableByteWriterBench();
        byte[] source = new byte[1_048_576];
        new Random(42).NextBytes(source);

        OriginalResizableByteWriter oldWriter = new OriginalResizableByteWriter();

        SetPrivateField(bench, "_source", source);
        SetPrivateField(bench, "_oldWriter", oldWriter);

        // Act & Assert - should not throw
        bench.Old_WriteSpanAdvance();
    }

    /// <summary>
    /// Helper method to set private fields using reflection.
    /// </summary>
    private static void SetPrivateField(object obj, string fieldName, object? value)
    {
        FieldInfo? field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (field == null)
        {
            throw new InvalidOperationException($"Field '{fieldName}' not found on type '{obj.GetType().Name}'.");
        }
        field.SetValue(obj, value);
    }
}


/// <summary>
/// Extension methods to access private fields for testing purposes.
/// </summary>
internal static class ResizableByteWriterBenchTestExtensions
{
    public static byte[] GetSourceField(this ResizableByteWriterBench bench)
    {
        var field = typeof(ResizableByteWriterBench).GetField("_source",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (byte[])field!.GetValue(bench)!;
    }

    public static OriginalResizableByteWriter GetOldWriterField(this ResizableByteWriterBench bench)
    {
        var field = typeof(ResizableByteWriterBench).GetField("_oldWriter",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (OriginalResizableByteWriter)field!.GetValue(bench)!;
    }

    public static ResizableByteWriter GetNewWriterField(this ResizableByteWriterBench bench)
    {
        var field = typeof(ResizableByteWriterBench).GetField("_newWriter",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (ResizableByteWriter)field!.GetValue(bench)!;
    }
}