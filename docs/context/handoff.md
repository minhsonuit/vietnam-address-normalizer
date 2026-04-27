# Handoff — VnAddressSanitizer

> Last updated: 2026-04-26T23:32+07:00

## Current State

- **Phase**: V1.0.0 Published to NuGet successfully via GitHub Actions.
- **Status**: All 78 tests pass, automated CI/CD pipeline active.
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

Based on industry standards (e.g., libpostal, Mapzen), added logic to expand common Vietnamese administrative abbreviations to improve Geocoding match rates (tests expanded to 78/78):
1. **Stage 1.5 - Abbreviation Expansion**: Added regex to safely expand `Q1`/`Q.1` -> `Quận 1`, `P. 12` -> `Phường 12`, `TP` -> `Thành phố`, `TX` -> `Thị xã`, `TT` -> `Thị trấn`. Enabled by default via `ExpandAbbreviations = true`.
2. **Extended Building Info**: Upgraded `RemoveBuildingInfo` logic to include `Chung cư`, `Căn hộ`, `Tòa nhà`, `KĐT`, `KDC`, `Khu tập thể` (e.g., `Khu đô thị Sala`, `Chung cư Vinhome`).

### Documentation & Rule Set (2026-04-26 session 5)
- Created `docs/rules/noise-patterns-logic.md` to formally document the 7 logical groups of address noise and the regex strategies used to sanitize them (Contact info, Instructions, Landmarks, Typos, Duplication, Building info, Postal codes).
- Initialized Git repository and pushed initial commit to GitHub (`minhsonuit/vietnam-address-normalizer`).

### CI/CD & Publishing (2026-04-26 session 6)
- Created GitHub Actions Workflow (`.github/workflows/publish.yml`) to automatically test, build, pack, and push the package to NuGet.org on branch `main` pushes or manual triggers.
- Verified NuGet package successfully published.

### Regex Edge Case Fixes (2026-04-26 session 8)
- Fixed Abbreviation Collisions: Added negative lookbehinds in `AbbrQuanNumber`/`AbbrPhuongNumber` to prevent expanding `P12` and `Q1` when preceded by `Block`, `Tower`, etc.
- Fixed Instruction False Positives: Added negative lookbehinds in `Instructions` regex to prevent stripping keywords (`Thank`, `Gửi bảo vệ`, `Nhận hộ`, etc.) when they immediately follow valid street indicators like `Đường`, `Hẻm`, `Phường`, etc.
- Fixed English Landmark False Positives: Added negative lookahead to prevent English directions (`near`, `opposite`, `behind`) from destroying English proper nouns (e.g., `Near East Plaza`).
- Fixed Start-of-String Landmark False Negatives: Added `^` anchor to `DirectionNotes` to properly remove landmark notes that appear at the very beginning of the address (e.g., `gần chợ Bến Thành, ...`).
- Added 16 new test cases to `AddressSanitizerTests.cs` to ensure these regressions stay fixed. Tests passed: 94/94.

## Future Maintenance

- [ ] Consider additional patterns from new real-world datasets as they arise.

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
- `sample-input-codeX.txt`
