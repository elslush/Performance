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
| **Old.Write(chunked)**           | **4096**       | **16**        |  **4,209.1 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| New.Write(chunked)           | 4096       | 16        |  4,450.2 ns |  1.06 |    0.02 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 4096       | 16        |  8,614.7 ns |  2.05 |    0.03 |         - |          NA |
| New.GetSpan+Advance(chunked) | 4096       | 16        |  9,255.2 ns |  2.20 |    0.05 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **4096**       | **256**       |    **639.8 ns** |  **1.00** |    **0.05** |         **-** |          **NA** |
| New.Write(chunked)           | 4096       | 256       |    760.4 ns |  1.19 |    0.06 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 4096       | 256       |    944.1 ns |  1.48 |    0.06 |         - |          NA |
| New.GetSpan+Advance(chunked) | 4096       | 256       |    920.4 ns |  1.44 |    0.06 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **4096**       | **4096**      |    **526.3 ns** |  **1.04** |    **0.31** |         **-** |          **NA** |
| New.Write(chunked)           | 4096       | 4096      |    394.1 ns |  0.78 |    0.18 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 4096       | 4096      |    388.9 ns |  0.77 |    0.17 |         - |          NA |
| New.GetSpan+Advance(chunked) | 4096       | 4096      |    396.7 ns |  0.79 |    0.17 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **65536**      | **16**        | **30,000.8 ns** |  **1.00** |    **0.00** |         **-** |          **NA** |
| New.Write(chunked)           | 65536      | 16        | 27,800.2 ns |  0.93 |    0.00 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 65536      | 16        | 45,867.6 ns |  1.53 |    0.01 |         - |          NA |
| New.GetSpan+Advance(chunked) | 65536      | 16        | 34,680.5 ns |  1.16 |    0.01 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **65536**      | **256**       |  **9,205.8 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| New.Write(chunked)           | 65536      | 256       |  9,392.9 ns |  1.02 |    0.02 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 65536      | 256       | 14,253.7 ns |  1.55 |    0.02 |         - |          NA |
| New.GetSpan+Advance(chunked) | 65536      | 256       | 15,048.4 ns |  1.63 |    0.03 |         - |          NA |
|                              |            |           |             |       |         |           |             |
| **Old.Write(chunked)**           | **65536**      | **4096**      |  **5,325.6 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| New.Write(chunked)           | 65536      | 4096      |  5,269.3 ns |  0.99 |    0.02 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 65536      | 4096      |  6,567.9 ns |  1.23 |    0.02 |         - |          NA |
| New.GetSpan+Advance(chunked) | 65536      | 4096      |  8,589.4 ns |  1.61 |    0.22 |         - |          NA |
