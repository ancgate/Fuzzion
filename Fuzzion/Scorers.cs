using System;
using Fuzzion.PreProcess;
using Fuzzion.SimilarityRatio.Scorer;

namespace Fuzzion;

/// <summary>
/// A scorer whose result is a distance: lower is better and 0 means equal. Searches
/// (<see cref="Process"/>, pipelines, <see cref="Process.Cdist"/>) return the closest
/// choices first and keep only results within <see cref="MaxDistance"/>.
/// Create one with <see cref="Scorers.FromDistance(Func{string, string, int}, int?)"/>.
/// </summary>
public interface IDistanceScorer : IRatioScorer
{
    /// <summary>
    /// Largest distance a search keeps; <c>null</c> keeps every result, 0 keeps exact matches only.
    /// </summary>
    int? MaxDistance { get; }
}

/// <summary>Adapters that turn plain functions into scorers for searches (plan item #26).</summary>
public static class Scorers
{
    /// <summary>
    /// A distance scorer, for example
    /// <c>Scorers.FromDistance((a, b) => Levenshtein.Distance(a, b), maxDistance: 2)</c>.
    /// Searches with it return the smallest distances first.
    /// </summary>
    /// <param name="distance">The distance function; must return 0 or more.</param>
    /// <param name="maxDistance">Largest distance a search keeps; <c>null</c> (the default) keeps everything.</param>
    public static IDistanceScorer FromDistance(Func<string, string, int> distance, int? maxDistance = null)
    {
        if (distance == null) throw new ArgumentNullException(nameof(distance));
        if (maxDistance < 0) throw new ArgumentOutOfRangeException(nameof(maxDistance), maxDistance, "maxDistance must be 0 or more.");
        return new DistanceScorer(distance, maxDistance);
    }

    /// <summary>
    /// The Levenshtein distance (unit costs) as a distance scorer. Same results as
    /// <c>FromDistance((a, b) => Levenshtein.Distance(a, b), maxDistance)</c>, but
    /// <see cref="Process.Cdist"/> recognizes it and scores short choices many at a time with
    /// SIMD, and the maximum is used to stop each comparison early.
    /// </summary>
    public static IDistanceScorer Levenshtein(int? maxDistance = null)
    {
        if (maxDistance < 0) throw new ArgumentOutOfRangeException(nameof(maxDistance), maxDistance, "maxDistance must be 0 or more.");
        return new LevenshteinScorer(maxDistance);
    }

    /// <summary>
    /// A similarity scorer (higher is better) from any function, for example
    /// <c>Scorers.FromSimilarity((a, b) => 100 * JaroWinkler.Similarity(a, b))</c>.
    /// </summary>
    public static IRatioScorer FromSimilarity(Func<string, string, double> similarity)
    {
        if (similarity == null) throw new ArgumentNullException(nameof(similarity));
        return new SimilarityScorer(similarity);
    }

    private sealed class DistanceScorer(Func<string, string, int> distance, int? maxDistance) : IDistanceScorer
    {
        public int? MaxDistance { get; } = maxDistance;

        public double Score(string input1, string input2) => distance(input1, input2);

        public double Score(string input1, string input2, Func<string, string> preprocessor)
        {
            preprocessor ??= StringPreprocessor.Full;
            return distance(preprocessor(input1), preprocessor(input2));
        }
    }

    internal sealed class LevenshteinScorer(int? maxDistance) : IDistanceScorer
    {
        public int? MaxDistance { get; } = maxDistance;

        public double Score(string input1, string input2) =>
            Fuzzion.Levenshtein.Distance(input1, input2, scoreCutoff: MaxDistance);

        public double Score(string input1, string input2, Func<string, string> preprocessor)
        {
            preprocessor ??= StringPreprocessor.Full;
            return Score(preprocessor(input1), preprocessor(input2));
        }
    }

    private sealed class SimilarityScorer(Func<string, string, double> similarity) : IRatioScorer
    {
        public double Score(string input1, string input2) => similarity(input1, input2);

        public double Score(string input1, string input2, Func<string, string> preprocessor)
        {
            preprocessor ??= StringPreprocessor.Full;
            return similarity(preprocessor(input1), preprocessor(input2));
        }
    }
}
