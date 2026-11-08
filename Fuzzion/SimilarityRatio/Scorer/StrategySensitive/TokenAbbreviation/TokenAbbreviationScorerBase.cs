using Fuzzion.Extensions;
using Fuzzion.Utils;
using System;

namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

/// <summary>
/// Base class for abbreviation scorers, which test whether the words of one string abbreviate
/// words of the other. Words are runs of letters; digits and other characters only separate them.
/// Each word of the string with fewer words is paired with a different word of the other string;
/// a pair is scored with <see cref="StrategySensitiveScorerBase.Scorer"/> only when all characters
/// of the shorter word appear in order in the longer word, and scores 0 otherwise. The result is
/// the best average over all pairings. The score is 0 when the longer string is less than 1.5 times
/// as long as the shorter, or when the string with fewer words has more than four.
/// </summary>
public abstract class TokenAbbreviationScorerBase : StrategySensitiveScorerBase
{
    /// <summary>Scores whether one string's words abbreviate the other's; the order of the inputs does not matter.</summary>
    /// <param name="shorter">One input (swapped with <paramref name="longer"/> if it is the longer one).</param>
    /// <param name="longer">The other input.</param>
    /// <returns>A score from 0 to 100.</returns>
    public override double Score(string shorter, string longer)
    {
        SequenceUtils.SwapIfSourceIsLonger(ref shorter, ref longer);

        double lenRatio = (double)longer.Length / shorter.Length;

        // if longer isn't at least 1.5 times longer than the other, then its probably not an abbreviation
        if (lenRatio < 1.5) return 0;

        // numbers can't be abbreviations for other numbers, though that would be hilarious. "Yes, 4 - as in 4,238"
        var tokensLonger = longer.ExtractTokens();
        var tokensShorter = shorter.ExtractTokens();

        SequenceUtils.SwapIfSourceIsLonger(ref tokensShorter, ref tokensLonger);

        // more than 4 tokens and it's probably not an abbreviation (and could get costly)
        if (tokensShorter.Count > 4)
        {
            return 0;
        }

        var allPermutations = tokensLonger.PermutationsOfSize(tokensShorter.Count);

        double maxScore = 0;

        foreach (var permutation in allPermutations)
        {
            double sum = 0;
            for (int i = 0; i < tokensShorter.Count; i++)
            {
                var i1 = permutation[i];
                var i2 = tokensShorter[i];
                if (StringContainsInOrder(i1.AsSpan(), i2.AsSpan())) // must be at least twice as long
                {
                    var score = Scorer(i1, i2);
                    sum += score;
                }
            }
            var avgScore = sum / tokensShorter.Count;
            if (avgScore > maxScore)
            {
                maxScore = avgScore;
            }
        }

        return maxScore;
    }

    /// <summary>
    /// Does s2 have all its characters appear in order in s1? (Basically, is s2 a potential abbreviation of s1?)
    /// </summary>
    /// <param name="s1"></param>
    /// <param name="s2"></param>
    /// <returns></returns>
    private static bool StringContainsInOrder(ReadOnlySpan<char> s1, ReadOnlySpan<char> s2)
    {
        if (s1.Length < s2.Length) return false;
        int s2_idx = 0;
        for (int i = 0; i < s1.Length; i++)
        {
            if (s2[s2_idx] == s1[i])
                s2_idx++;
            if (s2_idx == s2.Length)
                return true;
            if (i + s2.Length - s2_idx == s1.Length)
                return false;
        }
        return false;
    }
}
