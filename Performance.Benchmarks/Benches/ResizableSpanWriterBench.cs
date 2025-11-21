using BenchmarkDotNet.Attributes;
using Performance.Benchmarks.Original;
using Performance.Buffers;

namespace Performance.Benchmarks.Benches;

[MemoryDiagnoser]
[HideColumns("Error", "StdDev", "Median")]
public class ResizableSpanWriterBench
{
    [Params(256, 4_096, 65_536)]
    public int Size;

    private int[] _source = null!;
    private OriginalResizableSpanWriter<int> _oldWriter = null!;
    private ResizableSpanWriter<int> _newWriter = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _source = new int[Size];
        for (int i = 0; i < _source.Length; i++)
        {
            _source[i] = i;
        }

        _oldWriter = new();
        _newWriter = new();
    }

    [IterationCleanup]
    public void Cleanup()
    {
        _oldWriter.Reset();
        _newWriter.Reset();
    }

    [Benchmark(Baseline = true, Description = "Old.Write(T[])")]
    public void Old_WriteArray()
    {
        _oldWriter.Write(_source);
    }

    [Benchmark(Description = "New.Write(T[])")]
    public void New_WriteArray()
    {
        _newWriter.Write(_source);
    }

    [Benchmark(Description = "Old.WriteSpanAdvance")]
    public void Old_WriteSpanAdvance()
    {
        var span = _oldWriter.GetSpan(_source.Length);
        _source.CopyTo(span);
        _oldWriter.Advance(_source.Length);
    }

    [Benchmark(Description = "New.WriteSpanAdvance")]
    public void New_WriteSpanAdvance()
    {
        var span = _newWriter.GetSpan(_source.Length);
        _source.CopyTo(span);
        _newWriter.Advance(_source.Length);
    }

    [Benchmark(Description = "Old.ResetReuse")]
    public void Old_ResetReuse()
    {
        _oldWriter.Write(_source);
        _oldWriter.Reset();
        _oldWriter.Write(_source);
    }

    [Benchmark(Description = "New.ResetReuse")]
    public void New_ResetReuse()
    {
        _newWriter.Write(_source);
        _newWriter.Reset();
        _newWriter.Write(_source);
    }

    [Benchmark(Description = "Old.WriteSingles")]
    public void Old_WriteSingles()
    {
        for (int i = 0; i < _source.Length; i++)
        {
            _oldWriter.Write(_source[i]);
        }
    }

    [Benchmark(Description = "New.WriteSingles")]
    public void New_WriteSingles()
    {
        for (int i = 0; i < _source.Length; i++)
        {
            _newWriter.Write(_source[i]);
        }
    }
}
