# ResizableSpanWriter Regression Fix (Post-Fix11)

Date: 2026-02-24

## Scope
Targeted the remaining regression in:
- `ResizableSpanWriterStreamingBench.New.GetSpan+Advance(chunked)`
- Focus parameter set: `TotalItems=65536`, `ChunkSize=16`

## Code changes
Updated:
- `Performance/Performance/Buffers/ResizableSpanWriter.cs`

Key hot-path changes:
- Kept reservation tracking with `_available` and retained reservation invalidation semantics.
- Simplified growth path (`Grow`) to a single fast path with one bounds check.
- Made `Advance`, `GetSpan`, and `GetMemory` aggressively inlineable.
- Optimized `Advance` so disposed checks are off the common valid path.

## Verification command
```bash
dotnet run -c Release --project Performance.Benchmarks -- --filter "Performance.Benchmarks.Benches.ResizableSpanWriterStreamingBench.*" --memory --join --exporters GitHub CSV HTML --artifacts /home/ai/Appliances/Performance/BenchmarkResults/2026-02-24-resizable-spanwriter-post-fix11
```

## Key comparison (`TotalItems=65536`, `ChunkSize=16`)
Historical full-suite (before latest fix):
- Old.GetSpan+Advance(chunked): `35,338.3846 ns`
- New.GetSpan+Advance(chunked): `60,042.1667 ns` (`~1.70x` slower vs old row)

Post-fix11 focused run:
- Old.GetSpan+Advance(chunked): `45,265.8 ns`
- New.GetSpan+Advance(chunked): `36,841.2 ns` (`~0.81x` vs old row, faster)

## Stability note
This environment reports `Failed to set up high priority (Permission denied)`, and this benchmark uses very short iteration times. Adjacent reruns showed multimodal variance in the same row, so interpret single-run deltas cautiously.

## Artifacts
- Markdown: `results/Performance.Benchmarks.Benches.ResizableSpanWriterStreamingBench-report-github.md`
- CSV: `results/Performance.Benchmarks.Benches.ResizableSpanWriterStreamingBench-report.csv`
- HTML: `results/Performance.Benchmarks.Benches.ResizableSpanWriterStreamingBench-report.html`
- Log: `Performance.Benchmarks.Benches.ResizableSpanWriterStreamingBench-20260223-235959.log`
