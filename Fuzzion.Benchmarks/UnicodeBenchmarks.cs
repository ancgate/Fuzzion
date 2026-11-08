using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Fuzzion.PreProcess;

namespace Fuzzion.Benchmarks;

/// <summary>
/// Phase 7 Unicode support (plan items #31, #32): the preprocessors and code-point
/// comparison, on text with accents and emoji.
/// <c>dotnet run -c Release --project Fuzzion.Benchmarks -- --filter *UnicodeBenchmarks*</c>
/// </summary>
[MemoryDiagnoser]
public class UnicodeBenchmarks
{
    private static readonly string[] Pieces = { "café", "crème", "Straße", "naïve", "😀", "Ångström", "über", "İstanbul", "x²", "word" };
    private string _a = "";
    private string _b = "";

    [Params(32, 256)]
    public int Length { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(17);
        string Make() => string.Join(" ", Enumerable.Range(0, Length).Select(_ => Pieces[random.Next(Pieces.Length)])).Substring(0, Length);
        _a = Make();
        _b = Make();
    }

    [Benchmark(Baseline = true)]
    public string Full() => StringPreprocessor.Full(_a);

    [Benchmark]
    public string FullUnicode() => StringPreprocessor.FullUnicode(_a);

    [Benchmark]
    public string RemoveDiacritics() => StringPreprocessor.RemoveDiacritics(_a);

    [Benchmark]
    public double RatioUtf16() => Fuzz.Ratio(_a, _b);

    [Benchmark]
    public double RatioCodePoints() => CodePoints.Compare(_a, _b, Fuzz.Ratio);
}
