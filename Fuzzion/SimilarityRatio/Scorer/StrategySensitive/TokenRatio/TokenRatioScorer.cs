using System;

namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

/// <summary>
/// The higher of <see cref="TokenSortScorer"/> and <see cref="TokenSetScorer"/>.
/// Same as RapidFuzz's <c>token_ratio</c>.
/// </summary>
public sealed class TokenRatioScorer : ScorerBase
{
    /// <inheritdoc/>
    public override double Score(string input1, string input2) =>
        Math.Max(
            ScorerCache.Get<TokenSortScorer>().Score(input1, input2),
            ScorerCache.Get<TokenSetScorer>().Score(input1, input2));
}

/// <summary>
/// The higher of <see cref="PartialTokenSortScorer"/> and <see cref="PartialTokenSetScorer"/>.
/// Same as RapidFuzz's <c>partial_token_ratio</c>. Returns 100 straight away when the
/// inputs share a word.
/// </summary>
public sealed class PartialTokenRatioScorer : ScorerBase
{
    /// <inheritdoc/>
    public override double Score(string input1, string input2)
    {
        var set = ScorerCache.Get<PartialTokenSetScorer>().Score(input1, input2);
        return set >= 100
            ? set
            : Math.Max(set, ScorerCache.Get<PartialTokenSortScorer>().Score(input1, input2));
    }
}

/// <summary><see cref="TokenRatioScorer"/> against one fixed query. Dispose it when done.</summary>
public sealed class CachedTokenRatioScorer(string input1) : ICachedRatioScorer
{
    private readonly CachedTokenSortScorer _sort = new(input1);
    private readonly CachedTokenSetScorer _set = new(input1);

    /// <inheritdoc/>
    public double Score(string input2) => Math.Max(_sort.Score(input2), _set.Score(input2));

    /// <inheritdoc/>
    public void Dispose()
    {
        _sort.Dispose();
        _set.Dispose();
    }
}

/// <summary><see cref="PartialTokenRatioScorer"/> against one fixed query. Dispose it when done.</summary>
public sealed class CachedPartialTokenRatioScorer(string input1) : ICachedRatioScorer
{
    private readonly CachedPartialTokenSortScorer _sort = new(input1);
    private readonly CachedPartialTokenSetScorer _set = new(input1);

    /// <inheritdoc/>
    public double Score(string input2)
    {
        var set = _set.Score(input2);
        return set >= 100 ? set : Math.Max(set, _sort.Score(input2));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _sort.Dispose();
        _set.Dispose();
    }
}
