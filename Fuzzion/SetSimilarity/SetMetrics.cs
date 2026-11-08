using System;
using System.Collections.Generic;

namespace Fuzzion;

// Set-based similarity metrics (plan items #9-#12). Strings are compared by their
// q-grams (see QGramOptions); the IEnumerable<T> overloads compare any collections, for
// example word lists. Written from the textbook definitions.
//
// Empty inputs: two empty collections are identical (similarity 1); one empty
// collection has similarity 0. Two strings that are both too short to have any q-gram
// (for example "a" and "b" with bigrams and no padding) are compared for plain
// equality: 1 if equal, otherwise 0.

/// <summary>Jaccard similarity: <c>|A ∩ B| / |A ∪ B|</c>, from 0 to 1.</summary>
public static class Jaccard
{
    /// <summary>Jaccard similarity of the q-grams of two strings.</summary>
    public static double Similarity(string a, string b, QGramOptions options = null) =>
        SetMetric.OfStrings(a, b, options, Formula);

    /// <summary><c>1 - Similarity</c>.</summary>
    public static double Distance(string a, string b, QGramOptions options = null) => 1.0 - Similarity(a, b, options);

    /// <summary>Jaccard similarity of two collections, for example word lists.</summary>
    public static double Similarity<T>(IEnumerable<T> a, IEnumerable<T> b, QGramMode mode = QGramMode.Set) =>
        SetMetric.OfElements(a, b, mode, Formula);

    /// <summary><c>1 - Similarity</c>.</summary>
    public static double Distance<T>(IEnumerable<T> a, IEnumerable<T> b, QGramMode mode = QGramMode.Set) =>
        1.0 - Similarity(a, b, mode);

    private static double Formula(OverlapStats s) => s.Common / (double)(s.SizeA + s.SizeB - s.Common);
}

/// <summary>Sørensen-Dice similarity: <c>2|A ∩ B| / (|A| + |B|)</c>, from 0 to 1.</summary>
public static class SorensenDice
{
    /// <summary>Sørensen-Dice similarity of the q-grams of two strings.</summary>
    public static double Similarity(string a, string b, QGramOptions options = null) =>
        SetMetric.OfStrings(a, b, options, Formula);

    /// <summary><c>1 - Similarity</c>.</summary>
    public static double Distance(string a, string b, QGramOptions options = null) => 1.0 - Similarity(a, b, options);

    /// <summary>Sørensen-Dice similarity of two collections.</summary>
    public static double Similarity<T>(IEnumerable<T> a, IEnumerable<T> b, QGramMode mode = QGramMode.Set) =>
        SetMetric.OfElements(a, b, mode, Formula);

    /// <summary><c>1 - Similarity</c>.</summary>
    public static double Distance<T>(IEnumerable<T> a, IEnumerable<T> b, QGramMode mode = QGramMode.Set) =>
        1.0 - Similarity(a, b, mode);

    private static double Formula(OverlapStats s) => 2.0 * s.Common / (s.SizeA + s.SizeB);
}

/// <summary>
/// Overlap coefficient (Szymkiewicz-Simpson): <c>|A ∩ B| / min(|A|, |B|)</c>, from 0 to 1.
/// It is 1 whenever one collection is contained in the other.
/// </summary>
public static class Overlap
{
    /// <summary>Overlap coefficient of the q-grams of two strings.</summary>
    public static double Similarity(string a, string b, QGramOptions options = null) =>
        SetMetric.OfStrings(a, b, options, Formula);

    /// <summary><c>1 - Similarity</c>.</summary>
    public static double Distance(string a, string b, QGramOptions options = null) => 1.0 - Similarity(a, b, options);

    /// <summary>Overlap coefficient of two collections.</summary>
    public static double Similarity<T>(IEnumerable<T> a, IEnumerable<T> b, QGramMode mode = QGramMode.Set) =>
        SetMetric.OfElements(a, b, mode, Formula);

    /// <summary><c>1 - Similarity</c>.</summary>
    public static double Distance<T>(IEnumerable<T> a, IEnumerable<T> b, QGramMode mode = QGramMode.Set) =>
        1.0 - Similarity(a, b, mode);

    private static double Formula(OverlapStats s)
    {
        var smaller = Math.Min(s.SizeA, s.SizeB);
        return smaller == 0 ? 0.0 : s.Common / (double)smaller;
    }
}

/// <summary>
/// Tanimoto coefficient: <c>A·B / (|A|² + |B|² − A·B)</c> on q-gram <b>count</b> vectors,
/// from 0 to 1. Repeated q-grams always count (the <see cref="QGramOptions.Mode"/> setting
/// is ignored): on plain sets this formula equals <see cref="Jaccard"/> exactly, so
/// counting repeats is what makes Tanimoto a separate metric.
/// </summary>
public static class Tanimoto
{
    /// <summary>Tanimoto coefficient of the q-gram counts of two strings.</summary>
    public static double Similarity(string a, string b, QGramOptions options = null) =>
        SetMetric.OfStrings(a, b, options, Formula);

    /// <summary><c>1 - Similarity</c>.</summary>
    public static double Distance(string a, string b, QGramOptions options = null) => 1.0 - Similarity(a, b, options);

    /// <summary>Tanimoto coefficient of the element counts of two collections.</summary>
    public static double Similarity<T>(IEnumerable<T> a, IEnumerable<T> b) =>
        SetMetric.OfElements(a, b, QGramMode.Multiset, Formula);

    /// <summary><c>1 - Similarity</c>.</summary>
    public static double Distance<T>(IEnumerable<T> a, IEnumerable<T> b) => 1.0 - Similarity(a, b);

    private static double Formula(OverlapStats s) => s.Dot / (s.SquaresA + s.SquaresB - s.Dot);
}

internal static class SetMetric
{
    public static double OfStrings(string a, string b, QGramOptions options, Func<OverlapStats, double> formula)
    {
        var stats = OverlapStats.ForStrings(a, b, options);
        if (stats.BothEmpty)
        {
            return string.Equals(a, b, StringComparison.Ordinal) ? 1.0 : 0.0;
        }

        return Clamp(formula(stats));
    }

    public static double OfElements<T>(IEnumerable<T> a, IEnumerable<T> b, QGramMode mode, Func<OverlapStats, double> formula)
    {
        var stats = OverlapStats.ForElements(a, b, mode);
        return stats.BothEmpty ? 1.0 : Clamp(formula(stats));
    }

    private static double Clamp(double value) => value < 0 ? 0 : value > 1 ? 1 : value;
}
