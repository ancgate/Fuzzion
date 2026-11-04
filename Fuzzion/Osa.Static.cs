using System;
using System.Buffers;
using Fuzzion.Utils;

namespace Fuzzion;

/// <summary>
/// Optimal String Alignment (OSA) distance, also called restricted Damerau-Levenshtein:
/// the minimum number of insertions, deletions, substitutions and swaps of two adjacent
/// elements, where no part of the input is edited more than once. So <c>("CA", "ABC")</c>
/// is 3 here but 2 for <see cref="DamerauLevenshtein"/>.
/// Ported from RapidFuzz (MIT, Max Bachmann). Inputs whose shorter side fits in 64
/// elements use the bit-parallel algorithm of Hyyrö (2003); longer ones use a three-row
/// table (the multi-block bit-parallel version is a planned optimization).
/// </summary>
public sealed partial class Osa
{
    /// <summary>Minimum number of OSA edits.</summary>
    public static int Distance(string s1, string s2) => Distance(s1.AsSpan(), s2.AsSpan());

    /// <inheritdoc cref="Distance(string, string)"/>
    public static int Distance<T>(ReadOnlySpan<T> s1, ReadOnlySpan<T> s2) where T : notnull, IEquatable<T>
    {
        // A shared start or end never changes the distance.
        SequenceUtils.TrimCommonAffix(ref s1, ref s2);
        SequenceUtils.SwapIfSourceIsLonger(ref s1, ref s2);

        if (s1.IsEmpty)
        {
            return s2.Length;
        }

        if (s1.Length <= 64)
        {
            using var pattern = PatternMatchVector.Create(s1);
            return DistanceHyrroe2003(pattern, s2);
        }

        return DistanceTable(s1, s2);
    }

    /// <summary><c>max(len1, len2) - Distance</c>.</summary>
    public static int Similarity(string s1, string s2) => Similarity(s1.AsSpan(), s2.AsSpan());

    /// <inheritdoc cref="Similarity(string, string)"/>
    public static int Similarity<T>(ReadOnlySpan<T> s1, ReadOnlySpan<T> s2) where T : notnull, IEquatable<T> =>
        Math.Max(s1.Length, s2.Length) - Distance(s1, s2);

    /// <summary>Distance divided by the longer length, from 0 (equal) to 1.</summary>
    public static double NormalizedDistance(string s1, string s2) => NormalizedDistance(s1.AsSpan(), s2.AsSpan());

    /// <inheritdoc cref="NormalizedDistance(string, string)"/>
    public static double NormalizedDistance<T>(ReadOnlySpan<T> s1, ReadOnlySpan<T> s2) where T : notnull, IEquatable<T> =>
        DistanceMath.Normalize(Distance(s1, s2), Math.Max(s1.Length, s2.Length));

    /// <summary><c>1 - NormalizedDistance</c>, from 0 to 1 (equal).</summary>
    public static double NormalizedSimilarity(string s1, string s2) => NormalizedSimilarity(s1.AsSpan(), s2.AsSpan());

    /// <inheritdoc cref="NormalizedSimilarity(string, string)"/>
    public static double NormalizedSimilarity<T>(ReadOnlySpan<T> s1, ReadOnlySpan<T> s2) where T : notnull, IEquatable<T> =>
        1.0 - NormalizedDistance(s1, s2);

    /// <summary>
    /// Bit-parallel OSA distance for a pattern of at most 64 elements (Hyyrö 2003,
    /// "A Bit-Vector Algorithm for Computing Levenshtein and Damerau Edit Distances").
    /// </summary>
    internal static int DistanceHyrroe2003<T>(IPatternMatchVector<T> pattern, ReadOnlySpan<T> text)
        where T : notnull, IEquatable<T>
    {
        var length = pattern.Length;
        var vp = length == 64 ? ulong.MaxValue : (1UL << length) - 1;
        var vn = 0UL;
        var d0 = 0UL;
        var pmPrevious = 0UL;
        var distance = length;
        var mask = 1UL << (length - 1);

        foreach (var ch in text)
        {
            var pm = pattern.GetOrZero(ch)[0];
            var tr = ((~d0 & pm) << 1) & pmPrevious;
            d0 = (((pm & vp) + vp) ^ vp) | pm | vn;
            d0 |= tr;

            var hp = vn | ~(d0 | vp);
            var hn = d0 & vp;

            if ((hp & mask) != 0) distance++;
            if ((hn & mask) != 0) distance--;

            hp = (hp << 1) | 1;
            hn <<= 1;

            vp = hn | ~(d0 | hp);
            vn = hp & d0;
            pmPrevious = pm;
        }

        return distance;
    }

    /// <summary>Classic O(n·m) OSA table with three rolling rows.</summary>
    private static int DistanceTable<T>(ReadOnlySpan<T> s1, ReadOnlySpan<T> s2) where T : IEquatable<T>
    {
        var columns = s2.Length + 1;
        var rented = ArrayPool<int>.Shared.Rent(columns * 3);
        try
        {
            var twoBack = rented.AsSpan(0, columns);
            var previous = rented.AsSpan(columns, columns);
            var current = rented.AsSpan(columns * 2, columns);

            for (var j = 0; j < columns; j++)
            {
                previous[j] = j;
            }

            for (var i = 1; i <= s1.Length; i++)
            {
                current[0] = i;
                for (var j = 1; j < columns; j++)
                {
                    var cost = s1[i - 1].Equals(s2[j - 1]) ? 0 : 1;
                    var value = Math.Min(Math.Min(previous[j] + 1, current[j - 1] + 1), previous[j - 1] + cost);
                    if (i > 1 && j > 1 && s1[i - 1].Equals(s2[j - 2]) && s1[i - 2].Equals(s2[j - 1]))
                    {
                        value = Math.Min(value, twoBack[j - 2] + 1);
                    }

                    current[j] = value;
                }

                // Rotate rows: twoBack <- previous <- current.
                var spare = twoBack;
                twoBack = previous;
                previous = current;
                current = spare;
            }

            return previous[columns - 1];
        }
        finally
        {
            ArrayPool<int>.Shared.Return(rented);
        }
    }
}
