using System;
using System.Collections.Generic;
#if NETCOREAPP3_0_OR_GREATER
using System.Numerics;
#endif

namespace Fuzzion.Utils;

/// <summary>
/// SIMD batch scoring for <see cref="Fuzz.Ratio(string, string)"/> (plan item #33): one
/// query against many short choices at once. Choices of 1 to 64 characters are packed
/// into the lanes of a hardware vector (8-bit lanes for choices of up to 8 characters,
/// 16-bit for up to 16, 32-bit for up to 32, 64-bit for up to 64), and every query
/// character runs the bit-parallel LCS step <c>S = (S + u) | (S - u)</c> on all lanes
/// together. The per-choice bit tables are built once and reused for every query.
/// Results are bit-for-bit equal to the scalar path. Choices that are empty or longer
/// than 64 characters are left to the scalar path.
/// </summary>
internal sealed class BatchRatio
{
    /// <summary>Tests set this to force the scalar path.</summary>
    internal static bool Disabled;

    private readonly IBatch[] _batches;

    /// <summary>What a batch computes for each choice.</summary>
    internal enum Metric
    {
        /// <summary><see cref="Fuzz.Ratio(string, string)"/> (bit-parallel LCS / Indel).</summary>
        Ratio,

        /// <summary><see cref="Levenshtein.Distance(string, string, int, int, int, int?, int?)"/> with unit costs (Hyyrö 2003).</summary>
        Levenshtein,
    }

    private BatchRatio(IBatch[] batches, bool[] handled)
    {
        _batches = batches;
        Handled = handled;
    }

    /// <summary>Which choices this batch scores; the caller scores the others.</summary>
    public bool[] Handled { get; }

    /// <summary>Prepares the choices, or returns null when SIMD is unavailable or disabled.</summary>
    public static BatchRatio TryCreate(IReadOnlyList<string> choices) => TryCreate(choices, Metric.Ratio);

    /// <summary>Prepares the choices for <paramref name="metric"/>, or returns null when SIMD is unavailable or disabled.</summary>
    public static BatchRatio TryCreate(IReadOnlyList<string> choices, Metric metric)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Disabled || !Vector.IsHardwareAccelerated || choices.Count == 0)
        {
            return null;
        }

        var byWidth = new List<int>[4];
        for (var i = 0; i < byWidth.Length; i++) byWidth[i] = new List<int>();

        for (var i = 0; i < choices.Count; i++)
        {
            var length = choices[i].Length;
            if (length is >= 1 and <= 8) byWidth[0].Add(i);
            else if (length is > 8 and <= 16) byWidth[1].Add(i);
            else if (length is > 16 and <= 32) byWidth[2].Add(i);
            else if (length is > 32 and <= 64) byWidth[3].Add(i);
        }

        var batches = new List<IBatch>();
        var handled = new bool[choices.Count];
        AddBatches<byte>(metric, choices, byWidth[0], batches, handled);
        AddBatches<ushort>(metric, choices, byWidth[1], batches, handled);
        AddBatches<uint>(metric, choices, byWidth[2], batches, handled);
        AddBatches<ulong>(metric, choices, byWidth[3], batches, handled);

        return batches.Count == 0 ? null : new BatchRatio(batches.ToArray(), handled);
#else
        return null;
#endif
    }

    /// <summary>Writes <c>Ratio(query, choices[j])</c> into <paramref name="row"/> for every handled choice.</summary>
    public void ScoreRow(string query, Span<double> row)
    {
        foreach (var batch in _batches)
        {
            batch.Score(query, row);
        }
    }

    private interface IBatch
    {
        void Score(string query, Span<double> row);
    }

#if NETCOREAPP3_0_OR_GREATER
    private static void AddBatches<T>(Metric metric, IReadOnlyList<string> choices, List<int> indices, List<IBatch> batches, bool[] handled)
        where T : unmanaged
    {
        var lanes = Vector<T>.Count;
        for (var start = 0; start < indices.Count; start += lanes)
        {
            var count = Math.Min(lanes, indices.Count - start);
            batches.Add(new Batch<T>(metric, choices, indices.GetRange(start, count)));
            for (var k = 0; k < count; k++) handled[indices[start + k]] = true;
        }
    }

    private sealed class Batch<T> : IBatch where T : unmanaged
    {
        private static readonly int Lanes = Vector<T>.Count;

        private readonly Metric _metric;
        private readonly int[] _columns;
        private readonly int[] _lengths;
        private readonly Vector<T> _initial;
        private readonly T[] _latin1;               // 256 rows of Lanes masks
        private readonly Dictionary<char, Vector<T>> _other;

        public Batch(Metric metric, IReadOnlyList<string> choices, List<int> columns)
        {
            _metric = metric;
            _columns = columns.ToArray();
            _lengths = new int[_columns.Length];
            _latin1 = new T[256 * Lanes];
            _other = new Dictionary<char, Vector<T>>();

            var otherMasks = new Dictionary<char, T[]>();
            var initial = new T[Lanes];
            for (var lane = 0; lane < _columns.Length; lane++)
            {
                var choice = choices[_columns[lane]];
                _lengths[lane] = choice.Length;
                initial[lane] = FromUInt64(LowBits(choice.Length));

                for (var position = 0; position < choice.Length; position++)
                {
                    var c = choice[position];
                    var bit = 1UL << position;
                    if (c < 256)
                    {
                        var index = c * Lanes + lane;
                        _latin1[index] = FromUInt64(ToUInt64(_latin1[index]) | bit);
                    }
                    else
                    {
                        if (!otherMasks.TryGetValue(c, out var masks))
                        {
                            otherMasks[c] = masks = new T[Lanes];
                        }

                        masks[lane] = FromUInt64(ToUInt64(masks[lane]) | bit);
                    }
                }
            }

            _initial = new Vector<T>(initial);
            foreach (var pair in otherMasks)
            {
                _other[pair.Key] = new Vector<T>(pair.Value);
            }
        }

        public void Score(string query, Span<double> row)
        {
            if (_metric == Metric.Levenshtein)
            {
                ScoreLevenshtein(query, row);
                return;
            }

            var s = _initial;
            foreach (var c in query)
            {
                Vector<T> m;
                if (c < 256)
                {
                    m = new Vector<T>(_latin1, c * Lanes);
                }
                else if (!_other.TryGetValue(c, out m))
                {
                    continue; // no lane contains c: u = 0 leaves S unchanged
                }

                var u = s & m;
                s = (s + u) | (s - u);
            }

            for (var lane = 0; lane < _columns.Length; lane++)
            {
                var length = _lengths[lane];
                var lcs = Polyfill.PopCount(~ToUInt64(s[lane]) & LowBits(length));

                // Same arithmetic as DefaultRatioStrategy / Indel.NormalizedSimilarityChar.
                var maximum = query.Length + length;
                var distance = maximum - 2 * lcs;
                var normalizedDistance = distance / (double)maximum;
                row[_columns[lane]] = 100 * (1 - normalizedDistance);
            }
        }

        /// <summary>
        /// Hyyrö's bit-parallel Levenshtein on every lane, the same steps as
        /// <c>Levenshtein.DistanceSingleULong</c>. A left shift by one is <c>v + v</c>. Each
        /// lane's distance change is kept in the lane (at most ±64 between flushes, so it
        /// fits even in 8 bits) and added to an integer total every 64 characters.
        /// </summary>
        private void ScoreLevenshtein(string query, Span<double> row)
        {
            Span<int> distance = stackalloc int[Lanes];
            for (var lane = 0; lane < _columns.Length; lane++) distance[lane] = _lengths[lane];

            var lastBits = new T[Lanes];
            for (var lane = 0; lane < _columns.Length; lane++) lastBits[lane] = FromUInt64(1UL << (_lengths[lane] - 1));
            var last = new Vector<T>(lastBits);
            var one = new Vector<T>(FromUInt64(1));

            var vp = _initial;
            var vn = Vector<T>.Zero;
            var delta = Vector<T>.Zero;
            var pending = 0;

            foreach (var c in query)
            {
                Vector<T> pm;
                if (c < 256)
                {
                    pm = new Vector<T>(_latin1, c * Lanes);
                }
                else if (!_other.TryGetValue(c, out pm))
                {
                    pm = Vector<T>.Zero;
                }

                var x = pm | vn;
                var d0 = (((x & vp) + vp) ^ vp) | x | vn;
                var hp = vn | ~(d0 | vp);
                var hn = d0 & vp;

                // Vector.Equals gives all ones (-1) per true lane: subtracting adds 1.
                delta = delta - Vector.Equals(hp & last, last) + Vector.Equals(hn & last, last);

                hp = (hp + hp) | one;
                hn += hn;
                vp = hn | ~(d0 | hp);
                vn = hp & d0;

                if (++pending == 64)
                {
                    Flush(delta, distance);
                    delta = Vector<T>.Zero;
                    pending = 0;
                }
            }

            Flush(delta, distance);
            for (var lane = 0; lane < _columns.Length; lane++)
            {
                row[_columns[lane]] = distance[lane];
            }
        }

        private void Flush(Vector<T> delta, Span<int> distance)
        {
            for (var lane = 0; lane < _columns.Length; lane++)
            {
                distance[lane] += ToSigned(delta[lane]);
            }
        }

        /// <summary>Reads a lane value as a signed number (the deltas wrap around).</summary>
        private static int ToSigned(T value) =>
            typeof(T) == typeof(byte) ? (sbyte)(byte)(object)value :
            typeof(T) == typeof(ushort) ? (short)(ushort)(object)value :
            typeof(T) == typeof(uint) ? (int)(uint)(object)value :
            (int)(long)(ulong)(object)value;

        private static ulong LowBits(int n) => n >= 64 ? ulong.MaxValue : (1UL << n) - 1;

        // typeof(T) checks are resolved by the JIT, so these do not box.
        private static ulong ToUInt64(T value) =>
            typeof(T) == typeof(byte) ? (byte)(object)value :
            typeof(T) == typeof(ushort) ? (ushort)(object)value :
            typeof(T) == typeof(uint) ? (uint)(object)value :
            (ulong)(object)value;

        private static T FromUInt64(ulong value) =>
            typeof(T) == typeof(byte) ? (T)(object)(byte)value :
            typeof(T) == typeof(ushort) ? (T)(object)(ushort)value :
            typeof(T) == typeof(uint) ? (T)(object)(uint)value :
            (T)(object)value;
    }
#endif
}
