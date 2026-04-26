# Project Context — VnAddressSanitizer

## Purpose

A .NET NuGet package to sanitize/clean Vietnamese address strings for geocoding.
Targets ERP/POS/e-commerce systems where users enter phone numbers, delivery instructions, landmarks, and notes into address fields.

## Business Problem

Vietnamese address data in production systems is extremely noisy. Users often treat the address field as a "note to shipper", and CRM/ERP systems introduce their own formatting artifacts. This results in 7 logical groups of incorrect inputs (noise):

1. **Contact Information (Thông tin liên lạc):** Phone numbers mixed with addresses, often with labels ("sđt: 090...", "phone:").
2. **Delivery/Order Instructions (Chỉ dẫn giao nhận):** Notes meant directly for the shipper ("gọi trước khi giao", "nhận hàng dùm", "ship tới", "mã đơn...").
3. **Direction & Landmark Notes (Chỉ dẫn tìm đường/Cột mốc):** Descriptive mốc địa lý rather than formal addresses ("gần chợ", "đối diện", "sau lưng", "ngay ngã 3").
4. **Formatting, Typos & Abbreviations (Viết tắt & Ký tự rác):** "Q1" instead of "Quận 1", "TP HCM", orphaned dashes `-,`, consecutive question marks `???`, and notes in parentheses `(...)`.
5. **Admin Unit Duplication (Trùng lặp hành chính):** Caused by ERP auto-append from combo boxes (e.g., "...Phường Bến Nghé, Phường Bến Nghé, Quận 1...").
6. **Building & Project Info (Thông tin tòa nhà/Nội khu):** "Tầng 3", "Block B", "Chung cư Vinhome" (often hurts geocoding match rates).
7. **Postal Codes & Country (Mã bưu điện & Quốc gia):** Superfluous info for local geocoders ("700000", "Việt Nam").

This noise causes geocoding APIs (VietMap, Google Maps) to return incorrect or no results.

## Core Data Characteristics (The ERP Context)

Our ERP system has a peculiar address creation flow:
- Users input free-text into the address field (often full of noise like phone numbers, notes, instructions).
- The system automatically appends the formal administrative units (Phường/Xã, Quận/Huyện, Tỉnh/TP) from combo boxes to the end of the string.
- **Result:** The beginning of the string is very noisy, but the end is perfectly standardized.

This characteristic is mapped into our system through 2 primary designs:
1. **Deduplication from the Tail (AdminUnitDeduplicator)**: We always prefer the formal suffix. If "Bình Chánh" appears twice, we keep the one at the end (appended by the ERP) and remove the one typed by the user.
2. **Left-to-Right Regex Consumption**: All noise removal patterns (Stage 3) are designed to consume noise from the beginning and **stop strictly before the formal separators** (commas/dashes). This ensures we never accidentally delete the valid administrative boundaries at the end.

## Design Decisions

1. **String sanitizer, not address parser** — We clean strings, not parse into structured admin units.
2. **Geocoding-optimized by default** — Options favor removing noise even at cost of losing some info.
3. **Safety fallback** — If sanitization produces empty output, return trimmed original.
4. **Multi-target .NET 6-9** — Use `static readonly Regex` (not `[GeneratedRegex]`) for compatibility.
5. **Pre-compiled regex** — All patterns compiled at class load, zero per-call overhead.
6. **Dual-variant patterns** — All Vietnamese patterns match both có dấu and không dấu.

## Technical Stack

- .NET 6.0 / 7.0 / 8.0 / 9.0 (multi-target)
- xUnit + FluentAssertions for testing
- Zero external dependencies in core library
