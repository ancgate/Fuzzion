namespace Fuzzion.Edits;

/// <summary>
/// An edit operation as ranges: <c>source[SourceBegin..SourceEnd)</c> is kept (equal), replaced
/// or deleted, or <c>dest[DestBegin..DestEnd)</c> is inserted. Same as one entry of RapidFuzz's
/// <c>Opcodes</c>; see <see cref="OpCodes"/>.
/// </summary>
public class OpCode
{
    /// <summary>
    /// The kind of edit; <see cref="EditType.EQUAL"/> (or <see cref="EditType.KEEP"/>) for an
    /// unchanged range.
    /// </summary>
    public EditType EditType    { get; set; }

    /// <summary>Start of the source range (inclusive).</summary>
    public int      SourceBegin { get; set; }

    /// <summary>End of the source range (exclusive). Equals <see cref="SourceBegin"/> for an insertion.</summary>
    public int      SourceEnd   { get; set; }

    /// <summary>Start of the destination range (inclusive).</summary>
    public int      DestBegin   { get; set; }

    /// <summary>End of the destination range (exclusive). Equals <see cref="DestBegin"/> for a deletion.</summary>
    public int      DestEnd     { get; set; }

    /// <summary>
    /// Formats the opcode as <c>TYPE(SourceBegin,SourceEnd,DestBegin,DestEnd)</c>, for example
    /// <c>REPLACE(2,3,2,3)</c>.
    /// </summary>
    public override string ToString()
    {
        return $"{EditType}({SourceBegin},{SourceEnd},{DestBegin},{DestEnd})";
    }
}