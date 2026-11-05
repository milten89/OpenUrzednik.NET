---
status: accepted
date: 2026-11-05
confidence: high
decision-makers: milten89
---

# 0008. Branching, versioning and release

## Context and Problem Statement

The repository publishes several NuGet packages that depend on each other. We need a predictable flow from a change to a preview package and to a stable release, a way to ship security fixes for the previous major version, and repository protections matching a single-maintainer project that accepts outside contributions. How are branches, versions and publishing organised? The steps are in [`docs/RELEASE-PROCESS.md`](../RELEASE-PROCESS.md), and the GitHub configuration in [`docs/GITHUB-SETUP.md`](../GITHUB-SETUP.md).

## Decision Drivers

* Merged changes to a package produce testable preview packages; changes to docs, tests or CI alone publish nothing.
* Stable releases are deliberate.
* One version for all packages: they reference each other and ship as one family.
* Every release sorts above all of its previews on nuget.org.
* Protections must not block a solo maintainer, but must not be skippable by accident.
* After a new major, the previous major gets security fixes for a time ([ADR-0013](0013-version-support-policy.md)), so they need a branch to be released from.

## Considered Options

* `feature/*` → `develop` (previews) → `main` (stable), merge commits
* The same flow with squash merges, and hotfixes cherry-picked into `develop`
* Trunk-based on `main` only
* Independent package versions (one `version.json` per package)

## Decision Outcome

Chosen option: "`feature/*` → `develop` → `main`, merge commits", with one version for all packages rather than "Independent package versions". Merge commits keep `main` inside `develop`'s history, so a release PR shows only new work; one version lets packages that reference each other be released together.

**Branches and protections**

* `develop` is the default branch. Work branches (`feature/…`, `fix/…`, `docs/…`, `chore/…`) are created from `develop` and merged into it through a PR with a merge commit; squash and rebase merging are disabled. The other branch names are `release/…`, `hotfix/…` and `merge/…`.
* `main` and `develop` are protected by a ruleset: a PR is required, merge commits only, 0 required approvals (solo maintainer) with CODEOWNERS review, required status checks, no force-push, no deletion; the admin bypass works only through pull requests. [`GITHUB-SETUP.md`](../GITHUB-SETUP.md) lists the rules and the required checks.

**Versions**

* Nerdbank.GitVersioning reads one root `version.json` for all packages (lockstep); projects have no `version.json` of their own. One tag `v<version>` and one GitHub Release cover every package, and `nuget-release.yml` stops before publishing if the packages don't share one stable version.
* `develop` carries the next release's number plus `-preview.{height}`; after a release it moves to the next minor, or to the next major in the PR that adds a breaking change. Previews carry the number of the release they lead to (the scheme xUnit uses), so every release sorts above its previews (`1.1.0-preview.37 < 1.1.0 < 1.2.0-preview.1`); a version with the git height in the patch number (`1.0.N-preview`) would sort above the release `1.0.0`. Until 1.0.0, `develop` stays at `0.1.0-preview.{height}` and breaking changes don't change the version; the 1.0.0 release changes the number instead of removing the suffix.
* A hotfix sets the next patch in `version.json` by hand, because NBGV doesn't raise a three-part version.
* `main` is a public ref (NBGV `publicReleaseRefSpec`), and so is a support branch (below). Other branches append the commit id (`X.Y.Z-preview.N.g<commit>` on `develop`, `X.Y.Z-g<commit>` on release and hotfix branches). `pathFilters` limit the height to commits that change package inputs.
* The assembly version is the major version only (`assemblyVersion.precision: major`, [ADR-0005](0005-target-frameworks.md)), so it changes only in a major release.
* A `ProjectReference` between packages becomes a NuGet dependency on the same version or later (`>=`), so within a major a newer Core must work with an older Nbp. Users should still update all OpenUrzednik packages together.
* SemVer 2; breaking changes only in a major version ([ADR-0011](0011-compatibility-policy.md)).

**Previews**

* Merging to `develop` publishes a preview of every package only when the merge changes a package input: the `paths` filter of `nuget-pre-release.yml` is the `pathFilters` list of `version.json`, so each published preview sorts above the one before. [`RELEASE-PROCESS.md`](../RELEASE-PROCESS.md) lists the package inputs.

**Releases and hotfixes**

* A release is a PR from a `release/X.Y` branch, made from `develop`, into `main` that removes the preview suffix from the root `version.json` (`"X.Y.0-preview.{height}"` → `"X.Y.0"`). Merging publishes every package, tags `v<version>` and creates one GitHub Release (`nuget-release.yml`).
* A hotfix is a PR from a `hotfix/…` branch, made from the tag of the released version, into `main`, with the next patch version.
* Every release and hotfix adds its release notes (`docs/release-notes/v<version>.md`); CI checks the file on the PR and again before publishing.
* After every release or hotfix, `main` is merged back into `develop` through a `merge/vX.Y.Z` branch made from `main` (a PR with a merge commit, labelled `skip-changelog`); never a PR straight from `main`, which would give `develop` the stable version.
* Publishing uses NuGet trusted publishing (OIDC). The `nuget-release` environment publishes only from the public refs and requires maintainer approval; `nuget-pre-release` is restricted to `develop`.

**Support line for the previous major**

* When a new major version ships, a support branch `release/N.x` is made from the last `vN.*` tag of the previous major N (e.g. `release/1.x` when 2.0.0 ships). It is a public ref, publishes stable `N.x.y` versions, and takes only security fixes, through `hotfix/…` PRs into `release/N.x`, for 6 months (the support window is [ADR-0013](0013-version-support-policy.md)). CI, the ruleset and the release-notes check cover it like `main`.

[`RELEASE-PROCESS.md`](../RELEASE-PROCESS.md) has the release, hotfix, back-merge and support-branch steps.

### Consequences

* Good, because every change is validated by the same checks, and changes to a package produce previews.
* Good, because `develop` always contains `main`, so a release PR shows only new work, and the generated release notes list the feature PRs.
* Good, because users of the previous major get security fixes without taking a breaking change.
* Bad, because every release or hotfix needs an extra back-merge PR.
* Bad, because feature-branch commits enter `develop`'s history, so the NBGV height grows by the longest parent path rather than by one per merge; previews still sort in order.
* Bad, because the ruleset matches required checks by CI job name: renaming a job requires updating the ruleset in the same change.
* Bad, because the package inputs are listed twice, in two syntaxes: `pathFilters` in `version.json` and the `paths` filter of `nuget-pre-release.yml` (a directory is `dir` in the first and `dir/**` in the second). They are kept in sync by hand (CLAUDE.md and the reviewer check it).
* Bad, because a Dependabot bump of a test-only package in `Directory.Packages.props` still publishes a preview. That is accepted, because the same file holds the packages' own dependency versions.

### Confirmation

Ruleset and environments are documented in `docs/GITHUB-SETUP.md` and can be compared with `gh api repos/milten89/OpenUrzednik.NET/rulesets`. After each back-merge, `git merge-base --is-ancestor origin/main origin/develop` succeeds. After a docs-only merge to `develop`, `gh run list --workflow nuget-pre-release.yml` shows no new run.

## Pros and Cons of the Options

### The same flow with squash merges, and hotfixes cherry-picked into `develop`

* Bad, because a release PR lands on `main` as a new commit that `develop` never contained, and a hotfix lands on `main` only. The two branches never share history again: every later release PR shows the whole history as changed, and a reviewer diffing against the merge base sees unrelated commits.

### Independent package versions

* Bad, because packages reference each other (`OpenUrzednik.Nbp` references Core and the shared HTTP layer, the DI package depends on the others directly or through Nbp), and a stable package with a prerelease sibling dependency fails the pack (NU5104), so they can't be released one at a time.
* Bad, because the shared build files are package inputs of every package, so a change to them re-versions all packages anyway.

## More Information

* The support branch's setup (`publicReleaseRefSpec`, the release workflow's trigger, the publishing environment's branch rule, CI triggers and ruleset protection) is implemented before 2.0 in [#119](https://github.com/milten89/OpenUrzednik.NET/issues/119).

### Changes before 1.0

Until 1.0 this record was changed in place; the full notes are in [the version before the consolidation](https://github.com/milten89/OpenUrzednik.NET/blob/8e72c4d274de2c92fa348641db6ad9d1b52b220b/docs/adr/0008-branching-versioning-and-release.md#more-information).

* 2026-10-04 · backlog item 25 · Required status checks extended (`test (net472)`, `dependency-review`, CodeQL); required signed commits and the code quality rule removed; GITHUB-SETUP.md lists the full configuration.
* 2026-10-17 · [#70](https://github.com/milten89/OpenUrzednik.NET/issues/70) · One version for all packages, with previews numbered after the release they lead to, one tag and one GitHub Release.
* 2026-10-17 · [#97](https://github.com/milten89/OpenUrzednik.NET/issues/97) · Previews are published only when a merge changes a package input.
* 2026-10-17 · [#79](https://github.com/milten89/OpenUrzednik.NET/issues/79) · Merge commits instead of squash merges, and a back-merge of `main` into `develop` after every release or hotfix.
* 2026-11-05 · [#111](https://github.com/milten89/OpenUrzednik.NET/issues/111), [#119](https://github.com/milten89/OpenUrzednik.NET/issues/119) · Support branch `release/N.x` for security fixes to the previous major.
