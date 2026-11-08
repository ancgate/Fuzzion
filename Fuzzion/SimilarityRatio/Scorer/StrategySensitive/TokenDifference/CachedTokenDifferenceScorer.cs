using System;
using Fuzzion.Extensions;
using Fuzzion.PreProcess;
using Fuzzion.SimilarityRatio.Strategy.Generic;

namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

/// <summary>
/// <see cref="TokenDifferenceScorer"/> against one fixed query: the query's sorted words and
/// their bit pattern are built once. Dispose it when done.
/// </summary>
public sealed class CachedTokenDifferenceScorer : ICachedRatioScorer
{
    private readonly Func<string, string> _preprocessor;
    private readonly CachedDefaultRatioStrategy<string> _scorer;

    /// <summary>Prepares <paramref name="input1"/> as the query.</summary>
    /// <param name="input1">The query.</param>
    /// <param name="preprocessor">Applied to the query and to every value; none by default.</param>
    public CachedTokenDifferenceScorer(string input1, Func<string, string> preprocessor = null)
    {
        _preprocessor = preprocessor ?? StringPreprocessor.None;
        var preprocessedInput1 = _preprocessor(input1);
        var tokens1 = preprocessedInput1.GetSortedWords();
        _scorer = new CachedDefaultRatioStrategy<string>(tokens1);
    }

    /// <inheritdoc />
    public double Score(string input2)
    {
        input2 = _preprocessor(input2);
        var tokens2 = input2.GetSortedWords();
        return _scorer.Calculate(tokens2);
    }

    /// <summary>Releases the pooled memory held for the query.</summary>
    public void Dispose()
    {
        _scorer.Dispose();
    }
}
