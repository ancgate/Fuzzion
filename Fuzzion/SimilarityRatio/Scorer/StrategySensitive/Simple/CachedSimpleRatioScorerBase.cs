namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

/// <summary>
/// Base class for cached scorers that score each second string with <see cref="Scorer"/>.
/// </summary>
public abstract class CachedSimpleRatioScorerBase : ICachedRatioScorer
{
    /// <summary>
    /// The function that scores a second string against the prepared query (0 to 100); each subclass supplies it.
    /// </summary>
    protected abstract CachedScorer Scorer { get; }
    /// <summary>Releases the pooled memory held for the query, if this scorer owns it.</summary>
    public abstract void Dispose();

    /// <inheritdoc />
    public double Score(string input2)
    {
        return Scorer(input2);
    }
}
