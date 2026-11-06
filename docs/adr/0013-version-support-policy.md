---
status: accepted
date: 2026-11-05
confidence: high
decision-makers: milten89
---

# 0013. Version support policy

## Context and Problem Statement

The packages share one version and follow SemVer: a minor or patch version doesn't break the public API ([ADR-0011](0011-compatibility-policy.md)), so within a major version users can always move to the latest minor. A new major version may break, and some users can't take it at once, e.g. a .NET Framework app that can't take a breaking change. The project has one maintainer. Which versions get fixes, and for how long?

## Decision Drivers

* Within a major version, updating to the latest minor is enough to get every fix ([ADR-0011](0011-compatibility-policy.md)).
* Users who can't move to a new major version yet still get security fixes.
* The support work stays within what one maintainer can sustain.

## Considered Options

* Latest minor, plus the previous major security-only for 6 months
* Latest version only
* A longer window for the previous major (12 months, or until the major after next)
* Bug fixes on the previous major too

## Decision Outcome

Chosen option: "Latest minor, plus the previous major security-only for 6 months", because users who can't take a breaking change yet still get security fixes, while the support branch stays short-lived and takes only security fixes.

* Fixes go to the latest minor version of the latest major version, as a new patch version (e.g. `1.4.2` after `1.4.1`). To get a fix, users update to the latest minor. Older minor versions get no fixes, and previews aren't supported.
* When a new major version is released, the previous major version gets security fixes for 6 months, released from its support branch `release/N.x` ([ADR-0008](0008-branching-versioning-and-release.md)). It gets no other fixes.
* Older major versions get no fixes.
* How to report a vulnerability, and how fast it is handled, is in [`SECURITY.md`](../../SECURITY.md).

### Consequences

* Good, because users of the previous major get security fixes without taking a breaking change at once.
* Good, because the support branch exists for 6 months and takes only security fixes.
* Bad, because users of an older minor version have to update to the latest minor to get a fix.
* Bad, because bug fixes that aren't security fixes don't reach the previous major.

### Confirmation

The "Supported versions" table in [`SECURITY.md`](../../SECURITY.md), the README's "Versioning and support" section and "Support for the previous major version" in [`RELEASE-PROCESS.md`](../RELEASE-PROCESS.md) match this ADR; the `reviewer` agent checks it.

## Pros and Cons of the Options

### Latest version only

* Bad, because users stuck on the old major (e.g. a .NET Framework app that can't take a breaking change) get no security fixes.

### A longer window for the previous major

* Bad, because the support branch lives two or more times longer, which is more work than one maintainer can sustain.

### Bug fixes on the previous major too

* Bad, because every fix is done twice, and moving to the new major slows down.

## More Information

* Decided with the maintainer on 2026-11-05. The policy was first published in `SECURITY.md` and the README ([#102](https://github.com/milten89/OpenUrzednik.NET/issues/102)); this record states the decision behind it.
* [ADR-0008](0008-branching-versioning-and-release.md) decides the support branch; [`RELEASE-PROCESS.md`](../RELEASE-PROCESS.md) has its steps, implemented before 2.0 in [#119](https://github.com/milten89/OpenUrzednik.NET/issues/119).
* [ADR-0011](0011-compatibility-policy.md) decides what a major, minor and patch version may change.
