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
| **Old.Write(chunked)**           | **4096**       | **16**        |  **4,124.4 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| New.Write(chunked)           | 4096       | 16        |  4,379.3 ns |  1.06 |    0.03 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 4096       | 16        |  8,462.2 ns |  2.05 |    0.05 |         - |          NA |
| New.GetSpan+Advance(chunked) | 4096       | 16        |  7,947.6 ns |  1.93 |    0.07 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **4096**       | **256**       |    **649.2 ns** |  **1.00** |    **0.06** |         **-** |          **NA** |
| New.Write(chunked)           | 4096       | 256       |    662.9 ns |  1.02 |    0.06 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 4096       | 256       |    946.9 ns |  1.46 |    0.06 |         - |          NA |
| New.GetSpan+Advance(chunked) | 4096       | 256       |    899.7 ns |  1.39 |    0.06 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **4096**       | **4096**      |    **381.5 ns** |  **1.01** |    **0.12** |         **-** |          **NA** |
| New.Write(chunked)           | 4096       | 4096      |    421.6 ns |  1.11 |    0.14 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 4096       | 4096      |    402.6 ns |  1.06 |    0.09 |         - |          NA |
| New.GetSpan+Advance(chunked) | 4096       | 4096      |    368.6 ns |  0.97 |    0.08 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **65536**      | **16**        | **24,715.6 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| New.Write(chunked)           | 65536      | 16        | 24,401.2 ns |  0.99 |    0.00 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 65536      | 16        | 24,749.0 ns |  1.00 |    0.01 |         - |          NA |
| New.GetSpan+Advance(chunked) | 65536      | 16        | 43,675.1 ns |  1.77 |    0.01 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **65536**      | **256**       |  **9,334.0 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| New.Write(chunked)           | 65536      | 256       |  9,794.8 ns |  1.05 |    0.04 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 65536      | 256       | 13,541.6 ns |  1.45 |    0.02 |         - |          NA |
| New.GetSpan+Advance(chunked) | 65536      | 256       | 13,177.2 ns |  1.41 |    0.03 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **65536**      | **4096**      |  **5,415.3 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| New.Write(chunked)           | 65536      | 4096      |  5,296.2 ns |  0.98 |    0.02 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 65536      | 4096      |  6,200.3 ns |  1.15 |    0.03 |         - |          NA |
| New.GetSpan+Advance(chunked) | 65536      | 4096      |  5,763.3 ns |  1.06 |    0.02 |         - |          NA |
