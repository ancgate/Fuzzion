using System;
using System.Buffers;
using Fuzzion.Utils;

namespace Fuzzion;

/// <summary>
/// Jaro similarity, from 0 (no match) to 1 (equal). Characters count as matching when
/// they are equal and no further apart than half the longer length minus one; the score
/// combines the share of matching characters in each input with the number of matches
/// that are out of order. Unlike edit distances, Jaro has no integer distance:
/// <c>Distance = 1 - Similarity</c>, and the normalized forms equal the plain ones.
/// Two empty inputs have similarity 1; exactly one empty input has similarity 0.
/// Ported from RapidFuzz (MIT, Max Bachmann): short inputs use the textbook algorithm,
/// longer ones a bit-parallel search on one or several 64-bit words.
/// </summary>
public sealed partial class Jaro
{
    /// <summary>Inputs up to this length use the plain algorithm; it beats building bit masks.</summary>
    internal const int ScalarLimit = 16;

    /// <summary>Jaro similarity from 0 to 1.</summary>
    public static double Similarity(string s1, string s2) => Similarity(s1.AsSpan(), s2.AsSpan());

    /// <inheritdoc cref="Similarity(string, string)"/>
    public static double Similarity<T>(ReadOnlySpan<T> s1, ReadOnlySpan<T> s2) where T : notnull, IEquatable<T>
    {
        if (!Prepare(ref s1, ref s2, out var bound, out var patternLength, out var textLength, out var result))
        {
            return result;
        }

        if (Math.Max(s1.Length, s2.Length) <= ScalarLimit)
        {
            return Scalar(s1, s2, bound, patternLength, textLength);
        }

        // Shared start: every character matches its partner and none is out of order.
        var prefix = SequenceUtils.CommonPrefix(s1, s2);
        if (prefix == s1.Length || prefix == s2.Length)
        {
            return Calculate(patternLength, textLength, prefix, 0);
        }

        var p = s1.Slice(prefix);
        using var pattern = PatternMatchVector.Create(p);
        return Bits(pattern, p, s2.Slice(prefix), bound, patternLength, textLength, prefix);
    }

    /// <summary>
    /// Similarity using a pattern built once from the whole first input (instance classes).
    /// The shared-prefix shortcut is skipped because it would shift the pattern; the result
    /// is the same.
    /// </summary>
    internal static double SimilarityWithPattern<T>(IPatternMatchVector<T> pattern, ReadOnlySpan<T> s1, ReadOnlySpan<T> s2)
        where T : notnull, IEquatable<T>
    {
        if (!Prepare(ref s1, ref s2, out var bound, out var patternLength, out var textLength, out var result))
        {
            return result;
        }

        return pattern == null || Math.Max(s1.Length, s2.Length) <= ScalarLimit
            ? Scalar(s1, s2, bound, patternLength, textLength)
            : Bits(pattern, s1, s2, bound, patternLength, textLength, 0);
    }

    /// <summary>
    /// Handles empty and single-character inputs and cuts off the parts of the longer input
    /// that are out of reach of the match window. Returns false with the final
    /// <paramref name="result"/> when no search is needed.
    /// </summary>
    private static bool Prepare<T>(ref ReadOnlySpan<T> s1, ref ReadOnlySpan<T> s2, out int bound, out int patternLength, out int textLength, out double result)
        where T : notnull, IEquatable<T>
    {
        patternLength = s1.Length;
        textLength = s2.Length;
        bound = 0;
        result = 0;

        if (patternLength == 0 && textLength == 0)
        {
            result = 1.0;
            return false;
        }

        if (patternLength == 0 || textLength == 0)
        {
            return false;
        }

        if (patternLength == 1 && textLength == 1)
        {
            result = s1[0].Equals(s2[0]) ? 1.0 : 0.0;
            return false;
        }

        if (textLength > patternLength)
        {
            bound = textLength / 2 - 1;
            if (textLength > patternLength + bound)
            {
                s2 = s2.Slice(0, patternLength + bound);
            }
        }
        else
        {
            bound = patternLength / 2 - 1;
            if (patternLength > textLength + bound)
            {
                s1 = s1.Slice(0, textLength + bound);
            }
        }

        return true;
    }

    /// <summary>The textbook algorithm with match flags; fastest for short inputs.</summary>
    private static double Scalar<T>(ReadOnlySpan<T> s1, ReadOnlySpan<T> s2, int bound, int patternLength, int textLength)
        where T : notnull, IEquatable<T>
    {
        Span<bool> flags1 = stackalloc bool[s1.Length <= 64 ? s1.Length : 0];
        Span<bool> flags2 = stackalloc bool[s2.Length <= 64 ? s2.Length : 0];
        bool[] rented1 = null, rented2 = null;
        if (s1.Length > 64) flags1 = (rented1 = ArrayPool<bool>.Shared.Rent(s1.Length)).AsSpan(0, s1.Length);
        if (s2.Length > 64) flags2 = (rented2 = ArrayPool<bool>.Shared.Rent(s2.Length)).AsSpan(0, s2.Length);
        flags1.Clear();
        flags2.Clear();

        try
        {
            var commonChars = 0;
            for (var i = 0; i < s1.Length; i++)
            {
                var low = Math.Max(0, i - bound);
                var high = Math.Min(i + bound, s2.Length - 1);
                for (var j = low; j <= high; j++)
                {
                    if (!flags2[j] && s2[j].Equals(s1[i]))
                    {
                        flags1[i] = flags2[j] = true;
                        commonChars++;
                        break;
                    }
                }
            }

            if (commonChars == 0)
            {
                return 0.0;
            }

            // Count matched characters that appear in a different order.
            var transpositions = 0;
            var k = 0;
            for (var i = 0; i < s1.Length; i++)
            {
                if (!flags1[i])
                {
                    continue;
                }

                while (!flags2[k])
                {
                    k++;
                }

                if (!s1[i].Equals(s2[k]))
                {
                    transpositions++;
                }

                k++;
            }

            return Calculate(patternLength, textLength, commonChars, transpositions);
        }
        finally
        {
            if (rented1 != null) ArrayPool<bool>.Shared.Return(rented1);
            if (rented2 != null) ArrayPool<bool>.Shared.Return(rented2);
        }
    }

    /// <summary>
    /// Bit-parallel Jaro. <paramref name="pattern"/> holds the bit masks of <paramref name="p"/>
    /// (it may be longer: bits past <c>p.Length</c> are ignored). <paramref name="commonChars"/>
    /// are matches already counted (the shared prefix).
    /// </summary>
    private static double Bits<T>(IPatternMatchVector<T> pattern, ReadOnlySpan<T> p, ReadOnlySpan<T> t, int bound, int patternLength, int textLength, int commonChars)
        where T : notnull, IEquatable<T> =>
        p.Length <= 64 && t.Length <= 64
            ? SimilarityWord(pattern, p.Length, t, bound, patternLength, textLength, commonChars)
            : SimilarityBlock(pattern, p, t, bound, patternLength, textLength, commonChars);

    /// <summary><c>1 - Similarity</c>, from 0 (equal) to 1.</summary>
    public static double Distance(string s1, string s2) => 1.0 - Similarity(s1, s2);

    /// <inheritdoc cref="Distance(string, string)"/>
    public static double Distance<T>(ReadOnlySpan<T> s1, ReadOnlySpan<T> s2) where T : notnull, IEquatable<T> =>
        1.0 - Similarity(s1, s2);

    /// <summary>Same as <see cref="Similarity(string, string)"/> (Jaro is already normalized).</summary>
    public static double NormalizedSimilarity(string s1, string s2) => Similarity(s1, s2);

    /// <inheritdoc cref="NormalizedSimilarity(string, string)"/>
    public static double NormalizedSimilarity<T>(ReadOnlySpan<T> s1, ReadOnlySpan<T> s2) where T : notnull, IEquatable<T> =>
        Similarity(s1, s2);

    /// <summary>Same as <see cref="Distance(string, string)"/> (Jaro is already normalized).</summary>
    public static double NormalizedDistance(string s1, string s2) => Distance(s1, s2);

    /// <inheritdoc cref="NormalizedDistance(string, string)"/>
    public static double NormalizedDistance<T>(ReadOnlySpan<T> s1, ReadOnlySpan<T> s2) where T : notnull, IEquatable<T> =>
        Distance(s1, s2);

    /// <summary>
    /// Bit-parallel Jaro for inputs of at most 64 elements (rapidfuzz-cpp
    /// <c>flag_similar_characters_word</c> and <c>count_transpositions_word</c>): for each
    /// character of <paramref name="t"/>, the lowest unmatched equal character of the first
    /// input (bit masks in <paramref name="pm"/>, length <paramref name="pLength"/>) inside
    /// the window is found with bit masks.
    /// </summary>
    private static double SimilarityWord<T>(IPatternMatchVector<T> pm, int pLength, ReadOnlySpan<T> t, int bound, int patternLength, int textLength, int commonChars)
        where T : notnull, IEquatable<T>
    {
        ulong pFlag = 0, tFlag = 0;
        var boundMask = LowBits(bound + 1) & LowBits(pLength);

        var j = 0;
        for (; j < Math.Min(bound, t.Length); j++)
        {
            var pmj = pm.GetOrZero(t[j])[0] & boundMask & ~pFlag;
            pFlag |= LowestBit(pmj);
            tFlag |= (pmj != 0 ? 1UL : 0UL) << j;
            boundMask = ((boundMask << 1) | 1) & LowBits(pLength);
        }

        for (; j < t.Length; j++)
        {
            var pmj = pm.GetOrZero(t[j])[0] & boundMask & ~pFlag;
            pFlag |= LowestBit(pmj);
            tFlag |= (pmj != 0 ? 1UL : 0UL) << j;
            boundMask = (boundMask << 1) & LowBits(pLength);
        }

        var flagged = Utils.Polyfill.PopCount(pFlag);
        if (commonChars + flagged == 0)
        {
            return 0.0;
        }

        // Matched characters of t, in order, against matched characters of p, in order.
        var transpositions = 0;
        while (tFlag != 0)
        {
            var patternFlagMask = LowestBit(pFlag);
            if ((pm.GetOrZero(t[Utils.Polyfill.TrailingZeroCount(tFlag)])[0] & patternFlagMask) == 0)
            {
                transpositions++;
            }

            tFlag &= tFlag - 1;
            pFlag ^= patternFlagMask;
        }

        return Calculate(patternLength, textLength, commonChars + flagged, transpositions);
    }

    /// <summary>
    /// Multi-word version of <see cref="SimilarityWord{T}"/> for inputs longer than 64: for
    /// each character of <paramref name="t"/>, the lowest unmatched equal character of
    /// <paramref name="p"/> inside the window <c>[j - bound, j + bound]</c> is found by
    /// scanning only the 64-bit words the window covers.
    /// </summary>
    private static double SimilarityBlock<T>(IPatternMatchVector<T> pm, ReadOnlySpan<T> p, ReadOnlySpan<T> t, int bound, int patternLength, int textLength, int commonChars)
        where T : notnull, IEquatable<T>
    {
        var pWords = (p.Length + 63) >> 6;
        var tWords = (t.Length + 63) >> 6;
        var rented = ArrayPool<ulong>.Shared.Rent(pWords + tWords);
        try
        {
            var pFlag = rented.AsSpan(0, pWords);
            var tFlag = rented.AsSpan(pWords, tWords);
            pFlag.Clear();
            tFlag.Clear();
            var flagged = 0;

            for (var j = 0; j < t.Length; j++)
            {
                var low = Math.Max(0, j - bound);
                var high = Math.Min(p.Length - 1, j + bound);
                if (low > high)
                {
                    continue;
                }

                var masks = pm.GetOrZero(t[j]);
                var lowWord = low >> 6;
                var highWord = high >> 6;
                for (var word = lowWord; word <= highWord; word++)
                {
                    var candidates = masks[word] & ~pFlag[word];
                    if (word == lowWord) candidates &= ulong.MaxValue << (low & 63);
                    if (word == highWord) candidates &= LowBits((high & 63) + 1);
                    if (candidates != 0)
                    {
                        pFlag[word] |= LowestBit(candidates);
                        tFlag[j >> 6] |= 1UL << (j & 63);
                        flagged++;
                        break;
                    }
                }
            }

            if (commonChars + flagged == 0)
            {
                return 0.0;
            }

            // Pair the matched characters of t and p in order and count the mismatches.
            var transpositions = 0;
            var pWord = 0;
            var pBits = pFlag.Length > 0 ? pFlag[0] : 0;
            for (var tWord = 0; tWord < tWords; tWord++)
            {
                var tBits = tFlag[tWord];
                while (tBits != 0)
                {
                    while (pBits == 0)
                    {
                        pBits = pFlag[++pWord];
                    }

                    var pPosition = (pWord << 6) + Polyfill.TrailingZeroCount(pBits);
                    var tPosition = (tWord << 6) + Polyfill.TrailingZeroCount(tBits);
                    if (!p[pPosition].Equals(t[tPosition]))
                    {
                        transpositions++;
                    }

                    pBits &= pBits - 1;
                    tBits &= tBits - 1;
                }
            }

            return Calculate(patternLength, textLength, commonChars + flagged, transpositions);
        }
        finally
        {
            ArrayPool<ulong>.Shared.Return(rented);
        }
    }

    private static ulong LowBits(int n) => n >= 64 ? ulong.MaxValue : (1UL << n) - 1;

    private static ulong LowestBit(ulong x) => x & (0UL - x);

    private static double Calculate(int patternLength, int textLength, int commonChars, int transpositions)
    {
        transpositions /= 2;
        var similarity = commonChars / (double)patternLength
                         + commonChars / (double)textLength
                         + (commonChars - transpositions) / (double)commonChars;
        return similarity / 3.0;
    }
}
