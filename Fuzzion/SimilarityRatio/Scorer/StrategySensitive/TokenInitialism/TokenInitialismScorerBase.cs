using Fuzzion.Extensions;

namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

/// <summary>
/// Base class for initialism scorers: the first character of each whitespace-separated word of
/// the longer string forms an initialism, which is compared with the shorter string using
/// <see cref="StrategySensitiveScorerBase.Scorer"/>. The order of the inputs does not matter;
/// the score is 0 unless the longer string is at least three times as long as the shorter.
/// </summary>
public abstract class TokenInitialismScorerBase : StrategySensitiveScorerBase
{
    /// <inheritdoc />
    public override double Score(string input1, string input2)
    {
        string shorter;
        string longer;

        if (input1.Length < input2.Length)
        {
            shorter = input1;
            longer  = input2;
        }
        else
        {
            shorter = input2;
            longer  = input1;
        }

        double lenRatio = (double)longer.Length / shorter.Length;

        // if longer isn't at least 3 times longer than the other, then it's probably not an initialism
        if (lenRatio < 3) return 0;

        var initials = longer.GetInitials();

        return Scorer(initials, shorter);
    }
}
