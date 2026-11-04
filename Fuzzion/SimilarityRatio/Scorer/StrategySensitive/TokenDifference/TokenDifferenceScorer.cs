using Fuzzion.SimilarityRatio.Strategy.Generic;
using System;

namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

public sealed class TokenDifferenceScorer : TokenDifferenceScorerBase
{
    protected override Func<string[], string[], double> Scorer => DefaultRatioStrategy<string>.Calculate;
}
