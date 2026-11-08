using Fuzzion.Extensions;

namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

/// <summary>
/// Base class for token sort scorers: the words (split on whitespace) of each string are sorted
/// and rejoined with single spaces, and the results compared with <see cref="StrategySensitiveScorerBase.Scorer"/>.
/// </summary>
public abstract class TokenSortScorerBase : StrategySensitiveScorerBase
{
    /// <inheritdoc />
    public override double Score(string input1, string input2)
    {
        var sorted1 = input1.NormalizeSpacesAndSort();
        var sorted2 = input2.NormalizeSpacesAndSort();

        return Scorer(sorted1, sorted2);
    }
}
