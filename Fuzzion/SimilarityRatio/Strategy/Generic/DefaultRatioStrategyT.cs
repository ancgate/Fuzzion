using System;

namespace Fuzzion.SimilarityRatio.Strategy.Generic;

internal static class DefaultRatioStrategy<T> where T : IEquatable<T>
{
    public static double Calculate(T[] input1, T[] input2)
    {
        // Two empty inputs are identical (100); exactly one empty input scores 0.
        // Matches RapidFuzz and TheFuzz (see docs/known-differences.md, KD1).
        if (input1.Length == 0 || input2.Length == 0)
        {
            return input1.Length == 0 && input2.Length == 0 ? 100 : 0;
        }
            
        var result = 100 * Indel.NormalizedSimilarity((ReadOnlySpan<T>)input1, (ReadOnlySpan<T>)input2);

        return result;
    }
}
