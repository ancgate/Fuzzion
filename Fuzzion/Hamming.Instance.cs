using System;

namespace Fuzzion;

/// <summary>
/// Hamming distance against one fixed source string. Create it once and compare it with many
/// values. Results equal the static methods with the same <c>pad</c> setting.
/// </summary>
public sealed partial class Hamming(string source, bool pad = true) : IDisposable
{
    private readonly string _source = source ?? throw new ArgumentNullException(nameof(source));

    /// <summary>Distance from the source to <paramref name="value"/>.</summary>
    public int DistanceFrom(string value) => Hamming.Distance(_source.AsSpan(), value.AsSpan(), pad);

    /// <summary>Similarity of the source and <paramref name="value"/>.</summary>
    public int SimilarityWith(string value) => Hamming.Similarity(_source.AsSpan(), value.AsSpan(), pad);

    /// <summary>Normalized distance from the source to <paramref name="value"/>, from 0 to 1.</summary>
    public double NormalizedDistanceFrom(string value) => Hamming.NormalizedDistance(_source.AsSpan(), value.AsSpan(), pad);

    /// <summary>Normalized similarity of the source and <paramref name="value"/>, from 0 to 1.</summary>
    public double NormalizedSimilarityWith(string value) => Hamming.NormalizedSimilarity(_source.AsSpan(), value.AsSpan(), pad);

    /// <inheritdoc/>
    public void Dispose()
    {
    }
}

/// <summary>Generic version of <see cref="Hamming"/> for sequences of any <see cref="IEquatable{T}"/> type.</summary>
public sealed class HammingT<T>(T[] source, bool pad = true) : IDisposable where T : notnull, IEquatable<T>
{
    private readonly T[] _source = source ?? throw new ArgumentNullException(nameof(source));

    /// <summary>Distance from the source to <paramref name="value"/>.</summary>
    public int DistanceFrom(T[] value) => Hamming.Distance<T>(_source, value, pad);

    /// <summary>Similarity of the source and <paramref name="value"/>.</summary>
    public int SimilarityWith(T[] value) => Hamming.Similarity<T>(_source, value, pad);

    /// <summary>Normalized distance from the source to <paramref name="value"/>, from 0 to 1.</summary>
    public double NormalizedDistanceFrom(T[] value) => Hamming.NormalizedDistance<T>(_source, value, pad);

    /// <summary>Normalized similarity of the source and <paramref name="value"/>, from 0 to 1.</summary>
    public double NormalizedSimilarityWith(T[] value) => Hamming.NormalizedSimilarity<T>(_source, value, pad);

    /// <inheritdoc/>
    public void Dispose()
    {
    }
}
