namespace Fuzzion.SimilarityRatio.Strategy;

internal static class QRatioStrategy
{
    /// <summary>
    /// Same as <see cref="DefaultRatioStrategy.Calculate"/>, but 0 when either input is
    /// empty (including both), as RapidFuzz's QRatio.
    /// </summary>
    public static double Calculate(string input1, string input2) =>
        input1.Length == 0 || input2.Length == 0 ? 0 : DefaultRatioStrategy.Calculate(input1, input2);
}
