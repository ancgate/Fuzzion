namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

public abstract class StrategySensitiveScorerBase : ScorerBase
{
    protected abstract Fuzzion.Scorer Scorer { get; }
}