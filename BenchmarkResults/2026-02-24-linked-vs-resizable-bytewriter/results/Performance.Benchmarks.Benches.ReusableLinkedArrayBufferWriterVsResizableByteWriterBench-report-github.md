```

BenchmarkDotNet v0.15.3, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
INTEL XEON GOLD 6548N 0.80GHz, 2 CPU, 128 logical and 64 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v4
  Job-CNUJVU : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                             | TotalBytes | ChunkSize | Mean        | Ratio | RatioSD | Allocated | Alloc Ratio |
|----------------------------------- |----------- |---------- |------------:|------:|--------:|----------:|------------:|
| **Resizable.Write(chunked)**           | **4096**       | **16**        |  **4,034.7 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Linked.Write(chunked)              | 4096       | 16        | 15,602.9 ns |  3.87 |    0.04 |         - |          NA |
| Resizable.GetSpan+Advance(chunked) | 4096       | 16        |  8,180.9 ns |  2.03 |    0.02 |         - |          NA |
| Linked.GetSpan+Advance(chunked)    | 4096       | 16        |  9,911.9 ns |  2.46 |    0.02 |         - |          NA |
| LinkedPool.Rent+Write+Return       | 4096       | 16        | 15,959.8 ns |  3.96 |    0.04 |         - |          NA |
| LinkedPool.Rent+Span+Return        | 4096       | 16        | 10,021.1 ns |  2.48 |    0.04 |         - |          NA |
|                                    |            |           |             |       |         |           |             |
| **Resizable.Write(chunked)**           | **4096**       | **256**       |    **428.7 ns** |  **1.00** |    **0.05** |         **-** |          **NA** |
| Linked.Write(chunked)              | 4096       | 256       |  1,366.9 ns |  3.19 |    0.12 |         - |          NA |
| Resizable.GetSpan+Advance(chunked) | 4096       | 256       |    772.2 ns |  1.80 |    0.08 |         - |          NA |
| Linked.GetSpan+Advance(chunked)    | 4096       | 256       |  1,037.0 ns |  2.42 |    0.11 |         - |          NA |
| LinkedPool.Rent+Write+Return       | 4096       | 256       |  1,583.4 ns |  3.70 |    0.17 |         - |          NA |
| LinkedPool.Rent+Span+Return        | 4096       | 256       |  1,194.3 ns |  2.79 |    0.13 |         - |          NA |
|                                    |            |           |             |       |         |           |             |
| **Resizable.Write(chunked)**           | **4096**       | **4096**      |    **140.2 ns** |  **1.01** |    **0.13** |         **-** |          **NA** |
| Linked.Write(chunked)              | 4096       | 4096      |    334.1 ns |  2.40 |    0.22 |         - |          NA |
| Resizable.GetSpan+Advance(chunked) | 4096       | 4096      |    165.3 ns |  1.19 |    0.16 |         - |          NA |
| Linked.GetSpan+Advance(chunked)    | 4096       | 4096      |    286.4 ns |  2.06 |    0.21 |         - |          NA |
| LinkedPool.Rent+Write+Return       | 4096       | 4096      |    514.8 ns |  3.70 |    0.47 |         - |          NA |
| LinkedPool.Rent+Span+Return        | 4096       | 4096      |    475.2 ns |  3.42 |    0.34 |         - |          NA |
|                                    |            |           |             |       |         |           |             |
| **Resizable.Write(chunked)**           | **65536**      | **16**        | **29,933.4 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Linked.Write(chunked)              | 65536      | 16        | 72,035.0 ns |  2.41 |    0.07 |         - |          NA |
| Resizable.GetSpan+Advance(chunked) | 65536      | 16        | 42,020.9 ns |  1.40 |    0.04 |         - |          NA |
| Linked.GetSpan+Advance(chunked)    | 65536      | 16        | 50,238.5 ns |  1.68 |    0.02 |         - |          NA |
| LinkedPool.Rent+Write+Return       | 65536      | 16        | 71,877.3 ns |  2.40 |    0.05 |         - |          NA |
| LinkedPool.Rent+Span+Return        | 65536      | 16        | 48,635.2 ns |  1.63 |    0.03 |         - |          NA |
|                                    |            |           |             |       |         |           |             |
| **Resizable.Write(chunked)**           | **65536**      | **256**       |  **5,883.0 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| Linked.Write(chunked)              | 65536      | 256       | 17,922.9 ns |  3.05 |    0.06 |         - |          NA |
| Resizable.GetSpan+Advance(chunked) | 65536      | 256       | 11,282.5 ns |  1.92 |    0.01 |         - |          NA |
| Linked.GetSpan+Advance(chunked)    | 65536      | 256       | 15,598.3 ns |  2.65 |    0.02 |         - |          NA |
| LinkedPool.Rent+Write+Return       | 65536      | 256       | 18,098.8 ns |  3.08 |    0.03 |         - |          NA |
| LinkedPool.Rent+Span+Return        | 65536      | 256       | 13,178.5 ns |  2.24 |    0.02 |         - |          NA |
|                                    |            |           |             |       |         |           |             |
| **Resizable.Write(chunked)**           | **65536**      | **4096**      |  **1,571.0 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| Linked.Write(chunked)              | 65536      | 4096      |  2,639.1 ns |  1.68 |    0.03 |         - |          NA |
| Resizable.GetSpan+Advance(chunked) | 65536      | 4096      |  1,946.4 ns |  1.24 |    0.03 |         - |          NA |
| Linked.GetSpan+Advance(chunked)    | 65536      | 4096      |  2,289.0 ns |  1.46 |    0.02 |         - |          NA |
| LinkedPool.Rent+Write+Return       | 65536      | 4096      |  2,718.1 ns |  1.73 |    0.04 |         - |          NA |
| LinkedPool.Rent+Span+Return        | 65536      | 4096      |  2,439.8 ns |  1.55 |    0.04 |         - |          NA |
