using Fuzzion;
using Fuzzion.SimilarityRatio;
using Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

namespace FuzzionExamples.Searching;

/// <summary>
/// <c>Process.Cdist</c> scores every query against every choice and returns a matrix, as
/// RapidFuzz's <c>cdist</c>. It is the fastest way to match many queries at once: with
/// <c>Ratio</c>, <c>QRatio</c> or <c>Scorers.Levenshtein</c>, short choices are scored many
/// at a time with SIMD. <c>Process.Cpdist</c> scores pairs: first[i] with second[i].
/// </summary>
public static class ScoreMatrix
{
    public static void Run()
    {
        var queries = new[] { "apple", "banana", "cherry" };
        var choices = new[] { "appel", "bananna", "chery", "grape" };

        Show.Heading("Cdist: result[i, j] = score(queries[i], choices[j])  (default scorer: Ratio)");
        double[,] scores = Process.Cdist(queries, choices);
        PrintMatrix(queries, choices, scores);

        Show.Heading("The best choice for each query");
        for (var i = 0; i < queries.Length; i++)
        {
            var bestColumn = Enumerable.Range(0, choices.Length).OrderByDescending(j => scores[i, j]).First();
            Console.WriteLine($"   {queries[i],-8} -> {choices[bestColumn]} ({Show.Num(scores[i, bestColumn])})");
        }

        Show.Heading("Another scorer, a cutoff (lower scores become 0) and all CPU cores (workers: -1)");
        var tokenSort = ScorerCache.Get<TokenSortScorer>();
        var names = new[] { "John Smith", "Mary Jones" };
        var people = new[] { "Smith, John", "Jones Mary", "Jon Smith", "Marie Jonas" };
        PrintMatrix(names, people, Process.Cdist(names, people, tokenSort, scoreCutoff: 80, workers: -1));

        Show.Heading("Distances instead of scores: Scorers.Levenshtein(maxDistance: 2)");
        // Distances above maxDistance come back as maxDistance + 1 (here 3), as in RapidFuzz.
        PrintMatrix(queries, choices, Process.Cdist(queries, choices, Scorers.Levenshtein(maxDistance: 2)));

        Show.Heading("Cpdist: score row by row, for example old vs. new values of a column");
        var before = new[] { "123 Main Street", "45 Oak Ave", "9 Elm Road" };
        var after = new[] { "123 Main St", "45 Oak Avenue", "19 Elm Road" };
        double[] pairScores = Process.Cpdist(before, after, ScorerCache.Get<TokenSortScorer>());
        for (var i = 0; i < before.Length; i++)
        {
            Console.WriteLine($"   {before[i],-16} | {after[i],-14} {Show.Num(pairScores[i]),6}");
        }

        Show.Heading("Cancelling a large Cdist");
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        try
        {
            Process.Cdist(queries, choices, cancellationToken: cts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("   OperationCanceledException");
        }
    }

    private static void PrintMatrix(string[] rows, string[] columns, double[,] values)
    {
        var table = new List<string[]> { new[] { "" }.Concat(columns).ToArray() };
        for (var i = 0; i < rows.Length; i++)
        {
            table.Add(new[] { rows[i] }.Concat(Enumerable.Range(0, columns.Length).Select(j => Show.Num(values[i, j]))).ToArray());
        }

        Show.Table(table);
    }
}
