using System.Globalization;
using FuzzionExamples;
using FuzzionExamples.Advanced;
using FuzzionExamples.Basics;
using FuzzionExamples.Metrics;
using FuzzionExamples.Recipes;
using FuzzionExamples.Searching;

// Usage:
//   dotnet run --project examples/Fuzzion.Examples             list the examples
//   dotnet run --project examples/Fuzzion.Examples -- 7        run example 7
//   dotnet run --project examples/Fuzzion.Examples -- search   run every example whose name contains "search"
//   dotnet run --project examples/Fuzzion.Examples -- all      run every example

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
Console.OutputEncoding = System.Text.Encoding.UTF8;

var examples = new (string Category, string Name, string Title, Action Run)[]
{
    ("Basics", "quick-start", "Quick start", QuickStart.Run),
    ("Basics", "choosing-a-scorer", "Every Fuzz scorer side by side", ChoosingAScorer.Run),
    ("Basics", "preprocessing", "Cleaning text before comparing", Preprocessing.Run),
    ("Basics", "partial-matching", "Finding one string inside another", PartialMatching.Run),
    ("Basics", "token-scorers", "Word order, duplicates, abbreviations, initialisms", TokenScorers.Run),
    ("Basics", "cutoffs-and-spans", "Score cutoffs and ReadOnlySpan<char> input", CutoffsAndSpans.Run),

    ("Searching", "search-a-list", "Best matches in a list of strings", SearchAList.Run),
    ("Searching", "search-objects", "Best matches in a list of objects", SearchObjects.Run),
    ("Searching", "pipelines", "Reusable, cached and parallel search pipelines", Pipelines.Run),
    ("Searching", "distance-search", "Searching by distance (typos allowed)", DistanceSearch.Run),
    ("Searching", "score-matrix", "Every query against every choice: Cdist and Cpdist", ScoreMatrix.Run),
    ("Searching", "deduplication", "Removing and grouping near-duplicates", Deduplication.Run),

    ("Metrics", "edit-distances", "Levenshtein, Indel, LCS, OSA, Damerau-Levenshtein, Hamming", EditDistances.Run),
    ("Metrics", "jaro-winkler", "Jaro and Jaro-Winkler for names", JaroWinklerNames.Run),
    ("Metrics", "prefix-postfix", "Shared beginnings and endings", PrefixPostfix.Run),
    ("Metrics", "set-metrics", "Jaccard, Sorensen-Dice, Overlap, Tanimoto and q-grams", SetMetrics.Run),
    ("Metrics", "blocks-and-substrings", "Ratcliff-Obershelp (difflib) and common substrings", BlocksAndSubstrings.Run),
    ("Metrics", "approximately-equals", "Yes/no answers from several metrics", ApproximatelyEqualsExample.Run),
    ("Metrics", "reusable-instances", "Comparing one string with many, faster", ReusableInstances.Run),
    ("Metrics", "generic-sequences", "Comparing words, numbers and other sequences", GenericSequences.Run),

    ("Advanced", "edit-operations", "The edits that turn one string into another", EditOperations.Run),
    ("Advanced", "unicode", "Accents, emoji and other Unicode text", UnicodeText.Run),
    ("Advanced", "custom-scorers", "Writing your own scorer", CustomScorers.Run),

    ("Recipes", "did-you-mean", "\"Did you mean ...?\" spelling suggestions", DidYouMean.Run),
    ("Recipes", "company-names", "Matching company names", CompanyNames.Run),
    ("Recipes", "record-linkage", "Matching customer records on several fields", RecordLinkage.Run),
    ("Recipes", "autocomplete", "Fuzzy autocomplete while the user types", Autocomplete.Run),
    ("Recipes", "diff-highlighter", "Highlighting what changed between two texts", DiffHighlighter.Run),
    ("Recipes", "canonical-values", "Mapping messy input to known values", CanonicalValues.Run),
    ("Recipes", "log-grouping", "Grouping similar log and error messages", LogGrouping.Run),
};

var selection = args.Length == 0 ? null : string.Join(" ", args).Trim().ToLowerInvariant();

if (selection == null)
{
    Console.WriteLine("Fuzzion examples. Run one with:  dotnet run --project examples/Fuzzion.Examples -- <number or name>");
    Console.WriteLine("Run all with:                    dotnet run --project examples/Fuzzion.Examples -- all");
    string category = null;
    for (var i = 0; i < examples.Length; i++)
    {
        if (examples[i].Category != category)
        {
            category = examples[i].Category;
            Console.WriteLine();
            Console.WriteLine(category);
        }

        Console.WriteLine($"  {i + 1,2}  {examples[i].Name,-22} {examples[i].Title}");
    }

    return 0;
}

var chosen = examples
    .Select((e, i) => (e.Name, e.Title, e.Run, Number: i + 1))
    .Where(e => selection == "all" || selection == e.Number.ToString() || e.Name.Contains(selection))
    .ToList();

if (chosen.Count == 0)
{
    Console.Error.WriteLine($"No example matches \"{selection}\". Run without arguments to see the list.");
    return 1;
}

foreach (var example in chosen)
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 78));
    Console.WriteLine($"{example.Number}. {example.Title}  [{example.Name}]");
    Console.WriteLine(new string('=', 78));
    example.Run();
}

return 0;
