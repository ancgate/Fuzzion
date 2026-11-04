using System;
using Fuzzion.Extensions;
using Fuzzion.PreProcess;
using Fuzzion.SimilarityRatio.Scorer.StrategySensitive.Generic;

namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

public abstract class TokenDifferenceScorerBase : StrategySensitiveScorerBase<string>, IRatioScorer
{
    public override double Score(string[] input1, string[] input2)
    {
        return Scorer(input1, input2);
    }

    public double Score(string input1, string input2)
    {
        var tokens1 = input1.GetSortedWords();
        var tokens2 = input2.GetSortedWords();

        return Score(tokens1, tokens2);
    }


    public double Score(string input1, string input2, Func<string, string> preprocessor)
    {
        preprocessor ??= StringPreprocessor.Full;
        input1 = preprocessor(input1);
        input2 = preprocessor(input2);

        return Score(input1, input2);
    }
}
