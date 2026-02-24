using System.Reflection;

using Performance.Benchmarks.Benches;
using Performance.Buffers;

using Xunit;

namespace Performance.Benchmarks.Benches.UnitTests;

public class ReusableLinkedArrayBufferWriterShortStreamingBenchTests
{
    [Fact]
    public void GlobalSetup_InitializesWriters_Source_AndSink()
    {
        var bench = new ReusableLinkedArrayBufferWriterShortStreamingBench
        {
            TotalBytes = 1_024,
            ChunkSize = 32
        };

        bench.GlobalSetup();

        var source = GetField<byte[]>(bench, "_source");
        var resizable = GetField<ResizableByteWriter>(bench, "_resizableWriter");
        var linked = GetField<ReusableLinkedArrayBufferWriter>(bench, "_linkedWriter");
        var linkedFirst = GetField<ReusableLinkedArrayBufferWriter>(bench, "_linkedFirstWriter");
        var sink = GetField<object>(bench, "_sink");

        Assert.Equal(1_024, source.Length);
        Assert.Equal(0, resizable.WrittenSpan.Length);
        Assert.Equal(0, linked.TotalWritten);
        Assert.Equal(0, linkedFirst.TotalWritten);
        Assert.Equal(0L, GetSinkBytesWritten(sink));
    }

    [Theory]
    [InlineData(64, 8)]
    [InlineData(1_024, 32)]
    [InlineData(4_096, 128)]
    public async Task Resizable_WriteThenFlushAsync_WritesExpectedBytes_AndCleanupResetsSink(
        int totalBytes,
        int chunkSize)
    {
        var bench = CreateBench(totalBytes, chunkSize);

        await bench.Resizable_WriteThenFlushAsync();

        var sink = GetField<object>(bench, "_sink");
        var writer = GetField<ResizableByteWriter>(bench, "_resizableWriter");
        Assert.Equal(totalBytes, GetSinkBytesWritten(sink));
        Assert.Equal(0, writer.WrittenSpan.Length);

        bench.Cleanup();
        Assert.Equal(0L, GetSinkBytesWritten(sink));
    }

    [Theory]
    [InlineData(64, 8)]
    [InlineData(1_024, 32)]
    [InlineData(4_096, 128)]
    public async Task Linked_WriteThenFlushAsync_WritesExpectedBytes_AndResetsWriter(
        int totalBytes,
        int chunkSize)
    {
        var bench = CreateBench(totalBytes, chunkSize);

        await bench.Linked_WriteThenFlushAsync();

        var sink = GetField<object>(bench, "_sink");
        var writer = GetField<ReusableLinkedArrayBufferWriter>(bench, "_linkedWriter");
        Assert.Equal(totalBytes, GetSinkBytesWritten(sink));
        Assert.Equal(0, writer.TotalWritten);

        bench.Cleanup();
        Assert.Equal(0L, GetSinkBytesWritten(sink));
    }

    [Theory]
    [InlineData(64, 8)]
    [InlineData(1_024, 32)]
    [InlineData(4_096, 128)]
    public async Task Linked_SpanThenFlushAsync_WritesExpectedBytes_AndResetsWriter(
        int totalBytes,
        int chunkSize)
    {
        var bench = CreateBench(totalBytes, chunkSize);

        await bench.Linked_SpanThenFlushAsync();

        var sink = GetField<object>(bench, "_sink");
        var writer = GetField<ReusableLinkedArrayBufferWriter>(bench, "_linkedWriter");
        Assert.Equal(totalBytes, GetSinkBytesWritten(sink));
        Assert.Equal(0, writer.TotalWritten);

        bench.Cleanup();
        Assert.Equal(0L, GetSinkBytesWritten(sink));
    }

    [Theory]
    [InlineData(64, 8)]
    [InlineData(1_024, 32)]
    [InlineData(4_096, 128)]
    public async Task LinkedFirst_WriteThenFlushAsync_WritesExpectedBytes_AndResetsWriter(
        int totalBytes,
        int chunkSize)
    {
        var bench = CreateBench(totalBytes, chunkSize);

        await bench.LinkedFirst_WriteThenFlushAsync();

        var sink = GetField<object>(bench, "_sink");
        var writer = GetField<ReusableLinkedArrayBufferWriter>(bench, "_linkedFirstWriter");
        Assert.Equal(totalBytes, GetSinkBytesWritten(sink));
        Assert.Equal(0, writer.TotalWritten);
    }

    [Theory]
    [InlineData(64, 8)]
    [InlineData(1_024, 32)]
    [InlineData(4_096, 128)]
    public async Task LinkedPool_WriteThenFlushAsync_WritesExpectedBytes_AndReturnedWriterIsReset(
        int totalBytes,
        int chunkSize)
    {
        var bench = CreateBench(totalBytes, chunkSize);

        await bench.LinkedPool_WriteThenFlushAsync();

        var sink = GetField<object>(bench, "_sink");
        Assert.Equal(totalBytes, GetSinkBytesWritten(sink));

        var pooledWriter = ReusableLinkedArrayBufferWriterPool.Rent();
        Assert.Equal(0, pooledWriter.TotalWritten);
        ReusableLinkedArrayBufferWriterPool.Return(pooledWriter);
    }

    [Theory]
    [InlineData(64, 8)]
    [InlineData(1_024, 32)]
    [InlineData(4_096, 128)]
    public async Task LinkedPool_SpanThenFlushAsync_WritesExpectedBytes_AndReturnedWriterIsReset(
        int totalBytes,
        int chunkSize)
    {
        var bench = CreateBench(totalBytes, chunkSize);

        await bench.LinkedPool_SpanThenFlushAsync();

        var sink = GetField<object>(bench, "_sink");
        Assert.Equal(totalBytes, GetSinkBytesWritten(sink));

        var pooledWriter = ReusableLinkedArrayBufferWriterPool.Rent();
        Assert.Equal(0, pooledWriter.TotalWritten);
        ReusableLinkedArrayBufferWriterPool.Return(pooledWriter);
    }

    private static ReusableLinkedArrayBufferWriterShortStreamingBench CreateBench(int totalBytes, int chunkSize)
    {
        var bench = new ReusableLinkedArrayBufferWriterShortStreamingBench
        {
            TotalBytes = totalBytes,
            ChunkSize = chunkSize
        };
        bench.GlobalSetup();
        return bench;
    }

    private static T GetField<T>(object instance, string fieldName) where T : class
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);

        var value = field!.GetValue(instance);
        Assert.NotNull(value);
        return (T)value!;
    }

    private static long GetSinkBytesWritten(object sink)
    {
        var property = sink.GetType().GetProperty("BytesWritten", BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(property);

        var value = property!.GetValue(sink);
        Assert.IsType<long>(value);
        return (long)value!;
    }
}
