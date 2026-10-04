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
- User-facing docs (`README.md` in the root and per package, `CONTRIBUTING.md`, `SECURITY.md`, issue forms) are in Polish and link to their English version (`*.en.md`) at the top ([ADR-0009](docs/adr/0009-documentation-language.md)). Where an English version exists, update both together.
- When writing or editing them, apply `.claude/skills/miodkuj/SKILL.md` to the Polish text and `.claude/skills/stop-slop/SKILL.md` to the English text. Edit only the prose: keep code blocks, commands, package and API names, badges, links and numbers exactly as they are. Meaning comes before style: keep API limits and qualifiers ("only", "never", "at most") even where a style rule says to cut them.
