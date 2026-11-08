namespace Fuzzion.Edits;

/// <summary>
/// A run of <see cref="Length"/> equal elements that starts at <see cref="SourcePos"/> in the
/// source and at <see cref="DestPos"/> in the destination. Lists of matching blocks end with a
/// zero-length block at (source length, destination length), as in RapidFuzz and Python's
/// <c>difflib</c>.
/// </summary>
public record MatchingBlock
{
    /// <summary>Start of the block in the source.</summary>
    public int SourcePos { get; set; }

    /// <summary>Start of the block in the destination.</summary>
    public int DestPos { get; set; }

    /// <summary>Number of equal elements; 0 for the final block.</summary>
    public int Length { get; set; }

    /// <summary>Formats the block as <c>(SourcePos,DestPos,Length)</c>.</summary>
    public override string ToString() => $"({SourcePos},{DestPos},{Length})";
}