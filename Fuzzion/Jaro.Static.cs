using System;
using System.Buffers;

namespace Fuzzion;

/// <summary>
/// Jaro similarity, from 0 (no match) to 1 (equal). Characters count as matching when
/// they are equal and no further apart than half the longer length minus one; the score
/// combines the share of matching characters in each input with the number of matches
/// that are out of order. Unlike edit distances, Jaro has no integer distance:
/// <c>Distance = 1 - Similarity</c>, and the normalized forms equal the plain ones.
/// Two empty inputs have similarity 1; exactly one empty input has similarity 0.
/// Ported from RapidFuzz (MIT, Max Bachmann). This is the scalar algorithm; the
/// bit-parallel version from rapidfuzz-cpp is a planned optimization.
/// </summary>
public sealed partial class Jaro
{
    /// <summary>Jaro similarity from 0 to 1.</summary>
    public static double Similarity(string s1, string s2) => Similarity(s1.AsSpan(), s2.AsSpan());

    /// <inheritdoc cref="Similarity(string, string)"/>
    public static double Similarity<T>(ReadOnlySpan<T> s1, ReadOnlySpan<T> s2) where T : IEquatable<T>
    {
        if (s1.IsEmpty && s2.IsEmpty)
        {
            return 1.0;
        }

        var patternLength = s1.Length;
        var textLength = s2.Length;
        if (patternLength == 0 || textLength == 0)
        {
            return 0.0;
        }

        if (patternLength == 1 && textLength == 1)
        {
            return s1[0].Equals(s2[0]) ? 1.0 : 0.0;
        }

        // Match window. Characters of the longer input beyond (shorter length + window)
        // can never match, so they are cut off before searching.
        int bound;
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

        bool[] rented1 = null, rented2 = null;
        Span<bool> flags1 = patternLength <= 256 ? stackalloc bool[patternLength] : (rented1 = ArrayPool<bool>.Shared.Rent(patternLength)).AsSpan(0, patternLength);
        Span<bool> flags2 = textLength <= 256 ? stackalloc bool[textLength] : (rented2 = ArrayPool<bool>.Shared.Rent(textLength)).AsSpan(0, textLength);
        flags1.Clear();
        flags2.Clear();

        try
        {
            var commonChars = 0;
            for (var i = 0; i < s1.Length; i++)
            {
                var low = Math.Max(0, i - bound);
                var high = Math.Min(i + bound, textLength - 1);
                for (var j = low; j <= high && j < s2.Length; j++)
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

                var j = k;
                while (!flags2[j])
                {
                    j++;
                }

                k = j + 1;
                if (!s1[i].Equals(s2[j]))
                {
                    transpositions++;
                }
            }

            return Calculate(patternLength, textLength, commonChars, transpositions);
        }
        finally
        {
            if (rented1 != null) ArrayPool<bool>.Shared.Return(rented1);
            if (rented2 != null) ArrayPool<bool>.Shared.Return(rented2);
        }
    }

    /// <summary><c>1 - Similarity</c>, from 0 (equal) to 1.</summary>
    public static double Distance(string s1, string s2) => 1.0 - Similarity(s1, s2);

    /// <inheritdoc cref="Distance(string, string)"/>
    public static double Distance<T>(ReadOnlySpan<T> s1, ReadOnlySpan<T> s2) where T : IEquatable<T> =>
        1.0 - Similarity(s1, s2);

    /// <summary>Same as <see cref="Similarity(string, string)"/> (Jaro is already normalized).</summary>
    public static double NormalizedSimilarity(string s1, string s2) => Similarity(s1, s2);

    /// <inheritdoc cref="NormalizedSimilarity(string, string)"/>
    public static double NormalizedSimilarity<T>(ReadOnlySpan<T> s1, ReadOnlySpan<T> s2) where T : IEquatable<T> =>
        Similarity(s1, s2);

    /// <summary>Same as <see cref="Distance(string, string)"/> (Jaro is already normalized).</summary>
    public static double NormalizedDistance(string s1, string s2) => Distance(s1, s2);

    /// <inheritdoc cref="NormalizedDistance(string, string)"/>
    public static double NormalizedDistance<T>(ReadOnlySpan<T> s1, ReadOnlySpan<T> s2) where T : IEquatable<T> =>
        Distance(s1, s2);

    private static double Calculate(int patternLength, int textLength, int commonChars, int transpositions)
    {
        transpositions /= 2;
        var similarity = commonChars / (double)patternLength
                         + commonChars / (double)textLength
                         + (commonChars - transpositions) / (double)commonChars;
        return similarity / 3.0;
    }
}
