using System;
using System.Collections.Generic;

namespace Fuzzion.Tests.Support;

/// <summary>
/// Plain textbook implementations of every metric, written for clarity rather than speed.
/// The fast (trimmed, bit-parallel) implementations in Fuzzion are checked against these.
/// </summary>
public static class Naive
{
    public static int Levenshtein(string a, string b)
    {
        var d = new int[a.Length + 1, b.Length + 1];
        for (var i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (var j = 0; j <= b.Length; j++) d[0, j] = j;
        for (var i = 1; i <= a.Length; i++)
        for (var j = 1; j <= b.Length; j++)
        {
            var cost = a[i - 1] == b[j - 1] ? 0 : 1;
            d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
        }

        return d[a.Length, b.Length];
    }

    public static int LcsLength(string a, string b)
    {
        var d = new int[a.Length + 1, b.Length + 1];
        for (var i = 1; i <= a.Length; i++)
        for (var j = 1; j <= b.Length; j++)
        {
            d[i, j] = a[i - 1] == b[j - 1] ? d[i - 1, j - 1] + 1 : Math.Max(d[i - 1, j], d[i, j - 1]);
        }

        return d[a.Length, b.Length];
    }

    public static int Indel(string a, string b) => a.Length + b.Length - 2 * LcsLength(a, b);

    public static int Hamming(string a, string b)
    {
        var distance = Math.Max(a.Length, b.Length);
        for (var i = 0; i < Math.Min(a.Length, b.Length); i++)
        {
            if (a[i] == b[i]) distance--;
        }

        return distance;
    }

    public static int Prefix(string a, string b)
    {
        var n = 0;
        while (n < a.Length && n < b.Length && a[n] == b[n]) n++;
        return n;
    }

    public static int Postfix(string a, string b)
    {
        var n = 0;
        while (n < a.Length && n < b.Length && a[a.Length - 1 - n] == b[b.Length - 1 - n]) n++;
        return n;
    }

    /// <summary>Textbook Jaro similarity (window = max(len)/2 - 1, half-transpositions).</summary>
    public static double Jaro(string a, string b)
    {
        if (a.Length == 0 && b.Length == 0) return 1.0;
        if (a.Length == 0 || b.Length == 0) return 0.0;

        var window = Math.Max(0, Math.Max(a.Length, b.Length) / 2 - 1);
        var matchedA = new bool[a.Length];
        var matchedB = new bool[b.Length];
        var matches = 0;

        for (var i = 0; i < a.Length; i++)
        {
            var from = Math.Max(0, i - window);
            var to = Math.Min(b.Length - 1, i + window);
            for (var j = from; j <= to; j++)
            {
                if (matchedB[j] || a[i] != b[j]) continue;
                matchedA[i] = matchedB[j] = true;
                matches++;
                break;
            }
        }

        if (matches == 0) return 0.0;

        var halfTranspositions = 0;
        var k = 0;
        for (var i = 0; i < a.Length; i++)
        {
            if (!matchedA[i]) continue;
            while (!matchedB[k]) k++;
            if (a[i] != b[k]) halfTranspositions++;
            k++;
        }

        var m = (double)matches;
        return (m / a.Length + m / b.Length + (m - halfTranspositions / 2) / m) / 3.0;
    }

    public static double JaroWinkler(string a, string b, double prefixWeight = 0.1)
    {
        var jaro = Jaro(a, b);
        if (jaro <= 0.7) return jaro;
        var prefix = Math.Min(4, Prefix(a, b));
        return Math.Min(1.0, jaro + prefix * prefixWeight * (1 - jaro));
    }

    /// <summary>Optimal String Alignment: full table, adjacent swap allowed once.</summary>
    public static int Osa(string a, string b)
    {
        var d = new int[a.Length + 1, b.Length + 1];
        for (var i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (var j = 0; j <= b.Length; j++) d[0, j] = j;
        for (var i = 1; i <= a.Length; i++)
        for (var j = 1; j <= b.Length; j++)
        {
            var cost = a[i - 1] == b[j - 1] ? 0 : 1;
            d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            if (i > 1 && j > 1 && a[i - 1] == b[j - 2] && a[i - 2] == b[j - 1])
            {
                d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + 1);
            }
        }

        return d[a.Length, b.Length];
    }

    /// <summary>Unrestricted Damerau-Levenshtein: the Lowrance-Wagner full table.</summary>
    public static int DamerauLevenshtein(string a, string b)
    {
        var infinity = a.Length + b.Length;
        var lastRow = new Dictionary<char, int>();
        var d = new int[a.Length + 2, b.Length + 2];
        d[0, 0] = infinity;
        for (var i = 0; i <= a.Length; i++)
        {
            d[i + 1, 0] = infinity;
            d[i + 1, 1] = i;
        }

        for (var j = 0; j <= b.Length; j++)
        {
            d[0, j + 1] = infinity;
            d[1, j + 1] = j;
        }

        for (var i = 1; i <= a.Length; i++)
        {
            var lastMatchColumn = 0;
            for (var j = 1; j <= b.Length; j++)
            {
                var k = lastRow.TryGetValue(b[j - 1], out var row) ? row : 0;
                var l = lastMatchColumn;
                var cost = 1;
                if (a[i - 1] == b[j - 1])
                {
                    cost = 0;
                    lastMatchColumn = j;
                }

                d[i + 1, j + 1] = Math.Min(
                    Math.Min(d[i, j] + cost, d[i + 1, j] + 1),
                    Math.Min(d[i, j + 1] + 1, d[k, l] + (i - k - 1) + 1 + (j - l - 1)));
            }

            lastRow[a[i - 1]] = i;
        }

        return d[a.Length + 1, b.Length + 1];
    }
}
