using System;
using System.Collections.Generic;

namespace Fuzzion.Extractor;

/// <summary>
/// One match returned by a search: the choice, its score and its position in the choices.
/// </summary>
/// <typeparam name="T">The type of the choices.</typeparam>
/// <param name="value">The matched choice.</param>
/// <param name="score">The score of the choice.</param>
/// <param name="index">The zero-based position of the choice in the searched choices.</param>
public class ExtractedResult<T>(T value, double score, int index) : IComparable<ExtractedResult<T>>
{
    /// <summary>The matched choice, as it was passed in (not preprocessed).</summary>
    public readonly T Value = value;

    /// <summary>
    /// The score of the choice: a similarity (usually 0 to 100, higher is better), or a
    /// distance (lower is better) when the search used an <see cref="IDistanceScorer"/>.
    /// </summary>
    public readonly double Score = score;

    /// <summary>The zero-based position of the choice in the searched choices.</summary>
    public readonly int Index = index;

    /// <summary>Creates a result with <see cref="Index"/> set to 0.</summary>
    /// <param name="value">The matched choice.</param>
    /// <param name="score">The score of the choice.</param>
    public ExtractedResult(T value, double score) : this(value, score, 0)
    { }

    /// <summary>
    /// Compares by <see cref="Score"/> only, lowest first, so sorting a list of results puts
    /// the lowest score first. <see cref="Value"/> and <see cref="Index"/> are ignored.
    /// </summary>
    /// <param name="other">The result to compare with; must not be null.</param>
    /// <returns>Less than 0 when this score is lower, 0 when equal, more than 0 when higher.</returns>
    public int CompareTo(ExtractedResult<T> other)
    {
        return Comparer<double>.Default.Compare(this.Score, other.Score);
    }

    /// <summary>
    /// Formats the result as <c>(string: value, score: s, index: i)</c> for string choices,
    /// or <c>(value: value, score: s, index: i)</c> otherwise.
    /// </summary>
    /// <returns>The formatted result.</returns>
    public override string ToString()
    {
        if (typeof(T) == typeof(string))
        {
            return $"(string: {Value}, score: {Score}, index: {Index})";
        }
        return $"(value: {Value}, score: {Score}, index: {Index})";
    }
}
