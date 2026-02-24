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
| **Old.Write(chunked)**           | **4096**       | **16**        |  **4,186.0 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| New.Write(chunked)           | 4096       | 16        |  4,568.8 ns |  1.09 |    0.02 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 4096       | 16        |  9,020.3 ns |  2.16 |    0.03 |         - |          NA |
| New.GetSpan+Advance(chunked) | 4096       | 16        |  9,172.8 ns |  2.19 |    0.05 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **4096**       | **256**       |    **700.0 ns** |  **1.00** |    **0.07** |         **-** |          **NA** |
| New.Write(chunked)           | 4096       | 256       |    658.9 ns |  0.94 |    0.06 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 4096       | 256       |    976.4 ns |  1.40 |    0.07 |         - |          NA |
| New.GetSpan+Advance(chunked) | 4096       | 256       |    985.5 ns |  1.41 |    0.07 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **4096**       | **4096**      |    **451.2 ns** |  **1.04** |    **0.29** |         **-** |          **NA** |
| New.Write(chunked)           | 4096       | 4096      |    390.1 ns |  0.90 |    0.18 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 4096       | 4096      |    394.3 ns |  0.91 |    0.17 |         - |          NA |
| New.GetSpan+Advance(chunked) | 4096       | 4096      |    428.2 ns |  0.99 |    0.21 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **65536**      | **16**        | **14,776.1 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| New.Write(chunked)           | 65536      | 16        | 27,578.2 ns |  1.87 |    0.02 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 65536      | 16        | 45,783.1 ns |  3.10 |    0.03 |         - |          NA |
| New.GetSpan+Advance(chunked) | 65536      | 16        | 25,588.8 ns |  1.73 |    0.02 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **65536**      | **256**       |  **9,586.5 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| New.Write(chunked)           | 65536      | 256       |  8,774.2 ns |  0.92 |    0.02 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 65536      | 256       | 14,276.4 ns |  1.49 |    0.02 |         - |          NA |
| New.GetSpan+Advance(chunked) | 65536      | 256       | 14,836.4 ns |  1.55 |    0.02 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **65536**      | **4096**      |  **5,804.9 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| New.Write(chunked)           | 65536      | 4096      |  5,270.4 ns |  0.91 |    0.02 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 65536      | 4096      |  5,959.1 ns |  1.03 |    0.02 |         - |          NA |
| New.GetSpan+Advance(chunked) | 65536      | 4096      |  5,782.8 ns |  1.00 |    0.02 |         - |          NA |
