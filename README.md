# Performance

My collection of high-performance data structures designed for efficient memory management and fast operations in .NET applications.

## Overview

This library provides high-performance alternatives to standard .NET collections and utilities that prioritize memory efficiency and speed. The components are built with performance in mind, utilizing techniques like array pooling, zero-allocation operations, and efficient memory management patterns.

## Key Components

### 1. ResizableByteWriter
A high-performance byte writer that implements `Stream` and `IBufferWriter<byte>` interfaces. It efficiently manages memory allocation and provides methods for writing data with automatic buffer resizing.

Key features:
- Implements `Stream` interface for compatibility with existing code
- Implements `IBufferWriter<byte>` for efficient buffer management
- Uses array pooling for optimal memory reuse
- Automatically grows buffers using power-of-two sizing strategy
- Provides both `GetSpan()` and `GetMemory()` methods for efficient writing
- Supports reset and dispose operations

### 2. ResizableSpanWriter<T>
A generic version of the byte writer that works with any type `T`. Provides similar functionality to `ResizableByteWriter` but for any data type.

Key features:
- Generic type support
- Implements `IBufferWriter<T>` and `IMemoryOwner<T>` interfaces
- Efficient memory management with array pooling
- Automatic buffer growth with power-of-two sizing
- Support for various write operations including single items, spans, and arrays

### 3. SpanSplitEnumerator<T>
A high-performance enumerator for splitting spans by a separator element. Designed to work with any type that implements `IEquatable<T>`.

Key features:
- Zero-allocation enumeration
- Efficient splitting of spans without creating intermediate arrays
- Works with any comparable type
- Supports both character and byte splitting

### 4. WhitespaceSplitEnumerator
A highly optimized enumerator for splitting strings on whitespace characters. Specifically designed for performance with Unicode whitespace handling.

Key features:
- Extremely fast ASCII whitespace detection
- Unicode-aware whitespace handling
- Zero-allocation enumeration
- Optimized for common whitespace patterns

## Why This Library Is Useful

1. **Memory Efficiency**: All components utilize array pooling to minimize garbage collection pressure
2. **Performance**: Optimized algorithms and minimal allocations for high-throughput scenarios
3. **Flexibility**: Multiple interfaces implemented for broad compatibility
4. **Type Safety**: Generic versions maintain compile-time type safety
5. **Modern Patterns**: Follows .NET best practices for buffer management and memory usage

## Example Usage

### ResizableByteWriter Usage
```csharp
using Performance.Buffers;

// Create a byte writer with initial capacity
using var writer = new ResizableByteWriter(initialCapacity: 1024);

// Write individual bytes
writer.Write((byte)1);
writer.Write((byte)2);

// Write byte arrays
writer.Write(new byte[] { 3, 4, 5 });

// Write spans
var data = new byte[] { 6, 7, 8, 9 };
writer.Write(data.AsSpan(1, 2)); // Writes bytes 7 and 8

// Access written data
var writtenBytes = writer.WrittenSpan;
Console.WriteLine($"Written {writtenBytes.Length} bytes");
```

### ResizableSpanWriter<T> Usage
```csharp
using Performance.Buffers;

// Create a span writer for integers
using var writer = new ResizableSpanWriter<int>();

// Write individual items
writer.Write(1);
writer.Write(2);

// Write arrays
writer.Write(new int[] { 3, 4, 5 });

// Write spans
var data = new int[] { 6, 7, 8, 9 };
writer.Write(data.AsSpan(1, 2)); // Writes 7 and 8

// Access written data
var writtenItems = writer.WrittenSpan;
Console.WriteLine($"Written {writtenItems.Length} items");
```

### SpanSplitEnumerator Usage
```csharp
using Performance.Enumerators;

// Split a string by comma
string input = "apple,banana,cherry,date";
foreach (var segment in new SpanSplitEnumerator<char>(input.AsSpan(), ','))
{
    Console.WriteLine($"Found: '{segment.ToString()}'");
}

// Split a byte array by delimiter
byte[] data = { 1, 2, 0, 3, 0, 4 };
foreach (var segment in new SpanSplitEnumerator<byte>(data.AsSpan(), 0))
{
    Console.WriteLine($"Found: [{string.Join(",", segment.ToArray())}]");
}
```

### WhitespaceSplitEnumerator Usage
```csharp
using Performance.Enumerators;

// Split a string on whitespace
string input = "  Hello   world\t\n  from\t  .NET  ";
foreach (var token in new WhitespaceSplitEnumerator(input.AsSpan()))
{
    Console.WriteLine($"Token: '{token.ToString()}'");
}
// Output:
// Token: 'Hello'
// Token: 'world'
// Token: 'from'
// Token: '.NET'
```

## Performance Benefits

- **Reduced Allocations**: Buffer pooling minimizes garbage collection pressure
- **Zero-Cost Enumeration**: Enumerators don't allocate during iteration
- **Efficient Memory Layout**: Sequential memory access patterns for cache efficiency
- **Automatic Growth**: Smart buffer sizing prevents frequent reallocations
- **Stream Compatibility**: Integrates seamlessly with existing stream-based APIs

## Testing

The library includes comprehensive unit tests covering all major functionality, edge cases, and performance scenarios. Tests validate correctness, memory management, and performance characteristics.

## License

MIT License - see LICENSE file for details.