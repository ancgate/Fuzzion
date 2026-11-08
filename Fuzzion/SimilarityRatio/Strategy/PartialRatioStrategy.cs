using System;
using Fuzzion.SimilarityRatio.Strategy.Generic;

namespace Fuzzion.SimilarityRatio.Strategy;

internal static class PartialRatioStrategy
{
    /// <summary>
    /// Searches for the optimal alignment of the shorter span in the longer span
    /// and returns the partial fuzz.ratio for that alignment, as a value in [0…100].
    /// </summary>
    public static double Calculate(string input1, string input2)
    {
        return PartialRatioStrategy<char>.Calculate(input1.AsSpan(), input2.AsSpan());
    }

    /// <summary>The best partial alignment of the two inputs, with its score.</summary>
    public static ScoreAlignment Alignment(string input1, string input2)
    {
        var a = PartialRatioStrategy<char>.PartialRatioAlignment(input1.AsSpan(), input2.AsSpan());
        return new ScoreAlignment(a.Score, a.SrcStart, a.SrcEnd, a.DestStart, a.DestEnd);
    }
}
