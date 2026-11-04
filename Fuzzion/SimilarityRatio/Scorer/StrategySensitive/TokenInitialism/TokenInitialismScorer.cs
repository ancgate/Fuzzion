using Fuzzion.SimilarityRatio.Strategy;

namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

public sealed class TokenInitialismScorer : TokenInitialismScorerBase
{
    protected override Fuzzion.Scorer Scorer => DefaultRatioStrategy.Calculate;
}