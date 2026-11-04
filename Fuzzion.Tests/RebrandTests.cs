using System;
using System.Linq;
using Xunit;

namespace Fuzzion.Tests;

/// <summary>
/// Guards the rename from Raffinert.FuzzySharp to Fuzzion: the assembly and namespaces
/// changed, but the public surface and the scores must not.
/// </summary>
public class RebrandTests
{
    // Number of exported types in Raffinert.FuzzySharp v6.0.1, measured before the rename.
    private const int ExportedTypeCountBeforeRename = 70;

    [Fact]
    public void AssemblyNameIsFuzzion()
    {
        Assert.Equal("Fuzzion", typeof(Fuzz).Assembly.GetName().Name);
    }

    [Fact]
    public void NoPublicTypeInOldNamespace()
    {
        var oldNamespaceTypes = typeof(Fuzz).Assembly
            .GetExportedTypes()
            .Where(t => t.Namespace != null && t.Namespace.StartsWith("Raffinert", StringComparison.Ordinal))
            .Select(t => t.FullName)
            .ToArray();

        Assert.Empty(oldNamespaceTypes);
    }

    [Fact]
    public void EveryPublicTypeIsInFuzzionNamespace()
    {
        var outside = typeof(Fuzz).Assembly
            .GetExportedTypes()
            .Where(t => t.Namespace == null
                        || !(t.Namespace == "Fuzzion" || t.Namespace.StartsWith("Fuzzion.", StringComparison.Ordinal)))
            .Select(t => t.FullName)
            .ToArray();

        Assert.Empty(outside);
    }

    [Fact]
    public void PublicSurfaceUnchanged()
    {
        Assert.Equal(ExportedTypeCountBeforeRename, typeof(Fuzz).Assembly.GetExportedTypes().Length);
    }

    [Fact]
    public void BaselineRatioUnchanged()
    {
        // Same value as Python rapidfuzz 3.14.6: fuzz.ratio("this is a test", "this is a test!")
        Assert.Equal(96.551724, Fuzz.Ratio("this is a test", "this is a test!"), 6);
    }

    [Fact]
    public void BaselineTokenSortRatioUnchanged()
    {
        Assert.Equal(100, Fuzz.TokenSortRatio("fuzzy wuzzy was a bear", "wuzzy fuzzy was a bear"), 6);
    }
}
