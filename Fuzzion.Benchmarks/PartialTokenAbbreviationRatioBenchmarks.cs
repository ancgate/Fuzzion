using BenchmarkDotNet.Attributes;
using Fuzzion.PreProcess;
using Classic = FuzzySharp;

namespace Fuzzion.Benchmarks;

[MemoryDiagnoser]
[RankColumn]
public class PartialTokenAbbreviationRatioBenchmarks
{
    [Benchmark]
    public double PartialTokenAbbreviationRatio()
    {
        return Fuzz.PartialTokenAbbreviationRatio("bl 420", "Baseline section 420", StringPreprocessor.Full);
    }

    [Benchmark]
    public double PartialTokenAbbreviationRatioClassic()
    {
        return Classic.Fuzz.PartialTokenAbbreviationRatio("bl 420", "Baseline section 420", Classic.PreProcess.PreprocessMode.Full);
    }
}
