using System.Text.RegularExpressions;
using Fuzzion;
using Fuzzion.SimilarityRatio;
using Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

namespace FuzzionExamples.Recipes;

/// <summary>
/// Grouping error messages that differ only in IDs, numbers and small wording changes, to
/// see which problems happen most. Variable parts are masked first; then each message
/// joins the first group whose example is similar enough, or starts a new group.
/// </summary>
public static class LogGrouping
{
    private static readonly Regex Variable = new(
        @"[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}|0x[0-9a-f]+|\d+(\.\d+)*",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static void Run()
    {
        var log = new[]
        {
            "Timeout after 30000 ms calling PaymentService for order 18823",
            "Timeout after 30000ms calling PaymentService for order 18990",
            "User 4411 not found",
            "Timeout after 15000 ms calling InventoryService for order 18991",
            "user 9921 not found",
            "Failed to parse config file /etc/app/config.yaml at line 12",
            "Timeout after 30000 ms calling PaymentService (order 19001)",
            "Could not parse config file /etc/app/config.yaml at line 40",
            "User 77 was not found",
            "Disk usage at 91.5% on volume /data",
        };

        var scorer = ScorerCache.Get<TokenSortScorer>();
        var groups = new List<(string Example, string Masked, List<string> Messages)>();

        foreach (var message in log)
        {
            var masked = Variable.Replace(message, "#");
            var best = Process.ExtractOne(masked, groups.Select(g => g.Masked), scorer: scorer, cutoff: 80);
            if (best == null)
            {
                groups.Add((message, masked, new List<string> { message }));
            }
            else
            {
                groups[best.Index].Messages.Add(message);
            }
        }

        foreach (var group in groups.OrderByDescending(g => g.Messages.Count))
        {
            Console.WriteLine($"   {group.Messages.Count} x  {group.Masked}");
        }
    }
}
