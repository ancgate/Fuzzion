using Fuzzion;
using Fuzzion.PreProcess;
using Fuzzion.SimilarityRatio;
using Fuzzion.SimilarityRatio.Scorer.Composite;

namespace FuzzionExamples.Advanced;

/// <summary>
/// Accents, emoji and other Unicode. .NET strings are UTF-16: characters outside the Basic
/// Multilingual Plane (most emoji, some CJK characters) take two <c>char</c>s, so every
/// metric counts them twice. <see cref="CodePoints"/> counts each character once, as
/// Python and RapidFuzz do.
/// </summary>
public static class UnicodeText
{
    public static void Run()
    {
        Show.Heading("Removing accents");
        foreach (var text in new[] { "Zoë Saldaña", "Crème brûlée", "Straße", "Ångström", "Dvořák" })
        {
            Console.WriteLine($"   {text,-13} -> {StringPreprocessor.RemoveDiacritics(text)}");
        }

        var clean = StringPreprocessor.Compose(StringPreprocessor.RemoveDiacritics, StringPreprocessor.Full);
        Console.WriteLine($"   Fuzz.Ratio(\"Dvořák\", \"dvorak\") as-is:                 {Show.Num(Fuzz.Ratio("Dvořák", "dvorak"))}");
        Console.WriteLine($"   Fuzz.Ratio(\"Dvořák\", \"dvorak\") RemoveDiacritics+Full: {Show.Num(Fuzz.Ratio("Dvořák", "dvorak", clean))}");

        Show.Heading("Full vs FullUnicode");
        foreach (var text in new[] { "İSTANBUL", "x² + ½", "Ⅻ chapters" })
        {
            Console.WriteLine($"   {text,-11} Full: \"{StringPreprocessor.Full(text)}\"  FullUnicode: \"{StringPreprocessor.FullUnicode(text)}\"");
        }

        Console.WriteLine("   FullUnicode matches RapidFuzz's default_process: it keeps all numbers and");
        Console.WriteLine("   lowercases every letter the same way on every .NET runtime.");

        Show.Heading("Emoji: one character or two?");
        const string a = "😀abc";
        const string b = "😀abd";
        Console.WriteLine($"   \"{a}\".Length = {a.Length}, CodePoints.Count = {CodePoints.Count(a)}");
        Console.WriteLine($"   Fuzz.Ratio:                               {Show.Num(Fuzz.Ratio(a, b))}  (emoji counted as 2)");
        Console.WriteLine($"   CodePoints.Compare(a, b, Fuzz.Ratio):     {Show.Num(CodePoints.Compare(a, b, Fuzz.Ratio))}  (as RapidFuzz)");
        Console.WriteLine($"   Levenshtein \"👍🏽\" vs \"👍\": {Levenshtein.Distance("👍🏽", "👍")} as UTF-16, " +
                          $"{CodePoints.Compare("👍🏽", "👍", (x, y) => Levenshtein.Distance(x, y))} with CodePoints.Compare (the skin tone is one character)");
        Console.WriteLine($"   CodePoints.Of(\"a😀\"): [{string.Join(", ", CodePoints.Of("a😀").Select(c => $"U+{c:X4}"))}]");

        Show.Heading("Searching with code-point scoring");
        var scorer = CodePoints.Scorer(ScorerCache.Get<WeightedRatioScorer>());
        var reactions = new[] { "😀 happy", "😢 sad", "😡 angry", "🎉 party" };
        Show.Results(Process.ExtractTop("😀 hapy", reactions, scorer: scorer, limit: 2));
    }
}
