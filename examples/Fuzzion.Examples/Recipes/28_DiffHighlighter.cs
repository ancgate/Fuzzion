using Fuzzion;
using Fuzzion.Edits;

namespace FuzzionExamples.Recipes;

/// <summary>
/// Shows what changed between two texts, word by word or character by character, using
/// the generic <c>GetOpCodes&lt;T&gt;</c>. Deleted text is shown as [-old-], added text as
/// {+new+}. Swap the markers for HTML tags (&lt;del&gt;, &lt;ins&gt;) to show it on a page.
/// </summary>
public static class DiffHighlighter
{
    public static void Run()
    {
        const string before = "The quick brown fox jumps over the lazy dog.";
        const string after = "The quick red fox jumped over the very lazy dog.";

        Show.Heading("Word by word");
        Console.WriteLine("   " + WordDiff(before, after));

        Show.Heading("Character by character");
        Console.WriteLine("   " + CharDiff("color", "colour"));
        Console.WriteLine("   " + CharDiff("Jonh Smiht", "John Smith"));

        Show.Heading("How different are they?");
        var a = before.Split(' ');
        var b = after.Split(' ');
        Console.WriteLine($"   {Levenshtein.Distance<string>(a, b)} of {Math.Max(a.Length, b.Length)} words changed, " +
                          $"word similarity {Show.Num(Indel.NormalizedSimilarity<string>(a, b))}");
    }

    public static string WordDiff(string before, string after)
    {
        var a = before.Split(' ');
        var b = after.Split(' ');
        return Highlight(a, b, Levenshtein.GetOpCodes<string>(a, b), words => string.Join(" ", words));
    }

    public static string CharDiff(string before, string after) =>
        Highlight(before.ToCharArray(), after.ToCharArray(), Levenshtein.GetOpCodes(before, after), chars => new string(chars.ToArray()));

    private static string Highlight<T>(IReadOnlyList<T> a, IReadOnlyList<T> b, OpCodes codes, Func<IEnumerable<T>, string> join)
    {
        var parts = new List<string>();
        foreach (OpCode code in codes)
        {
            var removed = join(a.Skip(code.SourceBegin).Take(code.SourceEnd - code.SourceBegin));
            var added = join(b.Skip(code.DestBegin).Take(code.DestEnd - code.DestBegin));
            parts.Add(code.EditType switch
            {
                EditType.EQUAL => removed,
                EditType.DELETE => $"[-{removed}-]",
                EditType.INSERT => $"{{+{added}+}}",
                _ => $"[-{removed}-]{{+{added}+}}",
            });
        }

        return string.Join(typeof(T) == typeof(char) ? "" : " ", parts);
    }
}
