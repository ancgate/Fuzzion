using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Fuzzion.PreProcess;

// Unicode-aware cleanup (plan items #31 and #32).
public static partial class StringPreprocessor
{
    private static readonly Dictionary<char, string> NonDecomposingLetters = new()
    {
        ['ß'] = "ss", ['ẞ'] = "SS",
        ['æ'] = "ae", ['Æ'] = "Ae",
        ['œ'] = "oe", ['Œ'] = "Oe",
        ['ø'] = "o", ['Ø'] = "O",
        ['ł'] = "l", ['Ł'] = "L",
        ['đ'] = "d", ['Đ'] = "D",
        ['þ'] = "th", ['Þ'] = "Th",
        ['ı'] = "i",
    };

    /// <summary>
    /// Like <see cref="Full"/>, but works on whole characters and matches RapidFuzz's
    /// <c>utils.default_process</c>: letters and all numbers (including <c>²</c>, <c>½</c>,
    /// <c>Ⅻ</c>) are kept and lowercased, characters outside the Basic Multilingual Plane
    /// (emoji, rare CJK) are handled as one character instead of two halves, everything
    /// else (punctuation, <c>_</c>, combining accents, emoji) becomes a space, and spaces
    /// are trimmed. On .NET Framework, letters outside the BMP are kept but not lowercased.
    /// </summary>
    public static Func<string, string> FullUnicode { get; } = FullUnicodeImpl;

    /// <summary>
    /// Removes accents and other combining marks: <c>"Crème Brûlée"</c> → <c>"Creme Brulee"</c>.
    /// Text is decomposed (Unicode NFD), the marks are dropped and the rest is recomposed
    /// (NFC). Letters that have no decomposition are spelled out:
    /// <c>ß→ss</c>, <c>æ→ae</c>, <c>œ→oe</c>, <c>ø→o</c>, <c>ł→l</c>, <c>đ→d</c>, <c>þ→th</c>
    /// (and their capitals). Case and everything else are left unchanged.
    /// </summary>
    public static Func<string, string> RemoveDiacritics { get; } = RemoveDiacriticsImpl;

    /// <summary>
    /// Chains cleanup steps, applied left to right; for example
    /// <c>Compose(RemoveDiacritics, Full)</c> removes accents, then lowercases and strips punctuation.
    /// </summary>
    public static Func<string, string> Compose(params Func<string, string>[] steps)
    {
        if (steps == null) throw new ArgumentNullException(nameof(steps));
        foreach (var step in steps)
        {
            if (step == null) throw new ArgumentException("Steps must not be null.", nameof(steps));
        }

        var copy = (Func<string, string>[])steps.Clone();
        return input =>
        {
            foreach (var step in copy)
            {
                input = step(input);
            }

            return input;
        };
    }

    private static string FullUnicodeImpl(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        var result = new StringBuilder(input.Length);
        for (var i = 0; i < input.Length; i++)
        {
            var isPair = char.IsSurrogatePair(input, i);
            if (IsAlphanumeric(CharUnicodeInfo.GetUnicodeCategory(input, i)))
            {
                if (isPair)
                {
                    AppendLowerPair(result, input, i);
                }
                else
                {
                    // .NET's invariant casing leaves U+0130 (İ) unchanged; Unicode's simple
                    // lowercase mapping, which RapidFuzz uses, gives "i".
                    result.Append(input[i] == '\u0130' ? 'i' : char.ToLowerInvariant(input[i]));
                }
            }
            else
            {
                result.Append(' ');
            }

            if (isPair)
            {
                i++;
            }
        }

        return result.ToString().Trim(' ');
    }

    private static void AppendLowerPair(StringBuilder result, string input, int index)
    {
#if NETCOREAPP3_0_OR_GREATER
        var rune = System.Text.Rune.ToLowerInvariant(new System.Text.Rune(input[index], input[index + 1]));
        Span<char> buffer = stackalloc char[2];
        var written = rune.EncodeToUtf16(buffer);
        result.Append(buffer.Slice(0, written).ToString());
#else
        result.Append(input, index, 2);
#endif
    }

    /// <summary>Python's <c>str.isalnum</c>: letters plus decimal, letter and other numbers.</summary>
    private static bool IsAlphanumeric(UnicodeCategory category) => category switch
    {
        UnicodeCategory.UppercaseLetter or UnicodeCategory.LowercaseLetter or UnicodeCategory.TitlecaseLetter
            or UnicodeCategory.ModifierLetter or UnicodeCategory.OtherLetter => true,
        UnicodeCategory.DecimalDigitNumber or UnicodeCategory.LetterNumber or UnicodeCategory.OtherNumber => true,
        _ => false,
    };

    private static string RemoveDiacriticsImpl(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input ?? string.Empty;
        }

        var decomposed = input.Normalize(NormalizationForm.FormD);
        var result = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category is UnicodeCategory.NonSpacingMark or UnicodeCategory.EnclosingMark)
            {
                continue;
            }

            if (NonDecomposingLetters.TryGetValue(c, out var replacement))
            {
                result.Append(replacement);
            }
            else
            {
                result.Append(c);
            }
        }

        return result.ToString().Normalize(NormalizationForm.FormC);
    }
}
