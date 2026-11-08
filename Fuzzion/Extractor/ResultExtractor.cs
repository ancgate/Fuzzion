using Fuzzion.SimilarityRatio.Scorer;
using Fuzzion.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fuzzion.Extractor;

/// <summary>
/// The search loops behind <see cref="Process"/> and the pipelines built with
/// <see cref="Process.Configure"/>: each method scores the choices against a query and keeps
/// the ones whose score reaches the cutoff. <see cref="ExtractedResult{T}.Index"/> is the
/// position of the choice in the choices.
/// </summary>
/// <remarks>
/// Lower level than <see cref="Process"/>: the scorer is required, and distance scorers
/// (<see cref="IDistanceScorer"/>) are not handled here. A higher score always counts as
/// better, so with a distance scorer these methods return the farthest choices; use
/// <see cref="Process"/> or a pipeline for distance scorers.
/// </remarks>
public static partial class ResultExtractor
{
    internal static void ValidateLimit(int limit)
    {
        if (limit < 0)
            throw new ArgumentOutOfRangeException(nameof(limit), limit, "The limit must be non-negative.");
    }

    private static ExtractedResult<T> ExtractOneCore<T>(
        IEnumerable<T> choices,
        Func<T, double> scoreSelector,
        double cutoff)
    {
        var index = 0;
        var bestIndex = 0;
        var bestScore = 0.0;
        T bestValue = default;
        var hasBest = false;

        foreach (var choice in choices)
        {
            var score = scoreSelector(choice);
            if (score >= cutoff && (!hasBest || score > bestScore))
            {
                bestValue = choice;
                bestScore = score;
                bestIndex = index;
                hasBest = true;
            }

            index++;
        }

        if (!hasBest)
        {
            return null;
        }

        return new ExtractedResult<T>(bestValue, bestScore, bestIndex);
    }

    private static IEnumerable<ExtractedResult<T>> ExtractTopCore<T>(
        IEnumerable<T> choices,
        Func<T, double> scoreSelector,
        int limit,
        double cutoff)
    {
        ValidateLimit(limit);
        if (limit == 0)
            yield break;

        var comparer = ScoredCandidateComparer<T>.Instance;
        var heap = new MinHeap<ScoredCandidate<T>>(comparer);
        var index = 0;

        foreach (var choice in choices)
        {
            var score = scoreSelector(choice);
            if (score >= cutoff)
            {
                AddTopCandidate(heap, comparer, new ScoredCandidate<T>(choice, score, index), limit);
            }

            index++;
        }

        foreach (var result in CreateTopResults(heap))
        {
            yield return result;
        }
    }

    private static void AddTopCandidate<T>(
        MinHeap<ScoredCandidate<T>> heap,
        Comparer<ScoredCandidate<T>> comparer,
        ScoredCandidate<T> candidate,
        int limit)
    {
        if (heap.Count < limit)
        {
            heap.Add(candidate);
        }
        else if (comparer.Compare(candidate, heap.GetMin()) > 0)
        {
            heap.ExtractDominating();
            heap.Add(candidate);
        }
    }

    private static IEnumerable<ExtractedResult<T>> CreateTopResults<T>(MinHeap<ScoredCandidate<T>> heap)
    {
        var results = new ExtractedResult<T>[heap.Count];

        for (var i = results.Length - 1; i >= 0; i--)
        {
            var candidate = heap.ExtractDominating();
            results[i] = new ExtractedResult<T>(candidate.Value, candidate.Score, candidate.Index);
        }

        return results;
    }

    /// <summary>
    /// Scores every choice against the query and returns those that reach <paramref name="cutoff"/>, in their original order.
    /// </summary>
    /// <typeparam name="T">The type of the choices.</typeparam>
    /// <param name="query">The query; processed with <paramref name="processor"/> like the choices.</param>
    /// <param name="choices">The choices to search.</param>
    /// <param name="extractor">Gets the text to compare from each choice.</param>
    /// <param name="processor">Applied to the query and to every choice's text before scoring; <see cref="Fuzzion.PreProcess.StringPreprocessor.Full"/> when null.</param>
    /// <param name="scorer">Compares the processed query with each processed choice; must not be null. Higher scores count as better.</param>
    /// <param name="cutoff">The minimum score a choice needs to be returned (inclusive); 0 by default.</param>
    /// <returns>The matching choices in their original order. Evaluated lazily: the choices are scored each time the result is enumerated.</returns>
    public static IEnumerable<ExtractedResult<T>> ExtractWithoutOrder<T>(string query, IEnumerable<T> choices, Func<T, string> extractor, Func<string, string> processor, IRatioScorer scorer, double cutoff = 0)
    {
        int index = 0;
        processor ??= Process.DefaultStringProcessor;
        var processedQuery = processor(query);

        foreach (var choice in choices)
        {
            double score = scorer.Score(processedQuery, processor(extractor(choice)));
            if (score >= cutoff)
            {
                yield return new ExtractedResult<T>(choice, score, index);
            }
            index++;
        }
    }

    /// <summary>
    /// Scores every choice against the query and returns those that reach <paramref name="cutoff"/>, in their original order.
    /// </summary>
    /// <param name="query">The query; processed with <paramref name="processor"/> like the choices.</param>
    /// <param name="choices">The choices to search.</param>
    /// <param name="processor">Applied to the query and to every choice before scoring; <see cref="Fuzzion.PreProcess.StringPreprocessor.Full"/> when null.</param>
    /// <param name="scorer">Compares the processed query with each processed choice; must not be null. Higher scores count as better.</param>
    /// <param name="cutoff">The minimum score a choice needs to be returned (inclusive); 0 by default.</param>
    /// <returns>The matching choices in their original order. Evaluated lazily: the choices are scored each time the result is enumerated.</returns>
    public static IEnumerable<ExtractedResult<string>> ExtractWithoutOrder(string query, IEnumerable<string> choices, Func<string, string> processor, IRatioScorer scorer, double cutoff = 0)
    {
        int index = 0;
        processor ??= Process.DefaultStringProcessor;
        var processedQuery = processor(query);

        foreach (var choice in choices)
        {
            double score = scorer.Score(processedQuery, processor(choice));
            if (score >= cutoff)
            {
                yield return new ExtractedResult<string>(choice, score, index);
            }
            index++;
        }
    }

    /// <summary>
    /// Scores every choice against the query and returns those that reach <paramref name="cutoff"/>, in their original order.
    /// </summary>
    /// <typeparam name="T">The type of the query and the choices.</typeparam>
    /// <param name="query">The query; its text is taken with <paramref name="extractor"/> and processed like the choices.</param>
    /// <param name="choices">The choices to search.</param>
    /// <param name="extractor">Gets the text to compare from the query and from each choice.</param>
    /// <param name="processor">Applied to the query and to every choice's text before scoring; <see cref="Fuzzion.PreProcess.StringPreprocessor.Full"/> when null.</param>
    /// <param name="scorer">Compares the processed query with each processed choice; must not be null. Higher scores count as better.</param>
    /// <param name="cutoff">The minimum score a choice needs to be returned (inclusive); 0 by default.</param>
    /// <returns>The matching choices in their original order. Evaluated lazily: the choices are scored each time the result is enumerated.</returns>
    public static IEnumerable<ExtractedResult<T>> ExtractWithoutOrder<T>(T query, IEnumerable<T> choices, Func<T, string> extractor, Func<string, string> processor, IRatioScorer scorer, double cutoff = 0)
    {
        var extracted = extractor(query);
        return ExtractWithoutOrder(extracted, choices, extractor, processor, scorer, cutoff);
    }

    /// <summary>
    /// Finds the choice with the highest score against the query, if it reaches <paramref name="cutoff"/>.
    /// </summary>
    /// <typeparam name="T">The type of the query and the choices.</typeparam>
    /// <param name="query">The query; its text is taken with <paramref name="extractor"/> and processed like the choices.</param>
    /// <param name="choices">The choices to search.</param>
    /// <param name="extractor">Gets the text to compare from the query and from each choice.</param>
    /// <param name="processor">Applied to the query and to every choice's text before scoring; <see cref="Fuzzion.PreProcess.StringPreprocessor.Full"/> when null.</param>
    /// <param name="scorer">Compares the processed query with each processed choice; must not be null. Higher scores count as better.</param>
    /// <param name="cutoff">The minimum score a choice needs to be returned (inclusive); 0 by default.</param>
    /// <returns>The best match (the earliest choice when several share the top score), or <c>null</c> when no choice reaches <paramref name="cutoff"/>.</returns>
    public static ExtractedResult<T> ExtractOne<T>(T query, IEnumerable<T> choices, Func<T, string> extractor, Func<string, string> processor, IRatioScorer scorer, double cutoff = 0)
    {
        processor ??= Process.DefaultStringProcessor;
        var processedQuery = processor(extractor(query));
        return ExtractOneCore(choices, choice => scorer.Score(processedQuery, processor(extractor(choice))), cutoff);
    }

    /// <summary>
    /// Finds the choice with the highest score against the query, if it reaches <paramref name="cutoff"/>.
    /// </summary>
    /// <param name="query">The query; processed with <paramref name="processor"/> like the choices.</param>
    /// <param name="choices">The choices to search.</param>
    /// <param name="processor">Applied to the query and to every choice before scoring; <see cref="Fuzzion.PreProcess.StringPreprocessor.Full"/> when null.</param>
    /// <param name="scorer">Compares the processed query with each processed choice; must not be null. Higher scores count as better.</param>
    /// <param name="cutoff">The minimum score a choice needs to be returned (inclusive); 0 by default.</param>
    /// <returns>The best match (the earliest choice when several share the top score), or <c>null</c> when no choice reaches <paramref name="cutoff"/>.</returns>
    public static ExtractedResult<string> ExtractOne(string query, IEnumerable<string> choices, Func<string, string> processor, IRatioScorer scorer, double cutoff = 0)
    {
        processor ??= Process.DefaultStringProcessor;
        var processedQuery = processor(query);
        return ExtractOneCore(choices, choice => scorer.Score(processedQuery, processor(choice)), cutoff);
    }

    /// <summary>
    /// Finds the choice with the highest score against the query, if it reaches <paramref name="cutoff"/>.
    /// </summary>
    /// <typeparam name="T">The type of the choices.</typeparam>
    /// <param name="query">The query; processed with <paramref name="processor"/> like the choices.</param>
    /// <param name="choices">The choices to search.</param>
    /// <param name="extractor">Gets the text to compare from each choice.</param>
    /// <param name="processor">Applied to the query and to every choice's text before scoring; <see cref="Fuzzion.PreProcess.StringPreprocessor.Full"/> when null.</param>
    /// <param name="scorer">Compares the processed query with each processed choice; must not be null. Higher scores count as better.</param>
    /// <param name="cutoff">The minimum score a choice needs to be returned (inclusive); 0 by default.</param>
    /// <returns>The best match (the earliest choice when several share the top score), or <c>null</c> when no choice reaches <paramref name="cutoff"/>.</returns>
    public static ExtractedResult<T> ExtractOne<T>(string query, IEnumerable<T> choices, Func<T, string> extractor, Func<string, string> processor, IRatioScorer scorer, double cutoff = 0)
    {
        processor ??= Process.DefaultStringProcessor;
        var processedQuery = processor(query);
        return ExtractOneCore(choices, choice => scorer.Score(processedQuery, processor(extractor(choice))), cutoff);
    }

    /// <summary>
    /// Scores every choice against the query and returns those that reach <paramref name="cutoff"/>, highest score first.
    /// </summary>
    /// <typeparam name="T">The type of the query and the choices.</typeparam>
    /// <param name="query">The query; its text is taken with <paramref name="extractor"/> and processed like the choices.</param>
    /// <param name="choices">The choices to search.</param>
    /// <param name="extractor">Gets the text to compare from the query and from each choice.</param>
    /// <param name="processor">Applied to the query and to every choice's text before scoring; <see cref="Fuzzion.PreProcess.StringPreprocessor.Full"/> when null.</param>
    /// <param name="scorer">Compares the processed query with each processed choice; must not be null. Higher scores count as better.</param>
    /// <param name="cutoff">The minimum score a choice needs to be returned (inclusive); 0 by default.</param>
    /// <returns>The matching choices, highest score first; choices with equal scores keep their original order. Evaluated lazily, when the result is enumerated.</returns>
    public static IEnumerable<ExtractedResult<T>> ExtractSorted<T>(T query, IEnumerable<T> choices, Func<T, string> extractor, Func<string, string> processor, IRatioScorer scorer, double cutoff = 0)
    {
        return ExtractWithoutOrder(query, choices, extractor, processor, scorer, cutoff).OrderByDescending(r => r.Score);
    }

    /// <summary>
    /// Scores every choice against the query and returns those that reach <paramref name="cutoff"/>, highest score first.
    /// </summary>
    /// <param name="query">The query; processed with <paramref name="processor"/> like the choices.</param>
    /// <param name="choices">The choices to search.</param>
    /// <param name="processor">Applied to the query and to every choice before scoring; <see cref="Fuzzion.PreProcess.StringPreprocessor.Full"/> when null.</param>
    /// <param name="scorer">Compares the processed query with each processed choice; must not be null. Higher scores count as better.</param>
    /// <param name="cutoff">The minimum score a choice needs to be returned (inclusive); 0 by default.</param>
    /// <returns>The matching choices, highest score first; choices with equal scores keep their original order. Evaluated lazily, when the result is enumerated.</returns>
    public static IEnumerable<ExtractedResult<string>> ExtractSorted(string query, IEnumerable<string> choices, Func<string, string> processor, IRatioScorer scorer, double cutoff = 0)
    {
        return ExtractWithoutOrder(query, choices, processor, scorer, cutoff).OrderByDescending(r => r.Score);
    }

    /// <summary>
    /// Scores every choice against the query and returns those that reach <paramref name="cutoff"/>, highest score first.
    /// </summary>
    /// <typeparam name="T">The type of the choices.</typeparam>
    /// <param name="query">The query; processed with <paramref name="processor"/> like the choices.</param>
    /// <param name="choices">The choices to search.</param>
    /// <param name="extractor">Gets the text to compare from each choice.</param>
    /// <param name="processor">Applied to the query and to every choice's text before scoring; <see cref="Fuzzion.PreProcess.StringPreprocessor.Full"/> when null.</param>
    /// <param name="scorer">Compares the processed query with each processed choice; must not be null. Higher scores count as better.</param>
    /// <param name="cutoff">The minimum score a choice needs to be returned (inclusive); 0 by default.</param>
    /// <returns>The matching choices, highest score first; choices with equal scores keep their original order. Evaluated lazily, when the result is enumerated.</returns>
    public static IEnumerable<ExtractedResult<T>> ExtractSorted<T>(string query, IEnumerable<T> choices, Func<T, string> extractor, Func<string, string> processor, IRatioScorer scorer, double cutoff = 0)
    {
        return ExtractWithoutOrder(query, choices, extractor, processor, scorer, cutoff).OrderByDescending(r => r.Score);
    }

    /// <summary>
    /// Finds the <paramref name="limit"/> choices with the highest scores against the query that reach <paramref name="cutoff"/>.
    /// </summary>
    /// <typeparam name="T">The type of the query and the choices.</typeparam>
    /// <param name="query">The query; its text is taken with <paramref name="extractor"/> and processed like the choices.</param>
    /// <param name="choices">The choices to search.</param>
    /// <param name="extractor">Gets the text to compare from the query and from each choice.</param>
    /// <param name="processor">Applied to the query and to every choice's text before scoring; <see cref="Fuzzion.PreProcess.StringPreprocessor.Full"/> when null.</param>
    /// <param name="scorer">Compares the processed query with each processed choice; must not be null. Higher scores count as better.</param>
    /// <param name="limit">The maximum number of results; 0 returns none. Must not be negative.</param>
    /// <param name="cutoff">The minimum score a choice needs to be returned (inclusive); 0 by default.</param>
    /// <returns>At most <paramref name="limit"/> matches, highest score first. When scores are equal, which choices are kept and their order is not specified. Evaluated lazily, when the result is enumerated.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="limit"/> is negative.</exception>
    public static IEnumerable<ExtractedResult<T>> ExtractTop<T>(T query, IEnumerable<T> choices, Func<T, string> extractor, Func<string, string> processor, IRatioScorer scorer, int limit, double cutoff = 0)
    {
        ValidateLimit(limit);
        if (limit == 0)
            return Enumerable.Empty<ExtractedResult<T>>();

        var extracted = extractor(query);
        return ExtractTop(extracted, choices, extractor, processor, scorer, limit, cutoff);
    }

    /// <summary>
    /// Finds the <paramref name="limit"/> choices with the highest scores against the query that reach <paramref name="cutoff"/>.
    /// </summary>
    /// <param name="query">The query; processed with <paramref name="processor"/> like the choices.</param>
    /// <param name="choices">The choices to search.</param>
    /// <param name="processor">Applied to the query and to every choice before scoring; <see cref="Fuzzion.PreProcess.StringPreprocessor.Full"/> when null.</param>
    /// <param name="scorer">Compares the processed query with each processed choice; must not be null. Higher scores count as better.</param>
    /// <param name="limit">The maximum number of results; 0 returns none. Must not be negative.</param>
    /// <param name="cutoff">The minimum score a choice needs to be returned (inclusive); 0 by default.</param>
    /// <returns>At most <paramref name="limit"/> matches, highest score first. When scores are equal, which choices are kept and their order is not specified. Evaluated lazily, when the result is enumerated.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="limit"/> is negative.</exception>
    public static IEnumerable<ExtractedResult<string>> ExtractTop(string query, IEnumerable<string> choices, Func<string, string> processor, IRatioScorer scorer, int limit, double cutoff = 0)
    {
        ValidateLimit(limit);
        if (limit == 0)
            return Enumerable.Empty<ExtractedResult<string>>();

        processor ??= Process.DefaultStringProcessor;
        return ExtractTopIterator();

        IEnumerable<ExtractedResult<string>> ExtractTopIterator()
        {
            var processedQuery = processor(query);
            foreach (var result in ExtractTopCore(choices, choice => scorer.Score(processedQuery, processor(choice)), limit, cutoff))
            {
                yield return result;
            }
        }
    }

    /// <summary>
    /// Finds the <paramref name="limit"/> choices with the highest scores against the query that reach <paramref name="cutoff"/>.
    /// </summary>
    /// <typeparam name="T">The type of the choices.</typeparam>
    /// <param name="query">The query; processed with <paramref name="processor"/> like the choices.</param>
    /// <param name="choices">The choices to search.</param>
    /// <param name="extractor">Gets the text to compare from each choice.</param>
    /// <param name="processor">Applied to the query and to every choice's text before scoring; <see cref="Fuzzion.PreProcess.StringPreprocessor.Full"/> when null.</param>
    /// <param name="scorer">Compares the processed query with each processed choice; must not be null. Higher scores count as better.</param>
    /// <param name="limit">The maximum number of results; 0 returns none. Must not be negative.</param>
    /// <param name="cutoff">The minimum score a choice needs to be returned (inclusive); 0 by default.</param>
    /// <returns>At most <paramref name="limit"/> matches, highest score first. When scores are equal, which choices are kept and their order is not specified. Evaluated lazily, when the result is enumerated.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="limit"/> is negative.</exception>
    public static IEnumerable<ExtractedResult<T>> ExtractTop<T>(string query, IEnumerable<T> choices, Func<T, string> extractor, Func<string, string> processor, IRatioScorer scorer, int limit, double cutoff = 0)
    {
        ValidateLimit(limit);
        if (limit == 0)
            return Enumerable.Empty<ExtractedResult<T>>();

        processor ??= Process.DefaultStringProcessor;
        return ExtractTopIterator();

        IEnumerable<ExtractedResult<T>> ExtractTopIterator()
        {
            var processedQuery = processor(query);
            foreach (var result in ExtractTopCore(choices, choice => scorer.Score(processedQuery, processor(extractor(choice))), limit, cutoff))
            {
                yield return result;
            }
        }
    }
}
