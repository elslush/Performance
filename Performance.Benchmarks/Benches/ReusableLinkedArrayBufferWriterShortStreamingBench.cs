using BenchmarkDotNet.Attributes;

using Performance.Buffers;

namespace Performance.Benchmarks.Benches;

[MemoryDiagnoser]
[HideColumns("Error", "StdDev", "Median")]
public class ReusableLinkedArrayBufferWriterShortStreamingBench
{
    [Params(64, 256, 1_024, 4_096)]
    public int TotalBytes;

    [Params(8, 32, 128)]
    public int ChunkSize;

    private byte[] _source = null!;
    private ResizableByteWriter _resizableWriter = null!;
    private ReusableLinkedArrayBufferWriter _linkedWriter = null!;
    private ReusableLinkedArrayBufferWriter _linkedFirstWriter = null!;
    private CountingWriteStream _sink = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _source = new byte[TotalBytes];
        new Random(42).NextBytes(_source);

        _resizableWriter = new();
        _linkedWriter = new(useFirstBuffer: false, pinned: false);
        _linkedFirstWriter = new(useFirstBuffer: true, pinned: false);
        _sink = new CountingWriteStream();
    }

    [IterationCleanup]
    public void Cleanup()
    {
        _resizableWriter.Reset();
        _linkedWriter.Reset();
        _linkedFirstWriter.Reset();
        _sink.Reset();
    }

    [Benchmark(Baseline = true, Description = "Resizable.Write+FlushAsync")]
    public async ValueTask Resizable_WriteThenFlushAsync()
    {
        FillResizableWritePath();
        await _sink.WriteAsync(_resizableWriter.WrittenMemory, CancellationToken.None).ConfigureAwait(false);
        _resizableWriter.Reset();
    }

    [Benchmark(Description = "Linked.Write+WriteToResetAsync")]
    public async ValueTask Linked_WriteThenFlushAsync()
    {
        FillLinkedWritePath(_linkedWriter);
        await _linkedWriter.WriteToAndResetAsync(_sink, CancellationToken.None).ConfigureAwait(false);
    }

    [Benchmark(Description = "Linked.GetSpan+WriteToResetAsync")]
    public async ValueTask Linked_SpanThenFlushAsync()
    {
        FillLinkedSpanAdvancePath(_linkedWriter);
        await _linkedWriter.WriteToAndResetAsync(_sink, CancellationToken.None).ConfigureAwait(false);
    }

    [Benchmark(Description = "LinkedFirst.Write+WriteToResetAsync")]
    public async ValueTask LinkedFirst_WriteThenFlushAsync()
    {
        FillLinkedWritePath(_linkedFirstWriter);
        await _linkedFirstWriter.WriteToAndResetAsync(_sink, CancellationToken.None).ConfigureAwait(false);
    }

    [Benchmark(Description = "LinkedPool.Rent+Write+Flush+Return")]
    public async ValueTask LinkedPool_WriteThenFlushAsync()
    {
        var writer = ReusableLinkedArrayBufferWriterPool.Rent();
        FillLinkedWritePath(writer);
        await writer.WriteToAndResetAsync(_sink, CancellationToken.None).ConfigureAwait(false);
        ReusableLinkedArrayBufferWriterPool.Return(writer);
    }

    [Benchmark(Description = "LinkedPool.Rent+Span+Flush+Return")]
    public async ValueTask LinkedPool_SpanThenFlushAsync()
    {
        var writer = ReusableLinkedArrayBufferWriterPool.Rent();
        FillLinkedSpanAdvancePath(writer);
        await writer.WriteToAndResetAsync(_sink, CancellationToken.None).ConfigureAwait(false);
        ReusableLinkedArrayBufferWriterPool.Return(writer);
    }

    private void FillResizableWritePath()
    {
        for (int offset = 0; offset < _source.Length; offset += ChunkSize)
        {
            int count = Math.Min(ChunkSize, _source.Length - offset);
            _resizableWriter.Write(_source.AsSpan(offset, count));
        }
    }

    private void FillLinkedWritePath(ReusableLinkedArrayBufferWriter writer)
    {
        for (int offset = 0; offset < _source.Length; offset += ChunkSize)
        {
            int count = Math.Min(ChunkSize, _source.Length - offset);
            writer.Write(_source.AsSpan(offset, count));
        }
    }

    private void FillLinkedSpanAdvancePath(ReusableLinkedArrayBufferWriter writer)
    {
        for (int offset = 0; offset < _source.Length; offset += ChunkSize)
        {
            int count = Math.Min(ChunkSize, _source.Length - offset);
            var span = writer.GetSpan(count);
            _source.AsSpan(offset, count).CopyTo(span);
            writer.Advance(count);
        }
    }

    private sealed class CountingWriteStream : Stream
    {
        private long _bytesWritten;
        public long BytesWritten => _bytesWritten;

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => _bytesWritten;
        public override long Position
        {
            get => _bytesWritten;
            set => throw new NotSupportedException();
        }

        public void Reset() => _bytesWritten = 0;

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            _bytesWritten += count;
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            _bytesWritten += buffer.Length;
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            _bytesWritten += count;
            return Task.CompletedTask;
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            _bytesWritten += buffer.Length;
            return ValueTask.CompletedTask;
        }
    }
}
