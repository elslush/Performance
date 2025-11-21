using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Performance.Benchmarks.Original;

public sealed class OriginalResizableSpanWriter<T> : IBufferWriter<T>, IMemoryOwner<T>
{
    private T[]? _array;
    private readonly ArrayPool<T> _pool;
    private int _index;
    private readonly bool _disposed;

    public OriginalResizableSpanWriter()
        : this(ArrayPool<T>.Shared)
    {
    }

    public OriginalResizableSpanWriter(int initialCapacity = 0)
        : this(ArrayPool<T>.Shared, initialCapacity)
    {
    }

    public void Reset() => _index = 0;

    public OriginalResizableSpanWriter(ArrayPool<T> pool, int initialCapacity = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(initialCapacity);

        _pool = pool;
        _index = 0;
        _disposed = false;
        _array = initialCapacity == 0 ? [] : pool.Rent(initialCapacity);
    }

    public ReadOnlyMemory<T> WrittenMemory
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ThrowIfDisposed();
            return new ReadOnlyMemory<T>(_array, 0, _index);
        }
    }

    public ReadOnlySpan<T> WrittenSpan
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ThrowIfDisposed();
            return new Span<T>(_array, 0, _index);
        }
    }

    Memory<T> IMemoryOwner<T>.Memory
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ThrowIfDisposed();
            return new Memory<T>(_array);
        }
    }

    public void Advance(int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than or equal to 0.");

        if (_array is null || count > _array.Length)
            throw new InvalidOperationException("Attempt to advance beyond the length of the buffer.");

        _index += count;
    }

    public Memory<T> GetMemory(int sizeHint = 0)
    {
        if (sizeHint == 0) sizeHint = 8;
        Grow(sizeHint);
        return new Memory<T>(_array, _index, sizeHint);
    }

    public Span<T> GetSpan(int sizeHint = 0)
    {
        if (sizeHint == 0) sizeHint = 8;
        Grow(sizeHint);
        return new Span<T>(_array, _index, sizeHint);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(ReadOnlySpan<T> items) => Copy(items);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(ReadOnlyMemory<T> items) => Copy(items.Span);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(T[] items) => Copy(items);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(T item)
    {
        Grow(1);
        _array![_index] = item;
        _index += 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Copy(ReadOnlySpan<T> items)
    {
        Grow(items.Length);
        items.CopyTo(new Span<T>(_array, _index, items.Length));
        _index += items.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Grow(int size)
    {
        ThrowIfDisposed();
        if (!GrowIfRequired(size, out var length)) return;

        var next = _pool.Rent(length);
        new ReadOnlySpan<T>(_array, 0, _index).CopyTo(next);
        _pool.Return(_array!, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
        _array = next;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool GrowIfRequired(int size, out int length)
    {
        length = default;
        var newIndex = checked(_index + size);
        if (_array!.Length - newIndex >= 0) return false;
        length = RoundUpPow2Ceiling(newIndex);
        return true;
    }

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
    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, "The buffer has been disposed.");
    }
}
