---
status: accepted
date: 2026-11-08
confidence: high
decision-makers: milten89
---

# 0022. Record architecture decisions, frozen at release

## Context and Problem Statement

Many design decisions of OpenUrzednik.NET (error model, dependency policy, target frameworks, release flow) need to be known by everyone who changes the code. Contributors and AI coding agents cannot follow rules that are not written down, and unwritten decisions get silently re-litigated or violated. A record whose body is rewritten after users depend on it loses the reasoning of the earlier decision. [ADR-0001](0001-record-architecture-decisions.md) froze a record as soon as it was accepted, which happened when it was merged into `develop`. On 2026-11-08 [ADR-0021](0021-transport-subclasses-of-condition-errors.md) needed a change a few hours after its merge, before any release had shipped it, and the rule forced a whole superseding record for it. The project has one maintainer, who can't foresee every case a decision meets before the code that implements it is written. How do we keep decisions discoverable and durable, yet correctable until users depend on them?

## Decision Drivers

* Decisions must be readable by humans and by coding agents (Claude Code, Copilot, etc.).
* Low ceremony: one Markdown file per decision, reviewed in a normal PR.
* History must be kept: superseded decisions stay in the repo.
* A reader finds the whole current decision in one record.
* A decision binds the work on `develop` as soon as it is approved.
* A decision that no release has shipped yet can be corrected cheaply, because no user depends on it; once released, it is frozen.

## Considered Options

* MADR files in `docs/adr/`, with the status `released`: accepted records are corrected in place until a release ships them, then frozen
* MADR files in `docs/adr/`, frozen when accepted, i.e. when merged into `develop` (ADR-0001)
* MADR files in `docs/adr/`, `proposed` until a release ships them, then `accepted`
* Minimal Nygard-style ADRs
* A single `DECISIONS.md` log

## Decision Outcome

Chosen option: the first, because it keeps MADR's widely known structure, makes an approved decision binding at once, and freezes it when users can first depend on it: a release. Python's PEPs (Accepted, then Final once the implementation is merged) and Kubernetes' KEPs (`implementable`, then `implemented`) likewise separate an approved design from a completed one. The log stays append-only for released records, as Microsoft's guidance on maintaining an ADR recommends.

Rules:

* **When an ADR is needed:** for a decision that shapes the architecture or is hard to reverse (public API shape, the error or telemetry contract, dependencies, target frameworks, the release and support flow). The ADR comes in the same PR as the change, written before or with the code, also when the decision was settled in chat. Every change that contradicts an ADR comes with a new or corrected ADR in the same PR.
* **Files:** `NNNN-kebab-case-title.md`; numbers are sequential and never reused. A number is used once a record with it is merged into `develop`; a number that only an unmerged PR proposed is free. Start from [`template.md`](template.md). The index in [`README.md`](README.md) is updated together with each ADR.
* **Statuses:** `proposed`, `accepted`, `released`, `rejected` or `superseded by ADR-NNNN` (`deprecated` is not used).
  * `proposed`: written, not approved yet. It may change freely.
  * `accepted`: the maintainer approved it. It becomes `accepted` when the maintainer merges it to `develop`; when the maintainer approved the decision beforehand (e.g. in the PR's discussion or in a chat), the PR may set `accepted` itself. An accepted ADR is binding for the work on `develop`.
  * `released`: a release shipped it. The front matter adds `released-in: vX.Y.Z`, the first release that shipped it.
  * `rejected` and `superseded by ADR-NNNN` as in MADR. A record superseded or rejected before any release shipped it gets no `released-in`.
* **Correcting an accepted ADR:** until a release ships it, an accepted ADR is corrected in place, in a PR, when the implementation or a review shows a better rule. The body stays one whole, current decision; each correction adds a dated line under **More Information** → `Corrections before release` saying what changed and why, so the history survives in the record and in git. When the decision itself is replaced rather than corrected, a new ADR supersedes it, as for a released one.
* **When a record counts as released:** once a branch that ships it is cut: a `release/X.Y` branch, a `hotfix/…` branch, or a backport branch into `release/N.x` ([ADR-0008](0008-branching-versioning-and-release.md)). From then on it is frozen, also on `develop` before the back-merge shows its new status.
* **Releasing:** the release, hotfix or backport branch sets `status: released` and `released-in: v<version>` on every `accepted` ADR it ships, and changes their rows in the index; process decisions (CI, branching, this record) are released the same way. `/prepare-release` does it for releases and checks that no ADR on the branch is still `accepted` or `proposed`.
* **Frozen when released:** a released, rejected or superseded ADR is never edited, except its status line (`superseded by ADR-NNNN`), the `released-in` field, and a link to the new record.
* **Changing a released decision:** a new ADR that restates the whole current decision, so a reader never combines two ADRs, and supersedes the old one; the two link to each other. There are no partial "amends" records.
* **Confidence:** the front matter records `confidence: high`, `medium` or `low`, so that a low-confidence decision can be found when it is reconsidered.
* **Follow-ups and clarifications:** work an ADR leaves open is tracked in issues, which the ADR links. A clarification that changes no decision goes into `CLAUDE.md` or the linked docs, not into the ADR.
* **No design guides:** details that change often (CI checks, path lists, release steps) live in [`docs/GITHUB-SETUP.md`](../GITHUB-SETUP.md) and [`docs/RELEASE-PROCESS.md`](../RELEASE-PROCESS.md), and the ADR links to them.
* **Existing records:** once this record is merged, a follow-up PR marks the ADRs that shipped in `v1.0.0` (0001 to 0015), which were all accepted then: each gets `released-in: v1.0.0`, and those still accepted become `released`. Until then, and in this record's own PR, [ADR-0001](0001-record-architecture-decisions.md)'s rules apply to them, and any record in the `v1.0.0` tag counts as released. The ADRs added since `v1.0.0` stay `accepted` until the next release.

### Consequences

* Good, because agents and contributors get an authoritative source of rules (`CLAUDE.md` links here).
* Good, because the reasoning behind a decision survives, not only its outcome.
* Good, because each record holds the whole current decision, and the superseded records keep the history.
* Good, because a decision learned wrong during its implementation is corrected in its own record, without a superseding record nobody depends on.
* Good, because the status shows which decisions users already rely on (`released`) and which can still change (`accepted`).
* Bad, because it adds a small amount of writing to architecture-level PRs, and a dated line per correction.
* Bad, because a correction in place hides the earlier wording from a reader of the current file; the dated line and git history keep it.
* Bad, because each release, hotfix and backport adds a step that marks the shipped ADRs.
* Bad, because a correction to a released decision still needs a new ADR that restates the whole decision.

### Confirmation

The PR template asks whether the change complies with the ADRs or adds or corrects an ADR for a new or changed decision. The `reviewer` agent checks changes against the ADRs and flags an edit to a released, rejected or superseded record (beyond its status line, `released-in` and the link), a correction to an accepted record without its dated line, a correction to a record that a release, hotfix or backport branch already contains, and an `accepted` or `proposed` ADR on such a branch. `/prepare-release` marks the accepted ADRs `released` and checks that none is left `accepted` or `proposed`.

## Pros and Cons of the Options

### MADR, with the status `released` (chosen)

* Good, because MADR is widely known and has enough structure (drivers, options, consequences, confirmation) without tooling.
* Good, because an approved decision binds at once, yet stays correctable until users depend on it.
* Bad, because each release marks the shipped records, and the index shows one more status.

### MADR, frozen when accepted (ADR-0001)

* Good, because a record never changes after its merge.
* Bad, because a decision corrected before anyone depends on it needs a whole superseding record (ADR-0021, changed on 2026-11-08, hours after its merge).

### MADR, `proposed` until released

* Good, because the statuses stay MADR's own.
* Bad, because a binding decision on `develop` would look unapproved for weeks, and `proposed` would mean both "waiting for approval" and "approved, not shipped".

### Minimal Nygard-style ADRs

* Good, because it is the original, shortest format.
* Bad, because it lacks the drivers, options and confirmation that make the reasoning reviewable.

### A single `DECISIONS.md` log

* Good, because everything is in one file.
* Bad, because one decision can't be linked, superseded or reviewed on its own.

Within the chosen option, how a decision changes:

* **Append-only once released, a changed decision supersedes the old record** (chosen): Microsoft's guidance; each record holds a whole decision, and the old reasoning stays readable.
* **Corrected in place until released, with one dated line per correction** (chosen): the body is rewritten to stay one whole, current decision, and the dated lines are only history.
* **In place, with a dated note per change and the body left as it was** (used before 1.0): the notes piled up, some contradicted each other, and a reader had to combine the body with every later note to know the current rule.
* **Partial "amends" records** that change part of an older one: a reader would again combine several records; Microsoft describes only superseding.

## More Information

* Supersedes [ADR-0001](0001-record-architecture-decisions.md). What changes: the `released` status and `released-in`, corrections in place until a record is released, when a record counts as released, and the marking at release. Restated with a clarification: an ADR is written before or with the code, and a number counts as used once a record with it is merged. Everything else is restated unchanged.
* Microsoft, [Maintain an architecture decision record (ADR)](https://learn.microsoft.com/azure/well-architected/architect-role/architecture-decision-record): the basis for the append-only rule and for recording confidence; it doesn't say when a record becomes accepted.
* Two-stage lifecycles checked on 2026-11-08: Python's [PEP 1](https://peps.python.org/pep-0001/) (Accepted, then Final once the implementation is merged into the main repository; changes while Provisional, or while Accepted with the Steering Council's approval) and Kubernetes' [KEP process](https://github.com/kubernetes/enhancements/blob/master/keps/sig-architecture/0000-kep-process/README.md) (`implementable`, then `implemented`, "no longer actively changed"; merged KEPs are updated in small PRs). Neither ties its final state to a numbered release. Michael Nygard's [original ADRs](https://www.cognitect.com/blog/2011/11/15/documenting-architecture-decisions) and [MADR](https://adr.github.io/madr/) have no shipped state.
* An earlier history: until 1.0, records were changed in place with dated notes; see [ADR-0001](0001-record-architecture-decisions.md#changes-before-10).
