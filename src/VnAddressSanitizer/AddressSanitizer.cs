using System.Text;
using System.Text.RegularExpressions;

namespace VnAddressSanitizer;

/// <summary>
/// Sanitizes raw Vietnamese address strings for geocoding.
/// Removes phone numbers, delivery instructions, landmark notes, 
/// duplicate admin units, and other noise commonly found in 
/// ERP/POS/e-commerce address fields.
/// </summary>
public static class AddressSanitizer
{
    private static readonly SanitizeOptions DefaultOptions = new();

    /// <summary>
    /// Sanitizes a raw Vietnamese address for geocoding using default options.
    /// </summary>
    /// <param name="input">Raw address string, possibly containing noise.</param>
    /// <returns>Cleaned address optimized for geocoding, or empty string for null/blank input.</returns>
    public static string Sanitize(string? input)
    {
        return Sanitize(input, DefaultOptions);
    }

    /// <summary>
    /// Sanitizes a raw Vietnamese address for geocoding using custom options.
    /// </summary>
    /// <param name="input">Raw address string, possibly containing noise.</param>
    /// <param name="options">Options controlling which sanitization stages run.</param>
    /// <returns>Cleaned address optimized for geocoding, or empty string for null/blank input.</returns>
    public static string Sanitize(string? input, SanitizeOptions options)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var result = input;

        // Stage 1: Normalization
        result = Normalize(result);

        // Stage 1.5: Abbreviation Expansion
        if (options.ExpandAbbreviations)
            result = ExpandAbbreviations(result);

        // Stage 2: Parenthetical Content Removal
        if (options.RemoveParentheses)
            result = SanitizePatterns.Parentheses.Replace(result, " ");

        // Stage 3: Noise Removal
        result = RemoveNoise(result, options);

        // Stage 4: Admin Unit Deduplication
        if (options.DeduplicateAdminUnits)
            result = AdminUnitDeduplicator.Deduplicate(result);

        // Stage 5: Punctuation Cleanup
        result = CleanupPunctuation(result);

        // Safety: if result is empty after sanitization, return trimmed original
        if (string.IsNullOrWhiteSpace(result))
            return input.Trim();

        return result;
    }

    /// <summary>
    /// Stage 1: Unicode normalization, whitespace collapse, dash normalization.
    /// </summary>
    private static string Normalize(string input)
    {
        var result = input.Trim();
        // Unicode NFC normalization
        result = result.Normalize(NormalizationForm.FormC);
        // Normalize unicode dashes to ASCII hyphen
        result = SanitizePatterns.UnicodeDashes.Replace(result, "-");
        // Collapse multiple whitespace
        result = SanitizePatterns.MultipleWhitespace.Replace(result, " ");
        return result;
    }

    /// <summary>
    /// Stage 1.5: Expands common administrative abbreviations to improve geocoding matches.
    /// </summary>
    private static string ExpandAbbreviations(string input)
    {
        var result = input;
        result = SanitizePatterns.AbbrQuanNumber.Replace(result, "Quận $1");
        result = SanitizePatterns.AbbrPhuongNumber.Replace(result, "Phường $1");
        result = SanitizePatterns.AbbrThanhPho.Replace(result, "Thành phố ");
        result = SanitizePatterns.AbbrThiXa.Replace(result, "Thị xã ");
        result = SanitizePatterns.AbbrThiTran.Replace(result, "Thị trấn ");
        return result;
    }

    /// <summary>
    /// Stage 3: Sequential noise removal sub-stages.
    /// </summary>
    private static string RemoveNoise(string input, SanitizeOptions options)
    {
        var result = input;

        // 3a: Phone numbers
        if (options.RemovePhoneNumbers)
            result = SanitizePatterns.PhoneNumbers.Replace(result, " ");

        // 3b: Contact labels (after phone removal)
        if (options.RemovePhoneNumbers)
            result = SanitizePatterns.ContactLabels.Replace(result, " ");

        // 3c: Delivery/contact/order instructions
        if (options.RemoveInstructions)
            result = SanitizePatterns.Instructions.Replace(result, " ");

        // 3d: Direction/landmark notes
        if (options.RemoveDirectionNotes)
            result = SanitizePatterns.DirectionNotes.Replace(result, " ");

        // 3e: Postal codes and country name
        if (options.RemovePostalCodesAndCountry)
        {
            result = SanitizePatterns.PostalCodeWithCountry.Replace(result, " ");
            result = SanitizePatterns.TrailingCountryName.Replace(result, "");
            result = SanitizePatterns.StandalonePostalCode.Replace(result, " ");
        }

        // 3f: Standalone junk tokens
        result = SanitizePatterns.StandaloneHash.Replace(result, " ");
        result = SanitizePatterns.QuestionMarkJunk.Replace(result, " ");

        // 3g: Building/floor/unit info (optional)
        if (options.RemoveBuildingInfo)
            result = SanitizePatterns.BuildingInfo.Replace(result, " ");

        // 3h: Additional custom patterns
        if (options.AdditionalPatterns.Count > 0)
        {
            foreach (var pattern in options.AdditionalPatterns)
            {
                try
                {
                    var regex = new Regex(pattern,
                        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                    result = regex.Replace(result, " ");
                }
                catch (ArgumentException)
                {
                    // Skip invalid regex patterns silently
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Stage 5: Final punctuation cleanup.
    /// </summary>
    private static string CleanupPunctuation(string input)
    {
        var result = input;
        // Clean orphaned dashes before separators (e.g., "Quân -, Phường" → "Quân, Phường")
        result = SanitizePatterns.DashBeforeSeparator.Replace(result, "");
        // Collapse multiple commas
        result = SanitizePatterns.MultipleCommas.Replace(result, ", ");
        // Normalize spacing around separators
        result = SanitizePatterns.SpaceAroundSeparator.Replace(result, "$1 ");
        // Collapse multiple whitespace
        result = SanitizePatterns.MultipleWhitespace.Replace(result, " ");
        // Strip leading punctuation
        result = SanitizePatterns.LeadingPunctuation.Replace(result, "");
        // Strip trailing junk
        result = SanitizePatterns.TrailingJunk.Replace(result, "");
        return result.Trim();
    }
}
