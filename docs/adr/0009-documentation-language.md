---
status: accepted
date: 2026-10-01
decision-makers: milten89
---

# 0009. Documentation language

## Context and Problem Statement

The code and XML docs are in English, while `README.md` and `CONTRIBUTING.md` are in Polish. The primary audience is Polish developers and companies, but international teams also integrate with Polish public APIs, and coding agents work best with consistent rules. Which language is used where?

## Decision Outcome

* **English** – everything code-related: identifiers, XML docs, code comments, commit messages, PR descriptions, ADRs, `CLAUDE.md`/`AGENTS.md`/skills/agents, technical docs in `docs/`, error and log messages.
* **Polish** – user-facing documents: `README.md` (root and per package), `CONTRIBUTING.md`, `SECURITY.md`, issue forms. Each Polish document links to its English version (e.g. `README.en.md`) at the top.
* Polish domain terms with no good translation may appear in identifiers only when they are the official API name (e.g. NBP `cenyzlota` path), and are explained in XML docs.

### Consequences

* Good, because Polish users get native docs and everyone else has an English version.
* Bad, because user-facing documents must be maintained in two languages; a change to one requires updating the other in the same PR.

### Confirmation

PR template checkbox "Updated both language versions of user-facing docs".

## More Information

**2026-10-04 note.** English versions exist for the root and package READMEs, `CONTRIBUTING` and `SECURITY` (backlog item 22). Package READMEs link to each other with absolute GitHub URLs, because nuget.org shows the Polish README and doesn't resolve relative links. The issue forms are bilingual inline instead of linking to an English version: each label and description has a Polish and an English part.
