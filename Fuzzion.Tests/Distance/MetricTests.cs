using System;
using Fuzzion.Tests.Support;
using Xunit;

namespace Fuzzion.Tests.Distance;

// Each class runs the shared contract (T1-T13) plus its own cases from
// docs/IMPLEMENTATION-PLAN.md. Expected values are from Python rapidfuzz 3.14.6.

/// <summary>A stand-in for metrics that have no instance class (Prefix, Postfix).</summary>
internal sealed class SourceOnly(string source) : IDisposable
{
    public string Source { get; } = source;

    public void Dispose()
    {
    }
}

public class HammingTests : DistanceMetricContract
{
    protected override double Distance(string a, string b) => Hamming.Distance(a, b);
    protected override double Similarity(string a, string b) => Hamming.Similarity(a, b);
    protected override double NormalizedDistance(string a, string b) => Hamming.NormalizedDistance(a, b);
    protected override double NormalizedSimilarity(string a, string b) => Hamming.NormalizedSimilarity(a, b);
    protected override double Maximum(string a, string b) => Math.Max(a.Length, b.Length);
    protected override double NaiveDistance(string a, string b) => Naive.Hamming(a, b);
    protected override double GenericDistance(int[] a, int[] b) => Hamming.Distance<int>(a, b);
    protected override IDisposable CreateInstance(string source) => new Hamming(source);
    protected override double InstanceDistance(IDisposable instance, string value) => ((Hamming)instance).DistanceFrom(value);

    [Theory]
    [InlineData("karolin", "kathrin", 3, 0.571429)]
    [InlineData("1011101", "1001001", 2, 0.714286)]
    [InlineData("abc", "abcd", 1, 0.75)]
    [InlineData("abc", "abc", 0, 1.0)]
    [InlineData("", "", 0, 1.0)]
    public void KnownValues(string a, string b, int distance, double normalizedSimilarity)
    {
        Assert.Equal(distance, Hamming.Distance(a, b));
        Assert.Equal(normalizedSimilarity, Hamming.NormalizedSimilarity(a, b), 6);
    }

    [Fact]
    public void WithoutPaddingDifferentLengthsThrow()
    {
        Assert.Throws<ArgumentException>(() => Hamming.Distance("abc", "abcd", pad: false));
        Assert.Equal(1, Hamming.Distance("abc", "abd", pad: false));
    }

    [Fact]
    public void InstanceRespectsPad()
    {
        using var instance = new Hamming("abc", pad: false);
        Assert.Throws<ArgumentException>(() => instance.DistanceFrom("abcd"));
    }
}

public class JaroTests : DistanceMetricContract
{
    protected override double Distance(string a, string b) => Jaro.Distance(a, b);
    protected override double Similarity(string a, string b) => Jaro.Similarity(a, b);
    protected override double NormalizedDistance(string a, string b) => Jaro.NormalizedDistance(a, b);
    protected override double NormalizedSimilarity(string a, string b) => Jaro.NormalizedSimilarity(a, b);
    protected override double Maximum(string a, string b) => 1.0;
    protected override double NaiveDistance(string a, string b) => 1.0 - Naive.Jaro(a, b);
    protected override double GenericDistance(int[] a, int[] b) => Jaro.Distance<int>(a, b);
    protected override IDisposable CreateInstance(string source) => new Jaro(source);
    protected override double InstanceDistance(IDisposable instance, string value) => ((Jaro)instance).DistanceFrom(value);
    protected override double Tolerance => 1e-12;

    [Theory]
    [InlineData("MARTHA", "MARHTA", 0.944444)]
    [InlineData("DWAYNE", "DUANE", 0.822222)]
    [InlineData("DIXON", "DICKSONX", 0.766667)]
    [InlineData("CRATE", "TRACE", 0.733333)]
    [InlineData("abc", "xyz", 0.0)]
    [InlineData("", "", 1.0)]
    [InlineData("a", "", 0.0)]
    public void KnownValues(string a, string b, double similarity)
    {
        Assert.Equal(similarity, Jaro.Similarity(a, b), 6);
    }
}

public class JaroWinklerTests : DistanceMetricContract
{
    protected override double Distance(string a, string b) => JaroWinkler.Distance(a, b);
    protected override double Similarity(string a, string b) => JaroWinkler.Similarity(a, b);
    protected override double NormalizedDistance(string a, string b) => JaroWinkler.NormalizedDistance(a, b);
    protected override double NormalizedSimilarity(string a, string b) => JaroWinkler.NormalizedSimilarity(a, b);
    protected override double Maximum(string a, string b) => 1.0;
    protected override double NaiveDistance(string a, string b) => 1.0 - Naive.JaroWinkler(a, b);
    protected override double GenericDistance(int[] a, int[] b) => JaroWinkler.Distance<int>(a, b);
    protected override IDisposable CreateInstance(string source) => new JaroWinkler(source);
    protected override double InstanceDistance(IDisposable instance, string value) => ((JaroWinkler)instance).DistanceFrom(value);
    protected override double Tolerance => 1e-12;

    [Theory]
    [InlineData("MARTHA", "MARHTA", 0.961111, 0.977778)]
    [InlineData("DWAYNE", "DUANE", 0.84, 0.857778)]
    [InlineData("DIXON", "DICKSONX", 0.813333, 0.86)]
    [InlineData("CRATE", "TRACE", 0.733333, 0.733333)]
    public void KnownValues(string a, string b, double defaultWeight, double weight02)
    {
        Assert.Equal(defaultWeight, JaroWinkler.Similarity(a, b), 6);
        Assert.Equal(weight02, JaroWinkler.Similarity(a, b, prefixWeight: 0.2), 6);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.5)]
    [InlineData(double.NaN)]
    public void PrefixWeightOutOfRangeThrows(double weight)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => JaroWinkler.Similarity("a", "b", weight));
        Assert.Throws<ArgumentOutOfRangeException>(() => new JaroWinkler("a", weight));
    }

    [Fact]
    public void NeverBelowJaro()
    {
        foreach (var (a, b) in RandomPairs(2000))
        {
            Assert.True(JaroWinkler.Similarity(a, b) >= Jaro.Similarity(a, b) - 1e-15);
        }
    }

    [Fact]
    public void NoBoostAtOrBelowThreshold()
    {
        // Jaro("CRATE", "TRACE") = 0.733 > 0.7 but there is no common prefix, so no boost.
        Assert.Equal(Jaro.Similarity("CRATE", "TRACE"), JaroWinkler.Similarity("CRATE", "TRACE"), 15);
    }
}

public class OsaTests : DistanceMetricContract
{
    protected override double Distance(string a, string b) => Osa.Distance(a, b);
    protected override double Similarity(string a, string b) => Osa.Similarity(a, b);
    protected override double NormalizedDistance(string a, string b) => Osa.NormalizedDistance(a, b);
    protected override double NormalizedSimilarity(string a, string b) => Osa.NormalizedSimilarity(a, b);
    protected override double Maximum(string a, string b) => Math.Max(a.Length, b.Length);
    protected override double NaiveDistance(string a, string b) => Naive.Osa(a, b);
    protected override double GenericDistance(int[] a, int[] b) => Osa.Distance<int>(a, b);
    protected override IDisposable CreateInstance(string source) => new Osa(source);
    protected override double InstanceDistance(IDisposable instance, string value) => ((Osa)instance).DistanceFrom(value);

    [Theory]
    [InlineData("ab", "ba", 1, 0.5)]
    [InlineData("CA", "ABC", 3, 0.0)]
    [InlineData("abcdef", "abcfde", 2, 0.666667)]
    [InlineData("kitten", "sitting", 3, 0.571429)]
    public void KnownValues(string a, string b, int distance, double normalizedSimilarity)
    {
        Assert.Equal(distance, Osa.Distance(a, b));
        Assert.Equal(normalizedSimilarity, Osa.NormalizedSimilarity(a, b), 6);
    }

    [Fact]
    public void OsaDiffersFromDamerauOnCaAbc()
    {
        Assert.Equal(3, Osa.Distance("CA", "ABC"));
        Assert.Equal(2, DamerauLevenshtein.Distance("CA", "ABC"));
    }

    [Fact]
    public void NeverAboveLevenshtein()
    {
        foreach (var (a, b) in RandomPairs(2000, maxLength: 100))
        {
            Assert.True(Osa.Distance(a, b) <= Levenshtein.Distance(a, b));
        }
    }
}

public class DamerauLevenshteinTests : DistanceMetricContract
{
    protected override double Distance(string a, string b) => DamerauLevenshtein.Distance(a, b);
    protected override double Similarity(string a, string b) => DamerauLevenshtein.Similarity(a, b);
    protected override double NormalizedDistance(string a, string b) => DamerauLevenshtein.NormalizedDistance(a, b);
    protected override double NormalizedSimilarity(string a, string b) => DamerauLevenshtein.NormalizedSimilarity(a, b);
    protected override double Maximum(string a, string b) => Math.Max(a.Length, b.Length);
    protected override double NaiveDistance(string a, string b) => Naive.DamerauLevenshtein(a, b);
    protected override double GenericDistance(int[] a, int[] b) => DamerauLevenshtein.Distance<int>(a, b);
    protected override IDisposable CreateInstance(string source) => new DamerauLevenshtein(source);
    protected override double InstanceDistance(IDisposable instance, string value) => ((DamerauLevenshtein)instance).DistanceFrom(value);

    [Theory]
    [InlineData("CA", "ABC", 2, 0.333333)]
    [InlineData("ab", "ba", 1, 0.5)]
    [InlineData("abcdef", "abcfde", 2, 0.666667)]
    [InlineData("kitten", "sitting", 3, 0.571429)]
    public void KnownValues(string a, string b, int distance, double normalizedSimilarity)
    {
        Assert.Equal(distance, DamerauLevenshtein.Distance(a, b));
        Assert.Equal(normalizedSimilarity, DamerauLevenshtein.NormalizedSimilarity(a, b), 6);
    }

    [Fact]
    public void NeverAboveOsaOrLevenshtein()
    {
        foreach (var (a, b) in RandomPairs(2000, maxLength: 100))
        {
            var dl = DamerauLevenshtein.Distance(a, b);
            Assert.True(dl <= Osa.Distance(a, b));
            Assert.True(dl <= Levenshtein.Distance(a, b));
        }
    }

    [Fact]
    public void NonAsciiCharacters()
    {
        Assert.Equal(1, DamerauLevenshtein.Distance("жук", "ужк"));
        Assert.Equal(Naive.DamerauLevenshtein("中文字日本", "文中字本日"), DamerauLevenshtein.Distance("中文字日本", "文中字本日"));
    }
}

public class PrefixTests : DistanceMetricContract
{
    protected override double Distance(string a, string b) => Prefix.Distance(a, b);
    protected override double Similarity(string a, string b) => Prefix.Similarity(a, b);
    protected override double NormalizedDistance(string a, string b) => Prefix.NormalizedDistance(a, b);
    protected override double NormalizedSimilarity(string a, string b) => Prefix.NormalizedSimilarity(a, b);
    protected override double Maximum(string a, string b) => Math.Max(a.Length, b.Length);
    protected override double NaiveDistance(string a, string b) => Math.Max(a.Length, b.Length) - Naive.Prefix(a, b);
    protected override double GenericDistance(int[] a, int[] b) => Prefix.Distance<int>(a, b);
    protected override IDisposable CreateInstance(string source) => new SourceOnly(source);
    protected override double InstanceDistance(IDisposable instance, string value) => Prefix.Distance(((SourceOnly)instance).Source, value);

    [Theory]
    [InlineData("prefix", "preface", 4, 3)]
    [InlineData("testing", "resting", 0, 7)]
    [InlineData("abc", "xyz", 0, 3)]
    [InlineData("", "", 0, 0)]
    public void KnownValues(string a, string b, int similarity, int distance)
    {
        Assert.Equal(similarity, Prefix.Similarity(a, b));
        Assert.Equal(distance, Prefix.Distance(a, b));
    }
}

public class PostfixTests : DistanceMetricContract
{
    protected override double Distance(string a, string b) => Postfix.Distance(a, b);
    protected override double Similarity(string a, string b) => Postfix.Similarity(a, b);
    protected override double NormalizedDistance(string a, string b) => Postfix.NormalizedDistance(a, b);
    protected override double NormalizedSimilarity(string a, string b) => Postfix.NormalizedSimilarity(a, b);
    protected override double Maximum(string a, string b) => Math.Max(a.Length, b.Length);
    protected override double NaiveDistance(string a, string b) => Math.Max(a.Length, b.Length) - Naive.Postfix(a, b);
    protected override double GenericDistance(int[] a, int[] b) => Postfix.Distance<int>(a, b);
    protected override IDisposable CreateInstance(string source) => new SourceOnly(source);
    protected override double InstanceDistance(IDisposable instance, string value) => Postfix.Distance(((SourceOnly)instance).Source, value);

    [Theory]
    [InlineData("prefix", "preface", 0, 7)]
    [InlineData("testing", "resting", 6, 1)]
    [InlineData("abc", "xyz", 0, 3)]
    [InlineData("", "", 0, 0)]
    public void KnownValues(string a, string b, int similarity, int distance)
    {
        Assert.Equal(similarity, Postfix.Similarity(a, b));
        Assert.Equal(distance, Postfix.Distance(a, b));
    }
}

public class IndelSimilarityTests
{
    [Theory]
    [InlineData("kitten", "sitting", 5, 8)]
    [InlineData("abc", "abc", 0, 6)]
    [InlineData("", "", 0, 0)]
    [InlineData("abc", "", 3, 0)]
    public void KnownValues(string a, string b, int distance, int similarity)
    {
        Assert.Equal(distance, Indel.Distance(a.AsSpan(), b.AsSpan()));
        Assert.Equal(similarity, Indel.Similarity(a.AsSpan(), b.AsSpan()));
    }

    [Fact]
    public void SimilarityPlusDistanceIsTotalLength()
    {
        var random = new Random(55);
        for (var i = 0; i < 1000; i++)
        {
            var a = RandomString(random, random.Next(0, 40));
            var b = RandomString(random, random.Next(0, 40));
            Assert.Equal(a.Length + b.Length, Indel.Similarity(a.AsSpan(), b.AsSpan()) + Indel.Distance(a.AsSpan(), b.AsSpan()));
            Assert.Equal(2 * Naive.LcsLength(a, b), Indel.Similarity(a.AsSpan(), b.AsSpan()));
        }
    }

    private static string RandomString(Random random, int length)
    {
        var chars = new char[length];
        for (var i = 0; i < length; i++) chars[i] = "abcde"[random.Next(5)];
        return new string(chars);
    }
}
