using System;
using BenchmarkDotNet.Attributes;

namespace Fuzzion.Benchmarks;

/// <summary>
/// Phase 2 distance metrics (Hamming, Jaro, Jaro-Winkler, OSA, Damerau-Levenshtein,
/// Prefix) on random similar strings of different lengths, with Levenshtein as the
/// baseline. Run with:
/// <c>dotnet run -c Release --project Fuzzion.Benchmarks -- --filter *DistanceMetricsBenchmarks*</c>
/// </summary>
[MemoryDiagnoser]
public class DistanceMetricsBenchmarks
{
    private string _a = "";
    private string _b = "";

    [Params(8, 64, 256, 1024)]
    public int Length { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(42);
        var chars = new char[Length];
        for (var i = 0; i < Length; i++)
        {
            chars[i] = (char)('a' + random.Next(26));
        }

        _a = new string(chars);

        // A close variant: about one change in eight characters.
        for (var i = 0; i < Length; i += 8)
        {
            chars[random.Next(i, Math.Min(Length, i + 8))] = (char)('a' + random.Next(26));
        }

        _b = new string(chars);
    }

    [Benchmark(Baseline = true)]
    public int Levenshtein_() => Levenshtein.Distance(_a, _b);

    [Benchmark]
    public int Hamming_() => Hamming.Distance(_a, _b);

    [Benchmark]
    public double Jaro_() => Jaro.Similarity(_a, _b);

    [Benchmark]
    public double JaroWinkler_() => JaroWinkler.Similarity(_a, _b);

    [Benchmark]
    public int Osa_() => Osa.Distance(_a, _b);

    [Benchmark]
    public int DamerauLevenshtein_() => DamerauLevenshtein.Distance(_a, _b);

    [Benchmark]
    public int Prefix_() => Prefix.Similarity(_a, _b);
}
