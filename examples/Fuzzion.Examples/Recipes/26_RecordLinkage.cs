using Fuzzion;
using Fuzzion.PreProcess;

namespace FuzzionExamples.Recipes;

/// <summary>
/// Matching records on several fields (record linkage): score each field with a suitable
/// metric, combine the scores with weights and sort the results into "match",
/// "check by hand" and "no match".
/// </summary>
public static class RecordLinkage
{
    private sealed record Person(string Id, string Name, string Email, string Phone, string Zip);

    public static void Run()
    {
        var master = new[]
        {
            new Person("C-1", "Jonathan Smith", "jon.smith@example.com", "555-010-4477", "02134"),
            new Person("C-2", "Maria Garcia", "mgarcia@example.org", "555-010-9911", "73301"),
            new Person("C-3", "Wei Zhang", "wei.zhang@example.net", "555-020-1234", "94105"),
            new Person("C-4", "John Smithson", "jsmithson@example.com", "555-030-0000", "10001"),
        };

        var incoming = new[]
        {
            new Person("?", "Smith, Jonathan", "jon.smith@example.com", "(555) 010-4477", "02134"),
            new Person("?", "Mariah Garcia", "maria.garcia@example.org", "555 010 9911", "73301"),
            new Person("?", "W. Zhang", "wzhang@gmail.com", "555-999-1234", "94105"),
            new Person("?", "Jon Smith", "jsmith@yahoo.com", "555-777-0000", "60601"),
        };

        foreach (var person in incoming)
        {
            var best = master
                .Select(candidate => (Candidate: candidate, Score: Score(person, candidate)))
                .OrderByDescending(x => x.Score)
                .First();

            var decision = best.Score >= 85 ? "MATCH" : best.Score >= 60 ? "CHECK BY HAND" : "NO MATCH";
            Console.WriteLine($"   {person.Name,-16} -> {best.Candidate.Id} {best.Candidate.Name,-15} {Show.Num(best.Score),5}  {decision}");
        }

        Console.WriteLine();
        Console.WriteLine("   Weights: name 40%, email 25%, phone 25%, zip 10%. Tune them and the two");
        Console.WriteLine("   thresholds on a sample of records you have checked by hand.");
    }

    /// <summary>0-100: a weighted average of per-field scores.</summary>
    private static double Score(Person a, Person b)
    {
        // Names: word order varies ("Smith, Jonathan"), so use a token scorer.
        var name = Fuzz.TokenSortRatio(a.Name, b.Name, StringPreprocessor.Full);

        // Emails: compare the part before @; the domain often changes.
        var email = Fuzz.Ratio(a.Email.Split('@')[0], b.Email.Split('@')[0], StringPreprocessor.Full);

        // Phones: digits only; each wrong digit costs a lot.
        var digitsA = new string(a.Phone.Where(char.IsDigit).ToArray());
        var digitsB = new string(b.Phone.Where(char.IsDigit).ToArray());
        var phone = 100 * Math.Max(0, 1 - Levenshtein.Distance(digitsA, digitsB) / 3.0);

        // Zip: exact or nothing.
        var zip = a.Zip == b.Zip ? 100 : 0;

        return 0.40 * name + 0.25 * email + 0.25 * phone + 0.10 * zip;
    }
}
