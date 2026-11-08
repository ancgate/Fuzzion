using Fuzzion.Extensions;

namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

/// <summary>
/// <see cref="PartialTokenSortScorer"/> against one fixed query: the query's words are
/// sorted and its bit pattern built once. Dispose it when done.
/// </summary>
public sealed class CachedPartialTokenSortScorer(string input1) : ICachedRatioScorer
{
    private readonly CachedPartialRatioScorer _partial = new(input1.NormalizeSpacesAndSort());

    /// <inheritdoc/>
    public double Score(string input2) => _partial.Score(input2.NormalizeSpacesAndSort());

    /// <inheritdoc/>
    public void Dispose() => _partial.Dispose();
}
