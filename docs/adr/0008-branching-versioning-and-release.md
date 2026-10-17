---
status: accepted
date: 2026-10-17
decision-makers: milten89
---

# 0008. Branching, versioning and release

## Context and Problem Statement

The repository publishes several NuGet packages that depend on each other. We need a predictable flow from a change to a preview package and to a stable release, and repository protections matching a single-maintainer project that accepts outside contributions. This ADR records the existing setup (see [`docs/RELEASE-PROCESS.md`](../RELEASE-PROCESS.md)) and the GitHub configuration ([`docs/GITHUB-SETUP.md`](../GITHUB-SETUP.md)).

## Decision Drivers

* Merged changes to a package produce testable preview packages; changes to docs, tests or CI alone publish nothing.
* Stable releases are deliberate.
* One version for all packages: they reference each other and ship as one family.
* Every release sorts above all of its previews on nuget.org.
* Protections must not block a solo maintainer, but must not be skippable by accident.

## Considered Options

* `feature/*` → `develop` (previews) → `main` (stable), merge commits
* The same flow with squash merges, and hotfixes cherry-picked into `develop`
* Trunk-based on `main` only

## Decision Outcome

Chosen option: "`feature/*` → `develop` → `main`, merge commits".

* `develop` is the default branch. Feature/fix/docs branches are created from `develop` and merged to it through a PR with a merge commit; squash and rebase merging are disabled. Merging to `develop` publishes preview packages when the merge changes a package input (`nuget-pre-release.yml`, whose `paths` filter matches `pathFilters` in `version.json`).
* A release is a PR from a `release/X.Y` branch, made from `develop`, into `main` that removes the preview suffix from the root `version.json` (`"X.Y.0-preview.{height}"` → `"X.Y.0"`). Merging publishes every package, tags `v<version>` and creates one GitHub Release (`nuget-release.yml`).
* Versioning: Nerdbank.GitVersioning with one root `version.json` for all packages (lockstep); projects have no `version.json` of their own. `develop` carries the next release's number plus `-preview.{height}`; after a release it moves to the next minor, or to the next major in the PR that adds a breaking change. Until 1.0.0, develop stays at `0.1.0-preview.{height}`, and breaking changes don't change the version. A hotfix sets the next patch in `version.json` by hand, because NBGV doesn't raise a three-part version. Only `main` is a public ref, so other branches append the commit id (`X.Y.Z-preview.N.g<commit>` on develop, `X.Y.Z-g<commit>` on release and hotfix branches). `pathFilters` limit the height to commits that change package inputs. A `ProjectReference` between packages becomes a NuGet dependency on the same version or later (`>=`). SemVer 2; breaking changes only in a major version.
* After every release or hotfix, `main` is merged back into `develop` through a `merge/vX.Y.Z` branch made from `main` (a PR with a merge commit, labelled `skip-changelog`); never a PR straight from `main`, which would give `develop` the stable version.
* Branch protection (ruleset `protected-branches` on `main` and `develop`): PR required, merge commits only, no linear-history rule, 0 required approvals (solo maintainer), CODEOWNERS review, conversation resolution, required status checks (build for every TFM, format, CodeQL), no force-push, no deletion. Admin bypass only through pull requests.
* Publishing uses NuGet trusted publishing (OIDC). `nuget-release` environment is restricted to `main` and requires maintainer approval; `nuget-pre-release` is restricted to `develop`.
* Branch names: `feature/…`, `fix/…`, `docs/…`, `chore/…`, `release/…`, `hotfix/…`, `merge/…`.

### Consequences

* Good, because every change is validated by the same checks, and changes to a package produce previews.
* Good, because `develop` always contains `main`, so a release PR shows only new work, and the generated release notes list the feature PRs.
* Bad, because every release or hotfix needs an extra back-merge PR.
* Bad, because feature-branch commits enter `develop`'s history, so the NBGV height grows by the longest parent path rather than by one per merge; previews still sort in order.
* Bad, because the CI check names are referenced by the ruleset – renaming jobs requires updating the ruleset in the same change.
* Bad, because the package inputs are listed twice, in two syntaxes: `pathFilters` in `version.json` and the `paths` filter of `nuget-pre-release.yml`. They are kept in sync by hand (CLAUDE.md and the reviewer check it).

### Confirmation

Ruleset and environments are documented in `docs/GITHUB-SETUP.md` and can be compared with `gh api repos/milten89/OpenUrzednik.NET/rulesets`. After each back-merge, `git merge-base --is-ancestor origin/main origin/develop` succeeds. After a docs-only merge to `develop`, `gh run list --workflow nuget-pre-release.yml` shows no new run.

## More Information

**2026-10-04 note (backlog item 25).** The required status checks are now `build` for every TFM, `test (net472)`, `format` and `dependency-review`, plus the CodeQL code scanning rule (query suite `security-and-quality`). Requiring signed commits was tried and removed: it blocked merging PRs whose branch commits weren't signed. The `code_quality` rule was removed: GitHub Code Quality isn't available for the repository. [`GITHUB-SETUP.md`](../GITHUB-SETUP.md) lists the full configuration.

**2026-10-17 change: one version for all packages.** The maintainer replaced independent versions with lockstep versioning ([#70](https://github.com/milten89/OpenUrzednik.NET/issues/70)):

* The per-library `version.json` files said `1.0-preview`, so NBGV put the git height in the patch number. nuget.org already lists `1.0.N-preview.g<commit>` versions (up to `1.0.36-preview.g824c7f3c14` for Nbp), and SemVer sorts `1.0.0` and `1.0.1` below them, so preview users would never be offered the stable release. Previews now carry the number of the release they lead to (`0.1.0-preview.{height}`, the scheme xUnit uses), and every release sorts above its previews: `1.1.0-preview.37 < 1.1.0 < 1.2.0-preview.1`. The 1.0.0 release changes the number instead of removing the suffix; the old versions are unlisted on nuget.org, and the old `v0.1.0-preview` tag is ignored. Unlisting doesn't change how versions sort, so a project already on a `1.0.N` preview must update to 1.0.0 explicitly, and the 1.0 release notes say so.
* The packages couldn't be released one at a time: `OpenUrzednik.Nbp` references Core and Http, the DI package depends on the other five (directly or through Nbp), and a stable package with a prerelease dependency fails the pack (NU5104). The shared build files in every `pathFilters` list re-versioned all packages anyway. Now one version, one tag `v<version>` and one GitHub Release cover every package; `nuget-release.yml` stops before publishing if the packages don't share one stable version.
* Sibling dependencies are minimums (`>=`), so within a major a newer Core must work with an older Nbp. Users should still update all OpenUrzednik packages together.
* The removed `nbgv prepare-release` advice created branches and `-alpha` versions the process doesn't use. [`RELEASE-PROCESS.md`](../RELEASE-PROCESS.md) describes the release steps.

**2026-10-17 change: previews only for package changes** ([#97](https://github.com/milten89/OpenUrzednik.NET/issues/97)). Every merge to `develop` published a preview of every package, because a non-public version carries the commit id. A docs-only merge then published a version with the same height and a different commit id, so those previews sorted in no useful order. `nuget-pre-release.yml` now runs only when a merge changes a file on the `pathFilters` list (`src`, `version.json`, `Directory.Build.props`, `Directory.Packages.props`, `OpenUrzednik.Packages.slnf`, and the packed icons `assets/branding/icon.png` and `assets/branding/icon-nbp.png`), so each published preview sorts above the one before. The two lists must stay the same (a directory is `dir` in `pathFilters` and `dir/**` in `paths`). A Dependabot bump of a test-only package in `Directory.Packages.props` still publishes; that is accepted, because the same file holds the packages' own dependency versions.

**2026-10-17 change: merge commits** ([#79](https://github.com/milten89/OpenUrzednik.NET/issues/79)). With squash-only merges, a release PR from `develop` landed on `main` as a new commit that `develop` never contained, and a hotfix landed on `main` only. The two branches would never share history again: every later release PR would show the whole history as changed, and a reviewer diffing against the merge base would see unrelated commits. Every PR is now merged with a merge commit, and `main` comes back into `develop` after each release or hotfix through a `merge/vX.Y.Z` branch ([`RELEASE-PROCESS.md`](../RELEASE-PROCESS.md)), so `develop` always contains `main`.
