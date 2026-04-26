# AGENTS.md — AI Agent Entry Point

> This is the first file any AI agent should read when entering this repository.

## Project Overview

**VnAddressSanitizer** — A .NET NuGet package that sanitizes/cleans Vietnamese addresses for geocoding.  
Removes phone numbers, delivery instructions, landmark notes, duplicate admin units, and other noise commonly found in ERP/POS/e-commerce address fields.

## Project Directory Map

| Path | Purpose |
|------|---------|
| `src/VnAddressSanitizer/` | Core class library (multi-target: net6.0-net9.0) |
| `tests/VnAddressSanitizer.Tests/` | xUnit test suite |
| `tools/VnAddressSanitizer.Runner/` | Console batch test runner |
| `docs/context/` | Handoff state, decisions, project context |
| `docs/ai/shared/` | AI-optimized summaries and pointers |
| `docs/rules/` | Canonical project rules |
| `docs/plans/` | Execution plans and tracking |
| `.agents/` | Antigravity/Gemini specific config |
| `.claude/` | Claude Code specific rules |

## Architecture: Pipeline (5+ Stages)

```
Raw Input → Normalize → Expand Abbreviations → Remove Parens → Remove Noise → Dedup Admin → Cleanup → Output
```

### Key Source Files

| File | Responsibility |
|------|---------------|
| `AddressSanitizer.cs` | Public API, pipeline orchestration |
| `SanitizeOptions.cs` | Configurable options |
| `SanitizePatterns.cs` | All pre-compiled regex patterns |
| `AdminUnitDeduplicator.cs` | ERP duplicate suffix removal |
| `VietnameseTextHelper.cs` | Diacritics removal for comparison |

## Quick Commands

```bash
# Build all
dotnet build

# Run tests
dotnet test

# Pack NuGet
dotnet pack src/VnAddressSanitizer -c Release

# Run batch sanitizer
dotnet run --project tools/VnAddressSanitizer.Runner -- input.txt
```

## Scan Scope (for AI agents)

**Include:** `src/`, `tests/`, `tools/`, `docs/`  
**Exclude:** `bin/`, `obj/`, `*.nupkg`, `.git/`, `TestResults/`

## Critical Rules

1. **Never remove core address parts** (house number, street, phường, quận, tỉnh/TP).
2. **Empty result → return trimmed original input** (safety fallback).
3. **Regex must be `static readonly`** — no per-call compilation.
4. **"giao" must not be matched standalone** — only with companions like "giao cho", "giao tới".
5. **All Vietnamese patterns must have both dấu and không dấu variants.**
6. **Multi-target compatibility** — no .NET 7+ only APIs (e.g., no `[GeneratedRegex]`).

## Handoff Protocol

Before starting work, read: `docs/context/handoff.md`  
After completing work, update: `docs/context/handoff.md`
