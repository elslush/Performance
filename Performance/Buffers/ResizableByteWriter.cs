using System.Buffers;
using System.Runtime.CompilerServices;

namespace Performance.Buffers;

/// <summary>
/// A Resizeable Byte Buffer
/// </summary>
public sealed class ResizableByteWriter : Stream, IBufferWriter<byte>, IMemoryOwner<byte>
{
    private byte[]? _array;
    private readonly ArrayPool<byte> _pool;
    private int _index;
    private volatile bool _disposed;

    public ResizableByteWriter(int initialCapacity = 0)
        : this(ArrayPool<byte>.Shared, initialCapacity) { }

    public ResizableByteWriter(ArrayPool<byte> pool, int initialCapacity = 0)
    {
        ArgumentNullException.ThrowIfNull(pool);
        ArgumentOutOfRangeException.ThrowIfNegative(initialCapacity);
        _pool = pool;
        _index = 0;
        _disposed = false;
        _array = initialCapacity == 0 ? [] : pool.Rent(initialCapacity);
    }

    // -----------------------------------------------------------------
    // Stream overrides
    // -----------------------------------------------------------------
    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => _index;
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

    public override void Write(byte[] buffer, int offset, int count) =>
        Write(buffer.AsSpan(offset, count));

    // -----------------------------------------------------------------
    // Public helpers
    // -----------------------------------------------------------------
    public void Reset() => _index = 0;

    public ReadOnlyMemory<byte> WrittenMemory
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ThrowIfDisposed();
            return new ReadOnlyMemory<byte>(_array!, 0, _index);
        }
    }

    public ReadOnlySpan<byte> WrittenSpan
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ThrowIfDisposed();
            return new ReadOnlySpan<byte>(_array!, 0, _index);
        }
    }

    // -----------------------------------------------------------------
    // IBufferWriter<byte>
    // -----------------------------------------------------------------
    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        EnsureCapacity(sizeHint);
        return new Memory<byte>(_array!, _index, sizeHint);
    }

    public Span<byte> GetSpan(int sizeHint = 0)
    {
        EnsureCapacity(sizeHint);
        return new Span<byte>(_array!, _index, sizeHint);
    }

    public void Advance(int count)
    {
        if (count < 0) ThrowHelper.ThrowArgumentOutOfRangeException(nameof(count));
        if (_array is null || count > _array.Length - _index)
            ThrowHelper.ThrowInvalidOperationException("Attempt to advance beyond the length of the buffer.");

        _index += count;
    }

    // -----------------------------------------------------------------
    // Write overloads – hot path
    // -----------------------------------------------------------------
    public void Write(byte value)
    {
        EnsureCapacity(1);
        _array![_index++] = value;
    }

    public override void Write(ReadOnlySpan<byte> source)
    {
        EnsureCapacity(source.Length);
        source.CopyTo(new Span<byte>(_array!, _index, source.Length));
        _index += source.Length;
    }

    public void Write(ReadOnlyMemory<byte> source) => Write(source.Span);

    public void Write(byte[] source) => Write(source.AsSpan());

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        Write(buffer.Span);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    // -----------------------------------------------------------------
    // IMemoryOwner<byte>
    // -----------------------------------------------------------------
    Memory<byte> IMemoryOwner<byte>.Memory
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ThrowIfDisposed();
            return new Memory<byte>(_array!);
        }
    }

    public new void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_array != null && _array.Length != 0)
        {
            _pool.Return(_array, RuntimeHelpers.IsReferenceOrContainsReferences<byte>());
            _array = null;
        }

        GC.SuppressFinalize(this);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureCapacity(int additional)
    {
        ThrowIfDisposed();

        if (_array!.Length - _index >= additional) return;

        int required = checked(_index + additional);
        int newCapacity = Math.Max(_array.Length * 2, required);
        if (newCapacity < 8) newCapacity = 8; // guard against zero‑length growth

        byte[] newArray = _pool.Rent(newCapacity);
        new Span<byte>(_array, 0, _index).CopyTo(newArray);

        if (_array.Length != 0)
            _pool.Return(_array, RuntimeHelpers.IsReferenceOrContainsReferences<byte>());

        _array = newArray;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
        if (_disposed) ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private static class ThrowHelper
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowArgumentOutOfRangeException(string paramName) =>
            throw new ArgumentOutOfRangeException(paramName);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalidOperationException(string message) =>
            throw new InvalidOperationException(message);
    }
}