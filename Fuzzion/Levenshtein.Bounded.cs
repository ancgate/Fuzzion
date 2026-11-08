using System;
using System.Buffers;

namespace Fuzzion;

// Levenshtein distance with a cutoff and a hint (plan item #30).
public sealed partial class Levenshtein
{
    /// <summary>
    /// Distance of already trimmed inputs, limited by <paramref name="scoreCutoff"/>.
    /// With unit costs, close strings are found with Ukkonen's banded search, which only
    /// looks at cells within k of the diagonal (O(n·k)); with a hint, k starts at the
    /// hint and doubles until the distance fits. Far strings fall back to the
    /// bit-parallel algorithm. Other costs compute the full distance and then apply the cutoff.
    /// </summary>
    private static int DistanceBounded<T>(
        ReadOnlySpan<T> source, ReadOnlySpan<T> target,
        int insertCost, int deleteCost, int replaceCost,
        int? scoreCutoff, int? scoreHint) where T : IEquatable<T>
    {
        if (scoreCutoff < 0) throw new ArgumentOutOfRangeException(nameof(scoreCutoff), scoreCutoff, "scoreCutoff must be 0 or more.");
        if (scoreHint < 0) throw new ArgumentOutOfRangeException(nameof(scoreHint), scoreHint, "scoreHint must be 0 or more.");

        var max = scoreCutoff ?? int.MaxValue;

        if (insertCost != 1 || deleteCost != 1 || replaceCost != 1)
        {
            var full = DistanceTrimmed(source, target, insertCost, deleteCost, replaceCost);
            return full <= max ? full : max + 1;
        }

        // At least |len1 - len2| insertions or deletions are needed.
        if (Math.Abs(source.Length - target.Length) > max)
        {
            return max + 1;
        }

        // The band pays off while it is narrow compared with the bit-parallel work
        // (about one 64-bit word per 64 characters of the shorter input).
        var bandLimit = Math.Max(1, 4 * ((Math.Min(source.Length, target.Length) + 63) / 64));

        if (scoreHint.HasValue && scoreHint.Value < max)
        {
            var k = Math.Max(scoreHint.Value, Math.Abs(source.Length - target.Length));
            while (k < max && k <= bandLimit)
            {
                var banded = BandedDistance(source, target, k);
                if (banded <= k)
                {
                    return banded;
                }

                k = k > max / 2 ? max : Math.Max(1, k * 2);
            }
        }

        if (max <= bandLimit)
        {
            return BandedDistance(source, target, max);
        }

        var distance = DistanceTrimmed(source, target, 1, 1, 1);
        return distance <= max ? distance : max + 1;
    }

    /// <summary>
    /// Ukkonen's banded Levenshtein distance with unit costs: exact when the distance is at
    /// most <paramref name="k"/>, otherwise <c>k + 1</c>.
    /// </summary>
    internal static int BandedDistance<T>(ReadOnlySpan<T> a, ReadOnlySpan<T> b, int k) where T : IEquatable<T>
    {
        int n = a.Length, m = b.Length;
        if (Math.Abs(n - m) > k) return k + 1;
        if (n == 0) return m;
        if (m == 0) return n;

        var infinity = k + 1;
        var rented = ArrayPool<int>.Shared.Rent(2 * (m + 1));
        try
        {
            var previous = rented.AsSpan(0, m + 1);
            var current = rented.AsSpan(m + 1, m + 1);
            for (var j = 0; j <= m; j++)
            {
                previous[j] = j <= k ? j : infinity;
            }

            for (var i = 1; i <= n; i++)
            {
                var low = Math.Max(1, i - k);
                var high = Math.Min(m, i + k);

                // The cell just left of the band.
                current[low - 1] = low == 1 ? (i <= k ? i : infinity) : infinity;
                var rowMinimum = current[low - 1];

                for (var j = low; j <= high; j++)
                {
                    var value = previous[j - 1] + (a[i - 1].Equals(b[j - 1]) ? 0 : 1);
                    value = Math.Min(value, previous[j] + 1);
                    value = Math.Min(value, current[j - 1] + 1);
                    current[j] = Math.Min(value, infinity);
                    rowMinimum = Math.Min(rowMinimum, current[j]);
                }

                // The next row reads one cell further right; it lies outside this band.
                if (high < m)
                {
                    current[high + 1] = infinity;
                }

                if (rowMinimum > k)
                {
                    return k + 1;
                }

                var swap = previous;
                previous = current;
                current = swap;
            }

            return previous[m] <= k ? previous[m] : k + 1;
        }
        finally
        {
            ArrayPool<int>.Shared.Return(rented);
        }
    }
}
