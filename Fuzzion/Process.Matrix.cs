using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fuzzion.PreProcess;
using Fuzzion.SimilarityRatio;
using Fuzzion.SimilarityRatio.Scorer;
using Fuzzion.SimilarityRatio.Scorer.Composite;
using Fuzzion.SimilarityRatio.Scorer.StrategySensitive;
using Fuzzion.Utils;

namespace Fuzzion;

// Many-to-many scoring and duplicate removal (plan items #24, #25, #27).
public static partial class Process
{
    /// <summary>
    /// Scores every query against every choice and returns the scores as a matrix:
    /// <c>result[i, j] = scorer(queries[i], choices[j])</c>. Same as RapidFuzz's
    /// <c>process.cdist</c>.
    /// </summary>
    /// <param name="queries">The rows.</param>
    /// <param name="choices">The columns.</param>
    /// <param name="scorer">
    /// The scorer; <see cref="DefaultRatioScorer"/> (plain ratio, as RapidFuzz's cdist) when null.
    /// Built-in scorers are prepared once per query. For a distance scorer
    /// (<see cref="Scorers.FromDistance"/>), distances above its maximum are stored as maximum + 1.
    /// </param>
    /// <param name="processor">Applied once to every query and choice; none by default (as RapidFuzz).</param>
    /// <param name="scoreCutoff">Similarity scores below this are stored as 0. Must be 0 for distance scorers.</param>
    /// <param name="workers">1 = single thread (default), -1 = all cores, n = at most n threads.</param>
    /// <param name="cancellationToken">Stops the work between rows.</param>
    /// <remarks>The matrix takes <c>queries × choices × 8</c> bytes. Custom scorers must be thread-safe when <paramref name="workers"/> is not 1.</remarks>
    public static double[,] Cdist(
        IReadOnlyList<string> queries,
        IReadOnlyList<string> choices,
        IRatioScorer scorer = null,
        Func<string, string> processor = null,
        double scoreCutoff = 0,
        int workers = 1,
        CancellationToken cancellationToken = default)
    {
        if (queries == null) throw new ArgumentNullException(nameof(queries));
        if (choices == null) throw new ArgumentNullException(nameof(choices));

        var scoring = MatrixScoring.Create(scorer, scoreCutoff);
        var processedQueries = MatrixScoring.Process(queries, processor);
        var processedChoices = MatrixScoring.Process(choices, processor);
        var result = new double[processedQueries.Length, processedChoices.Length];

        // Ratio, QRatio and Scorers.Levenshtein score short choices many at a time with SIMD (plan item #33).
        var isQRatio = scoring.Scorer is QRatioScorer;
        var batch = scoring.Scorer switch
        {
            DefaultRatioScorer or QRatioScorer => BatchRatio.TryCreate(processedChoices),
            Scorers.LevenshteinScorer => BatchRatio.TryCreate(processedChoices, BatchRatio.Metric.Levenshtein),
            _ => null,
        };

        MatrixScoring.ForEachRow(processedQueries.Length, workers, cancellationToken, row =>
        {
            var query = processedQueries[row];
            double[] batchRow = null;
            if (batch != null)
            {
                batchRow = ArrayPool<double>.Shared.Rent(processedChoices.Length);
                batch.ScoreRow(query, batchRow);
            }

            try
            {
                using var cached = MatrixScoring.TryCreateCached(scoring.Scorer, query);
                for (var column = 0; column < processedChoices.Length; column++)
                {
                    double raw;
                    if (batchRow != null && batch.Handled[column])
                    {
                        // Batched choices are never empty; QRatio is 0 for an empty query.
                        raw = isQRatio && query.Length == 0 ? 0 : batchRow[column];
                    }
                    else
                    {
                        raw = cached != null ? cached.Score(processedChoices[column]) : scoring.Scorer.Score(query, processedChoices[column]);
                    }

                    result[row, column] = scoring.Apply(raw);
                }
            }
            finally
            {
                if (batchRow != null) ArrayPool<double>.Shared.Return(batchRow);
            }
        });

        return result;
    }

    /// <summary>
    /// Scores each pair <c>(first[i], second[i])</c>. Same as RapidFuzz's <c>process.cpdist</c>.
    /// Parameters work as in <see cref="Cdist"/>.
    /// </summary>
    /// <exception cref="ArgumentException">The lists have different lengths.</exception>
    public static double[] Cpdist(
        IReadOnlyList<string> first,
        IReadOnlyList<string> second,
        IRatioScorer scorer = null,
        Func<string, string> processor = null,
        double scoreCutoff = 0,
        int workers = 1,
        CancellationToken cancellationToken = default)
    {
        if (first == null) throw new ArgumentNullException(nameof(first));
        if (second == null) throw new ArgumentNullException(nameof(second));
        if (first.Count != second.Count)
        {
            throw new ArgumentException($"Both lists must have the same length ({first.Count} vs {second.Count}).", nameof(second));
        }

        var scoring = MatrixScoring.Create(scorer, scoreCutoff);
        var a = MatrixScoring.Process(first, processor);
        var b = MatrixScoring.Process(second, processor);
        var result = new double[a.Length];

        MatrixScoring.ForEachRow(a.Length, workers, cancellationToken, i => result[i] = scoring.Apply(scoring.Scorer.Score(a[i], b[i])));
        return result;
    }

    /// <summary>
    /// Removes near-duplicates, as TheFuzz's <c>process.dedupe</c>: for each item, find every
    /// item scoring at least <paramref name="threshold"/> against it and keep the longest of
    /// them (ties: the one that sorts last, ordinally). Returns the kept items in order of
    /// first appearance, or the original list when nothing was removed.
    /// </summary>
    /// <param name="items">The strings to deduplicate.</param>
    /// <param name="threshold">Minimum score (0 to 100) for two items to count as duplicates.</param>
    /// <param name="scorer">The scorer; <see cref="TokenSetScorer"/> when null (as TheFuzz).</param>
    /// <param name="processor">Applied before scoring; <see cref="StringPreprocessor.Full"/> when null (as TheFuzz).</param>
    public static IReadOnlyList<string> Dedupe(
        IEnumerable<string> items,
        double threshold = 70,
        IRatioScorer scorer = null,
        Func<string, string> processor = null)
    {
        if (items == null) throw new ArgumentNullException(nameof(items));
        if (scorer is IDistanceScorer)
        {
            throw new ArgumentException("Dedupe needs a similarity scorer (higher is better).", nameof(scorer));
        }

        scorer ??= ScorerCache.Get<TokenSetScorer>();
        processor ??= StringPreprocessor.Full;

        var list = new List<string>(items);
        var processed = new string[list.Count];
        for (var i = 0; i < list.Count; i++)
        {
            processed[i] = processor(list[i] ?? throw new ArgumentException("items must not contain null.", nameof(items)));
        }

        var kept = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < list.Count; i++)
        {
            using var cached = MatrixScoring.TryCreateCached(scorer, processed[i]);
            string best = null;
            for (var j = 0; j < list.Count; j++)
            {
                var score = cached != null ? cached.Score(processed[j]) : scorer.Score(processed[i], processed[j]);
                if (score >= threshold && (best == null || IsLongerOrLater(list[j], best)))
                {
                    best = list[j];
                }
            }

            // An item that matches nothing (not even itself, e.g. it is empty after
            // processing) is kept as is. TheFuzz raises an error in that case.
            best ??= list[i];
            if (seen.Add(best))
            {
                kept.Add(best);
            }
        }

        return kept.Count != list.Count ? kept : list;
    }

    private static bool IsLongerOrLater(string candidate, string best) =>
        candidate.Length != best.Length
            ? candidate.Length > best.Length
            : string.CompareOrdinal(candidate, best) > 0;
}

/// <summary>Shared pieces of <see cref="Process.Cdist"/>, <see cref="Process.Cpdist"/> and <see cref="Process.Dedupe"/>.</summary>
internal readonly struct MatrixScoring
{
    private readonly bool _isDistance;
    private readonly double _limit;

    private MatrixScoring(IRatioScorer scorer, bool isDistance, double limit)
    {
        Scorer = scorer;
        _isDistance = isDistance;
        _limit = limit;
    }

    public IRatioScorer Scorer { get; }

    public static MatrixScoring Create(IRatioScorer scorer, double scoreCutoff)
    {
        if (double.IsNaN(scoreCutoff)) throw new ArgumentOutOfRangeException(nameof(scoreCutoff), "scoreCutoff must be a number.");

        scorer ??= ScorerCache.Get<DefaultRatioScorer>();
        if (scorer is IDistanceScorer distance)
        {
            if (scoreCutoff != 0)
            {
                throw new ArgumentException(
                    "scoreCutoff is a minimum similarity and does not apply to distance scorers; set maxDistance on the scorer.",
                    nameof(scoreCutoff));
            }

            return new MatrixScoring(scorer, isDistance: true, distance.MaxDistance ?? double.PositiveInfinity);
        }

        return new MatrixScoring(scorer, isDistance: false, scoreCutoff);
    }

    /// <summary>Similarity below the cutoff → 0; distance above the maximum → maximum + 1 (RapidFuzz's convention).</summary>
    public double Apply(double raw) =>
        _isDistance
            ? raw <= _limit ? raw : _limit + 1
            : raw >= _limit ? raw : 0;

    public static string[] Process(IReadOnlyList<string> values, Func<string, string> processor)
    {
        var result = new string[values.Count];
        for (var i = 0; i < result.Length; i++)
        {
            var value = values[i] ?? throw new ArgumentException("The lists must not contain null.");
            result[i] = processor == null ? value : processor(value);
        }

        return result;
    }

    public static void ForEachRow(int rows, int workers, CancellationToken cancellationToken, Action<int> body)
    {
        if (workers == 0 || workers < -1)
        {
            throw new ArgumentOutOfRangeException(nameof(workers), workers, "workers must be 1 or more, or -1 for all cores.");
        }

        if (workers == 1)
        {
            for (var row = 0; row < rows; row++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                body(row);
            }

            return;
        }

        var options = new ParallelOptions { MaxDegreeOfParallelism = workers, CancellationToken = cancellationToken };
        Parallel.For(0, rows, options, body);
    }

    /// <summary>The cached version of a built-in scorer, prepared for <paramref name="query"/>; null for other scorers.</summary>
    public static ICachedRatioScorer TryCreateCached(IRatioScorer scorer, string query) => scorer switch
    {
        DefaultRatioScorer => new CachedDefaultRatioScorer(query),
        QRatioScorer => new CachedQRatioScorer(query),
        PartialRatioScorer => new CachedPartialRatioScorer(query),
        WeightedRatioScorer => new CachedWeightedRatioScorer(query),
        TokenSortScorer => new CachedTokenSortScorer(query),
        PartialTokenSortScorer => new CachedPartialTokenSortScorer(query),
        TokenSetScorer => new CachedTokenSetScorer(query),
        PartialTokenSetScorer => new CachedPartialTokenSetScorer(query),
        TokenRatioScorer => new CachedTokenRatioScorer(query),
        PartialTokenRatioScorer => new CachedPartialTokenRatioScorer(query),
        _ => null,
    };
}
