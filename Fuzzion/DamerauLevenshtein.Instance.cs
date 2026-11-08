using System;

namespace Fuzzion;

/// <summary>
/// Damerau-Levenshtein distance against one fixed source string. Create it once and compare it with many
/// values. Results equal the static methods.
/// </summary>
public sealed partial class DamerauLevenshtein(string source) : IDisposable
{
    private readonly string _source = source ?? throw new ArgumentNullException(nameof(source));

    /// <summary>Distance from the source to <paramref name="value"/>.</summary>
    public int DistanceFrom(string value) => DamerauLevenshtein.Distance(_source, value);

    /// <summary>Similarity of the source and <paramref name="value"/>.</summary>
    public int SimilarityWith(string value) => DamerauLevenshtein.Similarity(_source, value);

    /// <summary>Normalized distance from the source to <paramref name="value"/>, from 0 to 1.</summary>
    public double NormalizedDistanceFrom(string value) => DamerauLevenshtein.NormalizedDistance(_source, value);

    /// <summary>Normalized similarity of the source and <paramref name="value"/>, from 0 to 1.</summary>
    public double NormalizedSimilarityWith(string value) => DamerauLevenshtein.NormalizedSimilarity(_source, value);

    /// <inheritdoc/>
    public void Dispose()
    {
    }
}

/// <summary>Generic version of <see cref="DamerauLevenshtein"/> for sequences of any <see cref="IEquatable{T}"/> type.</summary>
public sealed class DamerauLevenshteinT<T>(T[] source) : IDisposable where T : notnull, IEquatable<T>
{
    private readonly T[] _source = source ?? throw new ArgumentNullException(nameof(source));

    /// <summary>Distance from the source to <paramref name="value"/>.</summary>
    public int DistanceFrom(T[] value) => DamerauLevenshtein.Distance<T>(_source, value);

    /// <summary>Similarity of the source and <paramref name="value"/>.</summary>
    public int SimilarityWith(T[] value) => DamerauLevenshtein.Similarity<T>(_source, value);

    /// <summary>Normalized distance from the source to <paramref name="value"/>, from 0 to 1.</summary>
    public double NormalizedDistanceFrom(T[] value) => DamerauLevenshtein.NormalizedDistance<T>(_source, value);

    /// <summary>Normalized similarity of the source and <paramref name="value"/>, from 0 to 1.</summary>
    public double NormalizedSimilarityWith(T[] value) => DamerauLevenshtein.NormalizedSimilarity<T>(_source, value);

    /// <inheritdoc/>
    public void Dispose()
    {
    }
}
