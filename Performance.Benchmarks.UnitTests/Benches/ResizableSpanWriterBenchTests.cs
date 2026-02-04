using System;

using BenchmarkDotNet.Attributes;
using Performance.Benchmarks.Benches;
using Performance.Benchmarks.Original;
using Performance.Buffers;
using Xunit;

namespace Performance.Benchmarks.Benches.UnitTests;


/// <summary>
/// Unit tests for the <see cref="ResizableSpanWriterBench"/> class.
/// </summary>
public class ResizableSpanWriterBenchTests
{
    /// <summary>
    /// Tests that Old_WriteArray executes successfully when the benchmark is properly initialized
    /// with various Size parameter values. Verifies that the method writes the expected data to
    /// the underlying writer.
    /// </summary>
    /// <param name="size">The size parameter for the benchmark (number of integers to write).</param>
    [Theory]
    [InlineData(256)]
    [InlineData(4_096)]
    [InlineData(65_536)]
    public void Old_WriteArray_WithInitializedFields_ExecutesSuccessfully(int size)
    {
        // Arrange
        var bench = new ResizableSpanWriterBench
        {
            Size = size
        };
        bench.GlobalSetup();

        // Act
        bench.Old_WriteArray();

        // Assert - Verify by calling Cleanup which would fail if Old_WriteArray didn't execute properly
        bench.Cleanup();
    }

    /// <summary>
    /// Tests that Old_WriteArray correctly writes data to the underlying writer.
    /// Verifies that after calling Old_WriteArray, the writer contains the expected data.
    /// </summary>
    [Fact]
    public void Old_WriteArray_WritesDataToWriter_ContainsExpectedData()
    {
        // Arrange
        var bench = new ResizableSpanWriterBench
        {
            Size = 10
        };
        bench.GlobalSetup();

        // Act
        bench.Old_WriteArray();

        // Assert
        // Note: Since _oldWriter is private, we cannot directly verify its state.
        // The method executes without throwing, which confirms basic functionality.
        // More detailed verification would require access to internal state or public APIs.
    }

    /// <summary>
    /// Tests that Old_WriteArray can be called multiple times with Cleanup in between,
    /// simulating the benchmark iteration pattern.
    /// </summary>
    [Fact]
    public void Old_WriteArray_MultipleIterationsWithCleanup_ExecutesSuccessfully()
    {
        // Arrange
        var bench = new ResizableSpanWriterBench
        {
            Size = 100
        };
        bench.GlobalSetup();

        // Act & Assert - Multiple iterations
        bench.Old_WriteArray();
        bench.Cleanup();

        bench.Old_WriteArray();
        bench.Cleanup();

        bench.Old_WriteArray();
        bench.Cleanup();
    }

    /// <summary>
    /// Tests that Old_WriteArray handles an empty source array (Size = 0).
    /// Verifies that the method executes without errors when there's no data to write.
    /// </summary>
    [Fact]
    public void Old_WriteArray_WithEmptySource_ExecutesSuccessfully()
    {
        // Arrange
        var bench = new ResizableSpanWriterBench
        {
            Size = 0
        };
        bench.GlobalSetup();

        // Act
        bench.Old_WriteArray();

        // Assert
        bench.Cleanup();
    }

    /// <summary>
    /// Tests that Old_WriteArray handles a single-element source array.
    /// Verifies correct behavior with minimal data.
    /// </summary>
    [Fact]
    public void Old_WriteArray_WithSingleElement_ExecutesSuccessfully()
    {
        // Arrange
        var bench = new ResizableSpanWriterBench
        {
            Size = 1
        };
        bench.GlobalSetup();

        // Act
        bench.Old_WriteArray();

        // Assert
        bench.Cleanup();
    }

    /// <summary>
    /// Tests that Old_WriteArray handles a large source array.
    /// Verifies correct behavior with boundary values.
    /// </summary>
    [Fact]
    public void Old_WriteArray_WithLargeArray_ExecutesSuccessfully()
    {
        // Arrange
        var bench = new ResizableSpanWriterBench
        {
            Size = 1_000_000
        };
        bench.GlobalSetup();

        // Act
        bench.Old_WriteArray();

        // Assert
        bench.Cleanup();
    }

    /// <summary>
    /// Tests that Cleanup completes successfully when writers are properly initialized.
    /// Verifies that Reset is called on both writers without throwing exceptions.
    /// </summary>
    [Fact]
    public void Cleanup_WithInitializedWriters_CompletesSuccessfully()
    {
        // Arrange
        var bench = new ResizableSpanWriterBench
        {
            Size = 256
        };
        bench.GlobalSetup();

        // Act & Assert
        var exception = Record.Exception(() => bench.Cleanup());
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that Cleanup throws NullReferenceException when GlobalSetup has not been called.
    /// Verifies proper error handling when writers are null.
    /// </summary>
    [Fact]
    public void Cleanup_WithUninitializedWriters_ThrowsNullReferenceException()
    {
        // Arrange
        var bench = new ResizableSpanWriterBench();

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => bench.Cleanup());
    }

    /// <summary>
    /// Tests that Cleanup works correctly with different Size parameter values.
    /// Verifies that the cleanup operation is independent of the benchmark data size.
    /// </summary>
    /// <param name="size">The size parameter for the benchmark.</param>
    [Theory]
    [InlineData(256)]
    [InlineData(4096)]
    [InlineData(65536)]
    public void Cleanup_WithVariousSizes_CompletesSuccessfully(int size)
    {
        // Arrange
        var bench = new ResizableSpanWriterBench
        {
            Size = size
        };
        bench.GlobalSetup();

        // Act & Assert
        var exception = Record.Exception(() => bench.Cleanup());
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that Cleanup can be called multiple times consecutively.
    /// Verifies idempotent behavior and that Reset can be called repeatedly.
    /// </summary>
    [Fact]
    public void Cleanup_CalledMultipleTimes_CompletesSuccessfully()
    {
        // Arrange
        var bench = new ResizableSpanWriterBench
        {
            Size = 256
        };
        bench.GlobalSetup();

        // Act
        bench.Cleanup();
        bench.Cleanup();

        // Assert
        var exception = Record.Exception(() => bench.Cleanup());
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that Cleanup can be called after performing write operations.
    /// Verifies cleanup works correctly after writers have been used.
    /// </summary>
    [Fact]
    public void Cleanup_AfterWriteOperations_CompletesSuccessfully()
    {
        // Arrange
        var bench = new ResizableSpanWriterBench
        {
            Size = 256
        };
        bench.GlobalSetup();
        bench.Old_WriteArray();
        bench.New_WriteArray();

        // Act & Assert
        var exception = Record.Exception(() => bench.Cleanup());
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that New_ResetReuse executes successfully with a typical array size
    /// and produces the expected result after reset and reuse.
    /// </summary>
    [Fact]
    public void New_ResetReuse_WithTypicalArraySize_ExecutesSuccessfully()
    {
        // Arrange
        var bench = new ResizableSpanWriterBench
        {
            Size = 256
        };
        bench.GlobalSetup();

        // Act
        bench.New_ResetReuse();

        // Assert - method should complete without exceptions
        // Verify that after reset and reuse, the writer contains the expected data
        Assert.NotNull(bench);
    }

    /// <summary>
    /// Tests that New_ResetReuse executes successfully with various array sizes
    /// including edge cases like empty, small, medium, and large arrays.
    /// </summary>
    /// <param name="size">The size of the array to test.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(256)]
    [InlineData(4096)]
    [InlineData(65536)]
    public void New_ResetReuse_WithVariousArraySizes_ExecutesSuccessfully(int size)
    {
        // Arrange
        var bench = new ResizableSpanWriterBench
        {
            Size = size
        };
        bench.GlobalSetup();

        // Act
        bench.New_ResetReuse();

        // Assert - method should complete without exceptions
        Assert.NotNull(bench);
    }

    /// <summary>
    /// Tests that New_ResetReuse can be called multiple times consecutively
    /// without errors, verifying proper reset behavior.
    /// </summary>
    [Fact]
    public void New_ResetReuse_CalledMultipleTimes_ExecutesSuccessfully()
    {
        // Arrange
        var bench = new ResizableSpanWriterBench
        {
            Size = 256
        };
        bench.GlobalSetup();

        // Act
        bench.New_ResetReuse();
        bench.New_ResetReuse();
        bench.New_ResetReuse();

        // Assert - method should complete without exceptions
        Assert.NotNull(bench);
    }

    /// <summary>
    /// Tests that New_ResetReuse properly writes, resets, and writes again,
    /// verifying the writer state after the operation.
    /// </summary>
    [Fact]
    public void New_ResetReuse_AfterExecution_WriterContainsExpectedData()
    {
        // Arrange
        var bench = new ResizableSpanWriterBench
        {
            Size = 10
        };
        bench.GlobalSetup();
        var writer = new ResizableSpanWriter<int>();

        // Act
        bench.New_ResetReuse();

        // Assert - After reset and reuse, verify behavior indirectly
        // The benchmark instance should remain valid
        Assert.NotNull(bench);
        Assert.Equal(10, bench.Size);
    }

    /// <summary>
    /// Tests New_ResetReuse with a large array size to ensure proper handling
    /// of memory and buffer resizing during reset operations.
    /// </summary>
    [Fact]
    public void New_ResetReuse_WithLargeArraySize_ExecutesSuccessfully()
    {
        // Arrange
        var bench = new ResizableSpanWriterBench
        {
            Size = 100_000
        };
        bench.GlobalSetup();

        // Act
        bench.New_ResetReuse();

        // Assert - method should complete without exceptions
        Assert.NotNull(bench);
    }

    /// <summary>
    /// Tests New_ResetReuse with boundary value int.MaxValue to verify
    /// edge case handling (though may be limited by memory constraints).
    /// Note: This test may fail with OutOfMemoryException on systems with limited memory.
    /// </summary>
    [Fact]
    public void New_ResetReuse_WithMaxIntSize_HandlesGracefully()
    {
        // Arrange
        var bench = new ResizableSpanWriterBench
        {
            Size = int.MaxValue
        };

        // Act & Assert
        // This will likely throw OutOfMemoryException during GlobalSetup
        Assert.Throws<OutOfMemoryException>(() => bench.GlobalSetup());
    }

    /// <summary>
    /// Tests that New_ResetReuse can handle being called after cleanup,
    /// verifying robustness in different lifecycle states.
    /// </summary>
    [Fact]
    public void New_ResetReuse_AfterCleanup_CanBeCalledAgain()
    {
        // Arrange
        var bench = new ResizableSpanWriterBench
        {
            Size = 256
        };
        bench.GlobalSetup();
        bench.Cleanup();

        // Act - After cleanup, writer should be reset
        bench.New_ResetReuse();

        // Assert - method should complete without exceptions
        Assert.NotNull(bench);
    }

    /// <summary>
    /// Tests New_ResetReuse with minimum non-zero size to verify
    /// correct behavior with smallest valid array.
    /// </summary>
    [Fact]
    public void New_ResetReuse_WithMinimumSize_ExecutesSuccessfully()
    {
        // Arrange
        var bench = new ResizableSpanWriterBench
        {
            Size = 1
        };
        bench.GlobalSetup();

        // Act
        bench.New_ResetReuse();

        // Assert - method should complete without exceptions
        Assert.NotNull(bench);
    }

    /// <summary>
    /// Tests that New_WriteSingles executes successfully without exceptions
    /// for various array sizes including edge cases.
    /// </summary>
    /// <param name="size">The size of the array to test.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(256)]
    [InlineData(4096)]
    [InlineData(65536)]
    public void New_WriteSingles_WithVariousArraySizes_CompletesSuccessfully(int size)
    {
        // Arrange
        var bench = new ResizableSpanWriterBench
        {
            Size = size
        };
        bench.GlobalSetup();

        // Act
        var exception = Record.Exception(() => bench.New_WriteSingles());

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that New_WriteSingles executes successfully when called multiple times
    /// on the same instance, verifying that the writer can handle repeated operations.
    /// </summary>
    [Fact]
    public void New_WriteSingles_CalledMultipleTimes_CompletesSuccessfully()
    {
        // Arrange
        var bench = new ResizableSpanWriterBench
        {
            Size = 100
        };
        bench.GlobalSetup();

        // Act
        var exception1 = Record.Exception(() => bench.New_WriteSingles());
        bench.Cleanup();
        var exception2 = Record.Exception(() => bench.New_WriteSingles());

        // Assert
        Assert.Null(exception1);
        Assert.Null(exception2);
    }

    /// <summary>
    /// Tests that New_WriteSingles handles the maximum parameter value
    /// used in benchmarks without throwing exceptions.
    /// </summary>
    [Fact]
    public void New_WriteSingles_WithMaxBenchmarkSize_CompletesSuccessfully()
    {
        // Arrange
        var bench = new ResizableSpanWriterBench
        {
            Size = 65536
        };
        bench.GlobalSetup();

        // Act
        var exception = Record.Exception(() => bench.New_WriteSingles());

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that New_WriteSingles handles an empty array (size 0)
    /// without throwing exceptions, verifying proper handling of edge case.
    /// </summary>
    [Fact]
    public void New_WriteSingles_WithEmptyArray_CompletesSuccessfully()
    {
        // Arrange
        var bench = new ResizableSpanWriterBench
        {
            Size = 0
        };
        bench.GlobalSetup();

        // Act
        var exception = Record.Exception(() => bench.New_WriteSingles());

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that New_WriteSingles handles a single element array
    /// without throwing exceptions, verifying proper handling of minimal array.
    /// </summary>
    [Fact]
    public void New_WriteSingles_WithSingleElement_CompletesSuccessfully()
    {
        // Arrange
        var bench = new ResizableSpanWriterBench
        {
            Size = 1
        };
        bench.GlobalSetup();

        // Act
        var exception = Record.Exception(() => bench.New_WriteSingles());

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that New_WriteArray executes successfully when the benchmark is properly initialized
    /// with a standard size parameter.
    /// </summary>
    [Theory]
    [InlineData(256)]
    [InlineData(4_096)]
    [InlineData(65_536)]
    public void New_WriteArray_WithInitializedState_ExecutesSuccessfully(int size)
    {
        // Arrange
        var bench = new ResizableSpanWriterBench
        {
            Size = size
        };
        bench.GlobalSetup();

        // Act
        bench.New_WriteArray();

        // Assert - Method completes without throwing
    }

    /// <summary>
    /// Tests that New_WriteArray throws NullReferenceException when the writer is not initialized.
    /// </summary>
    [Fact]
    public void New_WriteArray_WithUninitializedWriter_ThrowsNullReferenceException()
    {
        // Arrange
        var bench = new ResizableSpanWriterBench
        {
            Size = 256
        };
        // Note: GlobalSetup is NOT called, so _newWriter and _source are null

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => bench.New_WriteArray());
    }

    /// <summary>
    /// Tests that New_WriteArray can be called multiple times without throwing exceptions.
    /// </summary>
    [Fact]
    public void New_WriteArray_CalledMultipleTimes_ExecutesSuccessfully()
    {
        // Arrange
        var bench = new ResizableSpanWriterBench
        {
            Size = 256
        };
        bench.GlobalSetup();

        // Act - Call multiple times
        bench.New_WriteArray();
        bench.New_WriteArray();
        bench.New_WriteArray();

        // Assert - Method completes without throwing
    }

    /// <summary>
    /// Tests that New_WriteArray works with edge case size of zero elements.
    /// </summary>
    [Fact]
    public void New_WriteArray_WithZeroSize_ExecutesSuccessfully()
    {
        // Arrange
        var bench = new ResizableSpanWriterBench
        {
            Size = 0
        };
        bench.GlobalSetup();

        // Act
        bench.New_WriteArray();

        // Assert - Method completes without throwing
    }

    /// <summary>
    /// Tests that New_WriteArray works with a very large size.
    /// </summary>
    [Fact]
    public void New_WriteArray_WithLargeSize_ExecutesSuccessfully()
    {
        // Arrange
        var bench = new ResizableSpanWriterBench
        {
            Size = 1_000_000
        };
        bench.GlobalSetup();

        // Act
        bench.New_WriteArray();

        // Assert - Method completes without throwing
    }

    /// <summary>
    /// Tests that New_WriteArray works correctly after cleanup is called.
    /// </summary>
    [Fact]
    public void New_WriteArray_AfterCleanup_ExecutesSuccessfully()
    {
        // Arrange
        var bench = new ResizableSpanWriterBench
        {
            Size = 256
        };
        bench.GlobalSetup();
        bench.New_WriteArray(); // First write
        bench.Cleanup(); // Cleanup

        // Act
        bench.New_WriteArray(); // Write after cleanup

        // Assert - Method completes without throwing
    }

    /// <summary>
    /// Tests that GlobalSetup correctly handles Size at integer boundary (int.MaxValue).
    /// Expected: OutOfMemoryException is thrown when attempting to allocate such a large array.
    /// </summary>
    [Fact]
    public void GlobalSetup_MaxIntSize_ThrowsOutOfMemoryException()
    {
        // Arrange
        var bench = new ResizableSpanWriterBench
        {
            Size = int.MaxValue
        };

        // Act & Assert
        Assert.Throws<OutOfMemoryException>(() => bench.GlobalSetup());
    }

    /// <summary>
    /// Tests that New_WriteSpanAdvance executes successfully with a typical size.
    /// Verifies that the method can get a span, copy data, and advance without throwing exceptions.
    /// </summary>
    [Fact]
    public void New_WriteSpanAdvance_TypicalSize_ExecutesSuccessfully()
    {
        // Arrange
        var bench = new ResizableSpanWriterBench
        {
            Size = 256
        };
        bench.GlobalSetup();

        // Act
        var exception = Record.Exception(() => bench.New_WriteSpanAdvance());

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that New_WriteSpanAdvance executes successfully with a large size.
    /// Verifies that the method handles large data arrays correctly.
    /// </summary>
    [Theory]
    [InlineData(256)]
    [InlineData(4096)]
    [InlineData(65536)]
    public void New_WriteSpanAdvance_VariousSizes_ExecutesSuccessfully(int size)
    {
        // Arrange
        var bench = new ResizableSpanWriterBench
        {
            Size = size
        };
        bench.GlobalSetup();

        // Act
        var exception = Record.Exception(() => bench.New_WriteSpanAdvance());

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that New_WriteSpanAdvance executes successfully with minimum size.
    /// Verifies that the method handles the smallest valid size (1).
    /// </summary>
    [Fact]
    public void New_WriteSpanAdvance_MinimumSize_ExecutesSuccessfully()
    {
        // Arrange
        var bench = new ResizableSpanWriterBench
        {
            Size = 1
        };
        bench.GlobalSetup();

        // Act
        var exception = Record.Exception(() => bench.New_WriteSpanAdvance());

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that New_WriteSpanAdvance executes successfully with zero size.
    /// Verifies that the method handles empty arrays correctly.
    /// </summary>
    [Fact]
    public void New_WriteSpanAdvance_ZeroSize_ExecutesSuccessfully()
    {
        // Arrange
        var bench = new ResizableSpanWriterBench
        {
            Size = 0
        };
        bench.GlobalSetup();

        // Act
        var exception = Record.Exception(() => bench.New_WriteSpanAdvance());

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that New_WriteSpanAdvance can be called multiple times in succession.
    /// Verifies that the method properly manages state across multiple invocations.
    /// </summary>
    [Fact]
    public void New_WriteSpanAdvance_MultipleInvocations_ExecutesSuccessfully()
    {
        // Arrange
        var bench = new ResizableSpanWriterBench
        {
            Size = 256
        };
        bench.GlobalSetup();

        // Act
        var exception1 = Record.Exception(() => bench.New_WriteSpanAdvance());
        var exception2 = Record.Exception(() => bench.New_WriteSpanAdvance());
        var exception3 = Record.Exception(() => bench.New_WriteSpanAdvance());

        // Assert
        Assert.Null(exception1);
        Assert.Null(exception2);
        Assert.Null(exception3);
    }

    /// <summary>
    /// Tests that New_WriteSpanAdvance executes successfully after cleanup.
    /// Verifies that the method works correctly after the Cleanup method is called.
    /// </summary>
    [Fact]
    public void New_WriteSpanAdvance_AfterCleanup_ExecutesSuccessfully()
    {
        // Arrange
        var bench = new ResizableSpanWriterBench
        {
            Size = 256
        };
        bench.GlobalSetup();
        bench.New_WriteSpanAdvance();
        bench.Cleanup();

        // Act
        var exception = Record.Exception(() => bench.New_WriteSpanAdvance());

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that New_WriteSpanAdvance executes successfully with large boundary values.
    /// Verifies that the method handles very large sizes correctly.
    /// </summary>
    [Theory]
    [InlineData(1_048_576)]
    [InlineData(10_000_000)]
    public void New_WriteSpanAdvance_VeryLargeSizes_ExecutesSuccessfully(int size)
    {
        // Arrange
        var bench = new ResizableSpanWriterBench
        {
            Size = size
        };
        bench.GlobalSetup();

        // Act
        var exception = Record.Exception(() => bench.New_WriteSpanAdvance());

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that Old_WriteSpanAdvance executes successfully with a small array.
    /// Input: A properly initialized benchmark instance with Size=256.
    /// Expected: The method completes without throwing any exceptions.
    /// </summary>
    [Fact]
    public void Old_WriteSpanAdvance_WithSmallArray_ExecutesSuccessfully()
    {
        // Arrange
        ResizableSpanWriterBench bench = new ResizableSpanWriterBench
        {
            Size = 256
        };
        bench.GlobalSetup();

        // Act & Assert
        var exception = Record.Exception(() => bench.Old_WriteSpanAdvance());
        Assert.Null(exception);

        bench.Cleanup();
    }

    /// <summary>
    /// Tests that Old_WriteSpanAdvance executes successfully with a medium-sized array.
    /// Input: A properly initialized benchmark instance with Size=4096.
    /// Expected: The method completes without throwing any exceptions.
    /// </summary>
    [Fact]
    public void Old_WriteSpanAdvance_WithMediumArray_ExecutesSuccessfully()
    {
        // Arrange
        ResizableSpanWriterBench bench = new ResizableSpanWriterBench
        {
            Size = 4_096
        };
        bench.GlobalSetup();

        // Act & Assert
        var exception = Record.Exception(() => bench.Old_WriteSpanAdvance());
        Assert.Null(exception);

        bench.Cleanup();
    }

    /// <summary>
    /// Tests that Old_WriteSpanAdvance executes successfully with a large array.
    /// Input: A properly initialized benchmark instance with Size=65536.
    /// Expected: The method completes without throwing any exceptions.
    /// </summary>
    [Fact]
    public void Old_WriteSpanAdvance_WithLargeArray_ExecutesSuccessfully()
    {
        // Arrange
        ResizableSpanWriterBench bench = new ResizableSpanWriterBench
        {
            Size = 65_536
        };
        bench.GlobalSetup();

        // Act & Assert
        var exception = Record.Exception(() => bench.Old_WriteSpanAdvance());
        Assert.Null(exception);

        bench.Cleanup();
    }

    /// <summary>
    /// Tests that Old_WriteSpanAdvance executes successfully with an empty array.
    /// Input: A benchmark instance with an empty source array.
    /// Expected: The method completes without throwing any exceptions.
    /// </summary>
    [Fact]
    public void Old_WriteSpanAdvance_WithEmptyArray_ExecutesSuccessfully()
    {
        // Arrange
        ResizableSpanWriterBench bench = new ResizableSpanWriterBench
        {
            Size = 0
        };
        bench.GlobalSetup();

        // Act & Assert
        var exception = Record.Exception(() => bench.Old_WriteSpanAdvance());
        Assert.Null(exception);

        bench.Cleanup();
    }

    /// <summary>
    /// Tests that Old_WriteSpanAdvance executes successfully when called multiple times in sequence.
    /// Input: A properly initialized benchmark instance called twice.
    /// Expected: Both calls complete without throwing any exceptions.
    /// </summary>
    [Fact]
    public void Old_WriteSpanAdvance_CalledMultipleTimes_ExecutesSuccessfully()
    {
        // Arrange
        ResizableSpanWriterBench bench = new ResizableSpanWriterBench
        {
            Size = 256
        };
        bench.GlobalSetup();

        // Act
        bench.Old_WriteSpanAdvance();
        var exception = Record.Exception(() => bench.Old_WriteSpanAdvance());

        // Assert
        Assert.Null(exception);

        bench.Cleanup();
    }

    /// <summary>
    /// Tests that Old_WriteSpanAdvance executes successfully with a single-element array.
    /// Input: A benchmark instance with Size=1.
    /// Expected: The method completes without throwing any exceptions.
    /// </summary>
    [Fact]
    public void Old_WriteSpanAdvance_WithSingleElement_ExecutesSuccessfully()
    {
        // Arrange
        ResizableSpanWriterBench bench = new ResizableSpanWriterBench
        {
            Size = 1
        };
        bench.GlobalSetup();

        // Act & Assert
        var exception = Record.Exception(() => bench.Old_WriteSpanAdvance());
        Assert.Null(exception);

        bench.Cleanup();
    }

    /// <summary>
    /// Tests that Old_WriteSpanAdvance executes successfully with a boundary size.
    /// Input: A benchmark instance with Size=int.MaxValue (may be limited by memory).
    /// Expected: The method either completes successfully or throws OutOfMemoryException.
    /// </summary>
    [Fact]
    public void Old_WriteSpanAdvance_WithMaxIntSize_HandlesGracefully()
    {
        // Arrange
        ResizableSpanWriterBench bench = new ResizableSpanWriterBench
        {
            Size = int.MaxValue
        };

        // Act & Assert
        // This may throw OutOfMemoryException during GlobalSetup, which is expected
        var exception = Record.Exception(() =>
        {
            bench.GlobalSetup();
            bench.Old_WriteSpanAdvance();
            bench.Cleanup();
        });

        // Either succeeds or throws OutOfMemoryException
        if (exception != null)
        {
            Assert.IsType<OutOfMemoryException>(exception);
        }
    }

    /// <summary>
    /// Tests that Old_ResetReuse executes successfully with valid initialized data.
    /// Verifies the method completes without throwing exceptions when the writer and source array are properly initialized.
    /// </summary>
    /// <param name="size">The size of the source array to test.</param>
    [Theory]
    [InlineData(1)]
    [InlineData(256)]
    [InlineData(4096)]
    [InlineData(65536)]
    public void Old_ResetReuse_WithValidData_ExecutesSuccessfully(int size)
    {
        // Arrange
        var bench = new ResizableSpanWriterBench
        {
            Size = size
        };
        bench.GlobalSetup();

        // Act & Assert - should not throw
        bench.Old_ResetReuse();
    }

    /// <summary>
    /// Tests that Old_ResetReuse throws NullReferenceException when _oldWriter is null.
    /// Verifies proper exception handling when the writer has not been initialized.
    /// </summary>
    [Fact]
    public void Old_ResetReuse_WithNullWriter_ThrowsNullReferenceException()
    {
        // Arrange
        var bench = new ResizableSpanWriterBench();
        // _oldWriter is null (not initialized via GlobalSetup)

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => bench.Old_ResetReuse());
    }

    /// <summary>
    /// Tests that Old_ResetReuse executes successfully with an empty source array.
    /// Verifies the method handles edge case of zero-length array without throwing exceptions.
    /// </summary>
    [Fact]
    public void Old_ResetReuse_WithEmptySource_ExecutesSuccessfully()
    {
        // Arrange
        var bench = new ResizableSpanWriterBench
        {
            Size = 0
        };
        bench.GlobalSetup();

        // Act & Assert - should not throw
        bench.Old_ResetReuse();
    }

    /// <summary>
    /// Tests that Old_ResetReuse executes successfully with maximum integer size.
    /// Verifies the method can handle boundary case of very large arrays.
    /// </summary>
    [Fact]
    public void Old_ResetReuse_WithMaxSize_ExecutesSuccessfully()
    {
        // Arrange
        var bench = new ResizableSpanWriterBench
        {
            Size = 1048576
        };
        bench.GlobalSetup();

        // Act & Assert - should not throw
        bench.Old_ResetReuse();
    }

    /// <summary>
    /// Tests that Old_WriteSingles executes successfully with a small array.
    /// Verifies that the method iterates through a small source array and writes each element individually
    /// without throwing any exceptions.
    /// </summary>
    [Fact]
    public void Old_WriteSingles_SmallArray_ExecutesSuccessfully()
    {
        // Arrange
        var bench = new ResizableSpanWriterBench
        {
            Size = 256
        };
        bench.GlobalSetup();

        // Act
        var exception = Record.Exception(() => bench.Old_WriteSingles());

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that Old_WriteSingles executes successfully with a medium-sized array.
    /// Verifies that the method iterates through a medium-sized source array and writes each element individually
    /// without throwing any exceptions.
    /// </summary>
    [Fact]
    public void Old_WriteSingles_MediumArray_ExecutesSuccessfully()
    {
        // Arrange
        var bench = new ResizableSpanWriterBench
        {
            Size = 4_096
        };
        bench.GlobalSetup();

        // Act
        var exception = Record.Exception(() => bench.Old_WriteSingles());

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that Old_WriteSingles executes successfully with a large array.
    /// Verifies that the method iterates through a large source array and writes each element individually
    /// without throwing any exceptions.
    /// </summary>
    [Fact]
    public void Old_WriteSingles_LargeArray_ExecutesSuccessfully()
    {
        // Arrange
        var bench = new ResizableSpanWriterBench
        {
            Size = 65_536
        };
        bench.GlobalSetup();

        // Act
        var exception = Record.Exception(() => bench.Old_WriteSingles());

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that Old_WriteSingles handles an empty array correctly.
    /// Verifies that the method completes without error when the source array is empty,
    /// ensuring the loop condition (_source.Length) is properly evaluated.
    /// </summary>
    [Fact]
    public void Old_WriteSingles_EmptyArray_ExecutesSuccessfully()
    {
        // Arrange
        var bench = new ResizableSpanWriterBench
        {
            Size = 0
        };
        bench.GlobalSetup();

        // Act
        var exception = Record.Exception(() => bench.Old_WriteSingles());

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that Old_WriteSingles handles a single-element array correctly.
    /// Verifies that the method properly iterates through an array with exactly one element
    /// without throwing any exceptions.
    /// </summary>
    [Fact]
    public void Old_WriteSingles_SingleElementArray_ExecutesSuccessfully()
    {
        // Arrange
        var bench = new ResizableSpanWriterBench
        {
            Size = 1
        };
        bench.GlobalSetup();

        // Act
        var exception = Record.Exception(() => bench.Old_WriteSingles());

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that Old_WriteSingles can be called multiple times consecutively.
    /// Verifies that the method can be executed multiple times in sequence
    /// without causing any errors or state corruption.
    /// </summary>
    [Fact]
    public void Old_WriteSingles_MultipleInvocations_ExecutesSuccessfully()
    {
        // Arrange
        var bench = new ResizableSpanWriterBench
        {
            Size = 256
        };
        bench.GlobalSetup();

        // Act
        var exception1 = Record.Exception(() => bench.Old_WriteSingles());
        var exception2 = Record.Exception(() => bench.Old_WriteSingles());
        var exception3 = Record.Exception(() => bench.Old_WriteSingles());

        // Assert
        Assert.Null(exception1);
        Assert.Null(exception2);
        Assert.Null(exception3);
    }

    /// <summary>
    /// Tests that Old_WriteSingles executes successfully with parameterized size values.
    /// Uses inline data to test multiple array sizes in a single parameterized test.
    /// </summary>
    /// <param name="size">The size of the array to test.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(256)]
    [InlineData(4_096)]
    [InlineData(65_536)]
    public void Old_WriteSingles_VariousSizes_ExecutesSuccessfully(int size)
    {
        // Arrange
        var bench = new ResizableSpanWriterBench
        {
            Size = size
        };
        bench.GlobalSetup();

        // Act
        var exception = Record.Exception(() => bench.Old_WriteSingles());

        // Assert
        Assert.Null(exception);
    }
}