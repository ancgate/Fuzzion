using System;
using System.Collections.Generic;
using Fuzzion.Extensions;

namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

/// <summary>
/// Base class for cached token set scorers: the query is split into words once, and each
/// <see cref="Score"/> call works as <see cref="TokenSetScorerBase"/> does. No preprocessing is applied.
/// </summary>
/// <param name="input1">The query.</param>
public abstract class CachedTokenSetScorerBase(string input1) : ICachedRatioScorer
{
    private HashSet<string> Tokens1 { get; set; } = new HashSet<string>(input1.SplitByAnySpace());
    /// <summary>
    /// The function that compares the joined word strings and returns a score from 0 to 100; each subclass supplies it.
    /// </summary>
    protected abstract Fuzzion.Scorer Scorer { get; }

    /// <inheritdoc />
    public double Score(string input2)
    {
        var tokens2 = new HashSet<string>(input2.SplitByAnySpace());

        if (Tokens1.Count == 0 || tokens2.Count == 0)
        {
            return 0;
        }

        var tokens1 = new HashSet<string>(Tokens1);

        var intersection = TokenSetScorerHelpers.GetIntersectionAndExcept(tokens1, tokens2);

        intersection.Sort(StringComparer.Ordinal);

        var sortedIntersection = string.Join(" ", intersection);
        var sortedDiff1To2 = TokenSetScorerHelpers.JoinIntersectionAndDifference(sortedIntersection, tokens1);
        var sortedDiff2To1 = TokenSetScorerHelpers.JoinIntersectionAndDifference(sortedIntersection, tokens2);

        var score1 = Scorer(sortedIntersection, sortedDiff1To2);
        var score2 = Scorer(sortedIntersection, sortedDiff2To1);
        var score3 = Scorer(sortedDiff1To2, sortedDiff2To1);

        return Math.Max(score1, Math.Max(score2, score3));
    }

    /// <summary>Does nothing: these scorers hold no pooled memory.</summary>
    public void Dispose()
    {
    }
}
