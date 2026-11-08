using Fuzzion.SimilarityRatio.Strategy;

namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

/// <summary>
/// Sorts the words of both strings, then compares the results with the partial ratio.
/// Used by <see cref="Fuzz.PartialTokenSortRatio(string, string)"/>.
/// </summary>
public sealed class PartialTokenSortScorer : TokenSortScorerBase
{
    /// <inheritdoc />
    protected override Fuzzion.Scorer Scorer => PartialRatioStrategy.Calculate;
}