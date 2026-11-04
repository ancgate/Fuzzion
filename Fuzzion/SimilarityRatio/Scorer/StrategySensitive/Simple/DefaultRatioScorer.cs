using Fuzzion.SimilarityRatio.Strategy;

namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

public sealed class DefaultRatioScorer : SimpleRatioScorerBase
{
    protected override Fuzzion.Scorer Scorer => DefaultRatioStrategy.Calculate;
}