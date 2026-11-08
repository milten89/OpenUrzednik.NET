---
status: superseded by ADR-0022
released-in: v1.0.0
date: 2026-11-05
confidence: high
decision-makers: milten89
---

# 0001. Record architecture decisions

## Context and Problem Statement

Many design decisions of OpenUrzednik.NET (error model, dependency policy, target frameworks, release flow) need to be known by everyone who changes the code. Contributors and AI coding agents cannot follow rules that are not written down, and unwritten decisions get silently re-litigated or violated. A record that is edited after the fact loses the reasoning of the earlier decision. How do we keep decisions discoverable and durable?

## Decision Drivers

* Decisions must be readable by humans and by coding agents (Claude Code, Copilot, etc.).
* Low ceremony: one Markdown file per decision, reviewed in a normal PR.
* History must be kept: superseded decisions stay in the repo.
* A reader finds the whole current decision in one record.

## Considered Options

* MADR (Markdown Any Decision Records) files in `docs/adr/`
* Minimal Nygard-style ADRs
* A single `DECISIONS.md` log

## Decision Outcome

Chosen option: "MADR in `docs/adr/`", because it is a widely known format with enough structure (drivers, options, consequences, confirmation) without heavy tooling. The log is append-only, as Microsoft's guidance on maintaining an ADR recommends.

Rules:

* **When an ADR is needed:** for a decision that shapes the architecture or is hard to reverse (public API shape, the error or telemetry contract, dependencies, target frameworks, the release and support flow). The ADR comes in the same PR as the change, also when the decision was settled in chat. Every change that contradicts an accepted ADR comes with a new ADR in the same PR.
* **Files:** `NNNN-kebab-case-title.md`; numbers are sequential and never reused. Start from [`template.md`](template.md). The index in [`README.md`](README.md) is updated together with each ADR.
* **Status:** `proposed`, `accepted`, `rejected` or `superseded by ADR-NNNN` (`deprecated` is not used). A new ADR starts as `proposed` and becomes `accepted` when the maintainer merges it to `develop`; when the maintainer approved the decision beforehand (e.g. in the PR's discussion or in a chat), the PR may set `accepted` itself.
* **Confidence:** the front matter records `confidence: high`, `medium` or `low`, so that a low-confidence decision can be found when it is reconsidered.
* **Append-only:** accepted and rejected ADRs are never edited, except the status line (`superseded by ADR-NNNN`) and a link to the new record.
* **Changing a decision:** a changed decision gets a new ADR that restates the whole current decision, so a reader never combines two ADRs, and supersedes the old one; the two link to each other. There are no partial "amends" records.
* **Follow-ups and clarifications:** work an ADR leaves open is tracked in issues, which the ADR links. A clarification that changes no decision goes into `CLAUDE.md` or the linked docs, not into the ADR.
* **No design guides:** details that change often (CI checks, path lists, release steps) live in [`docs/GITHUB-SETUP.md`](../GITHUB-SETUP.md) and [`docs/RELEASE-PROCESS.md`](../RELEASE-PROCESS.md), and the ADR links to them.

### Consequences

* Good, because agents and contributors get an authoritative source of rules (`CLAUDE.md` links here).
* Good, because the reasoning behind a decision survives, not only its outcome.
* Good, because each record holds the whole current decision, and the superseded records keep the history.
* Bad, because it adds a small amount of writing to architecture-level PRs.
* Bad, because a correction to an accepted decision needs a new ADR that restates the whole decision.

### Confirmation

The PR template asks whether the change complies with the ADRs or adds a new ADR for a new or changed decision; the `reviewer` agent checks changes against accepted ADRs.

## Pros and Cons of the Options

### MADR (Markdown Any Decision Records) files in `docs/adr/`

* Good, because it is widely known.
* Good, because it has enough structure (drivers, options, consequences, confirmation) without tooling.

Within the chosen option, how a decision changes:

* **Append-only, a changed decision supersedes the old record** (chosen): Microsoft's guidance; each record holds a whole decision, and the old reasoning stays readable.
* **In place, with a dated note per change** (used before 1.0): the notes piled up, some contradicted each other, and a reader had to combine the body with every later note to know the current rule.
* **Partial "amends" records** that change part of an older one: a reader would again combine several records; Microsoft describes only superseding.

## More Information

* Superseded by [ADR-0022](0022-record-architecture-decisions-frozen-at-release.md).
* Microsoft, [Maintain an architecture decision record (ADR)](https://learn.microsoft.com/azure/well-architected/architect-role/architecture-decision-record): the basis for the append-only rule and for recording confidence.
* [MADR](https://adr.github.io/madr/).

### Changes before 1.0

Until 1.0 this record was changed in place; the full notes are in [the version before the consolidation](https://github.com/milten89/OpenUrzednik.NET/blob/8e72c4d274de2c92fa348641db6ad9d1b52b220b/docs/adr/0001-record-architecture-decisions.md#more-information).

* 2026-10-02 · Accepted ADRs could be changed in place with a dated note until 1.0, because the design was still settling and several ADRs needed corrections that weren't new decisions.
* 2026-11-05 · [#111](https://github.com/milten89/OpenUrzednik.NET/issues/111) · The notes of every ADR were folded into its body, and in-place changes ended: the log is append-only, with `confidence` in the front matter.
