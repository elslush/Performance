using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Performance.Buffers;

public sealed class ResizableSpanWriter<T> : IBufferWriter<T>, IMemoryOwner<T>
{
    /// <summary>
    /// Array on current rental from the array pool.  Reference to the same memory as <see cref="_buffer"/>.
    /// </summary>
    private T[]? _array;

    /// <summary>
    /// The <see cref="ArrayPool{T}"/> instance used to rent <see cref="array"/>.
    /// </summary>
    private readonly ArrayPool<T> _pool;

    /// <summary>
    /// The current position of the writer.
    /// </summary>
    private int _index;

    /// <summary>
    /// The disposed state of the buffer.
    /// </summary>
    private readonly bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryWriterBuffer{T}"/> class.
    /// </summary>
    public ResizableSpanWriter()
        : this(ArrayPool<T>.Shared)
    {
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryWriterBuffer{T}"/> class.
    /// </summary>
    /// <param name="initialCapacity">The incremental size to grow the buffer.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="initialCapacity"/> is not valid.</exception>
    public ResizableSpanWriter(int initialCapacity = 0)
        : this(ArrayPool<T>.Shared, initialCapacity)
    {
    }

    //public int Length => _index;

    public void Reset() => _index = 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryWriterBuffer{T}"/> class.
    /// </summary>
    /// <param name="pool">The <see cref="ArrayPool{T}"/> instance to use.</param>
    /// <param name="initialCapacity">The minimum capacity with which to initialize the underlying buffer.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="initialCapacity"/> is not valid.</exception>
    public ResizableSpanWriter(ArrayPool<T> pool, int initialCapacity = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(initialCapacity);

        _pool = pool;

        _index = 0;

        _disposed = false;

        _array = initialCapacity == 0 ? [] : pool.Rent(initialCapacity);
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
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than or equal to 0.");

        if (_array is null || count > _array.Length)
            throw new InvalidOperationException("Attempt to advance beyond the length of the buffer.");

        _index += count;
    }

    /// <inheritdoc />
    public Memory<T> GetMemory(int sizeHint = 0)
    {
        if (sizeHint == 0) sizeHint = 8;

        Grow(sizeHint);

        return new Memory<T>(_array, _index, sizeHint);
    }

    /// <inheritdoc />
    public Span<T> GetSpan(int sizeHint = 0)
    {
        if (sizeHint == 0) sizeHint = 8;

        Grow(sizeHint);

        return new Span<T>(_array, _index, sizeHint);
    }

    /// <summary>
    /// Appends to the end of the buffer, advances the buffer and automatically growing the buffer if necessary.
    /// </summary>
    /// <param name="items">A <see cref="Span{T}"/> of items to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(ReadOnlySpan<T> items)
        => Copy(items);

    /// <summary>
    /// Appends to the end of the buffer, advances the buffer and automatically growing the buffer if necessary.
    /// </summary>
    /// <param name="items">A <see cref="Memory{T}"/> of items to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(ReadOnlyMemory<T> items)
        => Copy(items.Span);

    /// <summary>
    /// Appends to the end of the buffer, advances the buffer and automatically growing the buffer if necessary.
    /// </summary>
    /// <param name="items">A <see cref="Memory{T}"/> of items to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(T[] items)
        => Copy(items);

    /// <summary>
    /// Appends a single item to the end of the buffer, automatically growing the buffer if necessary.
    /// </summary>
    /// <param name="item"> Item <see cref="T"/> to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(T item)
    {
        Grow(1);

        _array![_index] = item;

        _index += 1;
    }

    /// <summary>
    /// Copies to the underlying buffer
    /// </summary>
    /// <param name="items">Items to add.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Copy(ReadOnlySpan<T> items)
    {
        Grow(items.Length);

        var dst = new Span<T>(_array, _index, items.Length);

        items.CopyTo(dst);

        _index += items.Length;
    }

    /// <summary>
    /// Grows the buffer if needed.
    /// </summary>
    /// <param name="length"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Grow(int size)
    {
        ThrowIfDisposed();

        if (!GrowIfRequired(size, out var length)) return;

        var next = _pool.Rent(length);

        var dst = new Span<T>(next, 0, _index);

        var src = new Span<T>(_array, 0, _index);

        src.CopyTo(dst);

        _pool.Return(_array!, RuntimeHelpers.IsReferenceOrContainsReferences<T>());

        _array = next;
    }

    /// <summary>
    /// Gets the length to growth the buffer
    /// </summary>
    /// <param name="length"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool GrowIfRequired(int size, out int length)
    {
        length = default;

        var newIndex = checked(_index + size);

        if (_array!.Length - newIndex >= 0) return false;

        length = RoundUpPow2Ceiling(newIndex);

        return true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;

        if (_array != null)
        {
            _pool.Return(_array, RuntimeHelpers.IsReferenceOrContainsReferences<T>());

            _array = null;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int RoundUpPow2Ceiling(int x)
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
    //[DoesNotReturn]
    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, "The buffer has been disposed.");
    }
}
