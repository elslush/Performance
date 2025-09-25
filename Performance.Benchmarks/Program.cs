using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Performance.Benchmarks.Benches;

//BenchmarkRunner.Run<WhitespaceSplitBench>();
BenchmarkRunner.Run<ResizableByteWriterBench>();