# AGENTS.md

The instructions for every coding agent (Claude Code, GitHub Copilot, Codex, Cursor, ...) are in [`CLAUDE.md`](CLAUDE.md). Read it before you change anything. This file doesn't repeat its rules.

The rest of each area's rules are in [`.claude/rules/`](.claude/rules/). Claude Code loads a rule file by itself when it works with a file the rule's `paths:` match. Other agents: before you change a file, read every rule file whose paths match it.

| Rule file | Paths |
|---|---|
| [`client-api.md`](.claude/rules/client-api.md) | `src/**/*.cs`, `tests/**/*.cs`, `samples/**/*.cs` |
| [`code-conventions.md`](.claude/rules/code-conventions.md) | `src/**/*.cs`, `samples/**/*.cs` |
| [`dependencies.md`](.claude/rules/dependencies.md) | `**/*.csproj`, `**/*.props`, `**/*.targets`, `.github/dependabot.yml`, `src/*.DependencyInjection/**` |
| [`docs.md`](.claude/rules/docs.md) | `**/*.md`, `.github/ISSUE_TEMPLATE/**` |
| [`errors.md`](.claude/rules/errors.md) | `src/**/*.cs`, `tests/**/*.cs` |
| [`http.md`](.claude/rules/http.md) | `src/**/*.cs`, `tests/**/*.cs` |
| [`packaging-and-ci.md`](.claude/rules/packaging-and-ci.md) | `.github/**`, `version.json`, `OpenUrzednik.Packages.slnf`, `OpenUrzednik.slnx`, `src/**/*.csproj`, `assets/branding/**` |
| [`performance.md`](.claude/rules/performance.md) | `src/**/*.cs` |
| [`target-frameworks.md`](.claude/rules/target-frameworks.md) | `src/**`, `tests/**`, `**/*.props`, `**/*.targets`, `version.json` |
| [`telemetry.md`](.claude/rules/telemetry.md) | `src/**/*.cs`, `tests/**/*.cs` |
| [`testing.md`](.claude/rules/testing.md) | `tests/**`, `src/**/*.cs` |

- Procedures (adding or changing a client method, checking the live API, writing an ADR, prose style): `.claude/skills/*/SKILL.md`.
- Review checklist: [`.claude/agents/reviewer.md`](.claude/agents/reviewer.md).
- Binding architecture decisions: [`docs/adr/`](docs/adr/README.md).
