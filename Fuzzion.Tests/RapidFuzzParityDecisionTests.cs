using System;
using Fuzzion.PreProcess;
using Fuzzion.SimilarityRatio.Scorer.Composite;
using Fuzzion.SimilarityRatio.Scorer.StrategySensitive;
using Xunit;

namespace Fuzzion.Tests;

/// <summary>
/// Two behaviors changed in Fuzzion to match RapidFuzz (see docs/known-differences.md).
/// Expected values are from Python rapidfuzz 3.14.6 and thefuzz 0.22.1.
/// </summary>
public class RapidFuzzParityDecisionTests
{
    // ---- Decision 1: two empty strings score 100 -------------------------------------

    [Fact]
    public void RatioOfTwoEmptyStringsIs100()
    {
        Assert.Equal(100, Fuzz.Ratio("", ""));
        Assert.Equal(100, Fuzz.Ratio("", "", StringPreprocessor.None));
        Assert.Equal(100, Fuzz.TokenSortRatio("", ""));
    }

    [Theory]
    [InlineData("abc", "")]
    [InlineData("", "abc")]
    public void RatioWithOneEmptyStringIs0(string a, string b)
    {
        Assert.Equal(0, Fuzz.Ratio(a, b));
    }

    [Fact]
    public void CachedRatioOfTwoEmptyStringsIs100()
    {
        using var scorer = new CachedDefaultRatioScorer("");
        Assert.Equal(100, scorer.Score(""));
        Assert.Equal(0, scorer.Score("abc"));
    }

    [Fact]
    public void CachedRatioWithEmptyCandidateIs0()
    {
        using var scorer = new CachedDefaultRatioScorer("abc");
        Assert.Equal(0, scorer.Score(""));
    }

    [Fact]
    public void PreprocessingToEmptyCountsAsEmpty()
    {
        // Full cleanup turns punctuation-only strings into "".
        Assert.Equal(100, Fuzz.Ratio("!!!", "???", StringPreprocessor.Full));
    }

    [Fact]
    public void WeightedRatioOfEmptyStringsStays0()
    {
        // RapidFuzz's WRatio returns 0 when either input is empty, including both.
        Assert.Equal(0, Fuzz.WeightedRatio("", ""));
        Assert.Equal(0, Fuzz.WeightedRatio("abc", ""));
    }

    // ---- Decision 2: WeightedRatio uses partial token scores ---------------------------

    [Theory]
    [InlineData("smith corp", "limited smitzh corp", 85.5)]
    [InlineData("dog mets", "america mets order america", 85.5)]
    [InlineData("of john", "order limited bank avenue new york", 36.642857142857146)]
    [InlineData("fuzzy wuzzy was a bear", "wuzzy fuzzy was a bear", 95)]
    public void WeightedRatioMatchesRapidFuzz(string a, string b, double expected)
    {
        Assert.Equal(expected, Fuzz.WeightedRatio(a, b, StringPreprocessor.None), 9);
    }

    [Fact]
    public void CachedWeightedRatioEqualsUncached()
    {
        string[] words = ["new", "york", "mets", "braves", "vs", "chicago", "cubs", "the", "a", "fuzzy", "bear", "smith", "corp"];
        var random = new Random(31);

        for (var i = 0; i < 300; i++)
        {
            var query = Sentence(random, words, random.Next(1, 4));
            using var cached = new CachedWeightedRatioScorer(query);
            for (var j = 0; j < 10; j++)
            {
                // Mix of similar and very different lengths, to reach both branches.
                var candidate = Sentence(random, words, random.Next(1, 12));
                Assert.Equal(Fuzz.WeightedRatio(query, candidate), cached.Score(candidate), 9);
            }
        }
    }

    [Fact]
    public void CachedWeightedRatioDisposeTwice()
    {
        var cached = new CachedWeightedRatioScorer("new york mets");
        cached.Dispose();
        cached.Dispose();
    }

    private static string Sentence(Random random, string[] words, int count)
    {
        var parts = new string[count];
        for (var i = 0; i < count; i++) parts[i] = words[random.Next(words.Length)];
        return string.Join(" ", parts);
    }
}
