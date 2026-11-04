using System;
using Fuzzion.PreProcess;

namespace Fuzzion.SimilarityRatio.Strategy;

internal class CachedDefaultRatioStrategy : ICachedStrategy
{
    private readonly Indel _indel;
    private readonly Func<string, string> _preprocessor;
    private readonly bool _input1IsEmpty;

    public CachedDefaultRatioStrategy(string input1, Func<string, string> preprocessor = null)
    {
        preprocessor ??= StringPreprocessor.None;
        _preprocessor = preprocessor;
        var processedInput1 = _preprocessor(input1);
        _input1IsEmpty = processedInput1.Length == 0;
        _indel = new Indel(processedInput1);
    }

    public double Calculate(string input2)
    {
        var  processedInput2 = _preprocessor(input2);
        // Two empty inputs are identical (100); exactly one empty input scores 0.
        // Matches RapidFuzz and TheFuzz (see docs/known-differences.md, KD1).
        if (processedInput2.Length == 0)
        {
            return _input1IsEmpty ? 100 : 0;
        }

        return 100 * _indel.NormalizedSimilarityWith(processedInput2);
    }

    public void Dispose()
    {
        _indel.Dispose();
    }
}
