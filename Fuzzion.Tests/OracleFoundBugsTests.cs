using System;
using Xunit;

namespace Fuzzion.Tests;

/// <summary>
/// Regression tests for bugs inherited from Raffinert.FuzzySharp v6.0.1 that the
/// RapidFuzz oracle found. Expected values are from Python rapidfuzz 3.14.6.
/// </summary>
public class OracleFoundBugsTests
{
    // Bug: Indel.NormalizedDistance trimmed the common prefix/suffix before dividing,
    // so it divided by the trimmed lengths instead of the full lengths.
    [Theory]
    [InlineData("abc", "abcd", 1.0 / 7)]
    [InlineData("testing", "resting", 2.0 / 14)]
    [InlineData("karolin", "kathrin", 4.0 / 14)]
    [InlineData("MARTHA", "MARHTA", 2.0 / 12)]
    [InlineData("café", "cafe", 2.0 / 8)]
    [InlineData("", "", 0.0)]
    [InlineData("abc", "", 1.0)]
    [InlineData("abc", "abc", 0.0)]
    public void IndelNormalizedDistanceUsesFullLengths(string a, string b, double expected)
    {
        Assert.Equal(expected, Indel.NormalizedDistance(a.AsSpan(), b.AsSpan()), 12);
    }

    [Theory]
    [InlineData("abc", "abcd")]
    [InlineData("prefix", "preface")]
    [InlineData("kitten", "sitting")]
    [InlineData("", "x")]
    public void IndelNormalizedDistanceAndSimilarityAddUpToOne(string a, string b)
    {
        var sum = Indel.NormalizedDistance(a.AsSpan(), b.AsSpan()) + Indel.NormalizedSimilarity(a.AsSpan(), b.AsSpan());
        Assert.Equal(1.0, sum, 12);
    }

    // Bug: LongestCommonSubsequence.NormalizedSimilarity returned 0 for two empty
    // sequences; they are identical, so the similarity is 1.
    [Fact]
    public void LcsNormalizedSimilarityOfTwoEmptyIsOne()
    {
        Assert.Equal(1.0, LongestCommonSubsequence.NormalizedSimilarity(ReadOnlySpan<char>.Empty, ReadOnlySpan<char>.Empty), 12);
    }

    [Theory]
    [InlineData("abc", "")]
    [InlineData("", "abc")]
    public void LcsNormalizedSimilarityWithOneEmptyIsZero(string a, string b)
    {
        Assert.Equal(0.0, LongestCommonSubsequence.NormalizedSimilarity(a.AsSpan(), b.AsSpan()), 12);
    }
}
