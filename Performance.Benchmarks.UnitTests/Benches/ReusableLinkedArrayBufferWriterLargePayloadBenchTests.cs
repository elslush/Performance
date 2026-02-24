using System.Reflection;

using Performance.Benchmarks.Benches;
using Performance.Buffers;

using Xunit;

namespace Performance.Benchmarks.Benches.UnitTests;

public class ReusableLinkedArrayBufferWriterLargePayloadBenchTests
{
    [Fact]
    public void GlobalSetup_InitializesSource_Writers_AndSinks()
    {
        var bench = new ReusableLinkedArrayBufferWriterLargePayloadBench
        {
            TotalBytes = 262_144,
            ChunkSize = 4_096
        };

        bench.GlobalSetup();

        var source = GetField<byte[]>(bench, "_source");
        var resizable = GetField<ResizableByteWriter>(bench, "_resizableWriter");
        var linked = GetField<ReusableLinkedArrayBufferWriter>(bench, "_linkedWriter");
        var streamSink = GetField<object>(bench, "_streamSink");
        var segmentSink = GetField<object>(bench, "_segmentSink");

        Assert.Equal(262_144, source.Length);
        Assert.Equal(0, resizable.WrittenSpan.Length);
        Assert.Equal(0, linked.TotalWritten);
        Assert.Equal(0L, GetProperty<long>(streamSink, "BytesWritten"));
        Assert.Equal(0, GetProperty<int>(streamSink, "WriteCalls"));
        Assert.Equal(0L, GetProperty<long>(segmentSink, "BytesConsumed"));
        Assert.Equal(0, GetProperty<int>(segmentSink, "SegmentsConsumed"));
    }

    [Theory]
    [InlineData(262_144, 4_096)]
    [InlineData(1_048_576, 65_536)]
    public async Task Resizable_FillThenSingleFlushAsync_WritesExpectedBytes_OneWriteCall_AndResetsWriter(
        int totalBytes,
        int chunkSize)
    {
        var bench = CreateBench(totalBytes, chunkSize);

        await bench.Resizable_FillThenSingleFlushAsync();

        var streamSink = GetField<object>(bench, "_streamSink");
        var resizable = GetField<ResizableByteWriter>(bench, "_resizableWriter");
        Assert.Equal(totalBytes, GetProperty<long>(streamSink, "BytesWritten"));
        Assert.Equal(1, GetProperty<int>(streamSink, "WriteCalls"));
        Assert.Equal(0, resizable.WrittenSpan.Length);
    }

    [Theory]
    [InlineData(262_144, 4_096)]
    [InlineData(1_048_576, 65_536)]
    public async Task Linked_FillThenSegmentFlushAsync_WritesExpectedBytes_MultipleOrSingleWriteCalls_AndResetsWriter(
        int totalBytes,
        int chunkSize)
    {
        var bench = CreateBench(totalBytes, chunkSize);

        await bench.Linked_FillThenSegmentFlushAsync();

        var streamSink = GetField<object>(bench, "_streamSink");
        var linked = GetField<ReusableLinkedArrayBufferWriter>(bench, "_linkedWriter");
        Assert.Equal(totalBytes, GetProperty<long>(streamSink, "BytesWritten"));
        Assert.True(GetProperty<int>(streamSink, "WriteCalls") >= 1);
        Assert.Equal(0, linked.TotalWritten);
    }

    [Theory]
    [InlineData(262_144, 4_096)]
    [InlineData(1_048_576, 65_536)]
    public async Task Linked_FillThenToArraySingleFlushAsync_WritesExpectedBytes_OneWriteCall_AndResetsWriter(
        int totalBytes,
        int chunkSize)
    {
        var bench = CreateBench(totalBytes, chunkSize);

        await bench.Linked_FillThenToArraySingleFlushAsync();

        var streamSink = GetField<object>(bench, "_streamSink");
        var linked = GetField<ReusableLinkedArrayBufferWriter>(bench, "_linkedWriter");
        Assert.Equal(totalBytes, GetProperty<long>(streamSink, "BytesWritten"));
        Assert.Equal(1, GetProperty<int>(streamSink, "WriteCalls"));
        Assert.Equal(0, linked.TotalWritten);
    }

    [Theory]
    [InlineData(262_144, 4_096)]
    [InlineData(1_048_576, 65_536)]
    public void Resizable_FillThenSegmentConsume_ConsumesExpectedBytes_AsSingleSegment(
        int totalBytes,
        int chunkSize)
    {
        var bench = CreateBench(totalBytes, chunkSize);

        bench.Resizable_FillThenSegmentConsume();

        var segmentSink = GetField<object>(bench, "_segmentSink");
        var resizable = GetField<ResizableByteWriter>(bench, "_resizableWriter");
        Assert.Equal(totalBytes, GetProperty<long>(segmentSink, "BytesConsumed"));
        Assert.Equal(1, GetProperty<int>(segmentSink, "SegmentsConsumed"));
        Assert.Equal(0, resizable.WrittenSpan.Length);
    }

    [Theory]
    [InlineData(262_144, 4_096)]
    [InlineData(1_048_576, 65_536)]
    public void Linked_FillThenSegmentConsume_ConsumesExpectedBytes_InAtLeastOneSegment(
        int totalBytes,
        int chunkSize)
    {
        var bench = CreateBench(totalBytes, chunkSize);

        bench.Linked_FillThenSegmentConsume();

        var segmentSink = GetField<object>(bench, "_segmentSink");
        var linked = GetField<ReusableLinkedArrayBufferWriter>(bench, "_linkedWriter");
        Assert.Equal(totalBytes, GetProperty<long>(segmentSink, "BytesConsumed"));
        Assert.True(GetProperty<int>(segmentSink, "SegmentsConsumed") >= 1);
        Assert.Equal(0, linked.TotalWritten);
    }

    [Fact]
    public async Task Cleanup_ResetsWriters_AndSinks()
    {
        var bench = CreateBench(262_144, 4_096);
        await bench.Resizable_FillThenSingleFlushAsync();
        bench.Linked_FillThenSegmentConsume();

        bench.Cleanup();

        var resizable = GetField<ResizableByteWriter>(bench, "_resizableWriter");
        var linked = GetField<ReusableLinkedArrayBufferWriter>(bench, "_linkedWriter");
        var streamSink = GetField<object>(bench, "_streamSink");
        var segmentSink = GetField<object>(bench, "_segmentSink");

        Assert.Equal(0, resizable.WrittenSpan.Length);
        Assert.Equal(0, linked.TotalWritten);
        Assert.Equal(0L, GetProperty<long>(streamSink, "BytesWritten"));
        Assert.Equal(0, GetProperty<int>(streamSink, "WriteCalls"));
        Assert.Equal(0L, GetProperty<long>(segmentSink, "BytesConsumed"));
        Assert.Equal(0, GetProperty<int>(segmentSink, "SegmentsConsumed"));
    }

    private static ReusableLinkedArrayBufferWriterLargePayloadBench CreateBench(int totalBytes, int chunkSize)
    {
        var bench = new ReusableLinkedArrayBufferWriterLargePayloadBench
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

    private static T GetProperty<T>(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(property);

        var value = property!.GetValue(instance);
        Assert.NotNull(value);
        Assert.IsType<T>(value);
        return (T)value!;
    }
}
