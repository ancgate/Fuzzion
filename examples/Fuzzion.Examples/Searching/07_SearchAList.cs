using Fuzzion;
using Fuzzion.PreProcess;
using Fuzzion.SimilarityRatio;
using Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

namespace FuzzionExamples.Searching;

/// <summary>
/// The <see cref="Process"/> methods search a list of choices. By default they score with
/// <c>WeightedRatio</c> after <c>StringPreprocessor.Full</c> (lowercase, no punctuation).
/// Each result has the matched <c>Value</c>, its <c>Score</c> and its <c>Index</c> in the list.
/// </summary>
public static class SearchAList
{
    private static readonly string[] Cities =
    {
        "New York", "Newark", "New Orleans", "Los Angeles", "Las Vegas",
        "San Francisco", "San Diego", "Santa Fe", "York", "Yonkers",
    };

    public static void Run()
    {
        Show.Heading("ExtractOne: the single best match");
        var best = Process.ExtractOne("new yrok", Cities);
        Console.WriteLine($"   {best.Value} (score {Show.Num(best.Score)}, index {best.Index})");

        Show.Heading("ExtractTop: the best few, highest first");
        Show.Results(Process.ExtractTop("san fran", Cities, limit: 3));

        Show.Heading("ExtractSorted: every choice, highest first (here with a cutoff of 50)");
        Show.Results(Process.ExtractSorted("las", Cities, cutoff: 50));

        Show.Heading("ExtractAll: every choice at or above the cutoff, in the original order");
        Show.Results(Process.ExtractAll("new", Cities, cutoff: 80));

        Show.Heading("No good match: ExtractOne returns null when nothing reaches the cutoff");
        var none = Process.ExtractOne("Chicago", Cities, cutoff: 80);
        Console.WriteLine(none == null ? "   null - ask the user instead of guessing" : $"   {none.Value}");

        Show.Heading("A different scorer and preprocessor");
        // ScorerCache.Get<T>() returns a shared instance of any built-in scorer.
        var tokenSort = ScorerCache.Get<TokenSortScorer>();
        var choices = new[] { "Doe, Jane", "Jane Doe-Smith", "John Doe" };
        Show.Results(Process.ExtractTop("jane doe", choices, StringPreprocessor.Full, tokenSort, limit: 3));

        Show.Heading("Without preprocessing, case and punctuation count");
        var withFull = Process.ExtractOne("NEWARK", Cities);
        var asIs = Process.ExtractOne("NEWARK", Cities, StringPreprocessor.None);
        Console.WriteLine($"   default (Full): {withFull.Value} ({Show.Num(withFull.Score)})");
        Console.WriteLine($"   None:           {asIs.Value} ({Show.Num(asIs.Score)})  <- \"NEWARK\" and \"Newark\" now differ");
    }
}
