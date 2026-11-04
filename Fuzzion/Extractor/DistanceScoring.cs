using System;
using System.Collections.Generic;
using System.Linq;
using Fuzzion.SimilarityRatio.Scorer;

namespace Fuzzion.Extractor;

/// <summary>
/// Lets the extractors (which keep the highest scores) work with distance scorers (where
/// lower is better): the search runs on the negated distance, with the maximum distance
/// as a negated cutoff, and the scores are flipped back in the results. Applied once at
/// each public entry point, never inside the extractors, so no result is flipped twice.
/// </summary>
internal static class DistanceScoring
{
    public static IEnumerable<ExtractedResult<T>> Run<T>(
        IRatioScorer scorer, double cutoff, Func<IRatioScorer, double, IEnumerable<ExtractedResult<T>>> extract)
    {
        if (scorer is not IDistanceScorer distance)
        {
            return extract(scorer, cutoff);
        }

        var (negated, negatedCutoff) = Prepare(distance, cutoff);
        return extract(negated, negatedCutoff).Select(Flip);
    }

    public static ExtractedResult<T> Run<T>(
        IRatioScorer scorer, double cutoff, Func<IRatioScorer, double, ExtractedResult<T>> extract)
    {
        if (scorer is not IDistanceScorer distance)
        {
            return extract(scorer, cutoff);
        }

        var (negated, negatedCutoff) = Prepare(distance, cutoff);
        var result = extract(negated, negatedCutoff);
        return result == null ? null : Flip(result);
    }

    private static (IRatioScorer Scorer, double Cutoff) Prepare(IDistanceScorer scorer, double cutoff)
    {
        if (cutoff != 0)
        {
            throw new ArgumentException(
                "cutoff is a minimum similarity and does not apply to distance scorers. " +
                "Set the maximum distance on the scorer instead, for example Scorers.FromDistance(f, maxDistance: 2).",
                nameof(cutoff));
        }

        var negatedCutoff = scorer.MaxDistance.HasValue ? -(double)scorer.MaxDistance.Value : double.NegativeInfinity;
        return (new NegatedScorer(scorer), negatedCutoff);
    }

    private static ExtractedResult<T> Flip<T>(ExtractedResult<T> result) =>
        new(result.Value, -result.Score + 0.0, result.Index);

    private sealed class NegatedScorer(IRatioScorer inner) : IRatioScorer
    {
        public double Score(string input1, string input2) => -inner.Score(input1, input2);

        public double Score(string input1, string input2, Func<string, string> preprocessor) =>
            -inner.Score(input1, input2, preprocessor);
    }
}
