using Fuzzion;
using Fuzzion.SimilarityRatio;
using Fuzzion.PreProcess;
using Fuzzion.SimilarityRatio.Scorer.Composite;

namespace FuzzionExamples.Searching;

/// <summary>
/// Cleaning lists that contain the same thing written several ways.
/// <c>Process.Dedupe</c> keeps one entry per group (the longest), as TheFuzz's <c>dedupe</c>.
/// To see the groups themselves, build them from a <c>Cdist</c> matrix.
/// </summary>
public static class Deduplication
{
    public static void Run()
    {
        var names = new[]
        {
            "Frodo Baggins", "Frodo Baggin", "F. Baggins", "Samwise Gamgee", "Sam Gamgee",
            "Gandalf the Grey", "Gandalf", "Bilbo Baggins",
        };

        Show.Heading("Process.Dedupe with the default threshold (70, WeightedRatio)");
        Console.WriteLine("   " + string.Join(" | ", Process.Dedupe(names)));
        Console.WriteLine("   Too loose for these names: Bilbo Baggins was merged into Frodo Baggins.");

        Show.Heading("threshold: 80");
        Console.WriteLine("   " + string.Join(" | ", Process.Dedupe(names, threshold: 80)));

        Show.Heading("threshold: 90 is too strict: F. Baggins and Sam Gamgee stay");
        Console.WriteLine("   " + string.Join(" | ", Process.Dedupe(names, threshold: 90)));
        Console.WriteLine("   Try a few thresholds on your own data and check the result by eye.");

        Show.Heading("Groups instead of survivors: connect every pair scoring at least 85");
        // Unlike Process.Extract*, Cdist uses no preprocessing unless you pass one.
        var companies = new[]
        {
            "Acme Inc", "ACME Incorporated", "Acme, Inc.", "Globex Corp", "Globex Corporation",
            "Initech", "Initech LLC", "Umbrella Corp",
        };

        double[,] scores = Process.Cdist(companies, companies, ScorerCache.Get<WeightedRatioScorer>(), StringPreprocessor.Full, workers: -1);
        foreach (var group in Group(companies.Length, (i, j) => scores[i, j] >= 85))
        {
            Console.WriteLine("   { " + string.Join(", ", group.Select(i => companies[i])) + " }");
        }
    }

    /// <summary>Connected groups (union-find): items i and j are in the same group when linked(i, j).</summary>
    private static IEnumerable<List<int>> Group(int count, Func<int, int, bool> linked)
    {
        var parent = Enumerable.Range(0, count).ToArray();
        int Find(int i) => parent[i] == i ? i : parent[i] = Find(parent[i]);

        for (var i = 0; i < count; i++)
        {
            for (var j = i + 1; j < count; j++)
            {
                if (linked(i, j))
                {
                    parent[Find(i)] = Find(j);
                }
            }
        }

        return Enumerable.Range(0, count).GroupBy(Find).Select(g => g.ToList());
    }
}
