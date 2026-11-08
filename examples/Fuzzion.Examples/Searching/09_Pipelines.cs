using Fuzzion;
using Fuzzion.SimilarityRatio;
using Fuzzion.SimilarityRatio.Scorer.Composite;
using Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

namespace FuzzionExamples.Searching;

/// <summary>
/// <c>Process.Configure()</c> builds a reusable search pipeline: pick the scorer once,
/// optionally cache the query's preparation and search in parallel. Pipelines are
/// immutable and safe to share, for example as a singleton in a web app.
/// </summary>
public static class Pipelines
{
    public static void Run()
    {
        var choices = Enumerable.Range(0, 20_000).Select(i => $"customer {i:00000} account").ToList();
        choices.Add("Acme Corporation account");

        Show.Heading("Basic pipeline: same results as the static Process methods");
        var pipeline = Process.Configure().Build();
        var r1 = pipeline.ExtractOne("cowboys", "Atlanta Falcons", "New York Jets", "Dallas Cowboys");
        Console.WriteLine($"   {r1.Value} ({Show.Num(r1.Score)})");

        Show.Heading("Choose the scorer once");
        var tokenSet = Process.Configure().WithScorer(ScorerCache.Get<TokenSetScorer>()).Build();
        var r2 = tokenSet.ExtractOne("acme corp account", choices);
        Console.WriteLine($"   {r2.Value} ({Show.Num(r2.Score)})");

        Show.Heading("Cached(): prepare the query once per search, then score every choice fast");
        var cached = Process.Configure().Cached().Build();
        Console.WriteLine($"   {cached.ExtractOne("acme corporation", choices).Value}");

        Show.Heading("Parallel(): use several cores for large lists");
        var parallel = Process.Configure()
            .Cached()
            .Parallel(new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount })
            .Build();
        Show.Results(parallel.ExtractTop("customer 12345", choices, limit: 3));

        Show.Heading("Your own cached scorer: one query, many choice lists");
        // The scorer is prepared for the query once and reused for every call below.
        using (var scorer = new CachedWeightedRatioScorer("acme corporation"))
        {
            var forThisQuery = Process.Configure().Cached(scorer).Build();
            var batch1 = new[] { "ACME Corp.", "Apex Corporation", "Acme Co" };
            var batch2 = new[] { "acme corporation ltd", "Acne Corporation" };
            Console.WriteLine($"   batch 1: {forThisQuery.ExtractOne(batch1).Value}");
            Console.WriteLine($"   batch 2: {forThisQuery.ExtractOne(batch2).Value}");
        }

        Show.Heading("Cancelling a long parallel search");
        using var cts = new CancellationTokenSource();
        var cancellable = Process.Configure().Parallel(new ParallelOptions { CancellationToken = cts.Token }).Build();
        cts.Cancel(); // for example when the user types another letter
        try
        {
            cancellable.ExtractAll("customer", choices).ToList();
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("   OperationCanceledException: the search was cancelled");
        }
    }
}
