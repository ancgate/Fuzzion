using Fuzzion;

namespace FuzzionExamples.Metrics;

/// <summary>
/// Edit distances count how many small changes turn one string into the other. They
/// differ in which changes are allowed. Every metric has the same four functions:
/// <c>Distance</c>, <c>Similarity</c>, <c>NormalizedDistance</c> and
/// <c>NormalizedSimilarity</c> (0 to 1).
/// </summary>
public static class EditDistances
{
    public static void Run()
    {
        var pairs = new (string A, string B)[]
        {
            ("kitten", "sitting"), ("recieve", "receive"), ("CA", "ABC"), ("karolin", "kathrin"), ("abc", "abcd"),
        };

        var metrics = new (string Name, Func<string, string, int> Distance, string Allows)[]
        {
            ("Levenshtein", (a, b) => Levenshtein.Distance(a, b), "insert, delete, replace"),
            ("Indel", Indel.Distance, "insert, delete (a replace costs 2)"),
            ("LongestCommonSubsequence", LongestCommonSubsequence.Distance, "max length - common subsequence"),
            ("Osa", Osa.Distance, "Levenshtein + swap neighbours (each part edited once)"),
            ("DamerauLevenshtein", DamerauLevenshtein.Distance, "Levenshtein + swap neighbours, no restriction"),
            ("Hamming", (a, b) => Hamming.Distance(a, b), "replace only; extra length counts as differences"),
        };

        Show.Heading("Distance (number of edits) for each metric");
        var rows = new List<string[]> { new[] { "Metric" }.Concat(pairs.Select(p => $"{p.A}/{p.B}")).ToArray() };
        rows.AddRange(metrics.Select(m => new[] { m.Name }.Concat(pairs.Select(p => m.Distance(p.A, p.B).ToString())).ToArray()));
        Show.Table(rows);

        Show.Heading("What each one allows");
        foreach (var m in metrics)
        {
            Console.WriteLine($"   {m.Name,-26} {m.Allows}");
        }

        Show.Heading("Normalized similarity (0 = nothing in common, 1 = identical)");
        Console.WriteLine($"   Levenshtein kitten/sitting:        {Show.Num(Levenshtein.NormalizedSimilarity("kitten", "sitting"))}");
        Console.WriteLine($"   Indel kitten/sitting:              {Show.Num(Indel.NormalizedSimilarity("kitten", "sitting"))}   (= Fuzz.Ratio / 100)");
        Console.WriteLine($"   DamerauLevenshtein recieve/receive: {Show.Num(DamerauLevenshtein.NormalizedSimilarity("recieve", "receive"))}");
        Console.WriteLine($"   Osa recieve/receive:               {Show.Num(Osa.NormalizedSimilarity("recieve", "receive"))}");

        Show.Heading("Weighted Levenshtein: choose the cost of each kind of edit");
        Console.WriteLine($"   all costs 1:             {Levenshtein.Distance("kitten", "sitting")}");
        Console.WriteLine($"   replaceCost: 2 (=Indel): {Levenshtein.Distance("kitten", "sitting", replaceCost: 2)}");
        Console.WriteLine($"   insertCost: 5:           {Levenshtein.Distance("kitten", "sitting", insertCost: 5)}");

        Show.Heading("Hamming for codes of equal length; pad: false rejects different lengths");
        Console.WriteLine($"   Hamming(\"1011101\", \"1001001\"): {Hamming.Distance("1011101", "1001001")}");
        try
        {
            Hamming.Distance("abc", "abcd", pad: false);
        }
        catch (ArgumentException)
        {
            Console.WriteLine("   Hamming(\"abc\", \"abcd\", pad: false): ArgumentException (lengths differ)");
        }

        Show.Heading("Which one to use?");
        Console.WriteLine("   Typos from fast typing (swapped letters): DamerauLevenshtein or Osa");
        Console.WriteLine("   General spelling differences:            Levenshtein");
        Console.WriteLine("   Same scale as Fuzz.Ratio:                 Indel");
        Console.WriteLine("   Fixed-length codes, hashes, DNA reads:    Hamming");
    }
}
