using System;
using System.Collections.Generic;
using Fuzzion.Utils;

namespace Fuzzion.Extensions;

/// <summary>
/// Top-N helpers for sequences, kept from Raffinert.FuzzySharp.
/// </summary>
public static class EnumerableExtensions
{
    /// <summary>
    /// Returns the <paramref name="n"/> largest elements of <paramref name="source"/>, found with a
    /// <see cref="MinHeap{T}"/> of size <paramref name="n"/>. They come out in ascending order
    /// (the smallest of them first). An element replaces a kept one only when it is strictly
    /// larger, so among equal elements the earlier ones are kept. Evaluated lazily.
    /// </summary>
    /// <typeparam name="T">Element type.</typeparam>
    /// <param name="source">The elements.</param>
    /// <param name="n">
    /// How many elements to return. Must be at least 1: with 0 or less, enumerating a non-empty
    /// <paramref name="source"/> throws <see cref="InvalidOperationException"/>.
    /// </param>
    /// <returns>At most <paramref name="n"/> elements, smallest first.</returns>
    public static IEnumerable<T> MaxN<T>(this IEnumerable<T> source, int n) where T : IComparable<T>
    {
        var comparer = Comparer<T>.Default;
        var queue = new MinHeap<T>(comparer);
        
        foreach (var item in source)
        {
            if (queue.Count < n)
            {
                queue.Add(item);
            }
            else if (comparer.Compare(item, queue.GetMin()) > 0)
            {
                queue.ExtractDominating();
                queue.Add(item);
            }
        }

        for (int i = 0; i < n && queue.Count > 0; i++)
        {
            yield return queue.ExtractDominating();
        }
    }

    /// <summary>
    /// Returns the <paramref name="n"/> elements of <paramref name="source"/> with the largest
    /// <paramref name="selector"/> values, found with a <see cref="MinHeap{T}"/> of size
    /// <paramref name="n"/>. They come out in ascending order of value (the smallest of them
    /// first). An element replaces a kept one only when its value is strictly larger, so among
    /// equal values the earlier elements are kept. Evaluated lazily.
    /// </summary>
    /// <typeparam name="T">Element type.</typeparam>
    /// <typeparam name="TVal">Type of the value elements are ranked by.</typeparam>
    /// <param name="source">The elements.</param>
    /// <param name="n">
    /// How many elements to return. Must be at least 1: with 0 or less, enumerating a non-empty
    /// <paramref name="source"/> throws <see cref="InvalidOperationException"/>.
    /// </param>
    /// <param name="selector">
    /// Gives the value of an element. It is called on every comparison, so keep it cheap.
    /// </param>
    /// <returns>At most <paramref name="n"/> elements, smallest value first.</returns>
    public static IEnumerable<T> MaxNBy<T, TVal>(this IEnumerable<T> source, int n, Func<T, TVal> selector) where TVal : IComparable<TVal>
    {
        var valComparer = Comparer<TVal>.Default;
        var queue = new MinHeap<T>(Comparer<T>.Create((x, y) => valComparer.Compare(selector(x), selector(y))));
        
        foreach (var item in source)
        {
            if (queue.Count < n)
            {
                queue.Add(item);
            }
            else if (valComparer.Compare(selector(item), selector(queue.GetMin())) > 0)
            {
                queue.ExtractDominating();
                queue.Add(item);
            }
        }

        for (int i = 0; i < n && queue.Count > 0; i++)
        {
            yield return queue.ExtractDominating();
        }
    }
}