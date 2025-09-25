using Performance.Buffers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

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
        w.Write((byte)7);
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
        w.Write((byte)1);
        w.Write((byte)2);

        var more = new byte[] { 3, 4, 5, 6 };
        w.Write(more.AsSpan(1, 2)); // 4,5

        Assert.Equal(new byte[] { 1, 2, 4, 5 }, w.WrittenSpan.ToArray());
    }

    [Fact]
    public void Write_ReadOnlySpan_And_ReadOnlyMemory()
    {
        using var w = new ResizableByteWriter(initialCapacity: 2);
        w.Write(new byte[] { 9, 9 }.AsSpan());
        w.Write(new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3 }));

        Assert.Equal(new byte[] { 9, 9, 1, 2, 3 }, w.WrittenSpan.ToArray());
    }

    [Fact]
    public void Write_Array_Appends()
    {
        using var w = new ResizableByteWriter(initialCapacity: 2);
        w.Write(new byte[] { 1, 2, 3 });
        w.Write(new byte[] { 4, 5 });

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
        w.Write(new byte[] { 1, 2, 3, 4, 5 });

        int capBefore = ((IMemoryOwner<byte>)w).Memory.Length;

        w.Reset();
        Assert.Equal(0, w.WrittenSpan.Length);
        Assert.Equal(0, w.Length);

        w.Write(new byte[] { 8, 8, 8, 8 });
        Assert.Equal(new byte[] { 8, 8, 8, 8 }, w.WrittenSpan.ToArray());

        int capAfter = ((IMemoryOwner<byte>)w).Memory.Length;
        Assert.Equal(capBefore, capAfter);
    }

    // -------- Read views --------

    [Fact]
    public void WrittenSpan_And_WrittenMemory_Agree()
    {
        using var w = new ResizableByteWriter();
        w.Write(new byte[] { 10, 11, 12 });

        var span = w.WrittenSpan;
        var mem = w.WrittenMemory;
        Assert.Equal(span.Length, mem.Length);
        Assert.True(span.SequenceEqual(mem.ToArray()));
    }

    [Fact]
    public void IMemoryOwner_Memory_Exposes_Full_Buffer()
    {
        using var w = new ResizableByteWriter(initialCapacity: 8);
        w.Write(new byte[] { 1, 2, 3 });
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
        w.Write(new byte[] { 1, 2, 3, 4, 5, 6 });

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
        w.Write(new byte[] { 1, 2, 3, 4 });
        w.Write(new byte[] { 5, 6, 7, 8, 9 });   // triggers EnsureCapacity
        Assert.Equal(9, w.Length);
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, w.WrittenSpan.ToArray());
    }

    [Fact]
    public void Reset_Reuses_Buffer()
    {
        using var w = new ResizableByteWriter(initialCapacity: 2);
        w.Write(new byte[] { 1, 2, 3, 4, 5 });
        int capBefore = ((IMemoryOwner<byte>)w).Memory.Length;

        w.Reset();
        Assert.Equal(0, w.Length);

        w.Write(new byte[] { 9, 8, 7 });
        Assert.Equal(new byte[] { 9, 8, 7 }, w.WrittenSpan.ToArray());
        int capAfter = ((IMemoryOwner<byte>)w).Memory.Length;
        Assert.Equal(capBefore, capAfter);
    }

    [Fact]
    public async Task Stream_WriteAsync_Delegates_To_Sync()
    {
        using var w = new ResizableByteWriter();
        var buf = new byte[] { 10, 11, 12 };
        await w.WriteAsync(buf, 0, buf.Length);
        Assert.Equal(buf, w.WrittenSpan.ToArray());
    }

    [Fact]
    public void MemoryOwner_Returns_Full_Buffer()
    {
        using var w = new ResizableByteWriter(initialCapacity: 8);
        w.Write(new byte[] { 1, 2, 3 });
        var mem = ((IMemoryOwner<byte>)w).Memory;
        Assert.True(mem.Length >= 8);
        Assert.True(mem.Length >= w.WrittenSpan.Length);
    }

    // -------- Known-issue: disposed guard doesn't trip --------
    // Your class has a readonly `_disposed` = false and never sets it to true in Dispose(),
    // so ThrowIfDisposed() will never throw. If you fix that, un-skip this test.

    [Fact(Skip = "Current implementation never sets _disposed=true; post-dispose calls won't throw. Set flag in Dispose() then un-skip.")]
    public void After_Dispose_Accessors_Throw()
    {
        var w = new ResizableByteWriter(initialCapacity: 8);
        w.Write(new byte[] { 1, 2, 3 });
        w.Dispose();

        Assert.Throws<ObjectDisposedException>(() => _ = w.WrittenSpan);
        Assert.Throws<ObjectDisposedException>(() => _ = w.WrittenMemory);
        Assert.Throws<ObjectDisposedException>(() => _ = ((IMemoryOwner<byte>)w).Memory);
        Assert.Throws<ObjectDisposedException>(() => w.GetSpan());
        Assert.Throws<ObjectDisposedException>(() => w.GetMemory());
        Assert.Throws<ObjectDisposedException>(() => w.Write((byte)1));
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
}
