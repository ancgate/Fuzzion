using System;
using System.Linq;
using Fuzzion.PreProcess;
using Fuzzion.SimilarityRatio;
using Fuzzion.SimilarityRatio.Scorer;
using Fuzzion.SimilarityRatio.Scorer.StrategySensitive;
using Xunit;

namespace Fuzzion.Tests.FuzzyTests;

/// <summary>
/// Plan items #17-#20 and #23. Expected values are from Python rapidfuzz 3.14.6.
/// </summary>
public class Phase3ScorerTests
{
    private static readonly string[] Words =
        ["new", "york", "mets", "braves", "vs", "chicago", "cubs", "the", "a", "fuzzy", "wuzzy", "was", "bear", "smith", "corp"];

    // ---- #17 QRatio ------------------------------------------------------------------

    [Theory]
    [InlineData("this is a test", "this is a test!", 96.551724)]
    [InlineData("", "abc", 0)]
    [InlineData("abc", "", 0)]
    [InlineData("", "", 0)]
    [InlineData("abc", "abc", 100)]
    public void QRatioKnownValues(string a, string b, double expected)
    {
        Assert.Equal(expected, Fuzz.QRatio(a, b), 6);
    }

    [Fact]
    public void QRatioDiffersFromRatioOnlyForEmptyPair()
    {
        Assert.Equal(100, Fuzz.Ratio("", ""));
        Assert.Equal(0, Fuzz.QRatio("", ""));
        foreach (var (a, b) in RandomSentencePairs(500))
        {
            if (a.Length > 0 && b.Length > 0)
            {
                Assert.Equal(Fuzz.Ratio(a, b), Fuzz.QRatio(a, b), 12);
            }
        }
    }

    [Fact]
    public void QRatioPreprocessorToEmptyIsZero()
    {
        Assert.Equal(0, Fuzz.QRatio("!!!", "abc", StringPreprocessor.Full));
    }

    // ---- #18 TokenRatio --------------------------------------------------------------

    [Theory]
    [InlineData("fuzzy wuzzy was a bear", "wuzzy fuzzy was a bear", 100)]
    [InlineData("new york mets", "new york mets vs atlanta braves", 100)]
    [InlineData("this is a test", "this is a test!", 96.551724)]
    public void TokenRatioKnownValues(string a, string b, double expected)
    {
        Assert.Equal(expected, Fuzz.TokenRatio(a, b), 6);
    }

    [Fact]
    public void TokenRatioEqualsMaxOfSortAndSet()
    {
        foreach (var (a, b) in RandomSentencePairs(1000))
        {
            Assert.Equal(Math.Max(Fuzz.TokenSortRatio(a, b), Fuzz.TokenSetRatio(a, b)), Fuzz.TokenRatio(a, b), 12);
        }
    }

    // ---- #19 PartialTokenRatio -------------------------------------------------------

    [Theory]
    [InlineData("this is a test", "this is a test!", 100)]
    [InlineData("new york mets", "new york mets vs atlanta braves", 100)]
    [InlineData("", "abc", 0)]
    public void PartialTokenRatioKnownValues(string a, string b, double expected)
    {
        Assert.Equal(expected, Fuzz.PartialTokenRatio(a, b), 6);
    }

    [Fact]
    public void PartialTokenRatioEqualsMaxOfPartialSortAndSet()
    {
        foreach (var (a, b) in RandomSentencePairs(1000))
        {
            var expected = Math.Max(Fuzz.PartialTokenSortRatio(a, b), Fuzz.PartialTokenSetRatio(a, b));
            Assert.Equal(expected, Fuzz.PartialTokenRatio(a, b), 12);
        }
    }

    [Fact]
    public void PartialTokenRatioIs100WhenAWordIsShared()
    {
        Assert.Equal(100, Fuzz.PartialTokenRatio("chicago cubs vs philadelphia phillies", "braves vs mets"));
    }

    // ---- #20 PartialRatioAlignment ---------------------------------------------------

    [Theory]
    [InlineData("this is a test", "yes this is a test!", 100, 0, 14, 4, 18)]
    [InlineData("new york mets", "the new york mets vs atlanta braves", 100, 0, 13, 4, 17)]
    [InlineData("", "", 100, 0, 0, 0, 0)]
    public void PartialRatioAlignmentKnownValues(string a, string b, double score, int srcStart, int srcEnd, int destStart, int destEnd)
    {
        var alignment = Fuzz.PartialRatioAlignment(a, b);
        Assert.Equal(new ScoreAlignment(score, srcStart, srcEnd, destStart, destEnd), alignment);
    }

    [Fact]
    public void AlignmentScoreEqualsPartialRatio()
    {
        foreach (var (a, b) in RandomSentencePairs(1000))
        {
            Assert.Equal(Fuzz.PartialRatio(a, b), Fuzz.PartialRatioAlignment(a, b).Score, 12);
        }
    }

    [Fact]
    public void RatioOfAlignedSlicesEqualsScore()
    {
        foreach (var (a, b) in RandomSentencePairs(1000).Where(p => p.A.Length > 0 && p.B.Length > 0))
        {
            var (score, srcStart, srcEnd, destStart, destEnd) = Fuzz.PartialRatioAlignment(a, b);
            var sliceRatio = Fuzz.Ratio(a.Substring(srcStart, srcEnd - srcStart), b.Substring(destStart, destEnd - destStart));
            Assert.Equal(score, sliceRatio, 9);
        }
    }

    [Fact]
    public void AlignmentWithPreprocessorRefersToProcessedStrings()
    {
        var alignment = Fuzz.PartialRatioAlignment("NEW YORK METS!", "The New York Mets vs Atlanta", StringPreprocessor.Full);
        Assert.Equal(100, alignment.Score);
        Assert.Equal("new york mets", "the new york mets vs atlanta".Substring(alignment.DestStart, alignment.DestEnd - alignment.DestStart));
    }

    [Fact]
    public void ScoreAlignmentEqualityAndToString()
    {
        var a = new ScoreAlignment(50, 1, 2, 3, 4);
        Assert.Equal(a, new ScoreAlignment(50, 1, 2, 3, 4));
        Assert.True(a != new ScoreAlignment(50, 1, 2, 3, 5));
        Assert.Contains("SourceStart=1", a.ToString());
    }

    // ---- #23 Cached scorers ----------------------------------------------------------

    public static TheoryData<string> CachedScorerNames => new()
    {
        "QRatio", "PartialRatio", "PartialTokenSort", "TokenRatio", "PartialTokenRatio",
    };

    [Theory]
    [MemberData(nameof(CachedScorerNames))]
    public void CachedEqualsUncached(string name)
    {
        var (uncached, createCached) = Scorers(name);
        var random = new Random(name.GetHashCode() & 0xFFFF);

        for (var i = 0; i < 200; i++)
        {
            var query = Sentence(random, random.Next(0, 5));
            using var cached = createCached(query);
            for (var j = 0; j < 10; j++)
            {
                var candidate = Sentence(random, random.Next(0, 10));
                Assert.Equal(uncached.Score(query, candidate), cached.Score(candidate), 9);
            }
        }
    }

    [Fact]
    public void CachedPartialRatioAcrossBlockBoundaries()
    {
        var random = new Random(64);
        foreach (var length in new[] { 1, 63, 64, 65, 127, 128, 129, 300 })
        {
            var query = RandomLetters(random, length);
            using var cached = new CachedPartialRatioScorer(query);
            foreach (var otherLength in new[] { 0, length - 1, length, length + 1, length * 2 + 3 })
            {
                var other = RandomLetters(random, Math.Max(0, otherLength));
                Assert.Equal(Fuzz.PartialRatio(query, other), cached.Score(other), 9);
            }
        }
    }

    [Theory]
    [MemberData(nameof(CachedScorerNames))]
    public void CachedScorerWorksInPipeline(string name)
    {
        var (uncached, createCached) = Scorers(name);
        string[] choices = ["new york mets vs chicago cubs", "chicago cubs vs chicago white sox", "philadelphia phillies vs atlanta braves", "braves vs mets"];
        const string query = "new york mets at atlanta braves";

        using var cached = createCached(query);
        var viaCached = Process.Configure().Cached(cached).Build().ExtractOne(choices);
        var viaUncached = Process.ExtractOne(query, choices, s => s, uncached);

        Assert.Equal(viaUncached.Value, viaCached.Value);
        Assert.Equal(viaUncached.Score, viaCached.Score, 9);
    }

    [Theory]
    [MemberData(nameof(CachedScorerNames))]
    public void CachedScorerDisposeTwice(string name)
    {
        var cached = Scorers(name).CreateCached("some query");
        cached.Dispose();
        cached.Dispose();
    }

    [Fact]
    public void CachedQRatioAndPartialRatioApplyPreprocessor()
    {
        using var q = new CachedQRatioScorer("NEW YORK", StringPreprocessor.Full);
        Assert.Equal(100, q.Score("new york!"));
        using var p = new CachedPartialRatioScorer("NEW YORK", StringPreprocessor.Full);
        Assert.Equal(100, p.Score("the new york mets"));
    }

    private static (IRatioScorer Uncached, Func<string, ICachedRatioScorer> CreateCached) Scorers(string name) => name switch
    {
        "QRatio" => (ScorerCache.Get<QRatioScorer>(), q => new CachedQRatioScorer(q)),
        "PartialRatio" => (ScorerCache.Get<PartialRatioScorer>(), q => new CachedPartialRatioScorer(q)),
        "PartialTokenSort" => (ScorerCache.Get<PartialTokenSortScorer>(), q => new CachedPartialTokenSortScorer(q)),
        "TokenRatio" => (ScorerCache.Get<TokenRatioScorer>(), q => new CachedTokenRatioScorer(q)),
        "PartialTokenRatio" => (ScorerCache.Get<PartialTokenRatioScorer>(), q => new CachedPartialTokenRatioScorer(q)),
        _ => throw new ArgumentOutOfRangeException(nameof(name)),
    };

    private static (string A, string B)[] RandomSentencePairs(int count)
    {
        var random = new Random(2026);
        return Enumerable.Range(0, count)
            .Select(_ => (Sentence(random, random.Next(0, 6)), Sentence(random, random.Next(0, 9))))
            .ToArray();
    }

    private static string Sentence(Random random, int words) =>
        string.Join(" ", Enumerable.Range(0, words).Select(_ => Words[random.Next(Words.Length)]));

    private static string RandomLetters(Random random, int length) =>
        new(Enumerable.Range(0, length).Select(_ => "abcd"[random.Next(4)]).ToArray());
}
