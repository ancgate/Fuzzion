using System;
using Fuzzion.SimilarityRatio.Strategy.Generic;

namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

/// <summary>
/// Splits the strings into sorted words and computes the partial ratio on those words
/// (not the individual characters, but the words themselves).
/// Used by <see cref="Fuzz.PartialTokenDifferenceRatio(string, string)"/>.
/// </summary>
public sealed class PartialTokenDifferenceScorer : TokenDifferenceScorerBase
{
    /// <inheritdoc />
    protected override Func<string[], string[], double> Scorer => static (strings1, strings2) => PartialRatioStrategy<string>.Calculate(strings1.AsSpan(), strings2.AsSpan());
}
