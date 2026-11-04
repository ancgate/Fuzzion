using System;

namespace Fuzzion.SimilarityRatio.Strategy;

internal static class ApproximateSubstringScore
{
    /// <summary>
    /// Converts a raw Indel substring distance into a score from 0 to 100, relative to
    /// the pattern length. Not rounded, so fractional scores are kept (v6 convention).
    /// </summary>
    public static double FromDistance(int distance, int patternLength)
    {
        if (patternLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(patternLength));
        }

        double similarity = 1.0 - distance / (double)patternLength;
        return 100.0 * Math.Max(0.0, Math.Min(1.0, similarity));
    }
}
