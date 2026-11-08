using Fuzzion.SimilarityRatio.Scorer;
using Fuzzion.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fuzzion.Extractor;

public static partial class ResultExtractor
{
    private static readonly ParallelOptions DefaultParallelOptions = new ParallelOptions();

    /// <summary>
    /// The <see cref="ResultExtractor"/> searches, with the choices scored on several threads
    /// by <see cref="System.Threading.Tasks.Parallel"/>. The choices are copied to a list
    /// first, and the processor is required (there is no default).
    /// </summary>
    public static partial class Parallel
    {
        private static ExtractedResult<T> ExtractOneParallelCore<T>(
            IList<T> choices,
            Func<T, double> scoreSelector,
            double cutoff,
            ParallelOptions parallelOptions)
        {
            var sync = new object();
            var globalBest = new BestCandidate<T>();

            System.Threading.Tasks.Parallel.For(
                0,
                choices.Count,
                parallelOptions ?? DefaultParallelOptions,
                () => new BestCandidate<T>(),
                (index, _, localBest) =>
                {
                    var choice = choices[index];
                    localBest.Consider(choice, scoreSelector(choice), index, cutoff);
                    return localBest;
                },
                localBest =>
                {
                    if (localBest.HasValue)
                    {
                        lock (sync)
                        {
                            globalBest.Consider(localBest);
                        }
                    }
                });

            return globalBest.HasValue 
                ? new ExtractedResult<T>(globalBest.Candidate.Value, globalBest.Candidate.Score, globalBest.Candidate.Index) 
                : null;
        }

        private static IEnumerable<ExtractedResult<T>> ExtractTopParallelCore<T>(
            IList<T> choices,
            double[] scores,
            int limit,
            double cutoff)
        {
            ValidateLimit(limit);
            if (limit == 0)
                yield break;

            var heap = new MinHeap<ScoredCandidate<T>>(ScoredCandidateComparer<T>.Instance);
            var comparer = ScoredCandidateComparer<T>.Instance;

            for (var index = 0; index < scores.Length; index++)
            {
                var score = scores[index];
                if (score >= cutoff)
                {
                    AddTopCandidate(heap, comparer, new ScoredCandidate<T>(choices[index], score, index), limit);
                }
            }

            foreach (var result in CreateTopResults(heap))
            {
                yield return result;
            }
        }

        private static double[] ScoreParallel<T>(
            IList<T> choices,
            Func<T, double> scoreSelector,
            ParallelOptions parallelOptions)
        {
            var scores = new double[choices.Count];

            System.Threading.Tasks.Parallel.For(0, choices.Count, parallelOptions ?? DefaultParallelOptions, index =>
            {
                scores[index] = scoreSelector(choices[index]);
            });

            return scores;
        }

        /// <summary>
        /// Scores every choice against the query and returns those that reach <paramref name="cutoff"/>, in their original order. The choices are scored in parallel.
        /// </summary>
        /// <typeparam name="T">The type of the choices.</typeparam>
        /// <param name="query">The query; processed with <paramref name="processor"/> like the choices.</param>
        /// <param name="choices">The choices to search; copied to a list before scoring.</param>
        /// <param name="extractor">Gets the text to compare from each choice.</param>
        /// <param name="processor">Applied to the query and to every choice's text before scoring; must not be null (pass <see cref="Fuzzion.PreProcess.StringPreprocessor.None"/> to compare the text as is).</param>
        /// <param name="scorer">Compares the processed query with each processed choice; must not be null. Higher scores count as better. It is called from several threads at once, so it must be thread-safe.</param>
        /// <param name="cutoff">The minimum score a choice needs to be returned (inclusive); 0 by default.</param>
        /// <param name="parallelOptions">Options for the parallel loop, such as the maximum degree of parallelism or a cancellation token; the defaults when null.</param>
        /// <returns>The matching choices in their original order (the parallel scoring does not reorder them). The scoring is done before the method returns.</returns>
        public static IEnumerable<ExtractedResult<T>> ExtractWithoutOrder<T>(string query, IEnumerable<T> choices, Func<T, string> extractor, Func<string, string> processor, IRatioScorer scorer, double cutoff = 0, ParallelOptions parallelOptions = null)
        {
            var materializedChoices = choices.ToList();
            var result = new ExtractedResult<T>[materializedChoices.Count];
            var processedQuery = processor(query);

            System.Threading.Tasks.Parallel.ForEach(materializedChoices, parallelOptions ?? DefaultParallelOptions, (choice, _, index) =>
            {
                double score = scorer.Score(processedQuery, processor(extractor(choice)));
                if (score >= cutoff)
                {
                    result[index] = new ExtractedResult<T>(choice, score, (int)index);
                }
            });
            return result.Where(r => r != null);
        }

        /// <summary>
        /// Scores every choice against the query and returns those that reach <paramref name="cutoff"/>, in their original order. The choices are scored in parallel.
        /// </summary>
        /// <param name="query">The query; processed with <paramref name="processor"/> like the choices.</param>
        /// <param name="choices">The choices to search; copied to a list before scoring.</param>
        /// <param name="processor">Applied to the query and to every choice before scoring; must not be null (pass <see cref="Fuzzion.PreProcess.StringPreprocessor.None"/> to compare the text as is).</param>
        /// <param name="scorer">Compares the processed query with each processed choice; must not be null. Higher scores count as better. It is called from several threads at once, so it must be thread-safe.</param>
        /// <param name="cutoff">The minimum score a choice needs to be returned (inclusive); 0 by default.</param>
        /// <param name="parallelOptions">Options for the parallel loop, such as the maximum degree of parallelism or a cancellation token; the defaults when null.</param>
        /// <returns>The matching choices in their original order (the parallel scoring does not reorder them). The scoring is done before the method returns.</returns>
        public static IEnumerable<ExtractedResult<string>> ExtractWithoutOrder(string query, IEnumerable<string> choices, Func<string, string> processor, IRatioScorer scorer, double cutoff = 0, ParallelOptions parallelOptions = null)
        {
            var materializedChoices = choices.ToList();
            var result = new ExtractedResult<string>[materializedChoices.Count];
            var processedQuery = processor(query);

            System.Threading.Tasks.Parallel.ForEach(materializedChoices, parallelOptions ?? DefaultParallelOptions, (choice, _, index) =>
            {
                double score = scorer.Score(processedQuery, processor(choice));
                if (score >= cutoff)
                {
                    result[index] = new ExtractedResult<string>(choice, score, (int)index);
                }
            });
            return result.Where(r => r != null);
        }

        /// <summary>
        /// Scores every choice against the query and returns those that reach <paramref name="cutoff"/>, in their original order. The choices are scored in parallel.
        /// </summary>
        /// <typeparam name="T">The type of the query and the choices.</typeparam>
        /// <param name="query">The query; its text is taken with <paramref name="extractor"/> and processed like the choices.</param>
        /// <param name="choices">The choices to search; copied to a list before scoring.</param>
        /// <param name="extractor">Gets the text to compare from the query and from each choice.</param>
        /// <param name="processor">Applied to the query and to every choice's text before scoring; must not be null (pass <see cref="Fuzzion.PreProcess.StringPreprocessor.None"/> to compare the text as is).</param>
        /// <param name="scorer">Compares the processed query with each processed choice; must not be null. Higher scores count as better. It is called from several threads at once, so it must be thread-safe.</param>
        /// <param name="cutoff">The minimum score a choice needs to be returned (inclusive); 0 by default.</param>
        /// <param name="parallelOptions">Options for the parallel loop, such as the maximum degree of parallelism or a cancellation token; the defaults when null.</param>
        /// <returns>The matching choices in their original order (the parallel scoring does not reorder them). The scoring is done before the method returns.</returns>
        public static IEnumerable<ExtractedResult<T>> ExtractWithoutOrder<T>(T query, IEnumerable<T> choices, Func<T, string> extractor, Func<string, string> processor, IRatioScorer scorer, double cutoff = 0, ParallelOptions parallelOptions = null)
        {
            return ExtractWithoutOrder(extractor(query), choices, extractor, processor, scorer, cutoff, parallelOptions);
        }

        /// <summary>
        /// Finds the choice with the highest score against the query, if it reaches <paramref name="cutoff"/>. The choices are scored in parallel.
        /// </summary>
        /// <typeparam name="T">The type of the query and the choices.</typeparam>
        /// <param name="query">The query; its text is taken with <paramref name="extractor"/> and processed like the choices.</param>
        /// <param name="choices">The choices to search; copied to a list before scoring.</param>
        /// <param name="extractor">Gets the text to compare from the query and from each choice.</param>
        /// <param name="processor">Applied to the query and to every choice's text before scoring; must not be null (pass <see cref="Fuzzion.PreProcess.StringPreprocessor.None"/> to compare the text as is).</param>
        /// <param name="calculator">Compares the processed query with each processed choice; must not be null. Higher scores count as better. It is called from several threads at once, so it must be thread-safe.</param>
        /// <param name="cutoff">The minimum score a choice needs to be returned (inclusive); 0 by default.</param>
        /// <param name="parallelOptions">Options for the parallel loop, such as the maximum degree of parallelism or a cancellation token; the defaults when null.</param>
        /// <returns>The best match (the earliest choice when several share the top score), or <c>null</c> when no choice reaches <paramref name="cutoff"/>.</returns>
        public static ExtractedResult<T> ExtractOne<T>(T query, IEnumerable<T> choices, Func<T, string> extractor, Func<string, string> processor, IRatioScorer calculator, double cutoff = 0, ParallelOptions parallelOptions = null)
        {
            var materializedChoices = choices.ToList();
            var processedQuery = processor(extractor(query));
            return ExtractOneParallelCore(materializedChoices, choice => calculator.Score(processedQuery, processor(extractor(choice))), cutoff, parallelOptions);
        }

        /// <summary>
        /// Finds the choice with the highest score against the query, if it reaches <paramref name="cutoff"/>. The choices are scored in parallel.
        /// </summary>
        /// <param name="query">The query; processed with <paramref name="processor"/> like the choices.</param>
        /// <param name="choices">The choices to search; copied to a list before scoring.</param>
        /// <param name="processor">Applied to the query and to every choice before scoring; must not be null (pass <see cref="Fuzzion.PreProcess.StringPreprocessor.None"/> to compare the text as is).</param>
        /// <param name="calculator">Compares the processed query with each processed choice; must not be null. Higher scores count as better. It is called from several threads at once, so it must be thread-safe.</param>
        /// <param name="cutoff">The minimum score a choice needs to be returned (inclusive); 0 by default.</param>
        /// <param name="parallelOptions">Options for the parallel loop, such as the maximum degree of parallelism or a cancellation token; the defaults when null.</param>
        /// <returns>The best match (the earliest choice when several share the top score), or <c>null</c> when no choice reaches <paramref name="cutoff"/>.</returns>
        public static ExtractedResult<string> ExtractOne(string query, IEnumerable<string> choices, Func<string, string> processor, IRatioScorer calculator, double cutoff = 0, ParallelOptions parallelOptions = null)
        {
            var materializedChoices = choices.ToList();
            var processedQuery = processor(query);
            return ExtractOneParallelCore(materializedChoices, choice => calculator.Score(processedQuery, processor(choice)), cutoff, parallelOptions);
        }

        /// <summary>
        /// Finds the choice with the highest score against the query, if it reaches <paramref name="cutoff"/>. The choices are scored in parallel.
        /// </summary>
        /// <typeparam name="T">The type of the choices.</typeparam>
        /// <param name="query">The query; processed with <paramref name="processor"/> like the choices.</param>
        /// <param name="choices">The choices to search; copied to a list before scoring.</param>
        /// <param name="extractor">Gets the text to compare from each choice.</param>
        /// <param name="processor">Applied to the query and to every choice's text before scoring; must not be null (pass <see cref="Fuzzion.PreProcess.StringPreprocessor.None"/> to compare the text as is).</param>
        /// <param name="calculator">Compares the processed query with each processed choice; must not be null. Higher scores count as better. It is called from several threads at once, so it must be thread-safe.</param>
        /// <param name="cutoff">The minimum score a choice needs to be returned (inclusive); 0 by default.</param>
        /// <param name="parallelOptions">Options for the parallel loop, such as the maximum degree of parallelism or a cancellation token; the defaults when null.</param>
        /// <returns>The best match (the earliest choice when several share the top score), or <c>null</c> when no choice reaches <paramref name="cutoff"/>.</returns>
        public static ExtractedResult<T> ExtractOne<T>(string query, IEnumerable<T> choices, Func<T, string> extractor, Func<string, string> processor, IRatioScorer calculator, double cutoff = 0, ParallelOptions parallelOptions = null)
        {
            var materializedChoices = choices.ToList();
            var processedQuery = processor(query);
            return ExtractOneParallelCore(materializedChoices, choice => calculator.Score(processedQuery, processor(extractor(choice))), cutoff, parallelOptions);
        }

        /// <summary>
        /// Scores every choice against the query and returns those that reach <paramref name="cutoff"/>, highest score first. The choices are scored in parallel.
        /// </summary>
        /// <typeparam name="T">The type of the query and the choices.</typeparam>
        /// <param name="query">The query; its text is taken with <paramref name="extractor"/> and processed like the choices.</param>
        /// <param name="choices">The choices to search; copied to a list before scoring.</param>
        /// <param name="extractor">Gets the text to compare from the query and from each choice.</param>
        /// <param name="processor">Applied to the query and to every choice's text before scoring; must not be null (pass <see cref="Fuzzion.PreProcess.StringPreprocessor.None"/> to compare the text as is).</param>
        /// <param name="calculator">Compares the processed query with each processed choice; must not be null. Higher scores count as better. It is called from several threads at once, so it must be thread-safe.</param>
        /// <param name="cutoff">The minimum score a choice needs to be returned (inclusive); 0 by default.</param>
        /// <param name="parallelOptions">Options for the parallel loop, such as the maximum degree of parallelism or a cancellation token; the defaults when null.</param>
        /// <returns>The matching choices, highest score first; choices with equal scores keep their original order. The scoring is done before the method returns.</returns>
        public static IEnumerable<ExtractedResult<T>> ExtractSorted<T>(T query, IEnumerable<T> choices, Func<T, string> extractor, Func<string, string> processor, IRatioScorer calculator, double cutoff = 0, ParallelOptions parallelOptions = null)
        {
            return ExtractWithoutOrder(query, choices, extractor, processor, calculator, cutoff, parallelOptions).OrderByDescending(r => r.Score);
        }

        /// <summary>
        /// Scores every choice against the query and returns those that reach <paramref name="cutoff"/>, highest score first. The choices are scored in parallel.
        /// </summary>
        /// <param name="query">The query; processed with <paramref name="processor"/> like the choices.</param>
        /// <param name="choices">The choices to search; copied to a list before scoring.</param>
        /// <param name="processor">Applied to the query and to every choice before scoring; must not be null (pass <see cref="Fuzzion.PreProcess.StringPreprocessor.None"/> to compare the text as is).</param>
        /// <param name="calculator">Compares the processed query with each processed choice; must not be null. Higher scores count as better. It is called from several threads at once, so it must be thread-safe.</param>
        /// <param name="cutoff">The minimum score a choice needs to be returned (inclusive); 0 by default.</param>
        /// <param name="parallelOptions">Options for the parallel loop, such as the maximum degree of parallelism or a cancellation token; the defaults when null.</param>
        /// <returns>The matching choices, highest score first; choices with equal scores keep their original order. The scoring is done before the method returns.</returns>
        public static IEnumerable<ExtractedResult<string>> ExtractSorted(string query, IEnumerable<string> choices, Func<string, string> processor, IRatioScorer calculator, double cutoff = 0, ParallelOptions parallelOptions = null)
        {
            return ExtractWithoutOrder(query, choices, processor, calculator, cutoff, parallelOptions).OrderByDescending(r => r.Score);
        }

        /// <summary>
        /// Scores every choice against the query and returns those that reach <paramref name="cutoff"/>, highest score first. The choices are scored in parallel.
        /// </summary>
        /// <typeparam name="T">The type of the choices.</typeparam>
        /// <param name="query">The query; processed with <paramref name="processor"/> like the choices.</param>
        /// <param name="choices">The choices to search; copied to a list before scoring.</param>
        /// <param name="extractor">Gets the text to compare from each choice.</param>
        /// <param name="processor">Applied to the query and to every choice's text before scoring; must not be null (pass <see cref="Fuzzion.PreProcess.StringPreprocessor.None"/> to compare the text as is).</param>
        /// <param name="calculator">Compares the processed query with each processed choice; must not be null. Higher scores count as better. It is called from several threads at once, so it must be thread-safe.</param>
        /// <param name="cutoff">The minimum score a choice needs to be returned (inclusive); 0 by default.</param>
        /// <param name="parallelOptions">Options for the parallel loop, such as the maximum degree of parallelism or a cancellation token; the defaults when null.</param>
        /// <returns>The matching choices, highest score first; choices with equal scores keep their original order. The scoring is done before the method returns.</returns>
        public static IEnumerable<ExtractedResult<T>> ExtractSorted<T>(string query, IEnumerable<T> choices, Func<T, string> extractor, Func<string, string> processor, IRatioScorer calculator, double cutoff = 0, ParallelOptions parallelOptions = null)
        {
            return ExtractWithoutOrder(query, choices, extractor, processor, calculator, cutoff, parallelOptions).OrderByDescending(r => r.Score);
        }

        /// <summary>
        /// Finds the <paramref name="limit"/> choices with the highest scores against the query that reach <paramref name="cutoff"/>. The choices are scored in parallel.
        /// </summary>
        /// <typeparam name="T">The type of the query and the choices.</typeparam>
        /// <param name="query">The query; its text is taken with <paramref name="extractor"/> and processed like the choices.</param>
        /// <param name="choices">The choices to search; copied to a list before scoring.</param>
        /// <param name="extractor">Gets the text to compare from the query and from each choice.</param>
        /// <param name="processor">Applied to the query and to every choice's text before scoring; must not be null (pass <see cref="Fuzzion.PreProcess.StringPreprocessor.None"/> to compare the text as is).</param>
        /// <param name="calculator">Compares the processed query with each processed choice; must not be null. Higher scores count as better. It is called from several threads at once, so it must be thread-safe.</param>
        /// <param name="limit">The maximum number of results; 0 returns none. Must not be negative.</param>
        /// <param name="cutoff">The minimum score a choice needs to be returned (inclusive); 0 by default.</param>
        /// <param name="parallelOptions">Options for the parallel loop, such as the maximum degree of parallelism or a cancellation token; the defaults when null.</param>
        /// <returns>At most <paramref name="limit"/> matches, highest score first. When scores are equal, which choices are kept and their order is not specified. The scoring is done before the method returns.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="limit"/> is negative.</exception>
        public static IEnumerable<ExtractedResult<T>> ExtractTop<T>(T query, IEnumerable<T> choices, Func<T, string> extractor, Func<string, string> processor, IRatioScorer calculator, int limit, double cutoff = 0, ParallelOptions parallelOptions = null)
        {
            ValidateLimit(limit);
            if (limit == 0)
                return Enumerable.Empty<ExtractedResult<T>>();

            var materializedChoices = choices.ToList();
            var processedQuery = processor(extractor(query));
            var scores = ScoreParallel(materializedChoices, choice => calculator.Score(processedQuery, processor(extractor(choice))), parallelOptions);
            return ExtractTopParallelCore(materializedChoices, scores, limit, cutoff);
        }

        /// <summary>
        /// Finds the <paramref name="limit"/> choices with the highest scores against the query that reach <paramref name="cutoff"/>. The choices are scored in parallel.
        /// </summary>
        /// <param name="query">The query; processed with <paramref name="processor"/> like the choices.</param>
        /// <param name="choices">The choices to search; copied to a list before scoring.</param>
        /// <param name="processor">Applied to the query and to every choice before scoring; must not be null (pass <see cref="Fuzzion.PreProcess.StringPreprocessor.None"/> to compare the text as is).</param>
        /// <param name="calculator">Compares the processed query with each processed choice; must not be null. Higher scores count as better. It is called from several threads at once, so it must be thread-safe.</param>
        /// <param name="limit">The maximum number of results; 0 returns none. Must not be negative.</param>
        /// <param name="cutoff">The minimum score a choice needs to be returned (inclusive); 0 by default.</param>
        /// <param name="parallelOptions">Options for the parallel loop, such as the maximum degree of parallelism or a cancellation token; the defaults when null.</param>
        /// <returns>At most <paramref name="limit"/> matches, highest score first. When scores are equal, which choices are kept and their order is not specified. The scoring is done before the method returns.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="limit"/> is negative.</exception>
        public static IEnumerable<ExtractedResult<string>> ExtractTop(string query, IEnumerable<string> choices, Func<string, string> processor, IRatioScorer calculator, int limit, double cutoff = 0, ParallelOptions parallelOptions = null)
        {
            ValidateLimit(limit);
            if (limit == 0)
                return Enumerable.Empty<ExtractedResult<string>>();

            var materializedChoices = choices.ToList();
            var processedQuery = processor(query);
            var scores = ScoreParallel(materializedChoices, choice => calculator.Score(processedQuery, processor(choice)), parallelOptions);
            return ExtractTopParallelCore(materializedChoices, scores, limit, cutoff);
        }

        /// <summary>
        /// Finds the <paramref name="limit"/> choices with the highest scores against the query that reach <paramref name="cutoff"/>. The choices are scored in parallel.
        /// </summary>
        /// <typeparam name="T">The type of the choices.</typeparam>
        /// <param name="query">The query; processed with <paramref name="processor"/> like the choices.</param>
        /// <param name="choices">The choices to search; copied to a list before scoring.</param>
        /// <param name="extractor">Gets the text to compare from each choice.</param>
        /// <param name="processor">Applied to the query and to every choice's text before scoring; must not be null (pass <see cref="Fuzzion.PreProcess.StringPreprocessor.None"/> to compare the text as is).</param>
        /// <param name="calculator">Compares the processed query with each processed choice; must not be null. Higher scores count as better. It is called from several threads at once, so it must be thread-safe.</param>
        /// <param name="limit">The maximum number of results; 0 returns none. Must not be negative.</param>
        /// <param name="cutoff">The minimum score a choice needs to be returned (inclusive); 0 by default.</param>
        /// <param name="parallelOptions">Options for the parallel loop, such as the maximum degree of parallelism or a cancellation token; the defaults when null.</param>
        /// <returns>At most <paramref name="limit"/> matches, highest score first. When scores are equal, which choices are kept and their order is not specified. The scoring is done before the method returns.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="limit"/> is negative.</exception>
        public static IEnumerable<ExtractedResult<T>> ExtractTop<T>(string query, IEnumerable<T> choices, Func<T, string> extractor, Func<string, string> processor, IRatioScorer calculator, int limit, double cutoff = 0, ParallelOptions parallelOptions = null)
        {
            ValidateLimit(limit);
            if (limit == 0)
                return Enumerable.Empty<ExtractedResult<T>>();

            var materializedChoices = choices.ToList();
            var processedQuery = processor(query);
            var scores = ScoreParallel(materializedChoices, choice => calculator.Score(processedQuery, processor(extractor(choice))), parallelOptions);
            return ExtractTopParallelCore(materializedChoices, scores, limit, cutoff);
        }
    }
}
