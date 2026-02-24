# Resizable Streaming Benchmarks (ShortRun)

Run date: 2026-02-23  
Suite: `ResizableByteWriterStreamingBench` + `ResizableSpanWriterStreamingBench`  
Runtime: `.NET 10.0`, Linux x64

## Command used

```bash
dotnet run -c Release --project Performance.Benchmarks -- \
  --job short --memory --join \
  --filter '*ResizableByteWriterStreamingBench*' '*ResizableSpanWriterStreamingBench*' \
  --exporters GitHub \
  --artifacts Performance/BenchmarkResults/2026-02-23-resizable-streaming
```

## Saved outputs

- `BenchmarkResults/2026-02-23-resizable-streaming/resizable-streaming-shortrun-report.md`
- `BenchmarkResults/2026-02-23-resizable-streaming/resizable-streaming-shortrun-report.csv`
- `BenchmarkResults/2026-02-23-resizable-streaming/resizable-streaming-shortrun-report.html`
- `BenchmarkResults/2026-02-23-resizable-streaming/BenchmarkRun-20260223-163226.log`

## Quick takeaways

- `ResizableByteWriter` new implementation is mostly near parity or slightly faster than old across tested chunk sizes.
- `ResizableSpanWriter` new implementation is mixed:
  - Faster for small workload (`TotalItems=4096`, `ChunkSize=16`) in `Write(chunked)`.
  - Significantly slower for large workload with tiny chunks (`TotalItems=65536`, `ChunkSize=16`) in both write paths.
  - Generally slower for larger total workloads in this run.
- Allocation remained `-` (no managed allocations reported per operation) for all measured cases.

## Important note

This run used `Job.Short` (3 measured iterations), so some ratios have high variance.  
Use `--job medium` or `--job long` to confirm any large regression before making tuning decisions.
