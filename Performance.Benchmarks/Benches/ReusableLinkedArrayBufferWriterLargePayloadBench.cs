using BenchmarkDotNet.Attributes;

using Performance.Buffers;

namespace Performance.Benchmarks.Benches;

[MemoryDiagnoser]
[HideColumns("Error", "StdDev", "Median")]
public class ReusableLinkedArrayBufferWriterLargePayloadBench
{
    [Params(262_144, 1_048_576, 4_194_304, 16_777_216)]
    public int TotalBytes;

    [Params(256, 4_096, 65_536)]
    public int ChunkSize;

    private byte[] _source = null!;
    private ResizableByteWriter _resizableWriter = null!;
    private ReusableLinkedArrayBufferWriter _linkedWriter = null!;
    private CountingWriteStream _streamSink = null!;
    private CountingSegmentConsumer _segmentSink = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _source = new byte[TotalBytes];
        new Random(42).NextBytes(_source);

        _resizableWriter = new();
        _linkedWriter = new(useFirstBuffer: false, pinned: false);
        _streamSink = new CountingWriteStream();
        _segmentSink = new CountingSegmentConsumer();
    }

    [IterationCleanup]
    public void Cleanup()
    {
        _resizableWriter.Reset();
        _linkedWriter.Reset();
        _streamSink.Reset();
        _segmentSink.Reset();
    }

    [Benchmark(Baseline = true, Description = "Resizable.Fill+SingleFlush")]
    public async ValueTask Resizable_FillThenSingleFlushAsync()
    {
        FillResizableWritePath();
        await _streamSink.WriteAsync(_resizableWriter.WrittenMemory, CancellationToken.None).ConfigureAwait(false);
        _resizableWriter.Reset();
    }

    [Benchmark(Description = "Linked.Fill+SegmentFlush")]
    public async ValueTask Linked_FillThenSegmentFlushAsync()
    {
        FillLinkedWritePath(_linkedWriter);
        await _linkedWriter.WriteToAndResetAsync(_streamSink, CancellationToken.None).ConfigureAwait(false);
    }

    [Benchmark(Description = "Linked.Fill+ToArray+SingleFlush")]
    public async ValueTask Linked_FillThenToArraySingleFlushAsync()
    {
        FillLinkedWritePath(_linkedWriter);
        var contiguous = _linkedWriter.ToArrayAndReset();
        await _streamSink.WriteAsync(contiguous, CancellationToken.None).ConfigureAwait(false);
    }

    [Benchmark(Description = "Resizable.Fill+SegmentConsume")]
    public void Resizable_FillThenSegmentConsume()
    {
        FillResizableWritePath();
        _segmentSink.Consume(_resizableWriter.WrittenMemory);
        _resizableWriter.Reset();
    }

    [Benchmark(Description = "Linked.Fill+SegmentConsume")]
    public void Linked_FillThenSegmentConsume()
    {
        FillLinkedWritePath(_linkedWriter);
        foreach (var segment in _linkedWriter)
        {
            _segmentSink.Consume(segment);
        }
        _linkedWriter.Reset();
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

    private sealed class CountingWriteStream : Stream
    {
        private long _bytesWritten;
        private int _writeCalls;

        public long BytesWritten => _bytesWritten;
        public int WriteCalls => _writeCalls;

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => _bytesWritten;
        public override long Position
        {
            get => _bytesWritten;
            set => throw new NotSupportedException();
        }

        public void Reset()
        {
            _bytesWritten = 0;
            _writeCalls = 0;
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            _bytesWritten += count;
            _writeCalls++;
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            _bytesWritten += buffer.Length;
            _writeCalls++;
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            _bytesWritten += count;
            _writeCalls++;
            return Task.CompletedTask;
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            _bytesWritten += buffer.Length;
            _writeCalls++;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class CountingSegmentConsumer
    {
        private long _bytesConsumed;
        private int _segmentsConsumed;
        private uint _checksum;

        public long BytesConsumed => _bytesConsumed;
        public int SegmentsConsumed => _segmentsConsumed;
        public uint Checksum => _checksum;

        public void Consume(ReadOnlyMemory<byte> segment)
        {
            _bytesConsumed += segment.Length;
            _segmentsConsumed++;

            if (!segment.IsEmpty)
            {
                var span = segment.Span;
                _checksum = (_checksum << 5) ^ (_checksum >> 27) ^ span[0] ^ span[^1];
            }
        }

        public void Reset()
        {
            _bytesConsumed = 0;
            _segmentsConsumed = 0;
            _checksum = 0;
        }
    }
}
