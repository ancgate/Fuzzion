using System;

namespace Fuzzion.SimilarityRatio.Scorer.Composite;

/// <summary>
/// A weighted combination of the other ratios for best results, as FuzzyWuzzy's and RapidFuzz's
/// <c>WRatio</c>. It takes the highest of: the simple ratio; then, when one string is at least
/// 1.5 times as long as the other, the partial ratio, partial token sort and partial token set ratios
/// (scaled by 0.9, or by 0.6 when one string is more than 8 times as long, and the token ratios also by 0.95);
/// otherwise the token sort and token set ratios scaled by 0.95. 0 when either input is empty.
/// Used by <see cref="Fuzz.WeightedRatio(string, string)"/>.
/// </summary>
public class WeightedRatioScorer : ScorerBase
{
    private static readonly double UNBASE_SCALE = .95;
    private static readonly double PARTIAL_SCALE = .90;
    private static readonly bool TRY_PARTIALS = true;

    /// <inheritdoc />
    public override double Score(string input1, string input2)
    {
        int len1 = input1.Length;
        int len2 = input2.Length;

        if (len1 == 0 || len2 == 0)
        {
            return 0;
        }

        bool tryPartials = TRY_PARTIALS;
        double unbaseScale = UNBASE_SCALE;
        double partialScale = PARTIAL_SCALE;

        double baseRatio = Fuzz.Ratio(input1, input2);
        double lenRatio = (double)Math.Max(len1, len2) / Math.Min(len1, len2);

        // if strings are similar length don't use partials
        if (lenRatio < 1.5) tryPartials = false;

        // if one string is much shorter than the other
        if (lenRatio > 8) partialScale = .6;

        if (tryPartials)
        {
            // Strings of very different lengths: compare the shorter one with parts of the
            // longer one, using the partial token scores (as FuzzyWuzzy and RapidFuzz do).
            double partial = Fuzz.PartialRatio(input1, input2) * partialScale;
            double partialSor = Fuzz.PartialTokenSortRatio(input1, input2) * unbaseScale * partialScale;
            double partialSet = Fuzz.PartialTokenSetRatio(input1, input2) * unbaseScale * partialScale;

            return Math.Max(baseRatio, Math.Max(partial, Math.Max(partialSor, partialSet)));
        }

        double tokenSort = Fuzz.TokenSortRatio(input1, input2) * unbaseScale;
        double tokenSet = Fuzz.TokenSetRatio(input1, input2) * unbaseScale;
        return Math.Max(baseRatio, Math.Max(tokenSort, tokenSet));
    }
}
