---
paths:
  - "**/*.md"
  - ".github/ISSUE_TEMPLATE/**"
---

# Documentation language and prose style

- Language ([ADR-0009](../../docs/adr/0009-documentation-language.md)): everything is in English, user-facing docs included (`README.md` in the root and per package, `CONTRIBUTING.md`, `SECURITY.md`, the samples README).
  - English is the single source of truth: if a Polish text differs, the English one wins.
  - `README.pl.md` next to the root README and each published package's README has only the informational parts: what the package is and the "unofficial" notice, installation, the quick-start example (the same code) and links to the English sections.
  - Contract and reference content (API limits, data availability, errors, telemetry, configuration rules, dependencies, versioning, support) is English only.
  - `SECURITY.pl.md` says how to report a vulnerability.
  - Issue forms are bilingual inline.
  - When a PR changes an informational part of a README, it updates `README.pl.md` too, and renaming or removing a section of an English README updates the links to it in `README.pl.md`.
  - A package README (`src/*/README.md`, `README.pl.md`) links into the repository with absolute `https://github.com/milten89/OpenUrzednik.NET/blob/main/…` URLs: nuget.org shows the packed README, and `main` holds the released docs (`/prepare-release` checks it).
  - Such a link stays broken until the next release reaches `main`.
- Prose style: when you write or edit user-facing prose (READMEs, `CONTRIBUTING`, `SECURITY`), run `/stop-slop` on the English files and `/miodkuj` on the Polish ones (`*.pl.md`). Edit only the prose: keep code blocks, commands, package and API names, badges, links and numbers exactly as they are. Meaning comes before style: keep API limits and qualifiers ("only", "never", "at most") even where a style rule says to cut them (stop-slop removes absolutes and adverbs).
