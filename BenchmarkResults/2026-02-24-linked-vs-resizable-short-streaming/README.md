# ReusableLinked vs ResizableByteWriter (Short Streaming) Benchmark

Date: 2026-02-24

## Scope
This run targets short streaming behavior (small chunked appends followed by stream flush):
- `Performance.Buffers.ResizableByteWriter`
- `Performance.Buffers.ReusableLinkedArrayBufferWriter`
- `Performance.Buffers.ReusableLinkedArrayBufferWriterPool` (`Rent/Return` paths)

Benchmark class:
- `Performance.Benchmarks.Benches.ReusableLinkedArrayBufferWriterShortStreamingBench`

Why this covers short streaming:
- Each benchmark writes data in small chunks (`ChunkSize` 8/32/128).
- After filling, it flushes to a `Stream` via either direct `WriteAsync` (resizable) or `WriteToAndResetAsync` (linked).

## Command
```bash
dotnet run -c Release --project Performance.Benchmarks -- --filter "Performance.Benchmarks.Benches.ReusableLinkedArrayBufferWriterShortStreamingBench.*" --memory --join --exporters GitHub CSV HTML --artifacts /home/ai/Appliances/Performance/BenchmarkResults/2026-02-24-linked-vs-resizable-short-streaming
```

## Key outcome
- `Resizable.Write+FlushAsync` was fastest in all tested parameter sets.
- No linked variant beat resizable in this benchmark.

Representative rows:
- `TotalBytes=64, ChunkSize=32`
  - `Resizable.Write+FlushAsync`: `762.1 ns`
  - Best linked path (`LinkedFirst.Write+WriteToResetAsync`): `1,002.5 ns` (`1.32x` slower)
- `TotalBytes=256, ChunkSize=128`
  - `Resizable.Write+FlushAsync`: `805.6 ns`
  - Best linked path (`LinkedFirst.Write+WriteToResetAsync`): `980.5 ns` (`1.23x` slower)
- `TotalBytes=1024, ChunkSize=32`
  - `Resizable.Write+FlushAsync`: `4,583.3 ns`
  - Best linked path (`Linked.GetSpan+WriteToResetAsync`): `5,732.2 ns` (`1.25x` slower)
- `TotalBytes=4096, ChunkSize=32`
  - `Resizable.Write+FlushAsync`: `17,189.3 ns`
  - Best linked path (`LinkedPool.Rent+Span+Flush+Return`): `20,729.5 ns` (`1.21x` slower)

## Interpretation
- For this specific short-streaming pattern (chunked in-memory accumulation + immediate flush to one stream sink), resizable remains the throughput winner.
- Linked/pool variants may still make sense where segmented handoff is required by a downstream API or when avoiding large contiguous growth copies is the main design goal.

## Notes
- MemoryDiagnoser reported `0 B` allocated for all rows.
- BenchmarkDotNet logged: `Failed to set up high priority (Permission denied)`.

## Artifacts
- Markdown report: `linked-vs-resizable-short-streaming-report.md`
- CSV report: `linked-vs-resizable-short-streaming-report.csv`
- HTML report: `linked-vs-resizable-short-streaming-report.html`
- Run log: `Performance.Benchmarks.Benches.ReusableLinkedArrayBufferWriterShortStreamingBench-20260224-142801.log`
