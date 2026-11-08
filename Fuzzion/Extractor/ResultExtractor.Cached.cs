using Fuzzion.SimilarityRatio.Scorer;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fuzzion.Extractor;

public static partial class ResultExtractor
{
    /// <summary>
    /// Searches with an <see cref="ICachedRatioScorer"/>, which already holds the prepared
    /// query, so the methods take no query. The processor is required (there is no default).
    /// </summary>
    public static class Cached
    {
        /// <summary>
        /// Scores every choice against the query held by <paramref name="scorer"/> and returns those that reach <paramref name="cutoff"/>, in their original order.
        /// </summary>
        /// <typeparam name="T">The type of the choices.</typeparam>
        /// <param name="choices">The choices to search.</param>
        /// <param name="extractor">Gets the text to compare from each choice.</param>
        /// <param name="processor">Applied to every choice's text before it is passed to <paramref name="scorer"/>; must not be null. The query is not processed here: the cached scorer prepared it when it was created.</param>
        /// <param name="scorer">A cached scorer that already holds the query; it scores each processed choice. Must not be null.</param>
        /// <param name="cutoff">The minimum score a choice needs to be returned (inclusive); 0 by default.</param>
        /// <returns>The matching choices in their original order. Evaluated lazily: the choices are scored each time the result is enumerated.</returns>
        public static IEnumerable<ExtractedResult<T>> ExtractWithoutOrder<T>(IEnumerable<T> choices, Func<T, string> extractor, Func<string, string> processor, ICachedRatioScorer scorer, double cutoff = 0)
        {
            int index = 0;

            foreach (var choice in choices)
            {
                double score = scorer.Score(processor(extractor(choice)));
                if (score >= cutoff)
                {
                    yield return new ExtractedResult<T>(choice, score, index);
                }
                index++;
            }
        }

        /// <summary>
        /// Scores every choice against the query held by <paramref name="scorer"/> and returns those that reach <paramref name="cutoff"/>, in their original order.
        /// </summary>
        /// <param name="choices">The choices to search.</param>
        /// <param name="processor">Applied to every choice before it is passed to <paramref name="scorer"/>; must not be null. The query is not processed here: the cached scorer prepared it when it was created.</param>
        /// <param name="scorer">A cached scorer that already holds the query; it scores each processed choice. Must not be null.</param>
        /// <param name="cutoff">The minimum score a choice needs to be returned (inclusive); 0 by default.</param>
        /// <returns>The matching choices in their original order. Evaluated lazily: the choices are scored each time the result is enumerated.</returns>
        public static IEnumerable<ExtractedResult<string>> ExtractWithoutOrder(IEnumerable<string> choices, Func<string, string> processor, ICachedRatioScorer scorer, double cutoff = 0)
        {
            int index = 0;

            foreach (var choice in choices)
            {
                double score = scorer.Score(processor(choice));
                if (score >= cutoff)
                {
                    yield return new ExtractedResult<string>(choice, score, index);
                }
                index++;
            }
        }

        /// <summary>
        /// Finds the choice with the highest score against the query held by <paramref name="scorer"/>, if it reaches <paramref name="cutoff"/>.
        /// </summary>
        /// <typeparam name="T">The type of the choices.</typeparam>
        /// <param name="choices">The choices to search.</param>
        /// <param name="extractor">Gets the text to compare from each choice.</param>
        /// <param name="processor">Applied to every choice's text before it is passed to <paramref name="scorer"/>; must not be null. The query is not processed here: the cached scorer prepared it when it was created.</param>
        /// <param name="scorer">A cached scorer that already holds the query; it scores each processed choice. Must not be null.</param>
        /// <param name="cutoff">The minimum score a choice needs to be returned (inclusive); 0 by default.</param>
        /// <returns>The best match (the earliest choice when several share the top score), or <c>null</c> when no choice reaches <paramref name="cutoff"/>.</returns>
        public static ExtractedResult<T> ExtractOne<T>(IEnumerable<T> choices, Func<T, string> extractor, Func<string, string> processor, ICachedRatioScorer scorer, double cutoff = 0)
        {
            return ExtractOneCore(choices, choice => scorer.Score(processor(extractor(choice))), cutoff);
        }

        /// <summary>
        /// Finds the choice with the highest score against the query held by <paramref name="scorer"/>, if it reaches <paramref name="cutoff"/>.
        /// </summary>
        /// <param name="choices">The choices to search.</param>
        /// <param name="processor">Applied to every choice before it is passed to <paramref name="scorer"/>; must not be null. The query is not processed here: the cached scorer prepared it when it was created.</param>
        /// <param name="scorer">A cached scorer that already holds the query; it scores each processed choice. Must not be null.</param>
        /// <param name="cutoff">The minimum score a choice needs to be returned (inclusive); 0 by default.</param>
        /// <returns>The best match (the earliest choice when several share the top score), or <c>null</c> when no choice reaches <paramref name="cutoff"/>.</returns>
        public static ExtractedResult<string> ExtractOne(IEnumerable<string> choices, Func<string, string> processor, ICachedRatioScorer scorer, double cutoff = 0)
        {
            return ExtractOneCore(choices, choice => scorer.Score(processor(choice)), cutoff);
        }

        /// <summary>
        /// Scores every choice against the query held by <paramref name="scorer"/> and returns those that reach <paramref name="cutoff"/>, highest score first.
        /// </summary>
        /// <typeparam name="T">The type of the choices.</typeparam>
        /// <param name="choices">The choices to search.</param>
        /// <param name="extractor">Gets the text to compare from each choice.</param>
        /// <param name="processor">Applied to every choice's text before it is passed to <paramref name="scorer"/>; must not be null. The query is not processed here: the cached scorer prepared it when it was created.</param>
        /// <param name="scorer">A cached scorer that already holds the query; it scores each processed choice. Must not be null.</param>
        /// <param name="cutoff">The minimum score a choice needs to be returned (inclusive); 0 by default.</param>
        /// <returns>The matching choices, highest score first; choices with equal scores keep their original order. Evaluated lazily, when the result is enumerated.</returns>
        public static IEnumerable<ExtractedResult<T>> ExtractSorted<T>(IEnumerable<T> choices, Func<T, string> extractor, Func<string, string> processor, ICachedRatioScorer scorer, double cutoff = 0)
        {
            return ExtractWithoutOrder(choices, extractor, processor, scorer, cutoff).OrderByDescending(r => r.Score);
        }

        /// <summary>
        /// Scores every choice against the query held by <paramref name="scorer"/> and returns those that reach <paramref name="cutoff"/>, highest score first.
        /// </summary>
        /// <param name="choices">The choices to search.</param>
        /// <param name="processor">Applied to every choice before it is passed to <paramref name="scorer"/>; must not be null. The query is not processed here: the cached scorer prepared it when it was created.</param>
        /// <param name="scorer">A cached scorer that already holds the query; it scores each processed choice. Must not be null.</param>
        /// <param name="cutoff">The minimum score a choice needs to be returned (inclusive); 0 by default.</param>
        /// <returns>The matching choices, highest score first; choices with equal scores keep their original order. Evaluated lazily, when the result is enumerated.</returns>
        public static IEnumerable<ExtractedResult<string>> ExtractSorted(IEnumerable<string> choices, Func<string, string> processor, ICachedRatioScorer scorer, double cutoff = 0)
        {
            return ExtractWithoutOrder(choices, processor, scorer, cutoff).OrderByDescending(r => r.Score);
        }

        /// <summary>
        /// Finds the <paramref name="limit"/> choices with the highest scores against the query held by <paramref name="scorer"/> that reach <paramref name="cutoff"/>.
        /// </summary>
        /// <typeparam name="T">The type of the choices.</typeparam>
        /// <param name="choices">The choices to search.</param>
        /// <param name="extractor">Gets the text to compare from each choice.</param>
        /// <param name="processor">Applied to every choice's text before it is passed to <paramref name="scorer"/>; must not be null. The query is not processed here: the cached scorer prepared it when it was created.</param>
        /// <param name="scorer">A cached scorer that already holds the query; it scores each processed choice. Must not be null.</param>
        /// <param name="limit">The maximum number of results; 0 returns none. Must not be negative.</param>
        /// <param name="cutoff">The minimum score a choice needs to be returned (inclusive); 0 by default.</param>
        /// <returns>At most <paramref name="limit"/> matches, highest score first. When scores are equal, which choices are kept and their order is not specified. Evaluated lazily, when the result is enumerated.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="limit"/> is negative.</exception>
        public static IEnumerable<ExtractedResult<T>> ExtractTop<T>(IEnumerable<T> choices, Func<T, string> extractor, Func<string, string> processor, ICachedRatioScorer scorer, int limit, double cutoff = 0)
        {
            ValidateLimit(limit);
            if (limit == 0)
                return Enumerable.Empty<ExtractedResult<T>>();

            return ExtractTopCore(choices, choice => scorer.Score(processor(extractor(choice))), limit, cutoff);
        }

        /// <summary>
        /// Finds the <paramref name="limit"/> choices with the highest scores against the query held by <paramref name="scorer"/> that reach <paramref name="cutoff"/>.
        /// </summary>
        /// <param name="choices">The choices to search.</param>
        /// <param name="processor">Applied to every choice before it is passed to <paramref name="scorer"/>; must not be null. The query is not processed here: the cached scorer prepared it when it was created.</param>
        /// <param name="scorer">A cached scorer that already holds the query; it scores each processed choice. Must not be null.</param>
        /// <param name="limit">The maximum number of results; 0 returns none. Must not be negative.</param>
        /// <param name="cutoff">The minimum score a choice needs to be returned (inclusive); 0 by default.</param>
        /// <returns>At most <paramref name="limit"/> matches, highest score first. When scores are equal, which choices are kept and their order is not specified. Evaluated lazily, when the result is enumerated.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="limit"/> is negative.</exception>
        public static IEnumerable<ExtractedResult<string>> ExtractTop(IEnumerable<string> choices, Func<string, string> processor, ICachedRatioScorer scorer, int limit, double cutoff = 0)
        {
            ValidateLimit(limit);
            if (limit == 0)
                return Enumerable.Empty<ExtractedResult<string>>();

            return ExtractTopCore(choices, choice => scorer.Score(processor(choice)), limit, cutoff);
        }
    }
}
