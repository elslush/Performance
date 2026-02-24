using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Performance.Buffers;

using static GC;

// internal but used by generator code

public static class ReusableLinkedArrayBufferWriterPool
{
    static readonly ConcurrentQueue<ReusableLinkedArrayBufferWriter> queue = new ConcurrentQueue<ReusableLinkedArrayBufferWriter>();

    public static ReusableLinkedArrayBufferWriter Rent()
    {
        if (queue.TryDequeue(out var writer))
        {
            return writer;
        }
        return new ReusableLinkedArrayBufferWriter(useFirstBuffer: false, pinned: false); // does not cache firstBuffer
    }

    public static void Return(ReusableLinkedArrayBufferWriter writer)
    {
        writer.Reset();
        queue.Enqueue(writer);
    }
}

// This class has large buffer so should cache [ThreadStatic] or Pool.
public sealed class ReusableLinkedArrayBufferWriter : IBufferWriter<byte>
{
    const int DefaultSizeHint = 8;
    const int InitialBufferSize = 262144; // 256K(32768, 65536, 131072, 262144)
    static readonly byte[] noUseFirstBufferSentinel = new byte[0];

    List<BufferSegment> buffers; // add freezed buffer.

    byte[] firstBuffer; // cache firstBuffer to avoid call ArrayPoo.Rent/Return
    int firstBufferWritten;

    BufferSegment current;
    int nextBufferSize;
    int available;

    int totalWritten;

    public int TotalWritten => totalWritten;
    public int WrittenCount => totalWritten;
    public ReadOnlyMemory<byte> WrittenMemory => TryGetWrittenMemory(out var memory) ? memory : ToArray();
    public ReadOnlySpan<byte> WrittenSpan => WrittenMemory.Span;
    bool UseFirstBuffer => firstBuffer != noUseFirstBufferSentinel;

    public ReusableLinkedArrayBufferWriter(bool useFirstBuffer, bool pinned)
    {
        this.buffers = new List<BufferSegment>();
        this.firstBuffer = useFirstBuffer
            ? AllocateUninitializedArray<byte>(InitialBufferSize, pinned)
            : noUseFirstBufferSentinel;
        this.firstBufferWritten = 0;
        this.current = default;
        this.nextBufferSize = InitialBufferSize;
        this.available = 0;
        this.totalWritten = 0;
    }

    public byte[] DangerousGetFirstBuffer() => firstBuffer;

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        PrepareBuffer(sizeHint, out _, out var memory);
        return memory;
    }

    public Span<byte> GetSpan(int sizeHint = 0)
    {
        PrepareBuffer(sizeHint, out var span, out _);
        return span;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void PrepareBuffer(int sizeHint, out Span<byte> span, out Memory<byte> memory)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sizeHint);
        if (sizeHint == 0) sizeHint = DefaultSizeHint;

        if (current.IsNull)
        {
            // use firstBuffer
            var free = firstBuffer.Length - firstBufferWritten;
            if (free != 0 && sizeHint <= free)
            {
                available = free;
                span = firstBuffer.AsSpan(firstBufferWritten);
                memory = firstBuffer.AsMemory(firstBufferWritten);
                return;
            }
        }
        else
        {
            var buffer = current.FreeBuffer;
            if (buffer.Length >= sizeHint)
            {
                available = buffer.Length;
                span = buffer;
                memory = current.FreeMemory;
                return;
            }
        }

        BufferSegment next;
        if (sizeHint <= nextBufferSize)
        {
            next = new BufferSegment(nextBufferSize);
            nextBufferSize = MathEx.NewArrayCapacity(nextBufferSize);
        }
        else
        {
            next = new BufferSegment(sizeHint);
        }

        if (current.WrittenCount != 0)
        {
            buffers.Add(current);
        }
        current = next;
        available = next.FreeCount;
        span = next.FreeBuffer;
        memory = next.FreeMemory;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (count > available)
        {
            throw new InvalidOperationException("Cannot advance past the end of the reserved buffer segment.");
        }

        if (current.IsNull)
        {
            firstBufferWritten += count;
        }
        else
        {
            current.Advance(count);
        }
        totalWritten += count;
        available = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(ReadOnlySpan<byte> source)
    {
        available = 0;
        while (!source.IsEmpty)
        {
            var destination = GetSpan(source.Length);
            var copyLength = Math.Min(destination.Length, source.Length);
            source[..copyLength].CopyTo(destination);
            Advance(copyLength);
            source = source[copyLength..];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(ReadOnlyMemory<byte> source) => Write(source.Span);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(byte[] source)
    {
        ArgumentNullException.ThrowIfNull(source);
        Write(source.AsSpan());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteByte(byte value)
    {
        var destination = GetSpan(1);
        destination[0] = value;
        Advance(1);
    }

    public bool TryGetWrittenMemory(out ReadOnlyMemory<byte> memory)
    {
        if (totalWritten == 0)
        {
            memory = ReadOnlyMemory<byte>.Empty;
            return true;
        }

        if (UseFirstBuffer)
        {
            if (firstBufferWritten > 0 && buffers.Count == 0 && current.IsNull)
            {
                memory = firstBuffer.AsMemory(0, firstBufferWritten);
                return true;
            }

            memory = default;
            return false;
        }

        if (buffers.Count == 0 && !current.IsNull)
        {
            memory = current.WrittenMemory;
            return true;
        }

        memory = default;
        return false;
    }

    public byte[] ToArray()
    {
        if (totalWritten == 0) return Array.Empty<byte>();

        var result = AllocateUninitializedArray<byte>(totalWritten);
        var dest = result.AsSpan();

        if (UseFirstBuffer)
        {
            firstBuffer.AsSpan(0, firstBufferWritten).CopyTo(dest);
            dest = dest[firstBufferWritten..];
        }

        if (buffers.Count > 0)
        {
#if NET7_0_OR_GREATER
            foreach (ref var item in CollectionsMarshal.AsSpan(buffers))
#else
            foreach (var item in buffers)
#endif
            {
                item.WrittenBuffer.CopyTo(dest);
                dest = dest[item.WrittenCount..];
            }
        }

        if (!current.IsNull)
        {
            current.WrittenBuffer.CopyTo(dest);
        }

        return result;
    }

    public byte[] ToArrayAndReset()
    {
        if (totalWritten == 0)
        {
            Reset();
            return Array.Empty<byte>();
        }

        var result = AllocateUninitializedArray<byte>(totalWritten);
        var dest = result.AsSpan();

        if (UseFirstBuffer)
        {
            firstBuffer.AsSpan(0, firstBufferWritten).CopyTo(dest);
            dest = dest.Slice(firstBufferWritten);
        }

        if (buffers.Count > 0)
        {
#if NET7_0_OR_GREATER
            foreach (ref var item in CollectionsMarshal.AsSpan(buffers))
#else
            foreach (var item in buffers)
#endif
            {
                item.WrittenBuffer.CopyTo(dest);
                dest = dest.Slice(item.WrittenCount);
                item.Clear(); // reset buffer-segment in this loop to avoid iterate twice for Reset
            }
        }

        if (!current.IsNull)
        {
            current.WrittenBuffer.CopyTo(dest);
            current.Clear();
        }

        ResetCore();
        return result;
    }

//     public void WriteToAndReset<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer)
// #if NET7_0_OR_GREATER
//         where TBufferWriter : IBufferWriter<byte>
// #else
//         where TBufferWriter : class, IBufferWriter<byte>
// #endif
//     {
//         if (totalWritten == 0) return;

//         if (UseFirstBuffer)
//         {
//             ref var spanRef = ref writer.GetSpanReference(firstBufferWritten);
//             firstBuffer.AsSpan(0, firstBufferWritten).CopyTo(MemoryMarshal.CreateSpan(ref spanRef, firstBufferWritten));
//             writer.Advance(firstBufferWritten);
//         }

//         if (buffers.Count > 0)
//         {
// #if NET7_0_OR_GREATER
//             foreach (ref var item in CollectionsMarshal.AsSpan(buffers))
// #else
//             foreach (var item in buffers)
// #endif
//             {
//                 ref var spanRef = ref writer.GetSpanReference(item.WrittenCount);
//                 item.WrittenBuffer.CopyTo(MemoryMarshal.CreateSpan(ref spanRef, item.WrittenCount));
//                 writer.Advance(item.WrittenCount);
//                 item.Clear(); // reset
//             }
//         }

//         if (!current.IsNull)
//         {
//             ref var spanRef = ref writer.GetSpanReference(current.WrittenCount);
//             current.WrittenBuffer.CopyTo(MemoryMarshal.CreateSpan(ref spanRef, current.WrittenCount));
//             writer.Advance(current.WrittenCount);
//             current.Clear();
//         }

//         ResetCore();
//     }

    public async ValueTask WriteToAndResetAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (totalWritten == 0)
        {
            Reset();
            return;
        }

        if (UseFirstBuffer)
        {
            await stream.WriteAsync(firstBuffer.AsMemory(0, firstBufferWritten), cancellationToken).ConfigureAwait(false);
        }

        if (buffers.Count > 0)
        {
            foreach (var item in buffers)
            {
                await stream.WriteAsync(item.WrittenMemory, cancellationToken).ConfigureAwait(false);
                item.Clear(); // reset
            }
        }

        if (!current.IsNull)
        {
            await stream.WriteAsync(current.WrittenMemory, cancellationToken).ConfigureAwait(false);
            current.Clear();
        }

        ResetCore();
    }

    public Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    // reset without list's BufferSegment element
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void ResetCore()
    {
        firstBufferWritten = 0;
        buffers.Clear();
        totalWritten = 0;
        current = default;
        nextBufferSize = InitialBufferSize;
        available = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        if (totalWritten == 0 && buffers.Count == 0 && current.IsNull)
        {
            available = 0;
            return;
        }
#if NET7_0_OR_GREATER
        foreach (ref var item in CollectionsMarshal.AsSpan(buffers))
#else
        foreach (var item in buffers)
#endif
        {
            item.Clear();
        }
        current.Clear();
        ResetCore();
    }

    public struct Enumerator : IEnumerator<Memory<byte>>
    {
        ReusableLinkedArrayBufferWriter parent;
        State state;
        Memory<byte> current;
        List<BufferSegment>.Enumerator buffersEnumerator;

        public Enumerator(ReusableLinkedArrayBufferWriter parent)
        {
            this.parent = parent;
            this.state = default;
            this.current = default;
            this.buffersEnumerator = default;
        }

        public Memory<byte> Current => current;

        object IEnumerator.Current => throw new NotSupportedException();

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            if (state == State.FirstBuffer)
            {
                state = State.BuffersInit;

                if (parent.UseFirstBuffer && parent.firstBufferWritten > 0)
                {
                    current = parent.firstBuffer.AsMemory(0, parent.firstBufferWritten);
                    return true;
                }
            }

            if (state == State.BuffersInit)
            {
                state = State.BuffersIterate;

                buffersEnumerator = parent.buffers.GetEnumerator();
            }

            if (state == State.BuffersIterate)
            {
                if (buffersEnumerator.MoveNext())
                {
                    current = buffersEnumerator.Current.WrittenMemory;
                    return true;
                }

                buffersEnumerator.Dispose();
                state = State.Current;
            }

            if (state == State.Current)
            {
                if (parent.current.IsNull || parent.current.WrittenCount == 0)
                {
                    state = State.End;
                    return false;
                }

                state = State.End;
                current = parent.current.WrittenMemory;
                return true;
            }

            return false;
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }

        enum State
        {
            FirstBuffer,
            BuffersInit,
            BuffersIterate,
            Current,
            End
        }
    }
}

internal struct BufferSegment
{
    byte[] buffer;
    int written;

    public bool IsNull => buffer == null;

    public int WrittenCount => written;
    public int FreeCount => buffer.Length - written;
    public Span<byte> WrittenBuffer => buffer.AsSpan(0, written);
    public Memory<byte> WrittenMemory => buffer.AsMemory(0, written);
    public Span<byte> FreeBuffer => buffer.AsSpan(written);
    public Memory<byte> FreeMemory => buffer.AsMemory(written);

    public BufferSegment(int size)
    {
        buffer = ArrayPool<byte>.Shared.Rent(size);
        written = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int count)
    {
        written += count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        if (buffer != null)
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
        buffer = null!;
        written = 0;
    }
}

internal static class MathEx
{
    const int ArrayMexLength = 0x7FFFFFC7;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int NewArrayCapacity(int size)
    {
        var newSize = unchecked(size * 2);
        if ((uint)newSize > ArrayMexLength)
        {
            newSize = ArrayMexLength;
        }
        return newSize;
    }
}
