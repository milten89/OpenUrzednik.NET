# AGENTS.md

This repository's agent instructions are in [`CLAUDE.md`](CLAUDE.md). They apply to every coding agent (Claude Code, GitHub Copilot, Codex, Cursor, ...). Binding architecture decisions are in [`docs/adr/`](docs/adr/README.md), and current priorities are in [`docs/BACKLOG.md`](docs/BACKLOG.md).

Quick reference:

```bash
dotnet build OpenUrzednik.slnx
dotnet test OpenUrzednik.slnx -f net10.0
dotnet format OpenUrzednik.slnx --verify-no-changes
```

- Expected failures are returned as `OpenUrzednikResult` errors. Never use `catch (Exception)`.
- No new package dependencies in `src/` projects without an ADR.
- Branch from `develop`, open PRs to `develop`, squash-merge.
- When writing or editing READMEs or other user-facing docs, apply `.claude/skills/miodkuj/SKILL.md` to Polish text and `.claude/skills/stop-slop/SKILL.md` to English text. Don't touch code blocks, commands, names or links.
