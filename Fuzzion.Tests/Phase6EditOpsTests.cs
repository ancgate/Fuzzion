using System;
using System.Linq;
using Fuzzion.Edits;
using Fuzzion.Tests.Support;
using Xunit;

namespace Fuzzion.Tests;

/// <summary>Plan items #28-#30. Expected values are from Python rapidfuzz 3.14.6.</summary>
public class Phase6EditOpsTests
{
    private static (EditType, int, int)[] Ops(EditOps ops) =>
        ops.Select(o => (o.EditType, o.SourcePos, o.DestPos)).ToArray();

    private static (EditType, int, int, int, int)[] Codes(OpCodes codes) =>
        codes.Select(c => (c.EditType, c.SourceBegin, c.SourceEnd, c.DestBegin, c.DestEnd)).ToArray();

    // ---- #28 EditOps and OpCodes -----------------------------------------------------

    [Fact]
    public void LevenshteinEditOpsSpamPark()
    {
        var ops = Levenshtein.GetEditOps("spam", "park");

        Assert.Equal([(EditType.DELETE, 0, 0), (EditType.INSERT, 3, 2), (EditType.REPLACE, 3, 3)], Ops(ops));
        Assert.Equal(4, ops.SourceLength);
        Assert.Equal(4, ops.DestLength);
    }

    [Fact]
    public void InverseSpamPark()
    {
        var inverse = Levenshtein.GetEditOps("spam", "park").Inverse();
        Assert.Equal([(EditType.INSERT, 0, 0), (EditType.DELETE, 2, 3), (EditType.REPLACE, 3, 3)], Ops(inverse));
    }

    [Fact]
    public void ApplySpamPark()
    {
        Assert.Equal("park", Levenshtein.GetEditOps("spam", "park").Apply("spam", "park"));
        Assert.Equal("spam", Levenshtein.GetEditOps("spam", "park").Inverse().Apply("park", "spam"));
    }

    [Fact]
    public void LevenshteinOpCodesSpamPark()
    {
        var codes = Levenshtein.GetOpCodes("spam", "park");
        Assert.Equal(
            [(EditType.DELETE, 0, 1, 0, 0), (EditType.EQUAL, 1, 3, 0, 2), (EditType.INSERT, 3, 3, 2, 3), (EditType.REPLACE, 3, 4, 3, 4)],
            Codes(codes));
        Assert.Equal("park", codes.Apply("spam", "park"));
    }

    [Fact]
    public void OpCodesInverseSpamPark()
    {
        Assert.Equal(
            [(EditType.INSERT, 0, 0, 0, 1), (EditType.EQUAL, 0, 2, 1, 3), (EditType.DELETE, 2, 3, 3, 3), (EditType.REPLACE, 3, 4, 3, 4)],
            Codes(Levenshtein.GetOpCodes("spam", "park").Inverse()));
    }

    [Fact]
    public void RemoveSubsequenceShiftsPositions()
    {
        var ops = Levenshtein.GetEditOps("spam", "park");
        var insertOnly = new EditOps([ops[1]], 4, 4);

        Assert.Equal([(EditType.DELETE, 0, 0), (EditType.REPLACE, 4, 3)], Ops(ops.RemoveSubsequence(insertOnly)));
    }

    [Fact]
    public void RemoveSubsequenceRejectsForeignOperations()
    {
        var ops = Levenshtein.GetEditOps("spam", "park");
        var foreign = new EditOps([new EditOp { EditType = EditType.INSERT, SourcePos = 9, DestPos = 9 }], 4, 4);
        Assert.Throws<ArgumentException>(() => ops.RemoveSubsequence(foreign));
    }

    [Fact]
    public void MatchingBlocksSpamPark()
    {
        var blocks = Levenshtein.GetEditOps("spam", "park").AsMatchingBlocks();
        Assert.Equal([(1, 0, 2), (4, 4, 0)], blocks.Select(b => (b.SourcePos, b.DestPos, b.Length)));
        Assert.Equal([(1, 0, 2), (4, 4, 0)], Levenshtein.GetOpCodes("spam", "park").AsMatchingBlocks().Select(b => (b.SourcePos, b.DestPos, b.Length)));
    }

    [Fact]
    public void ApplyAlwaysGivesDest()
    {
        foreach (var (a, b) in RandomPairs(1000))
        {
            var ops = Levenshtein.GetEditOps(a, b);
            Assert.Equal(b, ops.Apply(a, b));
            Assert.Equal(b, ops.AsOpCodes().Apply(a, b));
            Assert.Equal(a, ops.Inverse().Apply(b, a));
            Assert.Equal(Levenshtein.Distance(a, b), ops.Count);
        }
    }

    [Fact]
    public void InverseTwiceIsOriginal()
    {
        foreach (var (a, b) in RandomPairs(500))
        {
            var ops = Levenshtein.GetEditOps(a, b);
            Assert.Equal(ops, ops.Inverse().Inverse());
            Assert.Equal(ops.AsOpCodes(), ops.AsOpCodes().Inverse().Inverse());
        }
    }

    [Fact]
    public void OpCodesRoundTrip()
    {
        foreach (var (a, b) in RandomPairs(500))
        {
            var ops = Levenshtein.GetEditOps(a, b);
            Assert.Equal(ops, EditOps.FromOpCodes(ops.AsOpCodes()));
            Assert.Equal(ops.AsOpCodes(), OpCodes.FromEditOps(ops));
        }
    }

    [Fact]
    public void OpCodesCoverBothStringsEndToEnd()
    {
        foreach (var (a, b) in RandomPairs(500))
        {
            var codes = Levenshtein.GetOpCodes(a, b).ToArray();
            if (codes.Length == 0)
            {
                Assert.True(a.Length == 0 && b.Length == 0);
                continue;
            }

            Assert.Equal(0, codes[0].SourceBegin);
            Assert.Equal(0, codes[0].DestBegin);
            Assert.Equal(a.Length, codes[^1].SourceEnd);
            Assert.Equal(b.Length, codes[^1].DestEnd);
            for (var i = 1; i < codes.Length; i++)
            {
                Assert.Equal(codes[i - 1].SourceEnd, codes[i].SourceBegin);
                Assert.Equal(codes[i - 1].DestEnd, codes[i].DestBegin);
            }
        }
    }

    [Fact]
    public void ConvertsToArrayForOlderCode()
    {
        EditOp[] array = Levenshtein.GetEditOps("kitten", "sitting");
        Assert.Equal(3, array.Length);
        Assert.Equal(3, Levenshtein.GetEditOps("kitten", "sitting").Length);
    }

    [Fact]
    public void IdenticalStringsHaveNoEditOps()
    {
        var ops = Levenshtein.GetEditOps("same", "same");
        Assert.Empty(ops);
        Assert.Equal([(EditType.EQUAL, 0, 4, 0, 4)], Codes(ops.AsOpCodes()));
        Assert.Empty(Levenshtein.GetOpCodes("", ""));
    }

    // ---- #29 Indel edit operations ---------------------------------------------------

    [Fact]
    public void IndelEditOpsSpamPark()
    {
        Assert.Equal(
            [(EditType.DELETE, 0, 0), (EditType.INSERT, 3, 2), (EditType.INSERT, 3, 3), (EditType.DELETE, 3, 4)],
            Ops(Indel.GetEditOps("spam", "park")));
        Assert.Equal(
            [(EditType.DELETE, 0, 1, 0, 0), (EditType.EQUAL, 1, 3, 0, 2), (EditType.INSERT, 3, 3, 2, 4), (EditType.DELETE, 3, 4, 4, 4)],
            Codes(Indel.GetOpCodes("spam", "park")));
    }

    [Fact]
    public void IndelEditOpsContainNoReplaceAndCountEqualsDistance()
    {
        foreach (var (a, b) in RandomPairs(1000))
        {
            var ops = Indel.GetEditOps(a, b);
            Assert.DoesNotContain(ops, o => o.EditType == EditType.REPLACE);
            Assert.Equal(Indel.Distance(a.AsSpan(), b.AsSpan()), ops.Count);
            Assert.Equal(b, ops.Apply(a, b));
        }
    }

    // ---- #30 Levenshtein scoreCutoff and scoreHint -----------------------------------

    [Fact]
    public void CutoffKnownValues()
    {
        Assert.Equal(3, Levenshtein.Distance("kitten", "sitting", scoreCutoff: 2));
        Assert.Equal(3, Levenshtein.Distance("kitten", "sitting", scoreCutoff: 3));
        Assert.Equal(3, Levenshtein.Distance("kitten", "sitting", scoreCutoff: 10));
        Assert.Equal(1, Levenshtein.Distance("kitten", "sitting", scoreCutoff: 0));
        Assert.Equal(0, Levenshtein.Distance("same", "same", scoreCutoff: 0));
    }

    [Fact]
    public void CutoffResultIsMinOfDistanceAndCutoffPlusOne()
    {
        var random = new Random(30);
        foreach (var (a, b) in RandomPairs(400, maxLength: 1000).Concat(RandomPairs(1000, maxLength: 40)))
        {
            var distance = Levenshtein.Distance(a, b);
            foreach (var cutoff in new[] { 0, 1, 2, 5, distance - 1, distance, distance + 1, random.Next(0, 300) })
            {
                if (cutoff < 0) continue;
                Assert.Equal(Math.Min(distance, cutoff + 1), Levenshtein.Distance(a, b, scoreCutoff: cutoff));
                Assert.Equal(Math.Min(distance, cutoff + 1), Levenshtein.Distance<char>(a.AsSpan(), b.AsSpan(), scoreCutoff: cutoff));
            }
        }
    }

    [Fact]
    public void HintDoesNotChangeResult()
    {
        foreach (var (a, b) in RandomPairs(400, maxLength: 1000).Concat(RandomPairs(1000, maxLength: 40)))
        {
            var distance = Levenshtein.Distance(a, b);
            foreach (var hint in new[] { 0, 1, 5, 1000 })
            {
                Assert.Equal(distance, Levenshtein.Distance(a, b, scoreHint: hint));
                Assert.Equal(Math.Min(distance, 11), Levenshtein.Distance(a, b, scoreCutoff: 10, scoreHint: hint));
            }
        }
    }

    [Fact]
    public void BandedDistanceMatchesNaive()
    {
        foreach (var (a, b) in RandomPairs(2000, maxLength: 30))
        {
            var naive = Naive.Levenshtein(a, b);
            foreach (var k in new[] { 0, 1, 2, 3, 5, 8, 40 })
            {
                Assert.Equal(naive <= k ? naive : k + 1, Levenshtein.BandedDistance<char>(a.AsSpan(), b.AsSpan(), k));
            }
        }
    }

    [Fact]
    public void CutoffWithCustomCosts()
    {
        var full = Levenshtein.Distance("kitten", "sitting", 2, 3, 4);
        Assert.Equal(full, Levenshtein.Distance("kitten", "sitting", 2, 3, 4, scoreCutoff: full));
        Assert.Equal(full, Levenshtein.Distance("kitten", "sitting", 2, 3, 4, scoreCutoff: full - 1));
        Assert.Equal(5, Levenshtein.Distance("kitten", "sitting", 2, 3, 4, scoreCutoff: 4));
    }

    [Fact]
    public void NegativeCutoffOrHintThrows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Levenshtein.Distance("a", "b", scoreCutoff: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Levenshtein.Distance("a", "b", scoreHint: -1));
    }

    // ---- Helpers ----------------------------------------------------------------------

    private static (string A, string B)[] RandomPairs(int count, int maxLength = 30)
    {
        var random = new Random(count + maxLength);
        return Enumerable.Range(0, count).Select(i =>
        {
            var a = Letters(random, random.Next(0, maxLength + 1));
            var b = i % 2 == 0 ? Letters(random, random.Next(0, maxLength + 1)) : Mutate(random, a);
            return (a, b);
        }).ToArray();
    }

    private static string Letters(Random random, int length) =>
        new(Enumerable.Range(0, length).Select(_ => "abcd"[random.Next(4)]).ToArray());

    private static string Mutate(Random random, string s)
    {
        var chars = s.ToList();
        for (var i = 0; i < Math.Max(1, chars.Count / 10); i++)
        {
            switch (random.Next(3))
            {
                case 0: chars.Insert(random.Next(chars.Count + 1), "abcd"[random.Next(4)]); break;
                case 1 when chars.Count > 0: chars.RemoveAt(random.Next(chars.Count)); break;
                default: if (chars.Count > 0) chars[random.Next(chars.Count)] = "abcd"[random.Next(4)]; break;
            }
        }

        return new string(chars.ToArray());
    }
}
