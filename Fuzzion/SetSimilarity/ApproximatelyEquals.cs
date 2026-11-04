using System;

namespace Fuzzion;

/// <summary>How similar two strings must be for <see cref="StringSimilarity.ApproximatelyEquals(string, string, SimilarityTolerance, SimilarityMetric[])"/>.</summary>
public enum SimilarityTolerance
{
    /// <summary>Average normalized similarity of at least 0.90.</summary>
    Strong,

    /// <summary>Average normalized similarity of at least 0.75.</summary>
    Normal,

    /// <summary>Average normalized similarity of at least 0.60.</summary>
    Weak,
}

/// <summary>A metric that <see cref="StringSimilarity.ApproximatelyEquals(string, string, SimilarityTolerance, SimilarityMetric[])"/> can combine.</summary>
public enum SimilarityMetric
{
    /// <summary><see cref="Fuzzion.Levenshtein"/>.</summary>
    Levenshtein,
    /// <summary><see cref="Fuzzion.DamerauLevenshtein"/>.</summary>
    DamerauLevenshtein,
    /// <summary><see cref="Fuzzion.Osa"/>.</summary>
    Osa,
    /// <summary><see cref="Fuzzion.Indel"/>.</summary>
    Indel,
    /// <summary><see cref="Fuzzion.Hamming"/> (with padding).</summary>
    Hamming,
    /// <summary><see cref="Fuzzion.Jaro"/>.</summary>
    Jaro,
    /// <summary><see cref="Fuzzion.JaroWinkler"/>.</summary>
    JaroWinkler,
    /// <summary><see cref="Fuzzion.LongestCommonSubsequence"/>.</summary>
    LongestCommonSubsequence,
    /// <summary><see cref="Fuzzion.LongestCommonSubstring"/>.</summary>
    LongestCommonSubstring,
    /// <summary><see cref="Fuzzion.Jaccard"/> on bigrams.</summary>
    Jaccard,
    /// <summary><see cref="Fuzzion.SorensenDice"/> on bigrams.</summary>
    SorensenDice,
    /// <summary><see cref="Fuzzion.Overlap"/> on bigrams.</summary>
    Overlap,
    /// <summary><see cref="Fuzzion.Tanimoto"/> on bigrams.</summary>
    Tanimoto,
    /// <summary><see cref="Fuzzion.RatcliffObershelp"/>.</summary>
    RatcliffObershelp,
}

/// <summary>Combines several normalized similarities into a yes/no answer (plan item #16).</summary>
public static class StringSimilarity
{
    /// <summary>The minimum average similarity for each <see cref="SimilarityTolerance"/>.</summary>
    public static double Threshold(SimilarityTolerance tolerance) => tolerance switch
    {
        SimilarityTolerance.Strong => 0.90,
        SimilarityTolerance.Normal => 0.75,
        SimilarityTolerance.Weak => 0.60,
        _ => throw new ArgumentOutOfRangeException(nameof(tolerance), tolerance, null),
    };

    /// <summary>
    /// True when the average normalized similarity of <paramref name="metrics"/> reaches
    /// the <paramref name="tolerance"/> threshold. Case is ignored.
    /// </summary>
    /// <exception cref="ArgumentException">No metric was given.</exception>
    public static bool ApproximatelyEquals(this string a, string b, SimilarityTolerance tolerance, params SimilarityMetric[] metrics) =>
        ApproximatelyEquals(a, b, Threshold(tolerance), caseSensitive: false, metrics);

    /// <summary>
    /// True when the average normalized similarity of <paramref name="metrics"/> is at least
    /// <paramref name="threshold"/> (0 to 1). Case is ignored.
    /// </summary>
    public static bool ApproximatelyEquals(this string a, string b, double threshold, params SimilarityMetric[] metrics) =>
        ApproximatelyEquals(a, b, threshold, caseSensitive: false, metrics);

    /// <summary>
    /// True when the average normalized similarity of <paramref name="metrics"/> is at least
    /// <paramref name="threshold"/> (0 to 1).
    /// </summary>
    public static bool ApproximatelyEquals(this string a, string b, double threshold, bool caseSensitive, params SimilarityMetric[] metrics) =>
        AverageSimilarity(a, b, caseSensitive, metrics) >= threshold;

    /// <summary>The average normalized similarity (0 to 1) of the given metrics.</summary>
    /// <exception cref="ArgumentException">No metric was given.</exception>
    public static double AverageSimilarity(string a, string b, bool caseSensitive, params SimilarityMetric[] metrics)
    {
        if (a == null) throw new ArgumentNullException(nameof(a));
        if (b == null) throw new ArgumentNullException(nameof(b));
        if (metrics == null || metrics.Length == 0)
        {
            throw new ArgumentException("Pass at least one metric.", nameof(metrics));
        }

        if (!caseSensitive)
        {
            a = a.ToLowerInvariant();
            b = b.ToLowerInvariant();
        }

        var total = 0.0;
        foreach (var metric in metrics)
        {
            total += NormalizedSimilarity(a, b, metric);
        }

        return total / metrics.Length;
    }

    /// <summary>The normalized similarity (0 to 1) of one metric.</summary>
    public static double NormalizedSimilarity(string a, string b, SimilarityMetric metric) => metric switch
    {
        SimilarityMetric.Levenshtein => Levenshtein.NormalizedSimilarity(a, b),
        SimilarityMetric.DamerauLevenshtein => DamerauLevenshtein.NormalizedSimilarity(a, b),
        SimilarityMetric.Osa => Osa.NormalizedSimilarity(a, b),
        SimilarityMetric.Indel => Indel.NormalizedSimilarity(a.AsSpan(), b.AsSpan()),
        SimilarityMetric.Hamming => Hamming.NormalizedSimilarity(a, b),
        SimilarityMetric.Jaro => Jaro.Similarity(a, b),
        SimilarityMetric.JaroWinkler => JaroWinkler.Similarity(a, b),
        SimilarityMetric.LongestCommonSubsequence => LongestCommonSubsequence.NormalizedSimilarity(a.AsSpan(), b.AsSpan()),
        SimilarityMetric.LongestCommonSubstring => LongestCommonSubstring.NormalizedSimilarity(a, b),
        SimilarityMetric.Jaccard => Jaccard.Similarity(a, b),
        SimilarityMetric.SorensenDice => SorensenDice.Similarity(a, b),
        SimilarityMetric.Overlap => Overlap.Similarity(a, b),
        SimilarityMetric.Tanimoto => Tanimoto.Similarity(a, b),
        SimilarityMetric.RatcliffObershelp => RatcliffObershelp.Similarity(a, b),
        _ => throw new ArgumentOutOfRangeException(nameof(metric), metric, null),
    };
}
