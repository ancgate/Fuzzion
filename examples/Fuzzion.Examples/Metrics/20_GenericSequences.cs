using Fuzzion;

namespace FuzzionExamples.Metrics;

/// <summary>
/// The distance metrics are not limited to characters. The generic overloads take a
/// <c>ReadOnlySpan&lt;T&gt;</c> of any <c>T</c> that implements <c>IEquatable&lt;T&gt;</c>:
/// words, numbers, chars, records. The <c>...T&lt;T&gt;</c> classes are the reusable
/// instance form.
/// </summary>
public static class GenericSequences
{
    private enum Step { Login, Search, AddToCart, Checkout, Pay, Logout }

    public static void Run()
    {
        Show.Heading("Edit distance between sentences, counted in words");
        var a = "the cat sat on the mat".Split(' ');
        var b = "the cat lay on the red mat".Split(' ');
        Console.WriteLine($"   Levenshtein (words): {Levenshtein.Distance<string>(a, b)}   (lay for sat, insert red)");
        Console.WriteLine($"   Levenshtein (chars): {Levenshtein.Distance(string.Join(" ", a), string.Join(" ", b))}");
        Console.WriteLine($"   Indel normalized similarity (words): {Show.Num(Indel.NormalizedSimilarity<string>(a, b))}");

        Show.Heading("Which word edits? (see also example 21)");
        foreach (var op in Levenshtein.GetEditOps<string>(a, b))
        {
            var detail = op.EditType switch
            {
                Fuzzion.Edits.EditType.REPLACE => $"\"{a[op.SourcePos]}\" -> \"{b[op.DestPos]}\"",
                Fuzzion.Edits.EditType.INSERT => $"insert \"{b[op.DestPos]}\"",
                _ => $"delete \"{a[op.SourcePos]}\"",
            };
            Console.WriteLine($"   {op.EditType,-8} {detail}");
        }

        Show.Heading("User journeys as sequences of enum values");
        // Enums do not implement IEquatable<T>, so compare their numeric values.
        var expected = new[] { Step.Login, Step.Search, Step.AddToCart, Step.Checkout, Step.Pay, Step.Logout };
        var actual = new[] { Step.Login, Step.Search, Step.Search, Step.AddToCart, Step.Pay, Step.Logout };
        int[] expectedIds = expected.Select(s => (int)s).ToArray();
        int[] actualIds = actual.Select(s => (int)s).ToArray();
        Console.WriteLine($"   Levenshtein:  {Levenshtein.Distance<int>(expectedIds, actualIds)}   (an extra Search, a missing Checkout)");
        Console.WriteLine($"   Osa:          {Osa.Distance<int>(expectedIds, actualIds)}");
        Console.WriteLine($"   Jaro-Winkler: {Show.Num(JaroWinkler.Similarity<int>(expectedIds, actualIds))}");

        Show.Heading("Numbers: compare two sensor readings sample by sample");
        var day1 = new[] { 3, 3, 4, 7, 7, 7, 2 };
        var day2 = new[] { 3, 3, 4, 6, 7, 7, 2 };
        Console.WriteLine($"   Hamming: {Hamming.Distance<int>(day1, day2)} sample(s) differ");

        Show.Heading("Reusable generic instances: one sequence against many");
        using var reference = new IndelT<string>(new[] { "hello", "world" });
        foreach (var words in new[] { new[] { "hello", "there" }, new[] { "hello", "world", "again" }, new[] { "goodbye" } })
        {
            Console.WriteLine($"   [{string.Join(" ", words)}]: distance {reference.DistanceFrom(words)}, " +
                              $"similarity {Show.Num(reference.NormalizedSimilarityWith(words))}");
        }

        using var journey = new OsaT<int>(expectedIds);
        Console.WriteLine($"   OsaT<int>.DistanceFrom(actualIds): {journey.DistanceFrom(actualIds)}");
    }
}
