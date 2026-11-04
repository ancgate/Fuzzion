using Fuzzion.SimilarityRatio.Strategy;

namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

public sealed class ApproximateSubstringRatioScorer : SimpleRatioScorerBase
{
    protected override Fuzzion.Scorer Scorer =>
        ApproximateSubstringRatioStrategy.Calculate;
}
