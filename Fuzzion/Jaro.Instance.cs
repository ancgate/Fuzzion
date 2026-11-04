using System;

namespace Fuzzion;

/// <summary>
/// Jaro similarity against one fixed source string. Create it once and compare it with many
/// values. Results equal the static methods.
/// </summary>
public sealed partial class Jaro(string source) : IDisposable
{
    private readonly string _source = source ?? throw new ArgumentNullException(nameof(source));

    /// <summary>Distance from the source to <paramref name="value"/>.</summary>
    public double DistanceFrom(string value) => Jaro.Distance(_source.AsSpan(), value.AsSpan());

    /// <summary>Similarity of the source and <paramref name="value"/>.</summary>
    public double SimilarityWith(string value) => Jaro.Similarity(_source.AsSpan(), value.AsSpan());

    /// <summary>Normalized distance from the source to <paramref name="value"/>, from 0 to 1.</summary>
    public double NormalizedDistanceFrom(string value) => Jaro.NormalizedDistance(_source.AsSpan(), value.AsSpan());

    /// <summary>Normalized similarity of the source and <paramref name="value"/>, from 0 to 1.</summary>
    public double NormalizedSimilarityWith(string value) => Jaro.NormalizedSimilarity(_source.AsSpan(), value.AsSpan());

    /// <inheritdoc/>
    public void Dispose()
    {
    }
}

/// <summary>Generic version of <see cref="Jaro"/> for sequences of any <see cref="IEquatable{T}"/> type.</summary>
public sealed class JaroT<T>(T[] source) : IDisposable where T : notnull, IEquatable<T>
{
    private readonly T[] _source = source ?? throw new ArgumentNullException(nameof(source));

    /// <summary>Distance from the source to <paramref name="value"/>.</summary>
    public double DistanceFrom(T[] value) => Jaro.Distance<T>(_source, value);

    /// <summary>Similarity of the source and <paramref name="value"/>.</summary>
    public double SimilarityWith(T[] value) => Jaro.Similarity<T>(_source, value);

    /// <summary>Normalized distance from the source to <paramref name="value"/>, from 0 to 1.</summary>
    public double NormalizedDistanceFrom(T[] value) => Jaro.NormalizedDistance<T>(_source, value);

    /// <summary>Normalized similarity of the source and <paramref name="value"/>, from 0 to 1.</summary>
    public double NormalizedSimilarityWith(T[] value) => Jaro.NormalizedSimilarity<T>(_source, value);

    /// <inheritdoc/>
    public void Dispose()
    {
    }
}
