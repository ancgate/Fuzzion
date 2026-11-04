using System;
using System.Collections.Generic;
using Fuzzion.PreProcess;
using Fuzzion.SimilarityRatio.Scorer;

namespace Fuzzion;

/// <summary>
/// Whole-character (Unicode code point) comparisons (plan item #31).
/// <para>
/// .NET strings are UTF-16, so characters outside the Basic Multilingual Plane (emoji,
/// rare CJK, historic scripts) are stored as two <c>char</c>s and every metric normally
/// sees them as two characters. Python, and so RapidFuzz, sees one. For example
/// <c>Fuzz.Ratio("😀abc", "😀abd")</c> is 80, but <c>CodePoints.Compare("😀abc", "😀abd", Fuzz.Ratio)</c>
/// is 75, as in RapidFuzz.
/// </para>
/// <para>
/// It works with any metric: for each comparison, every distinct character outside the
/// BMP is replaced by one unused private-use character (U+E000-U+F8FF), so the metric sees
/// one character per code point. Replacements keep the code point order, so word-sorting
/// scorers order words as RapidFuzz does (except relative to BMP characters above U+F8FF,
/// which sort after the replacements instead of before). Positions in the results
/// (alignments, edit operations) count code points. Strings without such characters are
/// passed through as is.
/// </para>
/// </summary>
public static class CodePoints
{
    private const char PrivateUseFirst = '';
    private const char PrivateUseLast = '';

    /// <summary>
    /// Runs <paramref name="metric"/> on <paramref name="a"/> and <paramref name="b"/>,
    /// counting each Unicode character once, for example
    /// <c>CodePoints.Compare(a, b, Fuzz.Ratio)</c> or
    /// <c>CodePoints.Compare(a, b, (x, y) => Levenshtein.Distance(x, y))</c>.
    /// </summary>
    /// <exception cref="ArgumentException">The two strings together contain more than 6,400 distinct characters outside the BMP.</exception>
    public static TResult Compare<TResult>(string a, string b, Func<string, string, TResult> metric)
    {
        if (metric == null) throw new ArgumentNullException(nameof(metric));
        var (mappedA, mappedB) = Map(a, b);
        return metric(mappedA, mappedB);
    }

    /// <summary>A scorer that compares whole characters, for use with <see cref="Process"/> and <see cref="Process.Cdist"/>.</summary>
    public static IRatioScorer Scorer(IRatioScorer inner) =>
        new CodePointScorer(inner ?? throw new ArgumentNullException(nameof(inner)));

    /// <summary>A distance scorer that compares whole characters, keeping the inner scorer's maximum distance.</summary>
    public static IDistanceScorer Scorer(IDistanceScorer inner) =>
        new CodePointDistanceScorer(inner ?? throw new ArgumentNullException(nameof(inner)));

    /// <summary>The number of Unicode characters (code points) in <paramref name="s"/>.</summary>
    public static int Count(string s)
    {
        if (s == null) throw new ArgumentNullException(nameof(s));
        var count = 0;
        for (var i = 0; i < s.Length; i++)
        {
            if (char.IsSurrogatePair(s, i)) i++;
            count++;
        }

        return count;
    }

    /// <summary>The code points of <paramref name="s"/>; a lone surrogate is returned as its own value.</summary>
    public static int[] Of(string s)
    {
        if (s == null) throw new ArgumentNullException(nameof(s));
        var result = new int[Count(s)];
        var k = 0;
        for (var i = 0; i < s.Length; i++)
        {
            if (char.IsSurrogatePair(s, i))
            {
                result[k++] = char.ConvertToUtf32(s[i], s[i + 1]);
                i++;
            }
            else
            {
                result[k++] = s[i];
            }
        }

        return result;
    }

    internal static (string A, string B) Map(string a, string b)
    {
        if (a == null) throw new ArgumentNullException(nameof(a));
        if (b == null) throw new ArgumentNullException(nameof(b));
        if (!HasSurrogatePair(a) && !HasSurrogatePair(b))
        {
            return (a, b);
        }

        // Private-use characters already present must not be reused.
        var used = new HashSet<char>();
        foreach (var c in a) if (c is >= PrivateUseFirst and <= PrivateUseLast) used.Add(c);
        foreach (var c in b) if (c is >= PrivateUseFirst and <= PrivateUseLast) used.Add(c);

        // Assign replacements in ascending code point order, so sorting (token scorers sort
        // words ordinally) keeps the same relative order as in Python.
        var distinct = new SortedSet<int>();
        CollectCodePoints(a, distinct);
        CollectCodePoints(b, distinct);

        var mapping = new Dictionary<int, char>();
        var next = PrivateUseFirst;
        foreach (var codePoint in distinct)
        {
            while (next <= PrivateUseLast && used.Contains(next)) next++;
            if (next > PrivateUseLast)
            {
                throw new ArgumentException("Too many distinct characters outside the Basic Multilingual Plane to compare by code point (at most 6,400).");
            }

            mapping[codePoint] = next++;
        }

        return (Replace(a), Replace(b));

        string Replace(string s)
        {
            if (!HasSurrogatePair(s))
            {
                return s;
            }

            var chars = new char[s.Length];
            var k = 0;
            for (var i = 0; i < s.Length; i++)
            {
                if (!char.IsSurrogatePair(s, i))
                {
                    chars[k++] = s[i];
                    continue;
                }

                chars[k++] = mapping[char.ConvertToUtf32(s[i], s[i + 1])];
                i++;
            }

            return new string(chars, 0, k);
        }
    }

    private static void CollectCodePoints(string s, SortedSet<int> into)
    {
        for (var i = 0; i < s.Length; i++)
        {
            if (char.IsSurrogatePair(s, i))
            {
                into.Add(char.ConvertToUtf32(s[i], s[i + 1]));
                i++;
            }
        }
    }

    private static bool HasSurrogatePair(string s)
    {
        for (var i = 0; i < s.Length - 1; i++)
        {
            if (char.IsHighSurrogate(s[i]) && char.IsLowSurrogate(s[i + 1])) return true;
        }

        return false;
    }

    private class CodePointScorer(IRatioScorer inner) : IRatioScorer
    {
        public double Score(string input1, string input2) => Compare(input1, input2, inner.Score);

        public double Score(string input1, string input2, Func<string, string> preprocessor)
        {
            preprocessor ??= StringPreprocessor.Full;
            return Compare(preprocessor(input1), preprocessor(input2), inner.Score);
        }
    }

    private sealed class CodePointDistanceScorer(IDistanceScorer inner) : CodePointScorer(inner), IDistanceScorer
    {
        public int? MaxDistance => inner.MaxDistance;
    }
}
