using System;

namespace Fuzzion;

/// <summary>
/// A function that scores two strings against each other. Kept for compatibility with
/// Raffinert.FuzzySharp; the library's own APIs take <see cref="SimilarityRatio.Scorer.IRatioScorer"/>.
/// </summary>
/// <param name="input1">The first string.</param>
/// <param name="input2">The second string.</param>
/// <returns>The score of the two strings.</returns>
public delegate double Scorer(string input1, string input2);
/// <summary>
/// A function that scores a string against a first string fixed in advance. Kept for
/// compatibility with Raffinert.FuzzySharp; the library's own APIs take
/// <see cref="SimilarityRatio.Scorer.ICachedRatioScorer"/>.
/// </summary>
/// <param name="input2">The string to compare with the fixed string.</param>
/// <returns>The score of the two strings.</returns>
public delegate double CachedScorer(string input2);
/// <summary>
/// Preprocesses a sequence before it is compared. The span is passed by reference, so the
/// processor can replace it with another span, for example a trimmed slice.
/// </summary>
/// <typeparam name="T">Element type of the sequence.</typeparam>
/// <param name="str">The sequence to process; on return, the processed sequence.</param>
public delegate void Processor<T>(ref ReadOnlySpan<T> str) where T : IEquatable<T>;
