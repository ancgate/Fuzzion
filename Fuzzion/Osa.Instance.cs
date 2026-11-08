using System;
using Fuzzion.Utils;

namespace Fuzzion;

/// <summary>
/// OSA distance against one fixed source string. Create it once and compare it with
/// many values: its bit pattern is built once and reused. Results equal the static methods. Dispose it when done.
/// </summary>
public sealed partial class Osa : IDisposable
{
    private readonly string _source;
    private readonly IPatternMatchVector<char> _pattern;

    /// <param name="source">The string to compare against.</param>
    public Osa(string source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        if (source.Length > 0)
        {
            _pattern = PatternMatchVector.Create(source.AsSpan());
        }
    }

    /// <summary>OSA distance from the source to <paramref name="value"/>.</summary>
    public int DistanceFrom(string value)
    {
        if (_pattern == null || value.Length < _source.Length)
        {
            // Empty source, or a shorter value (which should be the pattern).
            return Distance(_source.AsSpan(), value.AsSpan());
        }

        return _source.Length <= 64
            ? DistanceHyrroe2003(_pattern, value.AsSpan())
            : DistanceHyrroe2003Block(_pattern, value.AsSpan());
    }

    /// <summary>Similarity of the source and <paramref name="value"/>.</summary>
    public int SimilarityWith(string value) => Math.Max(_source.Length, value.Length) - DistanceFrom(value);

    /// <summary>Normalized distance from the source to <paramref name="value"/>, from 0 to 1.</summary>
    public double NormalizedDistanceFrom(string value) =>
        DistanceMath.Normalize(DistanceFrom(value), Math.Max(_source.Length, value.Length));

    /// <summary>Normalized similarity of the source and <paramref name="value"/>, from 0 to 1.</summary>
    public double NormalizedSimilarityWith(string value) => 1.0 - NormalizedDistanceFrom(value);

    /// <inheritdoc/>
    public void Dispose() => _pattern?.Dispose();
}

/// <summary>Generic version of <see cref="Osa"/> for sequences of any <see cref="IEquatable{T}"/> type.</summary>
public sealed class OsaT<T>(T[] source) : IDisposable where T : notnull, IEquatable<T>
{
    private readonly T[] _source = source ?? throw new ArgumentNullException(nameof(source));

    /// <summary>OSA distance from the source to <paramref name="value"/>.</summary>
    public int DistanceFrom(T[] value) => Osa.Distance<T>(_source, value);

    /// <summary>Similarity of the source and <paramref name="value"/>.</summary>
    public int SimilarityWith(T[] value) => Osa.Similarity<T>(_source, value);

    /// <summary>Normalized distance from the source to <paramref name="value"/>, from 0 to 1.</summary>
    public double NormalizedDistanceFrom(T[] value) => Osa.NormalizedDistance<T>(_source, value);

    /// <summary>Normalized similarity of the source and <paramref name="value"/>, from 0 to 1.</summary>
    public double NormalizedSimilarityWith(T[] value) => Osa.NormalizedSimilarity<T>(_source, value);

    /// <inheritdoc/>
    public void Dispose()
    {
    }
}
