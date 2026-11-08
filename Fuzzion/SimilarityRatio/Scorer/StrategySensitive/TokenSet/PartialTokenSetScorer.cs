using Fuzzion.SimilarityRatio.Strategy;

namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

/// <summary>
/// Compares the common and remaining words of the two strings with the partial ratio.
/// Used by <see cref="Fuzz.PartialTokenSetRatio(string, string)"/>.
/// </summary>
public sealed class PartialTokenSetScorer : TokenSetScorerBase
{
    /// <inheritdoc />
    protected override Fuzzion.Scorer Scorer => PartialRatioStrategy.Calculate;
}