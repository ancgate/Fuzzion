namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

/// <summary>
/// Base class for string scorers built on a comparison function that the subclass supplies:
/// the same algorithm, for example token sort, then works with the simple ratio
/// (<see cref="Fuzz.Ratio(string, string)"/>) or the partial ratio (<see cref="Fuzz.PartialRatio(string, string)"/>).
/// </summary>
public abstract class StrategySensitiveScorerBase : ScorerBase
{
    /// <summary>
    /// The function that compares two strings and returns a score from 0 to 100; each subclass supplies it.
    /// </summary>
    protected abstract Fuzzion.Scorer Scorer { get; }
}