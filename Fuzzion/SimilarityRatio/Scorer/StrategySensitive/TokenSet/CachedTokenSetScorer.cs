using Fuzzion.SimilarityRatio.Strategy;

namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

/// <summary>
/// <see cref="TokenSetScorer"/> against one fixed query: the query is split into words once.
/// </summary>
/// <param name="input1">The query.</param>
public sealed class CachedTokenSetScorer(string input1) : CachedTokenSetScorerBase(input1)
{
    /// <inheritdoc />
    protected override Fuzzion.Scorer Scorer => DefaultRatioStrategy.Calculate;
}