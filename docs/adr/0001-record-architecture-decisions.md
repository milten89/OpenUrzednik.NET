---
status: accepted
date: 2026-10-01
decision-makers: milten89
---

# 0001. Record architecture decisions

## Context and Problem Statement

Many design decisions of OpenUrzednik.NET (error model, dependency policy, target frameworks, release flow) existed only in the maintainer's head. Contributors and AI coding agents cannot follow rules that are not written down, and decisions get silently re-litigated or violated. How do we keep decisions discoverable and durable?

## Decision Drivers

* Decisions must be readable by humans and by coding agents (Claude Code, Copilot, etc.).
* Low ceremony – one Markdown file per decision, reviewed in a normal PR.
* History must be kept: superseded decisions stay in the repo.

## Considered Options

* MADR (Markdown Any Decision Records) files in `docs/adr/`
* Minimal Nygard-style ADRs
* A single `DECISIONS.md` log

## Decision Outcome

Chosen option: "MADR in `docs/adr/`", because it is a widely known format with enough structure (drivers, options, consequences, confirmation) without heavy tooling.

Rules:

* File name: `NNNN-kebab-case-title.md`, numbers are sequential and never reused. Start from [`template.md`](template.md).
* New ADRs start as `proposed`; they become `accepted` when merged to `develop` by the maintainer.
* An accepted ADR is not rewritten. To change a decision, add a new ADR and mark the old one `superseded by ADR-NNNN`.
* Every change that contradicts an accepted ADR must come with a new ADR in the same PR.
* The index in [`README.md`](README.md) is updated together with each ADR.

### Consequences

* Good, because agents and contributors get an authoritative source of rules (`CLAUDE.md` links here).
* Good, because the reasoning behind a decision survives, not only its outcome.
* Bad, because it adds a small amount of writing to architecture-level PRs.

### Confirmation

PR template contains an "ADR needed?" checkbox; the `reviewer` agent checks changes against accepted ADRs.
