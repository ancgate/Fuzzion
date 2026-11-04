using System;
using System.Collections.Generic;
using Fuzzion.Utils;

namespace Fuzzion.Extensions;

public static class EnumerableExtensions
{
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