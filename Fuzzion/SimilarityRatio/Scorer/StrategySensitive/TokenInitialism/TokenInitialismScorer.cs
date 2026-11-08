using Fuzzion.SimilarityRatio.Strategy;

namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

/// <summary>
/// Takes the initialism of the longer string's words and compares it to the shorter string
/// with the simple ratio. Used by <see cref="Fuzz.TokenInitialismRatio(string, string)"/>.
/// </summary>
public sealed class TokenInitialismScorer : TokenInitialismScorerBase
{
    /// <inheritdoc />
    protected override Fuzzion.Scorer Scorer => DefaultRatioStrategy.Calculate;
}