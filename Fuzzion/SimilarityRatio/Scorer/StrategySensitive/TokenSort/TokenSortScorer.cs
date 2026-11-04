using Fuzzion.SimilarityRatio.Strategy;

namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

public sealed class TokenSortScorer : TokenSortScorerBase
{
    protected override Fuzzion.Scorer Scorer => DefaultRatioStrategy.Calculate;
}