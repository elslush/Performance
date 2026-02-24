# ReusableLinked vs ResizableByteWriter Benchmark

Date: 2026-02-24

## Scope
Compared:
- `Performance.Buffers.ResizableByteWriter`
- `Performance.Buffers.ReusableLinkedArrayBufferWriter`
- `Performance.Buffers.ReusableLinkedArrayBufferWriterPool` (`Rent/Return` paths)

Benchmark class:
- `Performance.Benchmarks.Benches.ReusableLinkedArrayBufferWriterVsResizableByteWriterBench`

## Command
```bash
dotnet run -c Release --project Performance.Benchmarks -- --filter "Performance.Benchmarks.Benches.ReusableLinkedArrayBufferWriterVsResizableByteWriterBench.*" --memory --join --exporters GitHub CSV HTML --artifacts /home/ai/Appliances/Performance/BenchmarkResults/2026-02-24-linked-vs-resizable-bytewriter
```

## Key results
Across all tested payload/chunk combinations, `ResizableByteWriter` was faster than `ReusableLinkedArrayBufferWriter` for both write styles.

Representative rows:
- `TotalBytes=4096, ChunkSize=16`
  - `Resizable.Write(chunked)`: `4,034.7 ns`
  - `Linked.Write(chunked)`: `15,602.9 ns` (`3.87x` slower)
  - `Resizable.GetSpan+Advance(chunked)`: `8,180.9 ns`
  - `Linked.GetSpan+Advance(chunked)`: `9,911.9 ns` (`1.21x` slower)
- `TotalBytes=65536, ChunkSize=16`
  - `Resizable.Write(chunked)`: `29,933.4 ns`
  - `Linked.Write(chunked)`: `72,035.0 ns` (`2.41x` slower)
  - `Resizable.GetSpan+Advance(chunked)`: `42,020.9 ns`
  - `Linked.GetSpan+Advance(chunked)`: `50,238.5 ns` (`1.20x` slower)
- `TotalBytes=65536, ChunkSize=4096` (largest chunk)
  - `Resizable.Write(chunked)`: `1,571.0 ns`
  - `Linked.Write(chunked)`: `2,639.1 ns` (`1.68x` slower)

Pool paths:
- `LinkedPool.Rent+Write+Return` tracked closely to `Linked.Write(chunked)` and was generally the slowest path.
- `LinkedPool.Rent+Span+Return` was consistently faster than pool-write and often close to non-pooled linked span+advance, but still slower than resizable baselines.

Memory:
- Reported managed allocation remained `-` for all rows in this run.

## Interpretation
- For contiguous byte accumulation and chunked copy/write patterns, `ResizableByteWriter` is the throughput winner in this environment.
- `ReusableLinkedArrayBufferWriter` may still be useful for segmented serialization pipelines where avoiding full-buffer growth copy and direct segment emission are more important than raw in-memory append speed.

## Notes
- Environment warning from BenchmarkDotNet: `Failed to set up high priority (Permission denied)`.
- As with other short-iteration benchmarks in this repo, treat fine-grained deltas cautiously; relative direction here was consistent across all tested parameter sets.

## Artifacts
- Markdown: `linked-vs-resizable-report.md`
- CSV: `linked-vs-resizable-report.csv`
- HTML: `linked-vs-resizable-report.html`
- Run log: `Performance.Benchmarks.Benches.ReusableLinkedArrayBufferWriterVsResizableByteWriterBench-20260224-053005.log`
