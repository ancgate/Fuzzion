using System;
using System.Linq;
using System.Reflection;
using Fuzzion.PreProcess;
using Xunit;

namespace Fuzzion.Tests.FuzzyTests;

/// <summary>Plan items #21 (score cutoff overloads) and #22 (span overloads).</summary>
public class CutoffAndSpanTests
{
    private static readonly string[] Words = ["new", "york", "mets", "braves", "vs", "chicago", "cubs", "the", "a", "fuzzy", "bear", "inc", "ltd"];

    /// <summary>Names of every Fuzz scorer that has a (string, string, double scoreCutoff) overload.</summary>
    private static string[] CutoffMethodNames() =>
        typeof(Fuzz).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.ReturnType == typeof(double) && IsParams(m, typeof(string), typeof(string), typeof(double)))
            .Select(m => m.Name).Distinct().OrderBy(n => n).ToArray();

    public static TheoryData<string> CutoffMethods()
    {
        var data = new TheoryData<string>();
        foreach (var name in CutoffMethodNames())
        {
            data.Add(name);
        }

        return data;
    }

    // ---- #21 Score cutoff ------------------------------------------------------------

    [Fact]
    public void EveryScorerHasCutoffOverloads()
    {
        var plain = typeof(Fuzz).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.ReturnType == typeof(double) && IsParams(m, typeof(string), typeof(string)))
            .Select(m => m.Name).Distinct().ToArray();
        var withCutoff = CutoffMethodNames();
        var withPreprocessorAndCutoff = typeof(Fuzz).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => IsParams(m, typeof(string), typeof(string), typeof(Func<string, string>), typeof(double)))
            .Select(m => m.Name).Distinct().ToArray();

        Assert.Equal(17, plain.Length);
        Assert.Equal(plain.OrderBy(n => n), withCutoff.OrderBy(n => n));
        Assert.Equal(plain.OrderBy(n => n), withPreprocessorAndCutoff.OrderBy(n => n));
    }

    [Fact]
    public void RatioKnownValues()
    {
        Assert.Equal(61.538462, Fuzz.Ratio("kitten", "sitting"), 6);
        Assert.Equal(0, Fuzz.Ratio("kitten", "sitting", scoreCutoff: 80));
        Assert.Equal(61.538462, Fuzz.Ratio("kitten", "sitting", scoreCutoff: 61), 6);
    }

    [Theory]
    [MemberData(nameof(CutoffMethods))]
    public void CutoffMatchesFilteredScore(string name)
    {
        var plain = Method(name, typeof(string), typeof(string));
        var cut = Method(name, typeof(string), typeof(string), typeof(double));
        var random = new Random(name.Length * 7919);

        foreach (var (a, b) in RandomPairs(random, 300))
        {
            var score = (double)plain.Invoke(null, [a, b]);
            foreach (var cutoff in new[] { 0.0, score, Math.Max(0, score - 0.001), score + 0.001, random.NextDouble() * 100, 100.0 })
            {
                var expected = score >= cutoff ? score : 0;
                Assert.Equal(expected, (double)cut.Invoke(null, [a, b, cutoff]), 12);
            }
        }
    }

    [Theory]
    [MemberData(nameof(CutoffMethods))]
    public void PreprocessorCutoffMatchesPreprocessorScore(string name)
    {
        var withPre = Method(name, typeof(string), typeof(string), typeof(Func<string, string>));
        var withPreCut = Method(name, typeof(string), typeof(string), typeof(Func<string, string>), typeof(double));
        Func<string, string> full = StringPreprocessor.Full;

        foreach (var (a, b) in RandomPairs(new Random(5), 100).Select(p => (p.A.ToUpperInvariant() + "!", p.B)))
        {
            var score = (double)withPre.Invoke(null, [a, b, full]);
            Assert.Equal(score, (double)withPreCut.Invoke(null, [a, b, full, 0.0]), 12);
            Assert.Equal(score >= 50 ? score : 0, (double)withPreCut.Invoke(null, [a, b, full, 50.0]), 12);
        }
    }

    [Fact]
    public void LengthBoundSkipsHopelessPairs()
    {
        // 200 * 1 / (1 + 100) ≈ 1.98, so a cutoff of 2 cannot be reached.
        var longText = new string('a', 100);
        Assert.Equal(0, Fuzz.Ratio("a", longText, scoreCutoff: 2));
        Assert.Equal(Fuzz.Ratio("a", longText), Fuzz.Ratio("a", longText, scoreCutoff: 1.9), 12);
    }

    [Fact]
    public void CutoffEqualToTightBoundKeepsScore()
    {
        // When one input is a prefix of the other, the score equals the length bound
        // exactly. A rounding difference between the two used to make these return 0.
        for (var n = 1; n <= 60; n++)
        {
            for (var extra = 0; extra <= 60; extra++)
            {
                var a = new string('x', n);
                var b = a + new string('y', extra);
                var score = Fuzz.Ratio(a, b);
                Assert.Equal(score, Fuzz.Ratio(a, b, score), 12);
                Assert.Equal(score, Fuzz.QRatio(a, b, score), 12);
                Assert.Equal(score, Fuzz.Ratio(a.AsSpan(), b.AsSpan(), score), 12);
            }
        }
    }

    [Fact]
    public void NaNCutoffThrows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Fuzz.Ratio("a", "b", double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => Fuzz.WeightedRatio("a", "b", double.NaN));
    }

    [Fact]
    public void MethodGroupConversionsStillWork()
    {
        Func<string, string, double> ratio = Fuzz.Ratio;
        Func<string, string, Func<string, string>, double> ratioWithPreprocessor = Fuzz.Ratio;
        Func<string, string, double, double> ratioWithCutoff = Fuzz.Ratio;

        Assert.Equal(100, ratio("abc", "abc"));
        Assert.Equal(100, ratioWithPreprocessor("ABC", "abc", StringPreprocessor.Full));
        Assert.Equal(0, ratioWithCutoff("abc", "xyz", 50));
    }

    [Fact]
    public void NullPreprocessorStillMeansFull()
    {
        // Passing null must pick the Func overload, as before.
        Assert.Equal(100, Fuzz.Ratio("ABC!", "abc", null));
        Assert.Equal(100, Fuzz.Ratio("ABC!", "abc", null, 90));
    }

    // ---- #22 Span overloads ----------------------------------------------------------

    [Fact]
    public void SpanEqualsString()
    {
        var random = new Random(22);
        foreach (var (a, b) in RandomPairs(random, 500).Concat(RandomCharPairs(random, 300)))
        {
            Assert.Equal(Fuzz.Ratio(a, b), Fuzz.Ratio(a.AsSpan(), b.AsSpan()), 12);
            Assert.Equal(Fuzz.QRatio(a, b), Fuzz.QRatio(a.AsSpan(), b.AsSpan()), 12);
            Assert.Equal(Fuzz.PartialRatio(a, b), Fuzz.PartialRatio(a.AsSpan(), b.AsSpan()), 12);
            Assert.Equal(Fuzz.ApproximateSubstringRatio(a, b), Fuzz.ApproximateSubstringRatio(a.AsSpan(), b.AsSpan()), 12);

            Assert.Equal(Fuzz.Ratio(a, b, 50), Fuzz.Ratio(a.AsSpan(), b.AsSpan(), 50), 12);
            Assert.Equal(Fuzz.QRatio(a, b, 50), Fuzz.QRatio(a.AsSpan(), b.AsSpan(), 50), 12);
            Assert.Equal(Fuzz.PartialRatio(a, b, 50), Fuzz.PartialRatio(a.AsSpan(), b.AsSpan(), 50), 12);
            Assert.Equal(Fuzz.ApproximateSubstringRatio(a, b, 50), Fuzz.ApproximateSubstringRatio(a.AsSpan(), b.AsSpan(), 50), 12);
        }
    }

    [Fact]
    public void SliceOfLargerBuffer()
    {
        char[] buffer = "xxxx new york mets yyyy the new yorkk mets zzzz".ToCharArray();
        var first = buffer.AsSpan(5, 13);   // "new york mets"
        var second = buffer.AsSpan(28, 14); // "new yorkk mets"

        Assert.Equal(Fuzz.Ratio("new york mets", "new yorkk mets"), Fuzz.Ratio(first, second), 12);
        Assert.Equal(Fuzz.PartialRatio("new york mets", "new yorkk mets"), Fuzz.PartialRatio(first, second), 12);
    }

    [Theory]
    [InlineData("", "", 100, 0)]
    [InlineData("abc", "", 0, 0)]
    [InlineData("", "abc", 0, 0)]
    public void SpanEmptyRules(string a, string b, double ratio, double qratio)
    {
        Assert.Equal(ratio, Fuzz.Ratio(a.AsSpan(), b.AsSpan()));
        Assert.Equal(qratio, Fuzz.QRatio(a.AsSpan(), b.AsSpan()));
    }

    // ---- Helpers ----------------------------------------------------------------------

    private static bool IsParams(MethodInfo m, params Type[] types)
    {
        var p = m.GetParameters();
        return p.Length == types.Length && p.Select(x => x.ParameterType).SequenceEqual(types);
    }

    private static MethodInfo Method(string name, params Type[] types) =>
        typeof(Fuzz).GetMethod(name, BindingFlags.Public | BindingFlags.Static, null, types, null)
        ?? throw new InvalidOperationException($"Fuzz.{name}({string.Join(", ", types.Select(t => t.Name))}) not found");

    private static (string A, string B)[] RandomPairs(Random random, int count) =>
        Enumerable.Range(0, count)
            .Select(_ => (Sentence(random, random.Next(0, 5)), Sentence(random, random.Next(0, 9))))
            .ToArray();

    private static (string A, string B)[] RandomCharPairs(Random random, int count) =>
        Enumerable.Range(0, count)
            .Select(_ => (Letters(random, random.Next(0, 150)), Letters(random, random.Next(0, 150))))
            .ToArray();

    private static string Sentence(Random random, int words) =>
        string.Join(" ", Enumerable.Range(0, words).Select(_ => Words[random.Next(Words.Length)]));

    private static string Letters(Random random, int length) =>
        new(Enumerable.Range(0, length).Select(_ => "abcd"[random.Next(4)]).ToArray());
}
