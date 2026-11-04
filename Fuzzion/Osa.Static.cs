using System;
using System.Buffers;
using Fuzzion.Utils;

namespace Fuzzion;

/// <summary>
/// Optimal String Alignment (OSA) distance, also called restricted Damerau-Levenshtein:
/// the minimum number of insertions, deletions, substitutions and swaps of two adjacent
/// elements, where no part of the input is edited more than once. So <c>("CA", "ABC")</c>
/// is 3 here but 2 for <see cref="DamerauLevenshtein"/>.
/// Ported from RapidFuzz (MIT, Max Bachmann): the bit-parallel algorithm of Hyyrö (2003),
/// on one 64-bit word when the shorter input fits, otherwise on several words with carries.
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

        using var pattern = PatternMatchVector.Create(s1);
        return s1.Length <= 64 ? DistanceHyrroe2003(pattern, s2) : DistanceHyrroe2003Block(pattern, s2);
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

    /// <summary>
    /// Multi-word version of <see cref="DistanceHyrroe2003{T}"/> for patterns longer than 64
    /// elements: the same bit operations per 64-bit word, with the horizontal carries and
    /// the transposition bit passed from one word to the next (rapidfuzz-cpp
    /// <c>osa_hyrroe2003_block</c>).
    /// </summary>
    internal static int DistanceHyrroe2003Block<T>(IPatternMatchVector<T> pattern, ReadOnlySpan<T> text)
        where T : notnull, IEquatable<T>
    {
        var words = pattern.Blocks;
        var last = 1UL << ((pattern.Length - 1) % 64);
        var distance = pattern.Length;

        // Index 0 is a fixed zero row standing in for "the word before word 0".
        var rented = ArrayPool<ulong>.Shared.Rent(8 * (words + 1));
        try
        {
            var buffer = rented.AsSpan(0, 8 * (words + 1));
            buffer.Clear();
            var oldVp = buffer.Slice(0, words + 1);
            var oldVn = buffer.Slice(words + 1, words + 1);
            var oldD0 = buffer.Slice(2 * (words + 1), words + 1);
            var oldPm = buffer.Slice(3 * (words + 1), words + 1);
            var newVp = buffer.Slice(4 * (words + 1), words + 1);
            var newVn = buffer.Slice(5 * (words + 1), words + 1);
            var newD0 = buffer.Slice(6 * (words + 1), words + 1);
            var newPm = buffer.Slice(7 * (words + 1), words + 1);
            oldVp.Fill(ulong.MaxValue);
            newVp.Fill(ulong.MaxValue);

            foreach (var ch in text)
            {
                var masks = pattern.GetOrZero(ch);
                var hpCarry = 1UL;
                var hnCarry = 0UL;

                for (var word = 0; word < words; word++)
                {
                    var vn = oldVn[word + 1];
                    var vp = oldVp[word + 1];
                    var d0 = oldD0[word + 1];
                    var d0Last = oldD0[word];
                    var pmOld = oldPm[word + 1];
                    var pmLast = newPm[word];

                    var pm = masks[word];
                    var x = pm;
                    var tr = ((((~d0) & x) << 1) | (((~d0Last) & pmLast) >> 63)) & pmOld;

                    x |= hnCarry;
                    d0 = (((x & vp) + vp) ^ vp) | x | vn | tr;

                    var hp = vn | ~(d0 | vp);
                    var hn = d0 & vp;

                    if (word == words - 1)
                    {
                        if ((hp & last) != 0) distance++;
                        if ((hn & last) != 0) distance--;
                    }

                    var hpCarryIn = hpCarry;
                    hpCarry = hp >> 63;
                    hp = (hp << 1) | hpCarryIn;
                    var hnCarryIn = hnCarry;
                    hnCarry = hn >> 63;
                    hn = (hn << 1) | hnCarryIn;

                    newVp[word + 1] = hn | ~(d0 | hp);
                    newVn[word + 1] = hp & d0;
                    newD0[word + 1] = d0;
                    newPm[word + 1] = pm;
                }

                // Swap old and new rows.
                Swap(ref oldVp, ref newVp);
                Swap(ref oldVn, ref newVn);
                Swap(ref oldD0, ref newD0);
                Swap(ref oldPm, ref newPm);
            }

            return distance;
        }
        finally
        {
            ArrayPool<ulong>.Shared.Return(rented);
        }
    }

    private static void Swap(ref Span<ulong> a, ref Span<ulong> b)
    {
        var t = a;
        a = b;
        b = t;
    }
}
