```

BenchmarkDotNet v0.15.3, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
INTEL XEON GOLD 6548N 0.80GHz, 2 CPU, 128 logical and 64 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v4
  Job-CNUJVU : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Type                              | Method                       | TotalBytes | ChunkSize | TotalItems | Mean        | Ratio | RatioSD | Allocated | Alloc Ratio |
|---------------------------------- |----------------------------- |----------- |---------- |----------- |------------:|------:|--------:|----------:|------------:|
| **ResizableByteWriterStreamingBench** | **Old.Write(chunked)**           | **4096**       | **16**        | **?**          |  **7,026.1 ns** |  **1.00** |    **0.03** |         **-** |          **NA** |
| ResizableByteWriterStreamingBench | New.Write(chunked)           | 4096       | 16        | ?          |  7,880.7 ns |  1.12 |    0.09 |         - |          NA |
| ResizableByteWriterStreamingBench | Old.GetSpan+Advance(chunked) | 4096       | 16        | ?          |  8,319.2 ns |  1.18 |    0.02 |         - |          NA |
| ResizableByteWriterStreamingBench | New.GetSpan+Advance(chunked) | 4096       | 16        | ?          |  8,553.3 ns |  1.22 |    0.02 |         - |          NA |
|                                   |                              |            |           |            |             |       |         |           |             |
| **ResizableSpanWriterStreamingBench** | **Old.Write(chunked)**           | **?**          | **16**        | **4096**       |  **4,145.8 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| ResizableSpanWriterStreamingBench | New.Write(chunked)           | ?          | 16        | 4096       |  4,027.9 ns |  0.97 |    0.02 |         - |          NA |
| ResizableSpanWriterStreamingBench | Old.GetSpan+Advance(chunked) | ?          | 16        | 4096       |  7,932.4 ns |  1.91 |    0.04 |         - |          NA |
| ResizableSpanWriterStreamingBench | New.GetSpan+Advance(chunked) | ?          | 16        | 4096       |  7,603.0 ns |  1.83 |    0.04 |         - |          NA |
|                                   |                              |            |           |            |             |       |         |           |             |
| **ResizableByteWriterStreamingBench** | **Old.Write(chunked)**           | **4096**       | **256**       | **?**          |    **606.3 ns** |  **1.00** |    **0.07** |         **-** |          **NA** |
| ResizableByteWriterStreamingBench | New.Write(chunked)           | 4096       | 256       | ?          |    566.4 ns |  0.94 |    0.07 |         - |          NA |
| ResizableByteWriterStreamingBench | Old.GetSpan+Advance(chunked) | 4096       | 256       | ?          |    759.8 ns |  1.26 |    0.08 |         - |          NA |
| ResizableByteWriterStreamingBench | New.GetSpan+Advance(chunked) | 4096       | 256       | ?          |    730.6 ns |  1.21 |    0.07 |         - |          NA |
|                                   |                              |            |           |            |             |       |         |           |             |
| **ResizableSpanWriterStreamingBench** | **Old.Write(chunked)**           | **?**          | **256**       | **4096**       |    **644.6 ns** |  **1.00** |    **0.08** |         **-** |          **NA** |
| ResizableSpanWriterStreamingBench | New.Write(chunked)           | ?          | 256       | 4096       |    647.1 ns |  1.01 |    0.06 |         - |          NA |
| ResizableSpanWriterStreamingBench | Old.GetSpan+Advance(chunked) | ?          | 256       | 4096       |    968.8 ns |  1.51 |    0.09 |         - |          NA |
| ResizableSpanWriterStreamingBench | New.GetSpan+Advance(chunked) | ?          | 256       | 4096       |  1,038.1 ns |  1.62 |    0.10 |         - |          NA |
|                                   |                              |            |           |            |             |       |         |           |             |
| **ResizableByteWriterStreamingBench** | **Old.Write(chunked)**           | **4096**       | **4096**      | **?**          |    **170.8 ns** |  **1.02** |    **0.18** |         **-** |          **NA** |
| ResizableByteWriterStreamingBench | New.Write(chunked)           | 4096       | 4096      | ?          |    182.6 ns |  1.09 |    0.15 |         - |          NA |
| ResizableByteWriterStreamingBench | Old.GetSpan+Advance(chunked) | 4096       | 4096      | ?          |    195.9 ns |  1.17 |    0.20 |         - |          NA |
| ResizableByteWriterStreamingBench | New.GetSpan+Advance(chunked) | 4096       | 4096      | ?          |    190.4 ns |  1.13 |    0.16 |         - |          NA |
|                                   |                              |            |           |            |             |       |         |           |             |
| **ResizableSpanWriterStreamingBench** | **Old.Write(chunked)**           | **?**          | **4096**      | **4096**       |    **439.5 ns** |  **1.03** |    **0.27** |         **-** |          **NA** |
| ResizableSpanWriterStreamingBench | New.Write(chunked)           | ?          | 4096      | 4096       |    432.6 ns |  1.02 |    0.21 |         - |          NA |
| ResizableSpanWriterStreamingBench | Old.GetSpan+Advance(chunked) | ?          | 4096      | 4096       |    426.8 ns |  1.00 |    0.27 |         - |          NA |
| ResizableSpanWriterStreamingBench | New.GetSpan+Advance(chunked) | ?          | 4096      | 4096       |    419.8 ns |  0.99 |    0.19 |         - |          NA |
|                                   |                              |            |           |            |             |       |         |           |             |
| **ResizableByteWriterStreamingBench** | **Old.Write(chunked)**           | **65536**      | **16**        | **?**          | **39,388.9 ns** |  **1.00** |    **0.06** |         **-** |          **NA** |
| ResizableByteWriterStreamingBench | New.Write(chunked)           | 65536      | 16        | ?          | 39,997.4 ns |  1.02 |    0.05 |         - |          NA |
| ResizableByteWriterStreamingBench | Old.GetSpan+Advance(chunked) | 65536      | 16        | ?          | 41,400.6 ns |  1.05 |    0.05 |         - |          NA |
| ResizableByteWriterStreamingBench | New.GetSpan+Advance(chunked) | 65536      | 16        | ?          | 42,183.7 ns |  1.07 |    0.06 |         - |          NA |
|                                   |                              |            |           |            |             |       |         |           |             |
| **ResizableSpanWriterStreamingBench** | **Old.Write(chunked)**           | **?**          | **16**        | **65536**      | **30,684.8 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| ResizableSpanWriterStreamingBench | New.Write(chunked)           | ?          | 16        | 65536      | 21,055.6 ns |  0.69 |    0.01 |         - |          NA |
| ResizableSpanWriterStreamingBench | Old.GetSpan+Advance(chunked) | ?          | 16        | 65536      | 15,994.8 ns |  0.52 |    0.00 |         - |          NA |
| ResizableSpanWriterStreamingBench | New.GetSpan+Advance(chunked) | ?          | 16        | 65536      | 17,797.1 ns |  0.58 |    0.01 |         - |          NA |
|                                   |                              |            |           |            |             |       |         |           |             |
| **ResizableByteWriterStreamingBench** | **Old.Write(chunked)**           | **65536**      | **256**       | **?**          |  **7,591.9 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| ResizableByteWriterStreamingBench | New.Write(chunked)           | 65536      | 256       | ?          |  7,479.4 ns |  0.99 |    0.02 |         - |          NA |
| ResizableByteWriterStreamingBench | Old.GetSpan+Advance(chunked) | 65536      | 256       | ?          | 10,855.0 ns |  1.43 |    0.03 |         - |          NA |
| ResizableByteWriterStreamingBench | New.GetSpan+Advance(chunked) | 65536      | 256       | ?          |  9,942.3 ns |  1.31 |    0.02 |         - |          NA |
|                                   |                              |            |           |            |             |       |         |           |             |
| **ResizableSpanWriterStreamingBench** | **Old.Write(chunked)**           | **?**          | **256**       | **65536**      |  **9,011.9 ns** |  **1.00** |    **0.01** |         **-** |          **NA** |
| ResizableSpanWriterStreamingBench | New.Write(chunked)           | ?          | 256       | 65536      |  8,846.6 ns |  0.98 |    0.02 |         - |          NA |
| ResizableSpanWriterStreamingBench | Old.GetSpan+Advance(chunked) | ?          | 256       | 65536      | 14,050.2 ns |  1.56 |    0.02 |         - |          NA |
| ResizableSpanWriterStreamingBench | New.GetSpan+Advance(chunked) | ?          | 256       | 65536      | 14,384.5 ns |  1.60 |    0.02 |         - |          NA |
|                                   |                              |            |           |            |             |       |         |           |             |
| **ResizableByteWriterStreamingBench** | **Old.Write(chunked)**           | **65536**      | **4096**      | **?**          |  **1,621.3 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| ResizableByteWriterStreamingBench | New.Write(chunked)           | 65536      | 4096      | ?          |  1,729.8 ns |  1.07 |    0.03 |         - |          NA |
| ResizableByteWriterStreamingBench | Old.GetSpan+Advance(chunked) | 65536      | 4096      | ?          |  1,915.0 ns |  1.18 |    0.03 |         - |          NA |
| ResizableByteWriterStreamingBench | New.GetSpan+Advance(chunked) | 65536      | 4096      | ?          |  1,890.7 ns |  1.17 |    0.02 |         - |          NA |
|                                   |                              |            |           |            |             |       |         |           |             |
| **ResizableSpanWriterStreamingBench** | **Old.Write(chunked)**           | **?**          | **4096**      | **65536**      |  **5,297.0 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| ResizableSpanWriterStreamingBench | New.Write(chunked)           | ?          | 4096      | 65536      |  5,440.2 ns |  1.03 |    0.02 |         - |          NA |
| ResizableSpanWriterStreamingBench | Old.GetSpan+Advance(chunked) | ?          | 4096      | 65536      |  5,978.2 ns |  1.13 |    0.02 |         - |          NA |
| ResizableSpanWriterStreamingBench | New.GetSpan+Advance(chunked) | ?          | 4096      | 65536      |  6,204.6 ns |  1.17 |    0.02 |         - |          NA |
