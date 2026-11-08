using Fuzzion;

namespace FuzzionExamples.Metrics;

/// <summary>
/// Set metrics compare what two things contain, not the order. For strings they compare
/// q-grams (overlapping pieces of q characters, 2 by default). They also work on any
/// collection: words, tags, IDs. All return 0 to 1.
/// </summary>
public static class SetMetrics
{
    public static void Run()
    {
        Show.Heading("Q-grams: the pieces that are compared");
        Console.WriteLine($"   QGrams.Of(\"night\"):                {string.Join(" ", QGrams.Of("night"))}");
        Console.WriteLine($"   QGrams.Of(\"night\", q: 3):          {string.Join(" ", QGrams.Of("night", q: 3))}");
        Console.WriteLine($"   QGrams.Of(\"night\", pad: true):     {string.Join(" ", QGrams.Of("night", pad: true))}");

        Show.Heading("The four metrics on the same strings");
        var pairs = new (string A, string B)[] { ("night", "nacht"), ("context", "contact"), ("abcde", "cdeab") };
        var rows = new List<string[]> { new[] { "Pair", "Jaccard", "SorensenDice", "Overlap", "Tanimoto" } };
        foreach (var (a, b) in pairs)
        {
            rows.Add(new[]
            {
                $"{a} / {b}",
                Show.Num(Jaccard.Similarity(a, b)),
                Show.Num(SorensenDice.Similarity(a, b)),
                Show.Num(Overlap.Similarity(a, b)),
                Show.Num(Tanimoto.Similarity(a, b)),
            });
        }

        Show.Table(rows);
        Console.WriteLine("   Jaccard      = shared / all");
        Console.WriteLine("   SorensenDice = 2 x shared / (size A + size B)   (kinder to small overlaps)");
        Console.WriteLine("   Overlap      = shared / size of the smaller one  (1 when one contains the other)");
        Console.WriteLine("   Tanimoto     = like Jaccard, but repeated pieces count");

        Show.Heading("Options: q-gram length, padding, counting repeats");
        var options = new QGramOptions(q: 3, pad: true, mode: QGramMode.Multiset);
        Console.WriteLine($"   Jaccard(\"night\", \"nacht\", q: 3, pad: true, Multiset): {Show.Num(Jaccard.Similarity("night", "nacht", options))}");
        Console.WriteLine($"   Set vs Multiset on \"aaab\"/\"ab\": {Show.Num(Jaccard.Similarity("aaab", "ab"))} vs " +
                          $"{Show.Num(Jaccard.Similarity("aaab", "ab", new QGramOptions(mode: QGramMode.Multiset)))}");

        Show.Heading("Any collection: words, tags, IDs");
        var article1 = new[] { "dotnet", "csharp", "performance", "simd" };
        var article2 = new[] { "csharp", "simd", "vectorization" };
        Console.WriteLine($"   Jaccard(tags):      {Show.Num(Jaccard.Similarity(article1, article2))}");
        Console.WriteLine($"   SorensenDice(tags): {Show.Num(SorensenDice.Similarity(article1, article2))}");
        Console.WriteLine($"   Overlap(tags):      {Show.Num(Overlap.Similarity(article1, article2))}");

        var words1 = "the quick brown fox".Split(' ');
        var words2 = "the quick red fox jumps".Split(' ');
        Console.WriteLine($"   Jaccard(words):     {Show.Num(Jaccard.Similarity(words1, words2))}");

        var basket1 = new[] { 101, 101, 205, 330 };   // product IDs, 101 bought twice
        var basket2 = new[] { 101, 205, 205, 999 };
        Console.WriteLine($"   Tanimoto(baskets):  {Show.Num(Tanimoto.Similarity(basket1, basket2))}");
        Console.WriteLine($"   Jaccard distance:   {Show.Num(Jaccard.Distance(basket1, basket2))}   (= 1 - similarity)");
    }
}
