using Fuzzion.PreProcess;
using System;

namespace Fuzzion.SimilarityRatio.Scorer;

public abstract class ScorerBase : IRatioScorer
{
    public abstract double Score(string input1, string input2);

    public double Score(string input1, string input2, Func<string, string> preprocessor)
    {
        preprocessor ??= StringPreprocessor.Full;
        input1 = preprocessor(input1);
        input2 = preprocessor(input2);
        return Score(input1, input2);
    }
}
