using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fuzzion.Extensions;

namespace Fuzzion.Edits;

/// <summary>
/// The edit operations (insert, delete, replace) that turn a source sequence into a
/// destination sequence, in order, plus both lengths. Same as RapidFuzz's <c>Editops</c>
/// (plan item #28). It converts implicitly to <c>EditOp[]</c> for older code.
/// </summary>
public sealed class EditOps : IReadOnlyList<EditOp>, IEquatable<EditOps>
{
    private readonly EditOp[] _ops;

    /// <summary>Creates a list of edit operations.</summary>
    /// <param name="ops">The operations, ordered by position. Equal/keep operations are dropped.</param>
    /// <param name="sourceLength">Length of the source.</param>
    /// <param name="destLength">Length of the destination.</param>
    public EditOps(IEnumerable<EditOp> ops, int sourceLength, int destLength)
    {
        if (ops == null) throw new ArgumentNullException(nameof(ops));
        if (sourceLength < 0) throw new ArgumentOutOfRangeException(nameof(sourceLength));
        if (destLength < 0) throw new ArgumentOutOfRangeException(nameof(destLength));

        _ops = ops.Where(op => op.EditType is not (EditType.EQUAL or EditType.KEEP))
            .Select(op => new EditOp { EditType = op.EditType, SourcePos = op.SourcePos, DestPos = op.DestPos })
            .ToArray();
        SourceLength = sourceLength;
        DestLength = destLength;
    }

    /// <summary>Length of the source.</summary>
    public int SourceLength { get; }

    /// <summary>Length of the destination.</summary>
    public int DestLength { get; }

    /// <inheritdoc/>
    public int Count => _ops.Length;

    /// <summary>Same as <see cref="Count"/> (for code written against <c>EditOp[]</c>).</summary>
    public int Length => _ops.Length;

    /// <inheritdoc/>
    public EditOp this[int index] => _ops[index];

    /// <summary>
    /// Applies the operations to <paramref name="source"/>, taking inserted and replacing
    /// characters from <paramref name="dest"/>. Applying the full list gives <paramref name="dest"/>.
    /// </summary>
    public string Apply(string source, string dest)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (dest == null) throw new ArgumentNullException(nameof(dest));

        var result = new StringBuilder(dest.Length);
        var sourcePos = 0;
        foreach (var op in _ops)
        {
            // Unchanged characters before this operation.
            while (sourcePos < op.SourcePos)
            {
                result.Append(source[sourcePos++]);
            }

            switch (op.EditType)
            {
                case EditType.REPLACE:
                    result.Append(dest[op.DestPos]);
                    sourcePos++;
                    break;
                case EditType.INSERT:
                    result.Append(dest[op.DestPos]);
                    break;
                case EditType.DELETE:
                    sourcePos++;
                    break;
            }
        }

        while (sourcePos < source.Length)
        {
            result.Append(source[sourcePos++]);
        }

        return result.ToString();
    }

    /// <summary>The operations that turn the destination back into the source.</summary>
    public EditOps Inverse() =>
        new(_ops.Select(op => new EditOp { EditType = Swap(op.EditType), SourcePos = op.DestPos, DestPos = op.SourcePos }),
            DestLength, SourceLength);

    /// <summary>
    /// Removes the operations in <paramref name="subsequence"/> (which must appear in this
    /// list, in order) and shifts the source positions of the remaining ones to match.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="subsequence"/> is not a subsequence of this list.</exception>
    public EditOps RemoveSubsequence(EditOps subsequence)
    {
        if (subsequence == null) throw new ArgumentNullException(nameof(subsequence));
        if (subsequence.Count > Count) throw NotASubsequence();

        var result = new List<EditOp>(Count - subsequence.Count);
        var offset = 0;
        var position = 0;
        foreach (var removed in subsequence)
        {
            while (position != _ops.Length && !_ops[position].Equals(removed))
            {
                var op = _ops[position++];
                result.Add(new EditOp { EditType = op.EditType, SourcePos = op.SourcePos + offset, DestPos = op.DestPos });
            }

            if (position == _ops.Length) throw NotASubsequence();

            if (removed.EditType == EditType.INSERT) offset++;
            else if (removed.EditType == EditType.DELETE) offset--;
            position++;
        }

        while (position != _ops.Length)
        {
            var op = _ops[position++];
            result.Add(new EditOp { EditType = op.EditType, SourcePos = op.SourcePos + offset, DestPos = op.DestPos });
        }

        return new EditOps(result, SourceLength, DestLength);
    }

    /// <summary>
    /// The same edits as ranges (<see cref="OpCodes"/>), including the unchanged ("equal")
    /// ranges in between, with consecutive operations of the same kind merged.
    /// </summary>
    public OpCodes AsOpCodes()
    {
        var blocks = new List<OpCode>();
        int sourcePos = 0, destPos = 0, i = 0;
        while (i < _ops.Length)
        {
            if (sourcePos < _ops[i].SourcePos || destPos < _ops[i].DestPos)
            {
                blocks.Add(new OpCode { EditType = EditType.EQUAL, SourceBegin = sourcePos, SourceEnd = _ops[i].SourcePos, DestBegin = destPos, DestEnd = _ops[i].DestPos });
                sourcePos = _ops[i].SourcePos;
                destPos = _ops[i].DestPos;
            }

            int sourceBegin = sourcePos, destBegin = destPos;
            var type = _ops[i].EditType;
            while (i < _ops.Length && _ops[i].EditType == type && _ops[i].SourcePos == sourcePos && _ops[i].DestPos == destPos)
            {
                if (type == EditType.REPLACE) { sourcePos++; destPos++; }
                else if (type == EditType.INSERT) destPos++;
                else if (type == EditType.DELETE) sourcePos++;
                i++;
            }

            blocks.Add(new OpCode { EditType = type, SourceBegin = sourceBegin, SourceEnd = sourcePos, DestBegin = destBegin, DestEnd = destPos });
        }

        if (sourcePos < SourceLength || destPos < DestLength)
        {
            blocks.Add(new OpCode { EditType = EditType.EQUAL, SourceBegin = sourcePos, SourceEnd = SourceLength, DestBegin = destPos, DestEnd = DestLength });
        }

        return new OpCodes(blocks, SourceLength, DestLength);
    }

    /// <summary>The unchanged blocks, ending with a zero-length block at (SourceLength, DestLength).</summary>
    public List<MatchingBlock> AsMatchingBlocks() => _ops.AsMatchingBlocks(SourceLength, DestLength);

    /// <summary>Converts ranges back into single-character operations.</summary>
    public static EditOps FromOpCodes(OpCodes opCodes) =>
        (opCodes ?? throw new ArgumentNullException(nameof(opCodes))).AsEditOps();

    /// <summary>A copy of the operations as an array.</summary>
    public static implicit operator EditOp[](EditOps ops) =>
        ops?._ops.Select(op => new EditOp { EditType = op.EditType, SourcePos = op.SourcePos, DestPos = op.DestPos }).ToArray();

    /// <inheritdoc/>
    public IEnumerator<EditOp> GetEnumerator() => ((IEnumerable<EditOp>)_ops).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _ops.GetEnumerator();

    /// <inheritdoc/>
    public bool Equals(EditOps other) =>
        other != null && SourceLength == other.SourceLength && DestLength == other.DestLength && _ops.SequenceEqual(other._ops);

    /// <inheritdoc/>
    public override bool Equals(object obj) => Equals(obj as EditOps);

    /// <inheritdoc/>
    public override int GetHashCode() => unchecked((SourceLength * 397) ^ (DestLength * 31) ^ _ops.Length);

    /// <inheritdoc/>
    public override string ToString() =>
        $"EditOps([{string.Join(", ", _ops.Select(o => o.ToString()))}], SourceLength={SourceLength}, DestLength={DestLength})";

    internal static EditType Swap(EditType type) => type switch
    {
        EditType.INSERT => EditType.DELETE,
        EditType.DELETE => EditType.INSERT,
        _ => type,
    };

    private static ArgumentException NotASubsequence() =>
        new("subsequence is not a subsequence of these edit operations.", "subsequence");
}

/// <summary>
/// Edit operations as ranges: each <see cref="OpCode"/> says that
/// <c>source[SourceBegin..SourceEnd)</c> is kept (equal), replaced, deleted, or that
/// <c>dest[DestBegin..DestEnd)</c> is inserted. The ranges cover both sequences end to end.
/// Same as RapidFuzz's <c>Opcodes</c> (plan item #28).
/// </summary>
public sealed class OpCodes : IReadOnlyList<OpCode>, IEquatable<OpCodes>
{
    private readonly OpCode[] _codes;

    /// <summary>Creates a list of opcodes.</summary>
    public OpCodes(IEnumerable<OpCode> codes, int sourceLength, int destLength)
    {
        if (codes == null) throw new ArgumentNullException(nameof(codes));
        _codes = codes.Select(Copy).ToArray();
        SourceLength = sourceLength;
        DestLength = destLength;
    }

    /// <summary>Length of the source.</summary>
    public int SourceLength { get; }

    /// <summary>Length of the destination.</summary>
    public int DestLength { get; }

    /// <inheritdoc/>
    public int Count => _codes.Length;

    /// <inheritdoc/>
    public OpCode this[int index] => _codes[index];

    /// <summary>Builds the destination from the kept parts of <paramref name="source"/> and the new parts of <paramref name="dest"/>.</summary>
    public string Apply(string source, string dest)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (dest == null) throw new ArgumentNullException(nameof(dest));

        var result = new StringBuilder(dest.Length);
        foreach (var code in _codes)
        {
            switch (code.EditType)
            {
                case EditType.EQUAL:
                case EditType.KEEP:
                    result.Append(source, code.SourceBegin, code.SourceEnd - code.SourceBegin);
                    break;
                case EditType.REPLACE:
                case EditType.INSERT:
                    result.Append(dest, code.DestBegin, code.DestEnd - code.DestBegin);
                    break;
            }
        }

        return result.ToString();
    }

    /// <summary>The opcodes that turn the destination back into the source.</summary>
    public OpCodes Inverse() =>
        new(_codes.Select(c => new OpCode
        {
            EditType = EditOps.Swap(c.EditType),
            SourceBegin = c.DestBegin,
            SourceEnd = c.DestEnd,
            DestBegin = c.SourceBegin,
            DestEnd = c.SourceEnd,
        }), DestLength, SourceLength);

    /// <summary>The same edits as single-character operations.</summary>
    public EditOps AsEditOps()
    {
        var ops = new List<EditOp>();
        foreach (var c in _codes)
        {
            switch (c.EditType)
            {
                case EditType.REPLACE:
                    for (var j = 0; j < c.SourceEnd - c.SourceBegin; j++)
                        ops.Add(new EditOp { EditType = EditType.REPLACE, SourcePos = c.SourceBegin + j, DestPos = c.DestBegin + j });
                    break;
                case EditType.INSERT:
                    for (var j = 0; j < c.DestEnd - c.DestBegin; j++)
                        ops.Add(new EditOp { EditType = EditType.INSERT, SourcePos = c.SourceBegin, DestPos = c.DestBegin + j });
                    break;
                case EditType.DELETE:
                    for (var j = 0; j < c.SourceEnd - c.SourceBegin; j++)
                        ops.Add(new EditOp { EditType = EditType.DELETE, SourcePos = c.SourceBegin + j, DestPos = c.DestBegin });
                    break;
            }
        }

        return new EditOps(ops, SourceLength, DestLength);
    }

    /// <summary>The unchanged blocks, ending with a zero-length block at (SourceLength, DestLength).</summary>
    public List<MatchingBlock> AsMatchingBlocks()
    {
        var blocks = new List<MatchingBlock>();
        foreach (var c in _codes)
        {
            if (c.EditType is EditType.EQUAL or EditType.KEEP)
            {
                var length = Math.Min(c.SourceEnd - c.SourceBegin, c.DestEnd - c.DestBegin);
                if (length > 0)
                {
                    blocks.Add(new MatchingBlock { SourcePos = c.SourceBegin, DestPos = c.DestBegin, Length = length });
                }
            }
        }

        blocks.Add(new MatchingBlock { SourcePos = SourceLength, DestPos = DestLength, Length = 0 });
        return blocks;
    }

    /// <summary>Converts single-character operations into ranges.</summary>
    public static OpCodes FromEditOps(EditOps editOps) =>
        (editOps ?? throw new ArgumentNullException(nameof(editOps))).AsOpCodes();

    /// <summary>A copy of the opcodes as a list.</summary>
    public static implicit operator List<OpCode>(OpCodes codes) => codes?._codes.Select(Copy).ToList();

    /// <inheritdoc/>
    public IEnumerator<OpCode> GetEnumerator() => ((IEnumerable<OpCode>)_codes).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _codes.GetEnumerator();

    /// <inheritdoc/>
    public bool Equals(OpCodes other) =>
        other != null && SourceLength == other.SourceLength && DestLength == other.DestLength &&
        _codes.Length == other._codes.Length &&
        _codes.Zip(other._codes, (a, b) =>
            a.EditType == b.EditType && a.SourceBegin == b.SourceBegin && a.SourceEnd == b.SourceEnd &&
            a.DestBegin == b.DestBegin && a.DestEnd == b.DestEnd).All(x => x);

    /// <inheritdoc/>
    public override bool Equals(object obj) => Equals(obj as OpCodes);

    /// <inheritdoc/>
    public override int GetHashCode() => unchecked((SourceLength * 397) ^ (DestLength * 31) ^ _codes.Length);

    /// <inheritdoc/>
    public override string ToString() =>
        $"OpCodes([{string.Join(", ", _codes.Select(o => o.ToString()))}], SourceLength={SourceLength}, DestLength={DestLength})";

    private static OpCode Copy(OpCode c) => new()
    {
        EditType = c.EditType,
        SourceBegin = c.SourceBegin,
        SourceEnd = c.SourceEnd,
        DestBegin = c.DestBegin,
        DestEnd = c.DestEnd,
    };
}
