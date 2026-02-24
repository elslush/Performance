# Key Benchmarks (Short Streaming)

| TotalBytes | ChunkSize | Resizable.Write+FlushAsync | Best Linked Variant | Best Linked Mean | Delta vs Resizable |
|-----------:|----------:|---------------------------:|--------------------|-----------------:|-------------------:|
| 64 | 8 | 725.6 ns | `Linked.GetSpan+WriteToResetAsync` | 1,197.8 ns | 1.66x slower |
| 64 | 32 | 762.1 ns | `LinkedFirst.Write+WriteToResetAsync` | 1,002.5 ns | 1.32x slower |
| 64 | 128 | 592.8 ns | `LinkedFirst.Write+WriteToResetAsync` | 837.5 ns | 1.42x slower |
| 256 | 8 | 1,049.8 ns | `Linked.GetSpan+WriteToResetAsync` | 2,088.1 ns | 1.99x slower |
| 256 | 32 | 1,545.7 ns | `LinkedFirst.Write+WriteToResetAsync` | 2,023.3 ns | 1.31x slower |
| 256 | 128 | 805.6 ns | `LinkedFirst.Write+WriteToResetAsync` | 980.5 ns | 1.23x slower |
| 1024 | 8 | 2,632.1 ns | `Linked.GetSpan+WriteToResetAsync` | 5,828.4 ns | 2.22x slower |
| 1024 | 32 | 4,583.3 ns | `Linked.GetSpan+WriteToResetAsync` | 5,732.2 ns | 1.25x slower |
| 1024 | 128 | 1,555.7 ns | `LinkedFirst.Write+WriteToResetAsync` | 1,975.0 ns | 1.27x slower |
| 4096 | 8 | 9,842.4 ns | `LinkedPool.Rent+Span+Flush+Return` | 19,719.2 ns | 2.02x slower |
| 4096 | 32 | 17,189.3 ns | `LinkedPool.Rent+Span+Flush+Return` | 20,729.5 ns | 1.21x slower |
| 4096 | 128 | 4,746.4 ns | `LinkedPool.Rent+Span+Flush+Return` | 6,007.2 ns | 1.27x slower |

Conclusion: no tested short-streaming case beat `Resizable.Write+FlushAsync` in this environment.
