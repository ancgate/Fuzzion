using System;

namespace Fuzzion.SimilarityRatio.Strategy;

/// <summary>
/// A comparison prepared once for a fixed first string (the query), which then scores many second
/// strings against it. Several cached scorers can share one, see
/// <see cref="Scorer.StrategySensitive.CachedDefaultRatioScorer(ICachedStrategy, bool)"/>. Dispose it when done.
/// </summary>
public interface ICachedStrategy : IDisposable
{
    /// <summary>Scores <paramref name="input2"/> against the prepared query.</summary>
    /// <param name="input2">The string to compare with the query.</param>
    /// <returns>A score from 0 to 100.</returns>
    double Calculate(string input2);
}
