using System.Reflection;

using Performance.Benchmarks.Benches;
using Performance.Buffers;

using Xunit;

namespace Performance.Benchmarks.Benches.UnitTests;

public class ReusableLinkedArrayBufferWriterVsResizableByteWriterBenchTests
{
    [Fact]
    public void GlobalSetup_InitializesWriters_AndSource()
    {
        var bench = new ReusableLinkedArrayBufferWriterVsResizableByteWriterBench
        {
            TotalBytes = 65_536,
            ChunkSize = 16
        };

        bench.GlobalSetup();

        var source = GetField<byte[]>(bench, "_source");
        var resizable = GetField<ResizableByteWriter>(bench, "_resizableWriter");
        var linked = GetField<ReusableLinkedArrayBufferWriter>(bench, "_linkedWriter");

        Assert.NotNull(source);
        Assert.Equal(65_536, source.Length);
        Assert.Equal(0, resizable.WrittenSpan.Length);
        Assert.Equal(0, linked.TotalWritten);
    }

    [Theory]
    [InlineData(4_096, 16)]
    [InlineData(65_536, 16)]
    [InlineData(65_536, 256)]
    public void Resizable_WriteChunked_WritesExpectedCount_AndCleanupResets(int totalBytes, int chunkSize)
    {
        var bench = CreateBench(totalBytes, chunkSize);

        bench.Resizable_WriteChunked();

        var writer = GetField<ResizableByteWriter>(bench, "_resizableWriter");
        Assert.Equal(totalBytes, writer.WrittenSpan.Length);

        bench.Cleanup();
        Assert.Equal(0, writer.WrittenSpan.Length);
    }

    [Theory]
    [InlineData(4_096, 16)]
    [InlineData(65_536, 16)]
    [InlineData(65_536, 256)]
    public void Linked_WriteChunked_WritesExpectedCount_AndCleanupResets(int totalBytes, int chunkSize)
    {
        var bench = CreateBench(totalBytes, chunkSize);

        bench.Linked_WriteChunked();

        var writer = GetField<ReusableLinkedArrayBufferWriter>(bench, "_linkedWriter");
        Assert.Equal(totalBytes, writer.TotalWritten);

        bench.Cleanup();
        Assert.Equal(0, writer.TotalWritten);
    }

    [Theory]
    [InlineData(4_096, 16)]
    [InlineData(65_536, 16)]
    [InlineData(65_536, 4_096)]
    public void Resizable_WriteSpanAdvanceChunked_WritesExpectedCount_AndCleanupResets(int totalBytes, int chunkSize)
    {
        var bench = CreateBench(totalBytes, chunkSize);

        bench.Resizable_WriteSpanAdvanceChunked();

        var writer = GetField<ResizableByteWriter>(bench, "_resizableWriter");
        Assert.Equal(totalBytes, writer.WrittenSpan.Length);

        bench.Cleanup();
        Assert.Equal(0, writer.WrittenSpan.Length);
    }

    [Theory]
    [InlineData(4_096, 16)]
    [InlineData(65_536, 16)]
    [InlineData(65_536, 4_096)]
    public void Linked_WriteSpanAdvanceChunked_WritesExpectedCount_AndCleanupResets(int totalBytes, int chunkSize)
    {
        var bench = CreateBench(totalBytes, chunkSize);

        bench.Linked_WriteSpanAdvanceChunked();

        var writer = GetField<ReusableLinkedArrayBufferWriter>(bench, "_linkedWriter");
        Assert.Equal(totalBytes, writer.TotalWritten);

        bench.Cleanup();
        Assert.Equal(0, writer.TotalWritten);
    }

    [Theory]
    [InlineData(4_096, 16)]
    [InlineData(65_536, 16)]
    [InlineData(65_536, 256)]
    public void LinkedPool_RentWriteReturn_LeavesPooledWriterReset(int totalBytes, int chunkSize)
    {
        var bench = CreateBench(totalBytes, chunkSize);

        bench.LinkedPool_RentWriteReturn();

        var pooled = ReusableLinkedArrayBufferWriterPool.Rent();
        Assert.Equal(0, pooled.TotalWritten);
        ReusableLinkedArrayBufferWriterPool.Return(pooled);
    }

    [Theory]
    [InlineData(4_096, 16)]
    [InlineData(65_536, 16)]
    [InlineData(65_536, 256)]
    public void LinkedPool_RentSpanAdvanceReturn_LeavesPooledWriterReset(int totalBytes, int chunkSize)
    {
        var bench = CreateBench(totalBytes, chunkSize);

        bench.LinkedPool_RentSpanAdvanceReturn();

        var pooled = ReusableLinkedArrayBufferWriterPool.Rent();
        Assert.Equal(0, pooled.TotalWritten);
        ReusableLinkedArrayBufferWriterPool.Return(pooled);
    }

    private static ReusableLinkedArrayBufferWriterVsResizableByteWriterBench CreateBench(int totalBytes, int chunkSize)
    {
        var bench = new ReusableLinkedArrayBufferWriterVsResizableByteWriterBench
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
}
