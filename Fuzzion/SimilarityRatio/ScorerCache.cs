using System.Runtime.CompilerServices;
using Fuzzion.SimilarityRatio.Scorer;

namespace Fuzzion.SimilarityRatio;

/// <summary>
/// Shared scorer instances, one per scorer type, so callers need not create a scorer for each call.
/// </summary>
public static class ScorerCache
{
    /// <summary>
    /// Returns the shared instance of <typeparamref name="T"/>, created once (thread-safe) and then
    /// reused by every caller, <see cref="Fuzz"/> included. Only use it for scorers without mutable state,
    /// such as the built-in ones.
    /// </summary>
    /// <typeparam name="T">The scorer type, for example <see cref="Scorer.StrategySensitive.DefaultRatioScorer"/>.</typeparam>
    /// <returns>The shared instance, as an <see cref="IRatioScorer"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IRatioScorer Get<T>() where T : IRatioScorer, new() => GenericCache<T>.Instance;

    private static class GenericCache<T>
        where T : IRatioScorer, new()
    {
        public static readonly T Instance = new T();
    }
}