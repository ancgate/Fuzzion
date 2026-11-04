using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Fuzzion.PreProcess;
using Fuzzion.SimilarityRatio;
using Fuzzion.SimilarityRatio.Scorer.Composite;
using Fuzzion.SimilarityRatio.Scorer.StrategySensitive;
using Fuzzion.Tests.Oracle;
using Xunit;

namespace Fuzzion.Tests;

/// <summary>
/// Plan items #31 and #32. Oracle values in Oracle/data/unicode-cases.json come from
/// Python rapidfuzz 3.14.6, which compares whole characters.
/// </summary>
public class Phase7UnicodeTests
{
    private static readonly Lazy<JsonElement> Cases = new(() =>
        JsonDocument.Parse(File.ReadAllText(OracleData.FindFile("unicode-cases"), Encoding.UTF8)).RootElement.Clone());

    // ---- #31 Code points -------------------------------------------------------------

    [Fact]
    public void EmojiCodePointMatchesPython()
    {
        Assert.Equal(75, CodePoints.Compare("😀abc", "😀abd", Fuzz.Ratio), 9);
    }

    [Fact]
    public void EmojiDefaultModeUnchanged()
    {
        // UTF-16 counts the emoji as two characters; no silent behavior change.
        Assert.Equal(80, Fuzz.Ratio("😀abc", "😀abd"), 9);
    }

    [Fact]
    public void Oracle_CodePoints()
    {
        var check = new OracleAssert("unicode pairs");
        foreach (var row in Cases.Value.GetProperty("pairs").EnumerateArray())
        {
            var a = row.GetProperty("a").GetString();
            var b = row.GetProperty("b").GetString();
            var oracleRow = new OracleRow(JsonDocument.Parse($"{{\"group\":\"unicode\",\"a\":{JsonSerializer.Serialize(a)},\"b\":{JsonSerializer.Serialize(b)}}}").RootElement);

            check.Equal(oracleRow, "ratio", row.GetProperty("ratio").GetDouble(), CodePoints.Compare(a, b, Fuzz.Ratio), OracleAssert.Score);
            check.Equal(oracleRow, "partial_ratio", row.GetProperty("partial_ratio").GetDouble(), CodePoints.Compare(a, b, Fuzz.PartialRatio), OracleAssert.Score);
            check.Equal(oracleRow, "token_set_ratio", row.GetProperty("token_set_ratio").GetDouble(), CodePoints.Compare(a, b, Fuzz.TokenSetRatio), OracleAssert.Score);
            check.Equal(oracleRow, "wratio", row.GetProperty("wratio").GetDouble(), CodePoints.Compare(a, b, Fuzz.WeightedRatio), OracleAssert.Score);
            check.Equal(oracleRow, "levenshtein", row.GetProperty("levenshtein").GetDouble(), CodePoints.Compare(a, b, (x, y) => Levenshtein.Distance(x, y)), OracleAssert.Exact);
            check.Equal(oracleRow, "indel", row.GetProperty("indel").GetDouble(), CodePoints.Compare(a, b, (x, y) => Indel.Distance(x.AsSpan(), y.AsSpan())), OracleAssert.Exact);
            check.Equal(oracleRow, "jaro_winkler", row.GetProperty("jaro_winkler").GetDouble(), CodePoints.Compare(a, b, (x, y) => JaroWinkler.Similarity(x, y)), OracleAssert.Normalized);
        }

        check.Verify();
    }

    [Fact]
    public void BmpTextSameInBothModes()
    {
        var random = new Random(31);
        for (var i = 0; i < 500; i++)
        {
            var a = Letters(random, random.Next(0, 20), "abcé中 ");
            var b = Letters(random, random.Next(0, 20), "abcé中 ");
            Assert.Equal(Fuzz.WeightedRatio(a, b), CodePoints.Compare(a, b, Fuzz.WeightedRatio), 12);
            Assert.Same(a, CodePoints.Map(a, b).A);
        }
    }

    [Fact]
    public void ExistingPrivateUseCharactersAreNotReused()
    {
        // U+E000 is already in the input, so the emoji must map to a different character.
        Assert.Equal(1, CodePoints.Compare("", "😀", (x, y) => Levenshtein.Distance(x, y)));
        Assert.Equal(0, CodePoints.Compare("😀", "😀", (x, y) => Levenshtein.Distance(x, y)));
    }

    [Fact]
    public void SameCodePointGetsSameReplacementInBothStrings()
    {
        var (a, b) = CodePoints.Map("x😀y😁", "😁😀");
        Assert.Equal(4, a.Length);
        Assert.Equal(2, b.Length);
        Assert.Equal(a[1], b[1]);
        Assert.Equal(a[3], b[0]);
        Assert.NotEqual(a[1], a[3]);
    }

    [Fact]
    public void CountAndOf()
    {
        Assert.Equal(4, CodePoints.Count("😀abc"));
        Assert.Equal([0x1F600, 'a', 0x20000], CodePoints.Of("😀a𠀀"));
        Assert.Equal(['a', 0xD800], CodePoints.Of("a\uD800")); // a lone surrogate stays as is
    }

    [Fact]
    public void CodePointScorerWorksInSearchesAndCdist()
    {
        var scorer = CodePoints.Scorer(ScorerCache.Get<DefaultRatioScorer>());
        Assert.Equal(75, Process.ExtractOne("😀abc", ["😀abd", "zzzz"], s => s, scorer).Score, 9);
        Assert.Equal(75, Process.Cdist(["😀abc"], ["😀abd"], scorer)[0, 0], 9);

        var distance = CodePoints.Scorer(Scorers.FromDistance((a, b) => Levenshtein.Distance(a, b), maxDistance: 1));
        Assert.Equal(1, distance.MaxDistance);
        Assert.Equal(1, Process.ExtractOne("😀abc", ["😀abd", "😁xyz"], s => s, distance).Score);
    }

    // ---- Ordinal token sorting --------------------------------------------------------

    [Fact]
    public void TokenSortingIsOrdinal()
    {
        // Python sorts by code point: "Zoo" < "apple". A culture-sensitive sort gave 88.9.
        Assert.Equal(55.555556, Fuzz.TokenSortRatio("Zoo apple", "apple zoo"), 6);
    }

    [Fact]
    public void Oracle_MixedCaseTokenScorers()
    {
        foreach (var row in Cases.Value.GetProperty("mixed_case").EnumerateArray())
        {
            var a = row.GetProperty("a").GetString();
            var b = row.GetProperty("b").GetString();
            Assert.Equal(row.GetProperty("token_sort_ratio").GetDouble(), Fuzz.TokenSortRatio(a, b), 6);
            Assert.Equal(row.GetProperty("token_set_ratio").GetDouble(), Fuzz.TokenSetRatio(a, b), 6);
            Assert.Equal(row.GetProperty("partial_token_sort_ratio").GetDouble(), Fuzz.PartialTokenSortRatio(a, b), 6);
            Assert.Equal(row.GetProperty("wratio").GetDouble(), Fuzz.WeightedRatio(a, b), 6);

            using var cached = new CachedTokenSetScorer(a);
            Assert.Equal(row.GetProperty("token_set_ratio").GetDouble(), cached.Score(b), 6);
        }
    }

    // ---- FullUnicode -----------------------------------------------------------------

    [Fact]
    public void FullUnicodeKeepsSurrogatePairs()
    {
        Assert.Equal("𠀀abc", StringPreprocessor.FullUnicode("𠀀abc"));
        Assert.Equal("abc", StringPreprocessor.Full("𠀀abc").Trim()); // Full turns the two halves into spaces
    }

    [Theory]
    [InlineData("a_b", "a b")]
    [InlineData("x²y", "x²y")]
    [InlineData("½", "½")]
    [InlineData("Ⅻ", "ⅻ")]
    [InlineData("😀abc", "abc")]
    [InlineData("İstanbul", "istanbul")]
    [InlineData("ΣΑΣ", "σασ")]
    [InlineData("  Hi!  ", "hi")]
    [InlineData("áb", "a b")]
    [InlineData("٣٤", "٣٤")]
    [InlineData("ǅ", "ǆ")]
    public void FullUnicodeMatchesRapidFuzz(string input, string expected)
    {
        Assert.Equal(expected, StringPreprocessor.FullUnicode(input));
    }

    [Fact]
    public void Oracle_FullUnicode()
    {
        foreach (var row in Cases.Value.GetProperty("default_process").EnumerateArray())
        {
            var input = row.GetProperty("input").GetString();
            var expected = row.GetProperty("default_process").GetString();
#if NETFRAMEWORK
            // .NET Framework cannot lowercase letters outside the BMP (here U+10400).
            if (input.Contains("\U00010400")) continue;
#endif
            Assert.True(expected == StringPreprocessor.FullUnicode(input),
                $"input {JsonSerializer.Serialize(input)}: expected {JsonSerializer.Serialize(expected)}, got {JsonSerializer.Serialize(StringPreprocessor.FullUnicode(input))}");
        }
    }

    // ---- #32 RemoveDiacritics and Compose --------------------------------------------

    [Fact]
    public void CafeWithAndWithoutDiacritics()
    {
        Assert.Equal(75, Fuzz.Ratio("café", "cafe"), 9);
        Assert.Equal(100, Fuzz.Ratio("café", "cafe", StringPreprocessor.RemoveDiacritics), 9);
    }

    [Theory]
    [InlineData("Ærøskøbing", "Aeroskobing")]
    [InlineData("straße", "strasse")]
    [InlineData("Crème Brûlée", "Creme Brulee")]
    [InlineData("Łódź", "Lodz")]
    [InlineData("Þórr", "Thorr")]
    [InlineData("naïve façade", "naive facade")]
    [InlineData("Ĳ", "Ĳ")] // no decomposition and not in the map: unchanged
    [InlineData("", "")]
    public void RemoveDiacriticsExamples(string input, string expected)
    {
        Assert.Equal(expected, StringPreprocessor.RemoveDiacritics(input));
    }

    [Fact]
    public void PrecomposedEqualsDecomposed()
    {
        Assert.Equal(StringPreprocessor.RemoveDiacritics("é"), StringPreprocessor.RemoveDiacritics("é"));
        Assert.Equal("e", StringPreprocessor.RemoveDiacritics("é"));
    }

    [Theory]
    [InlineData("Москва")]
    [InlineData("中文字")]
    [InlineData("مرحبا")]
    [InlineData("hello 😀 world")]
    public void NonLatinUnchanged(string input)
    {
        Assert.Equal(input, StringPreprocessor.RemoveDiacritics(input));
    }

    [Fact]
    public void ComposeAppliesInOrder()
    {
        var clean = StringPreprocessor.Compose(StringPreprocessor.RemoveDiacritics, StringPreprocessor.Full);
        Assert.Equal("creme brulee", clean("Crème BRÛLÉE!"));
        Assert.Equal("x", StringPreprocessor.Compose()("x"));
        Assert.Throws<ArgumentException>(() => StringPreprocessor.Compose(StringPreprocessor.Full, null));
    }

    [Fact]
    public void ComposeWorksAsSearchProcessor()
    {
        var clean = StringPreprocessor.Compose(StringPreprocessor.RemoveDiacritics, StringPreprocessor.Full);
        var best = Process.ExtractOne("CREME BRULEE", ["crème brûlée", "tiramisu"], clean, ScorerCache.Get<WeightedRatioScorer>());
        Assert.Equal("crème brûlée", best.Value);
        Assert.Equal(100, best.Score);
    }

    private static string Letters(Random random, int length, string alphabet) =>
        new(Enumerable.Range(0, length).Select(_ => alphabet[random.Next(alphabet.Length)]).ToArray());
}
