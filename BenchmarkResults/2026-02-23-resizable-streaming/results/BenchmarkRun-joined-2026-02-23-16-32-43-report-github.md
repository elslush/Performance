```

BenchmarkDotNet v0.15.3, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
INTEL XEON GOLD 6548N 0.78GHz, 2 CPU, 128 logical and 64 physical cores
.NET SDK 10.0.100
  [Host]   : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Type                              | Method                       | TotalBytes | ChunkSize | TotalItems | Mean         | Ratio | RatioSD | Allocated | Alloc Ratio |
|---------------------------------- |----------------------------- |----------- |---------- |----------- |-------------:|------:|--------:|----------:|------------:|
| **ResizableByteWriterStreamingBench** | **Old.Write(chunked)**           | **4096**       | **16**        | **?**          |   **7,469.0 ns** |  **1.00** |    **0.06** |         **-** |          **NA** |
| ResizableByteWriterStreamingBench | New.Write(chunked)           | 4096       | 16        | ?          |   6,952.2 ns |  0.93 |    0.06 |         - |          NA |
| ResizableByteWriterStreamingBench | Old.GetSpan+Advance(chunked) | 4096       | 16        | ?          |   9,037.3 ns |  1.21 |    0.08 |         - |          NA |
| ResizableByteWriterStreamingBench | New.GetSpan+Advance(chunked) | 4096       | 16        | ?          |   8,842.5 ns |  1.19 |    0.08 |         - |          NA |
|                                   |                              |            |           |            |              |       |         |           |             |
| **ResizableSpanWriterStreamingBench** | **Old.Write(chunked)**           | **?**          | **16**        | **4096**       |   **5,842.3 ns** |  **1.01** |    **0.17** |         **-** |          **NA** |
| ResizableSpanWriterStreamingBench | New.Write(chunked)           | ?          | 16        | 4096       |   4,541.0 ns |  0.79 |    0.15 |         - |          NA |
| ResizableSpanWriterStreamingBench | Old.GetSpan+Advance(chunked) | ?          | 16        | 4096       |  10,607.0 ns |  1.84 |    0.24 |         - |          NA |
| ResizableSpanWriterStreamingBench | New.GetSpan+Advance(chunked) | ?          | 16        | 4096       |  11,220.7 ns |  1.94 |    0.41 |         - |          NA |
|                                   |                              |            |           |            |              |       |         |           |             |
| **ResizableByteWriterStreamingBench** | **Old.Write(chunked)**           | **4096**       | **256**       | **?**          |     **927.3 ns** |  **1.14** |    **0.61** |         **-** |          **NA** |
| ResizableByteWriterStreamingBench | New.Write(chunked)           | 4096       | 256       | ?          |     829.3 ns |  1.02 |    0.51 |         - |          NA |
| ResizableByteWriterStreamingBench | Old.GetSpan+Advance(chunked) | 4096       | 256       | ?          |     970.7 ns |  1.19 |    0.57 |         - |          NA |
| ResizableByteWriterStreamingBench | New.GetSpan+Advance(chunked) | 4096       | 256       | ?          |     976.0 ns |  1.19 |    0.60 |         - |          NA |
|                                   |                              |            |           |            |              |       |         |           |             |
| **ResizableSpanWriterStreamingBench** | **Old.Write(chunked)**           | **?**          | **256**       | **4096**       |     **949.3 ns** |  **1.14** |    **0.63** |         **-** |          **NA** |
| ResizableSpanWriterStreamingBench | New.Write(chunked)           | ?          | 256       | 4096       |   1,101.2 ns |  1.33 |    0.73 |         - |          NA |
| ResizableSpanWriterStreamingBench | Old.GetSpan+Advance(chunked) | ?          | 256       | 4096       |   1,195.3 ns |  1.44 |    0.61 |         - |          NA |
| ResizableSpanWriterStreamingBench | New.GetSpan+Advance(chunked) | ?          | 256       | 4096       |   1,415.8 ns |  1.70 |    0.85 |         - |          NA |
|                                   |                              |            |           |            |              |       |         |           |             |
| **ResizableByteWriterStreamingBench** | **Old.Write(chunked)**           | **4096**       | **4096**      | **?**          |     **538.3 ns** |  **1.45** |    **1.36** |         **-** |          **NA** |
| ResizableByteWriterStreamingBench | New.Write(chunked)           | 4096       | 4096      | ?          |     468.8 ns |  1.27 |    1.17 |         - |          NA |
| ResizableByteWriterStreamingBench | Old.GetSpan+Advance(chunked) | 4096       | 4096      | ?          |     487.5 ns |  1.32 |    1.23 |         - |          NA |
| ResizableByteWriterStreamingBench | New.GetSpan+Advance(chunked) | 4096       | 4096      | ?          |     539.2 ns |  1.46 |    1.37 |         - |          NA |
|                                   |                              |            |           |            |              |       |         |           |             |
| **ResizableSpanWriterStreamingBench** | **Old.Write(chunked)**           | **?**          | **4096**      | **4096**       |     **769.3 ns** |  **1.27** |    **0.95** |         **-** |          **NA** |
| ResizableSpanWriterStreamingBench | New.Write(chunked)           | ?          | 4096      | 4096       |     758.0 ns |  1.25 |    0.99 |         - |          NA |
| ResizableSpanWriterStreamingBench | Old.GetSpan+Advance(chunked) | ?          | 4096      | 4096       |     674.0 ns |  1.12 |    0.78 |         - |          NA |
| ResizableSpanWriterStreamingBench | New.GetSpan+Advance(chunked) | ?          | 4096      | 4096       |     784.5 ns |  1.30 |    1.02 |         - |          NA |
|                                   |                              |            |           |            |              |       |         |           |             |
| **ResizableByteWriterStreamingBench** | **Old.Write(chunked)**           | **65536**      | **16**        | **?**          |  **43,857.0 ns** |  **1.00** |    **0.06** |         **-** |          **NA** |
| ResizableByteWriterStreamingBench | New.Write(chunked)           | 65536      | 16        | ?          |  43,063.5 ns |  0.98 |    0.05 |         - |          NA |
| ResizableByteWriterStreamingBench | Old.GetSpan+Advance(chunked) | 65536      | 16        | ?          |  45,137.7 ns |  1.03 |    0.07 |         - |          NA |
| ResizableByteWriterStreamingBench | New.GetSpan+Advance(chunked) | 65536      | 16        | ?          |  45,853.7 ns |  1.05 |    0.06 |         - |          NA |
|                                   |                              |            |           |            |              |       |         |           |             |
| **ResizableSpanWriterStreamingBench** | **Old.Write(chunked)**           | **?**          | **16**        | **65536**      |  **39,101.2 ns** |  **1.02** |    **0.24** |         **-** |          **NA** |
| ResizableSpanWriterStreamingBench | New.Write(chunked)           | ?          | 16        | 65536      | 361,415.7 ns |  9.46 |    1.47 |         - |          NA |
| ResizableSpanWriterStreamingBench | Old.GetSpan+Advance(chunked) | ?          | 16        | 65536      |  59,203.8 ns |  1.55 |    0.30 |         - |          NA |
| ResizableSpanWriterStreamingBench | New.GetSpan+Advance(chunked) | ?          | 16        | 65536      | 380,982.3 ns |  9.97 |    1.53 |         - |          NA |
|                                   |                              |            |           |            |              |       |         |           |             |
| **ResizableByteWriterStreamingBench** | **Old.Write(chunked)**           | **65536**      | **256**       | **?**          |   **8,520.7 ns** |  **1.01** |    **0.13** |         **-** |          **NA** |
| ResizableByteWriterStreamingBench | New.Write(chunked)           | 65536      | 256       | ?          |   7,691.5 ns |  0.91 |    0.11 |         - |          NA |
| ResizableByteWriterStreamingBench | Old.GetSpan+Advance(chunked) | 65536      | 256       | ?          |  11,921.0 ns |  1.41 |    0.21 |         - |          NA |
| ResizableByteWriterStreamingBench | New.GetSpan+Advance(chunked) | 65536      | 256       | ?          |  10,841.5 ns |  1.28 |    0.16 |         - |          NA |
|                                   |                              |            |           |            |              |       |         |           |             |
| **ResizableSpanWriterStreamingBench** | **Old.Write(chunked)**           | **?**          | **256**       | **65536**      |  **11,511.7 ns** |  **1.02** |    **0.22** |         **-** |          **NA** |
| ResizableSpanWriterStreamingBench | New.Write(chunked)           | ?          | 256       | 65536      |  17,788.5 ns |  1.58 |    0.76 |         - |          NA |
| ResizableSpanWriterStreamingBench | Old.GetSpan+Advance(chunked) | ?          | 256       | 65536      |  17,095.2 ns |  1.52 |    0.33 |         - |          NA |
| ResizableSpanWriterStreamingBench | New.GetSpan+Advance(chunked) | ?          | 256       | 65536      |  24,052.7 ns |  2.13 |    0.50 |         - |          NA |
|                                   |                              |            |           |            |              |       |         |           |             |
| **ResizableByteWriterStreamingBench** | **Old.Write(chunked)**           | **65536**      | **4096**      | **?**          |   **2,458.8 ns** |  **1.17** |    **0.69** |         **-** |          **NA** |
| ResizableByteWriterStreamingBench | New.Write(chunked)           | 65536      | 4096      | ?          |   2,645.3 ns |  1.25 |    0.77 |         - |          NA |
| ResizableByteWriterStreamingBench | Old.GetSpan+Advance(chunked) | 65536      | 4096      | ?          |   2,899.8 ns |  1.37 |    0.81 |         - |          NA |
| ResizableByteWriterStreamingBench | New.GetSpan+Advance(chunked) | 65536      | 4096      | ?          |   2,561.2 ns |  1.21 |    0.58 |         - |          NA |
|                                   |                              |            |           |            |              |       |         |           |             |
| **ResizableSpanWriterStreamingBench** | **Old.Write(chunked)**           | **?**          | **4096**      | **65536**      |   **8,450.3 ns** |  **1.22** |    **0.84** |         **-** |          **NA** |
| ResizableSpanWriterStreamingBench | New.Write(chunked)           | ?          | 4096      | 65536      |   8,734.7 ns |  1.27 |    0.78 |         - |          NA |
| ResizableSpanWriterStreamingBench | Old.GetSpan+Advance(chunked) | ?          | 4096      | 65536      |  10,073.5 ns |  1.46 |    0.94 |         - |          NA |
| ResizableSpanWriterStreamingBench | New.GetSpan+Advance(chunked) | ?          | 4096      | 65536      |  11,699.3 ns |  1.70 |    1.26 |         - |          NA |
