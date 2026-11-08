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

    /// <summary>
    /// Checks the four functions of a distance metric against an oracle entry.
    /// Integer metrics are compared exactly; Jaro-style metrics within 1e-9.
    /// </summary>
    private static void CheckFour(
        string metric,
        Func<string, string, double> distance,
        Func<string, string, double> similarity,
        Func<string, string, double> normalizedDistance,
        Func<string, string, double> normalizedSimilarity,
        double distanceTolerance)
    {
        var check = new OracleAssert(metric);
        foreach (var row in OracleData.CharPairs)
        {
            var expected = row.Four(metric);
            check.Equal(row, "Distance", expected.Distance, distance(row.A, row.B), distanceTolerance);
            check.Equal(row, "Similarity", expected.Similarity, similarity(row.A, row.B), distanceTolerance);
            check.Equal(row, "NormalizedDistance", expected.NormalizedDistance, normalizedDistance(row.A, row.B), OracleAssert.Normalized);
            check.Equal(row, "NormalizedSimilarity", expected.NormalizedSimilarity, normalizedSimilarity(row.A, row.B), OracleAssert.Normalized);
        }

        check.Verify();
    }

    [Fact]
    public void Oracle_Hamming() => CheckFour("hamming",
        (a, b) => Hamming.Distance(a, b), (a, b) => Hamming.Similarity(a, b),
        (a, b) => Hamming.NormalizedDistance(a, b), (a, b) => Hamming.NormalizedSimilarity(a, b), OracleAssert.Exact);

    [Fact]
    public void Oracle_Jaro() => CheckFour("jaro",
        Jaro.Distance, Jaro.Similarity, Jaro.NormalizedDistance, Jaro.NormalizedSimilarity, OracleAssert.Normalized);

    [Theory]
    [InlineData("jaro_winkler", 0.1)]
    [InlineData("jaro_winkler_p02", 0.2)]
    public void Oracle_JaroWinkler(string metric, double prefixWeight) => CheckFour(metric,
        (a, b) => JaroWinkler.Distance(a, b, prefixWeight), (a, b) => JaroWinkler.Similarity(a, b, prefixWeight),
        (a, b) => JaroWinkler.NormalizedDistance(a, b, prefixWeight), (a, b) => JaroWinkler.NormalizedSimilarity(a, b, prefixWeight),
        OracleAssert.Normalized);

    [Fact]
    public void Oracle_Osa() => CheckFour("osa",
        (a, b) => Osa.Distance(a, b), (a, b) => Osa.Similarity(a, b),
        (a, b) => Osa.NormalizedDistance(a, b), (a, b) => Osa.NormalizedSimilarity(a, b), OracleAssert.Exact);

    [Fact]
    public void Oracle_DamerauLevenshtein() => CheckFour("damerau_levenshtein",
        (a, b) => DamerauLevenshtein.Distance(a, b), (a, b) => DamerauLevenshtein.Similarity(a, b),
        (a, b) => DamerauLevenshtein.NormalizedDistance(a, b), (a, b) => DamerauLevenshtein.NormalizedSimilarity(a, b), OracleAssert.Exact);

    [Fact]
    public void Oracle_Prefix() => CheckFour("prefix",
        (a, b) => Prefix.Distance(a, b), (a, b) => Prefix.Similarity(a, b),
        (a, b) => Prefix.NormalizedDistance(a, b), (a, b) => Prefix.NormalizedSimilarity(a, b), OracleAssert.Exact);

    [Fact]
    public void Oracle_Postfix() => CheckFour("postfix",
        (a, b) => Postfix.Distance(a, b), (a, b) => Postfix.Similarity(a, b),
        (a, b) => Postfix.NormalizedDistance(a, b), (a, b) => Postfix.NormalizedSimilarity(a, b), OracleAssert.Exact);

    [Fact]
    public void Oracle_IndelSimilarity()
    {
        var check = new OracleAssert("indel");
        foreach (var row in OracleData.CharPairs)
        {
            check.Equal(row, "Similarity", row.Four("indel").Similarity, Indel.Similarity(row.A.AsSpan(), row.B.AsSpan()), OracleAssert.Exact);
        }

        check.Verify();
    }

    // ---- Set-based metrics (Phase 4) -------------------------------------------------

    [Theory]
    [InlineData("set_q2", 2, false, QGramMode.Set)]
    [InlineData("set_q2_multiset", 2, false, QGramMode.Multiset)]
    [InlineData("set_q3_pad", 3, true, QGramMode.Set)]
    public void Oracle_SetMetrics(string metric, int q, bool pad, QGramMode mode)
    {
        var options = new QGramOptions(q, pad, mode);
        var check = new OracleAssert(metric);
        foreach (var row in OracleData.CharPairs)
        {
            check.Equal(row, "Jaccard", row.Nested(metric, "jaccard"), Jaccard.Similarity(row.A, row.B, options), OracleAssert.Normalized);
            check.Equal(row, "SorensenDice", row.Nested(metric, "dice"), SorensenDice.Similarity(row.A, row.B, options), OracleAssert.Normalized);
            check.Equal(row, "Overlap", row.Nested(metric, "overlap"), Overlap.Similarity(row.A, row.B, options), OracleAssert.Normalized);
            check.Equal(row, "Tanimoto", row.Nested(metric, "tanimoto"), Tanimoto.Similarity(row.A, row.B, options), OracleAssert.Normalized);
        }

        check.Verify();
    }

    [Theory]
    [InlineData("ratcliff_obershelp", false)]
    [InlineData("ratcliff_obershelp_autojunk", true)]
    public void Oracle_RatcliffObershelp(string metric, bool autoJunk)
    {
        var check = new OracleAssert(metric);
        foreach (var row in OracleData.CharPairs)
        {
            check.Equal(row, "Similarity", row.Number(metric), RatcliffObershelp.Similarity(row.A, row.B, autoJunk), OracleAssert.Normalized);
        }

        check.Verify();
    }

    [Fact]
    public void Oracle_LongestCommonSubstring()
    {
        var check = new OracleAssert("longest_common_substring");
        foreach (var row in OracleData.CharPairs)
        {
            var expected = row.Numbers("longest_common_substring");
            var actual = LongestCommonSubstring.Find(row.A, row.B);
            check.Equal(row, "Length", expected[2], actual.Length, OracleAssert.Exact);
            if (expected[2] > 0)
            {
                // difflib reports (0, 0, 0) when there is no match, so positions are only compared for real matches.
                check.Equal(row, "SourceStart", expected[0], actual.SourceStart, OracleAssert.Exact);
                check.Equal(row, "TargetStart", expected[1], actual.TargetStart, OracleAssert.Exact);
            }
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
    [InlineData("qratio")]
    [InlineData("token_ratio")]
    [InlineData("partial_token_ratio")]
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
            "qratio" => Fuzz.QRatio,
            "token_ratio" => Fuzz.TokenRatio,
            "partial_token_ratio" => Fuzz.PartialTokenRatio,
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

    [Fact]
    public void Oracle_PartialRatioAlignment()
    {
        var check = new OracleAssert("partial_ratio_alignment");
        foreach (var row in OracleData.SentencePairs)
        {
            var expected = row.Numbers("partial_ratio_alignment");
            var actual = Fuzz.PartialRatioAlignment(row.A, row.B, NoCleanup);
            check.Equal(row, "Score", expected[0], actual.Score, OracleAssert.Score);
            check.Equal(row, "SourceStart", expected[1], actual.SourceStart, OracleAssert.Exact);
            check.Equal(row, "SourceEnd", expected[2], actual.SourceEnd, OracleAssert.Exact);
            check.Equal(row, "DestStart", expected[3], actual.DestStart, OracleAssert.Exact);
            check.Equal(row, "DestEnd", expected[4], actual.DestEnd, OracleAssert.Exact);
        }

        check.Verify();
    }

    /// <summary>
    /// Cases where Fuzzion deliberately differs from RapidFuzz. Each one must be explained
    /// in docs/known-differences.md; keep the two in sync. There are none today.
    /// </summary>
    private static bool IsKnownDifference(string metric, OracleRow row) => false;

    [Fact]
    public void WRatioPartialBranchIsCompared()
    {
        // The partial branch (one string at least 1.5x longer) must be covered by the oracle.
        var partialRows = OracleData.SentencePairs.Count(r =>
            r.A.Length > 0 && r.B.Length > 0 &&
            (double)Math.Max(r.A.Length, r.B.Length) / Math.Min(r.A.Length, r.B.Length) >= 1.5);

        Assert.True(partialRows > 40, $"only {partialRows} sentence pairs reach WRatio's partial branch");
    }
}
