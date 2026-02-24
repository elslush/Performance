using BenchmarkDotNet.Attributes;

using Performance.Benchmarks.Original;
using Performance.Buffers;

namespace Performance.Benchmarks.Benches;

[MemoryDiagnoser]
[HideColumns("Error", "StdDev", "Median")]
public class ResizableSpanWriterStreamingBench
{
    private const int SteadyStateWarmupIterations = 256;

    [Params(4_096, 65_536)]
    public int TotalItems;

    [Params(16, 256, 4_096)]
    public int ChunkSize;

    private int[] _source = null!;
    private OriginalResizableSpanWriter<int> _oldWriter = null!;
    private ResizableSpanWriter<int> _newWriter = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _source = new int[TotalItems];
        for (int i = 0; i < _source.Length; i++)
        {
            _source[i] = i;
        }

        _oldWriter = new();
        _newWriter = new();

        WarmupSteadyStatePaths();
    }

    [IterationCleanup]
    public void Cleanup()
    {
        _oldWriter.Reset();
        _newWriter.Reset();
    }

    [Benchmark(Baseline = true, Description = "Old.Write(chunked)")]
    public void Old_WriteChunked()
        => Old_WriteChunkedCore();

    [Benchmark(Description = "New.Write(chunked)")]
    public void New_WriteChunked()
        => New_WriteChunkedCore();

    [Benchmark(Description = "Old.GetSpan+Advance(chunked)")]
    public void Old_WriteSpanAdvanceChunked()
        => Old_WriteSpanAdvanceChunkedCore();

    [Benchmark(Description = "New.GetSpan+Advance(chunked)")]
    public void New_WriteSpanAdvanceChunked()
        => New_WriteSpanAdvanceChunkedCore();

    private void Old_WriteChunkedCore()
    {
        for (int offset = 0; offset < _source.Length; offset += ChunkSize)
        {
            int count = Math.Min(ChunkSize, _source.Length - offset);
            _oldWriter.Write(_source.AsSpan(offset, count));
        }
    }

    private void New_WriteChunkedCore()
    {
        for (int offset = 0; offset < _source.Length; offset += ChunkSize)
        {
            int count = Math.Min(ChunkSize, _source.Length - offset);
            _newWriter.Write(_source.AsSpan(offset, count));
        }
    }

    private void Old_WriteSpanAdvanceChunkedCore()
    {
        for (int offset = 0; offset < _source.Length; offset += ChunkSize)
        {
            int count = Math.Min(ChunkSize, _source.Length - offset);
            var span = _oldWriter.GetSpan(count);
            _source.AsSpan(offset, count).CopyTo(span);
            _oldWriter.Advance(count);
        }
    }

    private void New_WriteSpanAdvanceChunkedCore()
    {
        for (int offset = 0; offset < _source.Length; offset += ChunkSize)
        {
            int count = Math.Min(ChunkSize, _source.Length - offset);
            var span = _newWriter.GetSpan(count);
            _source.AsSpan(offset, count).CopyTo(span);
            _newWriter.Advance(count);
        }
    }

    private void WarmupSteadyStatePaths()
    {
        // BenchmarkDotNet uses InvocationCount=1 when IterationCleanup is present.
        // Prime tiered JIT for the hot write paths so measurements reflect steady-state throughput.
        for (int i = 0; i < SteadyStateWarmupIterations; i++)
        {
            Old_WriteChunkedCore();
            _oldWriter.Reset();

            New_WriteChunkedCore();
            _newWriter.Reset();

            Old_WriteSpanAdvanceChunkedCore();
            _oldWriter.Reset();

            New_WriteSpanAdvanceChunkedCore();
            _newWriter.Reset();
        }
    }
}
