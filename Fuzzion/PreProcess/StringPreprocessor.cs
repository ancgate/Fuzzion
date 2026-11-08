using System;

namespace Fuzzion.PreProcess;

/// <summary>
/// Ready-made string preprocessors (cleanup applied to both strings before they are compared).
/// </summary>
public static partial class StringPreprocessor
{
    /// <summary>
    /// The default cleanup: letters and digits are lowercased (invariant culture), every other
    /// UTF-16 character becomes a space, and leading and trailing spaces are trimmed. Inner runs
    /// of spaces are kept, not collapsed. <c>null</c> or white space gives an empty string.
    /// See <see cref="FullUnicode"/> for a version that handles characters outside the Basic
    /// Multilingual Plane and matches RapidFuzz's <c>utils.default_process</c>.
    /// </summary>
    public static Func<string, string> Full { get; } = Default;

    /// <summary>Returns the string unchanged.</summary>
    public static Func<string, string> None { get; } = static s => s;

    private static string Default(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var result = new char[input.Length].AsSpan();

        for (var i = 0; i < input.Length; i++)
        {
            var c = input[i];
            result[i] = char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ';
        }

        return result.Trim().ToString();
    }
}
