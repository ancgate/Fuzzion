using Fuzzion.SimilarityRatio.Strategy;

namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

public sealed class PartialRatioScorer : SimpleRatioScorerBase
{
    protected override Fuzzion.Scorer Scorer => PartialRatioStrategy.Calculate;
}