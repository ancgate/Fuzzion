using Fuzzion.SimilarityRatio.Strategy;

namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

public sealed class CachedPartialTokenSetScorer(string input1) : CachedTokenSetScorerBase(input1)
{
    protected override Fuzzion.Scorer Scorer => PartialRatioStrategy.Calculate;
}