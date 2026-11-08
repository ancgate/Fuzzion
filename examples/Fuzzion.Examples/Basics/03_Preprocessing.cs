using Fuzzion;
using Fuzzion.PreProcess;

namespace FuzzionExamples.Basics;

/// <summary>
/// A preprocessor cleans each string before it is scored. <c>Fuzz</c> methods use none
/// by default; <c>Process</c> methods use <see cref="StringPreprocessor.Full"/>.
/// A preprocessor is just a <c>Func&lt;string, string&gt;</c>, so you can write your own.
/// </summary>
public static class Preprocessing
{
    public static void Run()
    {
        const string messy = "  Crème BRÛLÉE -- Café  N°1!  ";

        Show.Heading("What each built-in preprocessor does");
        Console.WriteLine($"   None:             \"{StringPreprocessor.None(messy)}\"");
        Console.WriteLine($"   Full:             \"{StringPreprocessor.Full(messy)}\"   (lowercase, punctuation -> space, trimmed)");
        Console.WriteLine($"   FullUnicode:      \"{StringPreprocessor.FullUnicode(messy)}\"   (as RapidFuzz's default_process)");
        Console.WriteLine($"   RemoveDiacritics: \"{StringPreprocessor.RemoveDiacritics(messy)}\"");

        // Compose runs steps in order: here, remove accents and then clean up.
        var clean = StringPreprocessor.Compose(StringPreprocessor.RemoveDiacritics, StringPreprocessor.Full);
        Console.WriteLine($"   Compose(RemoveDiacritics, Full): \"{clean(messy)}\"");

        Show.Heading("The same comparison with different preprocessing");
        const string a = "Crème Brûlée!";
        const string b = "creme brulee";
        Console.WriteLine($"   Fuzz.Ratio as-is:             {Show.Num(Fuzz.Ratio(a, b))}");
        Console.WriteLine($"   with StringPreprocessor.Full: {Show.Num(Fuzz.Ratio(a, b, StringPreprocessor.Full))}");
        Console.WriteLine($"   with RemoveDiacritics + Full: {Show.Num(Fuzz.Ratio(a, b, clean))}");

        Show.Heading("Your own preprocessor");
        // Any Func<string, string> works. This one also drops a few filler words.
        var stopWords = new HashSet<string> { "the", "a", "an", "of" };
        Func<string, string> noFillers = s => string.Join(" ",
            StringPreprocessor.Full(s).Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(w => !stopWords.Contains(w)));

        const string title1 = "The Lord of the Rings";
        const string title2 = "Lord Rings";
        Console.WriteLine($"   \"{title1}\" -> \"{noFillers(title1)}\"");
        Console.WriteLine($"   Ratio with Full:      {Show.Num(Fuzz.Ratio(title1, title2, StringPreprocessor.Full))}");
        Console.WriteLine($"   Ratio with noFillers: {Show.Num(Fuzz.Ratio(title1, title2, noFillers))}");

        Show.Heading("Process uses Full by default; pass None to keep case and punctuation");
        var languages = new[] { "C#", "C++", "F#", "c" };
        Console.WriteLine("   Searching for \"C#\". Full removes '#' and '+', so C#, C++ and c all become \"c\":");
        Show.Results(Process.ExtractTop("C#", languages, limit: 3));
        Console.WriteLine("   With StringPreprocessor.None, only the exact name scores 100:");
        Show.Results(Process.ExtractTop("C#", languages, StringPreprocessor.None, limit: 3));
    }
}
