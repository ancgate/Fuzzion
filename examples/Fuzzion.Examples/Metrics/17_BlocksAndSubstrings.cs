using Fuzzion;

namespace FuzzionExamples.Metrics;

/// <summary>
/// Metrics built on blocks of characters that appear in both strings:
/// Ratcliff-Obershelp (Python's <c>difflib.SequenceMatcher.ratio()</c>), the longest
/// common substring (consecutive characters) and the longest common subsequence (in
/// order, gaps allowed).
/// </summary>
public static class BlocksAndSubstrings
{
    public static void Run()
    {
        Show.Heading("RatcliffObershelp: the same numbers as Python difflib");
        Console.WriteLine($"   Similarity(\"WIKIMEDIA\", \"WIKIMANIA\"): {Show.Num(RatcliffObershelp.Similarity("WIKIMEDIA", "WIKIMANIA"))}");
        Console.WriteLine($"   Distance  (\"WIKIMEDIA\", \"WIKIMANIA\"): {Show.Num(RatcliffObershelp.Distance("WIKIMEDIA", "WIKIMANIA"))}");

        Show.Heading("The matching blocks it found");
        const string a = "The quick brown fox";
        const string b = "The quick red fox";
        foreach (var block in RatcliffObershelp.MatchingBlocks(a, b).Where(m => m.Length > 0))
        {
            Console.WriteLine($"   a[{block.SourcePos}], b[{block.DestPos}], length {block.Length}: \"{a.Substring(block.SourcePos, block.Length)}\"");
        }

        // difflib's "autojunk" treats very common characters in long strings as junk;
        // it is off by default here and only has an effect on strings of 200+ characters.
        Console.WriteLine($"   autoJunk applies from {RatcliffObershelp.AutoJunkMinLength} characters");

        Show.Heading("LongestCommonSubstring: the longest run of identical characters");
        const string x = "invoice_2024_final_v2.pdf";
        const string y = "INV-2024-final_v3.pdf";
        CommonSubstring found = LongestCommonSubstring.Find(x, y);
        Console.WriteLine($"   Of:   \"{LongestCommonSubstring.Of(x, y)}\"");
        Console.WriteLine($"   Find: starts at {found.SourceStart} in x and {found.TargetStart} in y, length {found.Length}");
        Console.WriteLine($"   NormalizedSimilarity: {Show.Num(LongestCommonSubstring.NormalizedSimilarity(x, y))}");

        Show.Heading("LongestCommonSubsequence: shared characters in order, gaps allowed");
        Console.WriteLine($"   Of(\"AGGTAB\", \"GXTXAYB\"):         \"{LongestCommonSubsequence.Of("AGGTAB", "GXTXAYB")}\"");
        Console.WriteLine($"   Similarity (its length):          {LongestCommonSubsequence.Similarity("AGGTAB", "GXTXAYB")}");
        Console.WriteLine($"   Of(\"{x}\", \"{y}\"): \"{LongestCommonSubsequence.Of(x, y)}\"");
    }
}
