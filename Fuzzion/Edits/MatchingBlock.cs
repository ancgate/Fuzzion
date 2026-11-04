namespace Fuzzion.Edits;

public record MatchingBlock
{
    public int SourcePos { get; set; }
    public int DestPos { get; set; }
    public int Length { get; set; }

    public override string ToString() => $"({SourcePos},{DestPos},{Length})";
}