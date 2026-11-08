using System;
using Fuzzion.SimilarityRatio.Scorer.StrategySensitive;
using Fuzzion.SimilarityRatio.Strategy;

namespace Fuzzion.SimilarityRatio.Scorer.Composite;

/// <summary>
/// <see cref="WeightedRatioScorer"/> against one fixed query. The simple ratio, token sort, token set
/// and partial token set parts are prepared once; the partial ratio and partial token sort parts are
/// computed for each value. No preprocessing is applied. Dispose it when done.
/// </summary>
public sealed class CachedWeightedRatioScorer : ICachedRatioScorer
{
    private static readonly double UNBASE_SCALE = .95;
    private static readonly double PARTIAL_SCALE = .90;
    private static readonly bool TRY_PARTIALS = true;

    private readonly ICachedStrategy _strategy;
    private readonly CachedDefaultRatioScorer _baseRatioScorer;
    private readonly string _input1;
    private readonly CachedTokenSortScorer _tokenSortScorer;
    private readonly CachedTokenSetScorer _tokenSetScorer;
    private readonly CachedPartialTokenSetScorer _partialTokenSetScorer;
    private bool _disposed;

    /// <summary>Prepares <paramref name="input1"/> as the query.</summary>
    /// <param name="input1">The query.</param>
    public CachedWeightedRatioScorer(string input1)
    {
        _input1 = input1;
        _strategy = new CachedDefaultRatioStrategy(input1);
        _baseRatioScorer = new CachedDefaultRatioScorer(_strategy);
        _tokenSortScorer = new CachedTokenSortScorer(_input1);
        _tokenSetScorer = new CachedTokenSetScorer(_input1);
        _partialTokenSetScorer = new CachedPartialTokenSetScorer(_input1);
    }

    /// <inheritdoc />
    public double Score(string input2)
    {
        int len1 = _input1.Length;
        int len2 = input2.Length;

        if (len1 == 0 || len2 == 0)
        {
            return 0;
        }

        bool tryPartials = TRY_PARTIALS;
        double unbaseScale = UNBASE_SCALE;
        double partialScale = PARTIAL_SCALE;

        double baseRatio = _baseRatioScorer.Score(input2);
        double lenRatio = (double)Math.Max(len1, len2) / Math.Min(len1, len2);

        // if strings are similar length don't use partials
        if (lenRatio < 1.5) tryPartials = false;

        // if one string is much shorter than the other
        if (lenRatio > 8) partialScale = .6;

        if (tryPartials)
        {
            double partial = Fuzz.PartialRatio(_input1, input2) * partialScale;
            // Partial token scores, as in WeightedRatioScorer and RapidFuzz.
            double partialSor = Fuzz.PartialTokenSortRatio(_input1, input2) * unbaseScale * partialScale;
            double partialSet = _partialTokenSetScorer.Score(input2) * unbaseScale * partialScale;

            return Math.Max(baseRatio, Math.Max(partial, Math.Max(partialSor, partialSet)));
        }

        double tokenSort = _tokenSortScorer.Score(input2) * unbaseScale;
        double tokenSet = _tokenSetScorer.Score(input2) * unbaseScale;
        return Math.Max(baseRatio, Math.Max(tokenSort, tokenSet));
    }

    /// <summary>Releases the pooled memory held for the query.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _strategy.Dispose();
        _tokenSortScorer.Dispose();
        _tokenSetScorer.Dispose();
        _partialTokenSetScorer.Dispose();
    }
}
