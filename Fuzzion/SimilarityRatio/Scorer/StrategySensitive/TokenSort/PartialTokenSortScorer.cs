using Fuzzion.SimilarityRatio.Strategy;

namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

public sealed class PartialTokenSortScorer : TokenSortScorerBase
{
    protected override Fuzzion.Scorer Scorer => PartialRatioStrategy.Calculate;
}