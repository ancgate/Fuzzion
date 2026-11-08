using Fuzzion.Utils;
using System;

namespace Fuzzion;

/// <remarks>
/// Instance form: create it once for a fixed source string and compare it with many values;
/// the source's bit pattern is built once and reused. It holds pooled memory, so dispose it
/// when done.
/// </remarks>
/// <param name="source">The string to compare against.</param>
public sealed partial class Levenshtein(string source) : IDisposable
{
    private readonly IPatternMatchVector<char> _patternMatchVector = PatternMatchVector.Create(source.AsSpan());

    /// <summary>
    /// Levenshtein distance from the source to <paramref name="value"/>, with all operations
    /// costing 1 (same as <see cref="Distance(string, string, int, int, int, int?, int?)"/> with
    /// the default costs).
    /// </summary>
    public int DistanceFrom(string value)
    {
        return Distance(_patternMatchVector, value.AsSpan());
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _patternMatchVector.Dispose();
    }
}