using System;

namespace Fuzzion.SimilarityRatio.Strategy.Generic;

internal class CachedDefaultRatioStrategy<T>(T[] input1) : IDisposable
    where T : IEquatable<T>
{
    private readonly IndelT<T> _indel = new(input1);

    public double Calculate(T[] input2)
    {
        // Two empty inputs are identical (100); exactly one empty input scores 0.
        // Matches RapidFuzz and TheFuzz (see docs/known-differences.md, KD1).
        if (input1.Length == 0 || input2.Length == 0)
        {
            return input1.Length == 0 && input2.Length == 0 ? 100 : 0;
        }

        var result = 100 * _indel.NormalizedSimilarityWith(input2);

        return result;
    }

    public void Dispose()
    {
        _indel.Dispose();
    }
}
