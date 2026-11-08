using System.Globalization;
using Fuzzion.Extractor;

namespace FuzzionExamples;

/// <summary>Small console helpers so the examples can focus on Fuzzion itself.</summary>
internal static class Show
{
    public static void Heading(string text)
    {
        Console.WriteLine();
        Console.WriteLine("-- " + text);
    }

    /// <summary>Scores and similarities with at most two decimals: 96.55, 0.57, 100.</summary>
    public static string Num(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    public static void Results<T>(IEnumerable<ExtractedResult<T>> results, Func<T, string> describe = null)
    {
        var any = false;
        foreach (var r in results)
        {
            any = true;
            var text = describe == null ? r.Value?.ToString() : describe(r.Value);
            Console.WriteLine($"   {Num(r.Score),6}  {text}  (index {r.Index})");
        }

        if (!any)
        {
            Console.WriteLine("   (no results)");
        }
    }

    /// <summary>Prints rows as aligned columns; the first row is the header.</summary>
    public static void Table(IReadOnlyList<string[]> rows)
    {
        var widths = new int[rows.Max(r => r.Length)];
        foreach (var row in rows)
        {
            for (var i = 0; i < row.Length; i++)
            {
                widths[i] = Math.Max(widths[i], row[i].Length);
            }
        }

        for (var r = 0; r < rows.Count; r++)
        {
            var cells = rows[r].Select((cell, i) => i == 0 ? cell.PadRight(widths[i]) : cell.PadLeft(widths[i]));
            Console.WriteLine("   " + string.Join("  ", cells).TrimEnd());
            if (r == 0)
            {
                Console.WriteLine("   " + string.Join("  ", widths.Select(w => new string('-', w))));
            }
        }
    }
}
