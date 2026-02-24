```

BenchmarkDotNet v0.15.3, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
INTEL XEON GOLD 6548N 0.77GHz, 2 CPU, 128 logical and 64 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v4
  Job-CNUJVU : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                          | TotalBytes | ChunkSize | Mean         | Ratio | RatioSD | Allocated  | Alloc Ratio |
|-------------------------------- |----------- |---------- |-------------:|------:|--------:|-----------:|------------:|
| **Resizable.Fill+SingleFlush**      | **262144**     | **256**       |   **135.915 μs** |  **1.00** |    **0.03** |          **-** |          **NA** |
| Linked.Fill+SegmentFlush        | 262144     | 256       |   182.491 μs |  1.34 |    0.04 |          - |          NA |
| Linked.Fill+ToArray+SingleFlush | 262144     | 256       |   187.357 μs |  1.38 |    0.03 |   262168 B |          NA |
| Resizable.Fill+SegmentConsume   | 262144     | 256       |    23.178 μs |  0.17 |    0.01 |          - |          NA |
| Linked.Fill+SegmentConsume      | 262144     | 256       |    68.591 μs |  0.50 |    0.02 |          - |          NA |
|                                 |            |           |              |       |         |            |             |
| **Resizable.Fill+SingleFlush**      | **262144**     | **4096**      |     **7.701 μs** |  **1.00** |    **0.01** |          **-** |          **NA** |
| Linked.Fill+SegmentFlush        | 262144     | 4096      |    11.375 μs |  1.48 |    0.03 |          - |          NA |
| Linked.Fill+ToArray+SingleFlush | 262144     | 4096      |    16.862 μs |  2.19 |    0.04 |   262168 B |          NA |
| Resizable.Fill+SegmentConsume   | 262144     | 4096      |     7.876 μs |  1.02 |    0.06 |          - |          NA |
| Linked.Fill+SegmentConsume      | 262144     | 4096      |    11.261 μs |  1.46 |    0.03 |          - |          NA |
|                                 |            |           |              |       |         |            |             |
| **Resizable.Fill+SingleFlush**      | **262144**     | **65536**     |     **5.682 μs** |  **1.00** |    **0.01** |          **-** |          **NA** |
| Linked.Fill+SegmentFlush        | 262144     | 65536     |     7.075 μs |  1.25 |    0.10 |          - |          NA |
| Linked.Fill+ToArray+SingleFlush | 262144     | 65536     |    12.047 μs |  2.12 |    0.03 |   262168 B |          NA |
| Resizable.Fill+SegmentConsume   | 262144     | 65536     |     5.336 μs |  0.94 |    0.02 |          - |          NA |
| Linked.Fill+SegmentConsume      | 262144     | 65536     |     6.023 μs |  1.06 |    0.02 |          - |          NA |
|                                 |            |           |              |       |         |            |             |
| **Resizable.Fill+SingleFlush**      | **1048576**    | **256**       |   **174.661 μs** |  **1.00** |    **0.02** |          **-** |          **NA** |
| Linked.Fill+SegmentFlush        | 1048576    | 256       |   232.597 μs |  1.33 |    0.03 |          - |          NA |
| Linked.Fill+ToArray+SingleFlush | 1048576    | 256       |   473.858 μs |  2.71 |    0.05 |  1048600 B |          NA |
| Resizable.Fill+SegmentConsume   | 1048576    | 256       |    67.116 μs |  0.38 |    0.01 |          - |          NA |
| Linked.Fill+SegmentConsume      | 1048576    | 256       |   119.055 μs |  0.68 |    0.01 |          - |          NA |
|                                 |            |           |              |       |         |            |             |
| **Resizable.Fill+SingleFlush**      | **1048576**    | **4096**      |    **48.205 μs** |  **1.00** |    **0.02** |          **-** |          **NA** |
| Linked.Fill+SegmentFlush        | 1048576    | 4096      |    67.404 μs |  1.40 |    0.02 |          - |          NA |
| Linked.Fill+ToArray+SingleFlush | 1048576    | 4096      |   305.737 μs |  6.34 |    0.10 |  1048600 B |          NA |
| Resizable.Fill+SegmentConsume   | 1048576    | 4096      |    48.369 μs |  1.00 |    0.01 |          - |          NA |
| Linked.Fill+SegmentConsume      | 1048576    | 4096      |    66.622 μs |  1.38 |    0.02 |          - |          NA |
|                                 |            |           |              |       |         |            |             |
| **Resizable.Fill+SingleFlush**      | **1048576**    | **65536**     |    **46.591 μs** |  **1.00** |    **0.01** |          **-** |          **NA** |
| Linked.Fill+SegmentFlush        | 1048576    | 65536     |    47.597 μs |  1.02 |    0.01 |          - |          NA |
| Linked.Fill+ToArray+SingleFlush | 1048576    | 65536     |   299.018 μs |  6.42 |    0.07 |  1048600 B |          NA |
| Resizable.Fill+SegmentConsume   | 1048576    | 65536     |    46.891 μs |  1.01 |    0.01 |          - |          NA |
| Linked.Fill+SegmentConsume      | 1048576    | 65536     |    47.734 μs |  1.02 |    0.01 |          - |          NA |
|                                 |            |           |              |       |         |            |             |
| **Resizable.Fill+SingleFlush**      | **4194304**    | **256**       |   **345.991 μs** |  **1.00** |    **0.01** |          **-** |          **NA** |
| Linked.Fill+SegmentFlush        | 4194304    | 256       |   401.375 μs |  1.16 |    0.01 |          - |          NA |
| Linked.Fill+ToArray+SingleFlush | 4194304    | 256       | 1,570.703 μs |  4.54 |    0.05 |  4194328 B |          NA |
| Resizable.Fill+SegmentConsume   | 4194304    | 256       |   292.318 μs |  0.84 |    0.02 |          - |          NA |
| Linked.Fill+SegmentConsume      | 4194304    | 256       |   397.591 μs |  1.15 |    0.01 |          - |          NA |
|                                 |            |           |              |       |         |            |             |
| **Resizable.Fill+SingleFlush**      | **4194304**    | **4096**      |   **223.447 μs** |  **1.00** |    **0.02** |          **-** |          **NA** |
| Linked.Fill+SegmentFlush        | 4194304    | 4096      |   306.867 μs |  1.37 |    0.02 |          - |          NA |
| Linked.Fill+ToArray+SingleFlush | 4194304    | 4096      | 1,337.515 μs |  5.99 |    0.07 |  4194328 B |          NA |
| Resizable.Fill+SegmentConsume   | 4194304    | 4096      |   228.025 μs |  1.02 |    0.02 |          - |          NA |
| Linked.Fill+SegmentConsume      | 4194304    | 4096      |   309.597 μs |  1.39 |    0.02 |          - |          NA |
|                                 |            |           |              |       |         |            |             |
| **Resizable.Fill+SingleFlush**      | **4194304**    | **65536**     |   **215.581 μs** |  **1.00** |    **0.01** |          **-** |          **NA** |
| Linked.Fill+SegmentFlush        | 4194304    | 65536     |   217.726 μs |  1.01 |    0.01 |          - |          NA |
| Linked.Fill+ToArray+SingleFlush | 4194304    | 65536     | 1,273.641 μs |  5.91 |    0.06 |  4194328 B |          NA |
| Resizable.Fill+SegmentConsume   | 4194304    | 65536     |   232.607 μs |  1.08 |    0.02 |          - |          NA |
| Linked.Fill+SegmentConsume      | 4194304    | 65536     |   219.871 μs |  1.02 |    0.01 |          - |          NA |
|                                 |            |           |              |       |         |            |             |
| **Resizable.Fill+SingleFlush**      | **16777216**   | **256**       | **1,091.247 μs** |  **1.00** |    **0.00** |          **-** |          **NA** |
| Linked.Fill+SegmentFlush        | 16777216   | 256       | 1,189.578 μs |  1.09 |    0.01 |          - |          NA |
| Linked.Fill+ToArray+SingleFlush | 16777216   | 256       | 2,534.901 μs |  2.32 |    0.31 | 16777240 B |          NA |
| Resizable.Fill+SegmentConsume   | 16777216   | 256       | 1,037.393 μs |  0.95 |    0.02 |          - |          NA |
| Linked.Fill+SegmentConsume      | 16777216   | 256       | 1,164.334 μs |  1.07 |    0.00 |          - |          NA |
|                                 |            |           |              |       |         |            |             |
| **Resizable.Fill+SingleFlush**      | **16777216**   | **4096**      |   **966.189 μs** |  **1.00** |    **0.00** |          **-** |          **NA** |
| Linked.Fill+SegmentFlush        | 16777216   | 4096      | 1,012.431 μs |  1.05 |    0.00 |          - |          NA |
| Linked.Fill+ToArray+SingleFlush | 16777216   | 4096      | 2,157.845 μs |  2.23 |    0.03 | 16777240 B |          NA |
| Resizable.Fill+SegmentConsume   | 16777216   | 4096      | 1,049.679 μs |  1.09 |    0.00 |          - |          NA |
| Linked.Fill+SegmentConsume      | 16777216   | 4096      | 1,005.607 μs |  1.04 |    0.00 |          - |          NA |
|                                 |            |           |              |       |         |            |             |
| **Resizable.Fill+SingleFlush**      | **16777216**   | **65536**     |   **942.942 μs** |  **1.00** |    **0.00** |          **-** |          **NA** |
| Linked.Fill+SegmentFlush        | 16777216   | 65536     |   925.603 μs |  0.98 |    0.00 |          - |          NA |
| Linked.Fill+ToArray+SingleFlush | 16777216   | 65536     | 2,054.619 μs |  2.18 |    0.03 | 16777240 B |          NA |
| Resizable.Fill+SegmentConsume   | 16777216   | 65536     |   954.131 μs |  1.01 |    0.00 |          - |          NA |
| Linked.Fill+SegmentConsume      | 16777216   | 65536     | 1,053.942 μs |  1.12 |    0.00 |          - |          NA |
