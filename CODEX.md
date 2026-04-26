# CODEX.md — OpenAI Codex Configuration

> Thin wrapper for Codex agents. Read `AGENTS.md` first.

## Read Order

1. `AGENTS.md` — Project overview, directory map, critical rules
2. `docs/context/handoff.md` — Current state and pending items

## Codex-Specific Instructions

### Environment Setup

```bash
# Restore and build
dotnet restore
dotnet build

# Run tests
dotnet test

# Pack NuGet package
dotnet pack src/VnAddressSanitizer -c Release -o ./nupkgs
```

### Key Constraints

1. **Multi-target .NET 6/7/8/9** — Do not use APIs exclusive to .NET 7+ (e.g., `[GeneratedRegex]`, `SearchValues<T>`)
2. **Pre-compiled regex only** — All patterns in `SanitizePatterns.cs` as `static readonly Regex`
3. **No external dependencies** in the core library — it must remain zero-dependency
4. **Nullable enabled** — All public APIs must be null-safe
5. **XML documentation** — Required on all public types and members

### Testing Rules

- Always run `dotnet test` after changes
- False positive tests in `FalsePositiveRegressionTests` are **mandatory pass criteria**
- Test both có dấu and không dấu input variants for Vietnamese patterns
- Test that empty/null/whitespace input returns `string.Empty`

### Working with Regex Patterns

When adding a new pattern:
1. Add the `static readonly Regex` field in `SanitizePatterns.cs`
2. Apply it in the correct stage in `AddressSanitizer.cs`
3. Gate it behind an option in `SanitizeOptions.cs` if it has false-positive risk
4. Add test cases in `AddressSanitizerTests.cs`
5. Verify false positive regression tests still pass

### Handoff

Update `docs/context/handoff.md` after completing work.
