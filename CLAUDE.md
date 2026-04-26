# CLAUDE.md — Claude Code / Opus Configuration

> Thin wrapper for Claude Code. Read `AGENTS.md` first for project overview.

## Read Order

1. `AGENTS.md` — Project overview, directory map, critical rules
2. `docs/context/handoff.md` — Current state and pending items
3. Relevant source files in `src/VnAddressSanitizer/`

## Claude-Specific Rules

### Mandatory Behaviors

- **Ask before assuming**: If a requirement is ambiguous, ask the user rather than guessing.
- **Use MCP tools proactively**: Search the codebase before making changes to understand existing patterns.
- **Verify regex changes**: After modifying any regex pattern, run the full test suite (`dotnet test`) to catch regressions.
- **Preserve diacritics in output**: Never strip Vietnamese diacritics from the final sanitized result; only strip internally for comparison.

### Code Style

- C# with nullable enabled, implicit usings
- XML documentation on all public members
- Pre-compiled regex as `static readonly` fields (no `[GeneratedRegex]` for .NET 6 compat)
- Follow existing pattern organization in `SanitizePatterns.cs`

### Commit Messages

Use conventional commits:
```
feat: add new noise pattern for [description]
fix: prevent false positive on [pattern]
test: add regression test for [scenario]
refactor: improve [component] performance
```

### Verification Checklist

Before completing any task:
- [ ] `dotnet build` passes
- [ ] `dotnet test` passes (all tests green)
- [ ] False positive regression tests still pass
- [ ] Update `docs/context/handoff.md` with changes made
