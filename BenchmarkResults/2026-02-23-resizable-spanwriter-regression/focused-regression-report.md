# ResizableSpanWriter Streaming Regression Report

Date: 2026-02-23

## Scope
Investigated and fixed the regression in:
- `ResizableSpanWriterStreamingBench.New.Write(chunked)`
- `ResizableSpanWriterStreamingBench.New.GetSpan+Advance(chunked)`

Target parameter set:
- `TotalItems=65536`, `ChunkSize=16`

## Symptom (initial)
From baseline full-suite and isolated repro:
- `Old.Write(chunked)`: `38,994.6 ns`
- `New.Write(chunked)`: `353,833.0 ns` (`9.07x` slower)
- `Old.GetSpan+Advance(chunked)`: `56,934.7 ns`
- `New.GetSpan+Advance(chunked)`: `388,226.3 ns` (`9.96x` slower)

Source:
- `Performance/BenchmarkResults/2026-02-23-resizable-spanwriter-diagnose/results/Performance.Benchmarks.Benches.ResizableSpanWriterStreamingBench-report-github.md`

## Investigation
1. Reproduced in isolated `ResizableSpanWriterStreamingBench` runs.
2. Verified sensitivity to warmup/tiered JIT in `InvocationCount=1` configuration.
3. Added explicit steady-state warmup in benchmark `GlobalSetup`.

## Fix 1 (benchmark stability)
Updated:
- `Performance/Performance.Benchmarks/Benches/ResizableSpanWriterStreamingBench.cs`

Changes:
- Added steady-state warmup loop in `GlobalSetup`.
- Split benchmark methods into explicit core methods and warmed each path.

Also added benchmark unit tests:
- `Performance/Performance.Benchmarks.UnitTests/Benches/ResizableSpanWriterStreamingBenchTests.cs`

## Full-suite rerun after Fix 1
Artifacts:
- `Performance/BenchmarkResults/2026-02-23-full-suite-after-streaming-fix/full-suite-report.md`

Target case (`65536/16`) from full suite:
- `Old.Write(chunked)`: `30,090.3077 ns`
- `New.Write(chunked)`: `29,744.1667 ns` (`0.99x`)
- `Old.GetSpan+Advance(chunked)`: `35,338.3846 ns`
- `New.GetSpan+Advance(chunked)`: `60,042.1667 ns` (`2.00x`)

Result:
- Catastrophic `9x` write regression removed.
- Residual slowdown remained for `New.GetSpan+Advance(chunked)`.

## Fix 2 (writer hot path)
Updated:
- `Performance/Performance/Buffers/ResizableSpanWriter.cs`

Changes:
- Replaced `_available` reservation tracking with `_reservationEnd` absolute bound.
- Removed per-advance reservation reset store.
- Kept invalidation semantics on direct writes/reset.

## Focused rerun after Fix 2
Artifacts:
- `Performance/BenchmarkResults/2026-02-23-resizable-spanwriter-post-full-verify/results/Performance.Benchmarks.Benches.ResizableSpanWriterStreamingBench-report-github.md`

Target case (`65536/16`) results:
- `Old.Write(chunked)`: `31,002.8 ns`
- `New.Write(chunked)`: `24,051.2 ns` (`0.78x`)
- `Old.GetSpan+Advance(chunked)`: `35,228.3 ns`
- `New.GetSpan+Advance(chunked)`: `43,899.0 ns` (`1.42x`)

## Conclusion
- The original severe regression is fixed.
- `New.Write(chunked)` is now at or better than parity in the target case.
- `New.GetSpan+Advance(chunked)` still shows a smaller residual overhead in the target case, but no longer exhibits catastrophic behavior.
