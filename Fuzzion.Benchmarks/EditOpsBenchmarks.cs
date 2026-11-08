using System;
using BenchmarkDotNet.Attributes;
using Fuzzion.Edits;

namespace Fuzzion.Benchmarks;

/// <summary>
/// Phase 6 edit operations (plan items #28, #29), with the plain distance as the baseline.
/// <c>dotnet run -c Release --project Fuzzion.Benchmarks -- --filter *EditOpsBenchmarks*</c>
/// </summary>
[MemoryDiagnoser]
public class EditOpsBenchmarks
{
    private string _a = "";
    private string _b = "";

    [Params(16, 64, 256)]
    public int Length { get; set; }

    [GlobalSetup]
    public void Setup() => (_a, _b) = BenchmarkStrings.Similar(Length, seed: 11);

    [Benchmark(Baseline = true)]
    public int LevenshteinDistance() => Levenshtein.Distance(_a, _b);

    [Benchmark]
    public EditOps LevenshteinEditOps() => Levenshtein.GetEditOps(_a, _b);

    [Benchmark]
    public OpCodes LevenshteinOpCodes() => Levenshtein.GetOpCodes(_a, _b);

    [Benchmark]
    public EditOps IndelEditOps() => Indel.GetEditOps(_a, _b);

    [Benchmark]
    public string ApplyEditOps() => Levenshtein.GetEditOps(_a, _b).Apply(_a, _b);
}

/// <summary>Random test strings shared by the newer benchmark classes.</summary>
internal static class BenchmarkStrings
{
    /// <summary>A random lowercase string and a copy with about one change in every 8 characters.</summary>
    public static (string A, string B) Similar(int length, int seed)
    {
        var random = new Random(seed);
        var chars = new char[length];
        for (var i = 0; i < length; i++) chars[i] = (char)('a' + random.Next(26));
        var a = new string(chars);
        for (var i = 0; i < length; i += 8) chars[random.Next(i, Math.Min(length, i + 8))] = (char)('a' + random.Next(26));
        return (a, new string(chars));
    }

    /// <summary>Random words of 2 to 8 letters separated by spaces, about <paramref name="length"/> characters long.</summary>
    public static string Words(Random random, int length)
    {
        var chars = new char[length];
        var untilSpace = random.Next(2, 9);
        for (var i = 0; i < length; i++)
        {
            if (untilSpace-- == 0)
            {
                chars[i] = ' ';
                untilSpace = random.Next(2, 9);
            }
            else
            {
                chars[i] = (char)('a' + random.Next(26));
            }
        }

        return new string(chars);
    }
}
