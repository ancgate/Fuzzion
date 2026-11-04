using Fuzzion.SimilarityRatio.Strategy;

namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

public sealed class TokenAbbreviationScorer : TokenAbbreviationScorerBase
{
    protected override Fuzzion.Scorer Scorer => DefaultRatioStrategy.Calculate;
}