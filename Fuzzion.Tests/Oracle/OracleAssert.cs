using System;
using System.Collections.Generic;
using System.Linq;
using Xunit.Sdk;

namespace Fuzzion.Tests.Oracle;

/// <summary>
/// Compares Fuzzion results with oracle rows and reports every mismatch at once,
/// instead of stopping at the first one.
/// </summary>
public sealed class OracleAssert
{
    /// <summary>Integer distances and similarities must match exactly.</summary>
    public const double Exact = 0;

    /// <summary>Normalized values in [0, 1].</summary>
    public const double Normalized = 1e-9;

    /// <summary>Fuzz scores in [0, 100].</summary>
    public const double Score = 1e-6;

    private const int MaxReported = 25;

    private readonly string _metric;
    private readonly List<string> _failures = new();
    private int _checked;

    public OracleAssert(string metric) => _metric = metric;

    public void Equal(OracleRow row, string what, double expected, double actual, double tolerance)
    {
        _checked++;
        if (Math.Abs(expected - actual) > tolerance || double.IsNaN(actual))
        {
            _failures.Add($"{what}: expected {expected:R}, got {actual:R} for {row}");
        }
    }

    public void Throws(OracleRow row, string what, Exception ex)
    {
        _checked++;
        _failures.Add($"{what}: threw {ex.GetType().Name} ({ex.Message}) for {row}");
    }

    /// <summary>Fails the test if any comparison did not match.</summary>
    public void Verify()
    {
        if (_checked == 0)
        {
            throw new XunitException($"{_metric}: no oracle values were checked.");
        }

        if (_failures.Count == 0)
        {
            return;
        }

        var shown = string.Join(Environment.NewLine, _failures.Take(MaxReported));
        var more = _failures.Count > MaxReported ? $"{Environment.NewLine}... and {_failures.Count - MaxReported} more" : "";
        throw new XunitException(
            $"{_metric}: {_failures.Count} of {_checked} oracle values differ from Python RapidFuzz:{Environment.NewLine}{shown}{more}");
    }
}
