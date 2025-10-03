using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Performance.Buffers;

/// <summary>
/// A high-performance, pooled, resizable buffer writer for generic types.
/// Implements <see cref="IBufferWriter{T}"/> and <see cref="IMemoryOwner{T}"/>
/// to provide a flexible and efficient way to build arrays dynamically.
/// </summary>
public sealed class ResizableSpanWriter<T> : IBufferWriter<T>, IMemoryOwner<T>
{
    /// <summary>
    /// The underlying array rented from the pool. Can be null if disposed.
    /// </summary>
    private T[]? _array;

    /// <summary>
    /// The <see cref="ArrayPool{T}"/> instance used to rent and return the buffer.
    /// </summary>
    private readonly ArrayPool<T> _pool;

    /// <summary>
    /// The number of items written to the buffer; the current position of the writer.
    /// </summary>
    private int _index;

    /// <summary>
    /// The number of items reserved by the last call to GetSpan or GetMemory.
    /// This is used to validate the count passed to Advance.
    /// </summary>
    private int _available;

    /// <summary>
    /// Tracks the disposed state of the writer to prevent use-after-free errors.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResizableSpanWriter{T}"/> class.
    /// </summary>
    public ResizableSpanWriter()
        : this(ArrayPool<T>.Shared)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResizableSpanWriter{T}"/> class.
    /// </summary>
    /// <param name="initialCapacity">The incremental size to grow the buffer.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="initialCapacity"/> is not valid.</exception>
    public ResizableSpanWriter(int initialCapacity = 0)
        : this(ArrayPool<T>.Shared, initialCapacity)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResizableSpanWriter{T}"/> class.
    /// </summary>
    /// <param name="pool">The <see cref="ArrayPool{T}"/> instance to use.</param>
    /// <param name="initialCapacity">The minimum capacity with which to initialize the underlying buffer.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="initialCapacity"/> is not valid.</exception>
    public ResizableSpanWriter(ArrayPool<T> pool, int initialCapacity = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(initialCapacity);

        _pool = pool;
        _index = 0;
        _available = 0;
        _disposed = false;
        _array = initialCapacity == 0 ? [] : pool.Rent(initialCapacity);
    }

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
    /// Gets the data written to the underlying buffer so far as a <see cref="ReadOnlyMemory{T}"/>.
    /// </summary>
    public ReadOnlyMemory<T> WrittenMemory
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ThrowIfDisposed();
            return new ReadOnlyMemory<T>(_array, 0, _index);
        }
    }

    /// <summary>
    /// Gets the data written to the underlying buffer so far as a <see cref="ReadOnlySpan{T}"/>.
    /// </summary>
    public ReadOnlySpan<T> WrittenSpan
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ThrowIfDisposed();
            return new Span<T>(_array, 0, _index);
        }
    }

    /// <inheritdoc />
    Memory<T> IMemoryOwner<T>.Memory
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ThrowIfDisposed();
            return new Memory<T>(_array);
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
    public Memory<T> GetMemory(int sizeHint = 0)
    {
        if (sizeHint == 0) sizeHint = 8;
        ThrowIfDisposed();
        Grow(sizeHint);

        _available = sizeHint;
        return new Memory<T>(_array, _index, sizeHint);
    }

    /// <inheritdoc />
    public Span<T> GetSpan(int sizeHint = 0)
    {
        if (sizeHint == 0) sizeHint = 8;
        ThrowIfDisposed();
        Grow(sizeHint);

        _available = sizeHint;
        return new Span<T>(_array, _index, sizeHint);
    }

    /// <summary>
    /// Appends a span of items to the end of the buffer, automatically growing the buffer if necessary.
    /// </summary>
    /// <param name="items">A <see cref="ReadOnlySpan{T}"/> of items to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(ReadOnlySpan<T> items) => Copy(items);

    /// <summary>
    /// Appends a memory block of items to the end of the buffer, automatically growing the buffer if necessary.
    /// </summary>
    /// <param name="items">A <see cref="ReadOnlyMemory{T}"/> of items to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(ReadOnlyMemory<T> items) => Copy(items.Span);

    /// <summary>
    /// Appends an array of items to the end of the buffer, automatically growing the buffer if necessary.
    /// </summary>
    /// <param name="items">An array of items to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(T[] items) => Copy(items);

    /// <summary>
    /// Appends a single item to the end of the buffer, automatically growing the buffer if necessary.
    /// </summary>
    /// <param name="item">Item to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(T item)
    {
        _available = 0; // Direct writes invalidate any outstanding reservation.
        Grow(1);
        _array![_index] = item;
        _index += 1;
    }

    /// <summary>
    /// Internal helper to copy data to the underlying buffer and advance the index.
    /// </summary>
    /// <param name="items">Items to add.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Copy(ReadOnlySpan<T> items)
    {
        if (items.IsEmpty) return;
        _available = 0; // Direct writes invalidate any outstanding reservation.
        Grow(items.Length);
        items.CopyTo(new Span<T>(_array, _index, items.Length));
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
            var source = new ReadOnlySpan<T>(_array, 0, _index);
            source.CopyTo(newBuffer);
        }

        if (_array is not null && _array.Length > 0)
        {
            _pool.Return(_array, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
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
            if (x == 0) return 8;
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
    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;

        if (_array != null && _array.Length > 0)
        {
            _pool.Return(_array, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
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

