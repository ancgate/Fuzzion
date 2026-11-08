using System.Text.RegularExpressions;
using Fuzzion;
using Fuzzion.PreProcess;
using Fuzzion.SimilarityRatio;
using Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

namespace FuzzionExamples.Recipes;

/// <summary>
/// Company names differ in legal suffixes (Inc, LLC, GmbH), "&amp;" vs "and", accents,
/// punctuation and word order. Normalizing those away first, with a custom preprocessor,
/// matters more than the choice of scorer.
/// </summary>
public static class CompanyNames
{
    private static readonly HashSet<string> Noise = new()
    {
        "the", "inc", "incorporated", "llc", "ltd", "limited", "corp", "corporation", "co", "company",
        "gmbh", "ag", "sa", "plc", "group", "holdings",
    };

    /// <summary>
    /// Turns "&amp;" into "and", removes dots (so "S.A." becomes "SA"), accents and punctuation,
    /// lowercases, and drops legal suffixes.
    /// </summary>
    public static readonly Func<string, string> Normalize = StringPreprocessor.Compose(
        s => s.Replace("&", " and ").Replace(".", ""),
        StringPreprocessor.RemoveDiacritics,
        StringPreprocessor.Full,
        s => string.Join(" ", Regex.Split(s, @"\s+").Where(w => w.Length > 0 && !Noise.Contains(w))));

    public static void Run()
    {
        var master = new[]
        {
            "Johnson & Johnson", "Procter & Gamble Co.", "Nestlé S.A.", "The Coca-Cola Company",
            "Alphabet Inc.", "Société Générale", "Barnes & Noble, Inc.",
        };

        Show.Heading("What the normalizer does");
        foreach (var name in master)
        {
            Console.WriteLine($"   {name,-24} -> \"{Normalize(name)}\"");
        }

        Show.Heading("Matching incoming names against the master list");
        var tokenSort = ScorerCache.Get<TokenSortScorer>();
        var incoming = new[] { "JOHNSON AND JOHNSON INC", "Procter and Gamble", "Nestle SA", "Coca Cola Co", "Societe Generale Group", "Barnes and Nobel", "Tesla" };
        foreach (var name in incoming)
        {
            var plain = Process.ExtractOne(name, master);
            var normalized = Process.ExtractOne(name, master, Normalize, tokenSort, cutoff: 85);
            Console.WriteLine($"   {name,-24} default: {plain.Value,-22} {Show.Num(plain.Score),5}   " +
                              $"normalized: {(normalized == null ? "(no match)" : $"{normalized.Value,-22} {Show.Num(normalized.Score),5}")}");
        }

        Console.WriteLine();
        Console.WriteLine("   With normalization the scores for true matches rise and \"Tesla\" no longer gets a guess.");
    }
}
