using System;
using System.Collections.Generic;
using System.Linq;

namespace Fuzzion.Utils;

/// <summary>
/// Steps through the permutations of a list in lexicographic order. A low-level helper kept
/// public for compatibility with Raffinert.FuzzySharp.
/// </summary>
/// <typeparam name="T">Element type.</typeparam>
public class Permutor<T> where T : IComparable<T>
{
    private readonly List<T> _set;

    /// <summary>Creates a permutor over a copy of <paramref name="set"/>, in the given order.</summary>
    public Permutor(IEnumerable<T> set)
    {
        _set = set.ToList();
    }

    /// <summary>
    /// Returns, as a new list, the <paramref name="i"/>-th permutation in lexicographic order,
    /// counting from 1 (1 or less gives the sorted list). It steps from the sorted list
    /// <c>i - 1</c> times, so it takes time proportional to <paramref name="i"/>; past the last
    /// permutation the result stays at the last one.
    /// </summary>
    public List<T> PermutationAt(long i)
    {
        var set = new List<T>(_set.OrderBy(e => e));
        for (long j = 0; j < i - 1; j++)
        {
            NextPermutation(set);
        }
        return set;
    }

    /// <summary>
    /// Advances the permutor's own list to the next permutation in lexicographic order and
    /// returns it (the same list instance every time). The list starts in the order given to the
    /// constructor; when it is already the last permutation, it is returned unchanged.
    /// </summary>
    public List<T> NextPermutation()
    {
        NextPermutation(_set);
        return _set;
    }

    /// <summary>
    /// Rearranges <paramref name="set"/> in place into the next permutation in lexicographic
    /// order. Does not use or change the permutor's own list.
    /// </summary>
    /// <returns>
    /// <c>true</c> if <paramref name="set"/> was changed; <c>false</c> if it already was the last
    /// permutation (in descending order), in which case it is left unchanged.
    /// </returns>
    public bool NextPermutation(List<T> set)
    {
        // Find non-increasing suffix
        int i = set.Count - 1;
        while (i > 0 && set[i - 1].CompareTo(set[i]) >= 0)
            i--;
        if (i <= 0)
            return false;

        // Find successor to pivot
        int j = set.Count - 1;
        while (set[j].CompareTo(set[i - 1]) <= 0)
            j--;
        T temp = set[i - 1];
        set[i - 1] = set[j];
        set[j] = temp;

        // Reverse suffix
        j = set.Count - 1;
        while (i < j)
        {
            temp = set[i];
            set[i] = set[j];
            set[j] = temp;
            i++;
            j--;
        }
        return true;
    }
}

/// <summary>
/// Permutation helpers. Low-level helpers kept public for compatibility with
/// Raffinert.FuzzySharp; the token abbreviation scorers use <see cref="PermutationsOfSize{T}(List{T}, int)"/>.
/// </summary>
public static class Permutation
{
    private static IEnumerable<List<T>> AllPermutations<T>(this IEnumerable<T> seed)
    {
        var set = new List<T>(seed);
        return Permute(set, 0, set.Count - 1);
    }

    /// <summary>
    /// Returns every ordered arrangement of <paramref name="size"/> elements of
    /// <paramref name="seed"/> (k-permutations), each as a new list. Elements are picked by
    /// position, so duplicates in <paramref name="seed"/> give repeated lists. The result is
    /// empty when <paramref name="size"/> is 0 or less or larger than <c>seed.Count</c>.
    /// </summary>
    public static IEnumerable<List<T>> PermutationsOfSize<T>(this List<T> seed, int size)
    {
        var result = seed.Count < size 
            ? [] 
            : seed.PermutationsOfSize([], size);

        return result;
    }

    private static IEnumerable<List<T>> PermutationsOfSize<T>(this List<T> seed, List<T> set, int size)
    {
        if (size == 0)
        {
            foreach (var permutation in set.AllPermutations())
            {
                yield return permutation;
            }

            yield break;
        }

        for (int i = 0; i < seed.Count; i++)
        {
            var newSet = new List<T>(set) { seed[i] };
            foreach (var permutation in seed.Skip(i + 1).ToList().PermutationsOfSize(newSet, size - 1))
            {
                yield return permutation;
            }
        }
    }

    private static IEnumerable<List<T>> Permute<T>(List<T> set, int start, int end)
    {
        if (start == end)
        {
            yield return [..set];
        }
        else
        {
            for (int i = start; i <= end; i++)
            {
                Swap(set, start, i);
                foreach (var v in Permute(set, start + 1, end))
                {
                    yield return v;
                }
                Swap(set, start, i);
            }
        }
    }

    private static void Swap<T>(List<T> set, int a, int b)
    {
        (set[a], set[b]) = (set[b], set[a]);
    }

    /// <summary>
    /// Returns every rotation of <paramref name="seed"/> as a new list: first <paramref name="seed"/>
    /// itself, then each time rotated left by one element.
    /// </summary>
    public static IEnumerable<List<T>> Cycles<T>(IEnumerable<T> seed)
    {
        var set = new LinkedList<T>(seed);
        for (int i = 0; i < set.Count; i++)
        {
            yield return [..set];
            var top = set.First!;
            set.RemoveFirst();
            set.AddLast(top);
        }
    }

    /// <summary>
    /// Whether <paramref name="set"/> and <paramref name="other"/> contain the same distinct
    /// elements. Despite the name this is a set comparison: order and duplicates are ignored, so
    /// <c>[a, a, b]</c> counts as a permutation of <c>[b, a]</c>.
    /// </summary>
    public static bool IsPermutationOf<T>(this IEnumerable<T> set, IEnumerable<T> other)
    {
        var hashedSet = new HashSet<T>(set);
        return hashedSet.SetEquals(other);
    }
}