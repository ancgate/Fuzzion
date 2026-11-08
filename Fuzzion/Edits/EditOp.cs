namespace Fuzzion.Edits;

/// <summary>
/// The kind of an <see cref="EditOp"/> or <see cref="OpCode"/>. The names are upper case for
/// compatibility with FuzzySharp.
/// </summary>
public enum EditType
{
    /// <summary>An element of the source is deleted.</summary>
    DELETE = 0,

    /// <summary>
    /// A range that is the same in both sequences, as produced by <see cref="EditOps.AsOpCodes()"/>
    /// (RapidFuzz's <c>equal</c>).
    /// </summary>
    EQUAL = 1,

    /// <summary>An element of the destination is inserted.</summary>
    INSERT = 2,

    /// <summary>An element of the source is replaced by an element of the destination.</summary>
    REPLACE = 3,

    /// <summary>
    /// A range that is the same in both sequences, as produced by the older
    /// <see cref="Extensions.EditOpsExtensions.AsOpCodes"/> extension. Treated like
    /// <see cref="EQUAL"/> everywhere else.
    /// </summary>
    KEEP = 4,
}

/// <summary>
/// A single edit operation (insert, delete or replace) and the positions it applies to in the
/// source and the destination. Same as one entry of RapidFuzz's <c>Editops</c>; see <see cref="EditOps"/>.
/// </summary>
public record EditOp
{
    /// <summary>The kind of operation: <see cref="EditType.INSERT"/>, <see cref="EditType.DELETE"/> or <see cref="EditType.REPLACE"/>.</summary>
    public EditType EditType { get; set; }

    /// <summary>
    /// Position in the source: the element that is deleted or replaced, or where the inserted
    /// element goes.
    /// </summary>
    public int SourcePos { get; set; }

    /// <summary>
    /// Position in the destination: the element that is inserted or that replaces the source
    /// element, or (for a deletion) where in the destination the deletion happens.
    /// </summary>
    public int DestPos { get; set; }

    /// <summary>Formats the operation as <c>TYPE(SourcePos, DestPos)</c>, for example <c>INSERT(2, 3)</c>.</summary>
    public override string ToString()
    {
        return $"{EditType}({SourcePos}, {DestPos})";
    }
}