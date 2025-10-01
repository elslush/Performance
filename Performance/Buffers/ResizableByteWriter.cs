using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Performance.Buffers;

/// <summary>
/// A high-performance, pooled, resizable buffer writer for byte sequences.
/// Implements <see cref="Stream"/>, <see cref="IBufferWriter{T}"/>, and <see cref="IMemoryOwner{T}"/>
/// to provide a flexible and efficient way to build byte arrays dynamically.
/// </summary>
public sealed class ResizableByteWriter : Stream, IBufferWriter<byte>, IMemoryOwner<byte>
{
    /// <summary>
    /// The underlying byte array rented from the pool. Can be null if disposed.
    /// </summary>
    private byte[]? _array;

    /// <summary>
    /// The <see cref="ArrayPool{T}"/> instance used to rent and return the buffer.
    /// </summary>
    private readonly ArrayPool<byte> _pool;

    /// <summary>
    /// The number of bytes written to the buffer; the current position of the writer.
    /// </summary>
    private int _index;

    /// <summary>
    /// The number of bytes reserved by the last call to GetSpan or GetMemory.
    /// This is used to validate the count passed to Advance.
    /// </summary>
    private int _available;

    /// <summary>
    /// Tracks the disposed state of the writer to prevent use-after-free errors.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResizableByteWriter"/> class using the shared <see cref="ArrayPool{T}"/>.
    /// </summary>
    public ResizableByteWriter() : this(ArrayPool<byte>.Shared) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResizableByteWriter"/> class with a specified initial capacity.
    /// </summary>
    /// <param name="initialCapacity">The minimum initial capacity of the buffer.</param>
    public ResizableByteWriter(int initialCapacity = 0) : this(ArrayPool<byte>.Shared, initialCapacity) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResizableByteWriter"/> class with a specific array pool and initial capacity.
    /// </summary>
    /// <param name="pool">The array pool to use for buffer management.</param>
    /// <param name="initialCapacity">The minimum initial capacity of the buffer.</param>
    public ResizableByteWriter(ArrayPool<byte> pool, int initialCapacity = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(initialCapacity);
        _pool = pool;
        _index = 0;
        _available = 0;
        _disposed = false;
        _array = initialCapacity == 0 ? [] : pool.Rent(initialCapacity);
    }

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));

    /// <inheritdoc />
    public override long Length => _index;

    /// <inheritdoc />
    public override bool CanRead => false;

    /// <inheritdoc />
    public override bool CanSeek => false;

    /// <inheritdoc />
    public override bool CanWrite => true;

    /// <inheritdoc />
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    /// <inheritdoc />
    public override void Flush() { }

    /// <inheritdoc />
    public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    /// <inheritdoc />
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <summary>
    /// Resets the writer to the beginning of the buffer, allowing it to be reused.
    /// The underlying allocated buffer is retained for performance.
    /// </summary>
    public void Reset()
    {
        ThrowIfDisposed();
        _index = 0;
        _available = 0;
    }

    /// <summary>
    /// Gets the data written to the buffer so far as a <see cref="ReadOnlyMemory{T}"/>.
    /// </summary>
    public ReadOnlyMemory<byte> WrittenMemory
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ThrowIfDisposed();
            return new ReadOnlyMemory<byte>(_array, 0, _index);
        }
    }

    /// <summary>
    /// Gets the data written to the buffer so far as a <see cref="ReadOnlySpan{T}"/>.
    /// </summary>
    public ReadOnlySpan<byte> WrittenSpan
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ThrowIfDisposed();
            return new Span<byte>(_array, 0, _index);
        }
    }

    /// <summary>
    /// Gets the entire underlying memory buffer, including unused space.
    /// </summary>
    Memory<byte> IMemoryOwner<byte>.Memory
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ThrowIfDisposed();
            return new Memory<byte>(_array);
        }
    }

    /// <inheritdoc />
    public void Advance(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ThrowIfDisposed();

        if (count > _available)
            throw new InvalidOperationException("Cannot advance past the end of the reserved buffer segment.");

        _index += count;
        _available = 0; // The reservation is consumed after advancing.
    }

    /// <inheritdoc />
    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        if (sizeHint == 0) sizeHint = 8;
        ThrowIfDisposed();
        Grow(sizeHint);

        _available = sizeHint;
        return new Memory<byte>(_array, _index, sizeHint);
    }

    /// <inheritdoc />
    public Span<byte> GetSpan(int sizeHint = 0)
    {
        if (sizeHint == 0) sizeHint = 8;
        ThrowIfDisposed();
        Grow(sizeHint);

        _available = sizeHint;
        return new Span<byte>(_array, _index, sizeHint);
    }

    /// <summary>
    /// Writes a span of bytes to the buffer.
    /// </summary>
    /// <param name="items">The data to write.</param>
    public new void Write(ReadOnlySpan<byte> items) => Copy(items);

    /// <summary>
    /// Writes a memory block of bytes to the buffer.
    /// </summary>
    /// <param name="items">The data to write.</param>
    public void Write(ReadOnlyMemory<byte> items) => Copy(items.Span);

    /// <summary>
    /// Writes an array of bytes to the buffer.
    /// </summary>
    /// <param name="items">The data to write.</param>
    public void Write(byte[] items) => Copy(items);

    /// <summary>
    /// Writes a single byte to the buffer.
    /// </summary>
    /// <param name="value">The byte to write.</param>
    public override void WriteByte(byte value)
    {
        _available = 0; // Direct writes invalidate any outstanding reservation.
        Grow(1);
        _array![_index] = value;
        _index += 1;
    }

    /// <summary>
    /// Internal helper to copy data and advance the index.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Copy(ReadOnlySpan<byte> items)
    {
        if (items.IsEmpty) return;
        _available = 0; // Direct writes invalidate any outstanding reservation.
        Grow(items.Length);
        items.CopyTo(new Span<byte>(_array, _index, items.Length));
        _index += items.Length;
    }

    /// <summary>
    /// Ensures the buffer has enough space for an upcoming write operation.
    /// If not, it rents a larger buffer and copies the existing data.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Grow(int size)
    {
        ThrowIfDisposed();
        if (!GrowIfRequired(size, out var newSize)) return;

        var newBuffer = _pool.Rent(newSize);
        if (_index > 0)
        {
            var source = new ReadOnlySpan<byte>(_array, 0, _index);
            source.CopyTo(newBuffer);
        }

        if (_array is not null && _array.Length > 0)
        {
            _pool.Return(_array);
        }

        _array = newBuffer;
    }

    /// <summary>
    /// Determines if the buffer needs to grow and calculates the required new size.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool GrowIfRequired(int size, out int length)
    {
        length = default;
        var newIndex = checked((long)_index + size);

        if (_array is not null && newIndex <= _array.Length)
        {
            return false;
        }

        length = RoundUpPow2Ceiling((int)newIndex);
        return true;
    }

    /// <summary>
    /// Calculates the next power of two greater than or equal to the input value.
    /// This is an efficient bit-twiddling algorithm for resizing buffers.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int RoundUpPow2Ceiling(int x)
    {
        checked
        {
            // Ensure x is not zero, as the algorithm doesn't handle it.
            if (x == 0) return 8; // Return a default small power of two for 0.
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

    /// <inheritdoc />
    public new void Dispose()
    {
        if (_disposed) return;

        _disposed = true;

        if (_array != null && _array.Length > 0)
        {
            _pool.Return(_array);
        }
        _array = null;
    }

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException"/> if the writer has been disposed.
    /// </summary>
    [StackTraceHidden]
    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}

