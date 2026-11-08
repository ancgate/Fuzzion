namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

/// <summary>
/// Base class for scorers that apply <see cref="StrategySensitiveScorerBase.Scorer"/> directly to the two strings.
/// </summary>
public abstract class SimpleRatioScorerBase : StrategySensitiveScorerBase
{
    /// <inheritdoc />
    public override double Score(string input1, string input2)
    {
        return Scorer(input1, input2);
    }
}
