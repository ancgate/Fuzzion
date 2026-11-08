using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Fuzzion.SimilarityRatio;
using Fuzzion.SimilarityRatio.Scorer.StrategySensitive;
using Fuzzion.Utils;

namespace Fuzzion.Benchmarks;

/// <summary>
/// Cdist with Scorers.Levenshtein: SIMD on versus off. (Batching Extract* for a single
/// query was also measured and was 1.2-2x slower, because the per-choice bit tables cannot
/// be reused; it was not shipped.)
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0)]
public class BatchSearchBenchmarks
{
    private string[] _queries = [];
    private string[] _choices = [];

    [Params(true, false)]
    public bool Simd { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(9);
        string Make(int n) => new(Enumerable.Range(0, n).Select(_ => (char)('a' + random.Next(26))).ToArray());
        _queries = Enumerable.Range(0, 100).Select(_ => Make(16)).ToArray();
        _choices = Enumerable.Range(0, 2000).Select(_ => Make(random.Next(8, 17))).ToArray();
        BatchRatio.Disabled = !Simd;
    }

    [GlobalCleanup]
    public void Cleanup() => BatchRatio.Disabled = false;

    [Benchmark]
    public double[,] CdistLevenshtein() => Process.Cdist(_queries, _choices, Scorers.Levenshtein());
}

/// <summary>Jaro: static calls and a cached instance, against Levenshtein for scale.</summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0)]
public class JaroBenchmarks
{
    private string _a = "";
    private string _b = "";
    private Jaro _cached = null!;

    [Params(8, 64, 256, 1024)]
    public int Length { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(42);
        var chars = new char[Length];
        for (var i = 0; i < Length; i++) chars[i] = (char)('a' + random.Next(26));
        _a = new string(chars);
        for (var i = 0; i < Length; i += 8) chars[random.Next(i, Math.Min(Length, i + 8))] = (char)('a' + random.Next(26));
        _b = new string(chars);
        _cached = new Jaro(_a);
    }

    [GlobalCleanup]
    public void Cleanup() => _cached.Dispose();

    [Benchmark(Baseline = true)]
    public int Levenshtein_() => Levenshtein.Distance(_a, _b);

    [Benchmark]
    public double JaroStatic() => Jaro.Similarity(_a, _b);

    [Benchmark]
    public double JaroCached() => _cached.SimilarityWith(_b);
}
