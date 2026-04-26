# Architecture Decisions — VnAddressSanitizer

## ADR-001: Multi-target .NET 6-9 instead of .NET 8 only

**Context**: The prompt specifies .NET 8+, but the user wants broad compatibility ("netcore trở lên phải xài được").  
**Decision**: Target `net6.0;net7.0;net8.0;net9.0` using compatible APIs only.  
**Consequence**: Cannot use `[GeneratedRegex]` (.NET 7+) or `SearchValues<T>` (.NET 8+). Use `static readonly Regex` with `RegexOptions.Compiled` instead.

## ADR-002: Pipeline architecture over single-pass regex

**Context**: Address noise varies wildly; a single regex cannot handle all cases.  
**Decision**: 5-stage sequential pipeline where each stage's output feeds the next.  
**Consequence**: Easier to debug, test, and extend individual stages. Slight performance cost from multiple passes over the string, but acceptable for typical address lengths.

## ADR-003: "giao" word boundary protection

**Context**: "giao" appears in street names like "Đường Thuận Giao 25".  
**Decision**: Only match "giao" when followed by companion words ("cho", "tới", "đến", "hàng", "giúp").  
**Consequence**: Some edge-case delivery instructions starting with standalone "giao" may not be removed, but this is an acceptable trade-off vs. false positives on street names.

## ADR-004: Admin dedup prefers formal suffix

**Context**: ERP systems append formal admin units ("Phường X, Quận Y") after informal user input.  
**Decision**: When duplicates are detected, keep the formal (later) suffix and remove the informal (earlier) one.  
**Consequence**: Output is more standardized for geocoding APIs.

## ADR-005: Abbreviation Expansion for Geocoding

**Context**: Vietnamese addresses heavily use abbreviations (Q.1, P.2, TP, TX, TT). Geocoding services (Google Maps, VietMap) generally perform better and yield higher match rates when querying formal, expanded names (Quận 1, Phường 2, Thành phố...).
**Decision**: Add an intermediate Stage 1.5 to automatically expand these abbreviations using safe Regex lookaheads/word boundaries. Enabled by default (`ExpandAbbreviations = true`).
**Consequence**: Significantly improves match rate on external map APIs. Requires strict regex boundaries (e.g. `\b(?:Q|q)[\.\s]*(\d+[A-Za-z]?)\b`) to prevent false positives like replacing the "Q" in a normal word.
