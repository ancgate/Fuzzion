using Fuzzion.SimilarityRatio.Strategy;

namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

public sealed class TokenSetScorer : TokenSetScorerBase
{
    protected override Fuzzion.Scorer Scorer => DefaultRatioStrategy.Calculate;
}