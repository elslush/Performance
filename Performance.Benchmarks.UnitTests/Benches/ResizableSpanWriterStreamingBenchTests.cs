using System.Reflection;

using Performance.Benchmarks.Benches;
using Performance.Benchmarks.Original;
using Performance.Buffers;

using Xunit;

namespace Performance.Benchmarks.Benches.UnitTests;

public class ResizableSpanWriterStreamingBenchTests
{
    [Fact]
    public void GlobalSetup_InitializesWriters_AndLeavesThemEmptyAfterWarmup()
    {
        var bench = new ResizableSpanWriterStreamingBench
        {
            TotalItems = 65_536,
            ChunkSize = 16
        };

        bench.GlobalSetup();

        var oldWriter = GetField<OriginalResizableSpanWriter<int>>(bench, "_oldWriter");
        var newWriter = GetField<ResizableSpanWriter<int>>(bench, "_newWriter");
        var source = GetField<int[]>(bench, "_source");

        Assert.NotNull(source);
        Assert.Equal(65_536, source.Length);
        Assert.Equal(0, oldWriter.WrittenSpan.Length);
        Assert.Equal(0, newWriter.WrittenSpan.Length);
    }

    [Theory]
    [InlineData(4_096, 16)]
    [InlineData(65_536, 16)]
    [InlineData(65_536, 4_096)]
    public void New_WriteChunked_WritesExpectedItemCount_AndCleanupResets(int totalItems, int chunkSize)
    {
        var bench = new ResizableSpanWriterStreamingBench
        {
            TotalItems = totalItems,
            ChunkSize = chunkSize
        };

        bench.GlobalSetup();
        bench.New_WriteChunked();

        var newWriter = GetField<ResizableSpanWriter<int>>(bench, "_newWriter");
        Assert.Equal(totalItems, newWriter.WrittenSpan.Length);

        bench.Cleanup();
        Assert.Equal(0, newWriter.WrittenSpan.Length);
    }

    [Theory]
    [InlineData(4_096, 16)]
    [InlineData(65_536, 16)]
    [InlineData(65_536, 256)]
    public void New_WriteSpanAdvanceChunked_WritesExpectedItemCount_AndCleanupResets(int totalItems, int chunkSize)
    {
        var bench = new ResizableSpanWriterStreamingBench
        {
            TotalItems = totalItems,
            ChunkSize = chunkSize
        };

        bench.GlobalSetup();
        bench.New_WriteSpanAdvanceChunked();

        var newWriter = GetField<ResizableSpanWriter<int>>(bench, "_newWriter");
        Assert.Equal(totalItems, newWriter.WrittenSpan.Length);

        bench.Cleanup();
        Assert.Equal(0, newWriter.WrittenSpan.Length);
    }

    [Theory]
    [InlineData(4_096, 16)]
    [InlineData(65_536, 16)]
    [InlineData(65_536, 256)]
    public void Old_WriteChunked_WritesExpectedItemCount_AndCleanupResets(int totalItems, int chunkSize)
    {
        var bench = new ResizableSpanWriterStreamingBench
        {
            TotalItems = totalItems,
            ChunkSize = chunkSize
        };

        bench.GlobalSetup();
        bench.Old_WriteChunked();

        var oldWriter = GetField<OriginalResizableSpanWriter<int>>(bench, "_oldWriter");
        Assert.Equal(totalItems, oldWriter.WrittenSpan.Length);

        bench.Cleanup();
        Assert.Equal(0, oldWriter.WrittenSpan.Length);
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
