using Fuzzion;

namespace FuzzionExamples.Basics;

/// <summary>
/// Two ways to make comparisons cheaper: tell the scorer the lowest score you care about
/// (it can then stop early), and pass slices of larger strings without copying them.
/// </summary>
public static class CutoffsAndSpans
{
    public static void Run()
    {
        Show.Heading("scoreCutoff: anything below the cutoff comes back as 0");
        const string a = "fuzzy matching library";
        const string b = "fuzzy matcher";
        Console.WriteLine($"   Ratio:                  {Show.Num(Fuzz.Ratio(a, b))}");
        Console.WriteLine($"   Ratio, scoreCutoff: 60: {Show.Num(Fuzz.Ratio(a, b, scoreCutoff: 60))}");
        Console.WriteLine($"   Ratio, scoreCutoff: 90: {Show.Num(Fuzz.Ratio(a, b, scoreCutoff: 90))}  <- below 90, so 0");
        Console.WriteLine($"   WeightedRatio, scoreCutoff: 90: {Show.Num(Fuzz.WeightedRatio(a, b, scoreCutoff: 90))}");

        Show.Heading("Levenshtein.Distance with scoreCutoff: stop once the distance is too large");
        var x = new string('a', 500) + "xyz";
        var y = new string('b', 500) + "xyz";
        Console.WriteLine($"   full distance:                {Levenshtein.Distance(x, y)}");
        Console.WriteLine($"   with scoreCutoff: 3:          {Levenshtein.Distance(x, y, scoreCutoff: 3)}  <- means \"more than 3\"");
        // scoreHint is a guess of the distance; it only affects speed, never the result.
        Console.WriteLine($"   kitten/sitting, scoreHint: 3: {Levenshtein.Distance("kitten", "sitting", scoreHint: 3)}");

        Show.Heading("ReadOnlySpan<char>: score slices without allocating substrings");
        const string line = "2024-05-01 ERROR PaymentService: card declined";
        ReadOnlySpan<char> level = line.AsSpan(11, 5);    // "ERROR"
        ReadOnlySpan<char> service = line.AsSpan(17, 14); // "PaymentService"
        Console.WriteLine($"   Ratio(\"{level.ToString()}\", \"EROR\"):            {Show.Num(Fuzz.Ratio(level, "EROR".AsSpan()))}");
        Console.WriteLine($"   Ratio(\"{service.ToString()}\", \"PaymentSvc\"): {Show.Num(Fuzz.Ratio(service, "PaymentSvc".AsSpan()))}");
        Console.WriteLine($"   Levenshtein.Distance<char>(spans):           {Levenshtein.Distance(service, "PaymentSvc".AsSpan())}");
        // Ratio, QRatio, PartialRatio and ApproximateSubstringRatio accept spans; every
        // distance metric (Levenshtein, Jaro, Osa, ...) has ReadOnlySpan<T> overloads.
    }
}
