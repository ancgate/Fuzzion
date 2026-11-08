using Fuzzion;
using Fuzzion.PreProcess;
using Fuzzion.SimilarityRatio.Scorer;

namespace FuzzionExamples.Advanced;

/// <summary>
/// Three ways to plug your own scoring into searches: a function
/// (<c>Scorers.FromSimilarity</c>), a distance (<c>Scorers.FromDistance</c>), or a class
/// that implements <see cref="IRatioScorer"/>. All three work with <c>Process.*</c>,
/// pipelines, <c>Cdist</c> and <c>Dedupe</c>.
/// </summary>
public static class CustomScorers
{
    /// <summary>
    /// Takes the best of three scorers. Product names are often abbreviated (TokenSet),
    /// contained in longer names (Partial) or misspelled (Ratio).
    /// </summary>
    private sealed class BestOfScorer : IRatioScorer
    {
        public double Score(string input1, string input2) => Score(input1, input2, StringPreprocessor.None);

        public double Score(string input1, string input2, Func<string, string> preprocessor) =>
            Math.Max(Fuzz.TokenSetRatio(input1, input2, preprocessor),
                Math.Max(0.9 * Fuzz.PartialRatio(input1, input2, preprocessor), Fuzz.Ratio(input1, input2, preprocessor)));
    }

    public static void Run()
    {
        var products = new[] { "Logitech MX Master 3S Wireless Mouse", "Logitech MX Keys Keyboard", "Apple Magic Mouse", "Razer DeathAdder" };

        Show.Heading("1. A function: Scorers.FromSimilarity");
        // Score 0-100 from any formula; here an average of two metrics.
        var blend = Scorers.FromSimilarity((a, b) => 50 * JaroWinkler.Similarity(a, b) + 0.5 * Fuzz.TokenSetRatio(a, b));
        Show.Results(Process.ExtractTop("mx master mouse", products, scorer: blend, limit: 2));

        Show.Heading("2. A distance: Scorers.FromDistance (lower is better)");
        // Compare phone numbers by their digits only, allowing one wrong digit.
        static string Digits(string s) => new string(s.Where(char.IsDigit).ToArray());
        var phoneDistance = Scorers.FromDistance((a, b) => Levenshtein.Distance(Digits(a), Digits(b)), maxDistance: 1);
        var phones = new[] { "(555) 010-4477", "555.010.4478", "+1 555 999 0000" };
        Show.Results(Process.ExtractSorted("555-010-4477", phones, StringPreprocessor.None, phoneDistance));

        Show.Heading("3. A class implementing IRatioScorer");
        var bestOf = new BestOfScorer();
        Show.Results(Process.ExtractTop("magic mouse", products, scorer: bestOf, limit: 2));

        Show.Heading("Custom scorers work everywhere: pipelines, Cdist, Dedupe");
        var pipeline = Process.Configure().WithScorer(bestOf).Parallel().Build();
        Console.WriteLine($"   pipeline: {pipeline.ExtractOne("deathadder", products).Value}");
        var matrix = Process.Cdist(new[] { "mx keys" }, products, bestOf);
        Console.WriteLine($"   Cdist row: [{string.Join(", ", Enumerable.Range(0, products.Length).Select(j => Show.Num(matrix[0, j])))}]");
        Console.WriteLine($"   Dedupe: {string.Join(" | ", Process.Dedupe(new[] { "MX Master 3S", "Logitech MX Master 3S", "Magic Mouse" }, 85, bestOf))}");
    }
}
