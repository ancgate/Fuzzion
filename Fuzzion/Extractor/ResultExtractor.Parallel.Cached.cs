using Fuzzion.SimilarityRatio.Scorer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fuzzion.Extractor;

public static partial class ResultExtractor
{
    public static partial class Parallel
    {
        /// <summary>
        /// The <see cref="ResultExtractor.Cached"/> searches, with the choices scored on several
        /// threads. The one cached scorer is shared by all threads, so it must be thread-safe.
        /// </summary>
        public static class Cached
        {
            /// <summary>
            /// Scores every choice against the query held by <paramref name="scorer"/> and returns those that reach <paramref name="cutoff"/>, in their original order. The choices are scored in parallel.
            /// </summary>
            /// <typeparam name="T">The type of the choices.</typeparam>
            /// <param name="choices">The choices to search; copied to a list before scoring.</param>
            /// <param name="extractor">Gets the text to compare from each choice.</param>
            /// <param name="processor">Applied to every choice's text before it is passed to <paramref name="scorer"/>; must not be null. The query is not processed here: the cached scorer prepared it when it was created.</param>
            /// <param name="scorer">A cached scorer that already holds the query; it scores each processed choice. Must not be null. It is called from several threads at once, so it must be thread-safe.</param>
            /// <param name="cutoff">The minimum score a choice needs to be returned (inclusive); 0 by default.</param>
            /// <param name="parallelOptions">Options for the parallel loop, such as the maximum degree of parallelism or a cancellation token; the defaults when null.</param>
            /// <returns>The matching choices in their original order (the parallel scoring does not reorder them). The scoring is done before the method returns.</returns>
            public static IEnumerable<ExtractedResult<T>> ExtractWithoutOrder<T>(IEnumerable<T> choices, Func<T, string> extractor, Func<string, string> processor, ICachedRatioScorer scorer, double cutoff = 0, ParallelOptions parallelOptions = null)
            {
                var materializedChoices = choices.ToList();
                var result = new ExtractedResult<T>[materializedChoices.Count];

                System.Threading.Tasks.Parallel.ForEach(materializedChoices, parallelOptions ?? DefaultParallelOptions, (choice, _, index) =>
                {
                    double score = scorer.Score(processor(extractor(choice)));
                    if (score >= cutoff)
                    {
                        result[index] = new ExtractedResult<T>(choice, score, (int)index);
                    }
                });
                return result.Where(r => r != null);
            }

            /// <summary>
            /// Scores every choice against the query held by <paramref name="scorer"/> and returns those that reach <paramref name="cutoff"/>, in their original order. The choices are scored in parallel.
            /// </summary>
            /// <param name="choices">The choices to search; copied to a list before scoring.</param>
            /// <param name="processor">Applied to every choice before it is passed to <paramref name="scorer"/>; must not be null. The query is not processed here: the cached scorer prepared it when it was created.</param>
            /// <param name="scorer">A cached scorer that already holds the query; it scores each processed choice. Must not be null. It is called from several threads at once, so it must be thread-safe.</param>
            /// <param name="cutoff">The minimum score a choice needs to be returned (inclusive); 0 by default.</param>
            /// <param name="parallelOptions">Options for the parallel loop, such as the maximum degree of parallelism or a cancellation token; the defaults when null.</param>
            /// <returns>The matching choices in their original order (the parallel scoring does not reorder them). The scoring is done before the method returns.</returns>
            public static IEnumerable<ExtractedResult<string>> ExtractWithoutOrder(IEnumerable<string> choices, Func<string, string> processor, ICachedRatioScorer scorer, double cutoff = 0, ParallelOptions parallelOptions = null)
            {
                var materializedChoices = choices.ToList();
                var result = new ExtractedResult<string>[materializedChoices.Count];

                System.Threading.Tasks.Parallel.ForEach(materializedChoices, parallelOptions ?? DefaultParallelOptions, (choice, _, index) =>
                {
                    double score = scorer.Score(processor(choice));
                    if (score >= cutoff)
                    {
                        result[index] = new ExtractedResult<string>(choice, score, (int)index);
                    }
                });
                return result.Where(r => r != null);
            }

            /// <summary>
            /// Finds the choice with the highest score against the query held by <paramref name="calculator"/>, if it reaches <paramref name="cutoff"/>. The choices are scored in parallel.
            /// </summary>
            /// <typeparam name="T">The type of the choices.</typeparam>
            /// <param name="choices">The choices to search; copied to a list before scoring.</param>
            /// <param name="extractor">Gets the text to compare from each choice.</param>
            /// <param name="processor">Applied to every choice's text before it is passed to <paramref name="calculator"/>; must not be null. The query is not processed here: the cached scorer prepared it when it was created.</param>
            /// <param name="calculator">A cached scorer that already holds the query; it scores each processed choice. Must not be null. It is called from several threads at once, so it must be thread-safe.</param>
            /// <param name="cutoff">The minimum score a choice needs to be returned (inclusive); 0 by default.</param>
            /// <param name="parallelOptions">Options for the parallel loop, such as the maximum degree of parallelism or a cancellation token; the defaults when null.</param>
            /// <returns>The best match (the earliest choice when several share the top score), or <c>null</c> when no choice reaches <paramref name="cutoff"/>.</returns>
            public static ExtractedResult<T> ExtractOne<T>(IEnumerable<T> choices, Func<T, string> extractor, Func<string, string> processor, ICachedRatioScorer calculator, double cutoff = 0, ParallelOptions parallelOptions = null)
            {
                var materializedChoices = choices.ToList();
                return ExtractOneParallelCore(materializedChoices, choice => calculator.Score(processor(extractor(choice))), cutoff, parallelOptions);
            }

            /// <summary>
            /// Finds the choice with the highest score against the query held by <paramref name="calculator"/>, if it reaches <paramref name="cutoff"/>. The choices are scored in parallel.
            /// </summary>
            /// <param name="choices">The choices to search; copied to a list before scoring.</param>
            /// <param name="processor">Applied to every choice before it is passed to <paramref name="calculator"/>; must not be null. The query is not processed here: the cached scorer prepared it when it was created.</param>
            /// <param name="calculator">A cached scorer that already holds the query; it scores each processed choice. Must not be null. It is called from several threads at once, so it must be thread-safe.</param>
            /// <param name="cutoff">The minimum score a choice needs to be returned (inclusive); 0 by default.</param>
            /// <param name="parallelOptions">Options for the parallel loop, such as the maximum degree of parallelism or a cancellation token; the defaults when null.</param>
            /// <returns>The best match (the earliest choice when several share the top score), or <c>null</c> when no choice reaches <paramref name="cutoff"/>.</returns>
            public static ExtractedResult<string> ExtractOne(IEnumerable<string> choices, Func<string, string> processor, ICachedRatioScorer calculator, double cutoff = 0, ParallelOptions parallelOptions = null)
            {
                var materializedChoices = choices.ToList();
                return ExtractOneParallelCore(materializedChoices, choice => calculator.Score(processor(choice)), cutoff, parallelOptions);
            }

            /// <summary>
            /// Scores every choice against the query held by <paramref name="calculator"/> and returns those that reach <paramref name="cutoff"/>, highest score first. The choices are scored in parallel.
            /// </summary>
            /// <typeparam name="T">The type of the choices.</typeparam>
            /// <param name="choices">The choices to search; copied to a list before scoring.</param>
            /// <param name="extractor">Gets the text to compare from each choice.</param>
            /// <param name="processor">Applied to every choice's text before it is passed to <paramref name="calculator"/>; must not be null. The query is not processed here: the cached scorer prepared it when it was created.</param>
            /// <param name="calculator">A cached scorer that already holds the query; it scores each processed choice. Must not be null. It is called from several threads at once, so it must be thread-safe.</param>
            /// <param name="cutoff">The minimum score a choice needs to be returned (inclusive); 0 by default.</param>
            /// <param name="parallelOptions">Options for the parallel loop, such as the maximum degree of parallelism or a cancellation token; the defaults when null.</param>
            /// <returns>The matching choices, highest score first; choices with equal scores keep their original order. The scoring is done before the method returns.</returns>
            public static IEnumerable<ExtractedResult<T>> ExtractSorted<T>(IEnumerable<T> choices, Func<T, string> extractor, Func<string, string> processor, ICachedRatioScorer calculator, double cutoff = 0, ParallelOptions parallelOptions = null)
            {
                return ExtractWithoutOrder(choices, extractor, processor, calculator, cutoff, parallelOptions).OrderByDescending(r => r.Score);
            }

            /// <summary>
            /// Scores every choice against the query held by <paramref name="calculator"/> and returns those that reach <paramref name="cutoff"/>, highest score first. The choices are scored in parallel.
            /// </summary>
            /// <param name="choices">The choices to search; copied to a list before scoring.</param>
            /// <param name="processor">Applied to every choice before it is passed to <paramref name="calculator"/>; must not be null. The query is not processed here: the cached scorer prepared it when it was created.</param>
            /// <param name="calculator">A cached scorer that already holds the query; it scores each processed choice. Must not be null. It is called from several threads at once, so it must be thread-safe.</param>
            /// <param name="cutoff">The minimum score a choice needs to be returned (inclusive); 0 by default.</param>
            /// <param name="parallelOptions">Options for the parallel loop, such as the maximum degree of parallelism or a cancellation token; the defaults when null.</param>
            /// <returns>The matching choices, highest score first; choices with equal scores keep their original order. The scoring is done before the method returns.</returns>
            public static IEnumerable<ExtractedResult<string>> ExtractSorted(IEnumerable<string> choices, Func<string, string> processor, ICachedRatioScorer calculator, double cutoff = 0, ParallelOptions parallelOptions = null)
            {
                return ExtractWithoutOrder(choices, processor, calculator, cutoff, parallelOptions).OrderByDescending(r => r.Score);
            }

            /// <summary>
            /// Finds the <paramref name="limit"/> choices with the highest scores against the query held by <paramref name="calculator"/> that reach <paramref name="cutoff"/>. The choices are scored in parallel.
            /// </summary>
            /// <typeparam name="T">The type of the choices.</typeparam>
            /// <param name="choices">The choices to search; copied to a list before scoring.</param>
            /// <param name="extractor">Gets the text to compare from each choice.</param>
            /// <param name="processor">Applied to every choice's text before it is passed to <paramref name="calculator"/>; must not be null. The query is not processed here: the cached scorer prepared it when it was created.</param>
            /// <param name="calculator">A cached scorer that already holds the query; it scores each processed choice. Must not be null. It is called from several threads at once, so it must be thread-safe.</param>
            /// <param name="limit">The maximum number of results; 0 returns none. Must not be negative.</param>
            /// <param name="cutoff">The minimum score a choice needs to be returned (inclusive); 0 by default.</param>
            /// <param name="parallelOptions">Options for the parallel loop, such as the maximum degree of parallelism or a cancellation token; the defaults when null.</param>
            /// <returns>At most <paramref name="limit"/> matches, highest score first. When scores are equal, which choices are kept and their order is not specified. The scoring is done before the method returns.</returns>
            /// <exception cref="ArgumentOutOfRangeException"><paramref name="limit"/> is negative.</exception>
            public static IEnumerable<ExtractedResult<T>> ExtractTop<T>(IEnumerable<T> choices, Func<T, string> extractor, Func<string, string> processor, ICachedRatioScorer calculator, int limit, double cutoff = 0, ParallelOptions parallelOptions = null)
            {
                ValidateLimit(limit);
                if (limit == 0)
                    return Enumerable.Empty<ExtractedResult<T>>();

                var materializedChoices = choices.ToList();
                var scores = ScoreParallel(materializedChoices, choice => calculator.Score(processor(extractor(choice))), parallelOptions);
                return ExtractTopParallelCore(materializedChoices, scores, limit, cutoff);
            }

            /// <summary>
            /// Finds the <paramref name="limit"/> choices with the highest scores against the query held by <paramref name="calculator"/> that reach <paramref name="cutoff"/>. The choices are scored in parallel.
            /// </summary>
            /// <param name="choices">The choices to search; copied to a list before scoring.</param>
            /// <param name="processor">Applied to every choice before it is passed to <paramref name="calculator"/>; must not be null. The query is not processed here: the cached scorer prepared it when it was created.</param>
            /// <param name="calculator">A cached scorer that already holds the query; it scores each processed choice. Must not be null. It is called from several threads at once, so it must be thread-safe.</param>
            /// <param name="limit">The maximum number of results; 0 returns none. Must not be negative.</param>
            /// <param name="cutoff">The minimum score a choice needs to be returned (inclusive); 0 by default.</param>
            /// <param name="parallelOptions">Options for the parallel loop, such as the maximum degree of parallelism or a cancellation token; the defaults when null.</param>
            /// <returns>At most <paramref name="limit"/> matches, highest score first. When scores are equal, which choices are kept and their order is not specified. The scoring is done before the method returns.</returns>
            /// <exception cref="ArgumentOutOfRangeException"><paramref name="limit"/> is negative.</exception>
            public static IEnumerable<ExtractedResult<string>> ExtractTop(IEnumerable<string> choices, Func<string, string> processor, ICachedRatioScorer calculator, int limit, double cutoff = 0, ParallelOptions parallelOptions = null)
            {
                ValidateLimit(limit);
                if (limit == 0)
                    return Enumerable.Empty<ExtractedResult<string>>();

                var materializedChoices = choices.ToList();
                var scores = ScoreParallel(materializedChoices, choice => calculator.Score(processor(choice)), parallelOptions);
                return ExtractTopParallelCore(materializedChoices, scores, limit, cutoff);
            }
        }
    }
}
