using System;
using BenchmarkDotNet.Attributes;

namespace Fuzzion.Benchmarks;

/// <summary>
/// Phase 4 set-based metrics on random similar strings, with Levenshtein as the baseline.
/// <c>dotnet run -c Release --project Fuzzion.Benchmarks -- --filter *SetMetricsBenchmarks*</c>
/// </summary>
[MemoryDiagnoser]
public class SetMetricsBenchmarks
{
    private static readonly QGramOptions Trigrams = new(q: 3, pad: true);
    private string _a = "";
    private string _b = "";

    [Params(16, 64, 256)]
    public int Length { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(7);
        var chars = new char[Length];
        for (var i = 0; i < Length; i++) chars[i] = (char)('a' + random.Next(26));
        _a = new string(chars);
        for (var i = 0; i < Length; i += 8) chars[random.Next(i, Math.Min(Length, i + 8))] = (char)('a' + random.Next(26));
        _b = new string(chars);
    }

    [Benchmark(Baseline = true)]
    public int Levenshtein_() => Levenshtein.Distance(_a, _b);

    [Benchmark]
    public double JaccardBigrams() => Jaccard.Similarity(_a, _b);

    [Benchmark]
    public double JaccardPaddedTrigrams() => Jaccard.Similarity(_a, _b, Trigrams);

    [Benchmark]
    public double Tanimoto_() => Tanimoto.Similarity(_a, _b);

    [Benchmark]
    public double RatcliffObershelp_() => RatcliffObershelp.Similarity(_a, _b);

    [Benchmark]
    public int LongestCommonSubstring_() => LongestCommonSubstring.Similarity(_a, _b);
}
