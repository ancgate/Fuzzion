using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Fuzzion.Benchmarks;

/// <summary>
/// Damerau-Levenshtein (P8.3): the string path (array of last rows for characters below
/// 256) against the generic path (dictionary), with Levenshtein for scale.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0)]
public class DamerauLevenshteinBenchmarks
{
    private string _a = "";
    private string _b = "";

    [Params(8, 64, 256)]
    public int Length { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(42);
        var chars = new char[Length];
        for (var i = 0; i < Length; i++) chars[i] = (char)('a' + random.Next(26));
        _a = new string(chars);
        for (var i = 0; i < Length; i += 8) chars[i] = (char)('a' + random.Next(26));
        _b = new string(chars);
    }

    [Benchmark(Baseline = true)]
    public int StringPath() => DamerauLevenshtein.Distance(_a, _b);

    [Benchmark]
    public int GenericPath() => DamerauLevenshtein.Distance<char>(_a.AsSpan(), _b.AsSpan());

    [Benchmark]
    public int Levenshtein_() => Levenshtein.Distance(_a, _b);
}
