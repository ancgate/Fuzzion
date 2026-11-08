using Fuzzion;
using Fuzzion.Edits;

namespace FuzzionExamples.Advanced;

/// <summary>
/// Not just how many edits, but which ones. <c>GetEditOps</c> lists single-character
/// edits; <c>GetOpCodes</c> groups them into ranges (like Python's difflib). Both can be
/// applied, inverted and converted into each other, with the same results as RapidFuzz.
/// </summary>
public static class EditOperations
{
    public static void Run()
    {
        const string source = "spam";
        const string target = "park";

        Show.Heading($"Levenshtein.GetEditOps(\"{source}\", \"{target}\")");
        EditOps ops = Levenshtein.GetEditOps(source, target);
        foreach (EditOp op in ops)
        {
            Console.WriteLine($"   {op.EditType,-8} source[{op.SourcePos}]  target[{op.DestPos}]");
        }

        Console.WriteLine($"   {ops.Count} edits = Levenshtein.Distance {Levenshtein.Distance(source, target)}");

        Show.Heading("GetOpCodes: the same edits as ranges, including the unchanged parts");
        OpCodes codes = Levenshtein.GetOpCodes(source, target);
        foreach (OpCode code in codes)
        {
            var from = source.Substring(code.SourceBegin, code.SourceEnd - code.SourceBegin);
            var to = target.Substring(code.DestBegin, code.DestEnd - code.DestBegin);
            Console.WriteLine($"   {code.EditType,-8} \"{from}\" -> \"{to}\"");
        }

        Show.Heading("Apply and Inverse");
        Console.WriteLine($"   ops.Apply(source, target):            \"{ops.Apply(source, target)}\"");
        Console.WriteLine($"   ops.Inverse().Apply(target, source):  \"{ops.Inverse().Apply(target, source)}\"");
        Console.WriteLine($"   codes.Apply(source, target):          \"{codes.Apply(source, target)}\"");

        Show.Heading("Converting between forms");
        Console.WriteLine($"   OpCodes -> EditOps equal to the original: {codes.AsEditOps().Equals(ops)}");
        Console.WriteLine($"   EditOps -> OpCodes equal to the original: {OpCodes.FromEditOps(ops).Equals(codes)}");
        foreach (var block in ops.AsMatchingBlocks().Where(b => b.Length > 0))
        {
            Console.WriteLine($"   matching block: \"{source.Substring(block.SourcePos, block.Length)}\" at source[{block.SourcePos}], target[{block.DestPos}]");
        }

        Show.Heading("Indel edits: only inserts and deletes (a change becomes delete + insert)");
        foreach (EditOp op in Indel.GetEditOps(source, target))
        {
            Console.WriteLine($"   {op.EditType,-8} source[{op.SourcePos}]  target[{op.DestPos}]");
        }

        Show.Heading("Turning edits into a readable change log (a swapped pair is one insert + one delete)");
        const string before = "Jonh Smiht, 42 Main Stret";
        const string after = "John Smith, 42 Main Street";
        foreach (OpCode code in Levenshtein.GetOpCodes(before, after).Where(c => c.EditType != EditType.EQUAL))
        {
            var from = before.Substring(code.SourceBegin, code.SourceEnd - code.SourceBegin);
            var to = after.Substring(code.DestBegin, code.DestEnd - code.DestBegin);
            Console.WriteLine(code.EditType switch
            {
                EditType.INSERT => $"   at {code.SourceBegin}: inserted \"{to}\"",
                EditType.DELETE => $"   at {code.SourceBegin}: deleted \"{from}\"",
                _ => $"   at {code.SourceBegin}: \"{from}\" became \"{to}\"",
            });
        }
    }
}
