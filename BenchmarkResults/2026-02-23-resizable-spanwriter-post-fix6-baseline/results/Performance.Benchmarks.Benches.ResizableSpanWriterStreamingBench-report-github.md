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
| **Old.Write(chunked)**           | **4096**       | **16**        |  **4,150.9 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| New.Write(chunked)           | 4096       | 16        |  4,470.8 ns |  1.08 |    0.03 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 4096       | 16        |  8,934.9 ns |  2.15 |    0.05 |         - |          NA |
| New.GetSpan+Advance(chunked) | 4096       | 16        |  9,084.1 ns |  2.19 |    0.05 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **4096**       | **256**       |    **660.6 ns** |  **1.00** |    **0.06** |         **-** |          **NA** |
| New.Write(chunked)           | 4096       | 256       |    715.1 ns |  1.08 |    0.06 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 4096       | 256       |    987.2 ns |  1.50 |    0.08 |         - |          NA |
| New.GetSpan+Advance(chunked) | 4096       | 256       |  1,007.1 ns |  1.53 |    0.07 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **4096**       | **4096**      |    **370.4 ns** |  **1.01** |    **0.10** |         **-** |          **NA** |
| New.Write(chunked)           | 4096       | 4096      |    386.4 ns |  1.05 |    0.11 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 4096       | 4096      |    423.7 ns |  1.15 |    0.18 |         - |          NA |
| New.GetSpan+Advance(chunked) | 4096       | 4096      |    387.1 ns |  1.05 |    0.10 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **65536**      | **16**        | **25,426.2 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| New.Write(chunked)           | 65536      | 16        | 21,703.4 ns |  0.85 |    0.01 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 65536      | 16        | 24,837.1 ns |  0.98 |    0.00 |         - |          NA |
| New.GetSpan+Advance(chunked) | 65536      | 16        | 34,028.4 ns |  1.34 |    0.01 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **65536**      | **256**       |  **9,741.7 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| New.Write(chunked)           | 65536      | 256       |  8,889.5 ns |  0.91 |    0.02 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 65536      | 256       | 14,089.0 ns |  1.45 |    0.02 |         - |          NA |
| New.GetSpan+Advance(chunked) | 65536      | 256       | 14,087.6 ns |  1.45 |    0.03 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **65536**      | **4096**      |  **5,495.4 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| New.Write(chunked)           | 65536      | 4096      |  6,177.5 ns |  1.12 |    0.04 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 65536      | 4096      |  6,438.6 ns |  1.17 |    0.03 |         - |          NA |
| New.GetSpan+Advance(chunked) | 65536      | 4096      |  6,147.0 ns |  1.12 |    0.04 |         - |          NA |
