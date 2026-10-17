---
status: accepted
date: 2026-10-17
decision-makers: milten89
---

# 0009. Documentation language

## Context and Problem Statement

The code, XML docs, package metadata and messages are in English. The primary audience is Polish developers and companies, many of them maintaining .NET Framework and ASP.NET Core applications, but international teams also integrate with Polish public APIs, and coding agents work best with consistent rules. nuget.org shows each package's packed README under its English description. Which language is used where, and which text wins when two language versions differ?

## Considered Options

* English as the base; a short Polish version of the informational parts only
* Polish as the base, with a full English mirror of every user-facing document
* English only

## Decision Outcome

Chosen option: "English as the base; a short Polish version of the informational parts only", because it gives every reader one complete, authoritative text in the language of the code and the package metadata, and keeps a Polish introduction where Polish readers decide whether to use a package.

* **English** – everything except the Polish files below: identifiers, XML docs, code comments, commit messages, PR descriptions, ADRs, `CLAUDE.md`/`AGENTS.md`/skills/agents, technical docs in `docs/`, error and log messages, and the user-facing docs: `README.md` (root and per package; it is the GitHub landing page and the README packed for nuget.org), `CONTRIBUTING.md`, `SECURITY.md`, the samples README and the skeleton READMEs.
* **English is the single source of truth.** If a Polish text differs from the English one, the English one wins, and each Polish file says so at the top. The XML docs define API behaviour; the READMEs explain it.
* **Polish** – `README.pl.md` next to the root README and the README of each published package. It has only the informational parts: what the package is and the "unofficial" notice, installation, the quick-start example (the same code as in English), and links to the English sections. Contract and reference content stays English only: API limits and data availability, errors, telemetry, configuration rules, dependencies, versioning, support. `SECURITY.pl.md` says how to report a vulnerability.
* Issue forms are bilingual inline: each label and description has a Polish and an English part.
* Package READMEs link to each other with absolute GitHub URLs, because nuget.org doesn't resolve relative links.
* Polish domain terms with no good translation may appear in identifiers only when they are the official API name (e.g. NBP `cenyzlota` path), and are explained in XML docs.

### Consequences

* Good, because nuget.org and GitHub show a complete README in the language of the package metadata and the XML docs.
* Good, because one text is authoritative, so a difference between the versions has a documented answer.
* Good, because the Polish files are short and change only when an informational part changes.
* Bad, because Polish readers find the details (limits, errors, configuration) in English.
* Bad, because the Polish files link to sections of the English READMEs, so renaming or removing such a section has to update those links.

### Confirmation

The PR template asks whether an informational part of a README changed, and if so, whether `README.pl.md` changed too; the `reviewer` agent checks that a `*.pl.md` file gets no contract content.

## More Information

**2026-10-04 note.** English versions exist for the root and package READMEs, `CONTRIBUTING` and `SECURITY` (backlog item 22). Package READMEs link to each other with absolute GitHub URLs, because nuget.org shows the Polish README (at the time) and doesn't resolve relative links. The issue forms are bilingual inline instead of linking to an English version: each label and description has a Polish and an English part.

**2026-10-17 change: English as the base** ([#73](https://github.com/milten89/OpenUrzednik.NET/issues/73)). The maintainer replaced the Polish-first rule. Before, every user-facing document was Polish with an English mirror (`*.en.md`), and the packed README was Polish under English metadata. Thirteen documents existed in two complete versions (26 files) kept in sync by hand, and nothing said which version was right when they differed. `README.md` is now English in the root and in every package, and `README.pl.md` keeps only the informational parts. `CONTRIBUTING`, the samples README and the skeleton READMEs are English only. `SECURITY.md` is English, and `SECURITY.pl.md` explains how to report. The Decision Outcome above also corrects the old bullet that listed issue forms among the Polish documents linking to an English version: they have been bilingual inline since the 2026-10-04 note.
