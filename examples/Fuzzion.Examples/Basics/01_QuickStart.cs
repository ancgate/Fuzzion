using Fuzzion;

namespace FuzzionExamples.Basics;

/// <summary>
/// The five calls most people need: score two strings, ignore word order, find the best
/// match in a list, find the top few, and count typos.
/// </summary>
public static class QuickStart
{
    public static void Run()
    {
        // 1. How similar are two strings? 0 = nothing in common, 100 = identical.
        double score = Fuzz.Ratio("this is a test", "this is a test!");
        Console.WriteLine($"Ratio:          {Show.Num(score)}");

        // 2. The same words in a different order.
        double sorted = Fuzz.TokenSortRatio("fuzzy wuzzy was a bear", "wuzzy fuzzy was a bear");
        Console.WriteLine($"TokenSortRatio: {Show.Num(sorted)}");

        // 3. The best match in a list. Process ignores case and punctuation by default.
        var teams = new[] { "Atlanta Falcons", "New York Jets", "New York Giants", "Dallas Cowboys" };
        var best = Process.ExtractOne("cowboys", teams);
        Console.WriteLine($"Best match for \"cowboys\": {best.Value} (score {Show.Num(best.Score)}, index {best.Index})");

        // 4. The top 2 matches.
        Console.WriteLine("Top 2 for \"new york\":");
        Show.Results(Process.ExtractTop("new york", teams, limit: 2));

        // 5. How many single-character edits apart? (insert, delete or replace)
        int edits = Levenshtein.Distance("kitten", "sitting");
        Console.WriteLine($"Levenshtein distance kitten -> sitting: {edits}");
    }
}
