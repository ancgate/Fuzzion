using System;
using System.Linq;
using Xunit;

namespace Fuzzion.Tests.SetSimilarity;

/// <summary>
/// Plan items #8-#16. Q-gram values were computed with the Python reference in
/// tools/oracle/generate.py; Ratcliff-Obershelp and longest common substring with difflib.
/// </summary>
public class SetSimilarityTests
{
    // ---- #8 QGrams -------------------------------------------------------------------

    [Fact]
    public void QGramsOf()
    {
        Assert.Equal(["ni", "ig", "gh", "ht"], QGrams.Of("night", 2));
        Assert.Equal(["aa", "aa", "aa"], QGrams.Of("aaaa", 2));
        Assert.Empty(QGrams.Of("a", 2));
        Assert.Empty(QGrams.Of("", 2));
        Assert.Equal(["\u0002a", "a\u0003"], QGrams.Of("a", 2, pad: true));
        Assert.Equal(["a", "b", "c"], QGrams.Of("abc", 1));
    }

    [Fact]
    public void QZeroThrows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => QGrams.Of("abc", 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new QGramOptions(q: 0));
    }

    // ---- #9-#12 Jaccard, Dice, Overlap, Tanimoto -------------------------------------

    [Theory]
    [InlineData("night", "nacht", 0.142857, 0.25, 0.25, 0.142857)]
    [InlineData("context", "contact", 0.333333, 0.5, 0.5, 0.333333)]
    [InlineData("aaaa", "aaaa", 1.0, 1.0, 1.0, 1.0)]
    [InlineData("abc", "xyz", 0.0, 0.0, 0.0, 0.0)]
    public void KnownValues(string a, string b, double jaccard, double dice, double overlap, double tanimoto)
    {
        Assert.Equal(jaccard, Jaccard.Similarity(a, b), 6);
        Assert.Equal(dice, SorensenDice.Similarity(a, b), 6);
        Assert.Equal(overlap, Overlap.Similarity(a, b), 6);
        Assert.Equal(tanimoto, Tanimoto.Similarity(a, b), 6);
    }

    [Fact]
    public void RepeatedCharactersScoreOne()
    {
        // kdjones/fuzzystring gives 0.25 (Dice) and 0.142857 (Tanimoto) here.
        Assert.Equal(1.0, SorensenDice.Similarity("aaaa", "aaaa"));
        Assert.Equal(1.0, Tanimoto.Similarity("aaaa", "aaaa"));
    }

    [Fact]
    public void DiceFromJaccard()
    {
        foreach (var (a, b) in RandomPairs(1000))
        {
            var j = Jaccard.Similarity(a, b);
            if (QGrams.Of(a).Count + QGrams.Of(b).Count > 0)
            {
                Assert.Equal(2 * j / (1 + j), SorensenDice.Similarity(a, b), 12);
            }
        }
    }

    [Fact]
    public void MultisetDiffersFromSet()
    {
        // Bigrams: "aaab" = aa, aa, ab; "aab" = aa, ab.
        Assert.Equal(1.0, Jaccard.Similarity("aaab", "aab"));
        Assert.Equal(2.0 / 3, Jaccard.Similarity("aaab", "aab", new QGramOptions(mode: QGramMode.Multiset)), 12);
    }

    [Fact]
    public void TanimotoOnSetsWouldEqualJaccard()
    {
        // Strings without repeated bigrams: count vectors are 0/1, so Tanimoto = Jaccard.
        Assert.Equal(Jaccard.Similarity("night", "nacht"), Tanimoto.Similarity("night", "nacht"), 12);
        // With repeats they differ.
        Assert.NotEqual(Jaccard.Similarity("aaab", "aab"), Tanimoto.Similarity("aaab", "aab"));
    }

    [Fact]
    public void WordSetOverload()
    {
        Assert.Equal(1.0 / 3, Jaccard.Similarity(new[] { "a", "b" }, new[] { "b", "c" }), 12);
        Assert.Equal(0.5, SorensenDice.Similarity(new[] { "a", "b" }, new[] { "b", "c" }), 12);
        Assert.Equal(1.0, Overlap.Similarity(new[] { "new", "york" }, new[] { "new", "york", "mets" }), 12);
        Assert.Equal(1.0, Jaccard.Similarity(Array.Empty<int>(), Array.Empty<int>()));
        Assert.Equal(0.0, Jaccard.Similarity(new[] { 1 }, Array.Empty<int>()));
    }

    [Theory]
    [InlineData("", "", 1.0)]
    [InlineData("a", "a", 1.0)]
    [InlineData("a", "b", 0.0)]
    [InlineData("", "a", 0.0)]
    [InlineData("abc", "", 0.0)]
    public void ShortAndEmptyStrings(string a, string b, double expected)
    {
        Assert.Equal(expected, Jaccard.Similarity(a, b));
        Assert.Equal(expected, SorensenDice.Similarity(a, b));
        Assert.Equal(expected, Overlap.Similarity(a, b));
        Assert.Equal(expected, Tanimoto.Similarity(a, b));
    }

    [Fact]
    public void PaddingGivesShortStringsQGrams()
    {
        var padded = new QGramOptions(q: 2, pad: true);
        Assert.Equal(0.0, Jaccard.Similarity("a", "b", padded));
        Assert.Equal(1.0, Jaccard.Similarity("a", "a", padded));
        Assert.True(Jaccard.Similarity("ab", "ac", padded) > 0);
    }

    [Fact]
    public void LongQGramsUseStringKeys()
    {
        // q > 4 takes the substring path instead of the packed 64-bit keys.
        var q5 = new QGramOptions(q: 5);
        Assert.Equal(1.0, Jaccard.Similarity("abcdefgh", "abcdefgh", q5));
        Assert.Equal(1.0 / 3, Jaccard.Similarity("abcdef", "abcdeg", q5), 12);
    }

    [Fact]
    public void RangeSymmetryAndDistance()
    {
        foreach (var (a, b) in RandomPairs(500))
        {
            foreach (var (sim, dist) in new (Func<string, string, double>, Func<string, string, double>)[]
                     {
                         ((x, y) => Jaccard.Similarity(x, y), (x, y) => Jaccard.Distance(x, y)),
                         ((x, y) => SorensenDice.Similarity(x, y), (x, y) => SorensenDice.Distance(x, y)),
                         ((x, y) => Overlap.Similarity(x, y), (x, y) => Overlap.Distance(x, y)),
                         ((x, y) => Tanimoto.Similarity(x, y), (x, y) => Tanimoto.Distance(x, y)),
                     })
            {
                var s = sim(a, b);
                Assert.InRange(s, 0.0, 1.0);
                Assert.Equal(s, sim(b, a), 12);
                Assert.Equal(1.0, s + dist(a, b), 12);
            }
        }
    }

    // ---- #13 Ratcliff-Obershelp ------------------------------------------------------

    [Theory]
    [InlineData("WIKIMEDIA", "WIKIMANIA", 0.777778)]
    [InlineData("GESTALT PATTERN MATCHING", "GESTALT PRACTICE", 0.6)]
    [InlineData("abcd", "bcde", 0.75)]
    [InlineData("", "", 1.0)]
    [InlineData("abc", "", 0.0)]
    public void RatcliffObershelpKnownValues(string a, string b, double expected)
    {
        Assert.Equal(expected, RatcliffObershelp.Similarity(a, b), 6);
    }

    [Fact]
    public void RatcliffObershelpMatchingBlocksAreOrderedAndMerged()
    {
        var blocks = RatcliffObershelp.MatchingBlocks("abxcd", "abcd");
        Assert.Equal([(0, 0, 2), (3, 2, 2)], blocks.Select(b => (b.SourcePos, b.DestPos, b.Length)));
    }

    [Fact]
    public void AutoJunkOnlyAppliesFrom200Characters()
    {
        var shortText = new string('a', 199);
        Assert.Equal(RatcliffObershelp.Similarity("aaa", shortText), RatcliffObershelp.Similarity("aaa", shortText, autoJunk: true));
    }

    // ---- #14 Longest common substring ------------------------------------------------

    [Theory]
    [InlineData("xabcdey", "zzabcdq", 1, 2, 4, "abcd")]
    [InlineData("GeeksforGeeks", "GeeksQuiz", 0, 0, 5, "Geeks")]
    [InlineData("abc", "xyz", 0, 0, 0, "")]
    [InlineData("", "", 0, 0, 0, "")]
    public void LongestCommonSubstringKnownValues(string a, string b, int sourceStart, int targetStart, int length, string text)
    {
        Assert.Equal(new CommonSubstring(sourceStart, targetStart, length), LongestCommonSubstring.Find(a, b));
        Assert.Equal(text, LongestCommonSubstring.Of(a, b));
    }

    [Fact]
    public void SubstringDiffersFromSubsequence()
    {
        Assert.Equal(1, LongestCommonSubstring.Similarity("abcde", "aXcXe"));
        Assert.Equal(3, LongestCommonSubsequence.Similarity("abcde".AsSpan(), "aXcXe".AsSpan()));
    }

    [Fact]
    public void LongestCommonSubstringMatchesNaive()
    {
        foreach (var (a, b) in RandomPairs(1000))
        {
            var found = LongestCommonSubstring.Find(a, b);
            Assert.Equal(NaiveLongestCommonSubstringLength(a, b), found.Length);
            Assert.Equal(a.Substring(found.SourceStart, found.Length), b.Substring(found.TargetStart, found.Length));
        }
    }

    [Fact]
    public void LongestCommonSubstringNormalized()
    {
        Assert.Equal(1.0, LongestCommonSubstring.NormalizedSimilarity("", ""));
        Assert.Equal(4.0 / 7, LongestCommonSubstring.NormalizedSimilarity("xabcdey", "zzabcdq"), 12);
        Assert.Equal(1 - 4.0 / 7, LongestCommonSubstring.NormalizedDistance("xabcdey", "zzabcdq"), 12);
    }

    // ---- #15 LCS as a string ---------------------------------------------------------

    [Theory]
    [InlineData("abcde", "ace", "ace")]
    [InlineData("abc", "xyz", "")]
    [InlineData("", "abc", "")]
    public void LcsOf(string a, string b, string expected)
    {
        Assert.Equal(expected, LongestCommonSubsequence.Of(a, b));
    }

    [Fact]
    public void LcsOfIsSubsequenceOfBothWithMaximumLength()
    {
        foreach (var (a, b) in RandomPairs(1000))
        {
            var lcs = LongestCommonSubsequence.Of(a, b);
            Assert.True(IsSubsequence(lcs, a));
            Assert.True(IsSubsequence(lcs, b));
            Assert.Equal(LongestCommonSubsequence.Similarity(a.AsSpan(), b.AsSpan()), lcs.Length);
        }
    }

    // ---- #16 ApproximatelyEquals -----------------------------------------------------

    [Fact]
    public void IdenticalIsTrueForEveryTolerance()
    {
        var all = (SimilarityMetric[])Enum.GetValues(typeof(SimilarityMetric));
        foreach (SimilarityTolerance tolerance in Enum.GetValues(typeof(SimilarityTolerance)))
        {
            Assert.True("new york mets".ApproximatelyEquals("new york mets", tolerance, all));
        }
    }

    [Fact]
    public void ApproximatelyEqualsExamples()
    {
        Assert.True("MARTHA".ApproximatelyEquals("MARHTA", SimilarityTolerance.Strong, SimilarityMetric.JaroWinkler));
        Assert.False("abc".ApproximatelyEquals("xyz", SimilarityTolerance.Weak, (SimilarityMetric[])Enum.GetValues(typeof(SimilarityMetric))));
    }

    [Fact]
    public void NoMetricsThrows()
    {
        Assert.Throws<ArgumentException>(() => "a".ApproximatelyEquals("b", SimilarityTolerance.Normal));
    }

    [Fact]
    public void CaseInsensitiveByDefault()
    {
        Assert.True("NEW YORK".ApproximatelyEquals("new york", SimilarityTolerance.Strong, SimilarityMetric.Levenshtein));
        Assert.False("NEW YORK".ApproximatelyEquals("new york", 0.9, caseSensitive: true, SimilarityMetric.Levenshtein));
    }

    [Fact]
    public void CustomThresholdBoundary()
    {
        var exact = StringSimilarity.AverageSimilarity("kitten", "sitting", false, SimilarityMetric.Levenshtein);
        Assert.True("kitten".ApproximatelyEquals("sitting", exact, SimilarityMetric.Levenshtein));
        Assert.False("kitten".ApproximatelyEquals("sitting", exact + 1e-9, SimilarityMetric.Levenshtein));
    }

    [Fact]
    public void EveryMetricIsSupported()
    {
        foreach (SimilarityMetric metric in Enum.GetValues(typeof(SimilarityMetric)))
        {
            Assert.InRange(StringSimilarity.NormalizedSimilarity("kitten", "sitting", metric), 0.0, 1.0);
            Assert.Equal(1.0, StringSimilarity.NormalizedSimilarity("same", "same", metric), 12);
        }
    }

    // ---- Helpers ----------------------------------------------------------------------

    private static (string A, string B)[] RandomPairs(int count)
    {
        var random = new Random(8);
        return Enumerable.Range(0, count)
            .Select(_ => (Letters(random, random.Next(0, 30)), Letters(random, random.Next(0, 30))))
            .ToArray();
    }

    private static string Letters(Random random, int length) =>
        new(Enumerable.Range(0, length).Select(_ => "abcd"[random.Next(4)]).ToArray());

    private static int NaiveLongestCommonSubstringLength(string a, string b)
    {
        var best = 0;
        for (var i = 0; i < a.Length; i++)
        for (var j = 0; j < b.Length; j++)
        {
            var k = 0;
            while (i + k < a.Length && j + k < b.Length && a[i + k] == b[j + k]) k++;
            best = Math.Max(best, k);
        }

        return best;
    }

    private static bool IsSubsequence(string sub, string s)
    {
        var k = 0;
        foreach (var c in s)
        {
            if (k < sub.Length && sub[k] == c) k++;
        }

        return k == sub.Length;
    }
}
