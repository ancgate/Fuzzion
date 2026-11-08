using System;
using Fuzzion.Utils;

namespace Fuzzion;

/// <summary>
/// Prefix similarity: the length of the shared start of two sequences.
/// <c>Distance = max(len1, len2) - Similarity</c>. Ported from RapidFuzz (MIT, Max Bachmann).
/// </summary>
public static class Prefix
{
    /// <summary>Length of the shared start.</summary>
    public static int Similarity(string s1, string s2) => Similarity(s1.AsSpan(), s2.AsSpan());

    /// <inheritdoc cref="Similarity(string, string)"/>
    public static int Similarity<T>(ReadOnlySpan<T> s1, ReadOnlySpan<T> s2) where T : IEquatable<T> =>
        SequenceUtils.CommonPrefix(s1, s2);

    /// <summary><c>max(len1, len2) - Similarity</c>.</summary>
    public static int Distance(string s1, string s2) => Distance(s1.AsSpan(), s2.AsSpan());

    /// <inheritdoc cref="Distance(string, string)"/>
    public static int Distance<T>(ReadOnlySpan<T> s1, ReadOnlySpan<T> s2) where T : IEquatable<T> =>
        Math.Max(s1.Length, s2.Length) - Similarity(s1, s2);

    /// <summary>Distance divided by the longer length, from 0 to 1.</summary>
    public static double NormalizedDistance(string s1, string s2) => NormalizedDistance(s1.AsSpan(), s2.AsSpan());

    /// <inheritdoc cref="NormalizedDistance(string, string)"/>
    public static double NormalizedDistance<T>(ReadOnlySpan<T> s1, ReadOnlySpan<T> s2) where T : IEquatable<T> =>
        DistanceMath.Normalize(Distance(s1, s2), Math.Max(s1.Length, s2.Length));

    /// <summary><c>1 - NormalizedDistance</c>, from 0 to 1.</summary>
    public static double NormalizedSimilarity(string s1, string s2) => NormalizedSimilarity(s1.AsSpan(), s2.AsSpan());

    /// <inheritdoc cref="NormalizedSimilarity(string, string)"/>
    public static double NormalizedSimilarity<T>(ReadOnlySpan<T> s1, ReadOnlySpan<T> s2) where T : IEquatable<T> =>
        1.0 - NormalizedDistance(s1, s2);
}

/// <summary>
/// Postfix similarity: the length of the shared end of two sequences.
/// <c>Distance = max(len1, len2) - Similarity</c>. Ported from RapidFuzz (MIT, Max Bachmann).
/// </summary>
public static class Postfix
{
    /// <summary>Length of the shared end.</summary>
    public static int Similarity(string s1, string s2) => Similarity(s1.AsSpan(), s2.AsSpan());

    /// <inheritdoc cref="Similarity(string, string)"/>
    public static int Similarity<T>(ReadOnlySpan<T> s1, ReadOnlySpan<T> s2) where T : IEquatable<T> =>
        SequenceUtils.CommonSuffix(s1, s2);

    /// <summary><c>max(len1, len2) - Similarity</c>.</summary>
    public static int Distance(string s1, string s2) => Distance(s1.AsSpan(), s2.AsSpan());

    /// <inheritdoc cref="Distance(string, string)"/>
    public static int Distance<T>(ReadOnlySpan<T> s1, ReadOnlySpan<T> s2) where T : IEquatable<T> =>
        Math.Max(s1.Length, s2.Length) - Similarity(s1, s2);

    /// <summary>Distance divided by the longer length, from 0 to 1.</summary>
    public static double NormalizedDistance(string s1, string s2) => NormalizedDistance(s1.AsSpan(), s2.AsSpan());

    /// <inheritdoc cref="NormalizedDistance(string, string)"/>
    public static double NormalizedDistance<T>(ReadOnlySpan<T> s1, ReadOnlySpan<T> s2) where T : IEquatable<T> =>
        DistanceMath.Normalize(Distance(s1, s2), Math.Max(s1.Length, s2.Length));

    /// <summary><c>1 - NormalizedDistance</c>, from 0 to 1.</summary>
    public static double NormalizedSimilarity(string s1, string s2) => NormalizedSimilarity(s1.AsSpan(), s2.AsSpan());

    /// <inheritdoc cref="NormalizedSimilarity(string, string)"/>
    public static double NormalizedSimilarity<T>(ReadOnlySpan<T> s1, ReadOnlySpan<T> s2) where T : IEquatable<T> =>
        1.0 - NormalizedDistance(s1, s2);
}
