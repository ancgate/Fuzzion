using System;
using System.Buffers;
using System.Collections.Generic;
using Fuzzion.Utils;

namespace Fuzzion;

/// <summary>
/// Damerau-Levenshtein distance (unrestricted): the minimum number of insertions,
/// deletions, substitutions and swaps of two adjacent elements, where a swapped pair may
/// be edited again. So <c>("CA", "ABC")</c> is 2 here but 3 for <see cref="Osa"/>.
/// Ported from RapidFuzz (MIT, Max Bachmann), which uses the algorithm of Zhao et al.
/// (2019), "Damerau-Levenshtein distance in linear space".
/// </summary>
public sealed partial class DamerauLevenshtein
{
    /// <summary>Minimum number of Damerau-Levenshtein edits.</summary>
    public static int Distance(string s1, string s2) => Distance(s1.AsSpan(), s2.AsSpan());

    /// <inheritdoc cref="Distance(string, string)"/>
    public static int Distance<T>(ReadOnlySpan<T> s1, ReadOnlySpan<T> s2) where T : notnull, IEquatable<T>
    {
        // A shared start or end never changes the distance.
        SequenceUtils.TrimCommonAffix(ref s1, ref s2);

        if (s1.IsEmpty)
        {
            return s2.Length;
        }

        if (s2.IsEmpty)
        {
            return s1.Length;
        }

        return DistanceZhao(s1, s2);
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

    private static int DistanceZhao<T>(ReadOnlySpan<T> s1, ReadOnlySpan<T> s2) where T : notnull, IEquatable<T>
    {
        var maxValue = Math.Max(s1.Length, s2.Length) + 1;
        var size = s2.Length + 2;

        // Row of the last occurrence of each element of s1 seen so far.
        var lastRowId = new Dictionary<T, int>();

        var rented = ArrayPool<int>.Shared.Rent(size * 3);
        try
        {
            var fr = rented.AsSpan(0, size);
            var r1 = rented.AsSpan(size, size);
            var r = rented.AsSpan(size * 2, size);

            fr.Fill(maxValue);
            r1.Fill(maxValue);
            for (var j = 0; j < size; j++)
            {
                r[j] = j;
            }

            r[size - 1] = maxValue;

            // Columns are 1-based; index -1 in the paper maps to the last slot (size - 1).
            for (var i = 1; i <= s1.Length; i++)
            {
                var swap = r;
                r = r1;
                r1 = swap;

                var lastColId = -1;
                var lastI2L1 = r[0];
                r[0] = i;
                var t = maxValue;

                for (var j = 1; j <= s2.Length; j++)
                {
                    var equal = s1[i - 1].Equals(s2[j - 1]);
                    var diag = r1[j - 1] + (equal ? 0 : 1);
                    var left = r[j - 1] + 1;
                    var up = r1[j] + 1;
                    var temp = Math.Min(diag, Math.Min(left, up));

                    if (equal)
                    {
                        lastColId = j;
                        fr[j] = r1[j >= 2 ? j - 2 : size - 1];
                        t = lastI2L1;
                    }
                    else
                    {
                        var k = lastRowId.TryGetValue(s2[j - 1], out var row) ? row : -1;
                        var l = lastColId;

                        if (j - l == 1)
                        {
                            temp = Math.Min(temp, fr[j] + (i - k));
                        }
                        else if (i - k == 1)
                        {
                            temp = Math.Min(temp, t + (j - l));
                        }
                    }

                    lastI2L1 = r[j];
                    r[j] = temp;
                }

                lastRowId[s1[i - 1]] = i;
            }

            return r[s2.Length];
        }
        finally
        {
            ArrayPool<int>.Shared.Return(rented);
        }
    }
}
