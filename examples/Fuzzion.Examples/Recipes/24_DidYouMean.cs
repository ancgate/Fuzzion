using Fuzzion;

namespace FuzzionExamples.Recipes;

/// <summary>
/// "Did you mean ...?" for commands, search terms or field names. Uses
/// Damerau-Levenshtein, so a swapped pair of letters ("stauts") counts as one typo, and
/// allows more typos in longer words. Jaro-Winkler breaks ties.
/// </summary>
public static class DidYouMean
{
    private static readonly string[] Commands =
    {
        "status", "commit", "checkout", "branch", "merge", "rebase", "push", "pull", "fetch", "stash", "clone", "config",
    };

    public static void Run()
    {
        foreach (var typed in new[] { "stauts", "comit", "chekcout", "pul", "brnach", "confg", "deploy", "PUSH" })
        {
            var suggestions = Suggest(typed, Commands);
            Console.WriteLine(suggestions.Count == 0
                ? $"   {typed,-9} -> unknown command"
                : $"   {typed,-9} -> did you mean {string.Join(" or ", suggestions.Select(s => $"'{s}'"))}?");
        }
    }

    /// <summary>Known words within the allowed number of typos, closest first.</summary>
    public static List<string> Suggest(string typed, IEnumerable<string> known, int max = 2)
    {
        var input = typed.ToLowerInvariant();

        // One typo for short words, two for longer ones.
        var allowed = input.Length <= 4 ? 1 : 2;

        return known
            .Select(word => (Word: word, Typos: DamerauLevenshtein.Distance(input, word)))
            .Where(x => x.Typos <= allowed)
            .OrderBy(x => x.Typos)
            .ThenByDescending(x => JaroWinkler.Similarity(input, x.Word))
            .Take(max)
            .Select(x => x.Word)
            .ToList();
    }
}
