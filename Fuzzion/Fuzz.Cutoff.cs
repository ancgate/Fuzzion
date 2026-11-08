using System;
using Fuzzion.PreProcess;

namespace Fuzzion;

// Overloads of every Fuzz scorer that take a minimum score (plan item #21).
// They are separate overloads, not optional parameters, so method-group conversions
// such as `Func<string, string, double> f = Fuzz.Ratio;` keep working.
public static partial class Fuzz
{
    /// <summary>
    /// Returns <paramref name="score"/> when it is at least <paramref name="scoreCutoff"/>,
    /// otherwise 0 (the RapidFuzz convention).
    /// </summary>
    private static double ApplyCutoff(double score, double scoreCutoff) => score >= scoreCutoff ? score : 0;

    private static void ValidateCutoff(double scoreCutoff)
    {
        if (double.IsNaN(scoreCutoff))
        {
            throw new ArgumentOutOfRangeException(nameof(scoreCutoff), "scoreCutoff must be a number.");
        }
    }

    /// <summary>
    /// Upper bound of <see cref="Ratio(string, string)"/> from the lengths alone: at least
    /// |len1 - len2| edits are needed, so the score cannot exceed 200 * min / (len1 + len2).
    /// </summary>
    private static bool RatioCannotReach(int length1, int length2, double scoreCutoff)
    {
        // The bound and the real score are computed differently, so they can differ by a
        // rounding step; the margin makes sure a pair that can exactly reach the cutoff
        // is never skipped.
        const double margin = 1e-9;
        var total = length1 + length2;
        return total > 0 && 200.0 * Math.Min(length1, length2) / total + margin < scoreCutoff;
    }

    /// <summary>
    /// <see cref="Ratio(string, string)"/>, but returns 0 when the score is below
    /// <paramref name="scoreCutoff"/>. Inputs that cannot reach the cutoff (judging by their lengths) return 0 without being compared.
    /// </summary>
    /// <param name="input1">The first input.</param>
    /// <param name="input2">The second input.</param>
    /// <param name="scoreCutoff">Minimum score from 0 to 100; lower scores return 0.</param>
    public static double Ratio(string input1, string input2, double scoreCutoff)
    {
        ValidateCutoff(scoreCutoff);
        if (RatioCannotReach(input1.Length, input2.Length, scoreCutoff))
        {
            return 0;
        }

        return ApplyCutoff(Ratio(input1, input2), scoreCutoff);
    }

    /// <summary>
    /// <see cref="Ratio(string, string, Func{string, string})"/>, but returns 0 when the
    /// score is below <paramref name="scoreCutoff"/>.
    /// </summary>
    /// <param name="input1">The first input.</param>
    /// <param name="input2">The second input.</param>
    /// <param name="preprocessor">Applied to both inputs first (<see cref="StringPreprocessor.Full"/> when null).</param>
    /// <param name="scoreCutoff">Minimum score from 0 to 100; lower scores return 0.</param>
    public static double Ratio(string input1, string input2, Func<string, string> preprocessor, double scoreCutoff)
    {
        preprocessor ??= StringPreprocessor.Full;
        return Ratio(preprocessor(input1), preprocessor(input2), scoreCutoff);
    }

    /// <summary>
    /// <see cref="QRatio(string, string)"/>, but returns 0 when the score is below
    /// <paramref name="scoreCutoff"/>. Inputs that cannot reach the cutoff (judging by their lengths) return 0 without being compared.
    /// </summary>
    /// <param name="input1">The first input.</param>
    /// <param name="input2">The second input.</param>
    /// <param name="scoreCutoff">Minimum score from 0 to 100; lower scores return 0.</param>
    public static double QRatio(string input1, string input2, double scoreCutoff)
    {
        ValidateCutoff(scoreCutoff);
        if (input1.Length == 0 || input2.Length == 0 || RatioCannotReach(input1.Length, input2.Length, scoreCutoff))
        {
            return 0;
        }

        return ApplyCutoff(QRatio(input1, input2), scoreCutoff);
    }

    /// <summary>
    /// <see cref="QRatio(string, string, Func{string, string})"/>, but returns 0 when the
    /// score is below <paramref name="scoreCutoff"/>.
    /// </summary>
    /// <param name="input1">The first input.</param>
    /// <param name="input2">The second input.</param>
    /// <param name="preprocessor">Applied to both inputs first (<see cref="StringPreprocessor.Full"/> when null).</param>
    /// <param name="scoreCutoff">Minimum score from 0 to 100; lower scores return 0.</param>
    public static double QRatio(string input1, string input2, Func<string, string> preprocessor, double scoreCutoff)
    {
        preprocessor ??= StringPreprocessor.Full;
        return QRatio(preprocessor(input1), preprocessor(input2), scoreCutoff);
    }

    /// <summary>
    /// <see cref="PartialRatio(string, string)"/>, but returns 0 when the score is below
    /// <paramref name="scoreCutoff"/>.
    /// </summary>
    /// <param name="input1">The first input.</param>
    /// <param name="input2">The second input.</param>
    /// <param name="scoreCutoff">Minimum score from 0 to 100; lower scores return 0.</param>
    public static double PartialRatio(string input1, string input2, double scoreCutoff)
    {
        ValidateCutoff(scoreCutoff);
        return ApplyCutoff(PartialRatio(input1, input2), scoreCutoff);
    }

    /// <summary>
    /// <see cref="PartialRatio(string, string, Func{string, string})"/>, but returns 0 when the
    /// score is below <paramref name="scoreCutoff"/>.
    /// </summary>
    /// <param name="input1">The first input.</param>
    /// <param name="input2">The second input.</param>
    /// <param name="preprocessor">Applied to both inputs first (<see cref="StringPreprocessor.Full"/> when null).</param>
    /// <param name="scoreCutoff">Minimum score from 0 to 100; lower scores return 0.</param>
    public static double PartialRatio(string input1, string input2, Func<string, string> preprocessor, double scoreCutoff)
    {
        preprocessor ??= StringPreprocessor.Full;
        return PartialRatio(preprocessor(input1), preprocessor(input2), scoreCutoff);
    }

    /// <summary>
    /// <see cref="ApproximateSubstringRatio(string, string)"/>, but returns 0 when the score is below
    /// <paramref name="scoreCutoff"/>.
    /// </summary>
    /// <param name="input1">The first input.</param>
    /// <param name="input2">The second input.</param>
    /// <param name="scoreCutoff">Minimum score from 0 to 100; lower scores return 0.</param>
    public static double ApproximateSubstringRatio(string input1, string input2, double scoreCutoff)
    {
        ValidateCutoff(scoreCutoff);
        return ApplyCutoff(ApproximateSubstringRatio(input1, input2), scoreCutoff);
    }

    /// <summary>
    /// <see cref="ApproximateSubstringRatio(string, string, Func{string, string})"/>, but returns 0 when the
    /// score is below <paramref name="scoreCutoff"/>.
    /// </summary>
    /// <param name="input1">The first input.</param>
    /// <param name="input2">The second input.</param>
    /// <param name="preprocessor">Applied to both inputs first (<see cref="StringPreprocessor.Full"/> when null).</param>
    /// <param name="scoreCutoff">Minimum score from 0 to 100; lower scores return 0.</param>
    public static double ApproximateSubstringRatio(string input1, string input2, Func<string, string> preprocessor, double scoreCutoff)
    {
        preprocessor ??= StringPreprocessor.Full;
        return ApproximateSubstringRatio(preprocessor(input1), preprocessor(input2), scoreCutoff);
    }

    /// <summary>
    /// <see cref="TokenSortRatio(string, string)"/>, but returns 0 when the score is below
    /// <paramref name="scoreCutoff"/>.
    /// </summary>
    /// <param name="input1">The first input.</param>
    /// <param name="input2">The second input.</param>
    /// <param name="scoreCutoff">Minimum score from 0 to 100; lower scores return 0.</param>
    public static double TokenSortRatio(string input1, string input2, double scoreCutoff)
    {
        ValidateCutoff(scoreCutoff);
        return ApplyCutoff(TokenSortRatio(input1, input2), scoreCutoff);
    }

    /// <summary>
    /// <see cref="TokenSortRatio(string, string, Func{string, string})"/>, but returns 0 when the
    /// score is below <paramref name="scoreCutoff"/>.
    /// </summary>
    /// <param name="input1">The first input.</param>
    /// <param name="input2">The second input.</param>
    /// <param name="preprocessor">Applied to both inputs first (<see cref="StringPreprocessor.Full"/> when null).</param>
    /// <param name="scoreCutoff">Minimum score from 0 to 100; lower scores return 0.</param>
    public static double TokenSortRatio(string input1, string input2, Func<string, string> preprocessor, double scoreCutoff)
    {
        preprocessor ??= StringPreprocessor.Full;
        return TokenSortRatio(preprocessor(input1), preprocessor(input2), scoreCutoff);
    }

    /// <summary>
    /// <see cref="PartialTokenSortRatio(string, string)"/>, but returns 0 when the score is below
    /// <paramref name="scoreCutoff"/>.
    /// </summary>
    /// <param name="input1">The first input.</param>
    /// <param name="input2">The second input.</param>
    /// <param name="scoreCutoff">Minimum score from 0 to 100; lower scores return 0.</param>
    public static double PartialTokenSortRatio(string input1, string input2, double scoreCutoff)
    {
        ValidateCutoff(scoreCutoff);
        return ApplyCutoff(PartialTokenSortRatio(input1, input2), scoreCutoff);
    }

    /// <summary>
    /// <see cref="PartialTokenSortRatio(string, string, Func{string, string})"/>, but returns 0 when the
    /// score is below <paramref name="scoreCutoff"/>.
    /// </summary>
    /// <param name="input1">The first input.</param>
    /// <param name="input2">The second input.</param>
    /// <param name="preprocessor">Applied to both inputs first (<see cref="StringPreprocessor.Full"/> when null).</param>
    /// <param name="scoreCutoff">Minimum score from 0 to 100; lower scores return 0.</param>
    public static double PartialTokenSortRatio(string input1, string input2, Func<string, string> preprocessor, double scoreCutoff)
    {
        preprocessor ??= StringPreprocessor.Full;
        return PartialTokenSortRatio(preprocessor(input1), preprocessor(input2), scoreCutoff);
    }

    /// <summary>
    /// <see cref="TokenSetRatio(string, string)"/>, but returns 0 when the score is below
    /// <paramref name="scoreCutoff"/>.
    /// </summary>
    /// <param name="input1">The first input.</param>
    /// <param name="input2">The second input.</param>
    /// <param name="scoreCutoff">Minimum score from 0 to 100; lower scores return 0.</param>
    public static double TokenSetRatio(string input1, string input2, double scoreCutoff)
    {
        ValidateCutoff(scoreCutoff);
        return ApplyCutoff(TokenSetRatio(input1, input2), scoreCutoff);
    }

    /// <summary>
    /// <see cref="TokenSetRatio(string, string, Func{string, string})"/>, but returns 0 when the
    /// score is below <paramref name="scoreCutoff"/>.
    /// </summary>
    /// <param name="input1">The first input.</param>
    /// <param name="input2">The second input.</param>
    /// <param name="preprocessor">Applied to both inputs first (<see cref="StringPreprocessor.Full"/> when null).</param>
    /// <param name="scoreCutoff">Minimum score from 0 to 100; lower scores return 0.</param>
    public static double TokenSetRatio(string input1, string input2, Func<string, string> preprocessor, double scoreCutoff)
    {
        preprocessor ??= StringPreprocessor.Full;
        return TokenSetRatio(preprocessor(input1), preprocessor(input2), scoreCutoff);
    }

    /// <summary>
    /// <see cref="PartialTokenSetRatio(string, string)"/>, but returns 0 when the score is below
    /// <paramref name="scoreCutoff"/>.
    /// </summary>
    /// <param name="input1">The first input.</param>
    /// <param name="input2">The second input.</param>
    /// <param name="scoreCutoff">Minimum score from 0 to 100; lower scores return 0.</param>
    public static double PartialTokenSetRatio(string input1, string input2, double scoreCutoff)
    {
        ValidateCutoff(scoreCutoff);
        return ApplyCutoff(PartialTokenSetRatio(input1, input2), scoreCutoff);
    }

    /// <summary>
    /// <see cref="PartialTokenSetRatio(string, string, Func{string, string})"/>, but returns 0 when the
    /// score is below <paramref name="scoreCutoff"/>.
    /// </summary>
    /// <param name="input1">The first input.</param>
    /// <param name="input2">The second input.</param>
    /// <param name="preprocessor">Applied to both inputs first (<see cref="StringPreprocessor.Full"/> when null).</param>
    /// <param name="scoreCutoff">Minimum score from 0 to 100; lower scores return 0.</param>
    public static double PartialTokenSetRatio(string input1, string input2, Func<string, string> preprocessor, double scoreCutoff)
    {
        preprocessor ??= StringPreprocessor.Full;
        return PartialTokenSetRatio(preprocessor(input1), preprocessor(input2), scoreCutoff);
    }

    /// <summary>
    /// <see cref="TokenRatio(string, string)"/>, but returns 0 when the score is below
    /// <paramref name="scoreCutoff"/>.
    /// </summary>
    /// <param name="input1">The first input.</param>
    /// <param name="input2">The second input.</param>
    /// <param name="scoreCutoff">Minimum score from 0 to 100; lower scores return 0.</param>
    public static double TokenRatio(string input1, string input2, double scoreCutoff)
    {
        ValidateCutoff(scoreCutoff);
        return ApplyCutoff(TokenRatio(input1, input2), scoreCutoff);
    }

    /// <summary>
    /// <see cref="TokenRatio(string, string, Func{string, string})"/>, but returns 0 when the
    /// score is below <paramref name="scoreCutoff"/>.
    /// </summary>
    /// <param name="input1">The first input.</param>
    /// <param name="input2">The second input.</param>
    /// <param name="preprocessor">Applied to both inputs first (<see cref="StringPreprocessor.Full"/> when null).</param>
    /// <param name="scoreCutoff">Minimum score from 0 to 100; lower scores return 0.</param>
    public static double TokenRatio(string input1, string input2, Func<string, string> preprocessor, double scoreCutoff)
    {
        preprocessor ??= StringPreprocessor.Full;
        return TokenRatio(preprocessor(input1), preprocessor(input2), scoreCutoff);
    }

    /// <summary>
    /// <see cref="PartialTokenRatio(string, string)"/>, but returns 0 when the score is below
    /// <paramref name="scoreCutoff"/>.
    /// </summary>
    /// <param name="input1">The first input.</param>
    /// <param name="input2">The second input.</param>
    /// <param name="scoreCutoff">Minimum score from 0 to 100; lower scores return 0.</param>
    public static double PartialTokenRatio(string input1, string input2, double scoreCutoff)
    {
        ValidateCutoff(scoreCutoff);
        return ApplyCutoff(PartialTokenRatio(input1, input2), scoreCutoff);
    }

    /// <summary>
    /// <see cref="PartialTokenRatio(string, string, Func{string, string})"/>, but returns 0 when the
    /// score is below <paramref name="scoreCutoff"/>.
    /// </summary>
    /// <param name="input1">The first input.</param>
    /// <param name="input2">The second input.</param>
    /// <param name="preprocessor">Applied to both inputs first (<see cref="StringPreprocessor.Full"/> when null).</param>
    /// <param name="scoreCutoff">Minimum score from 0 to 100; lower scores return 0.</param>
    public static double PartialTokenRatio(string input1, string input2, Func<string, string> preprocessor, double scoreCutoff)
    {
        preprocessor ??= StringPreprocessor.Full;
        return PartialTokenRatio(preprocessor(input1), preprocessor(input2), scoreCutoff);
    }

    /// <summary>
    /// <see cref="TokenDifferenceRatio(string, string)"/>, but returns 0 when the score is below
    /// <paramref name="scoreCutoff"/>.
    /// </summary>
    /// <param name="input1">The first input.</param>
    /// <param name="input2">The second input.</param>
    /// <param name="scoreCutoff">Minimum score from 0 to 100; lower scores return 0.</param>
    public static double TokenDifferenceRatio(string input1, string input2, double scoreCutoff)
    {
        ValidateCutoff(scoreCutoff);
        return ApplyCutoff(TokenDifferenceRatio(input1, input2), scoreCutoff);
    }

    /// <summary>
    /// <see cref="TokenDifferenceRatio(string, string, Func{string, string})"/>, but returns 0 when the
    /// score is below <paramref name="scoreCutoff"/>.
    /// </summary>
    /// <param name="input1">The first input.</param>
    /// <param name="input2">The second input.</param>
    /// <param name="preprocessor">Applied to both inputs first (<see cref="StringPreprocessor.Full"/> when null).</param>
    /// <param name="scoreCutoff">Minimum score from 0 to 100; lower scores return 0.</param>
    public static double TokenDifferenceRatio(string input1, string input2, Func<string, string> preprocessor, double scoreCutoff)
    {
        preprocessor ??= StringPreprocessor.Full;
        return TokenDifferenceRatio(preprocessor(input1), preprocessor(input2), scoreCutoff);
    }

    /// <summary>
    /// <see cref="PartialTokenDifferenceRatio(string, string)"/>, but returns 0 when the score is below
    /// <paramref name="scoreCutoff"/>.
    /// </summary>
    /// <param name="input1">The first input.</param>
    /// <param name="input2">The second input.</param>
    /// <param name="scoreCutoff">Minimum score from 0 to 100; lower scores return 0.</param>
    public static double PartialTokenDifferenceRatio(string input1, string input2, double scoreCutoff)
    {
        ValidateCutoff(scoreCutoff);
        return ApplyCutoff(PartialTokenDifferenceRatio(input1, input2), scoreCutoff);
    }

    /// <summary>
    /// <see cref="PartialTokenDifferenceRatio(string, string, Func{string, string})"/>, but returns 0 when the
    /// score is below <paramref name="scoreCutoff"/>.
    /// </summary>
    /// <param name="input1">The first input.</param>
    /// <param name="input2">The second input.</param>
    /// <param name="preprocessor">Applied to both inputs first (<see cref="StringPreprocessor.Full"/> when null).</param>
    /// <param name="scoreCutoff">Minimum score from 0 to 100; lower scores return 0.</param>
    public static double PartialTokenDifferenceRatio(string input1, string input2, Func<string, string> preprocessor, double scoreCutoff)
    {
        preprocessor ??= StringPreprocessor.Full;
        return PartialTokenDifferenceRatio(preprocessor(input1), preprocessor(input2), scoreCutoff);
    }

    /// <summary>
    /// <see cref="TokenInitialismRatio(string, string)"/>, but returns 0 when the score is below
    /// <paramref name="scoreCutoff"/>.
    /// </summary>
    /// <param name="input1">The first input.</param>
    /// <param name="input2">The second input.</param>
    /// <param name="scoreCutoff">Minimum score from 0 to 100; lower scores return 0.</param>
    public static double TokenInitialismRatio(string input1, string input2, double scoreCutoff)
    {
        ValidateCutoff(scoreCutoff);
        return ApplyCutoff(TokenInitialismRatio(input1, input2), scoreCutoff);
    }

    /// <summary>
    /// <see cref="TokenInitialismRatio(string, string, Func{string, string})"/>, but returns 0 when the
    /// score is below <paramref name="scoreCutoff"/>.
    /// </summary>
    /// <param name="input1">The first input.</param>
    /// <param name="input2">The second input.</param>
    /// <param name="preprocessor">Applied to both inputs first (<see cref="StringPreprocessor.Full"/> when null).</param>
    /// <param name="scoreCutoff">Minimum score from 0 to 100; lower scores return 0.</param>
    public static double TokenInitialismRatio(string input1, string input2, Func<string, string> preprocessor, double scoreCutoff)
    {
        preprocessor ??= StringPreprocessor.Full;
        return TokenInitialismRatio(preprocessor(input1), preprocessor(input2), scoreCutoff);
    }

    /// <summary>
    /// <see cref="PartialTokenInitialismRatio(string, string)"/>, but returns 0 when the score is below
    /// <paramref name="scoreCutoff"/>.
    /// </summary>
    /// <param name="input1">The first input.</param>
    /// <param name="input2">The second input.</param>
    /// <param name="scoreCutoff">Minimum score from 0 to 100; lower scores return 0.</param>
    public static double PartialTokenInitialismRatio(string input1, string input2, double scoreCutoff)
    {
        ValidateCutoff(scoreCutoff);
        return ApplyCutoff(PartialTokenInitialismRatio(input1, input2), scoreCutoff);
    }

    /// <summary>
    /// <see cref="PartialTokenInitialismRatio(string, string, Func{string, string})"/>, but returns 0 when the
    /// score is below <paramref name="scoreCutoff"/>.
    /// </summary>
    /// <param name="input1">The first input.</param>
    /// <param name="input2">The second input.</param>
    /// <param name="preprocessor">Applied to both inputs first (<see cref="StringPreprocessor.Full"/> when null).</param>
    /// <param name="scoreCutoff">Minimum score from 0 to 100; lower scores return 0.</param>
    public static double PartialTokenInitialismRatio(string input1, string input2, Func<string, string> preprocessor, double scoreCutoff)
    {
        preprocessor ??= StringPreprocessor.Full;
        return PartialTokenInitialismRatio(preprocessor(input1), preprocessor(input2), scoreCutoff);
    }

    /// <summary>
    /// <see cref="TokenAbbreviationRatio(string, string)"/>, but returns 0 when the score is below
    /// <paramref name="scoreCutoff"/>.
    /// </summary>
    /// <param name="input1">The first input.</param>
    /// <param name="input2">The second input.</param>
    /// <param name="scoreCutoff">Minimum score from 0 to 100; lower scores return 0.</param>
    public static double TokenAbbreviationRatio(string input1, string input2, double scoreCutoff)
    {
        ValidateCutoff(scoreCutoff);
        return ApplyCutoff(TokenAbbreviationRatio(input1, input2), scoreCutoff);
    }

    /// <summary>
    /// <see cref="TokenAbbreviationRatio(string, string, Func{string, string})"/>, but returns 0 when the
    /// score is below <paramref name="scoreCutoff"/>.
    /// </summary>
    /// <param name="input1">The first input.</param>
    /// <param name="input2">The second input.</param>
    /// <param name="preprocessor">Applied to both inputs first (<see cref="StringPreprocessor.Full"/> when null).</param>
    /// <param name="scoreCutoff">Minimum score from 0 to 100; lower scores return 0.</param>
    public static double TokenAbbreviationRatio(string input1, string input2, Func<string, string> preprocessor, double scoreCutoff)
    {
        preprocessor ??= StringPreprocessor.Full;
        return TokenAbbreviationRatio(preprocessor(input1), preprocessor(input2), scoreCutoff);
    }

    /// <summary>
    /// <see cref="PartialTokenAbbreviationRatio(string, string)"/>, but returns 0 when the score is below
    /// <paramref name="scoreCutoff"/>.
    /// </summary>
    /// <param name="input1">The first input.</param>
    /// <param name="input2">The second input.</param>
    /// <param name="scoreCutoff">Minimum score from 0 to 100; lower scores return 0.</param>
    public static double PartialTokenAbbreviationRatio(string input1, string input2, double scoreCutoff)
    {
        ValidateCutoff(scoreCutoff);
        return ApplyCutoff(PartialTokenAbbreviationRatio(input1, input2), scoreCutoff);
    }

    /// <summary>
    /// <see cref="PartialTokenAbbreviationRatio(string, string, Func{string, string})"/>, but returns 0 when the
    /// score is below <paramref name="scoreCutoff"/>.
    /// </summary>
    /// <param name="input1">The first input.</param>
    /// <param name="input2">The second input.</param>
    /// <param name="preprocessor">Applied to both inputs first (<see cref="StringPreprocessor.Full"/> when null).</param>
    /// <param name="scoreCutoff">Minimum score from 0 to 100; lower scores return 0.</param>
    public static double PartialTokenAbbreviationRatio(string input1, string input2, Func<string, string> preprocessor, double scoreCutoff)
    {
        preprocessor ??= StringPreprocessor.Full;
        return PartialTokenAbbreviationRatio(preprocessor(input1), preprocessor(input2), scoreCutoff);
    }

    /// <summary>
    /// <see cref="WeightedRatio(string, string)"/>, but returns 0 when the score is below
    /// <paramref name="scoreCutoff"/>.
    /// </summary>
    /// <param name="input1">The first input.</param>
    /// <param name="input2">The second input.</param>
    /// <param name="scoreCutoff">Minimum score from 0 to 100; lower scores return 0.</param>
    public static double WeightedRatio(string input1, string input2, double scoreCutoff)
    {
        ValidateCutoff(scoreCutoff);
        return ApplyCutoff(WeightedRatio(input1, input2), scoreCutoff);
    }

    /// <summary>
    /// <see cref="WeightedRatio(string, string, Func{string, string})"/>, but returns 0 when the
    /// score is below <paramref name="scoreCutoff"/>.
    /// </summary>
    /// <param name="input1">The first input.</param>
    /// <param name="input2">The second input.</param>
    /// <param name="preprocessor">Applied to both inputs first (<see cref="StringPreprocessor.Full"/> when null).</param>
    /// <param name="scoreCutoff">Minimum score from 0 to 100; lower scores return 0.</param>
    public static double WeightedRatio(string input1, string input2, Func<string, string> preprocessor, double scoreCutoff)
    {
        preprocessor ??= StringPreprocessor.Full;
        return WeightedRatio(preprocessor(input1), preprocessor(input2), scoreCutoff);
    }
}
