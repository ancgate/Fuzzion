using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
    public static int Distance(string s1, string s2)
    {
        var a = s1.AsSpan();
        var b = s2.AsSpan();
        SequenceUtils.TrimCommonAffix(ref a, ref b);

        if (a.IsEmpty) return b.Length;
        if (b.IsEmpty) return a.Length;

        // Strings use an array for characters below 256 instead of a dictionary lookup per cell.
        var lastRow = new CharLastRow();
        return DistanceZhao(a, b, ref lastRow);
    }

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

        var lastRow = new GenericLastRow<T>();
        return DistanceZhao(s1, s2, ref lastRow);
    }

    /// <summary><c>max(len1, len2) - Distance</c>.</summary>
    public static int Similarity(string s1, string s2) => Math.Max(s1.Length, s2.Length) - Distance(s1, s2);

    /// <inheritdoc cref="Similarity(string, string)"/>
    public static int Similarity<T>(ReadOnlySpan<T> s1, ReadOnlySpan<T> s2) where T : notnull, IEquatable<T> =>
        Math.Max(s1.Length, s2.Length) - Distance(s1, s2);

    /// <summary>Distance divided by the longer length, from 0 (equal) to 1.</summary>
    public static double NormalizedDistance(string s1, string s2) => DistanceMath.Normalize(Distance(s1, s2), Math.Max(s1.Length, s2.Length));

    /// <inheritdoc cref="NormalizedDistance(string, string)"/>
    public static double NormalizedDistance<T>(ReadOnlySpan<T> s1, ReadOnlySpan<T> s2) where T : notnull, IEquatable<T> =>
        DistanceMath.Normalize(Distance(s1, s2), Math.Max(s1.Length, s2.Length));

    /// <summary><c>1 - NormalizedDistance</c>, from 0 to 1 (equal).</summary>
    public static double NormalizedSimilarity(string s1, string s2) => 1.0 - NormalizedDistance(s1, s2);

    /// <inheritdoc cref="NormalizedSimilarity(string, string)"/>
    public static double NormalizedSimilarity<T>(ReadOnlySpan<T> s1, ReadOnlySpan<T> s2) where T : notnull, IEquatable<T> =>
        1.0 - NormalizedDistance(s1, s2);

    /// <summary>Row of the last occurrence of each element of s1 seen so far (-1 when none).</summary>
    private interface ILastRow<in T>
    {
        int Get(T key);
        void Set(T key, int row);
    }

    private struct GenericLastRow<T> : ILastRow<T> where T : notnull
    {
        private readonly Dictionary<T, int> _rows;

        public GenericLastRow() => _rows = new Dictionary<T, int>();

        public int Get(T key) => _rows.TryGetValue(key, out var row) ? row : -1;

        public void Set(T key, int row) => _rows[key] = row;
    }

    private struct CharLastRow : ILastRow<char>
    {
        // One table per thread, reset on each use, so no allocation per call.
        [ThreadStatic] private static int[] _sharedLatin1;

        private readonly int[] _latin1;
        private Dictionary<char, int> _other;

        public CharLastRow()
        {
            _latin1 = _sharedLatin1 ??= new int[256];
            _latin1.AsSpan().Fill(-1);
            _other = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Get(char key) =>
            key < 256 ? _latin1[key] : _other != null && _other.TryGetValue(key, out var row) ? row : -1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(char key, int row)
        {
            if (key < 256)
            {
                _latin1[key] = row;
            }
            else
            {
                (_other ??= new Dictionary<char, int>())[key] = row;
            }
        }
    }

    private static int DistanceZhao<T, TLastRow>(ReadOnlySpan<T> s1, ReadOnlySpan<T> s2, ref TLastRow lastRowId)
        where T : notnull, IEquatable<T>
        where TLastRow : struct, ILastRow<T>
    {
        var maxValue = Math.Max(s1.Length, s2.Length) + 1;
        var size = s2.Length + 2;

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
                var current = s1[i - 1];

                for (var j = 1; j <= s2.Length; j++)
                {
                    var equal = current.Equals(s2[j - 1]);
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
                        var k = lastRowId.Get(s2[j - 1]);
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

                lastRowId.Set(current, i);
            }

            return r[s2.Length];
        }
        finally
        {
            ArrayPool<int>.Shared.Return(rented);
        }
    }
}
