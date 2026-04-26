namespace VnAddressSanitizer;

/// <summary>
/// Options to control which sanitization stages are applied.
/// All options default to geocoding-optimized behavior.
/// </summary>
public sealed class SanitizeOptions
{
    /// <summary>
    /// Removes content inside parentheses, e.g. "(gần chợ)", "(Khách Sạn ABC)".
    /// Default: true. May remove useful info like "(Hẻm 1539)" but is usually noise for geocoding.
    /// </summary>
    public bool RemoveParentheses { get; set; } = true;

    /// <summary>
    /// Removes Vietnamese phone numbers (0xxx, +84xxx, 84xxx with 9-11 digits).
    /// Will not remove short numbers that look like house numbers (e.g. "031", "101").
    /// </summary>
    public bool RemovePhoneNumbers { get; set; } = true;

    /// <summary>
    /// Removes delivery/contact/order instructions such as "gọi trước khi giao",
    /// "giao cho", "ship tới", "liên hệ", "mã đơn: ...", etc.
    /// </summary>
    public bool RemoveInstructions { get; set; } = true;

    /// <summary>
    /// Removes direction and landmark notes such as "gần coopmart",
    /// "đối diện nhà thờ", "cuối hẻm", "phía sau".
    /// </summary>
    public bool RemoveDirectionNotes { get; set; } = true;

    /// <summary>
    /// Removes Vietnamese postal codes (6-digit) and country name "Việt Nam" / "Viet Nam".
    /// </summary>
    public bool RemovePostalCodesAndCountry { get; set; } = true;

    /// <summary>
    /// Deduplicates administrative unit suffixes that ERP/POS systems often auto-append.
    /// For example: "..., Phường X, Quận Y, TP Z, Phường X, Quận Y, TP Z" → keeps one copy.
    /// </summary>
    public bool DeduplicateAdminUnits { get; set; } = true;

    /// <summary>
    /// Expands common abbreviations for better geocoding match rates.
    /// For example: "Q.1" → "Quận 1", "P. 2" → "Phường 2", "TP. HCM" → "Thành phố HCM".
    /// </summary>
    public bool ExpandAbbreviations { get; set; } = true;

    /// <summary>
    /// Removes building/floor/unit details such as "block A", "tower B",
    /// "lầu 2", "tầng trệt", "phòng B951". Default false because these may
    /// still be useful in delivery workflows, even though they are often noise
    /// for geocoding.
    /// </summary>
    public bool RemoveBuildingInfo { get; set; } = false;

    /// <summary>
    /// Additional case-insensitive regex patterns to remove after built-in rules.
    /// Each string is compiled as a regex with <see cref="System.Text.RegularExpressions.RegexOptions.IgnoreCase"/>
    /// and <see cref="System.Text.RegularExpressions.RegexOptions.CultureInvariant"/>.
    /// </summary>
    public IReadOnlyList<string> AdditionalPatterns { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Returns a default instance optimized for geocoding.
    /// </summary>
    public static SanitizeOptions Default => new();
}
