using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Fuzzion.Utils;

namespace Fuzzion.Benchmarks;

/// <summary>
/// Process.Cdist with plain Ratio: SIMD batch scoring (plan item #33) against the scalar
/// path, for 100 queries × 2,000 choices of different lengths.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0)]
public class CdistBenchmarks
{
    private string[] _queries = [];
    private string[] _choices = [];

    [Params(8, 16, 32, 64)]
    public int ChoiceLength { get; set; }

    [Params(true, false)]
    public bool Simd { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(24);
        string Make(int n) => new(Enumerable.Range(0, n).Select(_ => (char)('a' + random.Next(26))).ToArray());
        _queries = Enumerable.Range(0, 100).Select(_ => Make(ChoiceLength)).ToArray();
        _choices = Enumerable.Range(0, 2000).Select(_ => Make(random.Next(ChoiceLength / 2 + 1, ChoiceLength + 1))).ToArray();
        BatchRatio.Disabled = !Simd;
    }

    [GlobalCleanup]
    public void Cleanup() => BatchRatio.Disabled = false;

    [Benchmark]
    public double[,] CdistRatio() => Process.Cdist(_queries, _choices);
}
