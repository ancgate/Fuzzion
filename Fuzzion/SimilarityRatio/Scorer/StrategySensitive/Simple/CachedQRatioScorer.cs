using System;
using Fuzzion.PreProcess;

namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

/// <summary>
/// <see cref="QRatioScorer"/> against one fixed query: the query is preprocessed and its
/// bit pattern built once. Dispose it when done.
/// </summary>
public sealed class CachedQRatioScorer : ICachedRatioScorer
{
    private readonly Func<string, string> _preprocessor;
    private readonly bool _queryIsEmpty;
    private readonly Indel _indel;
    private bool _disposed;

    /// <param name="input1">The query.</param>
    /// <param name="preprocessor">Applied to the query and to every value; none by default.</param>
    public CachedQRatioScorer(string input1, Func<string, string> preprocessor = null)
    {
        _preprocessor = preprocessor ?? StringPreprocessor.None;
        var query = _preprocessor(input1);
        _queryIsEmpty = query.Length == 0;
        _indel = new Indel(query);
    }

    /// <inheritdoc/>
    public double Score(string input2)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(CachedQRatioScorer));

        var value = _preprocessor(input2);
        if (_queryIsEmpty || value.Length == 0)
        {
            return 0;
        }

        return 100 * _indel.NormalizedSimilarityWith(value);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _indel.Dispose();
    }
}
