```

BenchmarkDotNet v0.15.3, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
INTEL XEON GOLD 6548N 0.80GHz, 2 CPU, 128 logical and 64 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v4
  Job-KBXOHV : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v4

InvocationCount=1  IterationCount=50  UnrollFactor=1  
WarmupCount=50  

```
| Method             | TotalItems | ChunkSize | Mean         | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |---------- |-------------:|------:|--------:|----------:|------------:|
| **Old.Write(chunked)** | **4096**       | **16**        |   **5,094.6 ns** |  **1.01** |    **0.10** |         **-** |          **NA** |
| New.Write(chunked) | 4096       | 16        |   5,341.8 ns |  1.05 |    0.11 |         - |          NA |
|                    |            |           |              |       |         |           |             |
| **Old.Write(chunked)** | **4096**       | **256**       |     **621.5 ns** |  **1.00** |    **0.04** |         **-** |          **NA** |
| New.Write(chunked) | 4096       | 256       |     691.8 ns |  1.11 |    0.08 |         - |          NA |
|                    |            |           |              |       |         |           |             |
| **Old.Write(chunked)** | **4096**       | **4096**      |     **344.1 ns** |  **1.00** |    **0.05** |         **-** |          **NA** |
| New.Write(chunked) | 4096       | 4096      |     385.6 ns |  1.12 |    0.07 |         - |          NA |
|                    |            |           |              |       |         |           |             |
| **Old.Write(chunked)** | **65536**      | **16**        |  **37,087.1 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| New.Write(chunked) | 65536      | 16        | 347,713.8 ns |  9.38 |    0.08 |         - |          NA |
|                    |            |           |              |       |         |           |             |
| **Old.Write(chunked)** | **65536**      | **256**       |   **9,623.7 ns** |  **1.00** |    **0.04** |         **-** |          **NA** |
| New.Write(chunked) | 65536      | 256       |   9,653.6 ns |  1.00 |    0.05 |         - |          NA |
|                    |            |           |              |       |         |           |             |
| **Old.Write(chunked)** | **65536**      | **4096**      |   **5,291.7 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| New.Write(chunked) | 65536      | 4096      |   5,475.2 ns |  1.03 |    0.02 |         - |          NA |
