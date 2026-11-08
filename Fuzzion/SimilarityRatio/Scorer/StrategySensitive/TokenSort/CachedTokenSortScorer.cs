using Fuzzion.Extensions;
using Fuzzion.SimilarityRatio.Strategy;

namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

/// <summary>
/// <see cref="TokenSortScorer"/> against one fixed query: the query's words are sorted and its
/// bit pattern built once. No preprocessing is applied. Dispose it when done.
/// </summary>
public sealed class CachedTokenSortScorer : ICachedRatioScorer
{
    private readonly ICachedStrategy _strategy;
    private readonly bool _isStrategyOwner;

    /// <summary>Prepares <paramref name="input1"/> as the query.</summary>
    /// <param name="input1">The query.</param>
    public CachedTokenSortScorer(string input1)
    {
        var sorted1 = input1.NormalizeSpacesAndSort();
        _strategy = new CachedDefaultRatioStrategy(sorted1);
        _isStrategyOwner = true;
    }

    /// <summary>
    /// Scores with an existing prepared query, which must have been prepared for the query with
    /// its words already sorted: <see cref="Score"/> sorts only the words of each value.
    /// </summary>
    /// <param name="strategy">The prepared query, with its words sorted.</param>
    /// <param name="isStrategyOwner">Whether <see cref="Dispose"/> also disposes <paramref name="strategy"/>;
    /// false by default, so the caller keeps ownership.</param>
    public CachedTokenSortScorer(ICachedStrategy strategy, bool isStrategyOwner = false)
    {
        _strategy = strategy;
        _isStrategyOwner = isStrategyOwner;
    }

    /// <inheritdoc />
    public double Score(string input2)
    {
        var sorted2 = input2.NormalizeSpacesAndSort();
        return _strategy.Calculate(sorted2);
    }

    /// <summary>Releases the pooled memory held for the query, if this scorer owns it.</summary>
    public void Dispose()
    {
        if(_isStrategyOwner)
        {
            _strategy.Dispose();
        }
    }
}
