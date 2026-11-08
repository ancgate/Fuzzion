using System;

namespace Fuzzion.SimilarityRatio.Strategy;

internal static class DefaultRatioStrategy
{
    public static double Calculate(string input1, string input2)
    {
        // Two empty inputs are identical (100); exactly one empty input scores 0.
        // Matches RapidFuzz and TheFuzz (see docs/known-differences.md, KD1).
        if (input1.Length == 0 || input2.Length == 0)
        {
            return input1.Length == 0 && input2.Length == 0 ? 100 : 0;
        }

        var input1Span = input1.AsSpan();
        var input2Span = input2.AsSpan();

        return 100 * Indel.NormalizedSimilarityChar(input1Span, input2Span);
    }
}
