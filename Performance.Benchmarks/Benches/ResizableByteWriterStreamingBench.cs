using BenchmarkDotNet.Attributes;

using Performance.Benchmarks.Original;
using Performance.Buffers;

namespace Performance.Benchmarks.Benches;

[MemoryDiagnoser]
[HideColumns("Error", "StdDev", "Median")]
public class ResizableByteWriterStreamingBench
{
    [Params(4_096, 65_536)]
    public int TotalBytes;

    [Params(16, 256, 4_096)]
    public int ChunkSize;

    private byte[] _source = null!;
    private OriginalResizableByteWriter _oldWriter = null!;
    private ResizableByteWriter _newWriter = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _source = new byte[TotalBytes];
        new Random(42).NextBytes(_source);

        _oldWriter = new();
        _newWriter = new();
    }

    [IterationCleanup]
    public void Cleanup()
    {
        _oldWriter.Reset();
        _newWriter.Reset();
    }

    [Benchmark(Baseline = true, Description = "Old.Write(chunked)")]
    public void Old_WriteChunked()
    {
        for (int offset = 0; offset < _source.Length; offset += ChunkSize)
        {
            int count = Math.Min(ChunkSize, _source.Length - offset);
            _oldWriter.Write(_source, offset, count);
        }
    }

    [Benchmark(Description = "New.Write(chunked)")]
    public void New_WriteChunked()
    {
        for (int offset = 0; offset < _source.Length; offset += ChunkSize)
        {
            int count = Math.Min(ChunkSize, _source.Length - offset);
            _newWriter.Write(_source, offset, count);
        }
    }

    [Benchmark(Description = "Old.GetSpan+Advance(chunked)")]
    public void Old_WriteSpanAdvanceChunked()
    {
        for (int offset = 0; offset < _source.Length; offset += ChunkSize)
        {
            int count = Math.Min(ChunkSize, _source.Length - offset);
            var span = _oldWriter.GetSpan(count);
            _source.AsSpan(offset, count).CopyTo(span);
            _oldWriter.Advance(count);
        }
    }

    [Benchmark(Description = "New.GetSpan+Advance(chunked)")]
    public void New_WriteSpanAdvanceChunked()
    {
        for (int offset = 0; offset < _source.Length; offset += ChunkSize)
        {
            int count = Math.Min(ChunkSize, _source.Length - offset);
            var span = _newWriter.GetSpan(count);
            _source.AsSpan(offset, count).CopyTo(span);
            _newWriter.Advance(count);
        }
    }
}
