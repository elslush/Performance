using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Performance.Enumerators;

public ref struct SpanSplitEnumerator<T> where T : IEquatable<T>
{
    private readonly ReadOnlySpan<T> _span;
    private readonly T _separator;
    private int _currentIndex;

    public SpanSplitEnumerator(ReadOnlySpan<T> span, T separator)
    {
        _span = span;
        _separator = separator;
        _currentIndex = 0;
        Current = default;
    }

    public ReadOnlySpan<T> Current { get; private set; }

    public readonly SpanSplitEnumerator<T> GetEnumerator() => this;

    public bool MoveNext()
    {
        if (_currentIndex > _span.Length)
            return false;

        int index = _span[_currentIndex..].IndexOf(_separator);
        if (index == -1)
        {
            Current = _span[_currentIndex..];
            _currentIndex = _span.Length + 1; // Move past the end
            return true;
        }

        Current = _span.Slice(_currentIndex, index);
        _currentIndex += index + 1;
        return true;
    }
}
