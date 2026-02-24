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
| **Old.Write(chunked)**           | **4096**       | **16**        |  **4,152.1 ns** |  **1.00** |    **0.04** |         **-** |          **NA** |
| New.Write(chunked)           | 4096       | 16        |  4,334.8 ns |  1.04 |    0.03 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 4096       | 16        |  8,657.9 ns |  2.09 |    0.06 |         - |          NA |
| New.GetSpan+Advance(chunked) | 4096       | 16        |  8,745.8 ns |  2.11 |    0.07 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **4096**       | **256**       |    **660.5 ns** |  **1.00** |    **0.07** |         **-** |          **NA** |
| New.Write(chunked)           | 4096       | 256       |    736.8 ns |  1.12 |    0.06 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 4096       | 256       |  1,257.7 ns |  1.91 |    0.10 |         - |          NA |
| New.GetSpan+Advance(chunked) | 4096       | 256       |    921.3 ns |  1.40 |    0.07 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **4096**       | **4096**      |    **559.0 ns** |  **1.03** |    **0.25** |         **-** |          **NA** |
| New.Write(chunked)           | 4096       | 4096      |    361.1 ns |  0.67 |    0.13 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 4096       | 4096      |    419.9 ns |  0.77 |    0.15 |         - |          NA |
| New.GetSpan+Advance(chunked) | 4096       | 4096      |    425.1 ns |  0.78 |    0.16 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **65536**      | **16**        | **21,510.1 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| New.Write(chunked)           | 65536      | 16        | 21,557.3 ns |  1.00 |    0.02 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 65536      | 16        | 15,634.6 ns |  0.73 |    0.01 |         - |          NA |
| New.GetSpan+Advance(chunked) | 65536      | 16        | 15,525.2 ns |  0.72 |    0.01 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **65536**      | **256**       |  **8,933.3 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| New.Write(chunked)           | 65536      | 256       |  9,202.4 ns |  1.03 |    0.02 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 65536      | 256       | 14,347.5 ns |  1.61 |    0.02 |         - |          NA |
| New.GetSpan+Advance(chunked) | 65536      | 256       | 14,404.8 ns |  1.61 |    0.03 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **65536**      | **4096**      |  **5,460.6 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| New.Write(chunked)           | 65536      | 4096      |  5,419.0 ns |  0.99 |    0.03 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 65536      | 4096      |  6,180.5 ns |  1.13 |    0.03 |         - |          NA |
| New.GetSpan+Advance(chunked) | 65536      | 4096      |  5,959.5 ns |  1.09 |    0.03 |         - |          NA |
