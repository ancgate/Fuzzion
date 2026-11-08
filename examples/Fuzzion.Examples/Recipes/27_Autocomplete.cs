using Fuzzion;

namespace FuzzionExamples.Recipes;

/// <summary>
/// Suggestions while the user types: the text so far should match the start of a choice,
/// but typos must not break it. Combines PartialRatio (typo-tolerant "contains") with a
/// bonus for a shared start, in one reusable, cached, parallel pipeline.
/// </summary>
public static class Autocomplete
{
    public static void Run()
    {
        var cities = new[]
        {
            "London", "Londonderry", "Long Beach", "Los Angeles", "Louisville", "New London", "New York", "Newark",
            "New Orleans", "Lyon", "Lisbon", "Ljubljana",
        };

        // The default preprocessor has already lowercased both strings when the scorer runs.
        // 80% "contains, typos allowed", 15% "starts the same way", 5% "short choices first".
        var scorer = Scorers.FromSimilarity((typed, choice) =>
            0.80 * Fuzz.PartialRatio(typed, choice)
            + 15.0 * Prefix.Similarity(typed, choice) / Math.Max(1, typed.Length)
            + 5.0 * Math.Min(typed.Length, choice.Length) / Math.Max(1, choice.Length));

        var suggest = Process.Configure().WithScorer(scorer).Parallel().Build();

        foreach (var typed in new[] { "lo", "lon", "lond", "londn", "new", "new yo", "lisb" })
        {
            var top = suggest.ExtractTop(typed, cities, limit: 3, cutoff: 60);
            Console.WriteLine($"   {typed,-7} -> {string.Join(", ", top.Select(r => $"{r.Value} ({Show.Num(r.Score)})"))}");
        }
    }
}
