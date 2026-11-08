using Fuzzion.SimilarityRatio.Strategy;

namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

/// <summary>
/// Sorts the words of both strings, then compares the results with the simple ratio.
/// Used by <see cref="Fuzz.TokenSortRatio(string, string)"/>.
/// </summary>
public sealed class TokenSortScorer : TokenSortScorerBase
{
    /// <inheritdoc />
    protected override Fuzzion.Scorer Scorer => DefaultRatioStrategy.Calculate;
}