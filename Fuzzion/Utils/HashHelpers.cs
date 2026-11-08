namespace Fuzzion.Utils;

internal static class HashHelpers
{
    internal static int PowerOf2(int v)
    {
        if ((v & (v - 1)) == 0) return v;
        int i = 2;
        while (i < v) i <<= 1;
        return i;
    }

    // must never be written to
    internal static readonly int[] SizeOneIntArray = new int[1];
}