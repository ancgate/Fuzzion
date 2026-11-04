using System;
using System.Linq;
using System.Reflection;
using Xunit;
using Fuzzion.SimilarityRatio;
using Fuzzion.SimilarityRatio.Scorer;

namespace Fuzzion.Tests.FuzzyTests;

public class RegressionTests
{

    /// <summary>
    /// Test to ensure that all IRatioScorer implementations handle scoring empty strings & whitespace strings
    /// </summary>
    [Fact]
    public void TestScoringEmptyString()
    {
        var scorerType = typeof(IRatioScorer);
        var assemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
        var types = assemblies.SelectMany(s =>
        {
            try
            {
                return s.GetTypes();
            }
            catch {}
            return [];
        }).ToList();
        // Only scorers that ScorerCache can create (public parameterless constructor). Adapters
        // that wrap a function (Scorers.FromDistance / FromSimilarity) are covered in
        // Phase5ProcessTests.AdaptersHandleEmptyStrings.
        var scorerTypes = types.Where(t => scorerType.IsAssignableFrom(t) && !t.IsAbstract && t.IsClass && t.GetConstructor(Type.EmptyTypes) != null).ToList();

        string emptyString = "";
        string whitespaceString = " ";

        string[] nullOrWhitespaceStrings = [emptyString, whitespaceString];
        MethodInfo getScorerCacheMethodInfo = typeof(ScorerCache).GetMethod("Get")!;

        foreach (var t in scorerTypes)
        {
            System.Diagnostics.Debug.WriteLine($"Testing {t.Name}");
            MethodInfo m = getScorerCacheMethodInfo.MakeGenericMethod(t);
            IRatioScorer scorer = m.Invoke(this, []) as IRatioScorer;

            foreach(var s in nullOrWhitespaceStrings)
            {
                System.Diagnostics.Debug.WriteLine($"Testing string '{s}'");
                try
                {
                    scorer.Score(s, "TEST");
                }
                catch (InvalidOperationException)
                {
                    Assert.Fail($"{t.Name}.score failed with empty string as first parameter");
                }
                try
                {
                    scorer.Score("TEST", s);
                } catch (InvalidOperationException)
                {
                    Assert.Fail($"{t.Name}.score failed with empty string as second parameter");
                }
                try
                {
                    scorer.Score(s, s);
                }
                catch (InvalidOperationException)
                {
                    Assert.Fail($"{t.Name}.score failed with empty string as both parameters");
                }
            }
        }
    }
}
