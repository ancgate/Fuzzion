using Fuzzion.SimilarityRatio.Strategy;

namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

public sealed class PartialTokenInitialismScorer : TokenInitialismScorerBase
{
    protected override Fuzzion.Scorer Scorer => PartialRatioStrategy.Calculate;
}