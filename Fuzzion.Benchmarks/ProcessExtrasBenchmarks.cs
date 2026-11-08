using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace Fuzzion.Benchmarks;

/// <summary>
/// Phase 5 additions that have no benchmark class of their own (plan items #24, #25, #27):
/// <c>Process.Dedupe</c>, <c>Process.Cpdist</c>, and <c>ApproximatelyEquals</c> (#16).
/// <c>dotnet run -c Release --project Fuzzion.Benchmarks -- --filter *ProcessExtrasBenchmarks*</c>
/// </summary>
[MemoryDiagnoser]
public class ProcessExtrasBenchmarks
{
    private string[] _items = Array.Empty<string>();
    private string[] _first = Array.Empty<string>();
    private string[] _second = Array.Empty<string>();

    [Params(200, 1000)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(19);

        // Groups of near-duplicates: a base name and a few variants with one change.
        var items = new List<string>();
        while (items.Count < Count)
        {
            var (a, b) = BenchmarkStrings.Similar(12, random.Next());
            items.Add(a);
            items.Add(b);
        }

        _items = items.Take(Count).ToArray();
        _first = _items;
        _second = _items.Reverse().ToArray();
    }

    [Benchmark]
    public IReadOnlyList<string> Dedupe() => Process.Dedupe(_items, threshold: 85);

    [Benchmark]
    public double[] Cpdist() => Process.Cpdist(_first, _second);

    [Benchmark]
    public int ApproximatelyEqualsAll()
    {
        var count = 0;
        for (var i = 0; i < _first.Length; i++)
        {
            if (_first[i].ApproximatelyEquals(_second[i], SimilarityTolerance.Normal, SimilarityMetric.JaroWinkler, SimilarityMetric.Levenshtein))
            {
                count++;
            }
        }

        return count;
    }
}
