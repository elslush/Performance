# Full Benchmark Rerun After Streaming Fix - 2026-02-23

## What was run
- Project: `Performance/Performance.Benchmarks`
- Command:
  - `dotnet run -c Release --project /home/ai/Appliances/Performance/Performance.Benchmarks -- --filter '*' --memory --join --exporters GitHub CSV HTML --artifacts /home/ai/Appliances/Performance/BenchmarkResults/2026-02-23-full-suite-after-streaming-fix`

## Run summary
- Date: 2026-02-23
- Benchmarks executed: 152
- Total elapsed: `00:58:17`
- Runtime observed in reports: `.NET 10.0.0`
- Note: BenchmarkDotNet reported `Failed to set up high priority (Permission denied)` in this environment.

## Stable output files
- Markdown report: `full-suite-report.md`
- CSV report: `full-suite-report.csv`
- HTML report: `full-suite-report.html`
- Full execution log: `full-suite-run.log`
- Filtered excerpt for key benches: `key-benchmarks.md`

## Key finding for target regression case (`TotalItems=65536`, `ChunkSize=16`)
From `full-suite-report.md`:
- `ResizableSpanWriterStreamingBench Old.Write(chunked)`: `30,090.3077 ns`
- `ResizableSpanWriterStreamingBench New.Write(chunked)`: `29,744.1667 ns` (`0.99x`)
- `ResizableSpanWriterStreamingBench Old.GetSpan+Advance(chunked)`: `35,338.3846 ns`
- `ResizableSpanWriterStreamingBench New.GetSpan+Advance(chunked)`: `60,042.1667 ns` (`2.00x`)

## Follow-up focused verification (after writer hot-path change)
After updating `Performance/Performance/Buffers/ResizableSpanWriter.cs`, reran only:
- `ResizableSpanWriterStreamingBench` with artifacts at:
  - `Performance/BenchmarkResults/2026-02-23-resizable-spanwriter-post-full-verify/`

Target case results from focused rerun:
- `Old.Write(chunked)`: `31,002.8 ns`
- `New.Write(chunked)`: `24,051.2 ns` (`0.78x`)
- `Old.GetSpan+Advance(chunked)`: `35,228.3 ns`
- `New.GetSpan+Advance(chunked)`: `43,899.0 ns` (`1.42x`)

This confirms the catastrophic 9x regression is removed; a smaller residual delta remains for `New.GetSpan+Advance(chunked)` in the `65536/16` case.

## Raw timestamped exporter files
- `results/BenchmarkRun-joined-2026-02-23-23-30-12-report-github.md`
- `results/BenchmarkRun-joined-2026-02-23-23-30-12-report.csv`
- `results/BenchmarkRun-joined-2026-02-23-23-30-12-report.html`
