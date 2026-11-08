using Fuzzion.SimilarityRatio.Strategy;

namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

/// <summary>
/// Takes the initialism of the longer string's words and compares it to the shorter string
/// with the partial ratio. Used by <see cref="Fuzz.PartialTokenInitialismRatio(string, string)"/>.
/// </summary>
public sealed class PartialTokenInitialismScorer : TokenInitialismScorerBase
{
    /// <inheritdoc />
    protected override Fuzzion.Scorer Scorer => PartialRatioStrategy.Calculate;
}