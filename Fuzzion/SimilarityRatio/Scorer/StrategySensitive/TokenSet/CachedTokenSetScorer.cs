using Fuzzion.SimilarityRatio.Strategy;

namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

public sealed class CachedTokenSetScorer(string input1) : CachedTokenSetScorerBase(input1)
{
    protected override Fuzzion.Scorer Scorer => DefaultRatioStrategy.Calculate;
}