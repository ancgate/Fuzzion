using Fuzzion.SimilarityRatio.Strategy;

namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

/// <summary>
/// Searches the shorter input approximately within the longer one using Indel distance.
/// Not numerically equivalent to <see cref="PartialRatioScorer"/>.
/// Used by <see cref="Fuzz.ApproximateSubstringRatio(string, string)"/>.
/// </summary>
public sealed class ApproximateSubstringRatioScorer : SimpleRatioScorerBase
{
    /// <inheritdoc />
    protected override Fuzzion.Scorer Scorer =>
        ApproximateSubstringRatioStrategy.Calculate;
}
