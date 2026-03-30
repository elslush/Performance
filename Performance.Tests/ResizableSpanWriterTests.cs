using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

using Performance.Buffers;
using Xunit;

namespace Performance.Tests;


public sealed class ResizableSpanWriterTests
{
    // ---------- Basic write APIs ----------

    [Fact]
    public void Write_SingleItems_Accumulates()
    {
        var w = new ResizableSpanWriter<byte>(initialCapacity: 4);
        w.Write(1);
        w.Write(2);
        w.Write(3);

        Assert.Equal(new byte[] { 1, 2, 3 }, w.WrittenSpan.ToArray());
        Assert.Equal(3, w.WrittenSpan.Length);
    }

    [Fact]
    public void Write_Span_Appends()
    {
        var w = new ResizableSpanWriter<char>(initialCapacity: 2);
        w.Write("ab".AsSpan());
        w.Write("cdef".AsSpan());

        Assert.Equal("abcdef", new string(w.WrittenSpan));
    }

    [Fact]
    public void Write_Array_Appends()
    {
        var w = new ResizableSpanWriter<int>(initialCapacity: 2);
        w.Write([1, 2]);
        w.Write([3, 4, 5]);

        Assert.Equal([1, 2, 3, 4, 5], w.WrittenSpan.ToArray());
    }

    // ---------- IBufferWriter flow ----------

    [Fact]
    public void GetSpan_DefaultSizeHint_Is8_And_Advance()
    {
        var w = new ResizableSpanWriter<byte>(initialCapacity: 0);

        // Request with sizeHint=0 -> should allocate/grow for 8
        var span = w.GetSpan(); // default sizeHint of 8 in the implementation
        Assert.Equal(8, span.Length);

        // Fill and advance
        for (int i = 0; i < span.Length; i++) span[i] = (byte)i;
        w.Advance(span.Length);

        Assert.Equal(8, w.WrittenSpan.Length);
        Assert.Equal(Enumerable.Range(0, 8).Select(i => (byte)i).ToArray(), w.WrittenSpan.ToArray());
    }

    [Fact]
    public void GetMemory_WriteThenAdvance()
    {
        var w = new ResizableSpanWriter<char>(initialCapacity: 0);

        var mem = w.GetMemory(sizeHint: 3);
        mem.Span[0] = 'x';
        mem.Span[1] = 'y';
        mem.Span[2] = 'z';
        w.Advance(3);

        Assert.Equal("xyz", new string(w.WrittenSpan));
    }

    [Fact]
    public void Advance_Negative_Throws()
    {
        var w = new ResizableSpanWriter<byte>(initialCapacity: 8);
        Assert.Throws<ArgumentOutOfRangeException>(() => w.Advance(-1));
    }

    [Fact]
    public void Advance_Without_Reservation_Throws()
    {
        var w = new ResizableSpanWriter<byte>(initialCapacity: 0);
        // _array starts as length 0 → advancing without reserving should throw
        Assert.Throws<InvalidOperationException>(() => w.Advance(1));
    }

    // ---------- Growth & capacity rounding ----------

    [Fact]
    public void Growth_RoundsTo_NextPowerOfTwo()
    {
        var w = new ResizableSpanWriter<byte>(initialCapacity: 0);

        // Write 9 bytes -> capacity should round up to 16
        var data = Enumerable.Range(0, 9).Select(i => (byte)i).ToArray();
        w.Write(data);

        // The IMemoryOwner<T>.Memory returns the full buffer, not just written segment.
        var owner = (IMemoryOwner<byte>)w;
        Assert.True(owner.Memory.Length >= 16);
        Assert.Equal(data, w.WrittenSpan.ToArray());
    }

    [Fact]
    public void LargeWrite_GrowsOnce_AndKeepsData()
    {
        var w = new ResizableSpanWriter<int>(initialCapacity: 4);
        var arr = Enumerable.Range(0, 1000).ToArray();
        w.Write(arr);

        Assert.Equal(1000, w.WrittenSpan.Length);
        Assert.True(((IMemoryOwner<int>)w).Memory.Length >= 1024); // next power of two above 1000 is 1024
        Assert.True(w.WrittenSpan.SequenceEqual(arr));
    }

    // ---------- Reset semantics ----------

    [Fact]
    public void Reset_SetsIndexToZero_ButKeepsCapacity()
    {
        var w = new ResizableSpanWriter<byte>(initialCapacity: 2);
        w.Write([1, 2, 3, 4, 5]);

        int capacityBefore = ((IMemoryOwner<byte>)w).Memory.Length;

        w.Reset();
        Assert.Equal(0, w.WrittenSpan.Length);

        // Write new data; should overwrite from start without reallocating
        w.Write([9, 9, 9]);
        Assert.Equal(new byte[] { 9, 9, 9 }, w.WrittenSpan.ToArray());

        int capacityAfter = ((IMemoryOwner<byte>)w).Memory.Length;
        Assert.Equal(capacityBefore, capacityAfter);
    }

    // ---------- Read views ----------

    [Fact]
    public void WrittenSpan_And_WrittenMemory_Agree()
    {
        var w = new ResizableSpanWriter<char>(initialCapacity: 0);
        w.Write("hello".AsSpan());

        var span = w.WrittenSpan;
        var mem = w.WrittenMemory;

        Assert.Equal(span.Length, mem.Length);
        Assert.Equal(new string(span), new string(mem.Span));
    }

    [Fact]
    public void IMemoryOwner_Memory_IsFullBuffer()
    {
        var w = new ResizableSpanWriter<byte>(initialCapacity: 8);
        w.Write([1, 2, 3]);
        var full = ((IMemoryOwner<byte>)w).Memory;

        // Full buffer should be >= written length (implementation returns whole array)
        Assert.True(full.Length >= w.WrittenSpan.Length);
        Assert.True(full.Length >= 8);
    }

    // ---------- Pool behavior ----------

    [Fact]
    public void Dispose_ReturnsBuffer_ToPool()
    {
        var pool = new TrackingArrayPool<byte>();
        var w = new ResizableSpanWriter<byte>(pool, initialCapacity: 4);
        w.Write([1, 2, 3, 4, 5, 6]); // should grow at least once

        Assert.True(pool.RentedCount >= 1);
        int returnsBefore = pool.ReturnedCount;

        w.Dispose();

        Assert.Equal(returnsBefore + 1, pool.ReturnedCount);

        // Dispose again should be a no-op (no double-return)
        w.Dispose();
        Assert.Equal(returnsBefore + 1, pool.ReturnedCount);
    }

    [Fact]
    public void SpanWriter_DirectWrite_Invalidates_GetSpanReservation()
    {
        // Arrange
        using var writer = new ResizableSpanWriter<int>();
        var span = writer.GetSpan(16);

        // Act
        writer.Write(42);

        // Assert
        var ex = Assert.Throws<InvalidOperationException>(() => writer.Advance(1));
        Assert.Equal("Cannot advance past the end of the reserved buffer segment.", ex.Message);
        Assert.Equal([42], writer.WrittenSpan.ToArray());
    }

    [Fact]
    public void SpanWriter_Reset_Clears_GetSpanReservation()
    {
        // Arrange
        using var writer = new ResizableSpanWriter<char>();
        var span = writer.GetSpan(16);

        // Act
        writer.Reset();

        // Assert
        var ex = Assert.Throws<InvalidOperationException>(() => writer.Advance(1));
        Assert.Equal("Cannot advance past the end of the reserved buffer segment.", ex.Message);
        Assert.Equal(0, writer.WrittenSpan.Length);
    }

    [Fact]
    public void SpanWriter_GetMemory_FollowedBy_Write_And_FailedAdvance()
    {
        // Arrange
        using var writer = new ResizableSpanWriter<string>();
        var memory = writer.GetMemory(10);
        memory.Span[0] = "first";

        // Act
        writer.Write("second");

        // Assert
        var ex = Assert.Throws<InvalidOperationException>(() => writer.Advance(1));
        Assert.Equal("Cannot advance past the end of the reserved buffer segment.", ex.Message);
        Assert.Equal(new string[] { "second" }, writer.WrittenSpan.ToArray());
    }

    // ---------- Known-issue: disposal guard ----------

    [Fact]
    public void AfterDispose_AccessorsThrow_ObjectDisposedException()
    {
        var w = new ResizableSpanWriter<byte>(initialCapacity: 8);
        w.Write([1, 2, 3]);
        w.Dispose();

        Assert.Throws<ObjectDisposedException>(() => _ = w.WrittenSpan);
        Assert.Throws<ObjectDisposedException>(() => _ = ((IMemoryOwner<byte>)w).Memory);
        Assert.Throws<ObjectDisposedException>(() => w.GetSpan());
        Assert.Throws<ObjectDisposedException>(() => w.GetMemory());
        Assert.Throws<ObjectDisposedException>(() => w.Write(1));
        Assert.Throws<ObjectDisposedException>(() => w.Write([9, 9, 9]));
        Assert.Throws<ObjectDisposedException>(() => w.Reset());
    }

    // ---------- Utility pool for verification ----------

    private sealed class TrackingArrayPool<T> : ArrayPool<T>
    {
        public int RentedCount { get; private set; }
        public int ReturnedCount { get; private set; }

        public override T[] Rent(int minimumLength)
        {
            RentedCount++;
            return GC.AllocateUninitializedArray<T>(Math.Max(1, minimumLength));
        }

        public override void Return(T[] array, bool clearArray = false)
        {
            ReturnedCount++;
            if (clearArray && RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                Array.Clear(array, 0, array.Length);
            }
        }
    }

    /// <summary>
    /// Tests that WrittenSpan returns an empty span when no items have been written.
    /// </summary>
    [Fact]
    public void WrittenSpan_NoWrites_ReturnsEmptySpan()
    {
        // Arrange
        using var writer = new ResizableSpanWriter<int>();

        // Act
        var span = writer.WrittenSpan;

        // Assert
        Assert.Equal(0, span.Length);
        Assert.True(span.IsEmpty);
    }

    /// <summary>
    /// Tests that WrittenSpan returns a span with correct length after writing a single item.
    /// </summary>
    [Fact]
    public void WrittenSpan_SingleWrite_ReturnsSpanWithOneElement()
    {
        // Arrange
        using var writer = new ResizableSpanWriter<int>();
        writer.Write(42);

        // Act
        var span = writer.WrittenSpan;

        // Assert
        Assert.Equal(1, span.Length);
        Assert.Equal(42, span[0]);
    }

    /// <summary>
    /// Tests that WrittenSpan returns a span containing all written items in correct order.
    /// </summary>
    /// <param name="count">The number of items to write.</param>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void WrittenSpan_MultipleWrites_ReturnsAllWrittenItems(int count)
    {
        // Arrange
        using var writer = new ResizableSpanWriter<int>();
        for (int i = 0; i < count; i++)
        {
            writer.Write(i);
        }

        // Act
        var span = writer.WrittenSpan;

        // Assert
        Assert.Equal(count, span.Length);
        for (int i = 0; i < count; i++)
        {
            Assert.Equal(i, span[i]);
        }
    }

    /// <summary>
    /// Tests that WrittenSpan only returns the written portion, not the entire buffer capacity.
    /// </summary>
    [Fact]
    public void WrittenSpan_PartialCapacity_ReturnsOnlyWrittenPortion()
    {
        // Arrange
        using var writer = new ResizableSpanWriter<int>(initialCapacity: 100);
        writer.Write(1);
        writer.Write(2);
        writer.Write(3);

        // Act
        var span = writer.WrittenSpan;

        // Assert
        Assert.Equal(3, span.Length);
        Assert.Equal(1, span[0]);
        Assert.Equal(2, span[1]);
        Assert.Equal(3, span[2]);
    }

    /// <summary>
    /// Tests that WrittenSpan returns an empty span after Reset is called.
    /// </summary>
    [Fact]
    public void WrittenSpan_AfterReset_ReturnsEmptySpan()
    {
        // Arrange
        using var writer = new ResizableSpanWriter<int>();
        writer.Write(1);
        writer.Write(2);
        writer.Write(3);
        writer.Reset();

        // Act
        var span = writer.WrittenSpan;

        // Assert
        Assert.Equal(0, span.Length);
        Assert.True(span.IsEmpty);
    }

    /// <summary>
    /// Tests that WrittenSpan reflects current state after writing arrays.
    /// </summary>
    [Fact]
    public void WrittenSpan_AfterWritingArray_ReflectsAllElements()
    {
        // Arrange
        using var writer = new ResizableSpanWriter<int>();
        int[] data = [1, 2, 3, 4, 5];
        writer.Write(data);

        // Act
        var span = writer.WrittenSpan;

        // Assert
        Assert.Equal(5, span.Length);
        for (int i = 0; i < data.Length; i++)
        {
            Assert.Equal(data[i], span[i]);
        }
    }

    /// <summary>
    /// Tests that WrittenSpan reflects current state after writing spans.
    /// </summary>
    [Fact]
    public void WrittenSpan_AfterWritingSpan_ReflectsAllElements()
    {
        // Arrange
        using var writer = new ResizableSpanWriter<byte>();
        ReadOnlySpan<byte> data = [1, 2, 3, 4, 5, 6, 7, 8];
        writer.Write(data);

        // Act
        var span = writer.WrittenSpan;

        // Assert
        Assert.Equal(8, span.Length);
        Assert.True(data.SequenceEqual(span));
    }

    /// <summary>
    /// Tests that WrittenSpan reflects current state after writing memory.
    /// </summary>
    [Fact]
    public void WrittenSpan_AfterWritingMemory_ReflectsAllElements()
    {
        // Arrange
        using var writer = new ResizableSpanWriter<char>();
        ReadOnlyMemory<char> data = "Hello".AsMemory();
        writer.Write(data);

        // Act
        var span = writer.WrittenSpan;

        // Assert
        Assert.Equal(5, span.Length);
        Assert.True(data.Span.SequenceEqual(span));
    }

    /// <summary>
    /// Tests that WrittenSpan correctly reflects accumulated writes from mixed operations.
    /// </summary>
    [Fact]
    public void WrittenSpan_MixedWrites_ReflectsAllAccumulatedData()
    {
        // Arrange
        using var writer = new ResizableSpanWriter<int>();
        writer.Write(1);
        writer.Write([2, 3, 4]);
        writer.Write(5);
        ReadOnlySpan<int> additional = [6, 7];
        writer.Write(additional);

        // Act
        var span = writer.WrittenSpan;

        // Assert
        Assert.Equal(7, span.Length);
        Assert.Equal(1, span[0]);
        Assert.Equal(2, span[1]);
        Assert.Equal(3, span[2]);
        Assert.Equal(4, span[3]);
        Assert.Equal(5, span[4]);
        Assert.Equal(6, span[5]);
        Assert.Equal(7, span[6]);
    }

    /// <summary>
    /// Tests that WrittenSpan works correctly with zero initial capacity.
    /// </summary>
    [Fact]
    public void WrittenSpan_ZeroInitialCapacity_WorksCorrectly()
    {
        // Arrange
        using var writer = new ResizableSpanWriter<int>(initialCapacity: 0);

        // Act - before write
        var emptySpan = writer.WrittenSpan;

        // Assert - before write
        Assert.Equal(0, emptySpan.Length);

        // Arrange - write data
        writer.Write(99);

        // Act - after write
        var spanWithData = writer.WrittenSpan;

        // Assert - after write
        Assert.Equal(1, spanWithData.Length);
        Assert.Equal(99, spanWithData[0]);
    }

    /// <summary>
    /// Tests that WrittenSpan correctly handles writes that cause buffer growth.
    /// </summary>
    [Fact]
    public void WrittenSpan_AfterBufferGrowth_ReflectsAllData()
    {
        // Arrange
        using var writer = new ResizableSpanWriter<int>(initialCapacity: 4);

        // Write more than initial capacity to force growth
        for (int i = 0; i < 20; i++)
        {
            writer.Write(i);
        }

        // Act
        var span = writer.WrittenSpan;

        // Assert
        Assert.Equal(20, span.Length);
        for (int i = 0; i < 20; i++)
        {
            Assert.Equal(i, span[i]);
        }
    }

    /// <summary>
    /// Tests that WrittenSpan works with different generic types.
    /// </summary>
    [Fact]
    public void WrittenSpan_DifferentGenericTypes_WorksCorrectly()
    {
        // Arrange & Act & Assert - string type
        using var stringWriter = new ResizableSpanWriter<string>();
        stringWriter.Write("test");
        var stringSpan = stringWriter.WrittenSpan;
        Assert.Equal(1, stringSpan.Length);
        Assert.Equal("test", stringSpan[0]);

        // Arrange & Act & Assert - double type
        using var doubleWriter = new ResizableSpanWriter<double>();
        doubleWriter.Write(3.14);
        var doubleSpan = doubleWriter.WrittenSpan;
        Assert.Equal(1, doubleSpan.Length);
        Assert.Equal(3.14, doubleSpan[0]);

        // Arrange & Act & Assert - bool type
        using var boolWriter = new ResizableSpanWriter<bool>();
        boolWriter.Write(true);
        var boolSpan = boolWriter.WrittenSpan;
        Assert.Equal(1, boolSpan.Length);
        Assert.True(boolSpan[0]);
    }

    /// <summary>
    /// Tests that WrittenSpan can be accessed multiple times and returns consistent results.
    /// </summary>
    [Fact]
    public void WrittenSpan_MultipleAccesses_ReturnsConsistentResults()
    {
        // Arrange
        using var writer = new ResizableSpanWriter<int>();
        writer.Write(1);
        writer.Write(2);
        writer.Write(3);

        // Act
        var span1 = writer.WrittenSpan;
        var span2 = writer.WrittenSpan;

        // Assert
        Assert.Equal(span1.Length, span2.Length);
        Assert.True(span1.SequenceEqual(span2));
    }

    /// <summary>
    /// Tests that WrittenSpan reflects changes after additional writes between accesses.
    /// </summary>
    [Fact]
    public void WrittenSpan_BetweenWrites_ReflectsUpdatedState()
    {
        // Arrange
        using var writer = new ResizableSpanWriter<int>();
        writer.Write(1);

        // Act
        var span1 = writer.WrittenSpan;

        writer.Write(2);
        var span2 = writer.WrittenSpan;

        writer.Write(3);
        var span3 = writer.WrittenSpan;

        // Assert
        Assert.Equal(1, span1.Length);
        Assert.Equal(2, span2.Length);
        Assert.Equal(3, span3.Length);
    }

    /// <summary>
    /// Tests that WrittenSpan works correctly with boundary value types.
    /// </summary>
    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1)]
    public void WrittenSpan_BoundaryIntValues_StoresCorrectly(int value)
    {
        // Arrange
        using var writer = new ResizableSpanWriter<int>();
        writer.Write(value);

        // Act
        var span = writer.WrittenSpan;

        // Assert
        Assert.Equal(1, span.Length);
        Assert.Equal(value, span[0]);
    }

    /// <summary>
    /// Tests that WrittenSpan correctly handles empty array writes.
    /// </summary>
    [Fact]
    public void WrittenSpan_AfterWritingEmptyArray_RemainsUnchanged()
    {
        // Arrange
        using var writer = new ResizableSpanWriter<int>();
        writer.Write(1);
        int[] emptyArray = [];
        writer.Write(emptyArray);

        // Act
        var span = writer.WrittenSpan;

        // Assert
        Assert.Equal(1, span.Length);
        Assert.Equal(1, span[0]);
    }

    /// <summary>
    /// Tests that WrittenSpan correctly handles empty span writes.
    /// </summary>
    [Fact]
    public void WrittenSpan_AfterWritingEmptySpan_RemainsUnchanged()
    {
        // Arrange
        using var writer = new ResizableSpanWriter<int>();
        writer.Write(1);
        ReadOnlySpan<int> emptySpan = [];
        writer.Write(emptySpan);

        // Act
        var span = writer.WrittenSpan;

        // Assert
        Assert.Equal(1, span.Length);
        Assert.Equal(1, span[0]);
    }

    /// <summary>
    /// Tests that Write with ReadOnlyMemory successfully appends items to the buffer.
    /// Input: Multiple ReadOnlyMemory blocks with char data.
    /// Expected: All items are appended in order.
    /// </summary>
    [Fact]
    public void Write_Memory_Appends()
    {
        // Arrange
        var w = new ResizableSpanWriter<char>(initialCapacity: 2);
        ReadOnlyMemory<char> mem1 = "ab".AsMemory();
        ReadOnlyMemory<char> mem2 = "cdef".AsMemory();

        // Act
        w.Write(mem1);
        w.Write(mem2);

        // Assert
        Assert.Equal("abcdef", new string(w.WrittenSpan));
    }

    /// <summary>
    /// Tests that Write with an empty ReadOnlyMemory performs no operation.
    /// Input: Empty ReadOnlyMemory.
    /// Expected: Buffer remains unchanged, no items are written.
    /// </summary>
    [Fact]
    public void Write_EmptyMemory_NoOp()
    {
        // Arrange
        var w = new ResizableSpanWriter<int>(initialCapacity: 4);
        ReadOnlyMemory<int> empty = ReadOnlyMemory<int>.Empty;

        // Act
        w.Write(empty);

        // Assert
        Assert.Equal(0, w.WrittenSpan.Length);
        Assert.Empty(w.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Tests that Write with default ReadOnlyMemory (uninitialized) performs no operation.
    /// Input: Default ReadOnlyMemory struct.
    /// Expected: Buffer remains unchanged, treated as empty.
    /// </summary>
    [Fact]
    public void Write_DefaultMemory_NoOp()
    {
        // Arrange
        var w = new ResizableSpanWriter<byte>(initialCapacity: 4);
        ReadOnlyMemory<byte> defaultMem = default;

        // Act
        w.Write(defaultMem);

        // Assert
        Assert.Equal(0, w.WrittenSpan.Length);
        Assert.Empty(w.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Tests that Write with a single-item ReadOnlyMemory correctly appends that item.
    /// Input: ReadOnlyMemory containing a single element.
    /// Expected: The single item is appended to the buffer.
    /// </summary>
    [Fact]
    public void Write_SingleItemMemory_Appends()
    {
        // Arrange
        var w = new ResizableSpanWriter<int>(initialCapacity: 2);
        int[] array = [42];
        ReadOnlyMemory<int> mem = new ReadOnlyMemory<int>(array);

        // Act
        w.Write(mem);

        // Assert
        Assert.Equal([42], w.WrittenSpan.ToArray());
        Assert.Equal(1, w.WrittenSpan.Length);
    }

    /// <summary>
    /// Tests that Write with a large ReadOnlyMemory correctly grows the buffer and appends all items.
    /// Input: ReadOnlyMemory larger than initial capacity.
    /// Expected: Buffer grows automatically, all items are appended.
    /// </summary>
    [Fact]
    public void Write_LargeMemory_GrowsBuffer()
    {
        // Arrange
        var w = new ResizableSpanWriter<int>(initialCapacity: 2);
        int[] largeArray = new int[100];
        for (int i = 0; i < 100; i++) largeArray[i] = i;
        ReadOnlyMemory<int> mem = new ReadOnlyMemory<int>(largeArray);

        // Act
        w.Write(mem);

        // Assert
        Assert.Equal(100, w.WrittenSpan.Length);
        Assert.Equal(largeArray, w.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Tests that multiple consecutive Write calls with ReadOnlyMemory correctly accumulate data.
    /// Input: Multiple ReadOnlyMemory blocks written sequentially.
    /// Expected: All items are appended in order without data loss.
    /// </summary>
    [Fact]
    public void Write_MultipleMemories_Accumulates()
    {
        // Arrange
        var w = new ResizableSpanWriter<byte>(initialCapacity: 4);
        ReadOnlyMemory<byte> mem1 = new byte[] { 1, 2 }.AsMemory();
        ReadOnlyMemory<byte> mem2 = new byte[] { 3, 4, 5 }.AsMemory();
        ReadOnlyMemory<byte> mem3 = new byte[] { 6 }.AsMemory();

        // Act
        w.Write(mem1);
        w.Write(mem2);
        w.Write(mem3);

        // Assert
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 6 }, w.WrittenSpan.ToArray());
        Assert.Equal(6, w.WrittenSpan.Length);
    }

    /// <summary>
    /// Tests that Write with ReadOnlyMemory created from a slice of an array works correctly.
    /// Input: ReadOnlyMemory slice from a larger array.
    /// Expected: Only the sliced portion is appended.
    /// </summary>
    [Fact]
    public void Write_MemorySlice_AppendsOnlySlice()
    {
        // Arrange
        var w = new ResizableSpanWriter<char>(initialCapacity: 4);
        char[] source = ['a', 'b', 'c', 'd', 'e'];
        ReadOnlyMemory<char> slice = new ReadOnlyMemory<char>(source, 1, 3); // "bcd"

        // Act
        w.Write(slice);

        // Assert
        Assert.Equal("bcd", new string(w.WrittenSpan));
        Assert.Equal(3, w.WrittenSpan.Length);
    }

    /// <summary>
    /// Tests that Write with ReadOnlyMemory invalidates any outstanding GetSpan reservation.
    /// Input: GetSpan called to reserve space, then Write with ReadOnlyMemory.
    /// Expected: The reservation is invalidated; subsequent Advance throws.
    /// </summary>
    [Fact]
    public void Write_Memory_InvalidatesGetSpanReservation()
    {
        // Arrange
        var w = new ResizableSpanWriter<int>(initialCapacity: 8);
        var span = w.GetSpan(3);
        ReadOnlyMemory<int> mem = new int[] { 10, 20 }.AsMemory();

        // Act
        w.Write(mem); // This invalidates the reservation

        // Assert
        var ex = Assert.Throws<InvalidOperationException>(() => w.Advance(3));
        Assert.Contains("reserved buffer segment", ex.Message);
    }

    /// <summary>
    /// Tests that Write with ReadOnlyMemory after disposal throws ObjectDisposedException.
    /// Input: Disposed ResizableSpanWriter, then Write with ReadOnlyMemory.
    /// Expected: ObjectDisposedException is thrown.
    /// </summary>
    [Fact]
    public void Write_Memory_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var w = new ResizableSpanWriter<byte>(initialCapacity: 4);
        w.Dispose();
        ReadOnlyMemory<byte> mem = new byte[] { 1, 2, 3 }.AsMemory();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => w.Write(mem));
    }

    /// <summary>
    /// Tests that Write with an empty ReadOnlySpan performs no operation
    /// and does not modify the writer state.
    /// </summary>
    [Fact]
    public void Write_Span_EmptySpan_NoOperation()
    {
        // Arrange
        var writer = new ResizableSpanWriter<int>(initialCapacity: 4);
        var emptySpan = ReadOnlySpan<int>.Empty;

        // Act
        writer.Write(emptySpan);

        // Assert
        Assert.Equal(0, writer.WrittenSpan.Length);
        Assert.Empty(writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Tests that Write with ReadOnlySpan correctly appends data for various sizes,
    /// including single element, small, medium, and large spans that require buffer growth.
    /// </summary>
    /// <param name="elementCount">The number of elements to write in the span.</param>
    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void Write_Span_VariousSizes_AppendsCorrectly(int elementCount)
    {
        // Arrange
        var writer = new ResizableSpanWriter<int>(initialCapacity: 4);
        var dataToWrite = Enumerable.Range(1, elementCount).ToArray();
        var expectedData = dataToWrite.ToArray();

        // Act
        writer.Write(dataToWrite.AsSpan());

        // Assert
        Assert.Equal(elementCount, writer.WrittenSpan.Length);
        Assert.Equal(expectedData, writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Tests that Write with ReadOnlySpan correctly handles boundary condition
    /// where the span length is exactly at int.MaxValue boundary consideration.
    /// Verifies that very large spans are handled correctly with proper growth.
    /// </summary>
    [Fact]
    public void Write_Span_LargeSpan_AppendsCorrectlyWithGrowth()
    {
        // Arrange
        var writer = new ResizableSpanWriter<byte>(initialCapacity: 2);
        var largeData = new byte[10000];
        for (int i = 0; i < largeData.Length; i++)
        {
            largeData[i] = (byte)(i % 256);
        }

        // Act
        writer.Write(largeData.AsSpan());

        // Assert
        Assert.Equal(10000, writer.WrittenSpan.Length);
        Assert.Equal(largeData, writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Tests that multiple consecutive Write calls with ReadOnlySpan accumulate data correctly,
    /// including scenarios where buffer growth is required between writes.
    /// </summary>
    [Fact]
    public void Write_Span_MultipleConsecutiveWrites_AccumulatesCorrectly()
    {
        // Arrange
        var writer = new ResizableSpanWriter<int>(initialCapacity: 2);
        var firstSpan = new int[] { 1, 2, 3 }.AsSpan();
        var secondSpan = new int[] { 4, 5, 6, 7 }.AsSpan();
        var thirdSpan = new int[] { 8, 9 }.AsSpan();

        // Act
        writer.Write(firstSpan);
        writer.Write(secondSpan);
        writer.Write(thirdSpan);

        // Assert
        Assert.Equal(9, writer.WrittenSpan.Length);
        Assert.Equal(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Tests that Write with ReadOnlySpan invalidates any outstanding GetSpan reservation.
    /// Verifies that calling Advance after a Write throws InvalidOperationException
    /// because the reservation was cleared.
    /// </summary>
    [Fact]
    public void Write_Span_InvalidatesGetSpanReservation_ThrowsOnAdvance()
    {
        // Arrange
        var writer = new ResizableSpanWriter<byte>(initialCapacity: 16);
        var reservedSpan = writer.GetSpan(8);

        // Act
        writer.Write(new byte[] { 1, 2, 3 }.AsSpan());

        // Assert
        Assert.Throws<InvalidOperationException>(() => writer.Advance(8));
    }

    /// <summary>
    /// Tests that Reset can be called on a newly initialized writer with zero initial capacity.
    /// Verifies that the writer remains in a valid state after reset.
    /// </summary>
    [Fact]
    public void Reset_OnNewWriterWithZeroCapacity_DoesNotThrow()
    {
        // Arrange
        var writer = new ResizableSpanWriter<int>();

        // Act
        writer.Reset();

        // Assert
        Assert.Equal(0, writer.WrittenSpan.Length);
    }

    /// <summary>
    /// Tests that Reset can be called on a newly initialized writer with non-zero initial capacity.
    /// Verifies that the writer remains in a valid state and maintains its capacity.
    /// </summary>
    [Fact]
    public void Reset_OnNewWriterWithNonZeroCapacity_MaintainsCapacity()
    {
        // Arrange
        var writer = new ResizableSpanWriter<int>(initialCapacity: 100);
        int capacityBefore = ((IMemoryOwner<int>)writer).Memory.Length;

        // Act
        writer.Reset();

        // Assert
        Assert.Equal(0, writer.WrittenSpan.Length);
        int capacityAfter = ((IMemoryOwner<int>)writer).Memory.Length;
        Assert.Equal(capacityBefore, capacityAfter);
    }

    /// <summary>
    /// Tests that multiple consecutive Reset calls work correctly without throwing.
    /// Verifies that the writer state remains valid after each reset.
    /// </summary>
    [Fact]
    public void Reset_MultipleConsecutiveCalls_DoesNotThrow()
    {
        // Arrange
        var writer = new ResizableSpanWriter<byte>(initialCapacity: 10);
        writer.Write([1, 2, 3]);

        // Act
        writer.Reset();
        writer.Reset();
        writer.Reset();

        // Assert
        Assert.Equal(0, writer.WrittenSpan.Length);
        writer.Write([5, 6]);
        Assert.Equal(new byte[] { 5, 6 }, writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Tests that Reset properly clears the index when called on an empty writer that has capacity.
    /// Verifies that subsequent writes start from the beginning.
    /// </summary>
    [Fact]
    public void Reset_OnEmptyWriterWithCapacity_AllowsWriteFromStart()
    {
        // Arrange
        var writer = new ResizableSpanWriter<char>(initialCapacity: 50);

        // Act
        writer.Reset();

        // Assert
        Assert.Equal(0, writer.WrittenSpan.Length);
        writer.Write(['a', 'b', 'c']);
        Assert.Equal(new char[] { 'a', 'b', 'c' }, writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Tests that Reset after writing a single item correctly clears the index.
    /// Verifies that subsequent writes overwrite the previous data.
    /// </summary>
    [Fact]
    public void Reset_AfterWritingSingleItem_ClearsIndex()
    {
        // Arrange
        var writer = new ResizableSpanWriter<int>();
        writer.Write(42);

        // Act
        writer.Reset();

        // Assert
        Assert.Equal(0, writer.WrittenSpan.Length);
        writer.Write(99);
        Assert.Equal(new int[] { 99 }, writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Tests that Reset after writing large amounts of data correctly resets index.
    /// Verifies that the buffer capacity is retained and can be reused efficiently.
    /// </summary>
    [Fact]
    public void Reset_AfterLargeWrite_RetainsCapacityAndClearsIndex()
    {
        // Arrange
        var writer = new ResizableSpanWriter<byte>();
        var largeData = new byte[10000];
        Array.Fill(largeData, (byte)1);
        writer.Write(largeData);
        int capacityBefore = ((IMemoryOwner<byte>)writer).Memory.Length;

        // Act
        writer.Reset();

        // Assert
        Assert.Equal(0, writer.WrittenSpan.Length);
        int capacityAfter = ((IMemoryOwner<byte>)writer).Memory.Length;
        Assert.Equal(capacityBefore, capacityAfter);
        Assert.True(capacityAfter >= 10000);
    }

    /// <summary>
    /// Tests that Reset clears both index and available reservation when called after GetSpan.
    /// Verifies that a subsequent Advance without a new reservation throws.
    /// </summary>
    [Fact]
    public void Reset_AfterGetSpanWithoutAdvance_ClearsReservation()
    {
        // Arrange
        var writer = new ResizableSpanWriter<int>();
        var span = writer.GetSpan(100);

        // Act
        writer.Reset();

        // Assert
        Assert.Equal(0, writer.WrittenSpan.Length);
        var exception = Assert.Throws<InvalidOperationException>(() => writer.Advance(10));
        Assert.Equal("Cannot advance past the end of the reserved buffer segment.", exception.Message);
    }

    /// <summary>
    /// Tests that Reset clears both index and available reservation when called after GetMemory.
    /// Verifies that a subsequent Advance without a new reservation throws.
    /// </summary>
    [Fact]
    public void Reset_AfterGetMemoryWithoutAdvance_ClearsReservation()
    {
        // Arrange
        var writer = new ResizableSpanWriter<int>();
        var memory = writer.GetMemory(100);

        // Act
        writer.Reset();

        // Assert
        Assert.Equal(0, writer.WrittenSpan.Length);
        var exception = Assert.Throws<InvalidOperationException>(() => writer.Advance(10));
        Assert.Equal("Cannot advance past the end of the reserved buffer segment.", exception.Message);
    }

    /// <summary>
    /// Tests that Reset throws ObjectDisposedException when called on a disposed writer.
    /// Verifies proper disposal state handling.
    /// </summary>
    [Fact]
    public void Reset_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var writer = new ResizableSpanWriter<int>(initialCapacity: 10);
        writer.Write([1, 2, 3]);
        writer.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => writer.Reset());
    }

    /// <summary>
    /// Tests that Reset properly handles the scenario where GetSpan was called and partially advanced.
    /// Verifies that Reset clears both the written index and the reservation.
    /// </summary>
    [Fact]
    public void Reset_AfterGetSpanAndPartialAdvance_ClearsBothIndexAndReservation()
    {
        // Arrange
        var writer = new ResizableSpanWriter<int>();
        var span = writer.GetSpan(100);
        span[0] = 10;
        span[1] = 20;
        writer.Advance(2);

        // Act
        writer.Reset();

        // Assert
        Assert.Equal(0, writer.WrittenSpan.Length);
        var exception = Assert.Throws<InvalidOperationException>(() => writer.Advance(1));
        Assert.Equal("Cannot advance past the end of the reserved buffer segment.", exception.Message);
    }

    /// <summary>
    /// Tests that Reset allows the writer to be reused multiple times in a cycle.
    /// Verifies that write, reset, write pattern works correctly without issues.
    /// </summary>
    [Fact]
    public void Reset_MultipleWriteResetCycles_WorksCorrectly()
    {
        // Arrange
        var writer = new ResizableSpanWriter<byte>(initialCapacity: 10);

        // Act & Assert - Cycle 1
        writer.Write([1, 2, 3]);
        Assert.Equal(new byte[] { 1, 2, 3 }, writer.WrittenSpan.ToArray());
        writer.Reset();
        Assert.Equal(0, writer.WrittenSpan.Length);

        // Act & Assert - Cycle 2
        writer.Write([4, 5]);
        Assert.Equal(new byte[] { 4, 5 }, writer.WrittenSpan.ToArray());
        writer.Reset();
        Assert.Equal(0, writer.WrittenSpan.Length);

        // Act & Assert - Cycle 3
        writer.Write([6, 7, 8, 9]);
        Assert.Equal(new byte[] { 6, 7, 8, 9 }, writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Tests that the constructor with default parameter (no argument) creates a functional writer
    /// with zero initial capacity.
    /// </summary>
    [Fact]
    public void Constructor_WithDefaultParameter_CreatesWriterWithZeroCapacity()
    {
        // Arrange & Act
        var writer = new ResizableSpanWriter<byte>();

        // Assert
        Assert.Equal(0, writer.WrittenSpan.Length);
        Assert.Equal(0, ((IMemoryOwner<byte>)writer).Memory.Length);
    }

    /// <summary>
    /// Tests that the constructor with explicit zero capacity creates a functional writer
    /// with zero initial capacity.
    /// </summary>
    [Fact]
    public void Constructor_WithZeroCapacity_CreatesWriterWithZeroCapacity()
    {
        // Arrange & Act
        var writer = new ResizableSpanWriter<int>(initialCapacity: 0);

        // Assert
        Assert.Equal(0, writer.WrittenSpan.Length);
        Assert.Equal(0, ((IMemoryOwner<int>)writer).Memory.Length);
    }

    /// <summary>
    /// Tests that the constructor with various positive capacities creates a functional writer
    /// with at least the requested capacity.
    /// </summary>
    /// <param name="initialCapacity">The initial capacity to test.</param>
    [Theory]
    [InlineData(1)]
    [InlineData(8)]
    [InlineData(16)]
    [InlineData(100)]
    [InlineData(1024)]
    [InlineData(10000)]
    public void Constructor_WithPositiveCapacity_CreatesWriterWithAtLeastRequestedCapacity(int initialCapacity)
    {
        // Arrange & Act
        var writer = new ResizableSpanWriter<byte>(initialCapacity);

        // Assert
        Assert.Equal(0, writer.WrittenSpan.Length);
        Assert.True(((IMemoryOwner<byte>)writer).Memory.Length >= initialCapacity);
    }

    /// <summary>
    /// Tests that the constructor with positive capacity creates a writer that can successfully
    /// write data and grow as needed.
    /// </summary>
    [Fact]
    public void Constructor_WithPositiveCapacity_CreatesUsableWriter()
    {
        // Arrange
        var writer = new ResizableSpanWriter<byte>(initialCapacity: 10);

        // Act
        writer.Write([1, 2, 3, 4, 5]);

        // Assert
        Assert.Equal(5, writer.WrittenSpan.Length);
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Tests that the constructor with negative capacity throws ArgumentOutOfRangeException.
    /// </summary>
    /// <param name="negativeCapacity">The negative capacity value to test.</param>
    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void Constructor_WithNegativeCapacity_ThrowsArgumentOutOfRangeException(int negativeCapacity)
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new ResizableSpanWriter<byte>(negativeCapacity));
    }

    [Fact]
    public void Constructor_WithNullPoolAndZeroCapacity_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new ResizableSpanWriter<byte>(null!, initialCapacity: 0));
        Assert.Equal("pool", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullPoolAndPositiveCapacity_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new ResizableSpanWriter<byte>(null!, initialCapacity: 10));
        Assert.Equal("pool", ex.ParamName);
    }

    [Fact]
    public void GetSpan_WithMaxIntSizeHint_ThrowsOverflowException()
    {
        using var writer = new ResizableSpanWriter<byte>(initialCapacity: 0);
        Assert.Throws<OverflowException>(() => writer.GetSpan(int.MaxValue));
    }

    [Fact]
    public void GetMemory_WithMaxIntSizeHint_ThrowsOverflowException()
    {
        using var writer = new ResizableSpanWriter<byte>(initialCapacity: 0);
        Assert.Throws<OverflowException>(() => writer.GetMemory(int.MaxValue));
    }

    /// <summary>
    /// Tests that the constructor with zero capacity creates a writer that can grow dynamically
    /// when data is written.
    /// </summary>
    [Fact]
    public void Constructor_WithZeroCapacity_AllowsDynamicGrowth()
    {
        // Arrange
        var writer = new ResizableSpanWriter<int>(initialCapacity: 0);

        // Act
        writer.Write([10, 20, 30]);

        // Assert
        Assert.Equal(3, writer.WrittenSpan.Length);
        Assert.Equal(new int[] { 10, 20, 30 }, writer.WrittenSpan.ToArray());
        Assert.True(((IMemoryOwner<int>)writer).Memory.Length >= 3);
    }

    /// <summary>
    /// Tests that the constructor with large capacity creates a writer without throwing exceptions.
    /// </summary>
    [Fact]
    public void Constructor_WithLargeCapacity_CreatesWriterSuccessfully()
    {
        // Arrange & Act
        var writer = new ResizableSpanWriter<byte>(initialCapacity: 1000000);

        // Assert
        Assert.Equal(0, writer.WrittenSpan.Length);
        Assert.True(((IMemoryOwner<byte>)writer).Memory.Length >= 1000000);
    }

    /// <summary>
    /// Tests that the constructor creates a writer that can be properly disposed.
    /// </summary>
    [Fact]
    public void Constructor_CreatesDisposableWriter()
    {
        // Arrange
        var writer = new ResizableSpanWriter<byte>(initialCapacity: 16);
        writer.Write([1, 2, 3]);

        // Act
        writer.Dispose();

        // Assert
        Assert.Throws<ObjectDisposedException>(() => _ = writer.WrittenSpan);
    }

    /// <summary>
    /// Tests that the constructor with int.MaxValue capacity throws or handles gracefully.
    /// Note: This may throw due to memory constraints, which is acceptable.
    /// </summary>
    [Fact]
    public void Constructor_WithMaxIntCapacity_ThrowsOrHandlesGracefully()
    {
        // Arrange, Act & Assert
        // This is expected to throw OutOfMemoryException or similar, which is acceptable
        try
        {
            var writer = new ResizableSpanWriter<byte>(int.MaxValue);
            // If it somehow succeeds (unlikely), verify basic functionality
            Assert.Equal(0, writer.WrittenSpan.Length);
        }
        catch (OutOfMemoryException)
        {
            // Expected and acceptable
        }
        catch (ArgumentOutOfRangeException)
        {
            // Also acceptable if ArrayPool refuses such large requests
        }
    }

    /// <summary>
    /// Verifies that Write with a null array is handled gracefully without throwing an exception.
    /// A null array should be treated as an empty span and result in no-op behavior.
    /// </summary>
    [Fact]
    public void WriteArray_NullArray_DoesNotThrow()
    {
        // Arrange
        var writer = new ResizableSpanWriter<int>(initialCapacity: 4);
        int[]? nullArray = null;

        // Act
        writer.Write(nullArray!);

        // Assert
        Assert.Equal(0, writer.WrittenSpan.Length);
        Assert.Empty(writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Verifies that Write with an empty array does not modify the writer state.
    /// An empty array should result in no items being written.
    /// </summary>
    [Fact]
    public void WriteArray_EmptyArray_DoesNothing()
    {
        // Arrange
        var writer = new ResizableSpanWriter<byte>(initialCapacity: 4);
        byte[] emptyArray = [];

        // Act
        writer.Write(emptyArray);

        // Assert
        Assert.Equal(0, writer.WrittenSpan.Length);
        Assert.Empty(writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Verifies that Write with a single-element array correctly appends the element.
    /// Tests the boundary case of an array with exactly one element.
    /// </summary>
    [Fact]
    public void WriteArray_SingleElement_Appends()
    {
        // Arrange
        var writer = new ResizableSpanWriter<int>(initialCapacity: 4);
        int[] singleElement = [42];

        // Act
        writer.Write(singleElement);

        // Assert
        Assert.Equal(1, writer.WrittenSpan.Length);
        Assert.Equal(42, writer.WrittenSpan[0]);
    }

    /// <summary>
    /// Verifies that Write with an array that fits within initial capacity appends correctly.
    /// Tests that no growth occurs when capacity is sufficient.
    /// </summary>
    [Fact]
    public void WriteArray_WithinCapacity_AppendsWithoutGrowth()
    {
        // Arrange
        var writer = new ResizableSpanWriter<int>(initialCapacity: 8);
        int[] smallArray = [1, 2, 3, 4];

        // Act
        writer.Write(smallArray);

        // Assert
        Assert.Equal(4, writer.WrittenSpan.Length);
        Assert.Equal(new int[] { 1, 2, 3, 4 }, writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Verifies that Write with an array larger than current capacity triggers buffer growth.
    /// The writer should automatically resize and preserve all data.
    /// </summary>
    [Fact]
    public void WriteArray_ExceedsCapacity_GrowsBuffer()
    {
        // Arrange
        var writer = new ResizableSpanWriter<int>(initialCapacity: 2);
        int[] largeArray = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

        // Act
        writer.Write(largeArray);

        // Assert
        Assert.Equal(10, writer.WrittenSpan.Length);
        Assert.Equal(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Verifies that multiple consecutive array writes accumulate correctly.
    /// Tests that the writer correctly maintains state across multiple Write operations.
    /// </summary>
    [Fact]
    public void WriteArray_MultipleArrays_Accumulates()
    {
        // Arrange
        var writer = new ResizableSpanWriter<char>(initialCapacity: 4);
        char[] first = ['a', 'b'];
        char[] second = ['c', 'd', 'e'];
        char[] third = ['f'];

        // Act
        writer.Write(first);
        writer.Write(second);
        writer.Write(third);

        // Assert
        Assert.Equal(6, writer.WrittenSpan.Length);
        Assert.Equal("abcdef", new string(writer.WrittenSpan));
    }

    /// <summary>
    /// Verifies that Write with a very large array triggers appropriate buffer growth.
    /// Tests extreme growth scenario with a large number of elements.
    /// </summary>
    [Fact]
    public void WriteArray_VeryLargeArray_GrowsAppropriately()
    {
        // Arrange
        var writer = new ResizableSpanWriter<int>(initialCapacity: 4);
        int[] largeArray = new int[10000];
        for (int i = 0; i < largeArray.Length; i++)
        {
            largeArray[i] = i;
        }

        // Act
        writer.Write(largeArray);

        // Assert
        Assert.Equal(10000, writer.WrittenSpan.Length);
        Assert.Equal(0, writer.WrittenSpan[0]);
        Assert.Equal(9999, writer.WrittenSpan[9999]);
        Assert.Equal(largeArray, writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Verifies that Write with an array after disposal throws ObjectDisposedException.
    /// Tests that the disposal guard is properly enforced.
    /// </summary>
    [Fact]
    public void WriteArray_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var writer = new ResizableSpanWriter<int>(initialCapacity: 4);
        int[] array = [1, 2, 3];
        writer.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => writer.Write(array));
    }

    /// <summary>
    /// Verifies that Write with an array correctly invalidates outstanding GetSpan reservations.
    /// Tests that _available is reset to 0 after a direct Write call.
    /// </summary>
    [Fact]
    public void WriteArray_InvalidatesGetSpanReservation()
    {
        // Arrange
        var writer = new ResizableSpanWriter<int>(initialCapacity: 8);
        var span = writer.GetSpan(4); // Reserve 4 items

        // Act
        writer.Write([10, 20]); // This should invalidate the reservation

        // Assert
        // Attempting to advance by 4 should fail since the reservation was invalidated
        Assert.Throws<InvalidOperationException>(() => writer.Advance(4));
    }

    /// <summary>
    /// Verifies that Write with mixed types of arrays (different lengths) works correctly.
    /// Tests boundary transitions between empty, small, and larger arrays.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void WriteArray_VariousLengths_AppendsCorrectly(int length)
    {
        // Arrange
        var writer = new ResizableSpanWriter<byte>(initialCapacity: 4);
        byte[] array = new byte[length];
        for (int i = 0; i < length; i++)
        {
            array[i] = (byte)(i % 256);
        }

        // Act
        writer.Write(array);

        // Assert
        Assert.Equal(length, writer.WrittenSpan.Length);
        if (length > 0)
        {
            Assert.Equal(array, writer.WrittenSpan.ToArray());
        }
    }

    /// <summary>
    /// Verifies that Write correctly preserves existing content when appending a new array.
    /// Tests that previously written data is not corrupted during subsequent Write operations.
    /// </summary>
    [Fact]
    public void WriteArray_PreservesExistingContent()
    {
        // Arrange
        var writer = new ResizableSpanWriter<int>(initialCapacity: 4);
        writer.Write(1);
        writer.Write(2);
        int[] array = [3, 4, 5];

        // Act
        writer.Write(array);

        // Assert
        Assert.Equal(5, writer.WrittenSpan.Length);
        Assert.Equal(new int[] { 1, 2, 3, 4, 5 }, writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Verifies that Write with an array works correctly after Reset.
    /// Tests that the writer state is properly reset and can be reused.
    /// </summary>
    [Fact]
    public void WriteArray_AfterReset_StartsFromBeginning()
    {
        // Arrange
        var writer = new ResizableSpanWriter<int>(initialCapacity: 8);
        writer.Write([1, 2, 3]);
        writer.Reset();
        int[] newArray = [10, 20];

        // Act
        writer.Write(newArray);

        // Assert
        Assert.Equal(2, writer.WrittenSpan.Length);
        Assert.Equal(new int[] { 10, 20 }, writer.WrittenSpan.ToArray());
    }
    // ---------- Parameterless constructor tests ----------

    /// <summary>
    /// Tests that the parameterless constructor creates a writer with an empty initial state.
    /// Input: None.
    /// Expected: WrittenSpan and WrittenMemory should both have zero length.
    /// </summary>
    [Fact]
    public void Constructor_NoParameters_CreatesWriterWithEmptyState()
    {
        // Arrange & Act
        using var writer = new ResizableSpanWriter<int>();

        // Assert
        Assert.Equal(0, writer.WrittenSpan.Length);
        Assert.Equal(0, writer.WrittenMemory.Length);
    }

    /// <summary>
    /// Tests that the parameterless constructor creates a writer that can immediately write data.
    /// Input: None, then writes a single byte value.
    /// Expected: The writer should accept the write and store the value correctly.
    /// </summary>
    [Fact]
    public void Constructor_NoParameters_AllowsImmediateWrite()
    {
        // Arrange
        using var writer = new ResizableSpanWriter<byte>();

        // Act
        writer.Write(42);

        // Assert
        Assert.Equal(1, writer.WrittenSpan.Length);
        Assert.Equal(42, writer.WrittenSpan[0]);
    }

    /// <summary>
    /// Tests that the parameterless constructor creates a writer that can write multiple items.
    /// Input: None, then writes an array of characters.
    /// Expected: All items should be written and retrievable.
    /// </summary>
    [Fact]
    public void Constructor_NoParameters_AllowsWritingMultipleItems()
    {
        // Arrange
        using var writer = new ResizableSpanWriter<char>();

        // Act
        writer.Write(['H', 'e', 'l', 'l', 'o']);

        // Assert
        Assert.Equal(5, writer.WrittenSpan.Length);
        Assert.Equal("Hello", new string(writer.WrittenSpan));
    }

    /// <summary>
    /// Tests that the parameterless constructor creates a writer that can be safely disposed.
    /// Input: None.
    /// Expected: Dispose should complete without throwing any exceptions.
    /// </summary>
    [Fact]
    public void Constructor_NoParameters_CanBeDisposedSafely()
    {
        // Arrange
        var writer = new ResizableSpanWriter<double>();

        // Act & Assert - should not throw
        writer.Dispose();
    }

    /// <summary>
    /// Tests that the parameterless constructor works correctly with reference types.
    /// Input: None, then writes string values.
    /// Expected: Reference type items should be stored and retrieved correctly.
    /// </summary>
    [Fact]
    public void Constructor_NoParameters_WorksWithReferenceTypes()
    {
        // Arrange
        using var writer = new ResizableSpanWriter<string>();

        // Act
        writer.Write("test");

        // Assert
        Assert.Equal(1, writer.WrittenSpan.Length);
        Assert.Equal("test", writer.WrittenSpan[0]);
    }

    /// <summary>
    /// Tests that the parameterless constructor initializes IMemoryOwner.Memory correctly.
    /// Input: None.
    /// Expected: Memory should be accessible through the IMemoryOwner interface.
    /// </summary>
    [Fact]
    public void Constructor_NoParameters_InitializesIMemoryOwnerCorrectly()
    {
        // Arrange & Act
        using var writer = new ResizableSpanWriter<int>();
        var memoryOwner = (IMemoryOwner<int>)writer;

        // Assert
        Assert.True(memoryOwner.Memory.Length >= 0);
    }

    /// <summary>
    /// Tests that the parameterless constructor creates a writer supporting IBufferWriter operations.
    /// Input: None, then uses GetSpan and Advance.
    /// Expected: IBufferWriter protocol should work correctly.
    /// </summary>
    [Fact]
    public void Constructor_NoParameters_SupportsIBufferWriterOperations()
    {
        // Arrange
        using var writer = new ResizableSpanWriter<byte>();

        // Act
        var span = writer.GetSpan(4);
        span[0] = 1;
        span[1] = 2;
        span[2] = 3;
        span[3] = 4;
        writer.Advance(4);

        // Assert
        Assert.Equal(4, writer.WrittenSpan.Length);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Tests that the parameterless constructor creates a writer that can be reset.
    /// Input: None, writes data, then resets.
    /// Expected: Reset should clear the written data.
    /// </summary>
    [Fact]
    public void Constructor_NoParameters_AllowsReset()
    {
        // Arrange
        using var writer = new ResizableSpanWriter<int>();
        writer.Write([1, 2, 3]);

        // Act
        writer.Reset();

        // Assert
        Assert.Equal(0, writer.WrittenSpan.Length);
    }

    /// <summary>
    /// Tests that multiple instances created with the parameterless constructor are independent.
    /// Input: Creates two writers and writes different data to each.
    /// Expected: Each writer should maintain its own independent state.
    /// </summary>
    [Fact]
    public void Constructor_NoParameters_CreatesIndependentInstances()
    {
        // Arrange
        using var writer1 = new ResizableSpanWriter<int>();
        using var writer2 = new ResizableSpanWriter<int>();

        // Act
        writer1.Write(100);
        writer2.Write(200);

        // Assert
        Assert.Equal(1, writer1.WrittenSpan.Length);
        Assert.Equal(100, writer1.WrittenSpan[0]);
        Assert.Equal(1, writer2.WrittenSpan.Length);
        Assert.Equal(200, writer2.WrittenSpan[0]);
    }
}



/// <summary>
/// Unit tests for the Dispose method of ResizableSpanWriter.
/// </summary>
public sealed partial class ResizableSpanWriterTests_Dispose
{
    /// <summary>
    /// Tests that Dispose with initialCapacity of 0 does not return the empty array to the pool.
    /// The empty array has Length == 0, so the condition _array.Length > 0 is false.
    /// </summary>
    [Fact]
    public void Dispose_WithInitialCapacityZero_DoesNotReturnEmptyArrayToPool()
    {
        // Arrange
        var pool = new TrackingArrayPool<byte>();
        var writer = new ResizableSpanWriter<byte>(pool, initialCapacity: 0);

        // Act
        writer.Dispose();

        // Assert
        Assert.Equal(0, pool.ReturnedCount);
    }

    /// <summary>
    /// Tests that Dispose immediately after construction with non-zero capacity returns buffer to pool.
    /// Verifies that a buffer is returned even without any write operations.
    /// </summary>
    [Fact]
    public void Dispose_ImmediatelyAfterConstruction_ReturnsBufferToPool()
    {
        // Arrange
        var pool = new TrackingArrayPool<int>();
        var writer = new ResizableSpanWriter<int>(pool, initialCapacity: 16);

        // Act
        writer.Dispose();

        // Assert
        Assert.Equal(1, pool.ReturnedCount);
    }

    /// <summary>
    /// Tests that Dispose with reference type correctly passes the clearArray parameter.
    /// Reference types should have clearArray set to true via RuntimeHelpers.IsReferenceOrContainsReferences.
    /// </summary>
    [Fact]
    public void Dispose_WithReferenceType_PassesClearArrayTrue()
    {
        // Arrange
        var pool = new TrackingArrayPool<string>();
        var writer = new ResizableSpanWriter<string>(pool, initialCapacity: 8);
        writer.Write(["test"]);

        // Act
        writer.Dispose();

        // Assert
        Assert.Equal(1, pool.ReturnedCount);
        Assert.True(pool.LastClearArray);
    }

    /// <summary>
    /// Tests that Dispose with value type correctly passes the clearArray parameter.
    /// Pure value types should have clearArray set to false via RuntimeHelpers.IsReferenceOrContainsReferences.
    /// </summary>
    [Fact]
    public void Dispose_WithValueType_PassesClearArrayFalse()
    {
        // Arrange
        var pool = new TrackingArrayPool<int>();
        var writer = new ResizableSpanWriter<int>(pool, initialCapacity: 8);
        writer.Write([1, 2, 3]);

        // Act
        writer.Dispose();

        // Assert
        Assert.Equal(1, pool.ReturnedCount);
        Assert.False(pool.LastClearArray);
    }

    /// <summary>
    /// Tests that calling Dispose multiple times on an instance with initialCapacity: 0 is safe.
    /// Verifies idempotency for the empty array edge case.
    /// </summary>
    [Fact]
    public void Dispose_MultipleTimesWithEmptyArray_IsSafe()
    {
        // Arrange
        var pool = new TrackingArrayPool<byte>();
        var writer = new ResizableSpanWriter<byte>(pool, initialCapacity: 0);

        // Act
        writer.Dispose();
        writer.Dispose();
        writer.Dispose();

        // Assert
        Assert.Equal(0, pool.ReturnedCount);
    }

    /// <summary>
    /// Tracking pool that records Return calls and clearArray parameter values.
    /// </summary>
    private sealed class TrackingArrayPool<T> : ArrayPool<T>
    {
        public int RentedCount { get; private set; }
        public int ReturnedCount { get; private set; }
        public bool LastClearArray { get; private set; }

        public override T[] Rent(int minimumLength)
        {
            RentedCount++;
            return new T[minimumLength];
        }

        public override void Return(T[] array, bool clearArray = false)
        {
            if (array == null || array.Length == 0)
                return;

            ReturnedCount++;
            LastClearArray = clearArray;
        }
    }

    // -------- Constructor validation --------

    [Fact]
    public void Constructor_NullPool_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new ResizableSpanWriter<byte>(null!, 8));
        Assert.Equal("pool", ex.ParamName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void Constructor_NegativeCapacity_ThrowsArgumentOutOfRangeException(int capacity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ResizableSpanWriter<byte>(capacity));
    }

    // -------- Negative sizeHint --------

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void GetSpan_NegativeSizeHint_ThrowsArgumentOutOfRangeException(int sizeHint)
    {
        using var w = new ResizableSpanWriter<byte>();
        Assert.Throws<ArgumentOutOfRangeException>(() => w.GetSpan(sizeHint));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void GetMemory_NegativeSizeHint_ThrowsArgumentOutOfRangeException(int sizeHint)
    {
        using var w = new ResizableSpanWriter<byte>();
        Assert.Throws<ArgumentOutOfRangeException>(() => w.GetMemory(sizeHint));
    }

    // -------- Advance(0) behavior --------

    [Fact]
    public void Advance_Zero_WithReservation_ClearsReservation()
    {
        using var w = new ResizableSpanWriter<byte>();
        _ = w.GetSpan(16);

        w.Advance(0);

        Assert.Throws<InvalidOperationException>(() => w.Advance(1));
        Assert.Equal(0, w.WrittenSpan.Length);
    }

    [Fact]
    public void Advance_Zero_WithoutReservation_IsNoOp()
    {
        using var w = new ResizableSpanWriter<byte>(initialCapacity: 0);
        w.Advance(0);
        Assert.Equal(0, w.WrittenSpan.Length);
    }

    // -------- Reference type Reset clears array --------

    [Fact]
    public void Reset_ReferenceType_ClearsWrittenElements()
    {
        var pool = new TrackingArrayPool<string>();
        var w = new ResizableSpanWriter<string>(pool, initialCapacity: 4);
        w.Write("hello");
        w.Write("world");

        w.Reset();

        Assert.Equal(0, w.WrittenSpan.Length);
        // After reset, write new data to verify no stale references
        w.Write("fresh");
        Assert.Equal(new[] { "fresh" }, w.WrittenSpan.ToArray());
        w.Dispose();
    }

    [Fact]
    public void Dispose_ReferenceType_PassesClearArrayTrue()
    {
        var pool = new TrackingArrayPool<string>();
        var w = new ResizableSpanWriter<string>(pool, initialCapacity: 4);
        w.Write("test");
        w.Dispose();

        Assert.True(pool.LastClearArray);
    }

    [Fact]
    public void Dispose_ValueType_PassesClearArrayFalse()
    {
        var pool = new TrackingArrayPool<int>();
        var w = new ResizableSpanWriter<int>(pool, initialCapacity: 4);
        w.Write(42);
        w.Dispose();

        Assert.False(pool.LastClearArray);
    }

    // -------- WrittenMemory property --------

    [Fact]
    public void WrittenMemory_ReturnsCorrectData()
    {
        using var w = new ResizableSpanWriter<int>();
        w.Write(1);
        w.Write(2);
        w.Write(3);

        var mem = w.WrittenMemory;

        Assert.Equal(3, mem.Length);
        Assert.Equal(new[] { 1, 2, 3 }, mem.ToArray());
    }

    [Fact]
    public void WrittenMemory_Empty_ReturnsEmptyMemory()
    {
        using var w = new ResizableSpanWriter<byte>();
        Assert.Equal(0, w.WrittenMemory.Length);
    }

    [Fact]
    public void WrittenMemory_AfterDispose_Throws()
    {
        var w = new ResizableSpanWriter<int>();
        w.Write(42);
        w.Dispose();

        Assert.Throws<ObjectDisposedException>(() => _ = w.WrittenMemory);
    }

    // -------- Empty writes --------

    [Fact]
    public void Write_EmptySpan_IsNoOp()
    {
        using var w = new ResizableSpanWriter<int>();
        w.Write(42);
        w.Write(ReadOnlySpan<int>.Empty);

        Assert.Equal(1, w.WrittenSpan.Length);
        Assert.Equal(42, w.WrittenSpan[0]);
    }

    [Fact]
    public void Write_EmptyArray_IsNoOp()
    {
        using var w = new ResizableSpanWriter<byte>();
        w.Write(1);
        w.Write(Array.Empty<byte>());

        Assert.Equal(1, w.WrittenSpan.Length);
    }

    [Fact]
    public void Write_EmptyMemory_IsNoOp()
    {
        using var w = new ResizableSpanWriter<char>();
        w.Write('x');
        w.Write(ReadOnlyMemory<char>.Empty);

        Assert.Equal(1, w.WrittenSpan.Length);
    }

    // -------- Over-advance --------

    [Fact]
    public void Advance_OverReservation_ThrowsInvalidOperationException()
    {
        using var w = new ResizableSpanWriter<byte>();
        _ = w.GetSpan(4);

        Assert.Throws<InvalidOperationException>(() => w.Advance(5));
    }

    // -------- Interleaved GetSpan/GetMemory --------

    [Fact]
    public void GetMemory_After_GetSpan_Replaces_Reservation()
    {
        using var w = new ResizableSpanWriter<int>();
        _ = w.GetSpan(4);
        var mem = w.GetMemory(8);
        mem.Span[0] = 77;
        w.Advance(1);

        Assert.Equal(new[] { 77 }, w.WrittenSpan.ToArray());
    }

    // -------- Multiple Reset cycles --------

    [Fact]
    public void MultipleResetCycles_WriteCorrectly()
    {
        using var w = new ResizableSpanWriter<int>(initialCapacity: 4);

        for (int cycle = 0; cycle < 5; cycle++)
        {
            w.Write(cycle);
            w.Write(cycle * 10);
            Assert.Equal(2, w.WrittenSpan.Length);
            Assert.Equal(cycle, w.WrittenSpan[0]);
            Assert.Equal(cycle * 10, w.WrittenSpan[1]);
            w.Reset();
            Assert.Equal(0, w.WrittenSpan.Length);
        }
    }

    // -------- Large single write --------

    [Fact]
    public void Write_LargePayload_PreservesIntegrity()
    {
        using var w = new ResizableSpanWriter<int>(initialCapacity: 0);
        var payload = Enumerable.Range(0, 50_000).ToArray();

        w.Write(payload);

        Assert.Equal(50_000, w.WrittenSpan.Length);
        Assert.True(w.WrittenSpan.SequenceEqual(payload));
    }

    // -------- Constructor pool-only --------

    [Fact]
    public void Constructor_PoolOnly_CreatesEmptyWriter()
    {
        using var w = new ResizableSpanWriter<byte>(ArrayPool<byte>.Shared);
        Assert.Equal(0, w.WrittenSpan.Length);
    }

    // -------- IMemoryOwner after dispose --------

    [Fact]
    public void IMemoryOwner_Memory_AfterDispose_Throws()
    {
        var w = new ResizableSpanWriter<byte>();
        w.Dispose();

        Assert.Throws<ObjectDisposedException>(() => _ = ((IMemoryOwner<byte>)w).Memory);
    }

    // -------- Write after dispose --------

    [Fact]
    public void Write_AfterDispose_AllOverloads_Throw()
    {
        var w = new ResizableSpanWriter<int>();
        w.Dispose();

        Assert.Throws<ObjectDisposedException>(() => w.Write(1));
        Assert.Throws<ObjectDisposedException>(() => w.Write(new[] { 1 }));
        Assert.Throws<ObjectDisposedException>(() => w.Write(new ReadOnlyMemory<int>([1])));
        Assert.Throws<ObjectDisposedException>(() => w.Reset());
    }
}
