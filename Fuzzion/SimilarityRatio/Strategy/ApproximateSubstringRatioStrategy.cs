using System;

namespace Fuzzion.SimilarityRatio.Strategy;

internal static class ApproximateSubstringRatioStrategy
{
    public static double Calculate(string input1, string input2) =>
        Calculate(input1.AsSpan(), input2.AsSpan());

    public static double Calculate(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
    {

        if (a.IsEmpty || b.IsEmpty)
        {
            return a.IsEmpty && b.IsEmpty ? 100 : 0;
        }

        // Call the char overload of BestSubstringMatch (not the generic one) so the
        // IndexOf exact-match fast path is used. From a generic method, C# binds to
        // BestSubstringMatch<T>, which has no fast path.
        if (a.Length < b.Length)
        {
            return Score(a, b);
        }

        if (b.Length < a.Length)
        {
            return Score(b, a);
        }

        double forward = Score(a, b);
        if (forward == 100)
        {
            return 100;
        }

        return Math.Max(forward, Score(b, a));
    }

    private static double Score(ReadOnlySpan<char> pattern, ReadOnlySpan<char> text) =>
        ApproximateSubstringScore.FromDistance(Indel.BestSubstringMatch(pattern, text).Distance, pattern.Length);
}
