using BenchmarkDotNet.Attributes;

using Performance.Buffers;

namespace Performance.Benchmarks.Benches;

[MemoryDiagnoser]
[HideColumns("Error", "StdDev", "Median")]
public class ReusableLinkedArrayBufferWriterVsResizableByteWriterBench
{
    [Params(4_096, 65_536)]
    public int TotalBytes;

    [Params(16, 256, 4_096)]
    public int ChunkSize;

    private byte[] _source = null!;
    private ResizableByteWriter _resizableWriter = null!;
    private ReusableLinkedArrayBufferWriter _linkedWriter = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _source = new byte[TotalBytes];
        new Random(42).NextBytes(_source);

        _resizableWriter = new();
        _linkedWriter = new(useFirstBuffer: false, pinned: false);
    }

    [IterationCleanup]
    public void Cleanup()
    {
        _resizableWriter.Reset();
        _linkedWriter.Reset();
    }

    [Benchmark(Baseline = true, Description = "Resizable.Write(chunked)")]
    public void Resizable_WriteChunked()
    {
        for (int offset = 0; offset < _source.Length; offset += ChunkSize)
        {
            int count = Math.Min(ChunkSize, _source.Length - offset);
            _resizableWriter.Write(_source.AsSpan(offset, count));
        }
    }

    [Benchmark(Description = "Linked.Write(chunked)")]
    public void Linked_WriteChunked()
    {
        for (int offset = 0; offset < _source.Length; offset += ChunkSize)
        {
            int count = Math.Min(ChunkSize, _source.Length - offset);
            _linkedWriter.Write(_source.AsSpan(offset, count));
        }
    }

    [Benchmark(Description = "Resizable.GetSpan+Advance(chunked)")]
    public void Resizable_WriteSpanAdvanceChunked()
    {
        for (int offset = 0; offset < _source.Length; offset += ChunkSize)
        {
            int count = Math.Min(ChunkSize, _source.Length - offset);
            var span = _resizableWriter.GetSpan(count);
            _source.AsSpan(offset, count).CopyTo(span);
            _resizableWriter.Advance(count);
        }
    }

    [Benchmark(Description = "Linked.GetSpan+Advance(chunked)")]
    public void Linked_WriteSpanAdvanceChunked()
    {
        for (int offset = 0; offset < _source.Length; offset += ChunkSize)
        {
            int count = Math.Min(ChunkSize, _source.Length - offset);
            var span = _linkedWriter.GetSpan(count);
            _source.AsSpan(offset, count).CopyTo(span);
            _linkedWriter.Advance(count);
        }
    }

    [Benchmark(Description = "LinkedPool.Rent+Write+Return")]
    public void LinkedPool_RentWriteReturn()
    {
        var writer = ReusableLinkedArrayBufferWriterPool.Rent();
        for (int offset = 0; offset < _source.Length; offset += ChunkSize)
        {
            int count = Math.Min(ChunkSize, _source.Length - offset);
            writer.Write(_source.AsSpan(offset, count));
        }
        ReusableLinkedArrayBufferWriterPool.Return(writer);
    }

    [Benchmark(Description = "LinkedPool.Rent+Span+Return")]
    public void LinkedPool_RentSpanAdvanceReturn()
    {
        var writer = ReusableLinkedArrayBufferWriterPool.Rent();
        for (int offset = 0; offset < _source.Length; offset += ChunkSize)
        {
            int count = Math.Min(ChunkSize, _source.Length - offset);
            var span = writer.GetSpan(count);
            _source.AsSpan(offset, count).CopyTo(span);
            writer.Advance(count);
        }
        ReusableLinkedArrayBufferWriterPool.Return(writer);
    }
}
