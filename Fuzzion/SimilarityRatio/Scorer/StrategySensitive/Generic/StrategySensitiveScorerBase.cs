using System;
using Fuzzion.SimilarityRatio.Scorer.Generic;

namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive.Generic;

/// <summary>
/// Base class for sequence scorers built on a comparison function that the subclass supplies.
/// </summary>
/// <typeparam name="T">The element type; elements match when they are equal.</typeparam>
public abstract class StrategySensitiveScorerBase<T> : ScorerBase<T> where T : IEquatable<T>
{
    /// <summary>
    /// The function that compares two sequences and returns a score from 0 to 100, for example
    /// the simple ratio or the partial ratio; each subclass supplies it.
    /// </summary>
    protected abstract Func<T[], T[], double> Scorer { get; }
}
