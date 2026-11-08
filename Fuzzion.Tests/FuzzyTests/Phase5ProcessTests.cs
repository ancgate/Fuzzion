using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Fuzzion.PreProcess;
using Fuzzion.SimilarityRatio;
using Fuzzion.SimilarityRatio.Scorer;
using Fuzzion.SimilarityRatio.Scorer.Composite;
using Fuzzion.SimilarityRatio.Scorer.StrategySensitive;
using Fuzzion.Tests.Oracle;
using Xunit;

namespace Fuzzion.Tests.FuzzyTests;

/// <summary>
/// Plan items #24-#27. Oracle values in Oracle/data/process-cases.json come from
/// Python rapidfuzz 3.14.6 (cdist, extract) and thefuzz 0.22.1 (dedupe).
/// </summary>
public class Phase5ProcessTests
{
    private static readonly Lazy<JsonElement> Cases = new(() =>
        JsonDocument.Parse(File.ReadAllText(OracleData.FindFile("process-cases"))).RootElement.Clone());

    private static readonly Func<string, string> Identity = s => s;
    private static readonly string[] Words = ["new", "york", "mets", "braves", "vs", "chicago", "cubs", "the", "a", "fuzzy", "bear"];

    private static IDistanceScorer LevenshteinScorer(int? maxDistance = null) =>
        Scorers.FromDistance((a, b) => Levenshtein.Distance(a, b), maxDistance);

    // ---- #26 Distance scorers in searches --------------------------------------------

    [Fact]
    public void ExtractOneWithDistanceScorer()
    {
        string[] choices = ["apple", "appel", "banana", "bandana"];
        var best = Process.ExtractOne("appel", choices, Identity, LevenshteinScorer());

        Assert.Equal("appel", best.Value);
        Assert.Equal(0, best.Score);
        Assert.Equal(1, best.Index);
    }

    [Fact]
    public void DistanceCutoffIsMaximum()
    {
        string[] choices = ["apple", "appel", "banana", "bandana"];
        var results = Process.ExtractAll("appel", choices, Identity, LevenshteinScorer(maxDistance: 2)).ToArray();

        Assert.Equal(["apple", "appel"], results.Select(r => r.Value));
        Assert.All(results, r => Assert.True(r.Score <= 2));
    }

    [Fact]
    public void ExtractSortedAscendingForDistance()
    {
        string[] choices = ["bandana", "apple", "banana", "appel"];
        var results = Process.ExtractSorted("appel", choices, Identity, LevenshteinScorer()).ToArray();

        Assert.Equal([0.0, 2, 5, 6], results.Select(r => r.Score));
        Assert.Equal("appel", results[0].Value);
    }

    [Fact]
    public void ExtractTopForDistanceTakesSmallest()
    {
        string[] choices = ["bandana", "apple", "banana", "appel"];
        var top = Process.ExtractTop("appel", choices, Identity, LevenshteinScorer(), limit: 2).ToArray();

        Assert.Equal(["appel", "apple"], top.Select(r => r.Value));
    }

    [Fact]
    public void MaxDistanceZeroKeepsExactMatchesOnly()
    {
        Assert.Null(Process.ExtractOne("appl", ["apple", "banana"], Identity, LevenshteinScorer(maxDistance: 0)));
        Assert.Equal("apple", Process.ExtractOne("apple", ["apple", "banana"], Identity, LevenshteinScorer(maxDistance: 0)).Value);
    }

    [Fact]
    public void CutoffWithDistanceScorerThrows()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Process.ExtractOne("a", ["a"], Identity, LevenshteinScorer(), cutoff: 2));
        Assert.Contains("maxDistance", ex.Message);
    }

    [Fact]
    public void DistanceScorerWorksInPipelines()
    {
        string[] choices = ["bandana", "apple", "banana", "appel"];
        var sequential = Process.Configure().WithScorer(LevenshteinScorer()).Build();
        var parallel = Process.Configure().WithScorer(LevenshteinScorer()).Parallel().Build();

        foreach (var pipeline in new[] { sequential, parallel })
        {
            Assert.Equal("appel", pipeline.ExtractOne("appel", choices, Identity).Value);
            Assert.Equal([0.0, 2, 5, 6], pipeline.ExtractSorted("appel", choices, Identity).Select(r => r.Score));
        }
    }

    [Fact]
    public void FromSimilarityWrapsAFunction()
    {
        var scorer = Scorers.FromSimilarity((a, b) => 100 * JaroWinkler.Similarity(a, b));
        var best = Process.ExtractOne("jonathon", ["john", "jonathan", "joan"], Identity, scorer);
        Assert.Equal("jonathan", best.Value);
    }

    [Fact]
    public void AdaptersHandleEmptyStrings()
    {
        IRatioScorer[] adapters =
        [
            LevenshteinScorer(),
            Scorers.FromSimilarity((a, b) => 100 * Jaro.Similarity(a, b)),
        ];

        foreach (var scorer in adapters)
        foreach (var s in new[] { "", " " })
        {
            scorer.Score(s, "TEST");
            scorer.Score("TEST", s);
            scorer.Score(s, "TEST", StringPreprocessor.Full);
        }

        Assert.Equal(4, LevenshteinScorer().Score("", "TEST"));
    }

    [Fact]
    public void Oracle_ExtractWithDistanceScorer()
    {
        var choices = Cases.Value.GetProperty("choices_words").EnumerateArray().Select(x => x.GetString()).ToArray();
        foreach (var c in Cases.Value.GetProperty("extract_distance").EnumerateArray())
        {
            var query = c.GetProperty("query").GetString();
            int? max = c.GetProperty("max_distance").ValueKind == JsonValueKind.Null ? null : c.GetProperty("max_distance").GetInt32();
            var expected = c.GetProperty("results").EnumerateArray()
                .Select(r => (r[0].GetString(), r[1].GetDouble(), r[2].GetInt32())).ToArray();

            var actual = Process.ExtractSorted(query, choices, Identity, LevenshteinScorer(max))
                .Select(r => (r.Value, r.Score, r.Index)).ToArray();

            Assert.Equal(expected, actual);
        }
    }

    // ---- #24 Cdist -------------------------------------------------------------------

    [Fact]
    public void CdistKnownValues()
    {
        string[] queries = ["apple", "banana"];
        string[] choices = ["apple", "appel", "banana", "bandana"];

        var ratio = Process.Cdist(queries, choices);
        double[,] expectedRatio = { { 100, 80, 18.181818, 16.666667 }, { 18.181818, 18.181818, 100, 92.307692 } };
        AssertMatrix(expectedRatio, ratio, 1e-6);

        var distance = Process.Cdist(queries, choices, LevenshteinScorer());
        AssertMatrix(new double[,] { { 0, 2, 5, 6 }, { 5, 5, 0, 1 } }, distance, 0);
    }

    [Fact]
    public void Oracle_Cdist()
    {
        var queries = Cases.Value.GetProperty("queries").EnumerateArray().Select(x => x.GetString()).ToArray();
        var choices = Cases.Value.GetProperty("choices").EnumerateArray().Select(x => x.GetString()).ToArray();

        foreach (var c in Cases.Value.GetProperty("cdist").EnumerateArray())
        {
            var name = c.GetProperty("scorer").GetString();
            var cutoff = c.GetProperty("cutoff");
            IRatioScorer scorer = name switch
            {
                "ratio" => ScorerCache.Get<DefaultRatioScorer>(),
                "wratio" => ScorerCache.Get<WeightedRatioScorer>(),
                "partial_ratio" => ScorerCache.Get<PartialRatioScorer>(),
                "token_set_ratio" => ScorerCache.Get<TokenSetScorer>(),
                "qratio" => ScorerCache.Get<QRatioScorer>(),
                "levenshtein" => LevenshteinScorer(cutoff.ValueKind == JsonValueKind.Null ? null : cutoff.GetInt32()),
                _ => throw new InvalidOperationException(name),
            };
            var scoreCutoff = name != "levenshtein" && cutoff.ValueKind != JsonValueKind.Null ? cutoff.GetDouble() : 0;

            var rows = c.GetProperty("matrix").EnumerateArray().Select(r => r.EnumerateArray().Select(v => v.GetDouble()).ToArray()).ToArray();
            var expected = new double[rows.Length, rows[0].Length];
            for (var i = 0; i < rows.Length; i++)
            for (var j = 0; j < rows[i].Length; j++)
                expected[i, j] = rows[i][j];

            AssertMatrix(expected, Process.Cdist(queries, choices, scorer, scoreCutoff: scoreCutoff), 1e-9, $"{name} cutoff {cutoff}");
            AssertMatrix(expected, Process.Cdist(queries, choices, scorer, scoreCutoff: scoreCutoff, workers: -1), 1e-9, $"{name} cutoff {cutoff} parallel");
        }
    }

    [Theory]
    [InlineData("ratio")]
    [InlineData("wratio")]
    [InlineData("partial_ratio")]
    [InlineData("token_sort")]
    [InlineData("token_set")]
    [InlineData("partial_token_set")]
    [InlineData("custom")]
    public void CellEqualsSingleCall(string name)
    {
        IRatioScorer scorer = name switch
        {
            "ratio" => ScorerCache.Get<DefaultRatioScorer>(),
            "wratio" => ScorerCache.Get<WeightedRatioScorer>(),
            "partial_ratio" => ScorerCache.Get<PartialRatioScorer>(),
            "token_sort" => ScorerCache.Get<TokenSortScorer>(),
            "token_set" => ScorerCache.Get<TokenSetScorer>(),
            "partial_token_set" => ScorerCache.Get<PartialTokenSetScorer>(),
            _ => Scorers.FromSimilarity((a, b) => 100 * Jaro.Similarity(a, b)),
        };
        var random = new Random(name.Length);
        var queries = Enumerable.Range(0, 30).Select(_ => Sentence(random)).ToArray();
        var choices = Enumerable.Range(0, 40).Select(_ => Sentence(random)).ToArray();

        var matrix = Process.Cdist(queries, choices, scorer);
        for (var i = 0; i < queries.Length; i++)
        for (var j = 0; j < choices.Length; j++)
            Assert.Equal(scorer.Score(queries[i], choices[j]), matrix[i, j], 9);
    }

    [Fact]
    public void ParallelEqualsSequential()
    {
        var random = new Random(200);
        var queries = Enumerable.Range(0, 200).Select(_ => Sentence(random)).ToArray();
        var choices = Enumerable.Range(0, 500).Select(_ => Sentence(random)).ToArray();
        var scorer = ScorerCache.Get<WeightedRatioScorer>();

        Assert.Equal(Process.Cdist(queries, choices, scorer), Process.Cdist(queries, choices, scorer, workers: -1));
        Assert.Equal(Process.Cdist(queries, choices, scorer), Process.Cdist(queries, choices, scorer, workers: 3));
    }

    [Fact]
    public void EmptyQueriesGivesEmptyMatrix()
    {
        var matrix = Process.Cdist(Array.Empty<string>(), ["a", "b"]);
        Assert.Equal(0, matrix.GetLength(0));
        Assert.Equal(2, matrix.GetLength(1));
    }

    [Fact]
    public void CutoffFillsWorstScore()
    {
        var similarity = Process.Cdist(["apple"], ["apple", "bandana"], scoreCutoff: 50);
        Assert.Equal(100, similarity[0, 0]);
        Assert.Equal(0, similarity[0, 1]);

        var distance = Process.Cdist(["apple"], ["apple", "bandana"], LevenshteinScorer(maxDistance: 3));
        Assert.Equal(0, distance[0, 0]);
        Assert.Equal(4, distance[0, 1]); // maxDistance + 1, as RapidFuzz
    }

    [Fact]
    public void ProcessorIsAppliedOnce()
    {
        var calls = 0;
        Func<string, string> counting = s => { Interlocked.Increment(ref calls); return s.ToLowerInvariant(); };
        var matrix = Process.Cdist(["APPLE", "BANANA"], ["apple", "banana", "cherry"], processor: counting);

        Assert.Equal(5, calls);
        Assert.Equal(100, matrix[0, 0]);
    }

    [Fact]
    public void InvalidArgumentsThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Process.Cdist(["a"], ["b"], workers: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Process.Cdist(["a"], ["b"], scoreCutoff: double.NaN));
        Assert.Throws<ArgumentException>(() => Process.Cdist(["a"], ["b"], LevenshteinScorer(), scoreCutoff: 1));
        Assert.Throws<ArgumentException>(() => Process.Cdist(["a", null], ["b"]));
    }

    [Fact]
    public void CancellationStopsTheWork()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(() => Process.Cdist(["a", "b"], ["c"], cancellationToken: cts.Token));
        Assert.ThrowsAny<OperationCanceledException>(() => Process.Cdist(["a", "b"], ["c"], workers: -1, cancellationToken: cts.Token));
    }

    // ---- #25 Cpdist ------------------------------------------------------------------

    [Fact]
    public void CpdistKnownValues()
    {
        var scores = Process.Cpdist(["apple", "banana"], ["appel", "bandana"]);
        Assert.Equal(80, scores[0], 6);
        Assert.Equal(92.307692, scores[1], 6);
    }

    [Fact]
    public void CpdistEqualsCdistDiagonal()
    {
        var random = new Random(25);
        var a = Enumerable.Range(0, 100).Select(_ => Sentence(random)).ToArray();
        var b = Enumerable.Range(0, 100).Select(_ => Sentence(random)).ToArray();
        var matrix = Process.Cdist(a, b, ScorerCache.Get<TokenSortScorer>());
        var pairs = Process.Cpdist(a, b, ScorerCache.Get<TokenSortScorer>(), workers: -1);

        for (var i = 0; i < a.Length; i++)
        {
            Assert.Equal(matrix[i, i], pairs[i], 12);
        }
    }

    [Fact]
    public void CpdistLengthMismatchThrows()
    {
        Assert.Throws<ArgumentException>(() => Process.Cpdist(["a", "b"], ["a"]));
    }

    // ---- #27 Dedupe ------------------------------------------------------------------

    [Fact]
    public void DedupeKnownValues()
    {
        string[] items = ["Frodo Baggin", "Frodo Baggins", "F. Baggins", "Samwise G.", "Gandalf", "Bilbo Baggins"];
        var result = Process.Dedupe(items);
        Assert.Equal(["Frodo Baggins", "Gandalf", "Samwise G."], result.OrderBy(x => x, StringComparer.Ordinal));
    }

    [Fact]
    public void Oracle_Dedupe()
    {
        foreach (var c in Cases.Value.GetProperty("dedupe").EnumerateArray())
        {
            var items = c.GetProperty("items").EnumerateArray().Select(x => x.GetString()).ToArray();
            var expected = c.GetProperty("expected").EnumerateArray().Select(x => x.GetString()).ToArray();
            Assert.Equal(expected, Process.Dedupe(items).OrderBy(x => x, StringComparer.Ordinal));
        }
    }

    [Fact]
    public void DedupeNoDuplicatesReturnsInput()
    {
        string[] items = ["apple", "zebra", "chicago"];
        Assert.Equal(items, Process.Dedupe(items));
    }

    [Fact]
    public void DedupeEmptyReturnsEmpty()
    {
        Assert.Empty(Process.Dedupe(Array.Empty<string>()));
    }

    [Fact]
    public void DedupeThresholdHundredOnlyRemovesExact()
    {
        Assert.Equal(["new york mets", "new york met"], Process.Dedupe(["new york mets", "new york met", "new york mets"], threshold: 100));
    }

    [Fact]
    public void DedupeKeepsOrderOfFirstAppearance()
    {
        Assert.Equal(["zebra", "new york mets"], Process.Dedupe(["zebra", "new york met", "new york mets"]));
    }

    [Fact]
    public void DedupeKeepsItemsThatMatchNothing()
    {
        // "!!!" is empty after processing and matches nothing, not even itself (TheFuzz raises here).
        Assert.Equal(["!!!", "apple"], Process.Dedupe(["!!!", "apple", "apple"]));
    }

    [Fact]
    public void DedupeRejectsDistanceScorer()
    {
        Assert.Throws<ArgumentException>(() => Process.Dedupe(["a", "b"], scorer: LevenshteinScorer()));
    }

    // ---- Helpers ----------------------------------------------------------------------

    private static string Sentence(Random random) =>
        string.Join(" ", Enumerable.Range(0, random.Next(0, 6)).Select(_ => Words[random.Next(Words.Length)]));

    private static void AssertMatrix(double[,] expected, double[,] actual, double tolerance, string context = "")
    {
        Assert.Equal(expected.GetLength(0), actual.GetLength(0));
        Assert.Equal(expected.GetLength(1), actual.GetLength(1));
        for (var i = 0; i < expected.GetLength(0); i++)
        for (var j = 0; j < expected.GetLength(1); j++)
        {
            Assert.True(Math.Abs(expected[i, j] - actual[i, j]) <= tolerance,
                $"{context} [{i},{j}]: expected {expected[i, j]}, got {actual[i, j]}");
        }
    }
}
