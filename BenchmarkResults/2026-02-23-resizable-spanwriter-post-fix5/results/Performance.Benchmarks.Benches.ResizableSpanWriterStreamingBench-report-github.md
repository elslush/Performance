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
| **Old.Write(chunked)**           | **4096**       | **16**        |  **4,293.6 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| New.Write(chunked)           | 4096       | 16        |  4,317.2 ns |  1.01 |    0.02 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 4096       | 16        |  8,747.9 ns |  2.04 |    0.04 |         - |          NA |
| New.GetSpan+Advance(chunked) | 4096       | 16        |  8,935.0 ns |  2.08 |    0.04 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **4096**       | **256**       |    **670.7 ns** |  **1.00** |    **0.06** |         **-** |          **NA** |
| New.Write(chunked)           | 4096       | 256       |    685.0 ns |  1.02 |    0.07 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 4096       | 256       |    997.1 ns |  1.49 |    0.07 |         - |          NA |
| New.GetSpan+Advance(chunked) | 4096       | 256       |    991.5 ns |  1.48 |    0.07 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **4096**       | **4096**      |    **581.4 ns** |  **1.03** |    **0.26** |         **-** |          **NA** |
| New.Write(chunked)           | 4096       | 4096      |    382.4 ns |  0.68 |    0.14 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 4096       | 4096      |    497.9 ns |  0.88 |    0.25 |         - |          NA |
| New.GetSpan+Advance(chunked) | 4096       | 4096      |    390.1 ns |  0.69 |    0.15 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **65536**      | **16**        | **29,983.2 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| New.Write(chunked)           | 65536      | 16        | 20,851.3 ns |  0.70 |    0.00 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 65536      | 16        | 25,267.0 ns |  0.84 |    0.01 |         - |          NA |
| New.GetSpan+Advance(chunked) | 65536      | 16        | 44,204.2 ns |  1.47 |    0.01 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **65536**      | **256**       |  **9,110.9 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| New.Write(chunked)           | 65536      | 256       |  9,468.0 ns |  1.04 |    0.02 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 65536      | 256       | 14,775.1 ns |  1.62 |    0.02 |         - |          NA |
| New.GetSpan+Advance(chunked) | 65536      | 256       | 14,836.7 ns |  1.63 |    0.03 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **65536**      | **4096**      |  **5,342.0 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| New.Write(chunked)           | 65536      | 4096      |  5,268.5 ns |  0.99 |    0.02 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 65536      | 4096      |  6,132.5 ns |  1.15 |    0.03 |         - |          NA |
| New.GetSpan+Advance(chunked) | 65536      | 4096      |  6,500.4 ns |  1.22 |    0.04 |         - |          NA |
