```

BenchmarkDotNet v0.15.3, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
INTEL XEON GOLD 6548N 0.80GHz, 2 CPU, 128 logical and 64 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v4
  Job-CNUJVU : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                       | TotalItems | ChunkSize | Mean        | Ratio | RatioSD | Allocated | Alloc Ratio |
|----------------------------- |----------- |---------- |------------:|------:|--------:|----------:|------------:|
| **Old.Write(chunked)**           | **4096**       | **16**        |  **4,161.3 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| New.Write(chunked)           | 4096       | 16        |  4,173.2 ns |  1.00 |    0.03 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 4096       | 16        |  8,554.6 ns |  2.06 |    0.04 |         - |          NA |
| New.GetSpan+Advance(chunked) | 4096       | 16        |  9,630.2 ns |  2.31 |    0.03 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **4096**       | **256**       |    **702.8 ns** |  **1.00** |    **0.06** |         **-** |          **NA** |
| New.Write(chunked)           | 4096       | 256       |    642.7 ns |  0.92 |    0.06 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 4096       | 256       |    895.7 ns |  1.28 |    0.06 |         - |          NA |
| New.GetSpan+Advance(chunked) | 4096       | 256       |    982.9 ns |  1.40 |    0.06 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **4096**       | **4096**      |    **520.0 ns** |  **1.05** |    **0.33** |         **-** |          **NA** |
| New.Write(chunked)           | 4096       | 4096      |    389.0 ns |  0.79 |    0.18 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 4096       | 4096      |    416.5 ns |  0.84 |    0.25 |         - |          NA |
| New.GetSpan+Advance(chunked) | 4096       | 4096      |    398.7 ns |  0.81 |    0.19 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **65536**      | **16**        | **30,083.2 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| New.Write(chunked)           | 65536      | 16        | 23,157.7 ns |  0.77 |    0.01 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 65536      | 16        | 45,265.8 ns |  1.50 |    0.01 |         - |          NA |
| New.GetSpan+Advance(chunked) | 65536      | 16        | 36,841.2 ns |  1.22 |    0.03 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **65536**      | **256**       |  **9,064.7 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| New.Write(chunked)           | 65536      | 256       |  8,741.8 ns |  0.96 |    0.02 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 65536      | 256       | 13,825.2 ns |  1.53 |    0.02 |         - |          NA |
| New.GetSpan+Advance(chunked) | 65536      | 256       | 14,644.6 ns |  1.62 |    0.02 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **65536**      | **4096**      |  **5,264.5 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| New.Write(chunked)           | 65536      | 4096      |  5,219.9 ns |  0.99 |    0.01 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 65536      | 4096      |  6,023.2 ns |  1.14 |    0.02 |         - |          NA |
| New.GetSpan+Advance(chunked) | 65536      | 4096      |  5,882.9 ns |  1.12 |    0.02 |         - |          NA |
