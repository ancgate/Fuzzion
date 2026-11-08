using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Fuzzion.Utils;

/// <summary>
/// The bit masks of a pattern, used by the bit-parallel algorithms: for each distinct element,
/// a mask with bit <c>i</c> set where the pattern has that element at position <c>i</c>. Create
/// one with <see cref="PatternMatchVector.Create{T}"/>. It holds pooled memory, so dispose it
/// when done. A low-level type kept public for compatibility with Raffinert.FuzzySharp; the
/// <c>Indel.Block*</c> methods accept it.
/// </summary>
/// <typeparam name="TKey">Element type of the pattern.</typeparam>
public interface IPatternMatchVector<in TKey> : IDisposable where TKey : notnull, IEquatable<TKey>
{
    /// <summary>Length of the pattern.</summary>
    int Length { get; }

    /// <summary>Number of 64-bit words in each mask: <c>(Length + 63) / 64</c>.</summary>
    int Blocks { get; }

    /// <summary>
    /// The mask of <paramref name="key"/> (<see cref="Blocks"/> words, lowest positions in the
    /// first word), or an all-zero mask when the pattern does not contain it.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    ReadOnlySpan<ulong> GetOrZero(TKey key);

    /// <summary>Whether the pattern contains <paramref name="key"/>.</summary>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    bool ContainsKey(TKey key);
}

internal interface IPatternMatchVectorImpl<TKey> : IPatternMatchVector<TKey> where TKey : IEquatable<TKey>
{
    void Populate(ReadOnlySpan<TKey> source);
}

/// <summary>
/// Creates <see cref="IPatternMatchVector{TKey}"/> instances. A low-level helper kept public for
/// compatibility with Raffinert.FuzzySharp.
/// </summary>
public sealed class PatternMatchVector
{
    /// <summary>
    /// Builds the bit masks of <paramref name="source"/>. <c>char</c> patterns use a specialized
    /// implementation with direct lookup for characters up to U+00FF.
    /// </summary>
    /// <typeparam name="T">Element type of the pattern.</typeparam>
    /// <param name="source">The pattern.</param>
    /// <returns>The masks. They hold pooled memory, so dispose them when done.</returns>
    public static IPatternMatchVector<T> Create<T>(ReadOnlySpan<T> source) where T : notnull, IEquatable<T>
    {
        var blocks = (source.Length + 63) >> 6;

        var pmv = typeof(T) == typeof(char)
            ? (IPatternMatchVectorImpl<T>)(object)new PatternMatchVectorChar(source.Length, estimatedNonAsciiCharCount: 8, blocks: blocks)
            : new PatternMatchVector<T>(source.Length, 64, blocks);

        pmv.Populate(source);

        return pmv;
    }
}

internal sealed class PatternMatchVector<T> : IPatternMatchVectorImpl<T> where T : notnull, IEquatable<T>
{
    private readonly ArrayPool<ulong> _pool;
    private readonly DictionarySlimPooled<T, int> _indexMap;
    private ulong[] _buffer;
    private int _capacity;
    private int _next;
    private readonly ulong[] _zeroMask;
    private bool _disposed;

    public PatternMatchVector(int length, int estimatedCharCount, int blocks, ArrayPool<ulong> pool = null)
    {
        Length = length;
        _pool = pool ?? ArrayPool<ulong>.Shared;
        Blocks = blocks;
        _capacity = estimatedCharCount;
        _buffer = _pool.Rent(_capacity * Blocks);
        _zeroMask = _pool.Rent(Blocks);
        Array.Clear(_zeroMask, 0, Blocks);
        _indexMap = new DictionarySlimPooled<T, int>(estimatedCharCount);
        _next = 0;
    }

    public int Length { get; }
    public int Blocks { get; }

    public void Populate(ReadOnlySpan<T> source)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(PatternMatchVector<T>));

        for (var position = 0; position < source.Length; position++)
        {
            AddBit(source[position], position);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddBit(T key, int position)
    {
        ref var index = ref _indexMap.GetOrAddValueRef(key);

        if (index == 0)
        {
            if (_next >= _capacity)
            {
                GrowBuffer();
            }

            index = ++_next;

            Array.Clear(_buffer, (index - 1) * Blocks, Blocks);
        }

        int block = position >> 6;
        int offset = position & 63;

        _buffer[(index - 1) * Blocks + block] |= 1UL << offset;
    }

    private void GrowBuffer()
    {
        int newCapacity = _capacity * 2;
        ulong[] newBuffer = _pool.Rent(newCapacity * Blocks);

        // Copy existing masks
        Array.Copy(_buffer, 0, newBuffer, 0, _capacity * Blocks);

        // Return old buffer
        _pool.Return(_buffer);

        _buffer = newBuffer;
        _capacity = newCapacity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsKey(T key)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(PatternMatchVector<T>));

        return _indexMap.ContainsKey(key);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetMask(T key, out ReadOnlySpan<ulong> mask)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(PatternMatchVector<T>));

        if (_indexMap.TryGetValue(key, out var index))
        {
            mask = new ReadOnlySpan<ulong>(_buffer, (index - 1) * Blocks, Blocks);
            return true;
        }
        mask = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<ulong> GetOrZero(T key)
    {
        return TryGetMask(key, out var mask) ? mask : _zeroMask;
    }

    public ReadOnlySpan<ulong> GetOrDefault(T key, ReadOnlySpan<ulong> fallback)
    {
        return TryGetMask(key, out var mask) ? mask : fallback;
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _indexMap.Dispose();
        _pool.Return(_buffer);
        _pool.Return(_zeroMask);

        _disposed = true;
    }
}
