using System;

namespace Fuzzion;

/// <summary>
/// Where the best partial match was found, as returned by
/// <see cref="Fuzz.PartialRatioAlignment(string, string)"/>.
/// The matched parts are <c>input1[SourceStart..SourceEnd)</c> and
/// <c>input2[DestStart..DestEnd)</c>; <see cref="Fuzz.Ratio(string, string)"/> of those
/// two slices equals <see cref="Score"/>.
/// </summary>
public readonly struct ScoreAlignment : IEquatable<ScoreAlignment>
{
    /// <summary>Creates an alignment.</summary>
    public ScoreAlignment(double score, int sourceStart, int sourceEnd, int destStart, int destEnd)
    {
        Score = score;
        SourceStart = sourceStart;
        SourceEnd = sourceEnd;
        DestStart = destStart;
        DestEnd = destEnd;
    }

    /// <summary>The partial ratio, from 0 to 100.</summary>
    public double Score { get; }

    /// <summary>Start of the matched part of the first input (inclusive).</summary>
    public int SourceStart { get; }

    /// <summary>End of the matched part of the first input (exclusive).</summary>
    public int SourceEnd { get; }

    /// <summary>Start of the matched part of the second input (inclusive).</summary>
    public int DestStart { get; }

    /// <summary>End of the matched part of the second input (exclusive).</summary>
    public int DestEnd { get; }

    /// <summary>Deconstructs into <c>(score, sourceStart, sourceEnd, destStart, destEnd)</c>.</summary>
    public void Deconstruct(out double score, out int sourceStart, out int sourceEnd, out int destStart, out int destEnd)
    {
        score = Score;
        sourceStart = SourceStart;
        sourceEnd = SourceEnd;
        destStart = DestStart;
        destEnd = DestEnd;
    }

    /// <inheritdoc/>
    public bool Equals(ScoreAlignment other) =>
        Score.Equals(other.Score) && SourceStart == other.SourceStart && SourceEnd == other.SourceEnd &&
        DestStart == other.DestStart && DestEnd == other.DestEnd;

    /// <inheritdoc/>
    public override bool Equals(object obj) => obj is ScoreAlignment other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = Score.GetHashCode();
            hash = hash * 31 + SourceStart;
            hash = hash * 31 + SourceEnd;
            hash = hash * 31 + DestStart;
            return hash * 31 + DestEnd;
        }
    }

    /// <summary>Equality.</summary>
    public static bool operator ==(ScoreAlignment left, ScoreAlignment right) => left.Equals(right);

    /// <summary>Inequality.</summary>
    public static bool operator !=(ScoreAlignment left, ScoreAlignment right) => !left.Equals(right);

    /// <inheritdoc/>
    public override string ToString() =>
        $"ScoreAlignment(Score={Score}, SourceStart={SourceStart}, SourceEnd={SourceEnd}, DestStart={DestStart}, DestEnd={DestEnd})";
}
