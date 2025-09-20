using BenchmarkDotNet.Attributes;
using Performance.Benchmarks.Original;
using Performance.Buffers;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Performance.Benchmarks.Benches;

[MemoryDiagnoser]
public class ResizableByteWriterBench
{
    // -----------------------------------------------------------------
    // Size of the payload we will write in each iteration.
    // Feel free to add more values (e.g. 1_024, 65_536, 1_048_576, …)
    // -----------------------------------------------------------------
    [Params(256, 4_096, 65_536, 1_048_576)]
    public int Size;

    private byte[] _source = null!; // filled in GlobalSetup

    // -----------------------------------------------------------------
    // One instance of each writer per benchmark run (so we measure only the
    // cost of the write, not the allocation of the writer itself).
    // -----------------------------------------------------------------
    private OriginalResizableByteWriter _oldWriter = null!;
    private ResizableByteWriter _newWriter = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _source = new byte[Size];
        // deterministic data – helps the JIT see that the array is constant‑filled.
        new Random(42).NextBytes(_source);

        _oldWriter = new();
        _newWriter = new();
    }

    [IterationCleanup]
    public void Cleanup()
    {
        // Reset writers so each iteration starts from a clean state.
        _oldWriter.Reset();
        _newWriter.Reset();
    }

    // -----------------------------------------------------------------
    // 1️⃣ Write using the classic Stream.Write(byte[], offset, count) method.
    // -----------------------------------------------------------------
    [Benchmark(Baseline = true, Description = "Old.Write(byte[])")]
    public void Old_WriteByteArray()
    {
        _oldWriter.Write(_source, 0, _source.Length);
    }

    [Benchmark(Description = "New.Write(byte[])")]
    public void New_WriteByteArray()
    {
        _newWriter.Write(_source, 0, _source.Length);
    }

    // -----------------------------------------------------------------
    // 2️⃣ Write using GetSpan/Advance (IBufferWriter pattern)
    // -----------------------------------------------------------------
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

    // -----------------------------------------------------------------
    // 3️⃣ Write, Reset, Write again – shows the cost of re‑using the buffer.
    // -----------------------------------------------------------------
    [Benchmark(Description = "Old.ResetReuse")]
    public void Old_ResetReuse()
    {
        _oldWriter.Write(_source, 0, _source.Length);
        _oldWriter.Reset();
        _oldWriter.Write(_source, 0, _source.Length);
    }

    [Benchmark(Description = "New.ResetReuse")]
    public void New_ResetReuse()
    {
        _newWriter.Write(_source, 0, _source.Length);
        _newWriter.Reset();
        _newWriter.Write(_source, 0, _source.Length);
    }
}
