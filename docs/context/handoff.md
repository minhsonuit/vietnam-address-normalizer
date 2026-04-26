# Handoff — VnAddressSanitizer

> Last updated: 2026-04-26T15:48+07:00

## Current State

- **Phase**: Initial implementation complete, batch-test issues fixed
- **Status**: All 52 tests pass, batch output verified clean
- **Batch stats**: Total: 1019 | Changed: 322 | Unchanged: 697

## What Was Done

- Created .NET solution with multi-target library (net6.0/net7.0/net8.0/net9.0)
- Implemented 5-stage sanitization pipeline:
  1. Normalization (NFC, whitespace, dashes)
  2. Parenthetical content removal
  3. Noise removal (phone, labels, instructions, landmarks, postal, junk, building)
  4. Admin unit deduplication
  5. Punctuation cleanup
- Created xUnit test suite with 11 test categories (52 tests total)
- Created console batch runner
- Created AI agent configs (AGENTS.md, CLAUDE.md, GEMINI.md, CODEX.md)
- Created README with NuGet metadata

### Batch-Test Fixes (2026-04-26 session 2)

Fixed 4 issues identified from `sample_input_output.txt` review:

1. **`-,` separator artifacts** (6 cases) — Added `DashBeforeSeparator` regex in Stage 5 cleanup to remove orphaned `-` before `,` or `;`. Root cause: `call truoc khi den` was removed but the preceding `-` separator remained.
2. **`next to` English landmarks** (10 cases) — Added `next to`, `opposite`, `behind`, `near` as English direction keywords in `DirectionNotes` pattern.
3. **`giao cổng bảo vệ` delivery instructions** (9 cases) — Added `cổng/cong`, `tại/tai`, `ở/o` as companion words for the `giao` instruction pattern.
4. **`???` junk tokens** (22 input, 1 output leftover) — Added `QuestionMarkJunk` pattern (`\?{2,}`) in Stage 3f to remove consecutive question marks.

Also expanded `call` instruction matching to include `call khi` (covers `call truoc khi den`).

### Additional Pattern Expansions (2026-04-26 session 3)

Expanded logic based on real-world edge cases (tests expanded from 52 to 65):
1. **Instruction Patterns**: Added `để/bỏ trước cổng/cửa`, `gửi bảo vệ/lễ tân`, `nhận hộ`, and time-based instructions (`giờ hành chính`).
2. **Direction Notes**: Added English landmark directions (`across from`, `in front of`, `beside`).
3. **False Positives**: Verified English name regression safety (`Callisto Tower`, `Near East Plaza`) and Vietnamese false positives (`Xã Giao Khẩu`).
*Note: Batch test stats remain the same (322 changed / 697 unchanged) because these specific edge cases were not present in the current `sample_input.txt`, but they are now covered by unit tests.*

### Abbreviation Expansion & Extended Building Info (2026-04-26 session 4)

Based on industry standards (e.g., libpostal, Mapzen), added logic to expand common Vietnamese administrative abbreviations to improve Geocoding match rates (tests expanded to 71/71):
1. **Stage 1.5 - Abbreviation Expansion**: Added regex to safely expand `Q1`/`Q.1` -> `Quận 1`, `P. 12` -> `Phường 12`, `TP` -> `Thành phố`, `TX` -> `Thị xã`, `TT` -> `Thị trấn`. Enabled by default via `ExpandAbbreviations = true`.
2. **Extended Building Info**: Upgraded `RemoveBuildingInfo` logic to include `Chung cư`, `Căn hộ`, `Tòa nhà`, `KĐT`, `KDC`, `Khu tập thể` (e.g., `Khu đô thị Sala`, `Chung cư Vinhome`).

## Pending Items

- [x] ~~Run `dotnet build` to verify compilation~~ ✅
- [x] ~~Run `dotnet test` to verify all tests pass~~ ✅ (71/71)
- [x] ~~Fine-tune regex patterns based on real-world data testing~~ ✅ (batch round 2 + expanded patterns + abbreviation expansion)
- [x] ~~Run batch test with production address data~~ ✅ (1019 addresses)
- [ ] Verify NuGet pack succeeds: `dotnet pack src/VnAddressSanitizer -c Release`
- [ ] Git init and first commit
- [ ] Publish to NuGet (when ready)
- [ ] Consider additional patterns from new real-world datasets

## Touched Files

- `VnAddressSanitizer.sln`
- `Directory.Build.props`
- `src/VnAddressSanitizer/*.cs` (5 files)
- `src/VnAddressSanitizer/VnAddressSanitizer.csproj`
- `tests/VnAddressSanitizer.Tests/AddressSanitizerTests.cs`
- `tests/VnAddressSanitizer.Tests/VnAddressSanitizer.Tests.csproj`
- `tools/VnAddressSanitizer.Runner/Program.cs`
- `tools/VnAddressSanitizer.Runner/VnAddressSanitizer.Runner.csproj`
- `AGENTS.md`, `CLAUDE.md`, `GEMINI.md`, `CODEX.md`
- `README.md`, `.gitignore`
