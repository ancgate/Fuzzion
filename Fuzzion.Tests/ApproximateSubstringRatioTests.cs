using System;
using System.Threading.Tasks;
using Fuzzion.SimilarityRatio.Scorer.StrategySensitive;
using Fuzzion.SimilarityRatio.Strategy;
using Xunit;

namespace Fuzzion.Tests;

public class ApproximateSubstringRatioTests
{
    [Theory]
    [InlineData("abc", "xxabcxx", 100)]
    [InlineData("xxabcxx", "abc", 100)]
    [InlineData("", "", 100)]
    [InlineData("abc", "", 0)]
    [InlineData("", "abc", 0)]
    public void ApproximateSubstringRatio_UsesExpectedEmptyAndExactSemantics(
        string input1,
        string input2,
        double expected)
    {
        Assert.Equal(expected, Fuzz.ApproximateSubstringRatio(input1, input2));
    }

    [Fact]
    public void ApproximateSubstringRatio_AppliesPreprocessor()
    {
        Assert.Equal(100, Fuzz.ApproximateSubstringRatio(
            "INVOICE-123",
            "processed invoice 123 successfully",
            input => input.Replace("-", " ").ToLowerInvariant()));
    }

    [Fact]
    public void ApproximateSubstringRatio_IsSymmetricAndUsesBetterEqualLengthDirection()
    {
        const string input1 = "abca";
        const string input2 = "caba";

        double expected = Math.Max(
            DirectionalScore(input1, input2),
            DirectionalScore(input2, input1));

        Assert.Equal(expected, Fuzz.ApproximateSubstringRatio(input1, input2));
        Assert.Equal(
            Fuzz.ApproximateSubstringRatio(input1, input2),
            Fuzz.ApproximateSubstringRatio(input2, input1));
    }

    [Fact]
    public void ApproximateSubstringRatio_DoesNotRetainEarlierInferiorOccurrence()
    {
        Assert.Equal(100, Fuzz.ApproximateSubstringRatio("abc", "abxabc"));
    }

    [Fact]
    public void ApproximateSubstringRatio_IsASeparateMetricFromPartialRatio()
    {
        double approximate = Fuzz.ApproximateSubstringRatio("abc", "axc");
        double partial = Fuzz.PartialRatio("abc", "axc");

        Assert.NotEqual(partial, approximate);
    }

    [Fact]
    public void ApproximateSubstringRatio_RemainsInRange()
    {
        var random = new Random(10091);

        for (int sample = 0; sample < 500; sample++)
        {
            string input1 = CreateRandomString(random, random.Next(0, 140));
            string input2 = CreateRandomString(random, random.Next(0, 200));
            double score = Fuzz.ApproximateSubstringRatio(input1, input2);

            Assert.InRange(score, 0.0, 100.0);
            Assert.Equal(score, Fuzz.ApproximateSubstringRatio(input2, input1));
        }
    }

    [Fact]
    public void CachedApproximateSubstringRatioScorer_MatchesNonCachedScorer()
    {
        const string query = "invoice number 12345";
        string[] candidates =
        {
            "processed invoice number 12345 successfully",
            "invoice 12345",
            "unrelated content",
            "",
            "invoice number 12345"
        };

        using var scorer = new CachedApproximateSubstringRatioScorer(query);
        foreach (string candidate in candidates)
        {
            Assert.Equal(
                Fuzz.ApproximateSubstringRatio(query, candidate),
                scorer.Score(candidate));
        }
    }

    [Fact]
    public void CachedApproximateSubstringRatioScorer_HandlesEmptyQuery()
    {
        using var scorer = new CachedApproximateSubstringRatioScorer(string.Empty);

        Assert.Equal(100, scorer.Score(string.Empty));
        Assert.Equal(0, scorer.Score("candidate"));
    }

    [Fact]
    public void CachedApproximateSubstringRatioScorer_IsSafeForConcurrentScores()
    {
        const string query = "the quick brown fox jumps over the lazy dog";
        string[] candidates =
        {
            "a quick brown fox jumps over a dog",
            "the quick brown fox jumps over the lazy dog",
            "unrelated text",
            "quick brown fox"
        };
        var expected = new double[candidates.Length];

        for (int index = 0; index < candidates.Length; index++)
        {
            expected[index] = Fuzz.ApproximateSubstringRatio(query, candidates[index]);
        }

        var actual = new double[256];
        using var scorer = new CachedApproximateSubstringRatioScorer(query);
        Parallel.For(0, 256, iteration =>
        {
            int index = iteration % candidates.Length;
            actual[iteration] = scorer.Score(candidates[index]);
        });

        for (int iteration = 0; iteration < actual.Length; iteration++)
        {
            Assert.Equal(expected[iteration % candidates.Length], actual[iteration]);
        }
    }

    [Fact]
    public void CachedApproximateSubstringRatioScorer_RespectsStrategyOwnership()
    {
        var externallyOwned = new TrackingStrategy();
        using (var scorer = new CachedApproximateSubstringRatioScorer(externallyOwned))
        {
            Assert.Equal(42, scorer.Score("candidate"));
        }
        Assert.False(externallyOwned.Disposed);

        var scorerOwned = new TrackingStrategy();
        var owningScorer = new CachedApproximateSubstringRatioScorer(scorerOwned, true);
        Assert.Equal(42, owningScorer.Score("candidate"));
        owningScorer.Dispose();
        owningScorer.Dispose();
        Assert.True(scorerOwned.Disposed);
        Assert.Equal(1, scorerOwned.DisposeCount);
    }

    [Fact]
    public void CachedApproximateSubstringRatioScorer_ThrowsAfterDisposal()
    {
        var scorer = new CachedApproximateSubstringRatioScorer("query");

        scorer.Dispose();

        Assert.Throws<ObjectDisposedException>(() => scorer.Score("candidate"));
        scorer.Dispose();
    }

    [Fact]
    public void CachedApproximateSubstringRatioScorer_MatchesNonCachedAcrossBoundaryLengths()
    {
        var random = new Random(81927);
        int[] queryLengths = { 63, 64, 65, 127, 128, 129 };

        foreach (int queryLength in queryLengths)
        {
            string query = CreateRandomString(random, queryLength);
            string[] candidates =
            {
                string.Empty,
                query,
                query.Substring(0, queryLength - 1),
                "prefix-" + query + "-suffix",
                new string('z', queryLength + 17),
                query.Substring(0, queryLength / 2) + "#" + query.Substring(queryLength / 2 + 1)
            };

            using var scorer = new CachedApproximateSubstringRatioScorer(query);
            foreach (string candidate in candidates)
            {
                Assert.Equal(
                    Fuzz.ApproximateSubstringRatio(query, candidate),
                    scorer.Score(candidate));
            }
        }
    }

    [Fact]
    public void ApproximateSubstringRatioScorer_IsAvailableToProcessBuilder()
    {
        var pipeline = new ProcessBuilder()
            .WithScorer(new ApproximateSubstringRatioScorer())
            .Build();

        Assert.Equal(typeof(ProcessPipeline), pipeline.GetType());
    }

    [Fact]
    public void ApproximateSubstringRatio_ReadmeExampleIs100()
    {
        Assert.Equal(100, Fuzz.ApproximateSubstringRatio(
            "invoice number 12345",
            "processed invoice number 12345 successfully"));
    }

    [Fact]
    public void ApproximateSubstringRatio_ReturnsFractionalScores()
    {
        // One substitution costs two Indel edits: 1 - 2/7 = 0.714285...
        double score = Fuzz.ApproximateSubstringRatio("abcdefg", "xxabcXefgxx");

        Assert.Equal(100.0 * (1 - 2.0 / 7), score, 10);
        Assert.NotEqual(Math.Round(score), score);
    }

    [Fact]
    public void ApproximateSubstringRatio_CharOverloadMatchesGenericOverload()
    {
        var random = new Random(4242);

        for (int sample = 0; sample < 500; sample++)
        {
            string pattern = CreateRandomString(random, random.Next(1, 130));
            string text = CreateRandomString(random, random.Next(0, 200));

            IndelSubstringMatch viaChar = Indel.BestSubstringMatch(pattern.AsSpan(), text.AsSpan());
            IndelSubstringMatch viaGeneric = Indel.BestSubstringMatch<char>(pattern.AsSpan(), text.AsSpan());

            Assert.Equal(viaGeneric.Distance, viaChar.Distance);
        }
    }

    [Fact]
    public void ApproximateSubstringRatio_StringPathMatchesGenericStrategy()
    {
        var random = new Random(777);

        for (int sample = 0; sample < 500; sample++)
        {
            string input1 = CreateRandomString(random, random.Next(0, 140));
            string input2 = CreateRandomString(random, random.Next(0, 140));

            double viaString = Fuzz.ApproximateSubstringRatio(input1, input2);
            double viaGeneric = SimilarityRatio.Strategy.Generic.ApproximateSubstringRatioStrategy<char>
                .Calculate(input1.AsSpan(), input2.AsSpan());

            Assert.Equal(viaGeneric, viaString, 10);
        }
    }

    [Fact]
    public void CachedApproximateSubstringRatioScorer_EqualInputsScore100()
    {
        using var scorer = new CachedApproximateSubstringRatioScorer("same text");

        Assert.Equal(100, scorer.Score("same text"));
    }

    private static double DirectionalScore(string pattern, string text)
    {
        IndelSubstringMatch match = Indel.BestSubstringMatch(
            pattern.AsSpan(), text.AsSpan());
        return ApproximateSubstringScore.FromDistance(
            match.Distance,
            pattern.Length);
    }

    private static string CreateRandomString(Random random, int length)
    {
        const string alphabet = "abcdef";
        var characters = new char[length];
        for (int index = 0; index < characters.Length; index++)
        {
            characters[index] = alphabet[random.Next(alphabet.Length)];
        }

        return new string(characters);
    }

    private sealed class TrackingStrategy : ICachedStrategy
    {
        public bool Disposed { get; private set; }
        public int DisposeCount { get; private set; }

        public double Calculate(string input2)
        {
            return 42;
        }

        public void Dispose()
        {
            DisposeCount++;
            Disposed = true;
        }
    }
}
