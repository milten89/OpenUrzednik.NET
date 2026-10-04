---
status: accepted
date: 2026-10-01
decision-makers: milten89
---

# 0008. Branching, versioning and release

## Context and Problem Statement

The repository publishes several independently versioned NuGet packages. We need a predictable flow from a change to a preview package and to a stable release, and repository protections matching a single-maintainer project that accepts outside contributions. This ADR records the existing setup (see [`docs/RELEASE-PROCESS.md`](../RELEASE-PROCESS.md)) and the GitHub configuration ([`docs/GITHUB-SETUP.md`](../GITHUB-SETUP.md)).

## Decision Drivers

* Every merged change produces a testable preview package.
* Stable releases are deliberate.
* Packages version independently.
* Protections must not block a solo maintainer, but must not be skippable by accident.

## Considered Options

* `feature/*` → `develop` (previews) → `main` (stable), squash merges
* Trunk-based on `main` only

## Decision Outcome

Chosen option: "`feature/*` → `develop` → `main`, squash merges".

* `develop` is the default branch. Feature/fix/docs branches are created from `develop` and merged to it via squash PR. Merging to `develop` publishes preview packages (`nuget-pre-release.yml`).
* A release is a PR `develop` → `main` that deliberately edits the library's `version.json` (drop `-preview` and/or bump). Merging publishes stable packages, tags `<Package>/v<version>` and creates a GitHub Release (`nuget-release.yml`).
* Versioning: Nerdbank.GitVersioning, one `version.json` per library with `pathFilters` for itself and its OpenUrzednik dependencies. SemVer 2; breaking changes only in a major version.
* Branch protection (ruleset `protected-branches` on `main` and `develop`): PR required, squash only, 0 required approvals (solo maintainer), CODEOWNERS review, conversation resolution, required status checks (build for every TFM, format, CodeQL), no force-push, no deletion. Admin bypass only through pull requests.
* Publishing uses NuGet trusted publishing (OIDC). `nuget-release` environment is restricted to `main` and requires maintainer approval; `nuget-pre-release` is restricted to `develop`.
* Branch names: `feature/…`, `fix/…`, `docs/…`, `chore/…`, `hotfix/…`.

### Consequences

* Good, because every change is validated by the same checks and produces a preview.
* Bad, because the CI check names are referenced by the ruleset – renaming jobs requires updating the ruleset in the same change.

### Confirmation

Ruleset and environments are documented in `docs/GITHUB-SETUP.md` and can be compared with `gh api repos/milten89/OpenUrzednik.NET/rulesets`.

**2026-10-04 note (backlog item 25).** The required status checks are now `build` for every TFM, `test (net472)`, `format` and `dependency-review`, plus the CodeQL code scanning rule (query suite `security-and-quality`). The ruleset also requires signed commits; squash merges satisfy it because GitHub signs them. The `code_quality` rule was removed: GitHub Code Quality isn't available for the repository. [`GITHUB-SETUP.md`](../GITHUB-SETUP.md) lists the full configuration.
