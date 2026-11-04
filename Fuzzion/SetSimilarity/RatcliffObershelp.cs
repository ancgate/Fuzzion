using System;
using System.Collections.Generic;
using Fuzzion.Edits;

namespace Fuzzion;

/// <summary>
/// Ratcliff-Obershelp similarity ("gestalt pattern matching"): find the longest common
/// block, repeat on the parts to its left and right, and score
/// <c>2 · matched characters / (len1 + len2)</c>, from 0 to 1. This is what Python's
/// <c>difflib.SequenceMatcher(None, a, b).ratio()</c> computes; Fuzzion's results match it.
/// Written from the published algorithm (Ratcliff and Metzener, 1988) and difflib's
/// documented behavior.
/// </summary>
public static class RatcliffObershelp
{
    /// <summary>Strings at least this long get difflib's "popular character" rule when <c>autoJunk</c> is on.</summary>
    public const int AutoJunkMinLength = 200;

    /// <summary>Ratcliff-Obershelp similarity from 0 to 1. Two empty strings give 1.</summary>
    /// <param name="a">The first string.</param>
    /// <param name="b">The second string.</param>
    /// <param name="autoJunk">
    /// difflib's automatic junk rule: when <paramref name="b"/> has at least 200 characters,
    /// characters that make up more than 1% of it (plus one) cannot start a match. Off by
    /// default so results do not change at 200 characters; turn it on to match difflib's
    /// default <c>SequenceMatcher(None, a, b)</c>.
    /// </param>
    public static double Similarity(string a, string b, bool autoJunk = false)
    {
        if (a == null) throw new ArgumentNullException(nameof(a));
        if (b == null) throw new ArgumentNullException(nameof(b));

        var total = a.Length + b.Length;
        if (total == 0)
        {
            return 1.0;
        }

        var matched = 0;
        foreach (var block in MatchingBlocks(a, b, autoJunk))
        {
            matched += block.Length;
        }

        return 2.0 * matched / total;
    }

    /// <summary><c>1 - Similarity</c>.</summary>
    public static double Distance(string a, string b, bool autoJunk = false) => 1.0 - Similarity(a, b, autoJunk);

    /// <summary>
    /// The matching blocks, in order, with adjacent blocks merged (difflib's
    /// <c>get_matching_blocks()</c> without its final zero-length sentinel).
    /// </summary>
    public static List<MatchingBlock> MatchingBlocks(string a, string b, bool autoJunk = false)
    {
        if (a == null) throw new ArgumentNullException(nameof(a));
        if (b == null) throw new ArgumentNullException(nameof(b));

        var popular = autoJunk ? PopularCharacters(b) : null;
        var found = new List<MatchingBlock>();
        var pending = new Stack<(int ALo, int AHi, int BLo, int BHi)>();
        pending.Push((0, a.Length, 0, b.Length));

        while (pending.Count > 0)
        {
            var (aLo, aHi, bLo, bHi) = pending.Pop();
            var (i, j, k) = LongestBlock.Find(a, aLo, aHi, b, bLo, bHi, popular);
            if (k == 0)
            {
                continue;
            }

            found.Add(new MatchingBlock { SourcePos = i, DestPos = j, Length = k });
            if (aLo < i && bLo < j) pending.Push((aLo, i, bLo, j));
            if (i + k < aHi && j + k < bHi) pending.Push((i + k, aHi, j + k, bHi));
        }

        found.Sort((x, y) => x.SourcePos != y.SourcePos ? x.SourcePos.CompareTo(y.SourcePos) : x.DestPos.CompareTo(y.DestPos));

        // Merge blocks that touch, as difflib does.
        var merged = new List<MatchingBlock>(found.Count);
        foreach (var block in found)
        {
            if (merged.Count > 0)
            {
                var last = merged[merged.Count - 1];
                if (last.SourcePos + last.Length == block.SourcePos && last.DestPos + last.Length == block.DestPos)
                {
                    last.Length += block.Length;
                    continue;
                }
            }

            merged.Add(new MatchingBlock { SourcePos = block.SourcePos, DestPos = block.DestPos, Length = block.Length });
        }

        return merged;
    }

    /// <summary>Characters that make up more than 1% (+1) of <paramref name="b"/>, when it has 200+ characters.</summary>
    private static HashSet<char> PopularCharacters(string b)
    {
        if (b.Length < AutoJunkMinLength)
        {
            return null;
        }

        var counts = new Dictionary<char, int>();
        foreach (var c in b)
        {
            counts.TryGetValue(c, out var n);
            counts[c] = n + 1;
        }

        var limit = b.Length / 100 + 1;
        var popular = new HashSet<char>();
        foreach (var pair in counts)
        {
            if (pair.Value > limit)
            {
                popular.Add(pair.Key);
            }
        }

        return popular.Count == 0 ? null : popular;
    }
}

/// <summary>A common substring of two strings: <c>a[SourceStart..SourceStart+Length) == b[TargetStart..TargetStart+Length)</c>.</summary>
public readonly struct CommonSubstring : IEquatable<CommonSubstring>
{
    /// <summary>Creates a common substring description.</summary>
    public CommonSubstring(int sourceStart, int targetStart, int length)
    {
        SourceStart = sourceStart;
        TargetStart = targetStart;
        Length = length;
    }

    /// <summary>Start in the first string.</summary>
    public int SourceStart { get; }

    /// <summary>Start in the second string.</summary>
    public int TargetStart { get; }

    /// <summary>Number of characters.</summary>
    public int Length { get; }

    /// <inheritdoc/>
    public bool Equals(CommonSubstring other) =>
        SourceStart == other.SourceStart && TargetStart == other.TargetStart && Length == other.Length;

    /// <inheritdoc/>
    public override bool Equals(object obj) => obj is CommonSubstring other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => unchecked((SourceStart * 397) ^ (TargetStart * 31) ^ Length);

    /// <summary>Equality.</summary>
    public static bool operator ==(CommonSubstring left, CommonSubstring right) => left.Equals(right);

    /// <summary>Inequality.</summary>
    public static bool operator !=(CommonSubstring left, CommonSubstring right) => !left.Equals(right);

    /// <inheritdoc/>
    public override string ToString() => $"CommonSubstring(SourceStart={SourceStart}, TargetStart={TargetStart}, Length={Length})";
}

/// <summary>
/// The longest run of consecutive equal characters shared by two strings (unlike
/// <see cref="LongestCommonSubsequence"/>, which allows gaps). On ties, the earliest
/// position in the first string wins, then the earliest in the second (as difflib's
/// <c>find_longest_match</c>).
/// </summary>
public static class LongestCommonSubstring
{
    /// <summary>Position and length of the longest common substring; length 0 when there is none.</summary>
    public static CommonSubstring Find(string a, string b)
    {
        if (a == null) throw new ArgumentNullException(nameof(a));
        if (b == null) throw new ArgumentNullException(nameof(b));

        var (i, j, k) = LongestBlock.Find(a, 0, a.Length, b, 0, b.Length, popular: null);
        return k == 0 ? new CommonSubstring(0, 0, 0) : new CommonSubstring(i, j, k);
    }

    /// <summary>The longest common substring itself ("" when there is none).</summary>
    public static string Of(string a, string b)
    {
        var found = Find(a, b);
        return a.Substring(found.SourceStart, found.Length);
    }

    /// <summary>Length of the longest common substring.</summary>
    public static int Similarity(string a, string b) => Find(a, b).Length;

    /// <summary>Length divided by the longer string's length, from 0 to 1. Two empty strings give 1.</summary>
    public static double NormalizedSimilarity(string a, string b)
    {
        var maximum = Math.Max(a?.Length ?? 0, b?.Length ?? 0);
        return maximum == 0 ? 1.0 : Similarity(a, b) / (double)maximum;
    }

    /// <summary><c>1 - NormalizedSimilarity</c>.</summary>
    public static double NormalizedDistance(string a, string b) => 1.0 - NormalizedSimilarity(a, b);
}

/// <summary>Longest common block search shared by Ratcliff-Obershelp and longest common substring.</summary>
internal static class LongestBlock
{
    /// <summary>
    /// The longest block <c>a[i..i+k) == b[j..j+k)</c> inside the given ranges. Characters in
    /// <paramref name="popular"/> cannot be part of the core match, but a found match is then
    /// extended over equal characters on both sides (difflib's behavior). Ties: smallest i,
    /// then smallest j. Time O(range lengths product), memory O(b range).
    /// </summary>
    public static (int I, int J, int K) Find(string a, int aLo, int aHi, string b, int bLo, int bHi, HashSet<char> popular)
    {
        int bestI = aLo, bestJ = bLo, bestK = 0;
        var width = bHi - bLo;
        if (aHi <= aLo || width <= 0)
        {
            return (bestI, bestJ, 0);
        }

        // previous[x] / current[x]: length of the common run ending at a[i-1]/a[i] and b[bLo + x - 1].
        var previous = new int[width + 1];
        var current = new int[width + 1];

        for (var i = aLo; i < aHi; i++)
        {
            var ch = a[i];
            var usable = popular == null || !popular.Contains(ch);
            for (var x = 1; x <= width; x++)
            {
                if (usable && b[bLo + x - 1] == ch)
                {
                    var k = previous[x - 1] + 1;
                    current[x] = k;
                    if (k > bestK)
                    {
                        bestK = k;
                        bestI = i - k + 1;
                        bestJ = bLo + x - k;
                    }
                }
                else
                {
                    current[x] = 0;
                }
            }

            (previous, current) = (current, previous);
        }

        if (popular != null)
        {
            // Extend over equal characters (popular ones included) on both sides.
            while (bestI > aLo && bestJ > bLo && a[bestI - 1] == b[bestJ - 1])
            {
                bestI--;
                bestJ--;
                bestK++;
            }

            while (bestI + bestK < aHi && bestJ + bestK < bHi && a[bestI + bestK] == b[bestJ + bestK])
            {
                bestK++;
            }
        }

        return (bestI, bestJ, bestK);
    }
}
