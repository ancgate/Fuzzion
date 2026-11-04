using Fuzzion.Utils;
using System;

namespace Fuzzion;

public sealed partial class LongestCommonSubsequence(string source) : IDisposable
{
    private readonly IPatternMatchVector<char> _patternMatchVector = PatternMatchVector.Create(source.AsSpan());

    public int DistanceFrom(string value)
    {
        return DistanceImpl(_patternMatchVector, value.AsSpan());
    }

    public void Dispose()
    {
        _patternMatchVector.Dispose();
    }
}