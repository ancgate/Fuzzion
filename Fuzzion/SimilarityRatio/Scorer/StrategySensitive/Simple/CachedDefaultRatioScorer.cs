using System;
using Fuzzion.PreProcess;
using Fuzzion.SimilarityRatio.Strategy;

namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

/// <summary>
/// <see cref="DefaultRatioScorer"/> against one fixed query: the query is preprocessed and its
/// bit pattern built once. Dispose it when done.
/// </summary>
public sealed class CachedDefaultRatioScorer : CachedSimpleRatioScorerBase
{
    private readonly ICachedStrategy _strategy;
    private readonly bool _isStrategyOwner;

    /// <summary>Prepares <paramref name="input1"/> as the query.</summary>
    /// <param name="input1">The query.</param>
    /// <param name="preprocessor">Applied to the query and to every value; none by default.</param>
    public CachedDefaultRatioScorer(string input1, Func<string, string> preprocessor = null)
    {
        _strategy = new CachedDefaultRatioStrategy(input1, preprocessor);
        _isStrategyOwner = true;
    }

    /// <summary>
    /// Scores with an existing prepared query, for example one shared with a <see cref="CachedTokenSortScorer"/>.
    /// </summary>
    /// <param name="strategy">The prepared query; each value is passed to it unchanged.</param>
    /// <param name="isStrategyOwner">Whether <see cref="Dispose"/> also disposes <paramref name="strategy"/>;
    /// false by default, so the caller keeps ownership.</param>
    public CachedDefaultRatioScorer(ICachedStrategy strategy, bool isStrategyOwner = false)
    {
        _strategy = strategy;
        _isStrategyOwner = isStrategyOwner;
    }

    /// <inheritdoc />
    protected override CachedScorer Scorer => input2 => _strategy.Calculate(input2);
    /// <inheritdoc />
    public override void Dispose()
    {
        if (_isStrategyOwner)
        {
            _strategy.Dispose();
        }
    }
}