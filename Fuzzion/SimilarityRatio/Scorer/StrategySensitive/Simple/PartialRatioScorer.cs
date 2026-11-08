using Fuzzion.SimilarityRatio.Strategy;

namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

/// <summary>
/// The partial ratio: the simple ratio of the shorter string against its best-matching part of
/// the longer one. Two empty inputs score 100; exactly one empty input scores 0.
/// Used by <see cref="Fuzz.PartialRatio(string, string)"/>.
/// </summary>
public sealed class PartialRatioScorer : SimpleRatioScorerBase
{
    /// <inheritdoc />
    protected override Fuzzion.Scorer Scorer => PartialRatioStrategy.Calculate;
}