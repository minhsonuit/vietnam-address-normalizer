# GEMINI.md — Gemini / Antigravity Configuration

> Thin wrapper for Gemini and Antigravity agents. Read `AGENTS.md` first.

## Read Order

1. `AGENTS.md` — Project overview, directory map, critical rules
2. `docs/context/handoff.md` — Current state and pending items
4. Relevant source files in `src/VnAddressSanitizer/`

## Gemini-Specific Rules

### Mandatory Behaviors

- **NEVER guess when uncertain** — ask the user for clarification.
- **ALWAYS search before implementing** — use tools to explore existing code before writing new code.
- **Test after every change** — run `dotnet test` after modifying source code.
- **Preserve backward compatibility** — changes must not break the public API contract.

### Vietnamese Text Processing Rules

- All regex patterns MUST handle both **có dấu** (with diacritics) and **không dấu** (without diacritics) variants.
- Use `VietnameseTextHelper.RemoveDiacritics()` for internal comparison only.
- NEVER modify the output to remove diacritics.
- The `đ`/`Đ` character requires special handling (Unicode decomposition doesn't cover it).

### Code Organization

| Concern | File |
|---------|------|
| New regex pattern | `SanitizePatterns.cs` |
| Pipeline stage logic | `AddressSanitizer.cs` |
| Admin unit logic | `AdminUnitDeduplicator.cs` |
| Text comparison helpers | `VietnameseTextHelper.cs` |
| New options | `SanitizeOptions.cs` |

### Verification Commands

```bash
dotnet build                    # Must pass
dotnet test                     # All tests must pass
dotnet test --filter "FalsePositive"   # Regression tests must pass
dotnet pack src/VnAddressSanitizer -c Release  # NuGet pack must succeed
```

### Handoff Protocol

After every session:
1. Update `docs/context/handoff.md` with changes, pending items, and touched files.
2. Keep handoff under 5 KB — archive completed items if needed.
