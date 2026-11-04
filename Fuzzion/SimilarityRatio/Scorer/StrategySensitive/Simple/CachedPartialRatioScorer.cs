using System;
using Fuzzion.PreProcess;
using Fuzzion.SimilarityRatio.Strategy.Generic;
using Fuzzion.Utils;

namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

/// <summary>
/// <see cref="PartialRatioScorer"/> against one fixed query. The query's bit pattern is
/// built once and reused whenever the query is the shorter (or equal-length) input.
/// Dispose it when done.
/// </summary>
public sealed class CachedPartialRatioScorer : ICachedRatioScorer
{
    private readonly Func<string, string> _preprocessor;
    private readonly string _query;
    private readonly IPatternMatchVector<char> _queryVector;
    private bool _disposed;

    /// <param name="input1">The query.</param>
    /// <param name="preprocessor">Applied to the query and to every value; none by default.</param>
    public CachedPartialRatioScorer(string input1, Func<string, string> preprocessor = null)
    {
        _preprocessor = preprocessor ?? StringPreprocessor.None;
        _query = _preprocessor(input1);
        _queryVector = PatternMatchVector.Create(_query.AsSpan());
    }

    /// <inheritdoc/>
    public double Score(string input2)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(CachedPartialRatioScorer));

        return PartialRatioStrategy<char>.CalculateWithQueryVector(_queryVector, _query.AsSpan(), _preprocessor(input2).AsSpan());
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _queryVector.Dispose();
    }
}
