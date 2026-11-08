using System;
using Fuzzion.SimilarityRatio.Strategy;
using Fuzzion.SimilarityRatio.Strategy.Generic;

namespace Fuzzion;

// ReadOnlySpan<char> overloads of the character-level scorers (plan item #22). They let
// callers score slices of larger buffers (for example parts of a line or a pooled char
// array) without creating substrings. Results equal the string overloads. Token-based
// scorers need words split into strings, so they have no span overloads.
public static partial class Fuzz
{
    /// <summary>
    /// <see cref="Ratio(string, string)"/> for spans: 100 × the normalized Indel similarity.
    /// Two empty inputs score 100; exactly one empty input scores 0.
    /// </summary>
    public static double Ratio(ReadOnlySpan<char> input1, ReadOnlySpan<char> input2)
    {
        if (input1.IsEmpty || input2.IsEmpty)
        {
            return input1.IsEmpty && input2.IsEmpty ? 100 : 0;
        }

        return 100 * Indel.NormalizedSimilarityChar(input1, input2);
    }

    /// <summary><see cref="Ratio(ReadOnlySpan{char}, ReadOnlySpan{char})"/>, but 0 below <paramref name="scoreCutoff"/>.</summary>
    public static double Ratio(ReadOnlySpan<char> input1, ReadOnlySpan<char> input2, double scoreCutoff)
    {
        ValidateCutoff(scoreCutoff);
        return RatioCannotReach(input1.Length, input2.Length, scoreCutoff)
            ? 0
            : ApplyCutoff(Ratio(input1, input2), scoreCutoff);
    }

    /// <summary><see cref="QRatio(string, string)"/> for spans: 0 when either input is empty.</summary>
    public static double QRatio(ReadOnlySpan<char> input1, ReadOnlySpan<char> input2) =>
        input1.IsEmpty || input2.IsEmpty ? 0 : Ratio(input1, input2);

    /// <summary><see cref="QRatio(ReadOnlySpan{char}, ReadOnlySpan{char})"/>, but 0 below <paramref name="scoreCutoff"/>.</summary>
    public static double QRatio(ReadOnlySpan<char> input1, ReadOnlySpan<char> input2, double scoreCutoff)
    {
        ValidateCutoff(scoreCutoff);
        return input1.IsEmpty || input2.IsEmpty || RatioCannotReach(input1.Length, input2.Length, scoreCutoff)
            ? 0
            : ApplyCutoff(Ratio(input1, input2), scoreCutoff);
    }

    /// <summary><see cref="PartialRatio(string, string)"/> for spans.</summary>
    public static double PartialRatio(ReadOnlySpan<char> input1, ReadOnlySpan<char> input2) =>
        PartialRatioStrategy<char>.Calculate(input1, input2);

    /// <summary><see cref="PartialRatio(ReadOnlySpan{char}, ReadOnlySpan{char})"/>, but 0 below <paramref name="scoreCutoff"/>.</summary>
    public static double PartialRatio(ReadOnlySpan<char> input1, ReadOnlySpan<char> input2, double scoreCutoff)
    {
        ValidateCutoff(scoreCutoff);
        return ApplyCutoff(PartialRatio(input1, input2), scoreCutoff);
    }

    /// <summary><see cref="ApproximateSubstringRatio(string, string)"/> for spans.</summary>
    public static double ApproximateSubstringRatio(ReadOnlySpan<char> input1, ReadOnlySpan<char> input2) =>
        ApproximateSubstringRatioStrategy.Calculate(input1, input2);

    /// <summary><see cref="ApproximateSubstringRatio(ReadOnlySpan{char}, ReadOnlySpan{char})"/>, but 0 below <paramref name="scoreCutoff"/>.</summary>
    public static double ApproximateSubstringRatio(ReadOnlySpan<char> input1, ReadOnlySpan<char> input2, double scoreCutoff)
    {
        ValidateCutoff(scoreCutoff);
        return ApplyCutoff(ApproximateSubstringRatio(input1, input2), scoreCutoff);
    }
}
