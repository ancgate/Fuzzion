using System;

namespace Fuzzion;

// String overloads for Indel and LongestCommonSubsequence, which previously only accepted
// ReadOnlySpan<T> (so Indel.Distance("a", "b") did not compile, unlike every other metric).
public sealed partial class Indel
{
    /// <summary>Indel distance: insertions plus deletions needed (a replacement counts as 2).</summary>
    public static int Distance(string s1, string s2) => Distance(s1.AsSpan(), s2.AsSpan());

    /// <summary>Indel similarity: <c>len1 + len2 - Distance</c> (twice the longest common subsequence).</summary>
    public static int Similarity(string s1, string s2) => Similarity(s1.AsSpan(), s2.AsSpan());

    /// <summary>Distance divided by <c>len1 + len2</c>, from 0 (equal) to 1.</summary>
    public static double NormalizedDistance(string s1, string s2) => NormalizedDistance(s1.AsSpan(), s2.AsSpan());

    /// <summary><c>1 - NormalizedDistance</c>; <c>Fuzz.Ratio</c> is this times 100.</summary>
    public static double NormalizedSimilarity(string s1, string s2) => NormalizedSimilarity(s1.AsSpan(), s2.AsSpan());
}

public sealed partial class LongestCommonSubsequence
{
    /// <summary><c>max(len1, len2) - Similarity</c>.</summary>
    public static int Distance(string s1, string s2) => Distance(s1.AsSpan(), s2.AsSpan());

    /// <summary>Length of the longest common subsequence.</summary>
    public static int Similarity(string s1, string s2) => Similarity(s1.AsSpan(), s2.AsSpan());

    /// <summary>Distance divided by the longer length, from 0 (equal) to 1.</summary>
    public static double NormalizedDistance(string s1, string s2) => NormalizedDistance(s1.AsSpan(), s2.AsSpan());

    /// <summary><c>1 - NormalizedDistance</c>.</summary>
    public static double NormalizedSimilarity(string s1, string s2) => NormalizedSimilarity(s1.AsSpan(), s2.AsSpan());
}
