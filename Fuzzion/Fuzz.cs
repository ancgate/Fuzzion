using System;
using Fuzzion.SimilarityRatio;
using Fuzzion.SimilarityRatio.Strategy;
using Fuzzion.SimilarityRatio.Scorer.Composite;
using Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

namespace Fuzzion;

/// <summary>
/// Similarity scores between two strings, from 0 (nothing in common) to 100 (equal):
/// simple, partial, token-based and weighted ratios, as in RapidFuzz's <c>fuzz</c> module.
/// Overloads take an optional preprocessor and a minimum score.
/// </summary>
public static partial class Fuzz
{
    #region Ratio
    /// <summary>
    /// Calculates a Levenshtein simple ratio between the strings.
    /// This indicates a measure of similarity
    /// </summary>
    /// <param name="input1"></param>
    /// <param name="input2"></param>
    /// <returns></returns>
    public static double Ratio(string input1, string input2)
    {
        return ScorerCache.Get<DefaultRatioScorer>().Score(input1, input2);
    }

    /// <summary>
    /// Calculates a Levenshtein simple ratio between the strings.
    /// This indicates a measure of similarity
    /// </summary>
    /// <param name="input1"></param>
    /// <param name="input2"></param>
    /// <param name="preprocessor"></param>
    /// <returns></returns>
    public static double Ratio(string input1, string input2, Func<string, string> preprocessor)
    {
        return ScorerCache.Get<DefaultRatioScorer>().Score(input1, input2, preprocessor);
    }
    #endregion

    #region PartialRatio
    /// <summary>
    /// Inconsistent substrings lead to problems in matching. This ratio
    /// uses a heuristic called "best partial" for when two strings
    /// are of noticeably different lengths. Two empty inputs score 100;
    /// exactly one empty input scores 0.
    /// </summary>
    /// <param name="input1"></param>
    /// <param name="input2"></param>
    /// <returns></returns>
    public static double PartialRatio(string input1, string input2)
    {
        return ScorerCache.Get<PartialRatioScorer>().Score(input1, input2);
    }

    /// <summary>
    /// Inconsistent substrings lead to problems in matching. This ratio
    /// uses a heuristic called "best partial" for when two strings
    /// are of noticeably different lengths. Two empty inputs score 100;
    /// exactly one empty input scores 0.
    /// </summary>
    /// <param name="input1"></param>
    /// <param name="input2"></param>
    /// <param name="preprocessor"></param>
    /// <returns></returns>
    public static double PartialRatio(string input1, string input2, Func<string, string> preprocessor)
    {
        return ScorerCache.Get<PartialRatioScorer>().Score(input1, input2, preprocessor);
    }
    #endregion

    #region ApproximateSubstringRatio
    /// <summary>
    /// Searches the shorter input approximately within the longer input using
    /// Indel distance. Insertions and deletions cost one and a substitution
    /// costs two edits. Equal-length inputs are evaluated in both directions
    /// unless the first direction is exact. Two empty inputs score 100; exactly
    /// one empty input scores 0. This metric is not numerically equivalent to
    /// <see cref="PartialRatio(string,string)"/>.
    /// </summary>
    /// <param name="input1">The first input.</param>
    /// <param name="input2">The second input.</param>
    /// <returns>A score from 0 to 100.</returns>
    public static double ApproximateSubstringRatio(string input1, string input2)
    {
        return ScorerCache.Get<ApproximateSubstringRatioScorer>()
            .Score(input1, input2);
    }

    /// <summary>
    /// Searches the shorter processed input approximately within the longer
    /// processed input using Indel distance. Insertions and deletions cost one
    /// and a substitution costs two edits. Equal-length inputs are evaluated in
    /// both directions unless the first direction is exact. Two empty processed
    /// inputs score 100; exactly one empty processed input scores 0. This metric
    /// is not numerically equivalent to <see cref="PartialRatio(string,string)"/>.
    /// </summary>
    /// <param name="input1">The first input.</param>
    /// <param name="input2">The second input.</param>
    /// <param name="preprocessor">A preprocessor applied to both inputs.</param>
    /// <returns>A score from 0 to 100.</returns>
    public static double ApproximateSubstringRatio(
        string input1,
        string input2,
        Func<string, string> preprocessor)
    {
        return ScorerCache.Get<ApproximateSubstringRatioScorer>()
            .Score(input1, input2, preprocessor);
    }
    #endregion

    #region TokenSortRatio
    /// <summary>
    /// Find all alphanumeric tokens in the string and sort
    /// those tokens and then take ratio of resulting
    /// joined strings.
    /// </summary>
    /// <param name="input1"></param>
    /// <param name="input2"></param>
    /// <returns></returns>
    public static double TokenSortRatio(string input1, string input2)
    {
        return ScorerCache.Get<TokenSortScorer>().Score(input1, input2);
    }

    /// <summary>
    /// Find all alphanumeric tokens in the string and sort
    /// those tokens and then take ratio of resulting
    /// joined strings.
    /// </summary>
    /// <param name="input1"></param>
    /// <param name="input2"></param>
    /// <param name="preprocessor"></param>
    /// <returns></returns>
    public static double TokenSortRatio(string input1, string input2, Func<string, string> preprocessor)
    {
        return ScorerCache.Get<TokenSortScorer>().Score(input1, input2, preprocessor);
    }

    /// <summary>
    /// Find all alphanumeric tokens in the string and sort
    /// those tokens and then take ratio of resulting
    /// joined strings.
    /// </summary>
    /// <param name="input1"></param>
    /// <param name="input2"></param>
    /// <returns></returns>
    public static double PartialTokenSortRatio(string input1, string input2)
    {
        return ScorerCache.Get<PartialTokenSortScorer>().Score(input1, input2);
    }

    /// <summary>
    /// Find all alphanumeric tokens in the string and sort
    /// those tokens and then take ratio of resulting
    /// joined strings.
    /// </summary>
    /// <param name="input1"></param>
    /// <param name="input2"></param>
    /// <param name="preprocessor"></param>
    /// <returns></returns>
    public static double PartialTokenSortRatio(string input1, string input2, Func<string, string> preprocessor)
    {
        return ScorerCache.Get<PartialTokenSortScorer>().Score(input1, input2, preprocessor);
    }
    #endregion

    #region TokenSetRatio
    /// <summary>
    /// Splits the strings into tokens and computes intersections and remainders
    /// between the tokens of the two strings.A comparison string is then
    /// built up and is compared using the simple ratio algorithm.
    /// Useful for strings where words appear redundantly.
    /// </summary>
    /// <param name="input1"></param>
    /// <param name="input2"></param>
    /// <returns></returns>
    public static double TokenSetRatio(string input1, string input2)
    {
        return ScorerCache.Get<TokenSetScorer>().Score(input1, input2);
    }

    /// <summary>
    /// Splits the strings into tokens and computes intersections and remainders
    /// between the tokens of the two strings.A comparison string is then
    /// built up and is compared using the simple ratio algorithm.
    /// Useful for strings where words appear redundantly.
    /// </summary>
    /// <param name="input1"></param>
    /// <param name="input2"></param>
    /// <param name="preprocessor"></param>
    /// <returns></returns>
    public static double TokenSetRatio(string input1, string input2, Func<string, string> preprocessor)
    {
        return ScorerCache.Get<TokenSetScorer>().Score(input1, input2, preprocessor);
    }

    /// <summary>
    /// Splits the strings into tokens and computes intersections and remainders
    /// between the tokens of the two strings.A comparison string is then
    /// built up and is compared using the simple ratio algorithm.
    /// Useful for strings where words appear redundantly.
    /// </summary>
    /// <param name="input1"></param>
    /// <param name="input2"></param>
    /// <returns></returns>
    public static double PartialTokenSetRatio(string input1, string input2)
    {
        return ScorerCache.Get<PartialTokenSetScorer>().Score(input1, input2);
    }

    /// <summary>
    /// Splits the strings into tokens and computes intersections and remainders
    /// between the tokens of the two strings.A comparison string is then
    /// built up and is compared using the simple ratio algorithm.
    /// Useful for strings where words appear redundantly.
    /// </summary>
    /// <param name="input1"></param>
    /// <param name="input2"></param>
    /// <param name="preprocessor"></param>
    /// <returns></returns>
    public static double PartialTokenSetRatio(string input1, string input2, Func<string, string> preprocessor)
    {
        return ScorerCache.Get<PartialTokenSetScorer>().Score(input1, input2, preprocessor);
    }
    #endregion

    #region TokenDifferenceRatio
    /// <summary>
    /// Splits the strings into tokens and computes the ratio on those tokens (not the individual chars,
    /// but the strings themselves)
    /// </summary>
    /// <param name="input1"></param>
    /// <param name="input2"></param>
    /// <returns></returns>
    public static double TokenDifferenceRatio(string input1, string input2)
    {
        return ScorerCache.Get<TokenDifferenceScorer>().Score(input1, input2);
    }

    /// <summary>
    /// Splits the strings into tokens and computes the ratio on those tokens (not the individual chars,
    /// but the strings themselves)
    /// </summary>
    /// <param name="input1"></param>
    /// <param name="input2"></param>
    /// <param name="preprocessor"></param>
    /// <returns></returns>
    public static double TokenDifferenceRatio(string input1, string input2, Func<string, string> preprocessor)
    {
        return ScorerCache.Get<TokenDifferenceScorer>().Score(input1, input2, preprocessor);
    }

    /// <summary>
    /// Splits the strings into tokens and computes the ratio on those tokens (not the individual chars,
    /// but the strings themselves)
    /// </summary>
    /// <param name="input1"></param>
    /// <param name="input2"></param>
    /// <returns></returns>
    public static double PartialTokenDifferenceRatio(string input1, string input2)
    {
        return ScorerCache.Get<PartialTokenDifferenceScorer>().Score(input1, input2);
    }

    /// <summary>
    /// Splits the strings into tokens and computes the ratio on those tokens (not the individual chars,
    /// but the strings themselves)
    /// </summary>
    /// <param name="input1"></param>
    /// <param name="input2"></param>
    /// <param name="preprocessor"></param>
    /// <returns></returns>
    public static double PartialTokenDifferenceRatio(string input1, string input2, Func<string, string> preprocessor)
    {
        return ScorerCache.Get<PartialTokenDifferenceScorer>().Score(input1, input2, preprocessor);
    }
    #endregion

    #region TokenInitialismRatio
    /// <summary>
    /// Splits longer string into tokens and takes the initialism and compares it to the shorter
    /// </summary>
    /// <param name="input1"></param>
    /// <param name="input2"></param>
    /// <returns></returns>
    public static double TokenInitialismRatio(string input1, string input2)
    {
        return ScorerCache.Get<TokenInitialismScorer>().Score(input1, input2);
    }

    /// <summary>
    /// Splits longer string into tokens and takes the initialism and compares it to the shorter
    /// </summary>
    /// <param name="input1"></param>
    /// <param name="input2"></param>
    /// <param name="preprocessor"></param>
    /// <returns></returns>
    public static double TokenInitialismRatio(string input1, string input2, Func<string, string> preprocessor)
    {
        return ScorerCache.Get<TokenInitialismScorer>().Score(input1, input2, preprocessor);
    }

    /// <summary>
    /// Splits longer string into tokens and takes the initialism and compares it to the shorter
    /// </summary>
    /// <param name="input1"></param>
    /// <param name="input2"></param>
    /// <returns></returns>
    public static double PartialTokenInitialismRatio(string input1, string input2)
    {
        return ScorerCache.Get<PartialTokenInitialismScorer>().Score(input1, input2);
    }

    /// <summary>
    /// Splits longer string into tokens and takes the initialism and compares it to the shorter
    /// </summary>
    /// <param name="input1"></param>
    /// <param name="input2"></param>
    /// <param name="preprocessor"></param>
    /// <returns></returns>
    public static double PartialTokenInitialismRatio(string input1, string input2, Func<string, string> preprocessor)
    {
        return ScorerCache.Get<PartialTokenInitialismScorer>().Score(input1, input2, preprocessor);
    }
    #endregion

    #region TokenAbbreviationRatio
    /// <summary>
    /// Similarity ratio that attempts to determine whether one strings tokens are an abbreviation
    /// of the other strings tokens. One string must have all its characters in order in the other string
    /// to even be considered.
    /// </summary>
    /// <param name="input1"></param>
    /// <param name="input2"></param>
    /// <returns></returns>
    public static double TokenAbbreviationRatio(string input1, string input2)
    {
        return ScorerCache.Get<TokenAbbreviationScorer>().Score(input1, input2);
    }

    /// <summary>
    /// Similarity ratio that attempts to determine whether one strings tokens are an abbreviation
    /// of the other strings tokens. One string must have all its characters in order in the other string
    /// to even be considered.
    /// </summary>
    /// <param name="input1"></param>
    /// <param name="input2"></param>
    /// <param name="preprocessor"></param>
    /// <returns></returns>
    public static double TokenAbbreviationRatio(string input1, string input2, Func<string, string> preprocessor)
    {
        return ScorerCache.Get<TokenAbbreviationScorer>().Score(input1, input2, preprocessor);
    }

    /// <summary>
    /// Similarity ratio that attempts to determine whether one strings tokens are an abbreviation
    /// of the other strings tokens. One string must have all its characters in order in the other string
    /// to even be considered.
    /// </summary>
    /// <param name="input1"></param>
    /// <param name="input2"></param>
    /// <returns></returns>
    public static double PartialTokenAbbreviationRatio(string input1, string input2)
    {
        return ScorerCache.Get<PartialTokenAbbreviationScorer>().Score(input1, input2);
    }

    /// <summary>
    /// Similarity ratio that attempts to determine whether one strings tokens are an abbreviation
    /// of the other strings tokens. One string must have all its characters in order in the other string
    /// to even be considered.
    /// </summary>
    /// <param name="input1"></param>
    /// <param name="input2"></param>
    /// <param name="preprocessor"></param>
    /// <returns></returns>
    public static double PartialTokenAbbreviationRatio(string input1, string input2, Func<string, string> preprocessor)
    {
        return ScorerCache.Get<PartialTokenAbbreviationScorer>().Score(input1, input2, preprocessor);
    }
    #endregion

    #region WeightedRatio
    /// <summary>
    /// Calculates a weighted ratio between the different algorithms for best results
    /// </summary>
    /// <param name="input1"></param>
    /// <param name="input2"></param>
    /// <returns></returns>
    public static double WeightedRatio(string input1, string input2)
    {
        return ScorerCache.Get<WeightedRatioScorer>().Score(input1, input2);
    }

    /// <summary>
    /// Calculates a weighted ratio between the different algorithms for best results
    /// </summary>
    /// <param name="input1"></param>
    /// <param name="input2"></param>
    /// <param name="preprocessor"></param>
    /// <returns></returns>
    public static double WeightedRatio(string input1, string input2, Func<string, string> preprocessor)
    {
        return ScorerCache.Get<WeightedRatioScorer>().Score(input1, input2, preprocessor);
    }
    #endregion

    #region QRatio
    /// <summary>
    /// Quick ratio: the same as <see cref="Ratio(string, string)"/>, but 0 when either
    /// input is empty (including both). Same as RapidFuzz's <c>QRatio</c>.
    /// </summary>
    public static double QRatio(string input1, string input2)
    {
        return ScorerCache.Get<QRatioScorer>().Score(input1, input2);
    }

    /// <inheritdoc cref="QRatio(string, string)"/>
    /// <param name="input1">The first input.</param>
    /// <param name="input2">The second input.</param>
    /// <param name="preprocessor">Applied to both inputs first (<see cref="PreProcess.StringPreprocessor.Full"/> when null).</param>
    public static double QRatio(string input1, string input2, Func<string, string> preprocessor)
    {
        return ScorerCache.Get<QRatioScorer>().Score(input1, input2, preprocessor);
    }
    #endregion

    #region TokenRatio
    /// <summary>
    /// The higher of <see cref="TokenSortRatio(string, string)"/> and
    /// <see cref="TokenSetRatio(string, string)"/>. Same as RapidFuzz's <c>token_ratio</c>.
    /// </summary>
    public static double TokenRatio(string input1, string input2)
    {
        return ScorerCache.Get<TokenRatioScorer>().Score(input1, input2);
    }

    /// <inheritdoc cref="TokenRatio(string, string)"/>
    /// <param name="input1">The first input.</param>
    /// <param name="input2">The second input.</param>
    /// <param name="preprocessor">Applied to both inputs first (<see cref="PreProcess.StringPreprocessor.Full"/> when null).</param>
    public static double TokenRatio(string input1, string input2, Func<string, string> preprocessor)
    {
        return ScorerCache.Get<TokenRatioScorer>().Score(input1, input2, preprocessor);
    }

    /// <summary>
    /// The higher of <see cref="PartialTokenSortRatio(string, string)"/> and
    /// <see cref="PartialTokenSetRatio(string, string)"/>. Same as RapidFuzz's
    /// <c>partial_token_ratio</c>. It is 100 whenever the inputs share a word.
    /// </summary>
    public static double PartialTokenRatio(string input1, string input2)
    {
        return ScorerCache.Get<PartialTokenRatioScorer>().Score(input1, input2);
    }

    /// <inheritdoc cref="PartialTokenRatio(string, string)"/>
    /// <param name="input1">The first input.</param>
    /// <param name="input2">The second input.</param>
    /// <param name="preprocessor">Applied to both inputs first (<see cref="PreProcess.StringPreprocessor.Full"/> when null).</param>
    public static double PartialTokenRatio(string input1, string input2, Func<string, string> preprocessor)
    {
        return ScorerCache.Get<PartialTokenRatioScorer>().Score(input1, input2, preprocessor);
    }
    #endregion

    #region PartialRatioAlignment
    /// <summary>
    /// <see cref="PartialRatio(string, string)"/> together with where the best match was
    /// found in each input. Same as RapidFuzz's <c>partial_ratio_alignment</c>.
    /// Two empty inputs give <c>(100, 0, 0, 0, 0)</c>.
    /// </summary>
    public static ScoreAlignment PartialRatioAlignment(string input1, string input2)
    {
        return PartialRatioStrategy.Alignment(input1, input2);
    }

    /// <inheritdoc cref="PartialRatioAlignment(string, string)"/>
    /// <param name="input1">The first input.</param>
    /// <param name="input2">The second input.</param>
    /// <param name="preprocessor">Applied to both inputs first (<see cref="PreProcess.StringPreprocessor.Full"/> when null).
    /// Positions refer to the preprocessed strings.</param>
    public static ScoreAlignment PartialRatioAlignment(string input1, string input2, Func<string, string> preprocessor)
    {
        preprocessor ??= PreProcess.StringPreprocessor.Full;
        return PartialRatioStrategy.Alignment(preprocessor(input1), preprocessor(input2));
    }
    #endregion
}
