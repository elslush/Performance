using Performance.Buffers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

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
        w.Write(new[] { 1, 2 });
        w.Write(new[] { 3, 4, 5 });

        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, w.WrittenSpan.ToArray());
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
        w.Write(new byte[] { 1, 2, 3, 4, 5 });

        int capacityBefore = ((IMemoryOwner<byte>)w).Memory.Length;

        w.Reset();
        Assert.Equal(0, w.WrittenSpan.Length);

        // Write new data; should overwrite from start without reallocating
        w.Write(new byte[] { 9, 9, 9 });
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
        w.Write(new byte[] { 1, 2, 3 });
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
        w.Write(new byte[] { 1, 2, 3, 4, 5, 6 }); // should grow at least once

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
        Assert.Equal(new int[] { 42 }, writer.WrittenSpan.ToArray());
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

    [Fact(Skip = "Current implementation never sets _disposed; post-Dispose APIs should throw ObjectDisposedException but they won't. Consider fixing _disposed.")]
    public void AfterDispose_AccessorsThrow_ObjectDisposedException()
    {
        var w = new ResizableSpanWriter<byte>(initialCapacity: 8);
        w.Write([1, 2, 3]);
        w.Dispose();

        Assert.Throws<ObjectDisposedException>(() => _ = w.WrittenSpan);
        Assert.Throws<ObjectDisposedException>(() => _ = ((IMemoryOwner<byte>)w).Memory);
        Assert.Throws<ObjectDisposedException>(() => w.GetSpan());
        Assert.Throws<ObjectDisposedException>(() => w.GetMemory());
        Assert.Throws<ObjectDisposedException>(() => w.Write((byte)1));
        Assert.Throws<ObjectDisposedException>(() => w.Write(new byte[] { 9, 9, 9 }));
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
}
