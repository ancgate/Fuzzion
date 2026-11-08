using System;

namespace Fuzzion;

/// <summary>
/// Jaro-Winkler similarity: Jaro similarity plus a bonus for a shared start of up to
/// 4 characters, applied only when the Jaro similarity is above 0.7:
/// <c>sim + prefixLength * prefixWeight * (1 - sim)</c>, capped at 1.
/// <c>Distance = 1 - Similarity</c>. Ported from RapidFuzz (MIT, Max Bachmann).
/// </summary>
public sealed partial class JaroWinkler
{
    /// <summary>The default prefix weight, 0.1, as in Winkler's paper and RapidFuzz.</summary>
    public const double DefaultPrefixWeight = 0.1;

    private const int MaxPrefix = 4;
    private const double BoostThreshold = 0.7;

    /// <summary>Jaro-Winkler similarity from 0 to 1.</summary>
    /// <param name="s1">First input.</param>
    /// <param name="s2">Second input.</param>
    /// <param name="prefixWeight">Weight of the shared-prefix bonus, from 0 to 1.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="prefixWeight"/> is outside [0, 1].</exception>
    public static double Similarity(string s1, string s2, double prefixWeight = DefaultPrefixWeight) =>
        Similarity(s1.AsSpan(), s2.AsSpan(), prefixWeight);

    /// <inheritdoc cref="Similarity(string, string, double)"/>
    public static double Similarity<T>(ReadOnlySpan<T> s1, ReadOnlySpan<T> s2, double prefixWeight = DefaultPrefixWeight)
        where T : notnull, IEquatable<T>
    {
        ValidatePrefixWeight(prefixWeight);

        return Boost(Jaro.Similarity(s1, s2), s1, s2, prefixWeight);
    }

    /// <summary>Adds the Winkler bonus for the shared start of up to 4 characters to a Jaro similarity.</summary>
    internal static double Boost<T>(double jaro, ReadOnlySpan<T> s1, ReadOnlySpan<T> s2, double prefixWeight)
        where T : notnull, IEquatable<T>
    {
        if (jaro <= BoostThreshold)
        {
            return jaro;
        }

        var maxPrefix = Math.Min(Math.Min(s1.Length, s2.Length), MaxPrefix);
        var prefix = 0;
        while (prefix < maxPrefix && s1[prefix].Equals(s2[prefix]))
        {
            prefix++;
        }

        return Math.Min(jaro + prefix * prefixWeight * (1.0 - jaro), 1.0);
    }

    /// <summary><c>1 - Similarity</c>, from 0 (equal) to 1.</summary>
    public static double Distance(string s1, string s2, double prefixWeight = DefaultPrefixWeight) =>
        1.0 - Similarity(s1, s2, prefixWeight);

    /// <inheritdoc cref="Distance(string, string, double)"/>
    public static double Distance<T>(ReadOnlySpan<T> s1, ReadOnlySpan<T> s2, double prefixWeight = DefaultPrefixWeight)
        where T : notnull, IEquatable<T> =>
        1.0 - Similarity(s1, s2, prefixWeight);

    /// <summary>Same as <see cref="Similarity(string, string, double)"/> (already normalized).</summary>
    public static double NormalizedSimilarity(string s1, string s2, double prefixWeight = DefaultPrefixWeight) =>
        Similarity(s1, s2, prefixWeight);

    /// <inheritdoc cref="NormalizedSimilarity(string, string, double)"/>
    public static double NormalizedSimilarity<T>(ReadOnlySpan<T> s1, ReadOnlySpan<T> s2, double prefixWeight = DefaultPrefixWeight)
        where T : notnull, IEquatable<T> =>
        Similarity(s1, s2, prefixWeight);

    /// <summary>Same as <see cref="Distance(string, string, double)"/> (already normalized).</summary>
    public static double NormalizedDistance(string s1, string s2, double prefixWeight = DefaultPrefixWeight) =>
        Distance(s1, s2, prefixWeight);

    /// <inheritdoc cref="NormalizedDistance(string, string, double)"/>
    public static double NormalizedDistance<T>(ReadOnlySpan<T> s1, ReadOnlySpan<T> s2, double prefixWeight = DefaultPrefixWeight)
        where T : notnull, IEquatable<T> =>
        Distance(s1, s2, prefixWeight);

    internal static void ValidatePrefixWeight(double prefixWeight)
    {
        if (!(prefixWeight >= 0.0 && prefixWeight <= 1.0))
        {
            throw new ArgumentOutOfRangeException(nameof(prefixWeight), prefixWeight, "prefixWeight must be between 0 and 1.");
        }
    }
}
