using Fuzzion;

namespace FuzzionExamples.Metrics;

/// <summary>
/// <see cref="Prefix"/> and <see cref="Postfix"/> measure how long the shared start or end
/// is. Useful for paths, URLs, phone numbers, product codes and file names.
/// </summary>
public static class PrefixPostfix
{
    public static void Run()
    {
        Show.Heading("Prefix: length of the shared start");
        Console.WriteLine($"   Prefix.Similarity(\"prefix\", \"preface\"): {Prefix.Similarity("prefix", "preface")}");
        Console.WriteLine($"   Prefix.Distance  (\"prefix\", \"preface\"): {Prefix.Distance("prefix", "preface")}   (longer length - shared)");
        Console.WriteLine($"   Prefix.NormalizedSimilarity:             {Show.Num(Prefix.NormalizedSimilarity("prefix", "preface"))}");

        Show.Heading("Which folders share the most of a path?");
        const string file = "/srv/app/logs/2024/05/error.log";
        foreach (var dir in new[] { "/srv/app/logs/2024/04/", "/srv/app/data/", "/var/log/" })
        {
            Console.WriteLine($"   {dir,-24} shared start: {Prefix.Similarity(file, dir),2}");
        }

        Show.Heading("Postfix: length of the shared end");
        Console.WriteLine($"   Postfix.Similarity(\"report_final.docx\", \"notes.docx\"): {Postfix.Similarity("report_final.docx", "notes.docx")}");

        Show.Heading("Same phone line, different country formatting");
        const string a = "+1 (555) 010-4477";
        const string b = "555-010-4477";
        var digitsA = new string(a.Where(char.IsDigit).ToArray());
        var digitsB = new string(b.Where(char.IsDigit).ToArray());
        Console.WriteLine($"   shared last digits: {Postfix.Similarity(digitsA, digitsB)} of {digitsB.Length}");
    }
}
