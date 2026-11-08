using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Fuzzion.Utils;

/// <summary>
/// A binary heap: the element that dominates all others (as decided by <see cref="Dominates"/>)
/// is at the top. A low-level helper kept public for compatibility with Raffinert.FuzzySharp;
/// the library uses <see cref="MinHeap{T}"/>.
/// </summary>
/// <typeparam name="T">Element type.</typeparam>
public abstract class Heap<T> : IEnumerable<T>
{
    private const int InitialCapacity = 0;
    private const int GrowFactor      = 2;
    private const int MinGrow         = 1;

    private T[] _heap     = new T[InitialCapacity];

    /// <summary>Number of elements in the heap.</summary>
    public int Count { get; private set; }

    /// <summary>Size of the internal array; it grows as elements are added.</summary>
    public int Capacity { get; private set; } = InitialCapacity;

    /// <summary>The comparer that orders the elements.</summary>
    protected          Comparer<T> Comparer { get; }

    /// <summary>
    /// Whether <paramref name="x"/> may be above <paramref name="y"/> in the heap (be its parent).
    /// It must also return <c>true</c> for equal elements.
    /// </summary>
    protected abstract bool        Dominates(T x, T y);

    /// <summary>Creates an empty heap that uses <see cref="Comparer{T}.Default"/>.</summary>
    protected Heap() : this(Comparer<T>.Default)
    {
    }

    /// <summary>Creates an empty heap that uses <paramref name="comparer"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is <c>null</c>.</exception>
    protected Heap(Comparer<T> comparer) : this([], comparer)
    {
    }

    /// <summary>Creates a heap of the elements of <paramref name="collection"/>, using <see cref="Comparer{T}.Default"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <c>null</c>.</exception>
    protected Heap(IEnumerable<T> collection)
        : this(collection, Comparer<T>.Default)
    {
    }

    /// <summary>Creates a heap of the elements of <paramref name="collection"/>, using <paramref name="comparer"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> or <paramref name="comparer"/> is <c>null</c>.</exception>
    protected Heap(IEnumerable<T> collection, Comparer<T> comparer)
    {
        Comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
        _ = collection ?? throw new ArgumentNullException(nameof(collection));

        foreach (var item in collection)
        {
            if (Count == Capacity)
                Grow();

            _heap[Count++] = item;
        }

        for (int i = Parent(Count - 1); i >= 0; i--)
            BubbleDown(i);
    }

    /// <summary>Adds <paramref name="item"/> to the heap.</summary>
    public void Add(T item)
    {
        if (Count == Capacity)
            Grow();

        _heap[Count++] = item;
        BubbleUp(Count - 1);
    }

    private void BubbleUp(int i)
    {
        while (true)
        {
            if (i == 0 || Dominates(_heap[Parent(i)], _heap[i])) return; //correct domination (or root)

            Swap(i, Parent(i));
            i = Parent(i);
        }
    }

    /// <summary>
    /// Returns the top element without removing it: the dominating one, which is the smallest
    /// in a <see cref="MinHeap{T}"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The heap is empty.</exception>
    public T GetMin()
    {
        if (Count == 0) throw new InvalidOperationException("Heap is empty");
        return _heap[0];
    }

    /// <summary>
    /// Removes and returns the top element: the dominating one, which is the smallest in a
    /// <see cref="MinHeap{T}"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The heap is empty.</exception>
    public T ExtractDominating()
    {
        if (Count == 0) throw new InvalidOperationException("Heap is empty");
        T ret = _heap[0];
        Count--;
        Swap(Count, 0);
        BubbleDown(0);
        return ret;
    }

    private void BubbleDown(int i)
    {
        while (true)
        {
            var dominatingNode = Dominating(i);
            if (dominatingNode == i) return;
            Swap(i, dominatingNode);
            i = dominatingNode;
        }
    }

    private int Dominating(int i)
    {
        int dominatingNode = i;
        dominatingNode = GetDominating(YoungChild(i), dominatingNode);
        dominatingNode = GetDominating(OldChild(i),   dominatingNode);

        return dominatingNode;
    }

    private int GetDominating(int newNode, int dominatingNode)
    {
        if (newNode < Count && !Dominates(_heap[dominatingNode], _heap[newNode]))
            return newNode;

        return dominatingNode;
    }

    private void Swap(int i, int j)
    {
        (_heap[i], _heap[j]) = (_heap[j], _heap[i]);
    }

    private static int Parent(int i)
    {
        return (i + 1) / 2 - 1;
    }

    private static int YoungChild(int i)
    {
        return (i + 1) * 2 - 1;
    }

    private static int OldChild(int i)
    {
        return YoungChild(i) + 1;
    }

    private void Grow()
    {
        int newCapacity = Capacity * GrowFactor + MinGrow;
        var newHeap     = new T[newCapacity];
        Array.Copy(_heap, newHeap, Capacity);
        _heap     = newHeap;
        Capacity = newCapacity;
    }

    /// <summary>Enumerates the elements in the heap's internal order, which is not sorted.</summary>
    public IEnumerator<T> GetEnumerator()
    {
        return _heap.Take(Count).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

/// <summary>
/// A heap whose top element is the smallest according to its comparer. A low-level helper kept
/// public for compatibility with Raffinert.FuzzySharp; used by
/// <see cref="Extensions.EnumerableExtensions.MaxN{T}"/>.
/// </summary>
/// <typeparam name="T">Element type.</typeparam>
public class MinHeap<T> : Heap<T>
{
    /// <summary>Creates an empty heap that uses <see cref="Comparer{T}.Default"/>.</summary>
    public MinHeap()
        : this(Comparer<T>.Default)
    {
    }

    /// <summary>Creates an empty heap that uses <paramref name="comparer"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is <c>null</c>.</exception>
    public MinHeap(Comparer<T> comparer)
        : base(comparer)
    {
    }

    /// <summary>Creates a heap of the elements of <paramref name="collection"/>, using <see cref="Comparer{T}.Default"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <c>null</c>.</exception>
    public MinHeap(IEnumerable<T> collection) : base(collection)
    {
    }

    /// <summary>Creates a heap of the elements of <paramref name="collection"/>, using <paramref name="comparer"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> or <paramref name="comparer"/> is <c>null</c>.</exception>
    public MinHeap(IEnumerable<T> collection, Comparer<T> comparer)
        : base(collection, comparer)
    {
    }

    /// <summary><c>true</c> when <paramref name="x"/> is less than or equal to <paramref name="y"/>.</summary>
    protected override bool Dominates(T x, T y)
    {
        return Comparer.Compare(x, y) <= 0;
    }
}