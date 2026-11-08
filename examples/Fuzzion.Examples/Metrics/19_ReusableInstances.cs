using System.Diagnostics;
using Fuzzion;
using Fuzzion.SimilarityRatio.Scorer.Composite;
using Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

namespace FuzzionExamples.Metrics;

/// <summary>
/// Comparing one string with many: create an instance for the fixed string. It prepares
/// the string once (for example its bit masks) and reuses that for every comparison.
/// Instances hold pooled memory, so dispose them (<c>using</c>).
/// </summary>
public static class ReusableInstances
{
    public static void Run()
    {
        var candidates = Enumerable.Range(0, 200_000).Select(i => $"order-{i:000000}-shipped").ToArray();
        const string query = "order-123456-shiped";

        Show.Heading("Levenshtein: static calls vs one prepared instance");
        var watch = Stopwatch.StartNew();
        var bestStatic = candidates.MinBy(c => Levenshtein.Distance(query, c));
        var staticTime = watch.Elapsed;

        watch.Restart();
        string bestInstance;
        using (var lev = new Levenshtein(query))
        {
            bestInstance = candidates.MinBy(c => lev.DistanceFrom(c));
        }

        var instanceTime = watch.Elapsed;
        Console.WriteLine($"   static:   {bestStatic}  {staticTime.TotalMilliseconds:0} ms");
        Console.WriteLine($"   instance: {bestInstance}  {instanceTime.TotalMilliseconds:0} ms");
        Console.WriteLine("   (timings vary by machine; the instance is usually faster)");

        Show.Heading("Every metric has an instance form");
        using (var indel = new Indel("chicago cubs"))
        using (var lcs = new LongestCommonSubsequence("chicago cubs"))
        using (var osa = new Osa("chicago cubs"))
        using (var damerau = new DamerauLevenshtein("chicago cubs"))
        using (var hamming = new Hamming("chicago cubs"))
        using (var jaro = new Jaro("chicago cubs"))
        using (var jaroWinkler = new JaroWinkler("chicago cubs"))
        {
            const string other = "chicago white sox";
            Console.WriteLine($"   Indel.DistanceFrom:                  {indel.DistanceFrom(other)}");
            Console.WriteLine($"   Indel.NormalizedSimilarityWith:      {Show.Num(indel.NormalizedSimilarityWith(other))}");
            Console.WriteLine($"   LongestCommonSubsequence.DistanceFrom: {lcs.DistanceFrom(other)}");
            Console.WriteLine($"   Osa.DistanceFrom:                    {osa.DistanceFrom(other)}");
            Console.WriteLine($"   DamerauLevenshtein.DistanceFrom:     {damerau.DistanceFrom(other)}");
            Console.WriteLine($"   Hamming.DistanceFrom:                {hamming.DistanceFrom(other)}");
            Console.WriteLine($"   Jaro.SimilarityWith:                 {Show.Num(jaro.SimilarityWith(other))}");
            Console.WriteLine($"   JaroWinkler.SimilarityWith:          {Show.Num(jaroWinkler.SimilarityWith(other))}");
        }

        Show.Heading("Cached Fuzz scorers: the same idea for 0-100 scores");
        using (var weighted = new CachedWeightedRatioScorer("new york mets vs atlanta braves"))
        using (var tokenSort = new CachedTokenSortScorer("new york mets vs atlanta braves"))
        {
            const string other = "atlanta braves vs new york mets";
            Console.WriteLine($"   CachedWeightedRatioScorer.Score: {Show.Num(weighted.Score(other))}");
            Console.WriteLine($"   CachedTokenSortScorer.Score:     {Show.Num(tokenSort.Score(other))}");
        }

        Console.WriteLine("   Other cached scorers: CachedDefaultRatioScorer (Ratio), CachedQRatioScorer, CachedPartialRatioScorer,");
        Console.WriteLine("   CachedTokenSetScorer, CachedTokenRatioScorer, CachedPartialTokenSortScorer, CachedPartialTokenSetScorer,");
        Console.WriteLine("   CachedPartialTokenRatioScorer, CachedTokenDifferenceScorer, CachedApproximateSubstringRatioScorer.");
    }
}
