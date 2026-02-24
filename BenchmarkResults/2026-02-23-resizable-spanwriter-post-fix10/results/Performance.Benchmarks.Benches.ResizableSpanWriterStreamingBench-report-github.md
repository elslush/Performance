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
| **Old.Write(chunked)**           | **4096**       | **16**        |  **4,291.7 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| New.Write(chunked)           | 4096       | 16        |  3,874.8 ns |  0.90 |    0.02 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 4096       | 16        |  8,811.2 ns |  2.05 |    0.04 |         - |          NA |
| New.GetSpan+Advance(chunked) | 4096       | 16        |  8,965.1 ns |  2.09 |    0.06 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **4096**       | **256**       |    **689.3 ns** |  **1.00** |    **0.08** |         **-** |          **NA** |
| New.Write(chunked)           | 4096       | 256       |    686.5 ns |  1.00 |    0.07 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 4096       | 256       |    941.3 ns |  1.37 |    0.09 |         - |          NA |
| New.GetSpan+Advance(chunked) | 4096       | 256       |    956.0 ns |  1.39 |    0.08 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **4096**       | **4096**      |    **601.1 ns** |  **1.04** |    **0.29** |         **-** |          **NA** |
| New.Write(chunked)           | 4096       | 4096      |    370.4 ns |  0.64 |    0.14 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 4096       | 4096      |    378.8 ns |  0.65 |    0.15 |         - |          NA |
| New.GetSpan+Advance(chunked) | 4096       | 4096      |    482.2 ns |  0.83 |    0.21 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **65536**      | **16**        | **24,926.7 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| New.Write(chunked)           | 65536      | 16        | 23,647.0 ns |  0.95 |    0.01 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 65536      | 16        | 25,302.1 ns |  1.02 |    0.01 |         - |          NA |
| New.GetSpan+Advance(chunked) | 65536      | 16        | 33,808.5 ns |  1.36 |    0.01 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **65536**      | **256**       |  **9,741.1 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| New.Write(chunked)           | 65536      | 256       |  9,250.3 ns |  0.95 |    0.02 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 65536      | 256       | 13,905.8 ns |  1.43 |    0.02 |         - |          NA |
| New.GetSpan+Advance(chunked) | 65536      | 256       | 14,046.9 ns |  1.44 |    0.02 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **65536**      | **4096**      |  **5,439.4 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| New.Write(chunked)           | 65536      | 4096      |  5,231.7 ns |  0.96 |    0.02 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 65536      | 4096      |  6,082.1 ns |  1.12 |    0.02 |         - |          NA |
| New.GetSpan+Advance(chunked) | 65536      | 4096      |  6,195.8 ns |  1.14 |    0.03 |         - |          NA |
