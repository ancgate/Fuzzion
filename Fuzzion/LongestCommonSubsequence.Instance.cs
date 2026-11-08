using Fuzzion.Utils;
using System;

namespace Fuzzion;

/// <remarks>
/// Instance form: create it once for a fixed source string and compare it with many values;
/// the source's bit pattern is built once and reused. Results equal the static methods.
/// It holds pooled memory, so dispose it when done.
/// </remarks>
/// <param name="source">The string to compare against.</param>
public sealed partial class LongestCommonSubsequence(string source) : IDisposable
{
    private readonly IPatternMatchVector<char> _patternMatchVector = PatternMatchVector.Create(source.AsSpan());

    /// <summary>
    /// LCS distance from the source to <paramref name="value"/>: the longer length minus the
    /// length of the longest common subsequence.
    /// </summary>
    public int DistanceFrom(string value)
    {
        return DistanceImpl(_patternMatchVector, value.AsSpan());
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _patternMatchVector.Dispose();
    }
}