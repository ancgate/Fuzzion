using System;
using System.Collections.Generic;
using Fuzzion.Extractor;
using Fuzzion.PreProcess;
using Fuzzion.SimilarityRatio;
using Fuzzion.SimilarityRatio.Scorer;
using Fuzzion.SimilarityRatio.Scorer.Composite;

namespace Fuzzion;

public static partial class Process
{
    internal static readonly WeightedRatioScorer DefaultScorer = (WeightedRatioScorer)ScorerCache.Get<WeightedRatioScorer>();
    internal static readonly Func<string, string> DefaultStringProcessor = StringPreprocessor.Full;

    /// <summary>
    /// Creates a new fluent builder for configuring a fuzzy string matching pipeline.
    /// Supports caching, parallel execution, and custom configuration.
    /// </summary>
    /// <returns>A new ProcessBuilder instance</returns>
    public static ProcessBuilder Configure() => new ProcessBuilder();

    #region ExtractAll
    /// <summary>
    /// Creates a list of ExtractedResult which contain all the choices with
    /// their corresponding score where higher is more similar
    /// </summary>
    /// <param name="query"></param>
    /// <param name="choices"></param>
    /// <param name="processor"></param>
    /// <param name="scorer"></param>
    /// <param name="cutoff"></param>
    /// <returns></returns>
    public static IEnumerable<ExtractedResult<string>> ExtractAll(
        string query, 
        IEnumerable<string> choices, 
        Func<string, string> processor = null, 
        IRatioScorer scorer = null,
        double cutoff = 0)
    {
        processor ??= DefaultStringProcessor;
        scorer ??= DefaultScorer;
        return DistanceScoring.Run(scorer, cutoff, (s, c) => ResultExtractor.ExtractWithoutOrder(query, choices, processor, processor, s, c));
    }


    /// <summary>
    /// Creates a list of ExtractedResult which contain all the choices with
    /// their corresponding score where higher is more similar
    /// </summary>
    /// <param name="query"></param>
    /// <param name="choices"></param>
    /// <param name="extractor"></param>
    /// <param name="processor"></param>
    /// <param name="scorer"></param>
    /// <param name="cutoff"></param>
    /// <returns></returns>
    public static IEnumerable<ExtractedResult<T>> ExtractAllBy<T>(
        T query, 
        IEnumerable<T> choices,
        Func<T, string> extractor,
        Func<string, string> processor = null,
        IRatioScorer scorer = null,
        double cutoff = 0)
    {
        processor ??= DefaultStringProcessor;
        scorer ??= DefaultScorer;
        return DistanceScoring.Run(scorer, cutoff, (s, c) => ResultExtractor.ExtractWithoutOrder(query, choices, extractor, processor, s, c));
    }

    /// <summary>
    /// Creates a list of ExtractedResult which contain all the choices with
    /// their corresponding score where higher is more similar
    /// </summary>
    /// <param name="query"></param>
    /// <param name="choices"></param>
    /// <param name="extractor"></param>
    /// <param name="processor"></param>
    /// <param name="scorer"></param>
    /// <param name="cutoff"></param>
    /// <returns></returns>
    public static IEnumerable<ExtractedResult<T>> ExtractAllBy<T>(
        string query,
        IEnumerable<T> choices,
        Func<T, string> extractor,
        Func<string, string> processor = null,
        IRatioScorer scorer = null,
        double cutoff = 0)
    {
        processor ??= DefaultStringProcessor;  
        scorer ??= DefaultScorer;
        return DistanceScoring.Run(scorer, cutoff, (s, c) => ResultExtractor.ExtractWithoutOrder(query, choices, extractor, processor, s, c));
    }

    #endregion

    #region ExtractTop
    /// <summary>
    /// Creates a sorted list of ExtractedResult  which contain the
    /// top limit most similar choices
    /// </summary>
    /// <param name="query"></param>
    /// <param name="choices"></param>
    /// <param name="processor"></param>
    /// <param name="scorer"></param>
    /// <param name="limit"></param>
    /// <param name="cutoff"></param>
    /// <returns></returns>
    public static IEnumerable<ExtractedResult<string>> ExtractTop(
        string query,
        IEnumerable<string> choices,
        Func<string, string> processor = null,
        IRatioScorer scorer = null,
        int limit = 5,
        double cutoff = 0)
    {
        ResultExtractor.ValidateLimit(limit);
        processor ??= DefaultStringProcessor;
        scorer ??= DefaultScorer;
        return DistanceScoring.Run(scorer, cutoff, (s, c) => ResultExtractor.ExtractTop(query, choices, processor, s, limit, c));
    }

    /// <summary>
    /// Creates a sorted list of ExtractedResult  which contain the
    /// top limit most similar choices
    /// </summary>
    /// <param name="query"></param>
    /// <param name="choices"></param>
    /// <param name="extractor"></param>
    /// <param name="processor"></param>
    /// <param name="scorer"></param>
    /// <param name="limit"></param>
    /// <param name="cutoff"></param>
    /// <returns></returns>
    public static IEnumerable<ExtractedResult<T>> ExtractTopBy<T>(
        string query,
        IEnumerable<T> choices,
        Func<T, string> extractor,
        Func<string, string> processor = null,
        IRatioScorer scorer = null,
        int limit = 5,
        double cutoff = 0)
    {
        ResultExtractor.ValidateLimit(limit);
        processor ??= DefaultStringProcessor;
        scorer ??= DefaultScorer;
        return DistanceScoring.Run(scorer, cutoff, (s, c) => ResultExtractor.ExtractTop(query, choices, extractor, processor, s, limit, c));
    }

    /// <summary>
    /// Creates a sorted list of ExtractedResult  which contain the
    /// top limit most similar choices
    /// </summary>
    /// <param name="query"></param>
    /// <param name="choices"></param>
    /// <param name="extractor"></param>
    /// <param name="processor"></param>
    /// <param name="scorer"></param>
    /// <param name="limit"></param>
    /// <param name="cutoff"></param>
    /// <returns></returns>
    public static IEnumerable<ExtractedResult<T>> ExtractTopBy<T>(
        T query,
        IEnumerable<T> choices,
        Func<T, string> extractor,
        Func<string, string> processor = null,
        IRatioScorer scorer = null,
        int limit = 5,
        double cutoff = 0)
    {
        ResultExtractor.ValidateLimit(limit);
        processor ??= DefaultStringProcessor;
        scorer ??= DefaultScorer;
        return DistanceScoring.Run(scorer, cutoff, (s, c) => ResultExtractor.ExtractTop(query, choices, extractor, processor, s, limit, c));
    }

    #endregion

    #region ExtractSorted
    
    /// <summary>
    /// Creates a sorted list of ExtractedResult with the closest matches first
    /// </summary>
    /// <param name="query"></param>
    /// <param name="choices"></param>
    /// <param name="processor"></param>
    /// <param name="scorer"></param>
    /// <param name="cutoff"></param>
    /// <returns></returns>
    public static IEnumerable<ExtractedResult<string>> ExtractSorted(
        string query,
        IEnumerable<string> choices,
        Func<string, string> processor = null,
        IRatioScorer scorer = null,
        double cutoff = 0)
    {
        processor ??= DefaultStringProcessor;
        scorer ??= DefaultScorer;
        return DistanceScoring.Run(scorer, cutoff, (s, c) => ResultExtractor.ExtractSorted(query, choices, processor, s, c));
    }

    /// <summary>
    /// Creates a sorted list of ExtractedResult with the closest matches first
    /// </summary>
    /// <param name="query"></param>
    /// <param name="choices"></param>
    /// <param name="extractor"></param>
    /// <param name="processor"></param>
    /// <param name="scorer"></param>
    /// <param name="cutoff"></param>
    /// <returns></returns>
    public static IEnumerable<ExtractedResult<T>> ExtractSortedBy<T>(
        T query,
        IEnumerable<T> choices,
        Func<T, string> extractor,
        Func<string, string> processor = null,
        IRatioScorer scorer = null,
        double cutoff = 0)
    {
        processor ??= DefaultStringProcessor;
        scorer ??= DefaultScorer;
        return DistanceScoring.Run(scorer, cutoff, (s, c) => ResultExtractor.ExtractSorted(query, choices, extractor, processor, s, c));
    }

    /// <summary>
    /// Creates a sorted list of ExtractedResult with the closest matches first
    /// </summary>
    /// <param name="query"></param>
    /// <param name="choices"></param>
    /// <param name="extractor"></param>
    /// <param name="processor"></param>
    /// <param name="scorer"></param>
    /// <param name="cutoff"></param>
    /// <returns></returns>
    public static IEnumerable<ExtractedResult<T>> ExtractSortedBy<T>(
        string query,
        IEnumerable<T> choices,
        Func<T, string> extractor,
        Func<string, string> processor = null,
        IRatioScorer scorer = null,
        double cutoff = 0)
    {
        processor ??= DefaultStringProcessor;
        scorer ??= DefaultScorer;
        return DistanceScoring.Run(scorer, cutoff, (s, c) => ResultExtractor.ExtractSorted(query, choices, extractor, processor, s, c));
    }

    #endregion

    #region ExtractOne
    /// <summary>
    /// Find the single best match above a score in a list of choices.
    /// </summary>
    /// <param name="query"></param>
    /// <param name="choices"></param>
    /// <param name="processor"></param>
    /// <param name="scorer"></param>
    /// <param name="cutoff"></param>
    /// <returns></returns>
    public static ExtractedResult<string> ExtractOne(
        string query, 
        IEnumerable<string> choices,
        Func<string, string> processor = null,
        IRatioScorer scorer = null,
        double cutoff = 0)
    {
        processor ??= DefaultStringProcessor;
        scorer ??= DefaultScorer;
        return DistanceScoring.Run(scorer, cutoff, (s, c) => ResultExtractor.ExtractOne(query, choices, processor, s, c));
    }

    /// <summary>
    /// Find the single best match above a score in a list of choices.
    /// </summary>
    /// <param name="query"></param>
    /// <param name="choices"></param>
    /// <param name="extractor"></param>
    /// <param name="processor"></param>
    /// <param name="scorer"></param>
    /// <param name="cutoff"></param>
    /// <returns></returns>
    public static ExtractedResult<T> ExtractOneBy<T>(
        T query,
        IEnumerable<T> choices,
        Func<T, string> extractor,
        Func<string, string> processor = null,
        IRatioScorer scorer = null,
        double cutoff = 0)
    {
        processor ??= DefaultStringProcessor;
        scorer ??= DefaultScorer;
        return DistanceScoring.Run(scorer, cutoff, (s, c) => ResultExtractor.ExtractOne(query, choices, extractor, processor, s, c));
    }

    /// <summary>
    /// Find the single best match above a score in a list of choices.
    /// </summary>
    /// <param name="query"></param>
    /// <param name="choices"></param>
    /// <param name="extractor"></param>
    /// <param name="processor"></param>
    /// <param name="scorer"></param>
    /// <param name="cutoff"></param>
    /// <returns></returns>
    public static ExtractedResult<T> ExtractOneBy<T>(
        string query,
        IEnumerable<T> choices,
        Func<T, string> extractor,
        Func<string, string> processor = null,
        IRatioScorer scorer = null,
        double cutoff = 0)
    {
        processor ??= DefaultStringProcessor;
        scorer ??= DefaultScorer;
        return DistanceScoring.Run(scorer, cutoff, (s, c) => ResultExtractor.ExtractOne(query, choices, extractor, processor, s, c));
    }

    /// <summary>
    /// Find the single best match above a score in a list of choices.
    /// </summary>
    /// <param name="query"></param>
    /// <param name="choices"></param>
    /// <returns></returns>
    public static ExtractedResult<string> ExtractOne(string query, params string[] choices)
    {
        return ResultExtractor.ExtractOne(query, choices, DefaultStringProcessor, DefaultScorer);
    }

    #endregion
}
