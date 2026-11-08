using Fuzzion;

namespace FuzzionExamples.Metrics;

/// <summary>
/// When you need a yes/no answer rather than a score. <c>ApproximatelyEquals</c> averages
/// the normalized similarity of the metrics you choose and compares it with a threshold.
/// It ignores case unless you ask otherwise.
/// </summary>
public static class ApproximatelyEqualsExample
{
    public static void Run()
    {
        Show.Heading("Named tolerances and their thresholds");
        foreach (var tolerance in new[] { SimilarityTolerance.Strong, SimilarityTolerance.Normal, SimilarityTolerance.Weak })
        {
            Console.WriteLine($"   {tolerance,-7} average similarity >= {Show.Num(StringSimilarity.Threshold(tolerance))}");
        }

        Show.Heading("Same person?");
        var pairs = new (string A, string B)[]
        {
            ("Jon Smith", "John Smith"), ("Jon Smith", "JOHN SMITH"), ("Jon Smith", "Joan Smithers"), ("Jon Smith", "Jane Doe"),
        };
        var metrics = new[] { SimilarityMetric.JaroWinkler, SimilarityMetric.Levenshtein };
        foreach (var (a, b) in pairs)
        {
            var average = StringSimilarity.AverageSimilarity(a, b, caseSensitive: false, metrics);
            Console.WriteLine($"   {a} / {b,-14} average {Show.Num(average),4}  " +
                              $"Strong: {a.ApproximatelyEquals(b, SimilarityTolerance.Strong, metrics),-5}  " +
                              $"Normal: {a.ApproximatelyEquals(b, SimilarityTolerance.Normal, metrics),-5}  " +
                              $"Weak: {a.ApproximatelyEquals(b, SimilarityTolerance.Weak, metrics)}");
        }

        Show.Heading("Your own threshold (0 to 1), case-sensitive or not");
        Console.WriteLine($"   \"Color\" ~ \"colour\" at 0.8, ignoring case:  {"Color".ApproximatelyEquals("colour", 0.8, SimilarityMetric.Levenshtein)}");
        Console.WriteLine($"   \"Color\" ~ \"colour\" at 0.8, case-sensitive: {"Color".ApproximatelyEquals("colour", 0.8, caseSensitive: true, SimilarityMetric.Levenshtein)}");

        Show.Heading("Every metric it can combine, on \"Jon Smith\" / \"John Smith\"");
        foreach (SimilarityMetric metric in Enum.GetValues(typeof(SimilarityMetric)))
        {
            Console.WriteLine($"   {metric,-25} {Show.Num(StringSimilarity.NormalizedSimilarity("Jon Smith", "John Smith", metric))}");
        }
    }
}
