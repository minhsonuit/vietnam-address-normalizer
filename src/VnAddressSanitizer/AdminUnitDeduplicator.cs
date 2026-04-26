using System.Text.RegularExpressions;

namespace VnAddressSanitizer;

internal static class AdminUnitDeduplicator
{
    private static readonly Regex AdminPrefixes = new(
        @"^\s*(?:thành\s*phố|thanh\s*pho|tp\.?\s*|tỉnh|tinh|quận|quan|q\.?\s*|huyện|huyen|thị\s*xã|thi\s*xa|tx\.?\s*|phường|phuong|p\.?\s*|xã|xa|thị\s*trấn|thi\s*tran|tt\.?\s*)\s*",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static string Deduplicate(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;
        var segments = input.Split(',');
        if (segments.Length < 3) return input;

        var trimmed = new string[segments.Length];
        for (int i = 0; i < segments.Length; i++)
            trimmed[i] = segments[i].Trim();

        // Try exact suffix dedup (lengths 4 down to 2)
        for (int suffixLen = Math.Min(4, (segments.Length - 1) / 2); suffixLen >= 2; suffixLen--)
        {
            int totalLen = trimmed.Length;
            if (totalLen < suffixLen * 2 + 1) continue;
            int suffixStart = totalLen - suffixLen;
            int candidateStart = suffixStart - suffixLen;
            bool isMatch = true;
            for (int i = 0; i < suffixLen; i++)
            {
                if (!SegmentsMatch(trimmed[candidateStart + i], trimmed[suffixStart + i]))
                { isMatch = false; break; }
            }
            if (isMatch)
            {
                var parts = new List<string>();
                for (int i = 0; i < candidateStart; i++) parts.Add(trimmed[i]);
                for (int i = suffixStart; i < totalLen; i++) parts.Add(trimmed[i]);
                return string.Join(", ", parts);
            }
        }

        // Partial overlap dedup
        return TryPartialDedup(trimmed);
    }

    private static string TryPartialDedup(string[] segments)
    {
        int len = segments.Length;
        for (int tailLen = Math.Min(4, len - 1); tailLen >= 2; tailLen--)
        {
            int tailStart = len - tailLen;
            for (int scanStart = 1; scanStart <= tailStart - tailLen; scanStart++)
            {
                bool allMatch = true;
                for (int i = 0; i < tailLen; i++)
                {
                    if (!SegmentsMatch(segments[scanStart + i], segments[tailStart + i]))
                    { allMatch = false; break; }
                }
                if (allMatch)
                {
                    var result = new List<string>();
                    for (int i = 0; i < scanStart; i++) result.Add(segments[i]);
                    for (int i = tailStart; i < len; i++) result.Add(segments[i]);
                    return string.Join(", ", result);
                }
            }
        }
        return string.Join(", ", segments);
    }

    /// <summary>
    /// Common Vietnamese abbreviation → full name mappings (all lowercase, no diacritics).
    /// </summary>
    private static readonly Dictionary<string, string> Abbreviations = new(StringComparer.OrdinalIgnoreCase)
    {
        { "hcm", "ho chi minh" },
        { "hn", "ha noi" },
        { "dn", "da nang" },
        { "hp", "hai phong" },
        { "ct", "can tho" },
        { "vt", "vung tau" },
        { "brvt", "ba ria vung tau" },
        { "bd", "binh duong" },
        { "la", "long an" },
    };

    private static bool SegmentsMatch(string a, string b)
    {
        var normA = Normalize(a);
        var normB = Normalize(b);
        if (string.Equals(normA, normB, StringComparison.OrdinalIgnoreCase))
            return true;
        // Try expanding abbreviations
        var expandedA = ExpandAbbreviation(normA);
        var expandedB = ExpandAbbreviation(normB);
        if (string.Equals(expandedA, expandedB, StringComparison.OrdinalIgnoreCase))
            return true;
        // Fallback: check if one contains the other (handles "hcm" vs "ho chi minh")
        if (normA.Length >= 2 && normB.Length >= 2)
        {
            if (expandedA.Contains(expandedB, StringComparison.OrdinalIgnoreCase) ||
                expandedB.Contains(expandedA, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static string ExpandAbbreviation(string normalized)
    {
        return Abbreviations.TryGetValue(normalized, out var expanded) ? expanded : normalized;
    }

    private static string Normalize(string segment)
    {
        var s = VietnameseTextHelper.NormalizeForComparison(segment.Trim());
        s = AdminPrefixes.Replace(s, "");
        s = s.Trim().Trim('.', ',', ' ');
        return SanitizePatterns.MultipleWhitespace.Replace(s, " ");
    }
}
