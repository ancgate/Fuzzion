using Fuzzion;
using Fuzzion.PreProcess;

namespace FuzzionExamples.Basics;

/// <summary>
/// "Token" scorers split strings into words first. Use them for names, titles, addresses
/// and anything else where the order or number of words can vary.
/// </summary>
public static class TokenScorers
{
    public static void Run()
    {
        Compare("Word order does not matter: TokenSortRatio",
            "Smith, John A.", "John A. Smith",
            ("Ratio", Fuzz.Ratio), ("TokenSortRatio", Fuzz.TokenSortRatio));

        Compare("Repeated or extra shared words matter less: TokenSetRatio",
            "mariners vs angels", "los angeles angels of anaheim vs seattle mariners",
            ("TokenSortRatio", Fuzz.TokenSortRatio), ("TokenSetRatio", Fuzz.TokenSetRatio));

        Compare("Both at once: TokenRatio (the better of the two)",
            "great gatsby the", "The Great Gatsby (1925 novel)",
            ("TokenSortRatio", Fuzz.TokenSortRatio), ("TokenSetRatio", Fuzz.TokenSetRatio), ("TokenRatio", Fuzz.TokenRatio));

        // Ratio says these are almost identical; TokenDifferenceRatio scores the words left
        // after removing the shared ones, so a shared prefix cannot hide the difference.
        Compare("Only the words that differ: TokenDifferenceRatio",
            "Acme Corp 2023 annual report", "Acme Corp 2024 annual report",
            ("Ratio", Fuzz.Ratio), ("TokenDifferenceRatio", Fuzz.TokenDifferenceRatio));

        Compare("Initials: TokenInitialismRatio",
            "IBM", "International Business Machines",
            ("Ratio", Fuzz.Ratio), ("TokenInitialismRatio", Fuzz.TokenInitialismRatio));

        Compare("Abbreviations: TokenAbbreviationRatio and PartialTokenAbbreviationRatio",
            "univ of calif", "University of California",
            ("Ratio", Fuzz.Ratio), ("TokenAbbreviationRatio", Fuzz.TokenAbbreviationRatio),
            ("PartialTokenAbbreviationRatio", Fuzz.PartialTokenAbbreviationRatio));

        Compare("Abbreviations of every word",
            "intl bus mach", "International Business Machines",
            ("Ratio", Fuzz.Ratio), ("PartialTokenAbbreviationRatio", Fuzz.PartialTokenAbbreviationRatio));

        Compare("Partial versions: one string is a few words of the other",
            "york yankees", "the new york yankees baseball club",
            ("TokenSortRatio", Fuzz.TokenSortRatio), ("PartialTokenSortRatio", Fuzz.PartialTokenSortRatio),
            ("PartialTokenSetRatio", Fuzz.PartialTokenSetRatio));
    }

    private static void Compare(string title, string a, string b, params (string Name, Func<string, string, Func<string, string>, double> Score)[] scorers)
    {
        Show.Heading(title);
        Console.WriteLine($"   \"{a}\"  vs  \"{b}\"");
        foreach (var (name, score) in scorers)
        {
            Console.WriteLine($"   {name,-30} {Show.Num(score(a, b, StringPreprocessor.Full)),6}");
        }
    }
}
