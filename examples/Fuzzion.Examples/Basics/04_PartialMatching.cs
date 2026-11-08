using Fuzzion;
using Fuzzion.PreProcess;

namespace FuzzionExamples.Basics;

/// <summary>
/// When the shorter string should appear somewhere inside the longer one: a team name in
/// a headline, an invoice number in an email, a product name in a description.
/// </summary>
public static class PartialMatching
{
    public static void Run()
    {
        const string shortText = "Yankees";
        const string longText = "Tonight: New York Yankees at Boston Red Sox";

        Show.Heading("Ratio compares whole strings; PartialRatio looks for the best window");
        Console.WriteLine($"   Ratio:        {Show.Num(Fuzz.Ratio(shortText, longText))}");
        Console.WriteLine($"   PartialRatio: {Show.Num(Fuzz.PartialRatio(shortText, longText))}");

        Show.Heading("Where was it found? PartialRatioAlignment");
        // A typo in the short string: the best window is still found.
        const string query = "Red Socks";
        ScoreAlignment where = Fuzz.PartialRatioAlignment(query, longText);
        Console.WriteLine($"   score {Show.Num(where.Score)}");
        Console.WriteLine($"   query part: \"{query.Substring(where.SourceStart, where.SourceEnd - where.SourceStart)}\"");
        Console.WriteLine($"   text part:  \"{longText.Substring(where.DestStart, where.DestEnd - where.DestStart)}\" at {where.DestStart}..{where.DestEnd}");

        Show.Heading("ApproximateSubstringRatio: the best approximate occurrence, by Indel distance");
        // PartialRatio scores windows of the text that are as long as the shorter string.
        // ApproximateSubstringRatio lets the occurrence be any length and counts every
        // missing or extra character (Indel distance), relative to the shorter string.
        // It is usually stricter, and Indel.BestSubstringMatch tells you where the match ends.
        const string invoice = "invoice 12345";
        const string email = "Hi, please pay invoice no. 12345 by Friday.";
        Console.WriteLine($"   PartialRatio:              {Show.Num(Fuzz.PartialRatio(invoice, email))}");
        Console.WriteLine($"   ApproximateSubstringRatio: {Show.Num(Fuzz.ApproximateSubstringRatio(invoice, email))}   (4 extra characters: \"no. \")");
        Console.WriteLine($"   New York in \"We flew to Neww Yorrk\": PartialRatio {Show.Num(Fuzz.PartialRatio("New York", "We flew to Neww Yorrk"))}, " +
                          $"ApproximateSubstringRatio {Show.Num(Fuzz.ApproximateSubstringRatio("New York", "We flew to Neww Yorrk"))}");

        // The raw result: the Indel distance of the best match and the index of its last character.
        IndelSubstringMatch match = Indel.BestSubstringMatch(invoice.AsSpan(), email.AsSpan());
        Console.WriteLine($"   Indel.BestSubstringMatch: distance {match.Distance}, match ends at index {match.FirstBestEndIndex}" +
                          $" (\"...{email.Substring(0, match.FirstBestEndIndex + 1)}\")");

        Show.Heading("Preprocessing works here too");
        Console.WriteLine($"   PartialRatio(\"YANKEES!\", text) as-is:  {Show.Num(Fuzz.PartialRatio("YANKEES!", longText))}");
        Console.WriteLine($"   PartialRatio(\"YANKEES!\", text) + Full: {Show.Num(Fuzz.PartialRatio("YANKEES!", longText, StringPreprocessor.Full))}");
    }
}
