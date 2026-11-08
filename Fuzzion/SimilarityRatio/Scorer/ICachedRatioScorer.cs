using System;

namespace Fuzzion.SimilarityRatio.Scorer;

/// <summary>
/// A scorer prepared once for a fixed first string (the query, <c>input1</c>), which then scores
/// many second strings against it. Implementations can hold pooled memory: dispose them when done.
/// </summary>
public interface ICachedRatioScorer: IDisposable
{
    /// <summary>Scores <paramref name="input2"/> against the query this scorer was prepared for.</summary>
    /// <param name="input2">The string to compare with the query.</param>
    /// <returns>A score from 0 to 100.</returns>
    double Score(string input2);
}
