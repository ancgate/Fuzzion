using System;
using System.Collections.Generic;

namespace Fuzzion;

/// <summary>How repeated q-grams (or elements) are counted by the set-based metrics.</summary>
public enum QGramMode
{
    /// <summary>Each distinct q-gram counts once (classic set semantics).</summary>
    Set,

    /// <summary>Repeated q-grams count as many times as they occur (bag / multiset semantics).</summary>
    Multiset,
}

/// <summary>Options for the q-gram based metrics (<see cref="Jaccard"/>, <see cref="SorensenDice"/>, <see cref="Overlap"/>, <see cref="Tanimoto"/>).</summary>
public sealed class QGramOptions
{
    /// <summary>Bigrams, no padding, set semantics.</summary>
    public static QGramOptions Default { get; } = new();

    /// <param name="q">Length of each q-gram (2 = bigrams, 3 = trigrams). At least 1.</param>
    /// <param name="pad">
    /// Add <c>q - 1</c> start markers (U+0002) before and end markers (U+0003) after each
    /// string, so the first and last characters appear in as many q-grams as the others
    /// and strings shorter than <paramref name="q"/> still produce q-grams.
    /// </param>
    /// <param name="mode">Whether repeated q-grams count once or every time.</param>
    public QGramOptions(int q = 2, bool pad = false, QGramMode mode = QGramMode.Set)
    {
        if (q < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(q), q, "q must be at least 1.");
        }

        Q = q;
        Pad = pad;
        Mode = mode;
    }

    /// <summary>Length of each q-gram.</summary>
    public int Q { get; }

    /// <summary>Whether strings are padded with start and end markers.</summary>
    public bool Pad { get; }

    /// <summary>Whether repeated q-grams count once or every time.</summary>
    public QGramMode Mode { get; }
}

/// <summary>Splits strings into q-grams: overlapping substrings of length q.</summary>
public static class QGrams
{
    internal const char StartMarker = '\u0002';
    internal const char EndMarker = '\u0003';

    /// <summary>
    /// The q-grams of <paramref name="s"/> in order, for example
    /// <c>Of("night", 2)</c> → <c>["ni", "ig", "gh", "ht"]</c>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="q"/> is less than 1.</exception>
    public static IReadOnlyList<string> Of(string s, int q = 2, bool pad = false)
    {
        if (q < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(q), q, "q must be at least 1.");
        }

        if (s == null)
        {
            throw new ArgumentNullException(nameof(s));
        }

        var text = pad ? Padded(s, q) : s;
        var grams = new List<string>(Math.Max(0, text.Length - q + 1));
        for (var i = 0; i + q <= text.Length; i++)
        {
            grams.Add(text.Substring(i, q));
        }

        return grams;
    }

    internal static string Padded(string s, int q) =>
        q <= 1 ? s : string.Concat(new string(StartMarker, q - 1), s, new string(EndMarker, q - 1));
}

/// <summary>Everything the set-based metrics need about two collections, computed in one pass.</summary>
internal readonly struct OverlapStats
{
    public OverlapStats(long sizeA, long sizeB, long common, double dot, double squaresA, double squaresB)
    {
        SizeA = sizeA;
        SizeB = sizeB;
        Common = common;
        Dot = dot;
        SquaresA = squaresA;
        SquaresB = squaresB;
    }

    /// <summary>|A| (distinct q-grams in set mode, all q-grams in multiset mode).</summary>
    public long SizeA { get; }

    /// <summary>|B|.</summary>
    public long SizeB { get; }

    /// <summary>|A ∩ B| (in multiset mode, the sum of the smaller count of each q-gram).</summary>
    public long Common { get; }

    /// <summary>Dot product of the count vectors (Tanimoto).</summary>
    public double Dot { get; }

    /// <summary>Sum of squared counts of A (Tanimoto).</summary>
    public double SquaresA { get; }

    /// <summary>Sum of squared counts of B (Tanimoto).</summary>
    public double SquaresB { get; }

    public bool BothEmpty => SizeA == 0 && SizeB == 0;

    public static OverlapStats ForStrings(string a, string b, QGramOptions options)
    {
        if (a == null) throw new ArgumentNullException(nameof(a));
        if (b == null) throw new ArgumentNullException(nameof(b));

        options ??= QGramOptions.Default;
        var q = options.Q;
        var textA = options.Pad ? QGrams.Padded(a, q) : a;
        var textB = options.Pad ? QGrams.Padded(b, q) : b;

        // q-grams of up to four characters fit in a 64-bit key, which avoids creating a
        // substring per q-gram.
        return q <= 4
            ? FromCounts(CountPacked(textA, q), CountPacked(textB, q), options.Mode)
            : FromCounts(CountStrings(textA, q), CountStrings(textB, q), options.Mode);
    }

    public static OverlapStats ForElements<T>(IEnumerable<T> a, IEnumerable<T> b, QGramMode mode)
    {
        if (a == null) throw new ArgumentNullException(nameof(a));
        if (b == null) throw new ArgumentNullException(nameof(b));

        return FromCounts(CountElements(a), CountElements(b), mode);
    }

    private static Dictionary<ulong, int> CountPacked(string text, int q)
    {
        var counts = new Dictionary<ulong, int>();
        for (var i = 0; i + q <= text.Length; i++)
        {
            ulong key = 0;
            for (var k = 0; k < q; k++)
            {
                key = (key << 16) | text[i + k];
            }

            counts.TryGetValue(key, out var n);
            counts[key] = n + 1;
        }

        return counts;
    }

    private static Dictionary<string, int> CountStrings(string text, int q)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i + q <= text.Length; i++)
        {
            var key = text.Substring(i, q);
            counts.TryGetValue(key, out var n);
            counts[key] = n + 1;
        }

        return counts;
    }

    private static Dictionary<T, int> CountElements<T>(IEnumerable<T> items)
    {
        var counts = new Dictionary<T, int>();
        foreach (var item in items)
        {
            counts.TryGetValue(item, out var n);
            counts[item] = n + 1;
        }

        return counts;
    }

    private static OverlapStats FromCounts<TKey>(Dictionary<TKey, int> a, Dictionary<TKey, int> b, QGramMode mode)
    {
        long sizeA = 0, sizeB = 0, common = 0;
        double dot = 0, squaresA = 0, squaresB = 0;
        var multiset = mode == QGramMode.Multiset;

        foreach (var pair in a)
        {
            sizeA += multiset ? pair.Value : 1;
            squaresA += (double)pair.Value * pair.Value;
            if (b.TryGetValue(pair.Key, out var countB))
            {
                common += multiset ? Math.Min(pair.Value, countB) : 1;
                dot += (double)pair.Value * countB;
            }
        }

        foreach (var pair in b)
        {
            sizeB += multiset ? pair.Value : 1;
            squaresB += (double)pair.Value * pair.Value;
        }

        return new OverlapStats(sizeA, sizeB, common, dot, squaresA, squaresB);
    }
}
