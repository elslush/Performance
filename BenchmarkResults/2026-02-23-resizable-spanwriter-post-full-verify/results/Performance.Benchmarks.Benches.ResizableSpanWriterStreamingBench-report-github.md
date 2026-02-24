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
| **Old.Write(chunked)**           | **4096**       | **16**        |  **4,264.8 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| New.Write(chunked)           | 4096       | 16        |  4,468.4 ns |  1.05 |    0.03 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 4096       | 16        |  8,769.3 ns |  2.06 |    0.04 |         - |          NA |
| New.GetSpan+Advance(chunked) | 4096       | 16        |  9,420.2 ns |  2.21 |    0.04 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **4096**       | **256**       |    **763.5 ns** |  **1.01** |    **0.12** |         **-** |          **NA** |
| New.Write(chunked)           | 4096       | 256       |    680.6 ns |  0.90 |    0.10 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 4096       | 256       |    962.1 ns |  1.27 |    0.11 |         - |          NA |
| New.GetSpan+Advance(chunked) | 4096       | 256       |  1,024.1 ns |  1.35 |    0.12 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **4096**       | **4096**      |    **508.7 ns** |  **1.05** |    **0.32** |         **-** |          **NA** |
| New.Write(chunked)           | 4096       | 4096      |    467.6 ns |  0.96 |    0.21 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 4096       | 4096      |    431.9 ns |  0.89 |    0.25 |         - |          NA |
| New.GetSpan+Advance(chunked) | 4096       | 4096      |    392.5 ns |  0.81 |    0.18 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **65536**      | **16**        | **31,002.8 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| New.Write(chunked)           | 65536      | 16        | 24,051.2 ns |  0.78 |    0.01 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 65536      | 16        | 35,228.3 ns |  1.14 |    0.01 |         - |          NA |
| New.GetSpan+Advance(chunked) | 65536      | 16        | 43,899.0 ns |  1.42 |    0.01 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **65536**      | **256**       |  **9,002.2 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| New.Write(chunked)           | 65536      | 256       |  9,403.1 ns |  1.04 |    0.02 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 65536      | 256       | 14,161.3 ns |  1.57 |    0.02 |         - |          NA |
| New.GetSpan+Advance(chunked) | 65536      | 256       | 14,475.2 ns |  1.61 |    0.02 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **65536**      | **4096**      |  **6,193.1 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| New.Write(chunked)           | 65536      | 4096      |  5,269.6 ns |  0.85 |    0.02 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 65536      | 4096      |  6,067.8 ns |  0.98 |    0.02 |         - |          NA |
| New.GetSpan+Advance(chunked) | 65536      | 4096      |  5,893.0 ns |  0.95 |    0.02 |         - |          NA |
