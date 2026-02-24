# Streaming Bench Suite (Post-Fix11)

Date: 2026-02-24

## Command
```bash
dotnet run -c Release --project Performance.Benchmarks -- --filter "*Resizable*StreamingBench*" --memory --join --exporters GitHub CSV HTML --artifacts /home/ai/Appliances/Performance/BenchmarkResults/2026-02-24-streaming-suite-post-fix11
```

## Scope
Benchmarks included:
- `ResizableByteWriterStreamingBench`
- `ResizableSpanWriterStreamingBench`

## Key target rows
`ResizableSpanWriterStreamingBench`, `TotalItems=65536`, `ChunkSize=16`:
- Old.Write(chunked): `30,684.8 ns`
- New.Write(chunked): `21,055.6 ns`
- Old.GetSpan+Advance(chunked): `15,994.8 ns`
- New.GetSpan+Advance(chunked): `17,797.1 ns`

## Notes
- The original severe regression from earlier full-suite runs is no longer present.
- A smaller residual delta remains in `New.GetSpan+Advance(chunked)` for the `65536/16` case in this run.
- Environment warning remains: benchmark process cannot elevate to high priority (`Permission denied`), which increases run-to-run variance.

## Artifacts
- Markdown report: `streaming-suite-report.md`
- CSV report: `streaming-suite-report.csv`
- HTML report: `streaming-suite-report.html`
- Full run log: `BenchmarkRun-20260224-000247.log`
