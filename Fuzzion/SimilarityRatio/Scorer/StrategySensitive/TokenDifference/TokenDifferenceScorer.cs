using Fuzzion.SimilarityRatio.Strategy.Generic;
using System;

namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

/// <summary>
/// Splits the strings into sorted words and computes the simple ratio on those words
/// (not the individual characters, but the words themselves).
/// Used by <see cref="Fuzz.TokenDifferenceRatio(string, string)"/>.
/// </summary>
public sealed class TokenDifferenceScorer : TokenDifferenceScorerBase
{
    /// <inheritdoc />
    protected override Func<string[], string[], double> Scorer => DefaultRatioStrategy<string>.Calculate;
}
