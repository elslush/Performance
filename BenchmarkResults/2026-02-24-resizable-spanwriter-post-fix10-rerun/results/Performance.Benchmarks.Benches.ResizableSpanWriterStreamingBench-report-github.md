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
| **Old.Write(chunked)**           | **4096**       | **16**        |  **4,153.2 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| New.Write(chunked)           | 4096       | 16        |  3,843.6 ns |  0.93 |    0.02 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 4096       | 16        |  8,691.5 ns |  2.09 |    0.04 |         - |          NA |
| New.GetSpan+Advance(chunked) | 4096       | 16        |  9,977.6 ns |  2.40 |    0.05 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **4096**       | **256**       |    **668.4 ns** |  **1.00** |    **0.06** |         **-** |          **NA** |
| New.Write(chunked)           | 4096       | 256       |    877.7 ns |  1.32 |    0.18 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 4096       | 256       |  1,013.8 ns |  1.52 |    0.07 |         - |          NA |
| New.GetSpan+Advance(chunked) | 4096       | 256       |  1,026.8 ns |  1.54 |    0.08 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **4096**       | **4096**      |    **471.4 ns** |  **1.04** |    **0.31** |         **-** |          **NA** |
| New.Write(chunked)           | 4096       | 4096      |    367.5 ns |  0.81 |    0.16 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 4096       | 4096      |    451.2 ns |  1.00 |    0.25 |         - |          NA |
| New.GetSpan+Advance(chunked) | 4096       | 4096      |    459.4 ns |  1.02 |    0.22 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **65536**      | **16**        | **24,889.2 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| New.Write(chunked)           | 65536      | 16        | 29,085.3 ns |  1.17 |    0.01 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 65536      | 16        | 35,070.3 ns |  1.41 |    0.01 |         - |          NA |
| New.GetSpan+Advance(chunked) | 65536      | 16        | 45,255.1 ns |  1.82 |    0.01 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **65536**      | **256**       |  **8,981.4 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| New.Write(chunked)           | 65536      | 256       |  9,012.9 ns |  1.00 |    0.02 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 65536      | 256       | 13,911.4 ns |  1.55 |    0.03 |         - |          NA |
| New.GetSpan+Advance(chunked) | 65536      | 256       | 14,232.5 ns |  1.59 |    0.03 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **65536**      | **4096**      |  **5,361.4 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| New.Write(chunked)           | 65536      | 4096      |  5,323.7 ns |  0.99 |    0.02 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 65536      | 4096      |  6,138.0 ns |  1.15 |    0.03 |         - |          NA |
| New.GetSpan+Advance(chunked) | 65536      | 4096      |  5,874.4 ns |  1.10 |    0.02 |         - |          NA |
