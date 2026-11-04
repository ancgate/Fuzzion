using System;
using BenchmarkDotNet.Attributes;

namespace Fuzzion.Benchmarks;

/// <summary>
/// Levenshtein with a small cutoff or a hint (plan item #30) against the full distance,
/// on close strings (a few edits apart) and on unrelated strings of 1,000 characters.
/// </summary>
[MemoryDiagnoser]
public class LevenshteinCutoffBenchmarks
{
    private string _a = "";
    private string _close = "";
    private string _far = "";

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(30);
        char[] Make(int n) { var c = new char[n]; for (var i = 0; i < n; i++) c[i] = (char)('a' + random.Next(26)); return c; }

        var chars = Make(1000);
        _a = new string(chars);
        chars[100] = '#';
        chars[500] = '#';
        chars[900] = '#';
        _close = new string(chars);
        _far = new string(Make(1000));
    }

    [Benchmark(Baseline = true)]
    public int CloseFull() => Levenshtein.Distance(_a, _close);

    [Benchmark]
    public int CloseCutoff5() => Levenshtein.Distance(_a, _close, scoreCutoff: 5);

    [Benchmark]
    public int CloseHint3() => Levenshtein.Distance(_a, _close, scoreHint: 3);

    [Benchmark]
    public int FarFull() => Levenshtein.Distance(_a, _far);

    [Benchmark]
    public int FarCutoff5() => Levenshtein.Distance(_a, _far, scoreCutoff: 5);
}
