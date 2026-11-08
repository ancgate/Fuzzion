using Fuzzion.SimilarityRatio.Strategy;

namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

/// <summary>
/// <see cref="PartialTokenSetScorer"/> against one fixed query: the query is split into words once.
/// </summary>
/// <param name="input1">The query.</param>
public sealed class CachedPartialTokenSetScorer(string input1) : CachedTokenSetScorerBase(input1)
{
    /// <inheritdoc />
    protected override Fuzzion.Scorer Scorer => PartialRatioStrategy.Calculate;
}