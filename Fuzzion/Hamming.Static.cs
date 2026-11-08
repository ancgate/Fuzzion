using System;
using Fuzzion.Utils;

namespace Fuzzion;

/// <summary>
/// Hamming distance: the number of positions at which two sequences differ.
/// With <c>pad</c> (the default) sequences of different lengths are allowed and every
/// extra element of the longer one counts as a difference. Without it, different
/// lengths throw <see cref="ArgumentException"/>.
/// Ported from RapidFuzz (MIT, Max Bachmann).
/// </summary>
public sealed partial class Hamming
{
    /// <summary>Number of differing positions (plus the length difference when padding).</summary>
    public static int Distance(string s1, string s2, bool pad = true) =>
        Distance(s1.AsSpan(), s2.AsSpan(), pad);

    /// <inheritdoc cref="Distance(string, string, bool)"/>
    public static int Distance<T>(ReadOnlySpan<T> s1, ReadOnlySpan<T> s2, bool pad = true) where T : IEquatable<T>
    {
        if (!pad && s1.Length != s2.Length)
        {
            throw new ArgumentException("Sequences are not the same length. Pass pad: true to allow different lengths.");
        }

        var minLength = Math.Min(s1.Length, s2.Length);
        var distance = Math.Max(s1.Length, s2.Length);
        for (var i = 0; i < minLength; i++)
        {
            if (s1[i].Equals(s2[i]))
            {
                distance--;
            }
        }

        return distance;
    }

    /// <summary>Number of equal positions: <c>max(len1, len2) - Distance</c>.</summary>
    public static int Similarity(string s1, string s2, bool pad = true) =>
        Similarity(s1.AsSpan(), s2.AsSpan(), pad);

    /// <inheritdoc cref="Similarity(string, string, bool)"/>
    public static int Similarity<T>(ReadOnlySpan<T> s1, ReadOnlySpan<T> s2, bool pad = true) where T : IEquatable<T> =>
        Math.Max(s1.Length, s2.Length) - Distance(s1, s2, pad);

    /// <summary>Distance divided by the longer length, from 0 (equal) to 1.</summary>
    public static double NormalizedDistance(string s1, string s2, bool pad = true) =>
        NormalizedDistance(s1.AsSpan(), s2.AsSpan(), pad);

    /// <inheritdoc cref="NormalizedDistance(string, string, bool)"/>
    public static double NormalizedDistance<T>(ReadOnlySpan<T> s1, ReadOnlySpan<T> s2, bool pad = true) where T : IEquatable<T> =>
        DistanceMath.Normalize(Distance(s1, s2, pad), Math.Max(s1.Length, s2.Length));

    /// <summary><c>1 - NormalizedDistance</c>, from 0 to 1 (equal).</summary>
    public static double NormalizedSimilarity(string s1, string s2, bool pad = true) =>
        NormalizedSimilarity(s1.AsSpan(), s2.AsSpan(), pad);

    /// <inheritdoc cref="NormalizedSimilarity(string, string, bool)"/>
    public static double NormalizedSimilarity<T>(ReadOnlySpan<T> s1, ReadOnlySpan<T> s2, bool pad = true) where T : IEquatable<T> =>
        1.0 - NormalizedDistance(s1, s2, pad);
}
