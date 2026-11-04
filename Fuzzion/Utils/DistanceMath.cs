using System.Runtime.CompilerServices;

namespace Fuzzion.Utils;

/// <summary>Shared conversions between distance, similarity and their normalized forms.</summary>
internal static class DistanceMath
{
    /// <summary>
    /// <paramref name="distance"/> divided by <paramref name="maximum"/>; 0 when the maximum is 0
    /// (two empty inputs are identical).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Normalize(int distance, int maximum) =>
        maximum == 0 ? 0.0 : distance / (double)maximum;
}
