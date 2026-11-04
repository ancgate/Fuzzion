using Fuzzion.SimilarityRatio.Strategy;

namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

public sealed class PartialTokenSetScorer : TokenSetScorerBase
{
    protected override Fuzzion.Scorer Scorer => PartialRatioStrategy.Calculate;
}