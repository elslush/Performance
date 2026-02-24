```

BenchmarkDotNet v0.15.3, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
INTEL XEON GOLD 6548N 0.80GHz, 2 CPU, 128 logical and 64 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v4
  Job-CNUJVU : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                              | TotalBytes | ChunkSize | Mean        | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |----------- |---------- |------------:|------:|--------:|----------:|------------:|
| **Resizable.Write+FlushAsync**          | **64**         | **8**         |    **725.6 ns** |  **1.00** |    **0.08** |         **-** |          **NA** |
| Linked.Write+WriteToResetAsync      | 64         | 8         |  1,400.5 ns |  1.94 |    0.12 |         - |          NA |
| Linked.GetSpan+WriteToResetAsync    | 64         | 8         |  1,197.8 ns |  1.66 |    0.10 |         - |          NA |
| LinkedFirst.Write+WriteToResetAsync | 64         | 8         |  1,210.8 ns |  1.67 |    0.10 |         - |          NA |
| LinkedPool.Rent+Write+Flush+Return  | 64         | 8         |  1,571.2 ns |  2.17 |    0.15 |         - |          NA |
| LinkedPool.Rent+Span+Flush+Return   | 64         | 8         |  1,469.3 ns |  2.03 |    0.14 |         - |          NA |
|                                     |            |           |             |       |         |           |             |
| **Resizable.Write+FlushAsync**          | **64**         | **32**        |    **762.1 ns** |  **1.00** |    **0.05** |         **-** |          **NA** |
| Linked.Write+WriteToResetAsync      | 64         | 32        |  1,098.4 ns |  1.44 |    0.06 |         - |          NA |
| Linked.GetSpan+WriteToResetAsync    | 64         | 32        |  1,102.9 ns |  1.45 |    0.07 |         - |          NA |
| LinkedFirst.Write+WriteToResetAsync | 64         | 32        |  1,002.5 ns |  1.32 |    0.09 |         - |          NA |
| LinkedPool.Rent+Write+Flush+Return  | 64         | 32        |  1,252.1 ns |  1.65 |    0.15 |         - |          NA |
| LinkedPool.Rent+Span+Flush+Return   | 64         | 32        |  1,145.8 ns |  1.51 |    0.09 |         - |          NA |
|                                     |            |           |             |       |         |           |             |
| **Resizable.Write+FlushAsync**          | **64**         | **128**       |    **592.8 ns** |  **1.00** |    **0.06** |         **-** |          **NA** |
| Linked.Write+WriteToResetAsync      | 64         | 128       |    898.9 ns |  1.52 |    0.09 |         - |          NA |
| Linked.GetSpan+WriteToResetAsync    | 64         | 128       |    878.1 ns |  1.48 |    0.07 |         - |          NA |
| LinkedFirst.Write+WriteToResetAsync | 64         | 128       |    837.5 ns |  1.42 |    0.13 |         - |          NA |
| LinkedPool.Rent+Write+Flush+Return  | 64         | 128       |  1,091.7 ns |  1.85 |    0.16 |         - |          NA |
| LinkedPool.Rent+Span+Flush+Return   | 64         | 128       |  1,044.8 ns |  1.77 |    0.18 |         - |          NA |
|                                     |            |           |             |       |         |           |             |
| **Resizable.Write+FlushAsync**          | **256**        | **8**         |  **1,049.8 ns** |  **1.00** |    **0.04** |         **-** |          **NA** |
| Linked.Write+WriteToResetAsync      | 256        | 8         |  2,912.1 ns |  2.78 |    0.09 |         - |          NA |
| Linked.GetSpan+WriteToResetAsync    | 256        | 8         |  2,088.1 ns |  1.99 |    0.08 |         - |          NA |
| LinkedFirst.Write+WriteToResetAsync | 256        | 8         |  2,595.0 ns |  2.47 |    0.09 |         - |          NA |
| LinkedPool.Rent+Write+Flush+Return  | 256        | 8         |  2,925.7 ns |  2.79 |    0.09 |         - |          NA |
| LinkedPool.Rent+Span+Flush+Return   | 256        | 8         |  2,241.7 ns |  2.14 |    0.09 |         - |          NA |
|                                     |            |           |             |       |         |           |             |
| **Resizable.Write+FlushAsync**          | **256**        | **32**        |  **1,545.7 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Linked.Write+WriteToResetAsync      | 256        | 32        |  2,231.4 ns |  1.44 |    0.03 |         - |          NA |
| Linked.GetSpan+WriteToResetAsync    | 256        | 32        |  2,141.1 ns |  1.39 |    0.04 |         - |          NA |
| LinkedFirst.Write+WriteToResetAsync | 256        | 32        |  2,023.3 ns |  1.31 |    0.05 |         - |          NA |
| LinkedPool.Rent+Write+Flush+Return  | 256        | 32        |  2,463.5 ns |  1.59 |    0.07 |         - |          NA |
| LinkedPool.Rent+Span+Flush+Return   | 256        | 32        |  2,152.8 ns |  1.39 |    0.04 |         - |          NA |
|                                     |            |           |             |       |         |           |             |
| **Resizable.Write+FlushAsync**          | **256**        | **128**       |    **805.6 ns** |  **1.01** |    **0.16** |         **-** |          **NA** |
| Linked.Write+WriteToResetAsync      | 256        | 128       |  1,151.9 ns |  1.45 |    0.17 |         - |          NA |
| Linked.GetSpan+WriteToResetAsync    | 256        | 128       |  1,093.7 ns |  1.38 |    0.16 |         - |          NA |
| LinkedFirst.Write+WriteToResetAsync | 256        | 128       |    980.5 ns |  1.23 |    0.16 |         - |          NA |
| LinkedPool.Rent+Write+Flush+Return  | 256        | 128       |  1,353.1 ns |  1.70 |    0.27 |         - |          NA |
| LinkedPool.Rent+Span+Flush+Return   | 256        | 128       |  1,256.2 ns |  1.58 |    0.22 |         - |          NA |
|                                     |            |           |             |       |         |           |             |
| **Resizable.Write+FlushAsync**          | **1024**       | **8**         |  **2,632.1 ns** |  **1.00** |    **0.05** |         **-** |          **NA** |
| Linked.Write+WriteToResetAsync      | 1024       | 8         |  9,092.8 ns |  3.46 |    0.12 |         - |          NA |
| Linked.GetSpan+WriteToResetAsync    | 1024       | 8         |  5,828.4 ns |  2.22 |    0.08 |         - |          NA |
| LinkedFirst.Write+WriteToResetAsync | 1024       | 8         |  8,070.8 ns |  3.07 |    0.11 |         - |          NA |
| LinkedPool.Rent+Write+Flush+Return  | 1024       | 8         |  8,915.9 ns |  3.39 |    0.13 |         - |          NA |
| LinkedPool.Rent+Span+Flush+Return   | 1024       | 8         |  6,476.7 ns |  2.46 |    0.09 |         - |          NA |
|                                     |            |           |             |       |         |           |             |
| **Resizable.Write+FlushAsync**          | **1024**       | **32**        |  **4,583.3 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Linked.Write+WriteToResetAsync      | 1024       | 32        |  6,181.4 ns |  1.35 |    0.02 |         - |          NA |
| Linked.GetSpan+WriteToResetAsync    | 1024       | 32        |  5,732.2 ns |  1.25 |    0.02 |         - |          NA |
| LinkedFirst.Write+WriteToResetAsync | 1024       | 32        |  6,289.9 ns |  1.37 |    0.03 |         - |          NA |
| LinkedPool.Rent+Write+Flush+Return  | 1024       | 32        |  6,372.5 ns |  1.39 |    0.03 |         - |          NA |
| LinkedPool.Rent+Span+Flush+Return   | 1024       | 32        |  5,940.6 ns |  1.30 |    0.03 |         - |          NA |
|                                     |            |           |             |       |         |           |             |
| **Resizable.Write+FlushAsync**          | **1024**       | **128**       |  **1,555.7 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Linked.Write+WriteToResetAsync      | 1024       | 128       |  2,271.4 ns |  1.46 |    0.04 |         - |          NA |
| Linked.GetSpan+WriteToResetAsync    | 1024       | 128       |  2,169.2 ns |  1.39 |    0.02 |         - |          NA |
| LinkedFirst.Write+WriteToResetAsync | 1024       | 128       |  1,975.0 ns |  1.27 |    0.02 |         - |          NA |
| LinkedPool.Rent+Write+Flush+Return  | 1024       | 128       |  2,349.4 ns |  1.51 |    0.05 |         - |          NA |
| LinkedPool.Rent+Span+Flush+Return   | 1024       | 128       |  2,167.1 ns |  1.39 |    0.03 |         - |          NA |
|                                     |            |           |             |       |         |           |             |
| **Resizable.Write+FlushAsync**          | **4096**       | **8**         |  **9,842.4 ns** |  **1.01** |    **0.13** |         **-** |          **NA** |
| Linked.Write+WriteToResetAsync      | 4096       | 8         | 32,225.0 ns |  3.30 |    0.30 |         - |          NA |
| Linked.GetSpan+WriteToResetAsync    | 4096       | 8         | 20,503.4 ns |  2.10 |    0.20 |         - |          NA |
| LinkedFirst.Write+WriteToResetAsync | 4096       | 8         | 30,361.9 ns |  3.11 |    0.28 |         - |          NA |
| LinkedPool.Rent+Write+Flush+Return  | 4096       | 8         | 31,658.4 ns |  3.24 |    0.30 |         - |          NA |
| LinkedPool.Rent+Span+Flush+Return   | 4096       | 8         | 19,719.2 ns |  2.02 |    0.19 |         - |          NA |
|                                     |            |           |             |       |         |           |             |
| **Resizable.Write+FlushAsync**          | **4096**       | **32**        | **17,189.3 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| Linked.Write+WriteToResetAsync      | 4096       | 32        | 22,072.0 ns |  1.28 |    0.00 |         - |          NA |
| Linked.GetSpan+WriteToResetAsync    | 4096       | 32        | 21,433.8 ns |  1.25 |    0.05 |         - |          NA |
| LinkedFirst.Write+WriteToResetAsync | 4096       | 32        | 21,580.6 ns |  1.26 |    0.01 |         - |          NA |
| LinkedPool.Rent+Write+Flush+Return  | 4096       | 32        | 22,861.2 ns |  1.33 |    0.01 |         - |          NA |
| LinkedPool.Rent+Span+Flush+Return   | 4096       | 32        | 20,729.5 ns |  1.21 |    0.01 |         - |          NA |
|                                     |            |           |             |       |         |           |             |
| **Resizable.Write+FlushAsync**          | **4096**       | **128**       |  **4,746.4 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Linked.Write+WriteToResetAsync      | 4096       | 128       |  6,393.2 ns |  1.35 |    0.02 |         - |          NA |
| Linked.GetSpan+WriteToResetAsync    | 4096       | 128       |  6,160.9 ns |  1.30 |    0.02 |         - |          NA |
| LinkedFirst.Write+WriteToResetAsync | 4096       | 128       |  6,065.7 ns |  1.28 |    0.01 |         - |          NA |
| LinkedPool.Rent+Write+Flush+Return  | 4096       | 128       |  6,705.7 ns |  1.41 |    0.02 |         - |          NA |
| LinkedPool.Rent+Span+Flush+Return   | 4096       | 128       |  6,007.2 ns |  1.27 |    0.02 |         - |          NA |
