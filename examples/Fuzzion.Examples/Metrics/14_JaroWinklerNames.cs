using Fuzzion;

namespace FuzzionExamples.Metrics;

/// <summary>
/// Jaro and Jaro-Winkler were designed for short strings such as people's names.
/// Jaro-Winkler adds a bonus when the strings start the same way, because people rarely
/// misspell the start of a name. Both return 0 to 1.
/// </summary>
public static class JaroWinklerNames
{
    public static void Run()
    {
        var pairs = new (string A, string B)[]
        {
            ("MARTHA", "MARHTA"), ("DWAYNE", "DUANE"), ("DIXON", "DICKSONX"), ("Jonathon", "Jonathan"),
            ("Catherine", "Katherine"), ("Smith", "Smyth"), ("Robert", "Bob"),
        };

        var rows = new List<string[]> { new[] { "Pair", "Jaro", "JaroWinkler", "Ratio/100" } };
        foreach (var (a, b) in pairs)
        {
            rows.Add(new[]
            {
                $"{a} / {b}",
                Show.Num(Jaro.Similarity(a, b)),
                Show.Num(JaroWinkler.Similarity(a, b)),
                Show.Num(Fuzz.Ratio(a, b) / 100),
            });
        }

        Show.Table(rows);

        Show.Heading("prefixWeight: how much a shared start counts (default 0.1, at most 0.25)");
        foreach (var weight in new[] { 0.0, 0.1, 0.25 })
        {
            Console.WriteLine($"   prefixWeight {weight,-4}: {Show.Num(JaroWinkler.Similarity("Jonathon", "Jonathan", prefixWeight: weight))}");
        }

        Show.Heading("Matching a name against a list (prepared once, see also example 19)");
        using var query = new JaroWinkler("Katharine");
        foreach (var name in new[] { "Katherine", "Catherine", "Kathryn", "Karen", "Kate" })
        {
            Console.WriteLine($"   {name,-10} {Show.Num(query.SimilarityWith(name))}");
        }

        Console.WriteLine();
        Console.WriteLine("   Note: nicknames such as Robert/Bob are not spelling variants; use a lookup table for those.");
    }
}
