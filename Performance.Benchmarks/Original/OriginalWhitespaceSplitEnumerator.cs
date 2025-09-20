using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Performance.Benchmarks.Whitespace;

public ref struct OriginalWhitespaceSplitEnumerator
{
    private readonly ReadOnlySpan<char> _span;
    private int _index;

    public OriginalWhitespaceSplitEnumerator(ReadOnlySpan<char> span)
    {
        _span = span;
        _index = 0;
        Current = default;
    }

    public ReadOnlySpan<char> Current { get; private set; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly OriginalWhitespaceSplitEnumerator GetEnumerator() => this;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext()
    {
        while (_index < _span.Length && char.IsWhiteSpace(_span[_index]))
            _index++;

        if (_index >= _span.Length)
            return false;

        int start = _index;

        while (_index < _span.Length && !char.IsWhiteSpace(_span[_index]))
            _index++;

        Current = _span[start.._index];
        return true;
    }
}

//// * Summary *

//BenchmarkDotNet v0.15.3, Windows 11 (10.0.26100.6584/24H2/2024Update/HudsonValley)
//13th Gen Intel Core i9-13950HX 2.20GHz, 1 CPU, 32 logical and 24 physical cores
//.NET SDK 10.0.100-rc.1.25451.107
//  [Host]  : .NET 10.0.0 (10.0.0-rc.1.25451.107, 10.0.25.45207), X64 RyuJIT x86-64-v3
//  Default : .NET 10.0.0 (10.0.0-rc.1.25451.107, 10.0.25.45207), X64 RyuJIT x86-64-v3

//Job = Default

//| Method | Length | WhitespaceRatio | UnicodeAware | Mean | Ratio | RatioSD | Allocated | Alloc Ratio |
//|-------------------- |------- |---------------- |------------- |-------------:|------:|--------:|----------:|------------:|
//| OriginalEnumerator  | 1000   | 0.1             | False        |     416.4 ns |  1.00 |    0.01 |         - |          NA |
//| OptimizedEnumerator | 1000   | 0.1             | False        |     128.3 ns |  0.31 |    0.01 |         - |          NA |
//|                     |        |                 |              |              |       |         |           |             |
//| OriginalEnumerator  | 1000   | 0.1             | True         |     431.2 ns |  1.00 |    0.02 |         - |          NA |
//| OptimizedEnumerator | 1000   | 0.1             | True         |     439.4 ns |  1.02 |    0.02 |         - |          NA |
//|                     |        |                 |              |              |       |         |           |             |
//| OriginalEnumerator  | 1000   | 0.3             | False        |     531.9 ns |  1.00 |    0.03 |         - |          NA |
//| OptimizedEnumerator | 1000   | 0.3             | False        |     473.6 ns |  0.89 |    0.02 |         - |          NA |
//|                     |        |                 |              |              |       |         |           |             |
//| OriginalEnumerator  | 1000   | 0.3             | True         |     526.4 ns |  1.00 |    0.02 |         - |          NA |
//| OptimizedEnumerator | 1000   | 0.3             | True         |     622.6 ns |  1.18 |    0.02 |         - |          NA |
//|                     |        |                 |              |              |       |         |           |             |
//| OriginalEnumerator  | 1000   | 0.5             | False        |     536.0 ns |  1.00 |    0.03 |         - |          NA |
//| OptimizedEnumerator | 1000   | 0.5             | False        |     801.0 ns |  1.50 |    0.03 |         - |          NA |
//|                     |        |                 |              |              |       |         |           |             |
//| OriginalEnumerator  | 1000   | 0.5             | True         |     521.1 ns |  1.00 |    0.02 |         - |          NA |
//| OptimizedEnumerator | 1000   | 0.5             | True         |     691.6 ns |  1.33 |    0.03 |         - |          NA |
//|                     |        |                 |              |              |       |         |           |             |
//| OriginalEnumerator  | 10000  | 0.1             | False        |   4,433.1 ns |  1.00 |    0.02 |         - |          NA |
//| OptimizedEnumerator | 10000  | 0.1             | False        |   1,913.8 ns |  0.43 |    0.01 |         - |          NA |
//|                     |        |                 |              |              |       |         |           |             |
//| OriginalEnumerator  | 10000  | 0.1             | True         |   4,848.0 ns |  1.00 |    0.03 |         - |          NA |
//| OptimizedEnumerator | 10000  | 0.1             | True         |   5,220.6 ns |  1.08 |    0.03 |         - |          NA |
//|                     |        |                 |              |              |       |         |           |             |
//| OriginalEnumerator  | 10000  | 0.3             | False        |   5,460.8 ns |  1.00 |    0.03 |         - |          NA |
//| OptimizedEnumerator | 10000  | 0.3             | False        |   5,255.5 ns |  0.96 |    0.02 |         - |          NA |
//|                     |        |                 |              |              |       |         |           |             |
//| OriginalEnumerator  | 10000  | 0.3             | True         |   4,941.9 ns |  1.00 |    0.02 |         - |          NA |
//| OptimizedEnumerator | 10000  | 0.3             | True         |   6,255.8 ns |  1.27 |    0.02 |         - |          NA |
//|                     |        |                 |              |              |       |         |           |             |
//| OriginalEnumerator  | 10000  | 0.5             | False        |   5,296.0 ns |  1.00 |    0.02 |         - |          NA |
//| OptimizedEnumerator | 10000  | 0.5             | False        |   8,356.1 ns |  1.58 |    0.02 |         - |          NA |
//|                     |        |                 |              |              |       |         |           |             |
//| OriginalEnumerator  | 10000  | 0.5             | True         |   5,298.4 ns |  1.00 |    0.02 |         - |          NA |
//| OptimizedEnumerator | 10000  | 0.5             | True         |   7,180.4 ns |  1.36 |    0.03 |         - |          NA |
//|                     |        |                 |              |              |       |         |           |             |
//| OriginalEnumerator  | 100000 | 0.1             | False        |  67,488.0 ns |  1.00 |    0.02 |         - |          NA |
//| OptimizedEnumerator | 100000 | 0.1             | False        |  18,329.5 ns |  0.27 |    0.01 |         - |          NA |
//|                     |        |                 |              |              |       |         |           |             |
//| OriginalEnumerator  | 100000 | 0.1             | True         |  66,460.2 ns |  1.00 |    0.02 |         - |          NA |
//| OptimizedEnumerator | 100000 | 0.1             | True         |  88,099.3 ns |  1.33 |    0.02 |         - |          NA |
//|                     |        |                 |              |              |       |         |           |             |
//| OriginalEnumerator  | 100000 | 0.3             | False        | 123,290.4 ns |  1.00 |    0.02 |         - |          NA |
//| OptimizedEnumerator | 100000 | 0.3             | False        |  90,054.4 ns |  0.73 |    0.01 |         - |          NA |
//|                     |        |                 |              |              |       |         |           |             |
//| OriginalEnumerator  | 100000 | 0.3             | True         | 135,527.8 ns |  1.00 |    0.02 |         - |          NA |
//| OptimizedEnumerator | 100000 | 0.3             | True         | 151,560.6 ns |  1.12 |    0.02 |         - |          NA |
//|                     |        |                 |              |              |       |         |           |             |
//| OriginalEnumerator  | 100000 | 0.5             | False        | 171,907.1 ns |  1.00 |    0.01 |         - |          NA |
//| OptimizedEnumerator | 100000 | 0.5             | False        | 164,038.8 ns |  0.95 |    0.01 |         - |          NA |
//|                     |        |                 |              |              |       |         |           |             |
//| OriginalEnumerator  | 100000 | 0.5             | True         | 173,875.1 ns |  1.00 |    0.01 |         - |          NA |
//| OptimizedEnumerator | 100000 | 0.5             | True         | 202,653.6 ns |  1.17 |    0.02 |         - |          NA |

//// * Hints *
//HideColumnsAnalyser
//  Summary -> Hidden columns: Error, StdDev
//Outliers
//  WhitespaceSplitBench.OriginalEnumerator: Default  -> 1 outlier was  removed(431.72 ns)
//  WhitespaceSplitBench.OriginalEnumerator: Default  -> 1 outlier was  removed(575.97 ns)
//  WhitespaceSplitBench.OptimizedEnumerator: Default -> 1 outlier was  detected(792.81 ns)
//  WhitespaceSplitBench.OriginalEnumerator: Default  -> 3 outliers were removed(5.26 us..6.55 us)
//  WhitespaceSplitBench.OptimizedEnumerator: Default -> 1 outlier was  removed, 2 outliers were detected(8.22 us, 8.71 us)
//  WhitespaceSplitBench.OptimizedEnumerator: Default -> 1 outlier was  removed(19.76 us)
//  WhitespaceSplitBench.OriginalEnumerator: Default  -> 2 outliers were detected(119.35 us, 119.50 us)
//  WhitespaceSplitBench.OptimizedEnumerator: Default -> 1 outlier was  detected(87.06 us)
//  WhitespaceSplitBench.OriginalEnumerator: Default  -> 1 outlier was  detected(131.37 us)
//  WhitespaceSplitBench.OptimizedEnumerator: Default -> 1 outlier was  detected(160.86 us)
//  WhitespaceSplitBench.OriginalEnumerator: Default  -> 1 outlier was  removed(180.30 us)
//  WhitespaceSplitBench.OptimizedEnumerator: Default -> 1 outlier was  detected(197.30 us)

//// * Legends *
//  Length          : Value of the 'Length' parameter
//  WhitespaceRatio : Value of the 'WhitespaceRatio' parameter
//  UnicodeAware    : Value of the 'UnicodeAware' parameter
//  Mean            : Arithmetic mean of all measurements
//  Ratio           : Mean of the ratio distribution([Current]/[Baseline])
//  RatioSD         : Standard deviation of the ratio distribution([Current]/[Baseline])
//  Allocated       : Allocated memory per single operation(managed only, inclusive, 1KB = 1024B)
//  Alloc Ratio     : Allocated memory ratio distribution([Current]/[Baseline])
//  1 ns            : 1 Nanosecond(0.000000001 sec)

//// * Diagnostic Output - MemoryDiagnoser *


//// ***** BenchmarkRunner: End *****
//Run time: 00:11:00 (660.44 sec), executed benchmarks: 36

//Global total time: 00:11:08 (668.8 sec), executed benchmarks: 36
//// * Artifacts cleanup *
//Artifacts cleanup is finished