using System.Text.RegularExpressions;

namespace VnAddressSanitizer;

/// <summary>
/// Pre-compiled regex patterns for address sanitization.
/// All patterns are static readonly to avoid recompilation per invocation.
/// </summary>
internal static class SanitizePatterns
{
    private const RegexOptions DefaultOptions =
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled;

    // ─── Stage 0: Abbreviation Expansion ─────────────────────────────────

    private const string RoomPrefixes = @"(?:block|tower|khu|tòa|toa|tòa\s*nhà|toa\s*nha|nhà|nha|tầng|tang|lầu|lau|phòng|phong|căn|can|căn\s*hộ|can\s*ho|ch|lô|lo|khu\s*nhà|khu\s*nha|khu\s*phố|khu\s*pho|kp)";
    private const string RoomLookbehind = @"(?<!\b" + RoomPrefixes + @"(?:\s+[A-Za-z0-9\-]+)?(?:[,.\-]\s*|\s+))";

    /// <summary>Expands 'Q' followed by number to 'Quận X' (e.g. Q1, Q.1)</summary>
    internal static readonly Regex AbbrQuanNumber = new(RoomLookbehind + @"\b(?:Q|q)[\.\s]*(\d+[A-Za-z]?)\b", DefaultOptions);

    /// <summary>Expands 'P' followed by number to 'Phường X' (e.g. P12, P. 12)</summary>
    internal static readonly Regex AbbrPhuongNumber = new(RoomLookbehind + @"\b(?:P|p)[\.\s]*(\d+[A-Za-z]?)\b", DefaultOptions);

    /// <summary>Expands 'TP' to 'Thành phố'</summary>
    internal static readonly Regex AbbrThanhPho = new(@"\b(?:TP|Tp|tp)[\.\s]+(?=\p{L})", DefaultOptions);

    /// <summary>Expands 'TX' to 'Thị xã'</summary>
    internal static readonly Regex AbbrThiXa = new(@"\b(?:TX|Tx|tx)[\.\s]+(?=\p{L})", DefaultOptions);

    /// <summary>Expands 'TT' to 'Thị trấn'</summary>
    internal static readonly Regex AbbrThiTran = new(@"\b(?:TT|Tt|tt)[\.\s]+(?=\p{L})", DefaultOptions);

    // ─── Stage 1: Normalization ───────────────────────────────────────────

    /// <summary>Collapses multiple whitespace characters into a single space.</summary>
    internal static readonly Regex MultipleWhitespace = new(@"\s{2,}", DefaultOptions);

    /// <summary>Normalizes various dash/hyphen Unicode characters to ASCII hyphen.</summary>
    internal static readonly Regex UnicodeDashes = new(@"[\u2010\u2011\u2012\u2013\u2014\u2015\u2212\uFE58\uFE63\uFF0D]", DefaultOptions);

    // ─── Stage 2: Parenthetical Content ──────────────────────────────────

    /// <summary>Removes content inside parentheses including the parens themselves.</summary>
    internal static readonly Regex Parentheses = new(@"\([^)]*\)", DefaultOptions);

    // ─── Stage 3a: Phone Numbers ─────────────────────────────────────────

    /// <summary>
    /// Matches Vietnamese phone numbers starting with 0, +84, or 84 followed by 8-10 digits.
    /// Uses lookbehind/lookahead to avoid eating house numbers like "031" or "101".
    /// </summary>
    internal static readonly Regex PhoneNumbers = new(
        @"(?<!\d)(?:\+?84|0)(?:[.\-\s]?\d){8,10}(?!\d)",
        DefaultOptions);

    // ─── Stage 3b: Contact Labels ────────────────────────────────────────

    /// <summary>
    /// Removes leftover contact labels after phone removal:
    /// phone:, tel:, sdt:, sđt:, dt:, đt:, điện thoại:, etc.
    /// </summary>
    internal static readonly Regex ContactLabels = new(
        @"\b(?:phone|tel|s[đd]t|s\.đ\.t|s\.d\.t|dt|đt|dien\s*thoai|điện\s*thoại)\s*[:：]?\s*,?\s*",
        DefaultOptions);

    // ─── Stage 3c: Delivery / Contact / Order Instructions ───────────────

    private const string NoiseBoundary = @"(?=[,.;-]|\b(?:ấp|ap|thôn|thon|xã|xa|phường|phuong|quận|quan|huyện|huyen|tỉnh|tinh|tp)\b|\b\d{1,5}(?:(?![hHpPkK]\b)[A-Za-z]|/\d+)*\s+(?!h\b|giờ\b|gio\b|phút\b|phut\b|ngày\b|ngay\b|tháng\b|thang\b|năm\b|nam\b|k\b|ngàn\b|ngan\b|đồng\b|dong\b|vnd\b|vnđ\b|cái\b|cai\b|chiếc\b|chiec\b|người\b|nguoi\b|cuốn\b|cuon\b|hộp\b|hop\b|kg\b|g\b|lít\b|lit\b|ml\b)|$)";
    private const string AddressPrefixes = @"(?:đường|duong|hẻm|hem|ngõ|ngo|ngách|ngach|kiệt|kiet|phường|phuong|quận|quan|xã|xa|thôn|thon|ấp|ap|khu)";

    /// <summary>
    /// Removes delivery and contact instruction phrases.
    /// IMPORTANT: Uses NoiseBoundary to avoid eating into the actual address when punctuation is missing.
    /// </summary>
    internal static readonly Regex Instructions = new(
        @"(?<!\b" + AddressPrefixes + @"\s+)" +
        @"(?:" +
            @"(?:đừng|dung)\s+(?:gọi|goi).*?" + NoiseBoundary +
            @"|(?:gọi|goi)\s+(?:số|so|cho|này|nay|giúp|giup|dùm|dum|trước|truoc|trươc|điện|dien|lại|lai).*?" + NoiseBoundary +
            @"|(?:call)\s+(?:trước|truoc|trươc|khi).*?" + NoiseBoundary +
            @"|(?:nhận\s+hàng|nhan\s+hang)\s+(?:dùm|dum|giúp|giup).*?" + NoiseBoundary +
            @"|(?:liên\s*hệ|lien\s*he).*?" + NoiseBoundary +
            @"|(?:(?:chỉ\s*)?giao)\s+(?:cho|tới|toi|đến|den|hàng|hang|giúp|giup|cổng|cong|tại|tai|ở|o\b|buổi|buoi|lúc|luc|ngoài|ngoai).*?" + NoiseBoundary +
            @"|(?:ship|book\s*ship)\s+(?:tới|toi|đến|den|cho|về|ve).*?" + NoiseBoundary +
            @"|(?:địa\s*chỉ|dia\s*chi|đ/c|dc|nơi\s*giao|noi\s*giao)\s*[:：].*?" + NoiseBoundary +
            @"|(?:để|de|bỏ|bo)\s+(?:trước|truoc|trươc|ở|o\b|tại|tai|ngoài|ngoai).*?" + NoiseBoundary +
            @"|(?:gửi|gui)\s+(?:bảo\s*vệ|bao\s*ve|lễ\s*tân|le\s*tan|ở|o\b|tại|tai|cho).*?" + NoiseBoundary +
            @"|(?:nhận\s*hộ|nhan\s*ho).*?" + NoiseBoundary +
            @"|(?:(?:chỉ|chi|khi|trưa|trua|tối|toi|sáng|sang|chiều|chieu)\s*)?(?:đến|den|nhận|nhan)\s+(?:hàng|hang).*?" + NoiseBoundary +
            @"|(?:giao\s*)?(?:trong\s*|ngoài\s*|ngoai\s*)?(?:giờ\s*hành\s*chính|gio\s*hanh\s*chinh|giờ\s*hc|gio\s*hc).*?" + NoiseBoundary +
            @"|(?:mã\s*đơn|ma\s*don|đơn\s*hàng|don\s*hang).*?" + NoiseBoundary +
            @"|(?:cảm\s*ơn|cam\s*on|thanks|thank|tks).*?" + NoiseBoundary +
            @"|(?:chọn\s+địa\s+điểm|chon\s+dia\s+diem).*?" + NoiseBoundary +
            @"|(?:có\s+định\s+vị|co\s+dinh\s+vi)" +
        @")",
        DefaultOptions);

    // ─── Stage 3d: Direction / Landmark Notes ────────────────────────────

    /// <summary>
    /// Removes direction and landmark notes.
    /// Must be after a separator (comma, dash, semicolon) or at start of string
    /// to reduce false positives.
    /// </summary>
    internal static readonly Regex DirectionNotes = new(
        @"(?:[,;.\-]\s*|^)" +
        @"(?:" +
            @"(?:gần|gan|gàn)" +
            @"|(?:đối\s*diện|doi\s*dien)" +
            @"|(?:phía\s*sau|phia\s*sau)" +
            @"|(?:sau\s*lưng|sau\s*lung)" +
            @"|(?:cuối\s*hẻm|cuoi\s*hem)" +
            @"|(?:đầu\s*hẻm|dau\s*hem)" +
            @"|(?:ngay\s*ngã|ngay\s*nga)" +
            @"|(?:mặt\s*tiền|mat\s*tien)" +
            @"|(?:không\s*nhớ\s*số\s*nhà|khong\s*nho\s*so\s*nha)" +
            @"|(?:cạnh|canh)" +
            @"|(?:kế\s*bên|ke\s*ben)" +
            @"|(?:bên\s*hông|ben\s*hong)" +
            @"|(?:(?:next\s+to|opposite|behind|near|across\s+from|in\s+front\s+of|beside)(?!\s+(?:East|West|North|South|House|Tower|Garden|Plaza|Center|Building|Apt|Apartment)\b))" +
        @")" +
        @".*?" + NoiseBoundary,
        DefaultOptions);

    // ─── Stage 3e: Postal Codes and Country Name ─────────────────────────

    /// <summary>Removes postal code followed by "Việt Nam" / "Viet Nam".</summary>
    internal static readonly Regex PostalCodeWithCountry = new(
        @"\b\d{6}\b,?\s*(?:Việt\s*Nam|Viet\s*Nam)\b",
        DefaultOptions);

    /// <summary>Removes trailing "Việt Nam" / "Viet Nam".</summary>
    internal static readonly Regex TrailingCountryName = new(
        @",?\s*(?:Việt\s*Nam|Viet\s*Nam)\s*$",
        DefaultOptions);

    /// <summary>Removes standalone 6-digit postal codes at end of segments.</summary>
    internal static readonly Regex StandalonePostalCode = new(
        @"(?<=\s)\d{6}\b(?=[,;\s#?.]|$)",
        DefaultOptions);

    // ─── Stage 3f: Standalone Junk Tokens ────────────────────────────────

    /// <summary>Removes standalone # between separators.</summary>
    internal static readonly Regex StandaloneHash = new(
        @"(?<=,)\s*#\s*(?=,)|(?<=\s)#(?=\s*,|\s+|$)",
        DefaultOptions);

    /// <summary>Removes sequences of question marks (e.g., ???) that are noise tokens.</summary>
    internal static readonly Regex QuestionMarkJunk = new(
        @"\?{2,}",
        DefaultOptions);

    // ─── Stage 3g: Building / Floor / Unit Info ──────────────────────────

    /// <summary>
    /// Removes building, floor, unit, and residential area information.
    /// Only applied when RemoveBuildingInfo option is true.
    /// </summary>
    internal static readonly Regex BuildingInfo = new(
        @"\b(?:" +
            @"(?:tầng\s*trệt|tang\s*tret)" +
            @"|(?:tầng|tang|lầu|lau)\s*\d+" +
            @"|block\s*[A-Za-z0-9]+" +
            @"|tower\s*[A-Za-z0-9]+" +
            @"|(?:phòng|phong)\s*[A-Za-z0-9]+" +
            @"|shophouse" +
            @"|(?:chung\s*cư|chung\s*cu|cc)\s*[A-Za-z0-9\s]+" + // Chung cư + name
            @"|(?:căn\s*hộ|can\s*ho|ch)\s*[A-Za-z0-9\s]+" + // Căn hộ + name
            @"|(?:tòa\s*nhà|toa\s*nha)\s*[A-Za-z0-9\s]+" + // Tòa nhà + name
            @"|(?:khu\s*dân\s*cư|khu\s*dan\s*cu|kdc)\s*[A-Za-z0-9\s]+" + // Khu dân cư + name
            @"|(?:khu\s*đô\s*thị|khu\s*do\s*thi|kđt|kdt)\s*[A-Za-z0-9\s]+" + // Khu đô thị + name
            @"|(?:khu\s*tập\s*thể|khu\s*tap\s*the|ktt)\s*[A-Za-z0-9\s]+" + // Khu tập thể + name
        @")\b",
        DefaultOptions);

    // ─── Stage 5: Punctuation Cleanup ────────────────────────────────────

    /// <summary>Removes orphaned dash before a separator (e.g., "Quân -, Phường" → "Quân, Phường").</summary>
    internal static readonly Regex DashBeforeSeparator = new(@"\s*-\s*(?=[,;])", DefaultOptions);

    /// <summary>Collapses multiple commas (with optional whitespace) into a single comma.</summary>
    internal static readonly Regex MultipleCommas = new(@"(?:\s*,\s*){2,}", DefaultOptions);

    /// <summary>Normalizes spacing around comma/dash/semicolon to "X, Y" or "X - Y".</summary>
    internal static readonly Regex SpaceAroundSeparator = new(@"\s*([,;])\s*", DefaultOptions);

    /// <summary>Strips leading punctuation and whitespace.</summary>
    internal static readonly Regex LeadingPunctuation = new(@"^[\s,\-;:\.]+", DefaultOptions);

    /// <summary>Strips trailing junk characters.</summary>
    internal static readonly Regex TrailingJunk = new(@"[\s,\-;:#?./]+$", DefaultOptions);
}
