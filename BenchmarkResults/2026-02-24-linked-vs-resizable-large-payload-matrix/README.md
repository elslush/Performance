# ReusableLinked vs ResizableByteWriter (Large Payload Matrix)

Date: 2026-02-24

## Scope
Hypothesis test: linked buffers may perform best for large, growth-heavy payloads, especially with segment-friendly consumers.

Benchmark class:
- `Performance.Benchmarks.Benches.ReusableLinkedArrayBufferWriterLargePayloadBench`

Matrix:
- `TotalBytes`: `256KB`, `1MB`, `4MB`, `16MB`
- `ChunkSize`: `256`, `4KB`, `64KB`

Compared paths:
- `Resizable.Fill+SingleFlush` (contiguous writer -> single stream write)
- `Linked.Fill+SegmentFlush` (segmented writer -> `WriteToAndResetAsync`)
- `Linked.Fill+ToArray+SingleFlush` (forces contiguous copy)
- `Resizable.Fill+SegmentConsume` (single-segment consumer)
- `Linked.Fill+SegmentConsume` (multi-segment consumer)

## Command
```bash
dotnet run -c Release --project Performance.Benchmarks -- --filter "Performance.Benchmarks.Benches.ReusableLinkedArrayBufferWriterLargePayloadBench.*" --memory --join --exporters GitHub CSV HTML --artifacts /home/ai/Appliances/Performance/BenchmarkResults/2026-02-24-linked-vs-resizable-large-payload-matrix
```

## Key findings
- For stream flush paths, `Resizable.Fill+SingleFlush` won almost everywhere.
- One crossover appeared at very large payload + large chunk:
  - `TotalBytes=16MB, ChunkSize=64KB`
  - `Linked.Fill+SegmentFlush`: `925.603 μs`
  - `Resizable.Fill+SingleFlush`: `942.942 μs`
  - Linked is ~`1.8%` faster.
- For segment-consume paths, linked wins in a few large/large-chunk cases, but not consistently:
  - `TotalBytes=4MB, ChunkSize=64KB`: linked `219.871 μs` vs resizable `232.607 μs`
  - `TotalBytes=16MB, ChunkSize=4KB`: linked `1,005.607 μs` vs resizable `1,049.679 μs`
- `Linked.Fill+ToArray+SingleFlush` was consistently worst and allocated one full payload-sized array each run (`~payload + 24 bytes`).

## Interpretation
- The data supports a refined version of the theory:
  - Linked can become competitive (or slightly faster) at large payloads when chunk sizes are large and the consumer can benefit from segmented flow.
  - For most practical combinations in this matrix, resizable contiguous writing remains faster.
  - Forcing linked into contiguous output (`ToArray`) removes its main advantage and adds substantial allocation/copy cost.

## Notes
- MemoryDiagnoser showed `0 B` allocations for all non-`ToArray` paths in this run.
- BenchmarkDotNet logged repeated: `Failed to set up high priority (Permission denied)`.

## Artifacts
- Markdown report: `linked-vs-resizable-large-payload-report.md`
- CSV report: `linked-vs-resizable-large-payload-report.csv`
- HTML report: `linked-vs-resizable-large-payload-report.html`
- Key rows summary: `key-benchmarks.md`
- Run log: `Performance.Benchmarks.Benches.ReusableLinkedArrayBufferWriterLargePayloadBench-20260224-143519.log`
