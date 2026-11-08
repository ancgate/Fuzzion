using System;
using Fuzzion.Extensions;
using Fuzzion.PreProcess;
using Fuzzion.SimilarityRatio.Scorer.StrategySensitive.Generic;

namespace Fuzzion.SimilarityRatio.Scorer.StrategySensitive;

/// <summary>
/// Base class for token difference scorers, which compare sequences of words rather than
/// characters: each word is one element and matches only an identical word.
/// </summary>
public abstract class TokenDifferenceScorerBase : StrategySensitiveScorerBase<string>, IRatioScorer
{
    /// <summary>Compares the two word sequences, in the order given, with <see cref="StrategySensitiveScorerBase{T}.Scorer"/>.</summary>
    /// <param name="input1">The first word sequence.</param>
    /// <param name="input2">The second word sequence.</param>
    /// <returns>A score from 0 to 100.</returns>
    public override double Score(string[] input1, string[] input2)
    {
        return Scorer(input1, input2);
    }

    /// <summary>
    /// Splits both strings on whitespace, sorts the words, and compares the word sequences.
    /// No preprocessing is applied.
    /// </summary>
    /// <param name="input1">The first input.</param>
    /// <param name="input2">The second input.</param>
    /// <returns>A score from 0 to 100.</returns>
    public double Score(string input1, string input2)
    {
        var tokens1 = input1.GetSortedWords();
        var tokens2 = input2.GetSortedWords();

        return Score(tokens1, tokens2);
    }


    /// <summary>
    /// Applies <paramref name="preprocessor"/> (<see cref="PreProcess.StringPreprocessor.Full"/> when null) to both strings,
    /// then calls <see cref="Score(string, string)"/>.
    /// </summary>
    /// <param name="input1">The first input.</param>
    /// <param name="input2">The second input.</param>
    /// <param name="preprocessor">Applied to both inputs first; <see cref="PreProcess.StringPreprocessor.Full"/> when null.</param>
    /// <returns>A score from 0 to 100.</returns>
    public double Score(string input1, string input2, Func<string, string> preprocessor)
    {
        preprocessor ??= StringPreprocessor.Full;
        input1 = preprocessor(input1);
        input2 = preprocessor(input2);

        return Score(input1, input2);
    }
}
