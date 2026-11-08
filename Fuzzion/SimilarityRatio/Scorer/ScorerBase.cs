using Fuzzion.PreProcess;
using System;

namespace Fuzzion.SimilarityRatio.Scorer;

/// <summary>
/// Base class for string scorers: a subclass implements <see cref="Score(string, string)"/>;
/// the preprocessing overload is provided.
/// </summary>
public abstract class ScorerBase : IRatioScorer
{
    /// <inheritdoc />
    public abstract double Score(string input1, string input2);

    /// <summary>
    /// Applies <paramref name="preprocessor"/> (<see cref="PreProcess.StringPreprocessor.Full"/> when null) to both strings,
    /// then calls <see cref="Score(string, string)"/>.
    /// </summary>
    /// <param name="input1">The first input.</param>
    /// <param name="input2">The second input.</param>
    /// <param name="preprocessor">Applied to both inputs first; <see cref="PreProcess.StringPreprocessor.Full"/> when null.</param>
    /// <returns>The score; from 0 to 100 for the built-in scorers.</returns>
    public double Score(string input1, string input2, Func<string, string> preprocessor)
    {
        preprocessor ??= StringPreprocessor.Full;
        input1 = preprocessor(input1);
        input2 = preprocessor(input2);
        return Score(input1, input2);
    }
}
