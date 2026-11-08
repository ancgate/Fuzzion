using Fuzzion.SimilarityRatio.Strategy;

namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

/// <summary>
/// Compares the common and remaining words of the two strings with the simple ratio;
/// useful for strings where words appear redundantly.
/// Used by <see cref="Fuzz.TokenSetRatio(string, string)"/>.
/// </summary>
public sealed class TokenSetScorer : TokenSetScorerBase
{
    /// <inheritdoc />
    protected override Fuzzion.Scorer Scorer => DefaultRatioStrategy.Calculate;
}