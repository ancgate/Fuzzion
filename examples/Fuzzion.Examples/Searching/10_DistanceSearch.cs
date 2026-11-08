using Fuzzion;

namespace FuzzionExamples.Searching;

/// <summary>
/// Sometimes "at most 2 typos" is easier to reason about than "a score of 85". Wrap a
/// distance with <see cref="Scorers"/> and the searches return the closest choices first
/// (lowest distance), dropping anything further than <c>maxDistance</c>.
/// </summary>
public static class DistanceSearch
{
    private static readonly string[] Words =
    {
        "apple", "apply", "ample", "maple", "apples", "applet", "pineapple", "banana", "grape",
    };

    public static void Run()
    {
        Show.Heading("Scorers.Levenshtein(maxDistance: 1): words at most one edit from \"aple\"");
        // Result scores are distances here: 0 = identical, 1 = one edit, ...
        Show.Results(Process.ExtractSorted("aple", Words, scorer: Scorers.Levenshtein(maxDistance: 1)));

        Show.Heading("maxDistance: 2");
        Show.Results(Process.ExtractSorted("aple", Words, scorer: Scorers.Levenshtein(maxDistance: 2)));

        Show.Heading("Swapped letters: \"appel\" is 2 Levenshtein edits from \"apple\" ...");
        Show.Results(Process.ExtractSorted("appel", Words, scorer: Scorers.Levenshtein(maxDistance: 1)));
        Console.WriteLine("   ... but 1 Damerau-Levenshtein edit. Any distance function works with Scorers.FromDistance:");
        var damerau = Scorers.FromDistance(DamerauLevenshtein.Distance, maxDistance: 1);
        Show.Results(Process.ExtractSorted("appel", Words, scorer: damerau));

        Show.Heading("ExtractOne with a distance scorer returns the closest choice");
        var closest = Process.ExtractOne("banan", Words, scorer: Scorers.Levenshtein());
        Console.WriteLine($"   {closest.Value} (distance {closest.Score})");

        Show.Heading("Any similarity function: Scorers.FromSimilarity");
        // Similarity scorers work like the built-in ones: higher is better, 0-100 is usual.
        var jaroWinkler = Scorers.FromSimilarity((a, b) => 100 * JaroWinkler.Similarity(a, b));
        Show.Results(Process.ExtractTop("aple", Words, scorer: jaroWinkler, limit: 3));
    }
}
