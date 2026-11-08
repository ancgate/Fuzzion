using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fuzzion.Tests.Support;
using Xunit;

namespace Fuzzion.Tests.Distance;

/// <summary>
/// The shared tests T1-T13 from docs/IMPLEMENTATION-PLAN.md section 4. Every distance
/// metric gets a small subclass; xUnit runs all of these tests once per subclass.
/// (T11, the oracle comparison, lives in Oracle/OracleTests.cs.)
/// </summary>
public abstract class DistanceMetricContract
{
    private const int Seed = 1234;

    // ---- What a subclass provides --------------------------------------------------

    protected abstract double Distance(string a, string b);
    protected abstract double Similarity(string a, string b);
    protected abstract double NormalizedDistance(string a, string b);
    protected abstract double NormalizedSimilarity(string a, string b);

    /// <summary>Value of <c>Similarity + Distance</c> for this pair.</summary>
    protected abstract double Maximum(string a, string b);

    /// <summary>The textbook value of <see cref="Distance"/> (see Support/Naive.cs).</summary>
    protected abstract double NaiveDistance(string a, string b);

    /// <summary>Distance via the generic <c>ReadOnlySpan&lt;T&gt;</c> overload with <c>int</c> elements.</summary>
    protected abstract double GenericDistance(int[] a, int[] b);

    /// <summary>Creates the metric's instance class for <paramref name="source"/>.</summary>
    protected abstract IDisposable CreateInstance(string source);

    /// <summary>Distance computed by an instance from <see cref="CreateInstance"/>.</summary>
    protected abstract double InstanceDistance(IDisposable instance, string value);

    protected virtual bool IsSymmetric => true;

    /// <summary>Tolerance for distance comparisons (0 for integer metrics).</summary>
    protected virtual double Tolerance => 0;

    // ---- T1-T13 ---------------------------------------------------------------------

    [Fact]
    public void T01_EmptyBoth()
    {
        Assert.Equal(0, Distance("", ""), 12);
        Assert.Equal(1, NormalizedSimilarity("", ""), 12);
        Assert.Equal(0, NormalizedDistance("", ""), 12);
    }

    [Theory]
    [InlineData("abc", "")]
    [InlineData("", "abc")]
    public void T02_EmptyOne(string a, string b)
    {
        Assert.Equal(Maximum(a, b), Distance(a, b), 12);
        Assert.Equal(0, NormalizedSimilarity(a, b), 12);
        Assert.Equal(1, NormalizedDistance(a, b), 12);
    }

    [Fact]
    public void T03_Identical()
    {
        var random = new Random(Seed);
        for (var i = 0; i < 20; i++)
        {
            var s = RandomString(random, random.Next(1, 200), "abcdefgh");
            Assert.Equal(0, Distance(s, s), 12);
            Assert.Equal(1, NormalizedSimilarity(s, s), 12);
        }
    }

    [Fact]
    public void T04_Symmetric()
    {
        if (!IsSymmetric)
        {
            return;
        }

        foreach (var (a, b) in RandomPairs(500))
        {
            Assert.Equal(Distance(a, b), Distance(b, a), 12);
        }
    }

    [Fact]
    public void T05_NormalizedRange()
    {
        foreach (var (a, b) in RandomPairs(500))
        {
            Assert.InRange(NormalizedDistance(a, b), 0.0, 1.0);
            Assert.InRange(NormalizedSimilarity(a, b), 0.0, 1.0);
        }
    }

    [Fact]
    public void T06_Complement()
    {
        foreach (var (a, b) in RandomPairs(500))
        {
            Assert.Equal(1.0, NormalizedSimilarity(a, b) + NormalizedDistance(a, b), 12);
            Assert.Equal(Maximum(a, b), Similarity(a, b) + Distance(a, b), 9);
        }
    }

    [Fact]
    public void T07_StaticEqualsInstance()
    {
        var random = new Random(Seed + 7);
        for (var i = 0; i < 50; i++)
        {
            var source = RandomString(random, random.Next(0, 140), "abcd");
            using var instance = CreateInstance(source);
            for (var j = 0; j < 10; j++)
            {
                var value = RandomString(random, random.Next(0, 140), "abcd");
                Assert.Equal(Distance(source, value), InstanceDistance(instance, value), 12);
            }
        }
    }

    [Fact]
    public void T08_StringEqualsGeneric()
    {
        foreach (var (a, b) in RandomPairs(500))
        {
            Assert.Equal(Distance(a, b), GenericDistance(ToInts(a), ToInts(b)), 12);
        }
    }

    [Fact]
    public void T09_BlockBoundaries()
    {
        int[] lengths = [0, 1, 63, 64, 65, 127, 128, 129, 300, 1000];
        var random = new Random(Seed + 9);
        foreach (var lengthA in lengths)
        {
            foreach (var lengthB in lengths.Where(l => Math.Abs(l - lengthA) <= 70))
            {
                var a = RandomString(random, lengthA, "abcd");
                var b = random.Next(2) == 0 ? RandomString(random, lengthB, "abcd") : Mutate(random, a, lengthB);
                Assert.True(
                    Math.Abs(NaiveDistance(a, b) - Distance(a, b)) <= Tolerance,
                    $"lengths {a.Length}/{b.Length}: naive {NaiveDistance(a, b)}, fast {Distance(a, b)}");
            }
        }
    }

    [Fact]
    public void T10_NaiveReference()
    {
        foreach (var (a, b) in RandomPairs(2000, maxLength: 20).Concat(RandomPairs(200, maxLength: 150, seed: Seed + 10)))
        {
            Assert.True(
                Math.Abs(NaiveDistance(a, b) - Distance(a, b)) <= Tolerance,
                $"a=\"{a}\" b=\"{b}\": naive {NaiveDistance(a, b)}, fast {Distance(a, b)}");
        }
    }

    [Fact]
    public void T12_ThreadSafety()
    {
        var pairs = RandomPairs(10_000, maxLength: 30).ToArray();
        var expected = pairs.Select(p => Distance(p.A, p.B)).ToArray();
        var actual = new double[pairs.Length];

        Parallel.For(0, pairs.Length, i => actual[i] = Distance(pairs[i].A, pairs[i].B));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void T13_DisposeTwice()
    {
        var instance = CreateInstance("some source");
        instance.Dispose();
        instance.Dispose();
    }

    // ---- Helpers ----------------------------------------------------------------------

    protected static (string A, string B)[] RandomPairs(int count, int maxLength = 80, int seed = Seed, string alphabet = "abcd")
    {
        var random = new Random(seed);
        var pairs = new (string, string)[count];
        for (var i = 0; i < count; i++)
        {
            var a = RandomString(random, random.Next(0, maxLength + 1), alphabet);
            var b = i % 2 == 0
                ? RandomString(random, random.Next(0, maxLength + 1), alphabet)
                : Mutate(random, a, Math.Max(0, a.Length + random.Next(-3, 4)));
            pairs[i] = (a, b);
        }

        return pairs;
    }

    protected static string RandomString(Random random, int length, string alphabet)
    {
        var sb = new StringBuilder(length);
        for (var i = 0; i < length; i++)
        {
            sb.Append(alphabet[random.Next(alphabet.Length)]);
        }

        return sb.ToString();
    }

    /// <summary>A close variant of <paramref name="s"/> with the given length: a few changes and swaps.</summary>
    private static string Mutate(Random random, string s, int length)
    {
        var chars = s.ToList();
        while (chars.Count < length) chars.Insert(random.Next(chars.Count + 1), "abcd"[random.Next(4)]);
        while (chars.Count > length) chars.RemoveAt(random.Next(chars.Count));
        for (var i = 0; i < Math.Max(1, chars.Count / 8) && chars.Count > 1; i++)
        {
            var p = random.Next(chars.Count - 1);
            if (random.Next(2) == 0)
            {
                (chars[p], chars[p + 1]) = (chars[p + 1], chars[p]);
            }
            else
            {
                chars[p] = "abcd"[random.Next(4)];
            }
        }

        return new string(chars.ToArray());
    }

    protected static int[] ToInts(string s) => s.Select(c => (int)c).ToArray();
}
