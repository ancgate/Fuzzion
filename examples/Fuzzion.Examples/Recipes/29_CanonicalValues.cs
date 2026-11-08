using System.Collections.Concurrent;
using Fuzzion;
using Fuzzion.PreProcess;
using Fuzzion.SimilarityRatio;
using Fuzzion.SimilarityRatio.Scorer.Composite;

namespace FuzzionExamples.Recipes;

/// <summary>
/// Cleaning a free-text column: map what users typed ("U.S.A", "Untied States", "UK") to
/// one canonical value per entry, with aliases for names that are not spelling variants,
/// a cutoff so unknown values go to review instead of being guessed, and a cache because
/// the same messy values repeat.
/// </summary>
public static class CanonicalValues
{
    private sealed record Alias(string Text, string Canonical);

    public static void Run()
    {
        var countries = new Dictionary<string, string[]>
        {
            ["United States"] = new[] { "USA", "US", "United States of America", "America" },
            ["United Kingdom"] = new[] { "UK", "Great Britain", "Britain", "England" },
            ["Germany"] = new[] { "Deutschland", "DE" },
            ["Netherlands"] = new[] { "Holland", "The Netherlands", "NL" },
            ["Côte d'Ivoire"] = new[] { "Ivory Coast", "Cote d'Ivoire" },
        };

        // Search the canonical names and the aliases together; each points to its canonical value.
        var aliases = countries
            .SelectMany(c => c.Value.Prepend(c.Key).Select(text => new Alias(text, c.Key)))
            .ToList();

        var scorer = ScorerCache.Get<WeightedRatioScorer>();

        // Remove dots first, so "U.S.A" becomes "USA" rather than "u s a".
        var clean = StringPreprocessor.Compose(s => s.Replace(".", ""), StringPreprocessor.Full);
        var cache = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        string ToCanonical(string input) => cache.GetOrAdd(input.Trim(), value =>
        {
            var best = Process.ExtractOneBy(value, aliases, a => a.Text, clean, scorer, cutoff: 86);
            return best?.Value.Canonical;
        });

        var column = new[] { "U.S.A", "united states", "Untied States", "uk", "Great Britian", "germany ", "Deutchland", "holland", "Ivory coast", "Narnia", "U.S.A" };
        foreach (var value in column)
        {
            Console.WriteLine($"   {"\"" + value + "\"",-17} -> {ToCanonical(value) ?? "(needs review)"}");
        }

        Console.WriteLine($"   {column.Length} values, {cache.Count} distinct searches (repeats come from the cache)");
    }
}
