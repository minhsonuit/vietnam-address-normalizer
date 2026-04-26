using System.Globalization;
using System.Text;

namespace VnAddressSanitizer;

/// <summary>
/// Internal helpers for Vietnamese text processing.
/// </summary>
internal static class VietnameseTextHelper
{
    /// <summary>
    /// Removes Vietnamese diacritics for internal comparison purposes only.
    /// The output should NOT be used as the final sanitized result.
    /// Handles đ/Đ specially since Unicode decomposition does not cover them.
    /// </summary>
    /// <param name="input">Vietnamese text with diacritics.</param>
    /// <returns>Text with all diacritics removed, lowercase.</returns>
    public static string RemoveDiacritics(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Replace đ/Đ before decomposition since they don't decompose
        var sb = new StringBuilder(input.Length);
        foreach (var c in input)
        {
            switch (c)
            {
                case 'đ':
                    sb.Append('d');
                    break;
                case 'Đ':
                    sb.Append('D');
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }

        // Normalize to FormD to decompose combined characters
        var normalized = sb.ToString().Normalize(NormalizationForm.FormD);

        var result = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                result.Append(c);
            }
        }

        return result.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Removes Vietnamese diacritics and converts to lowercase for comparison.
    /// </summary>
    public static string NormalizeForComparison(string input)
    {
        return RemoveDiacritics(input).ToLowerInvariant();
    }
}
