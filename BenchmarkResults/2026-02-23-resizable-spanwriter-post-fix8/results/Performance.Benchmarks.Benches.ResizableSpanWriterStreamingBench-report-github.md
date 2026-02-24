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
| **Old.Write(chunked)**           | **4096**       | **16**        |  **4,203.0 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| New.Write(chunked)           | 4096       | 16        |  4,465.2 ns |  1.06 |    0.03 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 4096       | 16        |  8,112.1 ns |  1.93 |    0.04 |         - |          NA |
| New.GetSpan+Advance(chunked) | 4096       | 16        |  9,973.6 ns |  2.37 |    0.06 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **4096**       | **256**       |    **708.4 ns** |  **1.00** |    **0.06** |         **-** |          **NA** |
| New.Write(chunked)           | 4096       | 256       |    698.9 ns |  0.99 |    0.06 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 4096       | 256       |  1,055.3 ns |  1.49 |    0.06 |         - |          NA |
| New.GetSpan+Advance(chunked) | 4096       | 256       |  1,048.4 ns |  1.48 |    0.06 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **4096**       | **4096**      |    **486.2 ns** |  **1.05** |    **0.33** |         **-** |          **NA** |
| New.Write(chunked)           | 4096       | 4096      |    388.4 ns |  0.84 |    0.19 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 4096       | 4096      |    439.4 ns |  0.95 |    0.23 |         - |          NA |
| New.GetSpan+Advance(chunked) | 4096       | 4096      |    525.4 ns |  1.13 |    0.28 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **65536**      | **16**        | **14,518.5 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| New.Write(chunked)           | 65536      | 16        | 28,001.7 ns |  1.93 |    0.02 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 65536      | 16        | 16,453.1 ns |  1.13 |    0.04 |         - |          NA |
| New.GetSpan+Advance(chunked) | 65536      | 16        | 42,585.6 ns |  2.93 |    0.02 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **65536**      | **256**       |  **9,507.4 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| New.Write(chunked)           | 65536      | 256       |  8,823.0 ns |  0.93 |    0.02 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 65536      | 256       | 13,642.1 ns |  1.44 |    0.03 |         - |          NA |
| New.GetSpan+Advance(chunked) | 65536      | 256       | 13,822.8 ns |  1.45 |    0.03 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **65536**      | **4096**      |  **5,342.5 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| New.Write(chunked)           | 65536      | 4096      |  5,701.3 ns |  1.07 |    0.03 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 65536      | 4096      |  6,305.0 ns |  1.18 |    0.04 |         - |          NA |
| New.GetSpan+Advance(chunked) | 65536      | 4096      |  6,081.3 ns |  1.14 |    0.03 |         - |          NA |
