using Fuzzion.SimilarityRatio.Strategy;

namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

/// <summary>
/// Scores whether one string's words are abbreviations of the other's, comparing word pairs
/// with the simple ratio. Used by <see cref="Fuzz.TokenAbbreviationRatio(string, string)"/>.
/// </summary>
public sealed class TokenAbbreviationScorer : TokenAbbreviationScorerBase
{
    /// <inheritdoc />
    protected override Fuzzion.Scorer Scorer => DefaultRatioStrategy.Calculate;
}