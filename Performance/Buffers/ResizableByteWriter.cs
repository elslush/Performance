using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Performance.Buffers;

public sealed class ResizableByteWriter : Stream, IBufferWriter<byte>, IMemoryOwner<byte>
{
    private byte[]? _array;
    private readonly ArrayPool<byte> _pool;
    private int _index;
    private readonly bool _disposed;

    public ResizableByteWriter() : this(ArrayPool<byte>.Shared) { }

    public ResizableByteWriter(int initialCapacity = 0) : this(ArrayPool<byte>.Shared, initialCapacity) { }

    public ResizableByteWriter(ArrayPool<byte> pool, int initialCapacity = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(initialCapacity);
        _pool = pool;
        _index = 0;
        _disposed = false;
        _array = initialCapacity == 0 ? [] : pool.Rent(initialCapacity);
    }

    public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));
    public override long Length => _index;
    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }
    public override void Flush() { }
    public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();

    public void Reset() => _index = 0;

    public ReadOnlyMemory<byte> WrittenMemory
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ThrowIfDisposed();
            return new ReadOnlyMemory<byte>(_array, 0, _index);
        }
    }

    public ReadOnlySpan<byte> WrittenSpan
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ThrowIfDisposed();
            return new Span<byte>(_array, 0, _index);
        }
    }

    Memory<byte> IMemoryOwner<byte>.Memory
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ThrowIfDisposed();
            return new Memory<byte>(_array);
        }
    }

    public void Advance(int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(null, nameof(count));
        if (_array is null || count > _array.Length)
            throw new InvalidOperationException("Attempt to advance beyond the length of the buffer.");
        _index += count;
    }

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        if (sizeHint == 0) sizeHint = 8;
        Grow(sizeHint);
        return new Memory<byte>(_array, _index, sizeHint);
    }

    public Span<byte> GetSpan(int sizeHint = 0)
    {
        if (sizeHint == 0) sizeHint = 8;
        Grow(sizeHint);
        return new Span<byte>(_array, _index, sizeHint);
    }

    public new void Write(ReadOnlySpan<byte> items) => Copy(items);
    public void Write(ReadOnlyMemory<byte> items) => Copy(items.Span);
    public void Write(byte[] items) => Copy(items);
    public void Write(byte item)
    {
        Grow(1);
        _array![_index] = item;
        _index += 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Copy(ReadOnlySpan<byte> items)
    {
        Grow(items.Length);
        var dst = new Span<byte>(_array, _index, items.Length);
        items.CopyTo(dst);
        _index += items.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Grow(int size)
    {
        ThrowIfDisposed();
        if (!GrowIfRequired(size, out var length)) return;
        var next = _pool.Rent(length);
        var dst = new Span<byte>(next, 0, _index);
        var src = new Span<byte>(_array, 0, _index);
        src.CopyTo(dst);
        if (_array is not null)
            _pool.Return(_array, RuntimeHelpers.IsReferenceOrContainsReferences<byte>());
        _array = next;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool GrowIfRequired(int size, out int length)
    {
        length = default;
        var newIndex = checked(_index + size);
        if (_array?.Length - newIndex >= 0) return false;
        length = RoundUpPow2Ceiling(newIndex);
        return true;
    }

    public new void Dispose()
    {
        if (_disposed) return;
        if (_array != null)
        {
            _pool.Return(_array, RuntimeHelpers.IsReferenceOrContainsReferences<byte>());
            _array = null;
        }
        GC.SuppressFinalize(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int RoundUpPow2Ceiling(int x)
    {
        checked
        {
            --x;
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            ++x;
        }
        return x;
    }

    [StackTraceHidden]
    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
