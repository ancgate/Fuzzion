using Fuzzion.SimilarityRatio.Strategy;

namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

/// <summary>
/// Scores whether one string's words are abbreviations of the other's, comparing word pairs
/// with the partial ratio. Used by <see cref="Fuzz.PartialTokenAbbreviationRatio(string, string)"/>.
/// </summary>
public sealed class PartialTokenAbbreviationScorer : TokenAbbreviationScorerBase
{
    /// <inheritdoc />
    protected override Fuzzion.Scorer Scorer => PartialRatioStrategy.Calculate;
}