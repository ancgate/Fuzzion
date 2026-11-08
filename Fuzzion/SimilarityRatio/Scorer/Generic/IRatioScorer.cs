using System;

namespace Fuzzion.SimilarityRatio.Scorer.Generic;

/// <summary>
/// Compares two sequences element by element and returns a similarity score from 0 to 100.
/// </summary>
/// <typeparam name="T">The element type; elements match when they are equal.</typeparam>
public interface IRatioScorer<in T> where T : IEquatable<T>
{
    /// <summary>Scores the two sequences.</summary>
    /// <param name="input1">The first sequence.</param>
    /// <param name="input2">The second sequence.</param>
    /// <returns>A score from 0 to 100.</returns>
    double Score(T[] input1, T[] input2);
}

/// <summary>
/// A sequence scorer prepared once for a fixed first sequence, which then scores many
/// second sequences against it.
/// </summary>
/// <typeparam name="T">The element type; elements match when they are equal.</typeparam>
public interface ICachedRatioScorer<in T> where T : IEquatable<T>
{
    /// <summary>Scores <paramref name="input2"/> against the sequence this scorer was prepared for.</summary>
    /// <param name="input2">The sequence to compare with the prepared one.</param>
    /// <returns>A score from 0 to 100.</returns>
    double Score(T[] input2);
}
