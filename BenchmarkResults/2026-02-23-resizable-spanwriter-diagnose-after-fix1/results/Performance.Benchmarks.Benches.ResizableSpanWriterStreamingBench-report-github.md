```

BenchmarkDotNet v0.15.3, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
INTEL XEON GOLD 6548N 0.80GHz, 2 CPU, 128 logical and 64 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v4
  Job-CNUJVU : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                       | TotalItems | ChunkSize | Mean         | Ratio | RatioSD | Allocated | Alloc Ratio |
|----------------------------- |----------- |---------- |-------------:|------:|--------:|----------:|------------:|
| **Old.Write(chunked)**           | **4096**       | **16**        |   **4,158.0 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| New.Write(chunked)           | 4096       | 16        |   5,546.4 ns |  1.33 |    0.14 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 4096       | 16        |  10,715.5 ns |  2.58 |    0.03 |         - |          NA |
| New.GetSpan+Advance(chunked) | 4096       | 16        |  10,352.0 ns |  2.49 |    0.04 |         - |          NA |
|                              |            |           |              |       |         |           |             |
| **Old.Write(chunked)**           | **4096**       | **256**       |     **624.2 ns** |  **1.00** |    **0.05** |         **-** |          **NA** |
| New.Write(chunked)           | 4096       | 256       |     699.9 ns |  1.12 |    0.12 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 4096       | 256       |     893.7 ns |  1.43 |    0.06 |         - |          NA |
| New.GetSpan+Advance(chunked) | 4096       | 256       |   1,184.6 ns |  1.90 |    0.12 |         - |          NA |
|                              |            |           |              |       |         |           |             |
| **Old.Write(chunked)**           | **4096**       | **4096**      |     **369.2 ns** |  **1.00** |    **0.05** |         **-** |          **NA** |
| New.Write(chunked)           | 4096       | 4096      |     365.8 ns |  0.99 |    0.06 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 4096       | 4096      |     434.1 ns |  1.18 |    0.15 |         - |          NA |
| New.GetSpan+Advance(chunked) | 4096       | 4096      |     362.1 ns |  0.98 |    0.06 |         - |          NA |
|                              |            |           |              |       |         |           |             |
| **Old.Write(chunked)**           | **65536**      | **16**        |  **38,591.6 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| New.Write(chunked)           | 65536      | 16        | 348,965.2 ns |  9.04 |    0.10 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 65536      | 16        |  56,413.7 ns |  1.46 |    0.02 |         - |          NA |
| New.GetSpan+Advance(chunked) | 65536      | 16        | 378,805.8 ns |  9.82 |    0.13 |         - |          NA |
|                              |            |           |              |       |         |           |             |
| **Old.Write(chunked)**           | **65536**      | **256**       |   **9,388.2 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| New.Write(chunked)           | 65536      | 256       |  11,304.7 ns |  1.20 |    0.02 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 65536      | 256       |  14,289.7 ns |  1.52 |    0.02 |         - |          NA |
| New.GetSpan+Advance(chunked) | 65536      | 256       |  16,357.0 ns |  1.74 |    0.14 |         - |          NA |
|                              |            |           |              |       |         |           |             |
| **Old.Write(chunked)**           | **65536**      | **4096**      |   **5,665.3 ns** |  **1.00** |    **0.05** |         **-** |          **NA** |
| New.Write(chunked)           | 65536      | 4096      |   5,711.8 ns |  1.01 |    0.04 |         - |          NA |
| Old.GetSpan+Advance(chunked) | 65536      | 4096      |   6,058.8 ns |  1.07 |    0.04 |         - |          NA |
| New.GetSpan+Advance(chunked) | 65536      | 4096      |   5,974.4 ns |  1.06 |    0.04 |         - |          NA |
