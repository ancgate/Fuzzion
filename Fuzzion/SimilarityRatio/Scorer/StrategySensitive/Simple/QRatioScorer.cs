using Fuzzion.SimilarityRatio.Strategy;

namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

/// <summary>
/// Quick ratio: <see cref="DefaultRatioScorer"/>, but 0 when either input is empty
/// (including both). Same as RapidFuzz's <c>QRatio</c>.
/// </summary>
public sealed class QRatioScorer : SimpleRatioScorerBase
{
    protected override Fuzzion.Scorer Scorer => QRatioStrategy.Calculate;
}
