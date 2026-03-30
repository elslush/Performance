using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Moq;
using Performance.Buffers;
using Xunit;

namespace Performance.Tests;


public sealed class ResizableByteWriterTests
{
    // -------- Stream surface tests --------

    [Fact]
    public void Stream_Capabilities_And_Length()
    {
        using var w = new ResizableByteWriter(initialCapacity: 0);
        Assert.False(w.CanRead);
        Assert.False(w.CanSeek);
        Assert.True(w.CanWrite);
        Assert.Equal(0, w.Length);

        // Position not supported
        Assert.Throws<NotSupportedException>(() => _ = w.Position);
        Assert.Throws<NotSupportedException>(() => w.Position = 0);
    }

    [Fact]
    public void Stream_Write_Array_Offset_Count_Works()
    {
        using var w = new ResizableByteWriter(initialCapacity: 4);
        var data = new byte[] { 0, 1, 2, 3, 4, 5 };

        w.Write(data, 2, 3); // 2,3,4
        Assert.Equal(new byte[] { 2, 3, 4 }, w.WrittenSpan.ToArray());
        Assert.Equal(3, w.Length);
    }

    [Fact]
    public void Stream_Unsupported_Members_Throw()
    {
        using var w = new ResizableByteWriter();

        Assert.Throws<NotSupportedException>(() => w.Read(new byte[10], 0, 10));
        Assert.Throws<NotSupportedException>(() => w.Seek(0, SeekOrigin.Begin));
        Assert.Throws<NotSupportedException>(() => w.SetLength(123));
    }

    [Fact]
    public async Task Stream_Flush_NoOp()
    {
        using var w = new ResizableByteWriter();
        w.WriteByte((byte)7);
        w.Flush();
        await w.FlushAsync(CancellationToken.None);
        Assert.Equal(new byte[] { 7 }, w.WrittenSpan.ToArray());
    }

    // -------- IBufferWriter flow --------

    [Fact]
    public void GetSpan_DefaultSizeHint_Is8_And_Advance()
    {
        using var w = new ResizableByteWriter(initialCapacity: 0);

        var span = w.GetSpan(8); // default sizeHint=8 in implementation
        Assert.Equal(8, span.Length);

        for (int i = 0; i < span.Length; i++) span[i] = (byte)i;
        w.Advance(span.Length);

        Assert.Equal(8, w.WrittenSpan.Length);
        Assert.True(w.WrittenSpan.SequenceEqual(Enumerable.Range(0, 8).Select(i => (byte)i).ToArray()));
        Assert.Equal(8, w.Length);
    }

    [Fact]
    public void GetMemory_Write_Then_Advance()
    {
        using var w = new ResizableByteWriter(initialCapacity: 0);
        var mem = w.GetMemory(sizeHint: 5);
        for (int i = 0; i < 5; i++) mem.Span[i] = (byte)(10 + i);
        w.Advance(5);

        Assert.Equal(new byte[] { 10, 11, 12, 13, 14 }, w.WrittenSpan.ToArray());
    }

    [Fact]
    public void Advance_Negative_Throws()
    {
        using var w = new ResizableByteWriter(initialCapacity: 8);
        Assert.Throws<ArgumentOutOfRangeException>(() => w.Advance(-1));
    }

    [Fact]
    public void Advance_Without_Reservation_Throws()
    {
        using var w = new ResizableByteWriter(initialCapacity: 0);
        // No buffer yet → invalid to advance
        Assert.Throws<InvalidOperationException>(() => w.Advance(1));
    }

    // -------- Write overloads --------

    [Fact]
    public void Write_SingleByte_And_Slices()
    {
        using var w = new ResizableByteWriter(initialCapacity: 1);
        w.WriteByte((byte)1);
        w.WriteByte((byte)2);

        var more = new byte[] { 3, 4, 5, 6 };
        w.Write(more.AsSpan(1, 2)); // 4,5

        Assert.Equal(new byte[] { 1, 2, 4, 5 }, w.WrittenSpan.ToArray());
    }

    [Fact]
    public void Write_ReadOnlySpan_And_ReadOnlyMemory()
    {
        using var w = new ResizableByteWriter(initialCapacity: 2);
        w.Write([9, 9]);
        w.Write(new ReadOnlyMemory<byte>([1, 2, 3]));

        Assert.Equal(new byte[] { 9, 9, 1, 2, 3 }, w.WrittenSpan.ToArray());
    }

    [Fact]
    public void Write_Array_Appends()
    {
        using var w = new ResizableByteWriter(initialCapacity: 2);
        w.Write([1, 2, 3]);
        w.Write([4, 5]);

        Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, w.WrittenSpan.ToArray());
    }

    // -------- Growth & capacity rounding --------

    [Fact]
    public void Growth_Rounds_To_Next_Power_Of_Two()
    {
        using var w = new ResizableByteWriter(initialCapacity: 0);
        var payload = Enumerable.Range(0, 1000).Select(i => (byte)i).ToArray();
        w.Write(payload);

        // IMemoryOwner<byte>.Memory returns the full backing buffer
        var full = ((IMemoryOwner<byte>)w).Memory;
        Assert.True(full.Length >= 1024); // next pow2 >= 1000
        Assert.True(w.WrittenSpan.SequenceEqual(payload));
    }

    [Fact]
    public void Large_GetSpan_GrowsOnce_And_Keeps_Data()
    {
        using var w = new ResizableByteWriter(initialCapacity: 4);

        // First chunk
        var s1 = w.GetSpan(700);
        for (int i = 0; i < 700; i++) s1[i] = (byte)i;
        w.Advance(700);

        // Second chunk
        var s2 = w.GetSpan(400);
        for (int i = 0; i < 400; i++) s2[i] = (byte)(i + 1);
        w.Advance(400);

        Assert.Equal(1100, w.WrittenSpan.Length);
        Assert.Equal(1100, w.Length);
        Assert.Equal(Enumerable.Range(0, 700).Select(i => (byte)i), w.WrittenSpan[..700].ToArray());
        Assert.Equal(Enumerable.Range(0, 400).Select(i => (byte)(i + 1)), w.WrittenSpan[700..].ToArray());
    }

    // -------- Reset semantics --------

    [Fact]
    public void Reset_SetsIndexToZero_But_Keeps_Capacity()
    {
        using var w = new ResizableByteWriter(initialCapacity: 2);
        w.Write([1, 2, 3, 4, 5]);

        int capBefore = ((IMemoryOwner<byte>)w).Memory.Length;

        w.Reset();
        Assert.Equal(0, w.WrittenSpan.Length);
        Assert.Equal(0, w.Length);

        w.Write([8, 8, 8, 8]);
        Assert.Equal(new byte[] { 8, 8, 8, 8 }, w.WrittenSpan.ToArray());

        int capAfter = ((IMemoryOwner<byte>)w).Memory.Length;
        Assert.Equal(capBefore, capAfter);
    }

    // -------- Read views --------

    [Fact]
    public void WrittenSpan_And_WrittenMemory_Agree()
    {
        using var w = new ResizableByteWriter();
        w.Write([10, 11, 12]);

        var span = w.WrittenSpan;
        var mem = w.WrittenMemory;
        Assert.Equal(span.Length, mem.Length);
        Assert.True(span.SequenceEqual(mem.ToArray()));
    }

    [Fact]
    public void IMemoryOwner_Memory_Exposes_Full_Buffer()
    {
        using var w = new ResizableByteWriter(initialCapacity: 8);
        w.Write([1, 2, 3]);
        var full = ((IMemoryOwner<byte>)w).Memory;

        Assert.True(full.Length >= w.WrittenSpan.Length);
        Assert.True(full.Length >= 8);
    }

    // -------- Pool behavior --------

    [Fact]
    public void Dispose_Returns_Buffer_To_Pool()
    {
        var pool = new TrackingArrayPool();
        var w = new ResizableByteWriter(pool, initialCapacity: 4);

        // cause growth
        w.Write([1, 2, 3, 4, 5, 6]);

        Assert.True(pool.RentedCount >= 1);
        int returnsBefore = pool.ReturnedCount;

        w.Dispose();

        Assert.Equal(returnsBefore + 1, pool.ReturnedCount);

        // Double-dispose is no-op
        w.Dispose();
        Assert.Equal(returnsBefore + 1, pool.ReturnedCount);
    }

    [Fact]
    public void GetSpan_ZeroSizeHint_AllowsWritingOneByteInALoop()
    {
        using var w = new ResizableByteWriter();

        // Common pattern: repeatedly request with sizeHint=0
        for (int i = 0; i < 10_000; i++)
        {
            var span = w.GetSpan();               // sizeHint = 0
            Assert.True(span.Length >= 1);        // MUST be non-zero
            span[0] = (byte)(i & 0xFF);
            w.Advance(1);
        }

        Assert.Equal(10_000, w.Length);
        Assert.Equal(10_000, w.WrittenSpan.Length);
        // spot check
        Assert.Equal((byte)0, w.WrittenSpan[0]);
        Assert.Equal((byte)255, w.WrittenSpan[255]);
        Assert.Equal((byte)(9999 & 0xFF), w.WrittenSpan[^1]);
    }

    [Fact]
    public void GetMemory_ZeroSizeHint_AllowsWriteAdvancePattern()
    {
        using var w = new ResizableByteWriter();

        for (int i = 0; i < 1024; i++)
        {
            var mem = w.GetMemory();              // sizeHint = 0
            Assert.True(mem.Length >= 1);         // MUST be non-zero
            mem.Span[0] = 42;
            w.Advance(1);
        }

        Assert.Equal(1024, w.Length);
        Assert.True(w.WrittenSpan.ToArray().All(b => b == 42));
    }

    [Fact]
    public void GetSpan_ZeroSizeHint_DoesNotRequireImmediateAdvance()
    {
        using var w = new ResizableByteWriter();

        var s1 = w.GetSpan();                     // should be non-zero
        Assert.True(s1.Length >= 1);

        // Ask again without advancing; should still return a valid span and not corrupt state.
        var s2 = w.GetSpan();
        Assert.True(s2.Length >= 1);

        s2[0] = 7; w.Advance(1);
        Assert.Equal(new byte[] { 7 }, w.WrittenSpan.ToArray());
    }

    [Fact]
    public void Advance_Throws_When_OverAdvancing_Beyond_Reserved()
    {
        using var w = new ResizableByteWriter();

        var s = w.GetSpan(4);
        s[0] = 1; s[1] = 2;
        Assert.Throws<InvalidOperationException>(() => w.Advance(5)); // only reserved 4
        w.Advance(2); // ok
        Assert.Equal(new byte[] { 1, 2 }, w.WrittenSpan.ToArray());
    }

    [Fact]
    public void Write_Span_Triggers_Growth_And_Preserves_Data()
    {
        using var w = new ResizableByteWriter(initialCapacity: 4);
        w.Write([1, 2, 3, 4]);
        w.Write([5, 6, 7, 8, 9]);   // triggers EnsureCapacity
        Assert.Equal(9, w.Length);
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, w.WrittenSpan.ToArray());
    }

    [Fact]
    public void Reset_Reuses_Buffer()
    {
        using var w = new ResizableByteWriter(initialCapacity: 2);
        w.Write([1, 2, 3, 4, 5]);
        int capBefore = ((IMemoryOwner<byte>)w).Memory.Length;

        w.Reset();
        Assert.Equal(0, w.Length);

        w.Write([9, 8, 7]);
        Assert.Equal(new byte[] { 9, 8, 7 }, w.WrittenSpan.ToArray());
        int capAfter = ((IMemoryOwner<byte>)w).Memory.Length;
        Assert.Equal(capBefore, capAfter);
    }

    [Fact]
    public async Task Stream_WriteAsync_Delegates_To_Sync()
    {
        using var w = new ResizableByteWriter();
        var buf = new byte[] { 10, 11, 12 };
        await w.WriteAsync(buf, 0, buf.Length, TestContext.Current.CancellationToken);
        Assert.Equal(buf, w.WrittenSpan.ToArray());
    }

    [Fact]
    public void MemoryOwner_Returns_Full_Buffer()
    {
        using var w = new ResizableByteWriter(initialCapacity: 8);
        w.Write([1, 2, 3]);
        var mem = ((IMemoryOwner<byte>)w).Memory;
        Assert.True(mem.Length >= 8);
        Assert.True(mem.Length >= w.WrittenSpan.Length);
    }

    [Fact]
    public void ByteWriter_DirectWrite_Invalidates_GetSpanReservation()
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        var span = writer.GetSpan(16); // Reserve 16 bytes

        // Act: Perform a direct write, which should invalidate the reservation.
        writer.WriteByte(42);

        // Assert: Advancing the original reservation should now fail.
        var ex = Assert.Throws<InvalidOperationException>(() => writer.Advance(1));
        Assert.Equal("Cannot advance past the end of the reserved buffer segment.", ex.Message);

        // The writer's content should only contain the directly written byte.
        Assert.Equal(new byte[] { 42 }, writer.WrittenSpan.ToArray());
    }

    [Fact]
    public void ByteWriter_WriteArray_Invalidates_GetSpanReservation()
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        var span = writer.GetSpan(16); // Reserve 16 bytes

        // Act: Perform a write, which should invalidate the reservation.
        writer.Write(new byte[] { 42 });

        // Assert: Advancing the original reservation should now fail.
        var ex = Assert.Throws<InvalidOperationException>(() => writer.Advance(1));
        Assert.Equal("Cannot advance past the end of the reserved buffer segment.", ex.Message);

        // The writer's content should only contain the written byte.
        Assert.Equal(new byte[] { 42 }, writer.WrittenSpan.ToArray());
    }

    [Fact]
    public void ByteWriter_Reset_Clears_GetSpanReservation()
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        var span = writer.GetSpan(16);

        // Act: Reset the writer.
        writer.Reset();

        // Assert: Advancing the original reservation should fail.
        var ex = Assert.Throws<InvalidOperationException>(() => writer.Advance(1));
        Assert.Equal("Cannot advance past the end of the reserved buffer segment.", ex.Message);
        Assert.Equal(0, writer.Length);
    }

    [Fact]
    public void ByteWriter_GetSpan_WithZeroSizeHint_StillRequiresAdvance()
    {
        // Arrange
        using var writer = new ResizableByteWriter();

        // Act
        var span = writer.GetSpan(0); // This will default to sizeHint = 8

        // Assert
        // Even though the request was for 0, a buffer is still returned.
        Assert.True(span.Length >= 8);

        var spanLength = span.Length;
        // Advancing by more than the *requested* hint (which defaults to 8) should fail.
        Assert.Throws<InvalidOperationException>(() => writer.Advance(spanLength + 1));

        // Advancing by a valid amount should succeed.
        span[0] = 1;
        writer.Advance(1);
        Assert.Equal(new byte[] { 1 }, writer.WrittenSpan.ToArray());
    }

    // -------- Known-issue: disposed guard doesn't trip --------
    // Your class has a readonly `_disposed` = false and never sets it to true in Dispose(),
    // so ThrowIfDisposed() will never throw. If you fix that, un-skip this test.

    [Fact]
    public void After_Dispose_Accessors_Throw()
    {
        var w = new ResizableByteWriter(initialCapacity: 8);
        w.Write([1, 2, 3]);
        w.Dispose();

        Assert.Throws<ObjectDisposedException>(() => _ = w.WrittenSpan);
        Assert.Throws<ObjectDisposedException>(() => _ = w.WrittenMemory);
        Assert.Throws<ObjectDisposedException>(() => _ = ((IMemoryOwner<byte>)w).Memory);
        Assert.Throws<ObjectDisposedException>(() => w.GetSpan());
        Assert.Throws<ObjectDisposedException>(() => w.GetMemory());
        Assert.Throws<ObjectDisposedException>(() => w.WriteByte((byte)1));
        Assert.Throws<ObjectDisposedException>(() => w.Reset());
    }

    // -------- Helpers --------

    private sealed class TrackingArrayPool : ArrayPool<byte>
    {
        public int RentedCount { get; private set; }
        public int ReturnedCount { get; private set; }

        public override byte[] Rent(int minimumLength)
        {
            RentedCount++;
            return GC.AllocateUninitializedArray<byte>(Math.Max(1, minimumLength));
        }

        public override void Return(byte[] array, bool clearArray = false)
        {
            ReturnedCount++;
            if (clearArray)
                Array.Clear(array, 0, array.Length);
        }
    }

    /// <summary>
    /// Tests that the constructor with zero initial capacity creates an empty buffer without renting from the pool.
    /// Input: pool = ArrayPool.Shared, initialCapacity = 0
    /// Expected: Object created successfully, no array rented from pool, WrittenSpan is empty, Length is 0
    /// </summary>
    [Fact]
    public void Constructor_WithZeroInitialCapacity_CreatesEmptyBuffer()
    {
        // Arrange & Act
        using var writer = new ResizableByteWriter(ArrayPool<byte>.Shared, initialCapacity: 0);

        // Assert
        Assert.Equal(0, writer.Length);
        Assert.Empty(writer.WrittenSpan.ToArray());
        Assert.True(writer.CanWrite);
    }

    /// <summary>
    /// Tests that the constructor with positive initial capacity rents a buffer from the pool.
    /// Input: pool = mock pool, initialCapacity = 16
    /// Expected: Pool.Rent is called with the specified capacity
    /// </summary>
    [Fact]
    public void Constructor_WithPositiveInitialCapacity_RentsBufferFromPool()
    {
        // Arrange
        var mockPool = new Mock<ArrayPool<byte>>();
        var rentedArray = new byte[16];
        mockPool.Setup(p => p.Rent(16)).Returns(rentedArray);

        // Act
        using var writer = new ResizableByteWriter(mockPool.Object, initialCapacity: 16);

        // Assert
        mockPool.Verify(p => p.Rent(16), Times.Once);
        Assert.Equal(0, writer.Length);
        Assert.Empty(writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Tests that the constructor throws ArgumentOutOfRangeException when initialCapacity is negative.
    /// Input: initialCapacity = -1
    /// Expected: ArgumentOutOfRangeException is thrown
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void Constructor_WithNegativeInitialCapacity_ThrowsArgumentOutOfRangeException(int negativeCapacity)
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ResizableByteWriter(ArrayPool<byte>.Shared, initialCapacity: negativeCapacity));

        Assert.Contains("initialCapacity", exception.Message);
    }

    /// <summary>
    /// Tests that the constructor accepts int.MaxValue as initial capacity without throwing during construction.
    /// Input: initialCapacity = int.MaxValue
    /// Expected: No exception during construction (pool handles allocation limits)
    /// </summary>
    [Fact]
    public void Constructor_WithMaxValueInitialCapacity_DoesNotThrowDuringConstruction()
    {
        // Arrange
        var mockPool = new Mock<ArrayPool<byte>>();
        var largeArray = new byte[1024]; // Pool can return whatever size it wants
        mockPool.Setup(p => p.Rent(int.MaxValue)).Returns(largeArray);

        // Act & Assert
        using var writer = new ResizableByteWriter(mockPool.Object, initialCapacity: int.MaxValue);

        mockPool.Verify(p => p.Rent(int.MaxValue), Times.Once);
        Assert.Equal(0, writer.Length);
    }

    /// <summary>
    /// Tests that the constructor validates pool when initialCapacity is zero.
    /// Input: pool = null, initialCapacity = 0
    /// Expected: ArgumentNullException is thrown.
    /// </summary>
    [Fact]
    public void Constructor_WithNullPoolAndZeroCapacity_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new ResizableByteWriter(null!, initialCapacity: 0));
        Assert.Equal("pool", ex.ParamName);
    }

    /// <summary>
    /// Tests that the constructor validates pool when initialCapacity is positive.
    /// Input: pool = null, initialCapacity = 10
    /// Expected: ArgumentNullException is thrown.
    /// </summary>
    [Fact]
    public void Constructor_WithNullPoolAndPositiveCapacity_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new ResizableByteWriter(null!, initialCapacity: 10));
        Assert.Equal("pool", ex.ParamName);
    }

    /// <summary>
    /// Tests that the parameterless constructor uses ArrayPool.Shared and creates an empty buffer.
    /// Input: No parameters (default constructor)
    /// Expected: Object created successfully with shared pool, WrittenSpan is empty
    /// </summary>
    [Fact]
    public void Constructor_Parameterless_UsesSharedPoolWithZeroCapacity()
    {
        // Arrange & Act
        using var writer = new ResizableByteWriter();

        // Assert
        Assert.Equal(0, writer.Length);
        Assert.Empty(writer.WrittenSpan.ToArray());
        Assert.True(writer.CanWrite);
    }

    /// <summary>
    /// Tests that the single-parameter constructor uses ArrayPool.Shared with specified capacity.
    /// Input: initialCapacity = 32
    /// Expected: Object created successfully, can write to buffer
    /// </summary>
    [Fact]
    public void Constructor_WithOnlyInitialCapacity_UsesSharedPool()
    {
        // Arrange & Act
        using var writer = new ResizableByteWriter(initialCapacity: 32);

        // Assert
        Assert.Equal(0, writer.Length);
        Assert.Empty(writer.WrittenSpan.ToArray());

        // Verify we can write to the buffer
        writer.WriteByte(42);
        Assert.Equal(1, writer.Length);
        Assert.Equal(new byte[] { 42 }, writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Tests boundary value of initialCapacity = 1 (minimum positive value).
    /// Input: initialCapacity = 1
    /// Expected: Buffer rented from pool, can write at least one byte
    /// </summary>
    [Fact]
    public void Constructor_WithInitialCapacityOne_RentsMinimalBuffer()
    {
        // Arrange
        var mockPool = new Mock<ArrayPool<byte>>();
        var rentedArray = new byte[1];
        mockPool.Setup(p => p.Rent(1)).Returns(rentedArray);

        // Act
        using var writer = new ResizableByteWriter(mockPool.Object, initialCapacity: 1);

        // Assert
        mockPool.Verify(p => p.Rent(1), Times.Once);
        Assert.Equal(0, writer.Length);
    }

    /// <summary>
    /// Tests that after construction, the writer is in a valid initial state.
    /// Input: initialCapacity = 8
    /// Expected: All stream capabilities are correct, position not supported, length is 0
    /// </summary>
    [Fact]
    public void Constructor_InitializesValidInitialState()
    {
        // Arrange & Act
        using var writer = new ResizableByteWriter(ArrayPool<byte>.Shared, initialCapacity: 8);

        // Assert
        Assert.False(writer.CanRead);
        Assert.False(writer.CanSeek);
        Assert.True(writer.CanWrite);
        Assert.Equal(0, writer.Length);
        Assert.Empty(writer.WrittenSpan.ToArray());

        // Position property should throw NotSupportedException
        Assert.Throws<NotSupportedException>(() => _ = writer.Position);
        Assert.Throws<NotSupportedException>(() => writer.Position = 0);
    }

    /// <summary>
    /// Verifies that CanRead always returns false for a newly constructed instance,
    /// regardless of which constructor overload is used.
    /// </summary>
    [Fact]
    public void CanRead_DefaultConstructor_ReturnsFalse()
    {
        // Arrange & Act
        using var writer = new ResizableByteWriter();

        // Assert
        Assert.False(writer.CanRead);
    }

    /// <summary>
    /// Verifies that CanRead returns false when constructed with an initial capacity.
    /// </summary>
    /// <param name="initialCapacity">The initial capacity to use.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(1024)]
    public void CanRead_ConstructorWithCapacity_ReturnsFalse(int initialCapacity)
    {
        // Arrange & Act
        using var writer = new ResizableByteWriter(initialCapacity);

        // Assert
        Assert.False(writer.CanRead);
    }

    /// <summary>
    /// Verifies that CanRead returns false when constructed with a custom ArrayPool.
    /// </summary>
    [Fact]
    public void CanRead_ConstructorWithPool_ReturnsFalse()
    {
        // Arrange
        var pool = ArrayPool<byte>.Shared;

        // Act
        using var writer = new ResizableByteWriter(pool);

        // Assert
        Assert.False(writer.CanRead);
    }

    /// <summary>
    /// Verifies that CanRead remains false after write operations are performed.
    /// </summary>
    [Fact]
    public void CanRead_AfterWriteOperations_RemainsFalse()
    {
        // Arrange
        using var writer = new ResizableByteWriter();

        // Act
        writer.WriteByte(42);
        var afterSingleByte = writer.CanRead;

        writer.Write(new byte[] { 1, 2, 3 });
        var afterArrayWrite = writer.CanRead;

        writer.Write(new ReadOnlySpan<byte>(new byte[] { 4, 5, 6 }));
        var afterSpanWrite = writer.CanRead;

        // Assert
        Assert.False(afterSingleByte);
        Assert.False(afterArrayWrite);
        Assert.False(afterSpanWrite);
    }

    /// <summary>
    /// Verifies that CanRead remains false after Reset is called.
    /// </summary>
    [Fact]
    public void CanRead_AfterReset_RemainsFalse()
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        writer.Write(new byte[] { 1, 2, 3 });

        // Act
        writer.Reset();

        // Assert
        Assert.False(writer.CanRead);
    }

    /// <summary>
    /// Verifies that CanRead remains false after Flush operations.
    /// </summary>
    [Fact]
    public void CanRead_AfterFlush_RemainsFalse()
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        writer.WriteByte(1);

        // Act
        writer.Flush();

        // Assert
        Assert.False(writer.CanRead);
    }

    /// <summary>
    /// Verifies that CanRead remains false after IBufferWriter operations (GetSpan/Advance).
    /// </summary>
    [Fact]
    public void CanRead_AfterBufferWriterOperations_RemainsFalse()
    {
        // Arrange
        using var writer = new ResizableByteWriter();

        // Act
        var span = writer.GetSpan(10);
        span[0] = 100;
        writer.Advance(1);

        // Assert
        Assert.False(writer.CanRead);
    }

    /// <summary>
    /// Verifies that CanSeek returns false for a newly created instance.
    /// </summary>
    [Fact]
    public void CanSeek_NewInstance_ReturnsFalse()
    {
        // Arrange
        using var writer = new ResizableByteWriter();

        // Act
        bool canSeek = writer.CanSeek;

        // Assert
        Assert.False(canSeek);
    }

    /// <summary>
    /// Verifies that CanSeek returns false after writing data to the buffer.
    /// </summary>
    [Fact]
    public void CanSeek_AfterWritingData_ReturnsFalse()
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        writer.WriteByte(42);
        writer.Write(new byte[] { 1, 2, 3 });

        // Act
        bool canSeek = writer.CanSeek;

        // Assert
        Assert.False(canSeek);
    }

    /// <summary>
    /// Verifies that CanSeek returns false after resetting the writer.
    /// </summary>
    [Fact]
    public void CanSeek_AfterReset_ReturnsFalse()
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        writer.WriteByte(100);
        writer.Reset();

        // Act
        bool canSeek = writer.CanSeek;

        // Assert
        Assert.False(canSeek);
    }

    /// <summary>
    /// Verifies that CanSeek returns false after disposing the writer.
    /// Note: The property does not check disposed state, so it still returns false.
    /// </summary>
    [Fact]
    public void CanSeek_AfterDispose_ReturnsFalse()
    {
        // Arrange
        var writer = new ResizableByteWriter();
        writer.Dispose();

        // Act
        bool canSeek = writer.CanSeek;

        // Assert
        Assert.False(canSeek);
    }

    /// <summary>
    /// Verifies that CanSeek returns false when initialized with a specific capacity.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(16)]
    [InlineData(1024)]
    public void CanSeek_WithInitialCapacity_ReturnsFalse(int initialCapacity)
    {
        // Arrange
        using var writer = new ResizableByteWriter(initialCapacity);

        // Act
        bool canSeek = writer.CanSeek;

        // Assert
        Assert.False(canSeek);
    }

    /// <summary>
    /// Verifies that CanSeek returns false when using a custom array pool.
    /// </summary>
    [Fact]
    public void CanSeek_WithCustomPool_ReturnsFalse()
    {
        // Arrange
        var pool = ArrayPool<byte>.Shared;
        using var writer = new ResizableByteWriter(pool, 10);

        // Act
        bool canSeek = writer.CanSeek;

        // Assert
        Assert.False(canSeek);
    }

    /// <summary>
    /// Tests that FlushAsync returns a completed task with CancellationToken.None.
    /// </summary>
    [Fact]
    public async Task FlushAsync_WithNoneCancellationToken_ReturnsCompletedTask()
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        writer.WriteByte(1);

        // Act
        Task result = writer.FlushAsync(CancellationToken.None);

        // Assert
        Assert.True(result.IsCompleted);
        await result;
        Assert.Equal(new byte[] { 1 }, writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Tests that FlushAsync does not throw when passed an already cancelled token.
    /// The method ignores the cancellation token and returns a completed task.
    /// </summary>
    [Fact]
    public async Task FlushAsync_WithCancelledToken_ReturnsCompletedTaskWithoutThrowing()
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        writer.WriteByte(42);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Task result = writer.FlushAsync(cts.Token);

        // Assert
        Assert.True(result.IsCompleted);
        await result; // Should not throw OperationCanceledException
        Assert.Equal(new byte[] { 42 }, writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Tests that FlushAsync does not modify the writer state or written data.
    /// </summary>
    [Fact]
    public async Task FlushAsync_DoesNotModifyWriterState()
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        byte[] data = { 10, 20, 30 };
        writer.Write(data);
        long lengthBefore = writer.Length;
        byte[] writtenBefore = writer.WrittenSpan.ToArray();

        // Act
        await writer.FlushAsync(CancellationToken.None);

        // Assert
        Assert.Equal(lengthBefore, writer.Length);
        Assert.Equal(writtenBefore, writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Tests that FlushAsync can be called multiple times consecutively without errors.
    /// </summary>
    [Fact]
    public async Task FlushAsync_CalledMultipleTimes_Succeeds()
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        writer.WriteByte(5);

        // Act & Assert
        await writer.FlushAsync(CancellationToken.None);
        await writer.FlushAsync(CancellationToken.None);
        await writer.FlushAsync(CancellationToken.None);

        Assert.Equal(new byte[] { 5 }, writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Tests that FlushAsync works on an empty writer with no data written.
    /// </summary>
    [Fact]
    public async Task FlushAsync_OnEmptyWriter_ReturnsCompletedTask()
    {
        // Arrange
        using var writer = new ResizableByteWriter();

        // Act
        Task result = writer.FlushAsync(CancellationToken.None);

        // Assert
        Assert.True(result.IsCompleted);
        await result;
        Assert.Equal(0, writer.Length);
    }

    /// <summary>
    /// Tests that FlushAsync returns the same completed task instance (Task.CompletedTask).
    /// </summary>
    [Fact]
    public async Task FlushAsync_ReturnsSameCompletedTaskInstance()
    {
        // Arrange
        using var writer1 = new ResizableByteWriter();
        using var writer2 = new ResizableByteWriter();

        // Act
        Task result1 = writer1.FlushAsync(CancellationToken.None);
        Task result2 = writer2.FlushAsync(CancellationToken.None);

        // Assert
        Assert.Same(Task.CompletedTask, result1);
        Assert.Same(Task.CompletedTask, result2);
        await result1;
        await result2;
    }

    /// <summary>
    /// Verifies that SetLength always throws NotSupportedException for all possible long values,
    /// including zero, positive, negative, and boundary values.
    /// </summary>
    /// <param name="value">The length value to test.</param>
    [Theory]
    [InlineData(0L)]
    [InlineData(1L)]
    [InlineData(100L)]
    [InlineData(-1L)]
    [InlineData(-100L)]
    [InlineData(long.MinValue)]
    [InlineData(long.MaxValue)]
    public void SetLength_AnyValue_ThrowsNotSupportedException(long value)
    {
        // Arrange
        using var writer = new ResizableByteWriter();

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => writer.SetLength(value));
    }

    /// <summary>
    /// Verifies that SetLength throws NotSupportedException even when called on a writer
    /// that has already been disposed.
    /// </summary>
    [Fact]
    public void SetLength_AfterDispose_ThrowsNotSupportedException()
    {
        // Arrange
        var writer = new ResizableByteWriter();
        writer.Dispose();

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => writer.SetLength(0));
    }

    /// <summary>
    /// Verifies that SetLength throws NotSupportedException even when the writer
    /// contains data.
    /// </summary>
    [Fact]
    public void SetLength_WithExistingData_ThrowsNotSupportedException()
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        writer.WriteByte(1);
        writer.WriteByte(2);
        writer.WriteByte(3);

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => writer.SetLength(1));
    }

    /// <summary>
    /// Tests that the Read method throws NotSupportedException with valid parameters.
    /// The Read operation is not supported by ResizableByteWriter as it's a write-only stream.
    /// </summary>
    [Fact]
    public void Read_ValidParameters_ThrowsNotSupportedException()
    {
        // Arrange
        using var w = new ResizableByteWriter();
        var buffer = new byte[10];

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => w.Read(buffer, 0, 5));
    }

    /// <summary>
    /// Tests that the Read method throws NotSupportedException for various offset and count combinations.
    /// Validates that the exception is thrown regardless of valid parameter values.
    /// </summary>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 10)]
    [InlineData(5, 5)]
    [InlineData(10, 0)]
    public void Read_VariousValidOffsetAndCount_ThrowsNotSupportedException(int offset, int count)
    {
        // Arrange
        using var w = new ResizableByteWriter();
        var buffer = new byte[10];

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => w.Read(buffer, offset, count));
    }

    /// <summary>
    /// Tests that the Read method throws NotSupportedException even with edge case numeric values.
    /// The method throws before any parameter validation, so invalid parameters still result in NotSupportedException.
    /// </summary>
    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(int.MinValue, 0)]
    [InlineData(0, int.MinValue)]
    [InlineData(int.MaxValue, 0)]
    [InlineData(0, int.MaxValue)]
    [InlineData(-1, -1)]
    [InlineData(int.MaxValue, int.MaxValue)]
    public void Read_EdgeCaseNumericParameters_ThrowsNotSupportedException(int offset, int count)
    {
        // Arrange
        using var w = new ResizableByteWriter();
        var buffer = new byte[10];

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => w.Read(buffer, offset, count));
    }

    /// <summary>
    /// Tests that the Read method throws NotSupportedException with an empty buffer.
    /// Even with no capacity to read, the NotSupportedException is still thrown.
    /// </summary>
    [Fact]
    public void Read_EmptyBuffer_ThrowsNotSupportedException()
    {
        // Arrange
        using var w = new ResizableByteWriter();
        var buffer = Array.Empty<byte>();

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => w.Read(buffer, 0, 0));
    }

    /// <summary>
    /// Tests that the Read method throws NotSupportedException after data has been written to the writer.
    /// The presence of written data doesn't change the unsupported nature of the Read operation.
    /// </summary>
    [Fact]
    public void Read_AfterWritingData_ThrowsNotSupportedException()
    {
        // Arrange
        using var w = new ResizableByteWriter();
        w.WriteByte(42);
        w.Write(new byte[] { 1, 2, 3 });
        var buffer = new byte[10];

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => w.Read(buffer, 0, 5));
    }

    /// <summary>
    /// Tests that the Read method throws NotSupportedException even after the writer has been disposed.
    /// NotSupportedException takes precedence over ObjectDisposedException.
    /// </summary>
    [Fact]
    public void Read_AfterDispose_ThrowsNotSupportedException()
    {
        // Arrange
        var w = new ResizableByteWriter();
        w.Dispose();
        var buffer = new byte[10];

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => w.Read(buffer, 0, 5));
    }

    /// <summary>
    /// Tests that the Read method throws NotSupportedException with different buffer sizes.
    /// Tests with single-element and large buffers to ensure consistent behavior.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(10000)]
    public void Read_DifferentBufferSizes_ThrowsNotSupportedException(int bufferSize)
    {
        // Arrange
        using var w = new ResizableByteWriter();
        var buffer = new byte[bufferSize];

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => w.Read(buffer, 0, bufferSize));
    }

    /// <summary>
    /// Tests that the Read method throws NotSupportedException with offset exceeding buffer length.
    /// Even with invalid offset values, NotSupportedException is thrown before validation.
    /// </summary>
    [Fact]
    public void Read_OffsetExceedsBufferLength_ThrowsNotSupportedException()
    {
        // Arrange
        using var w = new ResizableByteWriter();
        var buffer = new byte[5];

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => w.Read(buffer, 10, 1));
    }

    /// <summary>
    /// Tests that the Read method throws NotSupportedException when count exceeds buffer capacity.
    /// Even with invalid count values, NotSupportedException is thrown before validation.
    /// </summary>
    [Fact]
    public void Read_CountExceedsBufferCapacity_ThrowsNotSupportedException()
    {
        // Arrange
        using var w = new ResizableByteWriter();
        var buffer = new byte[5];

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => w.Read(buffer, 0, 100));
    }

    /// <summary>
    /// Tests that Reset can be called on a newly created writer without any prior writes.
    /// Verifies that Reset is safe to call when _index is already 0.
    /// </summary>
    [Fact]
    public void Reset_OnNewWriter_Succeeds()
    {
        // Arrange
        using var writer = new ResizableByteWriter();

        // Act
        writer.Reset();

        // Assert
        Assert.Equal(0, writer.Length);
        Assert.Equal(0, writer.WrittenSpan.Length);
    }

    /// <summary>
    /// Tests that Reset can be called multiple times consecutively.
    /// Verifies that Reset is idempotent and doesn't cause errors when called repeatedly.
    /// </summary>
    [Fact]
    public void Reset_MultipleTimes_IsIdempotent()
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        writer.Write(new byte[] { 1, 2, 3, 4 });

        // Act
        writer.Reset();
        writer.Reset();
        writer.Reset();

        // Assert
        Assert.Equal(0, writer.Length);
        Assert.Equal(0, writer.WrittenSpan.Length);
    }

    /// <summary>
    /// Tests that Reset clears the reservation made by GetMemory.
    /// Verifies that _available is set to 0, preventing advances after reset.
    /// </summary>
    [Fact]
    public void Reset_AfterGetMemory_ClearsReservation()
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        var memory = writer.GetMemory(16);

        // Act
        writer.Reset();

        // Assert
        var ex = Assert.Throws<InvalidOperationException>(() => writer.Advance(1));
        Assert.Equal("Cannot advance past the end of the reserved buffer segment.", ex.Message);
        Assert.Equal(0, writer.Length);
    }

    /// <summary>
    /// Tests that Reset works correctly after writing a single byte using WriteByte.
    /// Verifies that Reset clears both the index and the data written via WriteByte.
    /// </summary>
    [Fact]
    public void Reset_AfterWriteByte_ClearsData()
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        writer.WriteByte(42);
        writer.WriteByte(84);

        // Act
        writer.Reset();

        // Assert
        Assert.Equal(0, writer.Length);
        Assert.Equal(0, writer.WrittenSpan.Length);
    }

    /// <summary>
    /// Tests that Reset on an empty writer with initial capacity works correctly.
    /// Verifies Reset doesn't fail when called on a writer with allocated capacity but no data.
    /// </summary>
    [Fact]
    public void Reset_OnWriterWithInitialCapacity_Succeeds()
    {
        // Arrange
        using var writer = new ResizableByteWriter(initialCapacity: 64);

        // Act
        writer.Reset();

        // Assert
        Assert.Equal(0, writer.Length);
        Assert.Equal(0, writer.WrittenSpan.Length);
    }

    /// <summary>
    /// Tests that Reset followed by writes produces correct data.
    /// Verifies that after Reset, new writes start from position 0 and don't corrupt the buffer.
    /// </summary>
    [Fact]
    public void Reset_ThenWrite_ProducesCorrectData()
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        writer.Write(new byte[] { 1, 2, 3, 4, 5 });

        // Act
        writer.Reset();
        writer.WriteByte(99);

        // Assert
        Assert.Equal(1, writer.Length);
        Assert.Equal(new byte[] { 99 }, writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Tests that WrittenSpan returns an empty span when no data has been written.
    /// </summary>
    [Fact]
    public void WrittenSpan_NoDataWritten_ReturnsEmptySpan()
    {
        // Arrange
        using var writer = new ResizableByteWriter();

        // Act
        var span = writer.WrittenSpan;

        // Assert
        Assert.True(span.IsEmpty);
        Assert.Equal(0, span.Length);
    }

    /// <summary>
    /// Tests that WrittenSpan returns the correct span after writing a single byte.
    /// </summary>
    [Fact]
    public void WrittenSpan_SingleByteWritten_ReturnsCorrectSpan()
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        byte expectedByte = 42;
        writer.WriteByte(expectedByte);

        // Act
        var span = writer.WrittenSpan;

        // Assert
        Assert.Equal(1, span.Length);
        Assert.Equal(expectedByte, span[0]);
    }

    /// <summary>
    /// Tests that WrittenSpan returns the correct span after writing multiple bytes.
    /// </summary>
    [Fact]
    public void WrittenSpan_MultipleBytesWritten_ReturnsCorrectSpan()
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        byte[] data = { 1, 2, 3, 4, 5 };
        writer.Write(data);

        // Act
        var span = writer.WrittenSpan;

        // Assert
        Assert.Equal(data.Length, span.Length);
        for (int i = 0; i < data.Length; i++)
        {
            Assert.Equal(data[i], span[i]);
        }
    }

    /// <summary>
    /// Tests that WrittenSpan accumulates data from multiple write operations.
    /// </summary>
    [Fact]
    public void WrittenSpan_MultipleWrites_AccumulatesData()
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        byte[] firstData = { 10, 20, 30 };
        byte[] secondData = { 40, 50 };
        writer.Write(firstData);
        writer.Write(secondData);

        // Act
        var span = writer.WrittenSpan;

        // Assert
        Assert.Equal(5, span.Length);
        Assert.Equal(10, span[0]);
        Assert.Equal(20, span[1]);
        Assert.Equal(30, span[2]);
        Assert.Equal(40, span[3]);
        Assert.Equal(50, span[4]);
    }

    /// <summary>
    /// Tests that WrittenSpan returns an empty span after Reset is called.
    /// </summary>
    [Fact]
    public void WrittenSpan_AfterReset_ReturnsEmptySpan()
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        writer.Write(new byte[] { 1, 2, 3 });
        writer.Reset();

        // Act
        var span = writer.WrittenSpan;

        // Assert
        Assert.True(span.IsEmpty);
        Assert.Equal(0, span.Length);
    }

    /// <summary>
    /// Tests that WrittenSpan returns correct data after the buffer grows.
    /// </summary>
    [Fact]
    public void WrittenSpan_AfterBufferGrows_ReturnsCorrectData()
    {
        // Arrange
        using var writer = new ResizableByteWriter(initialCapacity: 4);
        byte[] largeData = new byte[100];
        for (int i = 0; i < largeData.Length; i++)
        {
            largeData[i] = (byte)(i % 256);
        }
        writer.Write(largeData);

        // Act
        var span = writer.WrittenSpan;

        // Assert
        Assert.Equal(100, span.Length);
        for (int i = 0; i < largeData.Length; i++)
        {
            Assert.Equal(largeData[i], span[i]);
        }
    }

    /// <summary>
    /// Tests that WrittenSpan returns correct span with maximum byte value.
    /// </summary>
    [Fact]
    public void WrittenSpan_WithMaxByteValue_ReturnsCorrectSpan()
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        writer.WriteByte(byte.MaxValue);

        // Act
        var span = writer.WrittenSpan;

        // Assert
        Assert.Equal(1, span.Length);
        Assert.Equal(byte.MaxValue, span[0]);
    }

    /// <summary>
    /// Tests that WrittenSpan returns correct span with minimum byte value.
    /// </summary>
    [Fact]
    public void WrittenSpan_WithMinByteValue_ReturnsCorrectSpan()
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        writer.WriteByte(byte.MinValue);

        // Act
        var span = writer.WrittenSpan;

        // Assert
        Assert.Equal(1, span.Length);
        Assert.Equal(byte.MinValue, span[0]);
    }

    /// <summary>
    /// Tests that WrittenSpan returns correct span after using GetSpan and Advance.
    /// </summary>
    [Fact]
    public void WrittenSpan_AfterGetSpanAndAdvance_ReturnsCorrectData()
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        var buffer = writer.GetSpan(5);
        buffer[0] = 100;
        buffer[1] = 101;
        buffer[2] = 102;
        writer.Advance(3);

        // Act
        var span = writer.WrittenSpan;

        // Assert
        Assert.Equal(3, span.Length);
        Assert.Equal(100, span[0]);
        Assert.Equal(101, span[1]);
        Assert.Equal(102, span[2]);
    }

    /// <summary>
    /// Tests that WrittenSpan returns an empty span when initialized with zero capacity.
    /// </summary>
    [Fact]
    public void WrittenSpan_WithZeroInitialCapacity_ReturnsEmptySpan()
    {
        // Arrange
        using var writer = new ResizableByteWriter(initialCapacity: 0);

        // Act
        var span = writer.WrittenSpan;

        // Assert
        Assert.True(span.IsEmpty);
        Assert.Equal(0, span.Length);
    }

    /// <summary>
    /// Tests that WrittenSpan returns correct span when initialized with specific capacity and data written.
    /// </summary>
    [Fact]
    public void WrittenSpan_WithSpecificInitialCapacity_ReturnsCorrectSpan()
    {
        // Arrange
        using var writer = new ResizableByteWriter(initialCapacity: 10);
        byte[] data = { 1, 2, 3 };
        writer.Write(data);

        // Act
        var span = writer.WrittenSpan;

        // Assert
        Assert.Equal(3, span.Length);
        Assert.Equal(1, span[0]);
        Assert.Equal(2, span[1]);
        Assert.Equal(3, span[2]);
    }

    /// <summary>
    /// Tests that WrittenSpan can be called multiple times and returns consistent results.
    /// </summary>
    [Fact]
    public void WrittenSpan_CalledMultipleTimes_ReturnsConsistentResults()
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        writer.Write(new byte[] { 5, 10, 15 });

        // Act
        var span1 = writer.WrittenSpan;
        var span2 = writer.WrittenSpan;

        // Assert
        Assert.Equal(span1.Length, span2.Length);
        for (int i = 0; i < span1.Length; i++)
        {
            Assert.Equal(span1[i], span2[i]);
        }
    }

    /// <summary>
    /// Tests that WrittenSpan reflects changes after additional writes between accesses.
    /// </summary>
    [Fact]
    public void WrittenSpan_AccessedBetweenWrites_ReflectsChanges()
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        writer.WriteByte(1);

        // Act
        var span1 = writer.WrittenSpan;
        writer.WriteByte(2);
        var span2 = writer.WrittenSpan;

        // Assert
        Assert.Equal(1, span1.Length);
        Assert.Equal(2, span2.Length);
        Assert.Equal(1, span2[0]);
        Assert.Equal(2, span2[1]);
    }

    /// <summary>
    /// Tests that WrittenSpan works correctly with very large data.
    /// </summary>
    [Fact]
    public void WrittenSpan_WithLargeData_ReturnsCorrectSpan()
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        byte[] largeData = new byte[10000];
        for (int i = 0; i < largeData.Length; i++)
        {
            largeData[i] = (byte)(i % 256);
        }
        writer.Write(largeData);

        // Act
        var span = writer.WrittenSpan;

        // Assert
        Assert.Equal(10000, span.Length);
        for (int i = 0; i < 100; i++) // Spot check first 100
        {
            Assert.Equal((byte)(i % 256), span[i]);
        }
    }

    /// <summary>
    /// Tests that WrittenSpan returns correct data after Reset and subsequent writes.
    /// </summary>
    [Fact]
    public void WrittenSpan_AfterResetAndWrite_ReturnsNewData()
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        writer.Write(new byte[] { 1, 2, 3 });
        writer.Reset();
        writer.Write(new byte[] { 4, 5 });

        // Act
        var span = writer.WrittenSpan;

        // Assert
        Assert.Equal(2, span.Length);
        Assert.Equal(4, span[0]);
        Assert.Equal(5, span[1]);
    }

    /// <summary>
    /// Tests that WrittenSpan with empty array write returns empty span.
    /// </summary>
    [Fact]
    public void WrittenSpan_AfterWritingEmptyArray_ReturnsEmptySpan()
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        writer.Write(Array.Empty<byte>());

        // Act
        var span = writer.WrittenSpan;

        // Assert
        Assert.True(span.IsEmpty);
        Assert.Equal(0, span.Length);
    }

    /// <summary>
    /// Tests that Advance with zero count succeeds when buffer space is reserved.
    /// Verifies that advancing by zero is a valid no-op that still clears the reservation.
    /// Expected: No exception, _available is cleared, index remains unchanged.
    /// </summary>
    [Fact]
    public void Advance_ZeroCount_WithReservation_Succeeds()
    {
        // Arrange
        using var writer = new ResizableByteWriter(initialCapacity: 16);
        _ = writer.GetSpan(8); // Reserve 8 bytes

        // Act
        writer.Advance(0);

        // Assert
        Assert.Equal(0, writer.Length);
        Assert.Equal(0, writer.WrittenSpan.Length);
    }

    /// <summary>
    /// Tests that Advance with count equal to available space succeeds.
    /// Verifies exact match between requested advance and reserved space.
    /// Expected: Index advances by exact amount, reservation is cleared.
    /// </summary>
    [Fact]
    public void Advance_CountEqualsAvailable_Succeeds()
    {
        // Arrange
        using var writer = new ResizableByteWriter(initialCapacity: 16);
        var span = writer.GetSpan(10);
        for (int i = 0; i < 10; i++)
        {
            span[i] = (byte)(i + 1);
        }

        // Act
        writer.Advance(10);

        // Assert
        Assert.Equal(10, writer.Length);
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Tests that Advance with count less than available space succeeds.
    /// Verifies partial advance within reserved buffer space.
    /// Expected: Index advances by specified count, reservation is cleared.
    /// </summary>
    [Fact]
    public void Advance_CountLessThanAvailable_Succeeds()
    {
        // Arrange
        using var writer = new ResizableByteWriter(initialCapacity: 16);
        var span = writer.GetSpan(10);
        for (int i = 0; i < 5; i++)
        {
            span[i] = (byte)(i + 1);
        }

        // Act
        writer.Advance(5); // Advance only 5 out of 10 reserved

        // Assert
        Assert.Equal(5, writer.Length);
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Tests that Advance throws when count exceeds available space by exactly one.
    /// Verifies boundary validation for over-advancing.
    /// Expected: InvalidOperationException with appropriate message.
    /// </summary>
    [Fact]
    public void Advance_CountExceedsAvailableByOne_ThrowsInvalidOperationException()
    {
        // Arrange
        using var writer = new ResizableByteWriter(initialCapacity: 16);
        _ = writer.GetSpan(10); // Reserve 10 bytes

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => writer.Advance(11));
        Assert.Contains("Cannot advance past the end of the reserved buffer segment", ex.Message);
    }

    /// <summary>
    /// Tests that multiple Advance calls without new reservation throw.
    /// Verifies that reservation is cleared after first advance and cannot be reused.
    /// Expected: Second advance throws InvalidOperationException.
    /// </summary>
    [Fact]
    public void Advance_CalledTwiceWithoutNewReservation_ThrowsInvalidOperationException()
    {
        // Arrange
        using var writer = new ResizableByteWriter(initialCapacity: 16);
        _ = writer.GetSpan(10);
        writer.Advance(5);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => writer.Advance(1));
        Assert.Contains("Cannot advance past the end of the reserved buffer segment", ex.Message);
    }

    /// <summary>
    /// Tests that Advance with int.MaxValue throws when no sufficient reservation exists.
    /// Verifies handling of extreme numeric boundary values.
    /// Expected: InvalidOperationException due to exceeding available space.
    /// </summary>
    [Fact]
    public void Advance_IntMaxValue_ThrowsInvalidOperationException()
    {
        // Arrange
        using var writer = new ResizableByteWriter(initialCapacity: 16);
        _ = writer.GetSpan(8);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => writer.Advance(int.MaxValue));
        Assert.Contains("Cannot advance past the end of the reserved buffer segment", ex.Message);
    }

    /// <summary>
    /// Tests that Advance with int.MinValue throws ArgumentOutOfRangeException.
    /// Verifies handling of extreme negative numeric boundary values.
    /// Expected: ArgumentOutOfRangeException for negative count.
    /// </summary>
    [Fact]
    public void Advance_IntMinValue_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        using var writer = new ResizableByteWriter(initialCapacity: 16);
        _ = writer.GetSpan(8);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.Advance(int.MinValue));
    }

    /// <summary>
    /// Tests that Advance properly clears reservation after successful advance.
    /// Verifies that _available is reset to 0 after advance, preventing reuse.
    /// Expected: Subsequent advance without new GetSpan/GetMemory throws.
    /// </summary>
    [Fact]
    public void Advance_ClearsReservation_AfterSuccessfulAdvance()
    {
        // Arrange
        using var writer = new ResizableByteWriter(initialCapacity: 16);
        _ = writer.GetSpan(10);
        writer.Advance(3);

        // Act & Assert - trying to advance again without new reservation
        var ex = Assert.Throws<InvalidOperationException>(() => writer.Advance(1));
        Assert.Contains("Cannot advance past the end of the reserved buffer segment", ex.Message);
    }

    /// <summary>
    /// Tests that Advance on disposed writer throws ObjectDisposedException.
    /// Verifies disposed state validation occurs before other checks.
    /// Expected: ObjectDisposedException with appropriate object name.
    /// </summary>
    [Fact]
    public void Advance_OnDisposedWriter_ThrowsObjectDisposedException()
    {
        // Arrange
        var writer = new ResizableByteWriter(initialCapacity: 16);
        _ = writer.GetSpan(8);
        writer.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => writer.Advance(5));
    }

    /// <summary>
    /// Tests that Advance properly updates Length property.
    /// Verifies that index and Length are synchronized after advance.
    /// Expected: Length equals sum of all successful advances.
    /// </summary>
    [Fact]
    public void Advance_UpdatesLength_Correctly()
    {
        // Arrange
        using var writer = new ResizableByteWriter(initialCapacity: 32);

        // Act & Assert - First advance
        _ = writer.GetSpan(5);
        writer.Advance(5);
        Assert.Equal(5, writer.Length);

        // Act & Assert - Second advance
        _ = writer.GetSpan(10);
        writer.Advance(10);
        Assert.Equal(15, writer.Length);

        // Act & Assert - Third advance
        _ = writer.GetSpan(3);
        writer.Advance(3);
        Assert.Equal(18, writer.Length);
    }

    /// <summary>
    /// Tests parameterized scenarios for various valid advance counts.
    /// Verifies correct behavior across multiple valid input ranges.
    /// Expected: All valid counts succeed and update length correctly.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(15)]
    public void Advance_ValidCounts_SucceedAndUpdateLength(int count)
    {
        // Arrange
        using var writer = new ResizableByteWriter(initialCapacity: 32);
        _ = writer.GetSpan(20); // Reserve 20 bytes

        // Act
        writer.Advance(count);

        // Assert
        Assert.Equal(count, writer.Length);
    }

    /// <summary>
    /// Tests parameterized scenarios for various invalid negative counts.
    /// Verifies consistent exception handling for all negative values.
    /// Expected: ArgumentOutOfRangeException for all negative inputs.
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(-10)]
    [InlineData(-100)]
    [InlineData(-1000)]
    public void Advance_NegativeCounts_ThrowArgumentOutOfRangeException(int count)
    {
        // Arrange
        using var writer = new ResizableByteWriter(initialCapacity: 16);
        _ = writer.GetSpan(10);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.Advance(count));
    }

    /// <summary>
    /// Tests that Advance with zero count on zero reservation is a valid no-op.
    /// Per the IBufferWriter&lt;T&gt; contract, Advance(0) should always succeed.
    /// Expected: No exception is thrown, writer state is unchanged.
    /// </summary>
    [Fact]
    public void Advance_ZeroCountWithZeroReservation_IsNoOp()
    {
        // Arrange
        using var writer = new ResizableByteWriter(initialCapacity: 16);
        // No GetSpan/GetMemory call - _available is 0

        // Act — Advance(0) should be a valid no-op
        writer.Advance(0);

        // Assert — writer state unchanged
        Assert.Equal(0, writer.Length);
    }

    /// <summary>
    /// Tests that GetMemory throws ArgumentOutOfRangeException when sizeHint is negative.
    /// Verifies proper input validation for invalid size hints.
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void GetMemory_NegativeSizeHint_ThrowsArgumentOutOfRangeException(int negativeSizeHint)
    {
        // Arrange
        using var writer = new ResizableByteWriter();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.GetMemory(negativeSizeHint));
    }

    /// <summary>
    /// Tests that GetMemory throws OverflowException when sizeHint would cause integer overflow.
    /// Verifies that checked arithmetic in Grow properly detects overflow conditions.
    /// </summary>
    [Fact]
    public void GetMemory_SizeHintCausesOverflow_ThrowsOverflowException()
    {
        // Arrange
        using var writer = new ResizableByteWriter(initialCapacity: 16);
        writer.WriteByte(1);
        writer.WriteByte(2);

        // Act & Assert
        Assert.Throws<OverflowException>(() => writer.GetMemory(int.MaxValue));
    }

    /// <summary>
    /// Tests that GetMemory with sizeHint of 1 allocates minimal required space.
    /// Verifies boundary behavior for the smallest positive size hint.
    /// </summary>
    [Fact]
    public void GetMemory_SizeHintOne_ReturnsMemoryOfLengthOne()
    {
        // Arrange
        using var writer = new ResizableByteWriter();

        // Act
        var memory = writer.GetMemory(sizeHint: 1);

        // Assert
        Assert.Equal(1, memory.Length);
        memory.Span[0] = 42;
        writer.Advance(1);
        Assert.Equal(42, writer.WrittenSpan[0]);
    }

    /// <summary>
    /// Tests that GetMemory properly updates internal available tracking.
    /// Verifies that _available is set correctly and subsequent operations work as expected.
    /// </summary>
    [Fact]
    public void GetMemory_UpdatesAvailableTracking_AllowsCorrectAdvance()
    {
        // Arrange
        using var writer = new ResizableByteWriter();

        // Act
        var memory1 = writer.GetMemory(sizeHint: 10);
        memory1.Span[0] = 1;
        writer.Advance(10);

        var memory2 = writer.GetMemory(sizeHint: 5);
        memory2.Span[0] = 2;
        writer.Advance(5);

        // Assert
        Assert.Equal(15, writer.Length);
        Assert.Equal(1, writer.WrittenSpan[0]);
        Assert.Equal(2, writer.WrittenSpan[10]);
    }

    /// <summary>
    /// Tests that GetMemory returns memory at the correct position after previous writes.
    /// Verifies that _index tracking is maintained correctly across multiple operations.
    /// </summary>
    [Fact]
    public void GetMemory_AfterWrites_ReturnsMemoryAtCorrectPosition()
    {
        // Arrange
        using var writer = new ResizableByteWriter(initialCapacity: 16);
        writer.Write(new byte[] { 1, 2, 3, 4, 5 });

        // Act
        var memory = writer.GetMemory(sizeHint: 3);
        memory.Span[0] = 10;
        memory.Span[1] = 11;
        memory.Span[2] = 12;
        writer.Advance(3);

        // Assert
        Assert.Equal(8, writer.Length);
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 10, 11, 12 }, writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Tests that GetMemory throws ObjectDisposedException after the writer has been disposed.
    /// Verifies proper disposal state checking.
    /// </summary>
    [Fact]
    public void GetMemory_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var writer = new ResizableByteWriter(initialCapacity: 8);
        writer.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => writer.GetMemory());
    }

    /// <summary>
    /// Tests that GetMemory with large valid sizeHint triggers buffer growth.
    /// Verifies that growth mechanism works correctly for substantial size requests.
    /// </summary>
    [Fact]
    public void GetMemory_LargeSizeHint_TriggersGrowthAndWorks()
    {
        // Arrange
        using var writer = new ResizableByteWriter(initialCapacity: 8);
        writer.Write(new byte[] { 1, 2 });

        // Act
        var memory = writer.GetMemory(sizeHint: 1024);

        // Assert
        Assert.Equal(1024, memory.Length);
        memory.Span[0] = 99;
        writer.Advance(1024);
        Assert.Equal(1026, writer.Length);
        Assert.Equal(99, writer.WrittenSpan[2]);
    }

    /// <summary>
    /// Tests that consecutive GetMemory calls without Advance maintain correct state.
    /// Verifies that _available is updated on each call and previous reservations are replaced.
    /// </summary>
    [Fact]
    public void GetMemory_ConsecutiveCallsWithoutAdvance_UpdatesAvailableCorrectly()
    {
        // Arrange
        using var writer = new ResizableByteWriter();

        // Act
        var memory1 = writer.GetMemory(sizeHint: 10);
        var memory2 = writer.GetMemory(sizeHint: 5);

        // Assert
        Assert.Equal(5, memory2.Length);
        memory2.Span[0] = 42;
        writer.Advance(5);
        Assert.Equal(5, writer.Length);
        Assert.Equal(42, writer.WrittenSpan[0]);
    }

    /// <summary>
    /// Tests that GetMemory with explicit zero sizeHint defaults to 8.
    /// Verifies the DefaultSizeHint constant behavior.
    /// </summary>
    [Fact]
    public void GetMemory_ExplicitZeroSizeHint_DefaultsToEight()
    {
        // Arrange
        using var writer = new ResizableByteWriter();

        // Act
        var memory = writer.GetMemory(sizeHint: 0);

        // Assert
        Assert.Equal(8, memory.Length);
    }

    /// <summary>
    /// Tests that WriteByte correctly stores edge byte values including minimum (0) and maximum (255) values.
    /// Validates that all byte value ranges are handled correctly.
    /// </summary>
    /// <param name="value">The byte value to write.</param>
    [Theory]
    [InlineData(byte.MinValue)] // 0
    [InlineData(byte.MaxValue)] // 255
    [InlineData(1)]
    [InlineData(127)]
    [InlineData(128)]
    [InlineData(254)]
    public void WriteByte_EdgeByteValues_StoresCorrectly(byte value)
    {
        // Arrange
        using var writer = new ResizableByteWriter();

        // Act
        writer.WriteByte(value);

        // Assert
        Assert.Equal(1, writer.Length);
        Assert.Equal(value, writer.WrittenSpan[0]);
    }

    /// <summary>
    /// Tests that WriteByte works correctly when starting with an empty buffer (initialCapacity: 0).
    /// Ensures that the buffer grows automatically and stores the value correctly.
    /// </summary>
    [Fact]
    public void WriteByte_ToEmptyBuffer_GrowsAndStoresValue()
    {
        // Arrange
        using var writer = new ResizableByteWriter(initialCapacity: 0);

        // Act
        writer.WriteByte(42);

        // Assert
        Assert.Equal(1, writer.Length);
        Assert.Equal(42, writer.WrittenSpan[0]);
    }

    /// <summary>
    /// Tests that WriteByte correctly updates the Length property after each write operation.
    /// Validates that the Length reflects the number of bytes written.
    /// </summary>
    [Fact]
    public void WriteByte_UpdatesLengthProperty_AfterEachWrite()
    {
        // Arrange
        using var writer = new ResizableByteWriter();

        // Act & Assert
        Assert.Equal(0, writer.Length);

        writer.WriteByte(1);
        Assert.Equal(1, writer.Length);

        writer.WriteByte(2);
        Assert.Equal(2, writer.Length);

        writer.WriteByte(3);
        Assert.Equal(3, writer.Length);
    }

    /// <summary>
    /// Tests that WriteByte correctly triggers multiple buffer growths when writing many bytes sequentially.
    /// Ensures that data is preserved across growth operations and all bytes are stored correctly.
    /// </summary>
    [Fact]
    public void WriteByte_ManySequentialWrites_TriggersGrowthAndPreservesData()
    {
        // Arrange
        using var writer = new ResizableByteWriter(initialCapacity: 1);
        const int count = 100;

        // Act
        for (int i = 0; i < count; i++)
        {
            writer.WriteByte((byte)(i % 256));
        }

        // Assert
        Assert.Equal(count, writer.Length);
        var written = writer.WrittenSpan;
        for (int i = 0; i < count; i++)
        {
            Assert.Equal((byte)(i % 256), written[i]);
        }
    }

    /// <summary>
    /// Tests that WriteByte works correctly after calling Reset.
    /// Ensures that the writer starts from index 0 and overwrites previous data.
    /// </summary>
    [Fact]
    public void WriteByte_AfterReset_StartsAtIndexZero()
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        writer.WriteByte(100);
        writer.WriteByte(200);
        Assert.Equal(2, writer.Length);

        // Act
        writer.Reset();
        writer.WriteByte(42);

        // Assert
        Assert.Equal(1, writer.Length);
        Assert.Equal(42, writer.WrittenSpan[0]);
    }

    /// <summary>
    /// Tests that WriteByte invalidates any outstanding GetMemory reservation.
    /// Ensures that attempting to advance after a direct write throws InvalidOperationException.
    /// </summary>
    [Fact]
    public void WriteByte_AfterGetMemory_InvalidatesReservation()
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        var memory = writer.GetMemory(16);

        // Act
        writer.WriteByte(99);

        // Assert
        var ex = Assert.Throws<InvalidOperationException>(() => writer.Advance(1));
        Assert.Equal("Cannot advance past the end of the reserved buffer segment.", ex.Message);
        Assert.Equal(new byte[] { 99 }, writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Tests that WriteByte throws ObjectDisposedException when called after disposal.
    /// Ensures proper resource management and prevents use-after-dispose errors.
    /// </summary>
    [Fact]
    public void WriteByte_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var writer = new ResizableByteWriter();
        writer.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => writer.WriteByte(1));
    }

    /// <summary>
    /// Tests that WriteByte correctly writes a sequence of all possible byte values.
    /// Validates that the full byte range (0-255) is handled correctly.
    /// </summary>
    [Fact]
    public void WriteByte_AllByteValues_StoresCorrectly()
    {
        // Arrange
        using var writer = new ResizableByteWriter();

        // Act
        for (int i = 0; i <= 255; i++)
        {
            writer.WriteByte((byte)i);
        }

        // Assert
        Assert.Equal(256, writer.Length);
        var written = writer.WrittenSpan;
        for (int i = 0; i <= 255; i++)
        {
            Assert.Equal((byte)i, written[i]);
        }
    }

    /// <summary>
    /// Tests that Write(ReadOnlyMemory{byte}) correctly writes an empty memory block.
    /// Expected: No data written, Length remains 0.
    /// </summary>
    [Fact]
    public void Write_ReadOnlyMemory_EmptyMemory_WritesNothing()
    {
        // Arrange
        using var writer = new ResizableByteWriter(initialCapacity: 8);
        var emptyMemory = ReadOnlyMemory<byte>.Empty;

        // Act
        writer.Write(emptyMemory);

        // Assert
        Assert.Equal(0, writer.Length);
        Assert.Empty(writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Tests that Write(ReadOnlyMemory{byte}) correctly writes a single byte.
    /// Expected: One byte written, Length is 1, content matches.
    /// </summary>
    [Fact]
    public void Write_ReadOnlyMemory_SingleByte_WritesCorrectly()
    {
        // Arrange
        using var writer = new ResizableByteWriter(initialCapacity: 8);
        var memory = new ReadOnlyMemory<byte>(new byte[] { 42 });

        // Act
        writer.Write(memory);

        // Assert
        Assert.Equal(1, writer.Length);
        Assert.Equal(new byte[] { 42 }, writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Tests that Write(ReadOnlyMemory{byte}) correctly writes multiple bytes.
    /// Expected: All bytes written in order, Length matches, content matches.
    /// </summary>
    [Fact]
    public void Write_ReadOnlyMemory_MultipleBytes_WritesCorrectly()
    {
        // Arrange
        using var writer = new ResizableByteWriter(initialCapacity: 8);
        var memory = new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3, 4, 5 });

        // Act
        writer.Write(memory);

        // Assert
        Assert.Equal(5, writer.Length);
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Tests that Write(ReadOnlyMemory{byte}) correctly appends data on consecutive writes.
    /// Expected: Data appended in order, Length accumulates, content matches all writes.
    /// </summary>
    [Fact]
    public void Write_ReadOnlyMemory_ConsecutiveWrites_AppendsCorrectly()
    {
        // Arrange
        using var writer = new ResizableByteWriter(initialCapacity: 8);
        var memory1 = new ReadOnlyMemory<byte>(new byte[] { 10, 20 });
        var memory2 = new ReadOnlyMemory<byte>(new byte[] { 30, 40, 50 });

        // Act
        writer.Write(memory1);
        writer.Write(memory2);

        // Assert
        Assert.Equal(5, writer.Length);
        Assert.Equal(new byte[] { 10, 20, 30, 40, 50 }, writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Tests that Write(ReadOnlyMemory{byte}) triggers buffer growth when capacity is exceeded.
    /// Expected: Buffer grows, all data preserved, new data appended correctly.
    /// </summary>
    [Fact]
    public void Write_ReadOnlyMemory_ExceedsCapacity_GrowsAndPreservesData()
    {
        // Arrange
        using var writer = new ResizableByteWriter(initialCapacity: 2);
        var memory1 = new ReadOnlyMemory<byte>(new byte[] { 1, 2 });
        var memory2 = new ReadOnlyMemory<byte>(new byte[] { 3, 4, 5, 6 });

        // Act
        writer.Write(memory1);
        writer.Write(memory2);

        // Assert
        Assert.Equal(6, writer.Length);
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 6 }, writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Tests that Write(ReadOnlyMemory{byte}) correctly handles a large memory block.
    /// Expected: Large data written correctly, Length matches, content matches.
    /// </summary>
    [Fact]
    public void Write_ReadOnlyMemory_LargeBlock_WritesCorrectly()
    {
        // Arrange
        using var writer = new ResizableByteWriter(initialCapacity: 16);
        var largeData = new byte[1024];
        for (int i = 0; i < largeData.Length; i++)
        {
            largeData[i] = (byte)(i % 256);
        }
        var memory = new ReadOnlyMemory<byte>(largeData);

        // Act
        writer.Write(memory);

        // Assert
        Assert.Equal(1024, writer.Length);
        Assert.Equal(largeData, writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Tests that Write(ReadOnlyMemory{byte}) works correctly with a sliced memory.
    /// Expected: Only the sliced portion is written, Length matches, content matches.
    /// </summary>
    [Fact]
    public void Write_ReadOnlyMemory_SlicedMemory_WritesOnlySlice()
    {
        // Arrange
        using var writer = new ResizableByteWriter(initialCapacity: 8);
        var array = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var memory = new ReadOnlyMemory<byte>(array, 2, 3); // [3, 4, 5]

        // Act
        writer.Write(memory);

        // Assert
        Assert.Equal(3, writer.Length);
        Assert.Equal(new byte[] { 3, 4, 5 }, writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Tests that Write(ReadOnlyMemory{byte}) throws ObjectDisposedException when called after disposal.
    /// Expected: ObjectDisposedException thrown.
    /// </summary>
    [Fact]
    public void Write_ReadOnlyMemory_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var writer = new ResizableByteWriter(initialCapacity: 8);
        writer.Dispose();
        var memory = new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3 });

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => writer.Write(memory));
    }

    /// <summary>
    /// Tests that Write(ReadOnlyMemory{byte}) works correctly after Reset.
    /// Expected: Previous data cleared, new data written from position 0, content matches.
    /// </summary>
    [Fact]
    public void Write_ReadOnlyMemory_AfterReset_WritesFromBeginning()
    {
        // Arrange
        using var writer = new ResizableByteWriter(initialCapacity: 8);
        var memory1 = new ReadOnlyMemory<byte>(new byte[] { 10, 20, 30 });
        var memory2 = new ReadOnlyMemory<byte>(new byte[] { 40, 50 });
        writer.Write(memory1);

        // Act
        writer.Reset();
        writer.Write(memory2);

        // Assert
        Assert.Equal(2, writer.Length);
        Assert.Equal(new byte[] { 40, 50 }, writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Tests that Write(ReadOnlyMemory{byte}) handles writing all byte values correctly.
    /// Expected: All byte values from 0 to 255 written correctly.
    /// </summary>
    [Fact]
    public void Write_ReadOnlyMemory_AllByteValues_WritesCorrectly()
    {
        // Arrange
        using var writer = new ResizableByteWriter(initialCapacity: 256);
        var allBytes = new byte[256];
        for (int i = 0; i < 256; i++)
        {
            allBytes[i] = (byte)i;
        }
        var memory = new ReadOnlyMemory<byte>(allBytes);

        // Act
        writer.Write(memory);

        // Assert
        Assert.Equal(256, writer.Length);
        Assert.Equal(allBytes, writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Tests that writing an empty span does not change the buffer length or content.
    /// Input: Empty ReadOnlySpan&lt;byte&gt;.
    /// Expected: Length remains 0, no data written.
    /// </summary>
    [Fact]
    public void Write_EmptySpan_DoesNotChangeLength()
    {
        // Arrange
        using var writer = new ResizableByteWriter(initialCapacity: 4);
        ReadOnlySpan<byte> emptySpan = ReadOnlySpan<byte>.Empty;

        // Act
        writer.Write(emptySpan);

        // Assert
        Assert.Equal(0, writer.Length);
        Assert.Empty(writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Tests that writing an empty span after existing data preserves the existing data.
    /// Input: Initial data [1, 2], then empty span.
    /// Expected: Length remains 2, data is [1, 2].
    /// </summary>
    [Fact]
    public void Write_EmptySpanAfterData_PreservesExistingData()
    {
        // Arrange
        using var writer = new ResizableByteWriter(initialCapacity: 4);
        writer.Write([1, 2]);
        ReadOnlySpan<byte> emptySpan = ReadOnlySpan<byte>.Empty;

        // Act
        writer.Write(emptySpan);

        // Assert
        Assert.Equal(2, writer.Length);
        Assert.Equal(new byte[] { 1, 2 }, writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Tests that writing a very large span successfully grows the buffer and writes all data.
    /// Input: Large span with 1000 bytes.
    /// Expected: All 1000 bytes are written correctly.
    /// </summary>
    [Fact]
    public void Write_VeryLargeSpan_GrowsBufferAndWritesAllData()
    {
        // Arrange
        using var writer = new ResizableByteWriter(initialCapacity: 8);
        var largeData = new byte[1000];
        for (int i = 0; i < largeData.Length; i++)
        {
            largeData[i] = (byte)(i % 256);
        }

        // Act
        writer.Write(largeData.AsSpan());

        // Assert
        Assert.Equal(1000, writer.Length);
        Assert.Equal(largeData, writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Tests that multiple consecutive writes of spans append data in correct order.
    /// Input: Three separate span writes.
    /// Expected: All data appended in order without loss.
    /// </summary>
    [Fact]
    public void Write_MultipleConsecutiveSpans_AppendsInCorrectOrder()
    {
        // Arrange
        using var writer = new ResizableByteWriter(initialCapacity: 2);
        ReadOnlySpan<byte> first = new byte[] { 1, 2, 3 };
        ReadOnlySpan<byte> second = new byte[] { 4, 5 };
        ReadOnlySpan<byte> third = new byte[] { 6, 7, 8, 9 };

        // Act
        writer.Write(first);
        writer.Write(second);
        writer.Write(third);

        // Assert
        Assert.Equal(9, writer.Length);
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Tests that writing a span exactly matching initial capacity does not trigger growth.
    /// Input: Span with length equal to initial capacity.
    /// Expected: Data written without buffer growth.
    /// </summary>
    [Fact]
    public void Write_SpanMatchingInitialCapacity_DoesNotTriggerGrowth()
    {
        // Arrange
        using var writer = new ResizableByteWriter(initialCapacity: 4);
        ReadOnlySpan<byte> data = new byte[] { 1, 2, 3, 4 };

        // Act
        writer.Write(data);

        // Assert
        Assert.Equal(4, writer.Length);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Tests that writing spans with boundary sizes (powers of 2) works correctly.
    /// Input: Consecutive writes totaling various power-of-2 sizes.
    /// Expected: All data written correctly with appropriate growth.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(16)]
    [InlineData(32)]
    [InlineData(64)]
    [InlineData(128)]
    [InlineData(256)]
    public void Write_BoundarySizes_WritesCorrectly(int size)
    {
        // Arrange
        using var writer = new ResizableByteWriter(initialCapacity: 1);
        var data = new byte[size];
        for (int i = 0; i < size; i++)
        {
            data[i] = (byte)(i % 256);
        }

        // Act
        writer.Write(data.AsSpan());

        // Assert
        Assert.Equal(size, writer.Length);
        Assert.Equal(data, writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Tests that writing a single-byte span works correctly.
    /// Input: Span containing a single byte.
    /// Expected: One byte is written.
    /// </summary>
    [Fact]
    public void Write_SingleByteSpan_WritesOneByte()
    {
        // Arrange
        using var writer = new ResizableByteWriter(initialCapacity: 4);
        ReadOnlySpan<byte> singleByte = new byte[] { 42 };

        // Act
        writer.Write(singleByte);

        // Assert
        Assert.Equal(1, writer.Length);
        Assert.Equal(new byte[] { 42 }, writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Tests that writing spans with all possible byte values works correctly.
    /// Input: Span containing bytes from 0 to 255.
    /// Expected: All byte values are preserved.
    /// </summary>
    [Fact]
    public void Write_AllByteValues_PreservesAllValues()
    {
        // Arrange
        using var writer = new ResizableByteWriter(initialCapacity: 16);
        var allBytes = new byte[256];
        for (int i = 0; i < 256; i++)
        {
            allBytes[i] = (byte)i;
        }

        // Act
        writer.Write(allBytes.AsSpan());

        // Assert
        Assert.Equal(256, writer.Length);
        Assert.Equal(allBytes, writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Tests that writing after reset appends from the beginning.
    /// Input: Write data, reset, write new data.
    /// Expected: Only new data is present after reset.
    /// </summary>
    [Fact]
    public void Write_AfterReset_WritesFromBeginning()
    {
        // Arrange
        using var writer = new ResizableByteWriter(initialCapacity: 8);
        writer.Write([1, 2, 3, 4]);
        writer.Reset();
        ReadOnlySpan<byte> newData = new byte[] { 5, 6, 7 };

        // Act
        writer.Write(newData);

        // Assert
        Assert.Equal(3, writer.Length);
        Assert.Equal(new byte[] { 5, 6, 7 }, writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Tests that GetSpan throws ArgumentOutOfRangeException when sizeHint is negative.
    /// A negative sizeHint will cause ArgumentOutOfRangeException when creating the Span.
    /// </summary>
    [Fact]
    public void GetSpan_NegativeSizeHint_ThrowsOverflowException()
    {
        // Arrange
        using var writer = new ResizableByteWriter();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.GetSpan(-1));
    }

    /// <summary>
    /// Tests that GetSpan throws ArgumentOutOfRangeException when sizeHint is int.MinValue.
    /// This extreme negative value is invalid for creating a Span.
    /// </summary>
    [Fact]
    public void GetSpan_IntMinValueSizeHint_ThrowsOverflowException()
    {
        // Arrange
        using var writer = new ResizableByteWriter();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.GetSpan(int.MinValue));
    }

    /// <summary>
    /// Tests that GetSpan throws OverflowException when the sum of current index and sizeHint exceeds int.MaxValue.
    /// This tests the overflow protection in the Grow method's checked arithmetic.
    /// </summary>
    [Fact]
    public void GetSpan_SizeHintCausesIntegerOverflow_ThrowsOverflowException()
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        var firstSpan = writer.GetSpan(1000);
        firstSpan[0] = 42;
        writer.Advance(1000);

        // Act & Assert
        // Requesting int.MaxValue when _index is already 1000 will overflow
        Assert.Throws<OverflowException>(() => writer.GetSpan(int.MaxValue));
    }

    /// <summary>
    /// Tests that GetSpan throws ObjectDisposedException when called after disposal.
    /// Verifies that the disposed state is properly checked before attempting operations.
    /// </summary>
    [Fact]
    public void GetSpan_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var writer = new ResizableByteWriter();
        writer.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => writer.GetSpan(10));
    }

    /// <summary>
    /// Tests that GetSpan returns a span positioned at the correct index after previous writes.
    /// Verifies that the span starts at _index and has the requested length.
    /// </summary>
    [Fact]
    public void GetSpan_AfterWrites_ReturnsSpanAtCorrectPosition()
    {
        // Arrange
        using var writer = new ResizableByteWriter();

        // Write first chunk
        var span1 = writer.GetSpan(5);
        for (int i = 0; i < 5; i++) span1[i] = (byte)(i + 10);
        writer.Advance(5);

        // Act
        var span2 = writer.GetSpan(3);

        // Assert
        Assert.Equal(3, span2.Length);
        // Write to second span and verify positioning
        for (int i = 0; i < 3; i++) span2[i] = (byte)(i + 20);
        writer.Advance(3);

        Assert.Equal(8, writer.WrittenSpan.Length);
        Assert.Equal(10, writer.WrittenSpan[0]);
        Assert.Equal(14, writer.WrittenSpan[4]);
        Assert.Equal(20, writer.WrittenSpan[5]);
        Assert.Equal(22, writer.WrittenSpan[7]);
    }

    /// <summary>
    /// Tests GetSpan with various valid positive sizeHint values.
    /// Verifies that the method returns spans of the correct length for different size requests.
    /// </summary>
    /// <param name="sizeHint">The size hint to request.</param>
    [Theory]
    [InlineData(1)]
    [InlineData(8)]
    [InlineData(16)]
    [InlineData(100)]
    [InlineData(1024)]
    [InlineData(10000)]
    public void GetSpan_VariousValidSizeHints_ReturnsCorrectLength(int sizeHint)
    {
        // Arrange
        using var writer = new ResizableByteWriter();

        // Act
        var span = writer.GetSpan(sizeHint);

        // Assert
        Assert.Equal(sizeHint, span.Length);
    }

    /// <summary>
    /// Tests that GetSpan with sizeHint of 1 returns a span of length 1.
    /// Verifies the minimum valid positive sizeHint boundary.
    /// </summary>
    [Fact]
    public void GetSpan_SizeHintOne_ReturnsSpanOfLengthOne()
    {
        // Arrange
        using var writer = new ResizableByteWriter();

        // Act
        var span = writer.GetSpan(1);

        // Assert
        Assert.Equal(1, span.Length);
        span[0] = 99;
        writer.Advance(1);
        Assert.Equal(99, writer.WrittenSpan[0]);
    }

    /// <summary>
    /// Tests that multiple GetSpan calls without Advance properly invalidate previous reservations.
    /// Verifies that calling GetSpan multiple times updates the _available field correctly.
    /// </summary>
    [Fact]
    public void GetSpan_MultipleCallsWithoutAdvance_UpdatesReservation()
    {
        // Arrange
        using var writer = new ResizableByteWriter();

        // Act
        var span1 = writer.GetSpan(10);
        Assert.Equal(10, span1.Length);

        var span2 = writer.GetSpan(20);
        Assert.Equal(20, span2.Length);

        // Assert - only the last reservation is valid
        // Writing 20 bytes and advancing should work
        for (int i = 0; i < 20; i++) span2[i] = (byte)i;
        writer.Advance(20);

        Assert.Equal(20, writer.WrittenSpan.Length);
    }

    /// <summary>
    /// Tests GetSpan behavior when requesting size larger than initial capacity.
    /// Verifies that buffer growth works correctly and returns a span of the requested size.
    /// </summary>
    [Fact]
    public void GetSpan_SizeHintLargerThanInitialCapacity_GrowsAndReturnsCorrectSpan()
    {
        // Arrange
        using var writer = new ResizableByteWriter(initialCapacity: 4);

        // Act
        var span = writer.GetSpan(100);

        // Assert
        Assert.Equal(100, span.Length);
        for (int i = 0; i < 100; i++) span[i] = (byte)(i % 256);
        writer.Advance(100);
        Assert.Equal(100, writer.WrittenSpan.Length);
    }

    /// <summary>
    /// Tests that WrittenMemory returns an empty ReadOnlyMemory when no data has been written.
    /// </summary>
    [Fact]
    public void WrittenMemory_NoDataWritten_ReturnsEmptyMemory()
    {
        // Arrange
        using var writer = new ResizableByteWriter();

        // Act
        var memory = writer.WrittenMemory;

        // Assert
        Assert.Equal(0, memory.Length);
        Assert.True(memory.IsEmpty);
    }

    /// <summary>
    /// Tests that WrittenMemory returns the correct data after writing a single byte.
    /// </summary>
    [Fact]
    public void WrittenMemory_SingleByteWritten_ReturnsCorrectData()
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        const byte expectedByte = 42;

        // Act
        writer.WriteByte(expectedByte);
        var memory = writer.WrittenMemory;

        // Assert
        Assert.Equal(1, memory.Length);
        Assert.Equal(expectedByte, memory.Span[0]);
    }

    /// <summary>
    /// Tests that WrittenMemory returns the correct data after writing multiple bytes via span.
    /// </summary>
    [Fact]
    public void WrittenMemory_MultipleSpanWrites_ReturnsAllWrittenData()
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        var data1 = new byte[] { 1, 2, 3 };
        var data2 = new byte[] { 4, 5, 6, 7 };

        // Act
        writer.Write(data1.AsSpan());
        writer.Write(data2.AsSpan());
        var memory = writer.WrittenMemory;

        // Assert
        Assert.Equal(7, memory.Length);
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 6, 7 }, memory.ToArray());
    }

    /// <summary>
    /// Tests that WrittenMemory returns the correct data after writing via ReadOnlyMemory overload.
    /// </summary>
    [Fact]
    public void WrittenMemory_WriteMemory_ReturnsCorrectData()
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        var data = new byte[] { 10, 20, 30, 40, 50 };
        var readOnlyMemory = new ReadOnlyMemory<byte>(data);

        // Act
        writer.Write(readOnlyMemory);
        var memory = writer.WrittenMemory;

        // Assert
        Assert.Equal(5, memory.Length);
        Assert.Equal(data, memory.ToArray());
    }

    /// <summary>
    /// Tests that WrittenMemory returns the correct data after writing via array overload.
    /// </summary>
    [Fact]
    public void WrittenMemory_WriteArray_ReturnsCorrectData()
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        var data = new byte[] { 100, 101, 102 };

        // Act
        writer.Write(data);
        var memory = writer.WrittenMemory;

        // Assert
        Assert.Equal(3, memory.Length);
        Assert.Equal(data, memory.ToArray());
    }

    /// <summary>
    /// Tests that WrittenMemory returns empty memory after calling Reset.
    /// </summary>
    [Fact]
    public void WrittenMemory_AfterReset_ReturnsEmptyMemory()
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        writer.Write(new byte[] { 1, 2, 3, 4, 5 });

        // Act
        writer.Reset();
        var memory = writer.WrittenMemory;

        // Assert
        Assert.Equal(0, memory.Length);
        Assert.True(memory.IsEmpty);
    }

    /// <summary>
    /// Tests that WrittenMemory returns correct data after multiple write-reset cycles.
    /// </summary>
    [Fact]
    public void WrittenMemory_MultipleWriteResetCycles_ReturnsCorrectData()
    {
        // Arrange
        using var writer = new ResizableByteWriter();

        // Act & Assert - First cycle
        writer.Write(new byte[] { 1, 2, 3 });
        Assert.Equal(3, writer.WrittenMemory.Length);
        Assert.Equal(new byte[] { 1, 2, 3 }, writer.WrittenMemory.ToArray());

        // Reset and second cycle
        writer.Reset();
        Assert.Equal(0, writer.WrittenMemory.Length);

        writer.Write(new byte[] { 10, 20 });
        Assert.Equal(2, writer.WrittenMemory.Length);
        Assert.Equal(new byte[] { 10, 20 }, writer.WrittenMemory.ToArray());
    }

    /// <summary>
    /// Tests that WrittenMemory correctly handles large data that triggers buffer growth.
    /// </summary>
    [Fact]
    public void WrittenMemory_LargeDataCausingGrowth_ReturnsCorrectData()
    {
        // Arrange
        using var writer = new ResizableByteWriter(initialCapacity: 4);
        var largeData = new byte[1024];
        for (int i = 0; i < largeData.Length; i++)
        {
            largeData[i] = (byte)(i % 256);
        }

        // Act
        writer.Write(largeData);
        var memory = writer.WrittenMemory;

        // Assert
        Assert.Equal(1024, memory.Length);
        Assert.Equal(largeData, memory.ToArray());
    }

    /// <summary>
    /// Tests that WrittenMemory throws ObjectDisposedException when accessed after disposal.
    /// </summary>
    [Fact]
    public void WrittenMemory_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var writer = new ResizableByteWriter();
        writer.Write(new byte[] { 1, 2, 3 });
        writer.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => writer.WrittenMemory);
    }

    /// <summary>
    /// Tests that WrittenMemory returns correct data with initial capacity specified.
    /// </summary>
    [Fact]
    public void WrittenMemory_WithInitialCapacity_ReturnsCorrectData()
    {
        // Arrange
        using var writer = new ResizableByteWriter(initialCapacity: 100);
        var data = new byte[] { 5, 10, 15 };

        // Act
        writer.Write(data);
        var memory = writer.WrittenMemory;

        // Assert
        Assert.Equal(3, memory.Length);
        Assert.Equal(data, memory.ToArray());
    }

    /// <summary>
    /// Tests that WrittenMemory returns correct data when using custom ArrayPool.
    /// </summary>
    [Fact]
    public void WrittenMemory_WithCustomPool_ReturnsCorrectData()
    {
        // Arrange
        var pool = ArrayPool<byte>.Create();
        using var writer = new ResizableByteWriter(pool, initialCapacity: 16);
        var data = new byte[] { 255, 254, 253 };

        // Act
        writer.Write(data);
        var memory = writer.WrittenMemory;

        // Assert
        Assert.Equal(3, memory.Length);
        Assert.Equal(data, memory.ToArray());
    }

    /// <summary>
    /// Tests that WrittenMemory returns correct data after using IBufferWriter pattern with GetSpan and Advance.
    /// </summary>
    [Fact]
    public void WrittenMemory_AfterGetSpanAndAdvance_ReturnsCorrectData()
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        var span = writer.GetSpan(5);
        span[0] = 11;
        span[1] = 22;
        span[2] = 33;

        // Act
        writer.Advance(3);
        var memory = writer.WrittenMemory;

        // Assert
        Assert.Equal(3, memory.Length);
        Assert.Equal(new byte[] { 11, 22, 33 }, memory.ToArray());
    }

    /// <summary>
    /// Tests that WrittenMemory returns correct data after using GetMemory and Advance.
    /// </summary>
    [Fact]
    public void WrittenMemory_AfterGetMemoryAndAdvance_ReturnsCorrectData()
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        var memory = writer.GetMemory(4);
        memory.Span[0] = 99;
        memory.Span[1] = 88;

        // Act
        writer.Advance(2);
        var writtenMemory = writer.WrittenMemory;

        // Assert
        Assert.Equal(2, writtenMemory.Length);
        Assert.Equal(new byte[] { 99, 88 }, writtenMemory.ToArray());
    }

    /// <summary>
    /// Tests that WrittenMemory returns correct data when writing maximum byte values.
    /// </summary>
    [Fact]
    public void WrittenMemory_WithMaxByteValues_ReturnsCorrectData()
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        var data = new byte[] { byte.MinValue, byte.MaxValue, 128 };

        // Act
        writer.Write(data);
        var memory = writer.WrittenMemory;

        // Assert
        Assert.Equal(3, memory.Length);
        Assert.Equal(data, memory.ToArray());
    }

    /// <summary>
    /// Tests that WrittenMemory can be accessed multiple times and returns consistent data.
    /// </summary>
    [Fact]
    public void WrittenMemory_AccessedMultipleTimes_ReturnsConsistentData()
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        var data = new byte[] { 1, 2, 3, 4 };
        writer.Write(data);

        // Act
        var memory1 = writer.WrittenMemory;
        var memory2 = writer.WrittenMemory;

        // Assert
        Assert.Equal(memory1.Length, memory2.Length);
        Assert.Equal(memory1.ToArray(), memory2.ToArray());
    }

    /// <summary>
    /// Tests that WrittenMemory returns empty memory with zero initial capacity.
    /// </summary>
    [Fact]
    public void WrittenMemory_ZeroInitialCapacity_ReturnsEmptyMemory()
    {
        // Arrange
        using var writer = new ResizableByteWriter(initialCapacity: 0);

        // Act
        var memory = writer.WrittenMemory;

        // Assert
        Assert.Equal(0, memory.Length);
        Assert.True(memory.IsEmpty);
    }

    /// <summary>
    /// Verifies that Seek throws NotSupportedException for various offset edge cases
    /// including long.MinValue, long.MaxValue, zero, negative, and positive values
    /// with different SeekOrigin values.
    /// </summary>
    /// <param name="offset">The offset value to test.</param>
    /// <param name="origin">The SeekOrigin value to test.</param>
    [Theory]
    [InlineData(long.MinValue, SeekOrigin.Begin)]
    [InlineData(long.MinValue, SeekOrigin.Current)]
    [InlineData(long.MinValue, SeekOrigin.End)]
    [InlineData(long.MaxValue, SeekOrigin.Begin)]
    [InlineData(long.MaxValue, SeekOrigin.Current)]
    [InlineData(long.MaxValue, SeekOrigin.End)]
    [InlineData(0L, SeekOrigin.Begin)]
    [InlineData(0L, SeekOrigin.Current)]
    [InlineData(0L, SeekOrigin.End)]
    [InlineData(-1L, SeekOrigin.Begin)]
    [InlineData(-1L, SeekOrigin.Current)]
    [InlineData(-1L, SeekOrigin.End)]
    [InlineData(1L, SeekOrigin.Begin)]
    [InlineData(1L, SeekOrigin.Current)]
    [InlineData(1L, SeekOrigin.End)]
    [InlineData(-100L, SeekOrigin.Begin)]
    [InlineData(100L, SeekOrigin.End)]
    [InlineData(1024L, SeekOrigin.Current)]
    public void Seek_WithVariousOffsetsAndOrigins_ThrowsNotSupportedException(long offset, SeekOrigin origin)
    {
        // Arrange
        using var writer = new ResizableByteWriter();

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => writer.Seek(offset, origin));
    }

    /// <summary>
    /// Verifies that Seek throws NotSupportedException even when passed invalid SeekOrigin enum values
    /// that are outside the defined enum range.
    /// </summary>
    /// <param name="invalidOrigin">An invalid SeekOrigin value cast from an out-of-range integer.</param>
    [Theory]
    [InlineData((SeekOrigin)(-1))]
    [InlineData((SeekOrigin)3)]
    [InlineData((SeekOrigin)99)]
    [InlineData((SeekOrigin)int.MinValue)]
    [InlineData((SeekOrigin)int.MaxValue)]
    public void Seek_WithInvalidSeekOriginValue_ThrowsNotSupportedException(SeekOrigin invalidOrigin)
    {
        // Arrange
        using var writer = new ResizableByteWriter();

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => writer.Seek(0, invalidOrigin));
    }

    /// <summary>
    /// Verifies that Seek throws NotSupportedException even after the writer has been disposed.
    /// </summary>
    [Fact]
    public void Seek_AfterDispose_ThrowsNotSupportedException()
    {
        // Arrange
        var writer = new ResizableByteWriter();
        writer.Dispose();

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => writer.Seek(0, SeekOrigin.Begin));
    }

    /// <summary>
    /// Verifies that Seek throws NotSupportedException even after data has been written to the writer.
    /// </summary>
    [Fact]
    public void Seek_AfterWritingData_ThrowsNotSupportedException()
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        writer.WriteByte(1);
        writer.WriteByte(2);
        writer.WriteByte(3);

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => writer.Seek(0, SeekOrigin.Begin));
        Assert.Throws<NotSupportedException>(() => writer.Seek(1, SeekOrigin.Current));
        Assert.Throws<NotSupportedException>(() => writer.Seek(-1, SeekOrigin.End));
    }

    /// <summary>
    /// Tests that accessing the Position property getter throws NotSupportedException.
    /// The Position property is not supported because the ResizableByteWriter does not support seeking.
    /// </summary>
    [Fact]
    public void Position_Get_ThrowsNotSupportedException()
    {
        // Arrange
        using var writer = new ResizableByteWriter();

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => _ = writer.Position);
    }

    /// <summary>
    /// Tests that setting the Position property throws NotSupportedException for various long values.
    /// The Position property setter is not supported because the ResizableByteWriter does not support seeking.
    /// </summary>
    /// <param name="value">The value to attempt to set the Position to.</param>
    [Theory]
    [InlineData(long.MinValue)]
    [InlineData(-1L)]
    [InlineData(0L)]
    [InlineData(1L)]
    [InlineData(100L)]
    [InlineData(long.MaxValue)]
    public void Position_Set_ThrowsNotSupportedException(long value)
    {
        // Arrange
        using var writer = new ResizableByteWriter();

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => writer.Position = value);
    }

    /// <summary>
    /// Verifies that the Length property returns zero for a newly instantiated writer
    /// regardless of the constructor used.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(16)]
    [InlineData(1024)]
    public void Length_NewInstance_IsZero(int initialCapacity)
    {
        // Arrange & Act
        using var writer = new ResizableByteWriter(initialCapacity);

        // Assert
        Assert.Equal(0L, writer.Length);
    }

    /// <summary>
    /// Verifies that the Length property correctly reflects the number of bytes
    /// written after a single WriteByte operation.
    /// </summary>
    [Fact]
    public void Length_AfterWritingSingleByte_IsOne()
    {
        // Arrange
        using var writer = new ResizableByteWriter();

        // Act
        writer.WriteByte(42);

        // Assert
        Assert.Equal(1L, writer.Length);
    }

    /// <summary>
    /// Verifies that the Length property correctly reflects the number of bytes
    /// written after multiple write operations of varying sizes.
    /// </summary>
    /// <param name="byteCount">The number of bytes to write.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(10000)]
    public void Length_AfterWritingBytes_ReflectsCount(int byteCount)
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        var data = new byte[byteCount];

        // Act
        writer.Write(data);

        // Assert
        Assert.Equal((long)byteCount, writer.Length);
    }

    /// <summary>
    /// Verifies that the Length property correctly accumulates across multiple
    /// separate write operations.
    /// </summary>
    [Fact]
    public void Length_AccumulatesAcrossMultipleWrites_ReflectsTotalCount()
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        var data1 = new byte[10];
        var data2 = new byte[20];
        var data3 = new byte[30];

        // Act
        writer.Write(data1);
        writer.Write(data2);
        writer.Write(data3);

        // Assert
        Assert.Equal(60L, writer.Length);
    }

    /// <summary>
    /// Verifies that the Length property correctly increments when using
    /// the Advance method after reserving space with GetSpan.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void Length_AfterAdvance_IncrementsCorrectly(int advanceCount)
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        var span = writer.GetSpan(advanceCount);

        // Act
        writer.Advance(advanceCount);

        // Assert
        Assert.Equal((long)advanceCount, writer.Length);
    }

    /// <summary>
    /// Verifies that the Length property resets to zero after calling Reset(),
    /// even when data has been written.
    /// </summary>
    [Fact]
    public void Length_AfterReset_ReturnsToZero()
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        writer.Write(new byte[100]);
        Assert.Equal(100L, writer.Length); // Precondition

        // Act
        writer.Reset();

        // Assert
        Assert.Equal(0L, writer.Length);
    }

    /// <summary>
    /// Verifies that the Length property resets to zero after multiple
    /// Reset operations.
    /// </summary>
    [Fact]
    public void Length_AfterMultipleResets_RemainsZero()
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        writer.Write(new byte[50]);
        writer.Reset();
        writer.Write(new byte[75]);

        // Act
        writer.Reset();

        // Assert
        Assert.Equal(0L, writer.Length);
    }

    /// <summary>
    /// Verifies that accessing the Length property after disposal
    /// throws an ObjectDisposedException.
    /// </summary>
    [Fact]
    public void Length_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var writer = new ResizableByteWriter();
        writer.Write(new byte[10]);

        // Act
        writer.Dispose();

        // Assert
        Assert.Throws<ObjectDisposedException>(() => _ = writer.Length);
    }

    /// <summary>
    /// Verifies that the Length property correctly handles large write operations
    /// and the implicit conversion from int to long.
    /// </summary>
    [Fact]
    public void Length_WithLargeWrites_ConvertsIntToLongCorrectly()
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        var largeData = new byte[1_000_000];

        // Act
        writer.Write(largeData);

        // Assert
        Assert.Equal(1_000_000L, writer.Length);
        Assert.IsType<long>(writer.Length);
    }

    /// <summary>
    /// Verifies that the Length property correctly reflects the count
    /// after writing via WriteByte multiple times.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void Length_AfterMultipleWriteByte_AccumulatesCorrectly(int count)
    {
        // Arrange
        using var writer = new ResizableByteWriter();

        // Act
        for (int i = 0; i < count; i++)
        {
            writer.WriteByte((byte)i);
        }

        // Assert
        Assert.Equal((long)count, writer.Length);
    }

    /// <summary>
    /// Verifies that the Length property remains accurate after growth
    /// is triggered by large writes.
    /// </summary>
    [Fact]
    public void Length_AfterGrowth_RemainsAccurate()
    {
        // Arrange
        using var writer = new ResizableByteWriter(initialCapacity: 4);
        var smallData = new byte[2];
        var largeData = new byte[100];

        // Act
        writer.Write(smallData);
        writer.Write(largeData); // This should trigger growth

        // Assert
        Assert.Equal(102L, writer.Length);
    }

    /// <summary>
    /// Verifies that the Length property correctly handles edge case
    /// where no data has been written but GetSpan/GetMemory were called.
    /// </summary>
    [Fact]
    public void Length_AfterGetSpanWithoutAdvance_RemainsZero()
    {
        // Arrange
        using var writer = new ResizableByteWriter();

        // Act
        _ = writer.GetSpan(100); // Reserve space but don't advance

        // Assert
        Assert.Equal(0L, writer.Length);
    }

    /// <summary>
    /// Verifies that the Length property handles sequential write operations
    /// using different Write overloads correctly.
    /// </summary>
    [Fact]
    public void Length_WithMixedWriteOverloads_AccumulatesCorrectly()
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        var array = new byte[] { 1, 2, 3 };
        ReadOnlySpan<byte> span = new byte[] { 4, 5 };
        ReadOnlyMemory<byte> memory = new byte[] { 6, 7, 8, 9 };

        // Act
        writer.Write(array);
        writer.Write(span);
        writer.Write(memory);

        // Assert
        Assert.Equal(9L, writer.Length);
    }

    /// <summary>
    /// Verifies that the Length property returns the correct value
    /// when using the array Write overload with offset and count.
    /// </summary>
    [Fact]
    public void Length_WithArrayOffsetCount_ReflectsWrittenBytes()
    {
        // Arrange
        using var writer = new ResizableByteWriter();
        var array = new byte[20];

        // Act
        writer.Write(array, offset: 5, count: 10);

        // Assert
        Assert.Equal(10L, writer.Length);
    }

    // -------- Advance(0) behavior --------

    [Fact]
    public void Advance_Zero_WithReservation_ClearsReservation()
    {
        using var w = new ResizableByteWriter();
        _ = w.GetSpan(16);

        w.Advance(0);

        Assert.Throws<InvalidOperationException>(() => w.Advance(1));
        Assert.Equal(0, w.Length);
    }

    [Fact]
    public void Advance_Zero_WithoutReservation_IsNoOp()
    {
        using var w = new ResizableByteWriter(initialCapacity: 0);

        // No GetSpan/GetMemory called; Advance(0) should not throw
        // because 0 <= _available (0)
        w.Advance(0);
        Assert.Equal(0, w.Length);
    }

    // -------- Empty write edge cases --------

    [Fact]
    public void Write_EmptySpan_IsNoOp()
    {
        using var w = new ResizableByteWriter(initialCapacity: 4);
        w.Write([1, 2]);
        w.Write(ReadOnlySpan<byte>.Empty);

        Assert.Equal(2, w.Length);
        Assert.Equal(new byte[] { 1, 2 }, w.WrittenSpan.ToArray());
    }

    [Fact]
    public void Write_EmptyArray_IsNoOp()
    {
        using var w = new ResizableByteWriter(initialCapacity: 4);
        w.Write([1]);
        w.Write(Array.Empty<byte>());

        Assert.Equal(1, w.Length);
        Assert.Equal(new byte[] { 1 }, w.WrittenSpan.ToArray());
    }

    [Fact]
    public void Write_EmptyMemory_IsNoOp()
    {
        using var w = new ResizableByteWriter();
        w.Write([5]);
        w.Write(ReadOnlyMemory<byte>.Empty);

        Assert.Equal(1, w.Length);
        Assert.Equal(new byte[] { 5 }, w.WrittenSpan.ToArray());
    }

    // -------- Disposal edge cases --------

    [Fact]
    public void Dispose_WithoutWriting_IsClean()
    {
        var w = new ResizableByteWriter(initialCapacity: 0);
        w.Dispose();
        // No exception expected
    }

    [Fact]
    public void After_Dispose_Write_Overloads_Throw()
    {
        var w = new ResizableByteWriter();
        w.Dispose();

        Assert.Throws<ObjectDisposedException>(() => w.Write(new byte[] { 1 }));
        Assert.Throws<ObjectDisposedException>(() => w.Write(new ReadOnlyMemory<byte>([1])));
        Assert.Throws<ObjectDisposedException>(() => w.Write(new byte[] { 1 }, 0, 1));
        Assert.Throws<ObjectDisposedException>(() => w.Advance(0));
        Assert.Throws<ObjectDisposedException>(() => _ = w.Length);
    }

    // -------- Constructor with pool-only --------

    [Fact]
    public void Constructor_PoolOnly_CreatesEmptyWriter()
    {
        using var w = new ResizableByteWriter(ArrayPool<byte>.Shared);

        Assert.Equal(0, w.Length);
        Assert.Empty(w.WrittenSpan.ToArray());
    }

    // -------- Interleaved GetSpan/GetMemory --------

    [Fact]
    public void GetMemory_After_GetSpan_Replaces_Reservation()
    {
        using var w = new ResizableByteWriter();

        _ = w.GetSpan(4);
        var mem = w.GetMemory(8);
        mem.Span[0] = 99;
        w.Advance(1);

        Assert.Equal(new byte[] { 99 }, w.WrittenSpan.ToArray());
    }

    // -------- Write after Reset --------

    [Fact]
    public void Reset_ThenMixedWrites_ProducesCorrectData()
    {
        using var w = new ResizableByteWriter(initialCapacity: 4);
        w.Write([1, 2, 3]);
        w.Reset();
        w.WriteByte(42);
        w.Write([10, 20]);

        Assert.Equal(3, w.Length);
        Assert.Equal(new byte[] { 42, 10, 20 }, w.WrittenSpan.ToArray());
    }

    // -------- Multiple growth cycles --------

    [Fact]
    public void Multiple_Growth_Cycles_PreserveAllData()
    {
        using var w = new ResizableByteWriter(initialCapacity: 2);

        for (int i = 0; i < 500; i++)
            w.WriteByte((byte)(i % 256));

        Assert.Equal(500, w.Length);
        for (int i = 0; i < 500; i++)
            Assert.Equal((byte)(i % 256), w.WrittenSpan[i]);
    }

    // -------- Stream CopyTo integration --------

    [Fact]
    public async Task Stream_WriteAsync_Memory_Overload()
    {
        using var w = new ResizableByteWriter();
        var data = new byte[] { 7, 8, 9 }.AsMemory();

        await w.WriteAsync(data, TestContext.Current.CancellationToken);

        Assert.Equal(new byte[] { 7, 8, 9 }, w.WrittenSpan.ToArray());
    }

    // -------- Large single write --------

    [Fact]
    public void Write_LargePayload_PreservesIntegrity()
    {
        using var w = new ResizableByteWriter(initialCapacity: 0);
        var payload = new byte[100_000];
        new Random(42).NextBytes(payload);

        w.Write(payload);

        Assert.Equal(100_000, w.Length);
        Assert.True(w.WrittenSpan.SequenceEqual(payload));
    }
}
