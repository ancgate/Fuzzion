using System;

namespace Fuzzion;

/// <summary>
/// Jaro-Winkler similarity against one fixed source string. Create it once and compare
/// it with many values: the Jaro bit pattern is built once. Results equal the static
/// methods with the same prefix weight. Dispose it when done.
/// </summary>
public sealed partial class JaroWinkler : IDisposable
{
    private readonly string _source;
    private readonly double _prefixWeight;
    private readonly Jaro _jaro;

    /// <param name="source">The string to compare against.</param>
    /// <param name="prefixWeight">Weight of the shared-prefix bonus, from 0 to 1.</param>
    public JaroWinkler(string source, double prefixWeight = DefaultPrefixWeight)
    {
        ValidatePrefixWeight(prefixWeight);
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _prefixWeight = prefixWeight;
        _jaro = new Jaro(source);
    }

    /// <summary>Distance (<c>1 - similarity</c>) from the source to <paramref name="value"/>.</summary>
    public double DistanceFrom(string value) => 1.0 - SimilarityWith(value);

    /// <summary>Similarity of the source and <paramref name="value"/>, from 0 to 1.</summary>
    public double SimilarityWith(string value) =>
        Boost(_jaro.SimilarityWith(value), _source.AsSpan(), value.AsSpan(), _prefixWeight);

    /// <summary>Same as <see cref="DistanceFrom"/>.</summary>
    public double NormalizedDistanceFrom(string value) => DistanceFrom(value);

    /// <summary>Same as <see cref="SimilarityWith"/>.</summary>
    public double NormalizedSimilarityWith(string value) => SimilarityWith(value);

    /// <inheritdoc/>
    public void Dispose() => _jaro.Dispose();
}

/// <summary>Generic version of <see cref="JaroWinkler"/> for sequences of any <see cref="IEquatable{T}"/> type.</summary>
public sealed class JaroWinklerT<T> : IDisposable where T : notnull, IEquatable<T>
{
    private readonly T[] _source;
    private readonly double _prefixWeight;
    private readonly JaroT<T> _jaro;

    /// <param name="source">The sequence to compare against.</param>
    /// <param name="prefixWeight">Weight of the shared-prefix bonus, from 0 to 1.</param>
    public JaroWinklerT(T[] source, double prefixWeight = JaroWinkler.DefaultPrefixWeight)
    {
        JaroWinkler.ValidatePrefixWeight(prefixWeight);
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _prefixWeight = prefixWeight;
        _jaro = new JaroT<T>(source);
    }

    /// <summary>Distance (<c>1 - similarity</c>) from the source to <paramref name="value"/>.</summary>
    public double DistanceFrom(T[] value) => 1.0 - SimilarityWith(value);

    /// <summary>Similarity of the source and <paramref name="value"/>, from 0 to 1.</summary>
    public double SimilarityWith(T[] value) =>
        JaroWinkler.Boost<T>(_jaro.SimilarityWith(value), _source, value, _prefixWeight);

    /// <summary>Same as <see cref="DistanceFrom"/>.</summary>
    public double NormalizedDistanceFrom(T[] value) => DistanceFrom(value);

    /// <summary>Same as <see cref="SimilarityWith"/>.</summary>
    public double NormalizedSimilarityWith(T[] value) => SimilarityWith(value);

    /// <inheritdoc/>
    public void Dispose() => _jaro.Dispose();
}
