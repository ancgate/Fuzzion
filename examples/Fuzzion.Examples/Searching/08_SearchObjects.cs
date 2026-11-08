using Fuzzion;
using Fuzzion.SimilarityRatio;
using Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

namespace FuzzionExamples.Searching;

/// <summary>
/// The <c>...By</c> methods search objects. You pass a function that picks the text to
/// compare; the result's <c>Value</c> is the whole object.
/// </summary>
public static class SearchObjects
{
    private sealed record Product(string Sku, string Name, decimal Price);

    private sealed record Customer(string FullName, string City, string Email);

    public static void Run()
    {
        var products = new[]
        {
            new Product("A-100", "Apple iPhone 15 Pro Silicone Case", 49m),
            new Product("A-101", "Apple iPhone 15 Clear Case", 39m),
            new Product("S-200", "Samsung Galaxy S24 Case", 29m),
            new Product("A-300", "Apple AirPods Pro (2nd generation)", 249m),
            new Product("G-400", "Google Pixel 8 Pro Case", 35m),
        };

        Show.Heading("A string query against objects: ExtractTopBy(query, items, item => text)");
        // TokenSetScorer: every word of the query appears in the name, in any order.
        var hits = Process.ExtractTopBy("iphone 15 pro case", products, p => p.Name, scorer: ScorerCache.Get<TokenSetScorer>(), limit: 3);
        foreach (var hit in hits)
        {
            Console.WriteLine($"   {Show.Num(hit.Score),6}  {hit.Value.Sku}  {hit.Value.Name}  ${hit.Value.Price}");
        }

        Show.Heading("ExtractOneBy with a different scorer");
        var airpods = Process.ExtractOneBy("airpods", products, p => p.Name, scorer: ScorerCache.Get<PartialRatioScorer>());
        Console.WriteLine($"   {airpods.Value.Sku} {airpods.Value.Name} ({Show.Num(airpods.Score)})");

        // When the query is the same kind of object as the choices, pass the object: the
        // same function is used to get the text from the query and from every choice.
        var crm = new[]
        {
            new Customer("Jonathan Smith", "Boston", "jsmith@example.com"),
            new Customer("Joan Smithers", "Austin", "joan@example.com"),
            new Customer("Jon Smyth", "Denver", "jon.smyth@example.com"),
        };
        var fromWebForm = new Customer("smith, jonathan", "boston", "");

        Show.Heading("An object query: ExtractOneBy(queryObject, items, item => text)");
        var match = Process.ExtractOneBy(fromWebForm, crm, c => c.FullName + " " + c.City,
            scorer: ScorerCache.Get<TokenSortScorer>());
        Console.WriteLine($"   {match.Value.FullName}, {match.Value.City} <{match.Value.Email}> ({Show.Num(match.Score)})");

        Show.Heading("ExtractAllBy with a cutoff: every product that mentions a case");
        Show.Results(Process.ExtractAllBy("case", products, p => p.Name,
            scorer: ScorerCache.Get<PartialRatioScorer>(), cutoff: 90), p => p.Name);
    }
}
