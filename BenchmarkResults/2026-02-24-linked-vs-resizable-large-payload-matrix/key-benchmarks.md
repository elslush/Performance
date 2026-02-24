# Key Benchmarks (Large Payload Matrix)

## Stream Flush Paths

| TotalBytes | ChunkSize | Resizable.Fill+SingleFlush | Linked.Fill+SegmentFlush | Winner |
|-----------:|----------:|---------------------------:|-------------------------:|:------|
| 256KB | 256 | 135.915 μs | 182.491 μs | Resizable |
| 256KB | 4KB | 7.701 μs | 11.375 μs | Resizable |
| 1MB | 64KB | 46.591 μs | 47.597 μs | Resizable (near tie) |
| 4MB | 64KB | 215.581 μs | 217.726 μs | Resizable (near tie) |
| 16MB | 4KB | 966.189 μs | 1,012.431 μs | Resizable |
| 16MB | 64KB | 942.942 μs | 925.603 μs | Linked |

## Segment Consume Paths

| TotalBytes | ChunkSize | Resizable.Fill+SegmentConsume | Linked.Fill+SegmentConsume | Winner |
|-----------:|----------:|------------------------------:|---------------------------:|:------|
| 256KB | 4KB | 7.876 μs | 11.261 μs | Resizable |
| 1MB | 4KB | 48.369 μs | 66.622 μs | Resizable |
| 4MB | 64KB | 232.607 μs | 219.871 μs | Linked |
| 16MB | 4KB | 1,049.679 μs | 1,005.607 μs | Linked |
| 16MB | 64KB | 954.131 μs | 1,053.942 μs | Resizable |

## ToArray Penalty

`Linked.Fill+ToArray+SingleFlush` allocations by payload:
- `256KB`: `262,168 B`
- `1MB`: `1,048,600 B`
- `4MB`: `4,194,328 B`
- `16MB`: `16,777,240 B`

It was slower than both non-`ToArray` paths in every tested case.
