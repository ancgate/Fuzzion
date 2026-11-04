using Fuzzion.Utils;
using System;

namespace Fuzzion;

public sealed partial class Levenshtein(string source) : IDisposable
{
    private readonly IPatternMatchVector<char> _patternMatchVector = PatternMatchVector.Create(source.AsSpan());

    public int DistanceFrom(string value)
    {
        return Distance(_patternMatchVector, value.AsSpan());
    }

    public void Dispose()
    {
        _patternMatchVector.Dispose();
    }
}