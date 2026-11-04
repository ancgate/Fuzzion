using System;
using System.Linq;
using Fuzzion.SimilarityRatio;
using Fuzzion.SimilarityRatio.Scorer.StrategySensitive;
using Fuzzion.Utils;
using Xunit;

namespace Fuzzion.Tests;

/// <summary>
/// Plan item #33: SIMD batch Ratio inside Cdist must give exactly the scalar results.
/// BatchRatio.Disabled is process-wide, so these tests do not run in parallel with others
/// that read it.
/// </summary>
[Collection("BatchRatio switch")]
public class Phase8SimdTests
{
    private static double[,] Scalar(string[] queries, string[] choices, bool qratio = false, double cutoff = 0)
    {
        BatchRatio.Disabled = true;
        try
        {
            return Run(queries, choices, qratio, cutoff);
        }
        finally
        {
            BatchRatio.Disabled = false;
        }
    }

    private static double[,] Run(string[] queries, string[] choices, bool qratio = false, double cutoff = 0) =>
        qratio
            ? Process.Cdist(queries, choices, ScorerCache.Get<QRatioScorer>(), scoreCutoff: cutoff)
            : Process.Cdist(queries, choices, scoreCutoff: cutoff);

    [Fact]
    public void BatchIsUsedWhenHardwareAllowsIt()
    {
#if NETCOREAPP3_0_OR_GREATER
        var batch = BatchRatio.TryCreate(["abc", "abd"]);
        Assert.Equal(System.Numerics.Vector.IsHardwareAccelerated, batch != null);
#endif
        BatchRatio.Disabled = true;
        try
        {
            Assert.Null(BatchRatio.TryCreate(["abc"]));
        }
        finally
        {
            BatchRatio.Disabled = false;
        }
    }

    [Theory]
    [InlineData(1, 8)]
    [InlineData(9, 16)]
    [InlineData(17, 32)]
    [InlineData(33, 64)]
    [InlineData(1, 64)]
    public void SimdEqualsScalar(int minLength, int maxLength)
    {
        var random = new Random(minLength * 100 + maxLength);
        var queries = Enumerable.Range(0, 40).Select(_ => Letters(random, random.Next(0, 80), "abcde")).ToArray();
        var choices = Enumerable.Range(0, 250).Select(_ => Letters(random, random.Next(minLength, maxLength + 1), "abcde")).ToArray();

        AssertExactlyEqual(Scalar(queries, choices), Run(queries, choices));
        AssertExactlyEqual(Scalar(queries, choices, qratio: true), Run(queries, choices, qratio: true));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(31)]
    [InlineData(32)]
    [InlineData(33)]
    public void LaneBoundaries(int choiceCount)
    {
        var random = new Random(choiceCount);
        var queries = Enumerable.Range(0, 10).Select(_ => Letters(random, random.Next(0, 20), "abc")).ToArray();
        var choices = Enumerable.Range(0, choiceCount).Select(_ => Letters(random, random.Next(1, 9), "abc")).ToArray();

        AssertExactlyEqual(Scalar(queries, choices), Run(queries, choices));
    }

    [Fact]
    public void MixedLengthsAndEmptyChoicesFallBack()
    {
        var random = new Random(65);
        var queries = Enumerable.Range(0, 20).Select(_ => Letters(random, random.Next(0, 100), "ab")).ToArray();
        var choices = Enumerable.Range(0, 120)
            .Select(i => i % 10 == 0 ? "" : Letters(random, random.Next(1, 200), "ab"))
            .ToArray();

        AssertExactlyEqual(Scalar(queries, choices), Run(queries, choices));
        AssertExactlyEqual(Scalar(queries, choices, qratio: true), Run(queries, choices, qratio: true));
    }

    [Fact]
    public void NonLatin1CharactersAndEmptyQuery()
    {
        string[] queries = ["", "中文字", "café", "日本語テキスト", "Ωmega"];
        string[] choices = ["中文", "cafe", "café", "テキスト", "omega", "Ω", "x"];

        AssertExactlyEqual(Scalar(queries, choices), Run(queries, choices));
        AssertExactlyEqual(Scalar(queries, choices, qratio: true), Run(queries, choices, qratio: true));
        Assert.Equal(0, Run(queries, choices)[0, 0]);
    }

    [Fact]
    public void CutoffAndParallelWithBatch()
    {
        var random = new Random(99);
        var queries = Enumerable.Range(0, 60).Select(_ => Letters(random, random.Next(1, 30), "abcd")).ToArray();
        var choices = Enumerable.Range(0, 200).Select(_ => Letters(random, random.Next(1, 30), "abcd")).ToArray();

        AssertExactlyEqual(Scalar(queries, choices, cutoff: 60), Run(queries, choices, cutoff: 60));
        AssertExactlyEqual(Scalar(queries, choices), Process.Cdist(queries, choices, workers: -1));
    }

    [Fact]
    public void BatchMatchesSingleCalls()
    {
        var random = new Random(7);
        var queries = Enumerable.Range(0, 30).Select(_ => Letters(random, random.Next(0, 40), "abcdefgh")).ToArray();
        var choices = Enumerable.Range(0, 100).Select(_ => Letters(random, random.Next(1, 64), "abcdefgh")).ToArray();
        var matrix = Run(queries, choices);

        for (var i = 0; i < queries.Length; i++)
        for (var j = 0; j < choices.Length; j++)
            Assert.Equal(Fuzz.Ratio(queries[i], choices[j]), matrix[i, j]);
    }

    private static void AssertExactlyEqual(double[,] expected, double[,] actual)
    {
        Assert.Equal(expected.GetLength(0), actual.GetLength(0));
        Assert.Equal(expected.GetLength(1), actual.GetLength(1));
        for (var i = 0; i < expected.GetLength(0); i++)
        for (var j = 0; j < expected.GetLength(1); j++)
            Assert.True(expected[i, j] == actual[i, j], $"[{i},{j}] scalar {expected[i, j]:R} vs batch {actual[i, j]:R}");
    }

    private static string Letters(Random random, int length, string alphabet) =>
        new(Enumerable.Range(0, length).Select(_ => alphabet[random.Next(alphabet.Length)]).ToArray());
}

[CollectionDefinition("BatchRatio switch", DisableParallelization = true)]
public class BatchRatioSwitchCollection
{
}
