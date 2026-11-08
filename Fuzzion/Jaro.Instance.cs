using System;
using Fuzzion.Utils;

namespace Fuzzion;

/// <summary>
/// Jaro similarity against one fixed source string. Create it once and compare it with
/// many values: the source's bit pattern is built once and reused. Results equal the
/// static methods. Dispose it when done.
/// </summary>
public sealed partial class Jaro : IDisposable
{
    private readonly string _source;
    private readonly IPatternMatchVector<char> _pattern;

    /// <param name="source">The string to compare against.</param>
    public Jaro(string source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        if (source.Length > ScalarLimit)
        {
            _pattern = PatternMatchVector.Create(source.AsSpan());
        }
    }

    /// <summary>Jaro similarity of the source and <paramref name="value"/>, from 0 to 1.</summary>
    public double SimilarityWith(string value) =>
        SimilarityWithPattern(_pattern, _source.AsSpan(), (value ?? throw new ArgumentNullException(nameof(value))).AsSpan());

    /// <summary><c>1 - SimilarityWith</c>.</summary>
    public double DistanceFrom(string value) => 1.0 - SimilarityWith(value);

    /// <summary>Same as <see cref="SimilarityWith"/>.</summary>
    public double NormalizedSimilarityWith(string value) => SimilarityWith(value);

    /// <summary>Same as <see cref="DistanceFrom"/>.</summary>
    public double NormalizedDistanceFrom(string value) => DistanceFrom(value);

    /// <inheritdoc/>
    public void Dispose() => _pattern?.Dispose();
}

/// <summary>Generic version of <see cref="Jaro"/> for sequences of any <see cref="IEquatable{T}"/> type.</summary>
public sealed class JaroT<T> : IDisposable where T : notnull, IEquatable<T>
{
    private readonly T[] _source;
    private readonly IPatternMatchVector<T> _pattern;

    /// <param name="source">The sequence to compare against.</param>
    public JaroT(T[] source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        if (source.Length > Jaro.ScalarLimit)
        {
            _pattern = PatternMatchVector.Create<T>(source);
        }
    }

    /// <summary>Jaro similarity of the source and <paramref name="value"/>, from 0 to 1.</summary>
    public double SimilarityWith(T[] value) => Jaro.SimilarityWithPattern<T>(_pattern, _source, value);

    /// <summary><c>1 - SimilarityWith</c>.</summary>
    public double DistanceFrom(T[] value) => 1.0 - SimilarityWith(value);

    /// <summary>Same as <see cref="SimilarityWith"/>.</summary>
    public double NormalizedSimilarityWith(T[] value) => SimilarityWith(value);

    /// <summary>Same as <see cref="DistanceFrom"/>.</summary>
    public double NormalizedDistanceFrom(T[] value) => DistanceFrom(value);

    /// <inheritdoc/>
    public void Dispose() => _pattern?.Dispose();
}
