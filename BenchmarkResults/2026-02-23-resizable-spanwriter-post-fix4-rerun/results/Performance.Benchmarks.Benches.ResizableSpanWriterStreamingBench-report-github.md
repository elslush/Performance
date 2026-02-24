```

BenchmarkDotNet v0.15.3, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
INTEL XEON GOLD 6548N 0.79GHz, 2 CPU, 128 logical and 64 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v4
  Job-CNUJVU : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                       | TotalItems | ChunkSize | Mean        | Ratio | RatioSD | Allocated | Alloc Ratio |
|----------------------------- |----------- |---------- |------------:|------:|--------:|----------:|------------:|
| **Old.Write(chunked)**           | **4096**       | **16**        |  **4,241.7 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| New.Write(chunked)           | 4096       | 16        |  4,462.8 ns |  1.05 |    0.02 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 4096       | 16        |  8,822.9 ns |  2.08 |    0.04 |         - |          NA |
| New.GetSpan+Advance(chunked) | 4096       | 16        |  9,904.6 ns |  2.34 |    0.07 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **4096**       | **256**       |    **644.6 ns** |  **1.00** |    **0.06** |         **-** |          **NA** |
| New.Write(chunked)           | 4096       | 256       |    681.3 ns |  1.06 |    0.07 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 4096       | 256       |    975.6 ns |  1.52 |    0.07 |         - |          NA |
| New.GetSpan+Advance(chunked) | 4096       | 256       |  1,003.0 ns |  1.56 |    0.08 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **4096**       | **4096**      |    **571.9 ns** |  **1.04** |    **0.28** |         **-** |          **NA** |
| New.Write(chunked)           | 4096       | 4096      |    386.5 ns |  0.70 |    0.16 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 4096       | 4096      |    412.7 ns |  0.75 |    0.18 |         - |          NA |
| New.GetSpan+Advance(chunked) | 4096       | 4096      |    404.9 ns |  0.73 |    0.17 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **65536**      | **16**        | **21,293.9 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| New.Write(chunked)           | 65536      | 16        | 29,466.8 ns |  1.38 |    0.02 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 65536      | 16        | 39,097.2 ns |  1.84 |    0.02 |         - |          NA |
| New.GetSpan+Advance(chunked) | 65536      | 16        | 15,887.0 ns |  0.75 |    0.01 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **65536**      | **256**       |  **9,189.2 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| New.Write(chunked)           | 65536      | 256       |  8,855.6 ns |  0.96 |    0.02 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 65536      | 256       | 14,022.5 ns |  1.53 |    0.03 |         - |          NA |
| New.GetSpan+Advance(chunked) | 65536      | 256       | 14,620.2 ns |  1.59 |    0.03 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **65536**      | **4096**      |  **5,658.5 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| New.Write(chunked)           | 65536      | 4096      |  5,526.9 ns |  0.98 |    0.04 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 65536      | 4096      |  6,264.1 ns |  1.11 |    0.02 |         - |          NA |
| New.GetSpan+Advance(chunked) | 65536      | 4096      |  5,876.8 ns |  1.04 |    0.02 |         - |          NA |
