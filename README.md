# VnAddressSanitizer

[![NuGet](https://img.shields.io/nuget/v/VnAddressSanitizer)](https://www.nuget.org/packages/VnAddressSanitizer)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

Vietnamese address sanitizer for geocoding. Cleans noisy user input (phone numbers, delivery instructions, landmarks, duplicate admin units) into standardized address strings optimized for VietMap, Google Maps, HERE, and Mapbox APIs.

## Installation

```bash
dotnet add package VnAddressSanitizer
```

## Quick Start

```csharp
using VnAddressSanitizer;

// Default geocoding-optimized sanitization
var clean = AddressSanitizer.Sanitize(
    "0868047361 gọi này nhận hàng dùm em, 31 trương phước phan"
);
// Result: "31 trương phước phan"
```

## Before / After Examples

| Input | Output |
|---|---|
| `0868047361 gọi này nhận hàng dùm em, 31 trương phước phan` | `31 trương phước phan` |
| `phone: 0944701399, 28b hai bà trưng, Xã Bình Hưng` | `28b hai bà trưng, Xã Bình Hưng` |
| `1279 Nguyễn Tất Thành (Khách Sạn Hiền Thuận)` | `1279 Nguyễn Tất Thành` |
| `158B Giai Phong, gần coopmart, Hải Phòng` | `158B Giai Phong, Hải Phòng` |
| `Hồ Chí Minh 700000, Việt Nam` | `Hồ Chí Minh` |

## Custom Options

```csharp
var options = new SanitizeOptions
{
    RemoveParentheses = true,        // Remove (...) content
    RemovePhoneNumbers = true,       // Remove VN phone numbers
    RemoveInstructions = true,       // Remove delivery/contact notes
    RemoveDirectionNotes = true,     // Remove landmark/direction notes
    RemovePostalCodesAndCountry = true,  // Remove postal codes & "Việt Nam"
    DeduplicateAdminUnits = true,    // Deduplicate Phường/Quận/TP suffixes
    RemoveBuildingInfo = false,      // Keep building/floor info (useful for delivery)
    AdditionalPatterns = new[] { @"\bCustomPattern\b" }  // Add custom regex
};

var clean = AddressSanitizer.Sanitize(rawAddress, options);
```

## Pipeline

```
Raw Input
  → Stage 1: Unicode Normalization (NFC, whitespace, dashes)
  → Stage 2: Parenthetical Content Removal
  → Stage 3: Noise Removal (phone, labels, instructions, landmarks, postal)
  → Stage 4: Admin Unit Deduplication
  → Stage 5: Punctuation Cleanup
  → Clean Output
```

## Batch Testing with Console Runner

```bash
dotnet run --project tools/VnAddressSanitizer.Runner -- input.txt
```
### Mac or Linux
```bash
dotnet run --project tools/VnAddressSanitizer.Runner -- "$(pwd)/sample_input.txt"
```



## Supported .NET Versions

- .NET 6.0
- .NET 7.0
- .NET 8.0
- .NET 9.0

## Known Trade-offs

- **Parentheses removal** may remove useful info like `(Hẻm 1539)`. Disable with `RemoveParentheses = false`.
- **Direction note removal** is context-aware (requires separator prefix) but may occasionally remove relevant info.
- **Building info** is preserved by default since it may be useful for delivery workflows.
- This package does **not** parse addresses into structured admin units — it only cleans strings for better geocoding results.

## License

MIT
