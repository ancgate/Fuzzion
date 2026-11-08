using System;
using BenchmarkDotNet.Attributes;
using Fuzzion.PreProcess;

namespace Fuzzion.Benchmarks;

/// <summary>
/// Phase 3 scorers that have no benchmark class of their own (plan items #17-#23), with
/// <c>Fuzz.Ratio</c> as the baseline. Inputs are sentences of random words that share
/// most words in a different order.
/// <c>dotnet run -c Release --project Fuzzion.Benchmarks -- --filter *TokenScorerParityBenchmarks*</c>
/// </summary>
[MemoryDiagnoser]
public class TokenScorerParityBenchmarks
{
    private string _a = "";
    private string _b = "";

    [Params(32, 128)]
    public int Length { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(13);
        _a = BenchmarkStrings.Words(random, Length);
        var words = _a.Split(' ');
        Array.Reverse(words);
        words[0] = "extra";
        _b = string.Join(" ", words);
    }

    [Benchmark(Baseline = true)]
    public double Ratio() => Fuzz.Ratio(_a, _b);

    [Benchmark]
    public double QRatio() => Fuzz.QRatio(_a, _b);

    [Benchmark]
    public double TokenRatio() => Fuzz.TokenRatio(_a, _b);

    [Benchmark]
    public double PartialTokenRatio() => Fuzz.PartialTokenRatio(_a, _b);

    [Benchmark]
    public double TokenDifferenceRatio() => Fuzz.TokenDifferenceRatio(_a, _b);

    [Benchmark]
    public double PartialTokenDifferenceRatio() => Fuzz.PartialTokenDifferenceRatio(_a, _b);

    [Benchmark]
    public double RatioWithCutoff() => Fuzz.Ratio(_a, _b, StringPreprocessor.None, scoreCutoff: 90);
}
