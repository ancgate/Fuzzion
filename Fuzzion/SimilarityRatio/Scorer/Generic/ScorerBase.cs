using System;

namespace Fuzzion.SimilarityRatio.Scorer.Generic;

/// <summary>Base class for <see cref="IRatioScorer{T}"/> implementations.</summary>
/// <typeparam name="T">The element type; elements match when they are equal.</typeparam>
public abstract class ScorerBase<T> : IRatioScorer<T> where T : IEquatable<T>
{
    /// <inheritdoc />
    public abstract double Score(T[] input1, T[] input2);
}
