```

BenchmarkDotNet v0.15.3, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
INTEL XEON GOLD 6548N 0.80GHz, 2 CPU, 128 logical and 64 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v4
  Job-CNUJVU : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                       | TotalItems | ChunkSize | Mean         | Ratio | RatioSD | Allocated | Alloc Ratio |
|----------------------------- |----------- |---------- |-------------:|------:|--------:|----------:|------------:|
| **Old.Write(chunked)**           | **4096**       | **16**        |   **5,468.8 ns** |  **1.01** |    **0.15** |         **-** |          **NA** |
| New.Write(chunked)           | 4096       | 16        |   5,395.2 ns |  1.00 |    0.10 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 4096       | 16        |  10,320.4 ns |  1.91 |    0.20 |         - |          NA |
| New.GetSpan+Advance(chunked) | 4096       | 16        |  11,492.3 ns |  2.12 |    0.22 |         - |          NA |
|                              |            |           |              |       |         |           |             |
| **Old.Write(chunked)**           | **4096**       | **256**       |     **657.5 ns** |  **1.00** |    **0.06** |         **-** |          **NA** |
| New.Write(chunked)           | 4096       | 256       |     663.3 ns |  1.01 |    0.05 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 4096       | 256       |     947.4 ns |  1.44 |    0.06 |         - |          NA |
| New.GetSpan+Advance(chunked) | 4096       | 256       |     984.0 ns |  1.50 |    0.07 |         - |          NA |
|                              |            |           |              |       |         |           |             |
| **Old.Write(chunked)**           | **4096**       | **4096**      |     **332.1 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| New.Write(chunked)           | 4096       | 4096      |     409.0 ns |  1.23 |    0.12 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 4096       | 4096      |     377.1 ns |  1.14 |    0.05 |         - |          NA |
| New.GetSpan+Advance(chunked) | 4096       | 4096      |     354.0 ns |  1.07 |    0.05 |         - |          NA |
|                              |            |           |              |       |         |           |             |
| **Old.Write(chunked)**           | **65536**      | **16**        |  **38,994.6 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| New.Write(chunked)           | 65536      | 16        | 353,833.0 ns |  9.07 |    0.14 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 65536      | 16        |  56,934.7 ns |  1.46 |    0.01 |         - |          NA |
| New.GetSpan+Advance(chunked) | 65536      | 16        | 388,226.3 ns |  9.96 |    0.40 |         - |          NA |
|                              |            |           |              |       |         |           |             |
| **Old.Write(chunked)**           | **65536**      | **256**       |   **9,737.2 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| New.Write(chunked)           | 65536      | 256       |   9,420.1 ns |  0.97 |    0.02 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 65536      | 256       |  15,048.6 ns |  1.55 |    0.03 |         - |          NA |
| New.GetSpan+Advance(chunked) | 65536      | 256       |  15,402.6 ns |  1.58 |    0.02 |         - |          NA |
|                              |            |           |              |       |         |           |             |
| **Old.Write(chunked)**           | **65536**      | **4096**      |   **6,514.8 ns** |  **1.00** |    **0.10** |         **-** |          **NA** |
| New.Write(chunked)           | 65536      | 4096      |   5,254.5 ns |  0.81 |    0.05 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 65536      | 4096      |   6,046.0 ns |  0.93 |    0.07 |         - |          NA |
| New.GetSpan+Advance(chunked) | 65536      | 4096      |   5,973.5 ns |  0.92 |    0.06 |         - |          NA |
