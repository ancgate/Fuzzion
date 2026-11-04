using System;
using System.IO;
using System.Linq;
using Fuzzion.PreProcess;
using Xunit;

namespace Fuzzion.Tests.Oracle;

/// <summary>
/// Checks Fuzzion against reference results from Python RapidFuzz 3.14.6
/// (see tools/oracle/generate.py). RapidFuzz does no text cleanup by default, so every
/// scorer is called with <see cref="StringPreprocessor.None"/>.
/// </summary>
public class OracleTests
{
    private static readonly Func<string, string> NoCleanup = StringPreprocessor.None;

    [Theory]
    [InlineData("char-pairs")]
    [InlineData("sentence-pairs")]
    public void OracleFilesExist(string name)
    {
        var path = OracleData.FindFile(name);
        Assert.True(new FileInfo(path).Length > 0, $"{path} is empty");
    }

    [Fact]
    public void OracleFilesParse()
    {
        Assert.True(OracleData.CharPairs.Count > 900);
        Assert.True(OracleData.SentencePairs.Count > 400);
        Assert.All(OracleData.CharPairs, row =>
        {
            Assert.NotNull(row.A);
            Assert.NotNull(row.B);
            Assert.True(row.Has("levenshtein"));
        });
        Assert.All(OracleData.SentencePairs, row => Assert.True(row.Has("ratio")));
    }

    [Fact]
    public void OracleCoversEveryGroup()
    {
        var groups = OracleData.CharPairs.Select(r => r.Group).Distinct().ToArray();
        Assert.Contains("edge", groups);
        Assert.Contains("short", groups);
        Assert.Contains("boundary", groups);
        Assert.Contains("long", groups);
        Assert.Contains("small-alphabet", groups);
    }

    // ---- Distance metrics -------------------------------------------------------------

    [Theory]
    [InlineData("levenshtein", 1, 1, 1)]
    [InlineData("levenshtein_w112", 1, 1, 2)]
    [InlineData("levenshtein_w234", 2, 3, 4)]
    public void Oracle_Levenshtein(string metric, int insertCost, int deleteCost, int replaceCost)
    {
        var check = new OracleAssert(metric);
        foreach (var row in OracleData.CharPairs)
        {
            var expected = row.Four(metric);
            check.Equal(row, "Distance", expected.Distance,
                Levenshtein.Distance(row.A, row.B, insertCost, deleteCost, replaceCost), OracleAssert.Exact);
            check.Equal(row, "Similarity", expected.Similarity,
                Levenshtein.Similarity(row.A, row.B, insertCost, deleteCost, replaceCost), OracleAssert.Exact);
            check.Equal(row, "NormalizedDistance", expected.NormalizedDistance,
                Levenshtein.NormalizedDistance(row.A, row.B, insertCost, deleteCost, replaceCost), OracleAssert.Normalized);
            check.Equal(row, "NormalizedSimilarity", expected.NormalizedSimilarity,
                Levenshtein.NormalizedSimilarity(row.A, row.B, insertCost, deleteCost, replaceCost), OracleAssert.Normalized);
        }

        check.Verify();
    }

    [Fact]
    public void Oracle_Indel()
    {
        var check = new OracleAssert("indel");
        foreach (var row in OracleData.CharPairs)
        {
            var expected = row.Four("indel");
            var a = row.A.AsSpan();
            var b = row.B.AsSpan();
            check.Equal(row, "Distance", expected.Distance, Indel.Distance(a, b), OracleAssert.Exact);
            check.Equal(row, "NormalizedDistance", expected.NormalizedDistance, Indel.NormalizedDistance(a, b), OracleAssert.Normalized);
            check.Equal(row, "NormalizedSimilarity", expected.NormalizedSimilarity, Indel.NormalizedSimilarity(a, b), OracleAssert.Normalized);
        }

        check.Verify();
    }

    [Fact]
    public void Oracle_LongestCommonSubsequence()
    {
        var check = new OracleAssert("lcsseq");
        foreach (var row in OracleData.CharPairs)
        {
            var expected = row.Four("lcsseq");
            var a = row.A.AsSpan();
            var b = row.B.AsSpan();
            check.Equal(row, "Distance", expected.Distance, LongestCommonSubsequence.Distance(a, b), OracleAssert.Exact);
            check.Equal(row, "Similarity", expected.Similarity, LongestCommonSubsequence.Similarity(a, b), OracleAssert.Exact);
            check.Equal(row, "NormalizedDistance", expected.NormalizedDistance, LongestCommonSubsequence.NormalizedDistance(a, b), OracleAssert.Normalized);
            check.Equal(row, "NormalizedSimilarity", expected.NormalizedSimilarity, LongestCommonSubsequence.NormalizedSimilarity(a, b), OracleAssert.Normalized);
        }

        check.Verify();
    }

    // ---- Fuzz scorers -----------------------------------------------------------------

    [Theory]
    [InlineData("ratio")]
    [InlineData("partial_ratio")]
    [InlineData("token_sort_ratio")]
    [InlineData("token_set_ratio")]
    [InlineData("partial_token_sort_ratio")]
    [InlineData("partial_token_set_ratio")]
    [InlineData("wratio")]
    public void Oracle_Fuzz(string metric)
    {
        Func<string, string, Func<string, string>, double> scorer = metric switch
        {
            "ratio" => Fuzz.Ratio,
            "partial_ratio" => Fuzz.PartialRatio,
            "token_sort_ratio" => Fuzz.TokenSortRatio,
            "token_set_ratio" => Fuzz.TokenSetRatio,
            "partial_token_sort_ratio" => Fuzz.PartialTokenSortRatio,
            "partial_token_set_ratio" => Fuzz.PartialTokenSetRatio,
            "wratio" => Fuzz.WeightedRatio,
            _ => throw new ArgumentOutOfRangeException(nameof(metric)),
        };

        var check = new OracleAssert(metric);
        foreach (var row in OracleData.SentencePairs)
        {
            if (IsKnownDifference(metric, row))
            {
                continue;
            }

            try
            {
                check.Equal(row, metric, row.Number(metric), scorer(row.A, row.B, NoCleanup), OracleAssert.Score);
            }
            catch (Exception ex)
            {
                check.Throws(row, metric, ex);
            }
        }

        check.Verify();
    }

    /// <summary>
    /// Cases where Fuzzion deliberately differs from RapidFuzz. Each one is explained in
    /// docs/known-differences.md; keep the two in sync.
    /// </summary>
    private static bool IsKnownDifference(string metric, OracleRow row)
    {
        // KD1: Fuzzion scores two empty strings 0; RapidFuzz scores them 100.
        if ((metric == "ratio" || metric == "token_sort_ratio") && row.A.Length == 0 && row.B.Length == 0)
        {
            return true;
        }

        // KD2: Fuzzion's WeightedRatio uses full (not partial) token scores when one
        // string is at least 1.5 times longer than the other.
        if (metric == "wratio" && IsWRatioPartialBranch(row))
        {
            return true;
        }

        return false;
    }

    private static bool IsWRatioPartialBranch(OracleRow row)
    {
        int len1 = row.A.Length, len2 = row.B.Length;
        if (len1 == 0 || len2 == 0)
        {
            return false;
        }

        return (double)Math.Max(len1, len2) / Math.Min(len1, len2) >= 1.5;
    }

    [Fact]
    public void KnownDifferencesStillExist()
    {
        // If one of these starts matching RapidFuzz, the difference was fixed: remove it
        // from IsKnownDifference and from docs/known-differences.md.
        Assert.Equal(0, Fuzz.Ratio("", "", NoCleanup));
        Assert.Equal(81, Fuzz.WeightedRatio("smith corp", "limited smitzh corp", NoCleanup), 6);
    }

    [Fact]
    public void KnownDifferencesSkipOnlyAFewRows()
    {
        var skipped = OracleData.SentencePairs.Count(r => IsKnownDifference("wratio", r));
        var compared = OracleData.SentencePairs.Count - skipped;

        // The non-partial WRatio branch must still be checked on most rows.
        Assert.True(compared > 200, $"only {compared} WRatio rows are compared");
    }
}
