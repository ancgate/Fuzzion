using System;

namespace Fuzzion.SimilarityRatio.Scorer;

/// <summary>
/// Compares two strings and returns a score: for the built-in scorers a similarity from 0 to 100,
/// higher meaning more similar (an <see cref="IDistanceScorer"/> returns a distance instead).
/// Searches in <see cref="Process"/> take one; get a shared built-in one from <see cref="ScorerCache.Get{T}"/>.
/// </summary>
public interface IRatioScorer
{
    /// <summary>Scores the two strings as given, without preprocessing.</summary>
    /// <param name="input1">The first input.</param>
    /// <param name="input2">The second input.</param>
    /// <returns>The score; from 0 to 100 for the built-in scorers.</returns>
    double Score(string input1, string input2);
    /// <summary>Applies <paramref name="preprocessor"/> to both strings, then scores them.</summary>
    /// <param name="input1">The first input.</param>
    /// <param name="input2">The second input.</param>
    /// <param name="preprocessor">Applied to both inputs first; the built-in scorers use <see cref="PreProcess.StringPreprocessor.Full"/> when it is null.</param>
    /// <returns>The score; from 0 to 100 for the built-in scorers.</returns>
    double Score(string input1, string input2, Func<string, string> preprocessor);
}
