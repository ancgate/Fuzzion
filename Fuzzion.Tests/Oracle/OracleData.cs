using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Fuzzion.Tests.Oracle;

/// <summary>
/// Loads the reference results written by <c>tools/oracle/generate.py</c> (Python RapidFuzz,
/// TheFuzz and difflib) from <c>Oracle/data</c> in the test output folder.
/// </summary>
public static class OracleData
{
    private static readonly Lazy<IReadOnlyList<OracleRow>> CharPairsLazy = new(() => Load("char-pairs"));
    private static readonly Lazy<IReadOnlyList<OracleRow>> SentencePairsLazy = new(() => Load("sentence-pairs"));

    /// <summary>Random and edge-case character strings, with distance-metric results.</summary>
    public static IReadOnlyList<OracleRow> CharPairs => CharPairsLazy.Value;

    /// <summary>Random and edge-case word sentences, with <c>fuzz.*</c> scorer results.</summary>
    public static IReadOnlyList<OracleRow> SentencePairs => SentencePairsLazy.Value;

    public static string DataDirectory => Path.Combine(AppContext.BaseDirectory, "Oracle", "data");

    public static string FindFile(string name)
    {
        var plain = Path.Combine(DataDirectory, name + ".json");
        if (File.Exists(plain))
        {
            return plain;
        }

        var gz = plain + ".gz";
        if (File.Exists(gz))
        {
            return gz;
        }

        throw new FileNotFoundException(
            $"Oracle fixture '{name}' not found in {DataDirectory}. Run tools/oracle/generate.py.");
    }

    private static IReadOnlyList<OracleRow> Load(string name)
    {
        var path = FindFile(name);
        using var file = File.OpenRead(path);
        using Stream stream = path.EndsWith(".gz", StringComparison.Ordinal)
            ? new GZipStream(file, CompressionMode.Decompress)
            : file;
        using var reader = new StreamReader(stream, Encoding.UTF8);
        using var document = JsonDocument.Parse(reader.ReadToEnd());

        return document.RootElement
            .GetProperty("rows")
            .EnumerateArray()
            .Select(row => new OracleRow(row.Clone()))
            .ToArray();
    }
}

/// <summary>One input pair and the results every reference metric produced for it.</summary>
public sealed class OracleRow(JsonElement element)
{
    public string Group => element.GetProperty("group").GetString();
    public string A => element.GetProperty("a").GetString();
    public string B => element.GetProperty("b").GetString();

    public bool Has(string metric) => element.TryGetProperty(metric, out _);

    /// <summary>The distance / similarity / normalized values of a distance metric.</summary>
    public OracleFour Four(string metric)
    {
        var m = element.GetProperty(metric);
        return new OracleFour(
            m.GetProperty("d").GetDouble(),
            m.GetProperty("s").GetDouble(),
            m.GetProperty("nd").GetDouble(),
            m.GetProperty("ns").GetDouble());
    }

    public double Number(string metric) => element.GetProperty(metric).GetDouble();

    /// <summary>A value inside a nested object, for example <c>Nested("set_q2", "jaccard")</c>.</summary>
    public double Nested(string metric, string field) => element.GetProperty(metric).GetProperty(field).GetDouble();

    public double[] Numbers(string metric) =>
        element.GetProperty(metric).EnumerateArray().Select(x => x.GetDouble()).ToArray();

    /// <summary>Readable form used in failure messages, with long strings shortened.</summary>
    public override string ToString() => $"[{Group}] a={Show(A)} b={Show(B)}";

    private static string Show(string s) =>
        s.Length <= 40 ? $"\"{s}\"" : $"\"{s.Substring(0, 37)}...\" (len {s.Length})";
}

public readonly record struct OracleFour(double Distance, double Similarity, double NormalizedDistance, double NormalizedSimilarity);
