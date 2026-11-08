using Fuzzion;
using Fuzzion.PreProcess;

namespace FuzzionExamples.Basics;

/// <summary>
/// Runs every <see cref="Fuzz"/> scorer on pairs that show a typical difference, so you
/// can see which scorer tolerates what. All of them return 0-100.
/// </summary>
public static class ChoosingAScorer
{
    private delegate double FuzzScorer(string a, string b, Func<string, string> preprocessor);

    public static void Run()
    {
        var pairs = new (string Label, string A, string B)[]
        {
            ("A typo", "New York Mets", "New Yrok Mets"),
            ("B order", "New York Mets vs Atlanta Braves", "Atlanta Braves vs New York Mets"),
            ("C repeats", "fuzzy was a bear", "fuzzy fuzzy was a bear"),
            ("D inside", "Yankees", "New York Yankees"),
            ("E abbrev", "NY Yankees", "New York Yankees"),
            ("F initials", "HP", "Hewlett Packard"),
            ("G extra", "Apple iPhone 15", "Apple iPhone 15 Pro Max 256GB Black"),
        };

        foreach (var (label, a, b) in pairs)
        {
            Console.WriteLine($"   {label,-10}  \"{a}\"  vs  \"{b}\"");
        }

        var scorers = new (string Name, FuzzScorer Score, string UseWhen)[]
        {
            ("Ratio", Fuzz.Ratio, "plain similarity of the whole strings"),
            ("QRatio", Fuzz.QRatio, "like Ratio, but empty input scores 0"),
            ("PartialRatio", Fuzz.PartialRatio, "the shorter string may sit inside the longer"),
            ("ApproximateSubstringRatio", Fuzz.ApproximateSubstringRatio, "best approximate occurrence of the shorter string"),
            ("TokenSortRatio", Fuzz.TokenSortRatio, "word order does not matter"),
            ("TokenSetRatio", Fuzz.TokenSetRatio, "repeated and extra shared words matter less"),
            ("TokenRatio", Fuzz.TokenRatio, "best of TokenSort and TokenSet"),
            ("PartialTokenSortRatio", Fuzz.PartialTokenSortRatio, "word order + substring"),
            ("PartialTokenSetRatio", Fuzz.PartialTokenSetRatio, "100 as soon as one word is shared"),
            ("PartialTokenRatio", Fuzz.PartialTokenRatio, "best of the two partial token scores"),
            ("TokenDifferenceRatio", Fuzz.TokenDifferenceRatio, "compares only the words that differ"),
            ("PartialTokenDifferenceRatio", Fuzz.PartialTokenDifferenceRatio, "partial version of TokenDifference"),
            ("TokenInitialismRatio", Fuzz.TokenInitialismRatio, "\"HP\" vs \"Hewlett Packard\""),
            ("PartialTokenInitialismRatio", Fuzz.PartialTokenInitialismRatio, "initials of some of the words"),
            ("TokenAbbreviationRatio", Fuzz.TokenAbbreviationRatio, "\"univ of calif\" vs \"University of California\""),
            ("PartialTokenAbbreviationRatio", Fuzz.PartialTokenAbbreviationRatio, "abbreviations of some of the words"),
            ("WeightedRatio", Fuzz.WeightedRatio, "good general default (what Process uses)"),
        };

        // StringPreprocessor.Full lowercases and removes punctuation, so the table shows
        // only how the scorers differ. Without it, "NY" and "ny" would differ too.
        var rows = new List<string[]> { new[] { "Scorer" }.Concat(pairs.Select(p => p.Label.Substring(0, 1))).ToArray() };
        foreach (var (name, score, _) in scorers)
        {
            rows.Add(new[] { name }.Concat(pairs.Select(p => Show.Num(score(p.A, p.B, StringPreprocessor.Full)))).ToArray());
        }

        Console.WriteLine();
        Show.Table(rows);

        Show.Heading("When to use which");
        foreach (var (name, _, useWhen) in scorers)
        {
            Console.WriteLine($"   {name,-30} {useWhen}");
        }
    }
}
