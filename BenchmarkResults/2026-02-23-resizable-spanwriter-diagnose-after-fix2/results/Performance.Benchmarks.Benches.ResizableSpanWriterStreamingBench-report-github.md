```

BenchmarkDotNet v0.15.3, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
INTEL XEON GOLD 6548N 0.76GHz, 2 CPU, 128 logical and 64 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v4
  Job-CNUJVU : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                       | TotalItems | ChunkSize | Mean        | Ratio | RatioSD | Allocated | Alloc Ratio |
|----------------------------- |----------- |---------- |------------:|------:|--------:|----------:|------------:|
| **Old.Write(chunked)**           | **4096**       | **16**        |  **4,154.1 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| New.Write(chunked)           | 4096       | 16        |  4,711.7 ns |  1.13 |    0.03 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 4096       | 16        |  8,734.7 ns |  2.10 |    0.03 |         - |          NA |
| New.GetSpan+Advance(chunked) | 4096       | 16        |  9,566.2 ns |  2.30 |    0.05 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **4096**       | **256**       |    **682.2 ns** |  **1.00** |    **0.06** |         **-** |          **NA** |
| New.Write(chunked)           | 4096       | 256       |    719.6 ns |  1.06 |    0.07 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 4096       | 256       |  1,028.0 ns |  1.51 |    0.08 |         - |          NA |
| New.GetSpan+Advance(chunked) | 4096       | 256       |  1,013.0 ns |  1.49 |    0.07 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **4096**       | **4096**      |    **485.9 ns** |  **1.05** |    **0.32** |         **-** |          **NA** |
| New.Write(chunked)           | 4096       | 4096      |    467.9 ns |  1.01 |    0.22 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 4096       | 4096      |    462.5 ns |  1.00 |    0.29 |         - |          NA |
| New.GetSpan+Advance(chunked) | 4096       | 4096      |    387.2 ns |  0.83 |    0.18 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **65536**      | **16**        | **14,704.3 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| New.Write(chunked)           | 65536      | 16        | 14,076.5 ns |  0.96 |    0.01 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 65536      | 16        | 34,800.9 ns |  2.37 |    0.02 |         - |          NA |
| New.GetSpan+Advance(chunked) | 65536      | 16        | 33,468.7 ns |  2.28 |    0.02 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **65536**      | **256**       |  **9,957.3 ns** |  **1.00** |    **0.05** |         **-** |          **NA** |
| New.Write(chunked)           | 65536      | 256       |  9,183.8 ns |  0.92 |    0.03 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 65536      | 256       | 14,082.3 ns |  1.42 |    0.05 |         - |          NA |
| New.GetSpan+Advance(chunked) | 65536      | 256       | 14,934.8 ns |  1.50 |    0.06 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **65536**      | **4096**      |  **5,417.4 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| New.Write(chunked)           | 65536      | 4096      |  5,288.9 ns |  0.98 |    0.02 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 65536      | 4096      |  6,167.4 ns |  1.14 |    0.02 |         - |          NA |
| New.GetSpan+Advance(chunked) | 65536      | 4096      |  5,829.5 ns |  1.08 |    0.02 |         - |          NA |
