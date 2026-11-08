using Fuzzion.SimilarityRatio.Strategy;

namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

/// <summary>
/// The simple ratio: the normalized Indel similarity of the two strings, from 0 to 100.
/// Two empty inputs score 100; exactly one empty input scores 0.
/// Used by <see cref="Fuzz.Ratio(string, string)"/>.
/// </summary>
public sealed class DefaultRatioScorer : SimpleRatioScorerBase
{
    /// <inheritdoc />
    protected override Fuzzion.Scorer Scorer => DefaultRatioStrategy.Calculate;
}