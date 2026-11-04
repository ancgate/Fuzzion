using Fuzzion.SimilarityRatio.Strategy;

namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

public sealed class PartialTokenAbbreviationScorer : TokenAbbreviationScorerBase
{
    protected override Fuzzion.Scorer Scorer => PartialRatioStrategy.Calculate;
}