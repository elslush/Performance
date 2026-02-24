using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Performance.Buffers;

using Xunit;

namespace Performance.Tests;

public sealed class ReusableLinkedArrayBufferWriterTests
{
    [Fact]
    public void Constructor_WithoutFirstBuffer_UsesSentinel()
    {
        var writer = new ReusableLinkedArrayBufferWriter(useFirstBuffer: false, pinned: false);

        Assert.Empty(writer.DangerousGetFirstBuffer());
        Assert.Equal(0, writer.TotalWritten);
        Assert.Equal(0, writer.WrittenCount);
        Assert.Equal(ReadOnlyMemory<byte>.Empty, writer.WrittenMemory);
    }

    [Fact]
    public void Constructor_WithFirstBuffer_AllocatesInitialBuffer()
    {
        var writer = new ReusableLinkedArrayBufferWriter(useFirstBuffer: true, pinned: false);

        Assert.True(writer.DangerousGetFirstBuffer().Length >= 262_144);
        Assert.Empty(writer.WrittenSpan.ToArray());
    }

    [Fact]
    public void GetSpan_DefaultSizeHint_ReturnsWritableSpan()
    {
        var writer = new ReusableLinkedArrayBufferWriter(useFirstBuffer: false, pinned: false);

        var span = writer.GetSpan();

        Assert.True(span.Length >= 8);
    }

    [Fact]
    public void GetMemory_DefaultSizeHint_ReturnsWritableMemory()
    {
        var writer = new ReusableLinkedArrayBufferWriter(useFirstBuffer: false, pinned: false);

        var memory = writer.GetMemory();

        Assert.True(memory.Length >= 8);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void GetSpan_NegativeSizeHint_ThrowsArgumentOutOfRangeException(int sizeHint)
    {
        var writer = new ReusableLinkedArrayBufferWriter(useFirstBuffer: false, pinned: false);

        Assert.Throws<ArgumentOutOfRangeException>(() => writer.GetSpan(sizeHint));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void GetMemory_NegativeSizeHint_ThrowsArgumentOutOfRangeException(int sizeHint)
    {
        var writer = new ReusableLinkedArrayBufferWriter(useFirstBuffer: false, pinned: false);

        Assert.Throws<ArgumentOutOfRangeException>(() => writer.GetMemory(sizeHint));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Advance_Negative_ThrowsArgumentOutOfRangeException(int count)
    {
        var writer = new ReusableLinkedArrayBufferWriter(useFirstBuffer: false, pinned: false);

        Assert.Throws<ArgumentOutOfRangeException>(() => writer.Advance(count));
    }

    [Fact]
    public void Advance_WithoutReservation_ThrowsInvalidOperationException()
    {
        var writer = new ReusableLinkedArrayBufferWriter(useFirstBuffer: false, pinned: false);

        var ex = Assert.Throws<InvalidOperationException>(() => writer.Advance(1));
        Assert.Equal("Cannot advance past the end of the reserved buffer segment.", ex.Message);
    }

    [Fact]
    public void Advance_ZeroWithoutReservation_IsNoOp()
    {
        var writer = new ReusableLinkedArrayBufferWriter(useFirstBuffer: false, pinned: false);

        writer.Advance(0);

        Assert.Equal(0, writer.TotalWritten);
    }

    [Fact]
    public void Advance_OverReservation_ThrowsInvalidOperationException()
    {
        var writer = new ReusableLinkedArrayBufferWriter(useFirstBuffer: false, pinned: false);
        _ = writer.GetSpan(4);

        Assert.Throws<InvalidOperationException>(() => writer.Advance(int.MaxValue));
    }

    [Fact]
    public void Advance_ZeroWithReservation_ClearsReservation()
    {
        var writer = new ReusableLinkedArrayBufferWriter(useFirstBuffer: false, pinned: false);
        _ = writer.GetSpan(16);

        writer.Advance(0);

        Assert.Throws<InvalidOperationException>(() => writer.Advance(1));
    }

    [Fact]
    public void GetSpan_WriteThenAdvance_WritesData()
    {
        var writer = new ReusableLinkedArrayBufferWriter(useFirstBuffer: false, pinned: false);
        var span = writer.GetSpan(4);
        span[0] = 1;
        span[1] = 2;
        span[2] = 3;
        span[3] = 4;

        writer.Advance(4);

        Assert.Equal(new byte[] { 1, 2, 3, 4 }, writer.WrittenSpan.ToArray());
        Assert.Equal(4, writer.TotalWritten);
    }

    [Fact]
    public void GetMemory_WriteThenAdvance_WritesData()
    {
        var writer = new ReusableLinkedArrayBufferWriter(useFirstBuffer: false, pinned: false);
        var memory = writer.GetMemory(3);
        memory.Span[0] = 9;
        memory.Span[1] = 8;
        memory.Span[2] = 7;

        writer.Advance(3);

        Assert.Equal(new byte[] { 9, 8, 7 }, writer.WrittenMemory.ToArray());
    }

    [Fact]
    public void WriteByte_Appends()
    {
        var writer = new ReusableLinkedArrayBufferWriter(useFirstBuffer: false, pinned: false);

        writer.WriteByte(1);
        writer.WriteByte(2);
        writer.WriteByte(3);

        Assert.Equal(new byte[] { 1, 2, 3 }, writer.WrittenSpan.ToArray());
    }

    [Fact]
    public void Write_Array_Span_Memory_Accumulates()
    {
        var writer = new ReusableLinkedArrayBufferWriter(useFirstBuffer: false, pinned: false);

        writer.Write(new byte[] { 1, 2 });
        writer.Write(new byte[] { 3, 4 }.AsSpan());
        writer.Write(new byte[] { 5, 6 }.AsMemory());

        Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 6 }, writer.WrittenMemory.ToArray());
    }

    [Fact]
    public void Write_EmptySpan_DoesNotChangeContent()
    {
        var writer = new ReusableLinkedArrayBufferWriter(useFirstBuffer: false, pinned: false);
        writer.Write(new byte[] { 1, 2, 3 });

        writer.Write(ReadOnlySpan<byte>.Empty);

        Assert.Equal(new byte[] { 1, 2, 3 }, writer.WrittenSpan.ToArray());
    }

    [Fact]
    public void Write_NullArray_ThrowsArgumentNullException()
    {
        var writer = new ReusableLinkedArrayBufferWriter(useFirstBuffer: false, pinned: false);

        Assert.Throws<ArgumentNullException>(() => writer.Write((byte[])null!));
    }

    [Fact]
    public void DirectWrite_InvalidatesOutstandingReservation()
    {
        var writer = new ReusableLinkedArrayBufferWriter(useFirstBuffer: false, pinned: false);
        _ = writer.GetSpan(32);

        writer.Write(new byte[] { 7, 8, 9 });

        Assert.Throws<InvalidOperationException>(() => writer.Advance(1));
        Assert.Equal(new byte[] { 7, 8, 9 }, writer.WrittenSpan.ToArray());
    }

    [Fact]
    public void WriteByte_InvalidatesOutstandingReservation()
    {
        var writer = new ReusableLinkedArrayBufferWriter(useFirstBuffer: false, pinned: false);
        _ = writer.GetMemory(32);

        writer.WriteByte(42);

        Assert.Throws<InvalidOperationException>(() => writer.Advance(1));
        Assert.Equal(new byte[] { 42 }, writer.WrittenSpan.ToArray());
    }

    [Fact]
    public void Reset_OnEmptyWriter_IsSafe()
    {
        var writer = new ReusableLinkedArrayBufferWriter(useFirstBuffer: false, pinned: false);

        writer.Reset();
        writer.Reset();

        Assert.Equal(0, writer.TotalWritten);
    }

    [Fact]
    public void Reset_AfterWrites_ClearsContent()
    {
        var writer = new ReusableLinkedArrayBufferWriter(useFirstBuffer: false, pinned: false);
        writer.Write(new byte[] { 1, 2, 3 });

        writer.Reset();

        Assert.Equal(0, writer.TotalWritten);
        Assert.Empty(writer.WrittenSpan.ToArray());
    }

    [Fact]
    public void Reset_AfterGetSpanWithoutAdvance_ClearsReservation()
    {
        var writer = new ReusableLinkedArrayBufferWriter(useFirstBuffer: false, pinned: false);
        _ = writer.GetSpan(10);

        writer.Reset();

        Assert.Throws<InvalidOperationException>(() => writer.Advance(1));
    }

    [Fact]
    public void Reset_AfterGetMemoryWithoutAdvance_ClearsReservation()
    {
        var writer = new ReusableLinkedArrayBufferWriter(useFirstBuffer: false, pinned: false);
        _ = writer.GetMemory(10);

        writer.Reset();

        Assert.Throws<InvalidOperationException>(() => writer.Advance(1));
    }

    [Fact]
    public void ToArray_Empty_ReturnsArrayEmpty()
    {
        var writer = new ReusableLinkedArrayBufferWriter(useFirstBuffer: false, pinned: false);

        var data = writer.ToArray();

        Assert.Empty(data);
    }

    [Fact]
    public void ToArray_DoesNotResetWriter()
    {
        var writer = new ReusableLinkedArrayBufferWriter(useFirstBuffer: false, pinned: false);
        writer.Write(new byte[] { 1, 2, 3 });

        var data = writer.ToArray();

        Assert.Equal(new byte[] { 1, 2, 3 }, data);
        Assert.Equal(3, writer.TotalWritten);
        Assert.Equal(new byte[] { 1, 2, 3 }, writer.WrittenSpan.ToArray());
    }

    [Fact]
    public void ToArrayAndReset_Empty_ClearsOutstandingReservation()
    {
        var writer = new ReusableLinkedArrayBufferWriter(useFirstBuffer: false, pinned: false);
        _ = writer.GetSpan(5);

        var data = writer.ToArrayAndReset();

        Assert.Empty(data);
        Assert.Throws<InvalidOperationException>(() => writer.Advance(1));
    }

    [Fact]
    public void ToArrayAndReset_ReturnsData_AndResetsState()
    {
        var writer = new ReusableLinkedArrayBufferWriter(useFirstBuffer: false, pinned: false);
        writer.Write(new byte[] { 5, 6, 7, 8 });

        var data = writer.ToArrayAndReset();

        Assert.Equal(new byte[] { 5, 6, 7, 8 }, data);
        Assert.Equal(0, writer.TotalWritten);
        Assert.Empty(writer.WrittenSpan.ToArray());
    }

    [Fact]
    public void ToArrayAndReset_MultiSegment_ConcatenatesInOrder()
    {
        var writer = new ReusableLinkedArrayBufferWriter(useFirstBuffer: false, pinned: false);
        var first = CreateSequence(0, 262_144);
        var second = CreateSequence(10, 128);

        writer.Write(first);
        writer.Write(second);

        var data = writer.ToArrayAndReset();

        Assert.Equal(first.Length + second.Length, data.Length);
        Assert.True(data.AsSpan(0, first.Length).SequenceEqual(first));
        Assert.True(data.AsSpan(first.Length).SequenceEqual(second));
        Assert.Equal(0, writer.TotalWritten);
    }

    [Fact]
    public async Task WriteToAndResetAsync_Empty_ClearsOutstandingReservation()
    {
        var writer = new ReusableLinkedArrayBufferWriter(useFirstBuffer: false, pinned: false);
        _ = writer.GetMemory(16);
        await using var stream = new MemoryStream();

        await writer.WriteToAndResetAsync(stream, TestContext.Current.CancellationToken);

        Assert.Empty(stream.ToArray());
        Assert.Throws<InvalidOperationException>(() => writer.Advance(1));
    }

    [Fact]
    public async Task WriteToAndResetAsync_WritesAndResets()
    {
        var writer = new ReusableLinkedArrayBufferWriter(useFirstBuffer: false, pinned: false);
        writer.Write(new byte[] { 4, 5, 6, 7 });
        await using var stream = new MemoryStream();

        await writer.WriteToAndResetAsync(stream, TestContext.Current.CancellationToken);

        Assert.Equal(new byte[] { 4, 5, 6, 7 }, stream.ToArray());
        Assert.Equal(0, writer.TotalWritten);
    }

    [Fact]
    public async Task WriteToAndResetAsync_MultiSegment_WritesAllData()
    {
        var writer = new ReusableLinkedArrayBufferWriter(useFirstBuffer: false, pinned: false);
        var first = CreateSequence(0, 262_144);
        var second = CreateSequence(20, 64);
        writer.Write(first);
        writer.Write(second);
        await using var stream = new MemoryStream();

        await writer.WriteToAndResetAsync(stream, TestContext.Current.CancellationToken);

        var data = stream.ToArray();
        Assert.Equal(first.Length + second.Length, data.Length);
        Assert.True(data.AsSpan(0, first.Length).SequenceEqual(first));
        Assert.True(data.AsSpan(first.Length).SequenceEqual(second));
        Assert.Equal(0, writer.TotalWritten);
    }

    [Fact]
    public void TryGetWrittenMemory_Empty_ReturnsTrueWithEmpty()
    {
        var writer = new ReusableLinkedArrayBufferWriter(useFirstBuffer: false, pinned: false);

        var success = writer.TryGetWrittenMemory(out var memory);

        Assert.True(success);
        Assert.Equal(ReadOnlyMemory<byte>.Empty, memory);
    }

    [Fact]
    public void TryGetWrittenMemory_SingleCurrentSegment_ReturnsTrue()
    {
        var writer = new ReusableLinkedArrayBufferWriter(useFirstBuffer: false, pinned: false);
        writer.Write(new byte[] { 1, 2, 3, 4 });

        var success = writer.TryGetWrittenMemory(out var memory);

        Assert.True(success);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, memory.ToArray());
    }

    [Fact]
    public void TryGetWrittenMemory_UseFirstBufferOnly_ReturnsTrue()
    {
        var writer = new ReusableLinkedArrayBufferWriter(useFirstBuffer: true, pinned: false);
        var span = writer.GetSpan(4);
        span[0] = 11;
        span[1] = 12;
        span[2] = 13;
        span[3] = 14;
        writer.Advance(4);

        var success = writer.TryGetWrittenMemory(out var memory);

        Assert.True(success);
        Assert.Equal(new byte[] { 11, 12, 13, 14 }, memory.ToArray());
    }

    [Fact]
    public void TryGetWrittenMemory_MultiSegment_ReturnsFalse()
    {
        var writer = new ReusableLinkedArrayBufferWriter(useFirstBuffer: false, pinned: false);
        writer.Write(CreateSequence(0, 262_144));
        writer.Write(new byte[] { 1 }); // force next segment

        var success = writer.TryGetWrittenMemory(out var memory);

        Assert.False(success);
        Assert.Equal(default, memory);
    }

    [Fact]
    public void WrittenMemory_MultiSegment_MatchesToArray()
    {
        var writer = new ReusableLinkedArrayBufferWriter(useFirstBuffer: false, pinned: false);
        writer.Write(CreateSequence(0, 262_144));
        writer.Write(CreateSequence(100, 32));

        var written = writer.WrittenMemory.ToArray();
        var copy = writer.ToArray();

        Assert.Equal(copy, written);
    }

    [Fact]
    public void Enumerator_Empty_HasNoItems()
    {
        var writer = new ReusableLinkedArrayBufferWriter(useFirstBuffer: false, pinned: false);

        var enumerator = writer.GetEnumerator();
        var hasValue = enumerator.MoveNext();

        Assert.False(hasValue);
    }

    [Fact]
    public void Enumerator_FirstBufferOnly_YieldsSingleChunk()
    {
        var writer = new ReusableLinkedArrayBufferWriter(useFirstBuffer: true, pinned: false);
        writer.Write(new byte[] { 7, 8, 9 });

        var chunks = writer.GetEnumerator().ToEnumerable().Select(x => x.ToArray()).ToList();

        Assert.Single(chunks);
        Assert.Equal(new byte[] { 7, 8, 9 }, chunks[0]);
    }

    [Fact]
    public void Enumerator_MultiSegment_YieldsChunksInOrder()
    {
        var writer = new ReusableLinkedArrayBufferWriter(useFirstBuffer: false, pinned: false);
        writer.Write(CreateSequence(0, 262_144));
        writer.Write(new byte[] { 9, 8, 7, 6 });

        var chunks = writer.GetEnumerator().ToEnumerable().Select(x => x.ToArray()).ToList();

        Assert.True(chunks.Count >= 2);
        Assert.True(chunks[0].SequenceEqual(CreateSequence(0, 262_144)));
        Assert.True(chunks[^1].SequenceEqual(new byte[] { 9, 8, 7, 6 }));
    }

    [Fact]
    public void Enumerator_Reset_ThrowsNotSupportedException()
    {
        var writer = new ReusableLinkedArrayBufferWriter(useFirstBuffer: false, pinned: false);
        var enumerator = writer.GetEnumerator();

        Assert.Throws<NotSupportedException>(() => enumerator.Reset());
    }

    [Fact]
    public void Enumerator_NonGenericCurrent_ThrowsNotSupportedException()
    {
        var writer = new ReusableLinkedArrayBufferWriter(useFirstBuffer: false, pinned: false);
        writer.Write(new byte[] { 1 });
        IEnumerator enumerator = writer.GetEnumerator();

        _ = enumerator.MoveNext();
        Assert.Throws<NotSupportedException>(() => _ = enumerator.Current);
    }

    [Fact]
    public void Pool_ReturnThenRent_ReusesInstance_AndResets()
    {
        var writer = ReusableLinkedArrayBufferWriterPool.Rent();
        writer.Write(new byte[] { 1, 2, 3 });

        ReusableLinkedArrayBufferWriterPool.Return(writer);
        var reused = ReusableLinkedArrayBufferWriterPool.Rent();

        Assert.Same(writer, reused);
        Assert.Equal(0, reused.TotalWritten);
        Assert.Empty(reused.WrittenSpan.ToArray());
    }

    [Fact]
    public void DangerousFirstBuffer_UseFirstBufferTrue_RetainedAcrossReset()
    {
        var writer = new ReusableLinkedArrayBufferWriter(useFirstBuffer: true, pinned: false);
        var before = writer.DangerousGetFirstBuffer();
        writer.Write(new byte[] { 1, 2, 3 });
        writer.Reset();
        var after = writer.DangerousGetFirstBuffer();

        Assert.Same(before, after);
    }

    private static byte[] CreateSequence(int seed, int count)
    {
        var data = new byte[count];
        for (int i = 0; i < count; i++)
        {
            data[i] = (byte)((seed + i) % 251);
        }
        return data;
    }
}

internal static class ReusableLinkedArrayBufferWriterTestExtensions
{
    public static System.Collections.Generic.IEnumerable<Memory<byte>> ToEnumerable(this ReusableLinkedArrayBufferWriter.Enumerator enumerator)
    {
        while (enumerator.MoveNext())
        {
            yield return enumerator.Current;
        }
    }
}
