using Fuzzion.Utils;
using System;

namespace Fuzzion;

/// <remarks>
/// Instance form: create it once for a fixed source string and compare it with many values;
/// the source's bit pattern is built once and reused. Results equal the static methods.
/// It holds pooled memory, so dispose it when done.
/// </remarks>
/// <param name="source">The string to compare against.</param>
public sealed partial class Indel(string source) : IDisposable
{
    private readonly IPatternMatchVector<char> _patternMatchVector = PatternMatchVector.Create(source.AsSpan());

    /// <summary>Indel distance from the source to <paramref name="value"/>.</summary>
    public int DistanceFrom(string value)
    {
        return DistanceImpl(_patternMatchVector, value.AsSpan());
    }

    /// <summary>Normalized Indel similarity of the source and <paramref name="value"/>, from 0 to 1.</summary>
    public double NormalizedSimilarityWith(string value)
    {
        return NormalizedSimilarityImpl(_patternMatchVector, value.AsSpan());
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _patternMatchVector.Dispose();
    }
}

/// <summary>
/// Generic version of <see cref="Indel"/>'s instance form for sequences of any
/// <see cref="IEquatable{T}"/> type. Create it once and compare it with many values: the
/// source's bit pattern is built once and reused. Results equal the static methods.
/// It holds pooled memory, so dispose it when done.
/// </summary>
/// <typeparam name="T">Element type of the sequences.</typeparam>
/// <param name="source">The sequence to compare against.</param>
public sealed class IndelT<T>(T[] source) : IDisposable where T : IEquatable<T>
{
    private readonly IPatternMatchVector<T> _patternMatchVector = PatternMatchVector.Create(source.AsSpan());

    /// <summary>Indel distance from the source to <paramref name="value"/>.</summary>
    public int DistanceFrom(T[] value)
    {
        return Indel.DistanceImpl(_patternMatchVector, value.AsSpan());
    }

    /// <summary>Normalized Indel similarity of the source and <paramref name="value"/>, from 0 to 1.</summary>
    public double NormalizedSimilarityWith(T[] value)
    {
        return Indel.NormalizedSimilarityImpl(_patternMatchVector, value.AsSpan());
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _patternMatchVector.Dispose();
    }
}
