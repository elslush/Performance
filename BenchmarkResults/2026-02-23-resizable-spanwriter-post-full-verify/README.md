# ResizableSpanWriter Streaming Post-Full Verification - 2026-02-23

## What was run
- Project: `Performance/Performance.Benchmarks`
- Command:
  - `dotnet run -c Release --project /home/ai/Appliances/Performance/Performance.Benchmarks -- --filter "Performance.Benchmarks.Benches.ResizableSpanWriterStreamingBench.*" --memory --join --exporters GitHub CSV HTML --artifacts /home/ai/Appliances/Performance/BenchmarkResults/2026-02-23-resizable-spanwriter-post-full-verify`

## Run summary
- Benchmarks executed: 24
- Total elapsed: `00:00:13`
- Runtime: `.NET 10.0.0`

## Stable output files
- Markdown report: `resizable-spanwriter-streaming-report.md`
- CSV report: `resizable-spanwriter-streaming-report.csv`
- HTML report: `resizable-spanwriter-streaming-report.html`
- Full execution log: `resizable-spanwriter-streaming-run.log`

## Target case (`TotalItems=65536`, `ChunkSize=16`)
- `Old.Write(chunked)`: `31,002.8 ns`
- `New.Write(chunked)`: `24,051.2 ns` (`0.78x`)
- `Old.GetSpan+Advance(chunked)`: `35,228.3 ns`
- `New.GetSpan+Advance(chunked)`: `43,899.0 ns` (`1.42x`)
