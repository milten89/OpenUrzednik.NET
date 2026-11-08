# Versioning and release process

## Branch structure

- **`main`** — protected, PR-only. Always contains the code of the latest release.
- **`develop`** — protected, PR-only. All feature work lands here; publishes pre-release packages.
- **`release/X.Y`** — made from `develop` for a release; its PR goes into `main`.
- **`hotfix/…`** — made from a release tag for a fix to the released version, its PR into `main`; or, for a security backport (`hotfix/N.x-…`), made from `release/N.x`, its PR into `release/N.x`.
- **`release/N.x`** — support branch of the previous major (e.g. `release/1.x` after `2.0.0`), made from its last `vN.*` tag; protected, PR-only; publishes stable `N.Y.Z` versions; only security fixes, for 6 months ([below](#support-for-the-previous-major-version)).
- **`merge/vX.Y.Z`** — made from `main` after a release or hotfix, to bring `main` back into `develop`.
- PR merge strategy: **merge commits** (squash and rebase are disabled).

## Versioning

All published packages share one version (lockstep, [ADR-0008](adr/0008-branching-versioning-and-release.md)). It comes from the root `version.json`, which [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning) (NBGV) reads; projects have no `version.json` of their own.

- **`develop`** carries the number of the next release plus a preview suffix: `"version": "0.1.0-preview.{height}"`. `{height}` is the git height: the number of commits from the one that set this version up to the one being built, both included, counting only commits that change a package input listed in `pathFilters` (`src`, `version.json`, `Directory.Build.props`, `Directory.Packages.props`, `OpenUrzednik.Packages.slnf`, the strong-name key `OpenUrzednik.snk`, and the packed icons `assets/branding/icon.png` and `assets/branding/icon-nbp.png`).
- **Only `main` and the support branches `release/N.x` are public refs** (`publicReleaseRefSpec`). Every other branch, `develop` and the `release/X.Y` branches included, appends the commit id: `develop` publishes versions like `0.1.0-preview.12.g1a2b3c4d5e`.
- **A release removes the suffix**: `"1.1.0-preview.{height}"` → `"1.1.0"`. SemVer sorts a prerelease below the release with the same number, so every release sorts above its previews:

  `1.1.0-preview.1 < … < 1.1.0-preview.37 < 1.1.0 < 1.1.1 < 1.2.0-preview.1`

- **After a release**, `develop` moves to the next minor (`"1.2.0-preview.{height}"`). A PR that adds a breaking change moves it to the next major instead (`"2.0.0-preview.{height}"`). Until 1.0.0, `develop` stays at `"0.1.0-preview.{height}"`, and breaking changes don't change the version.
- **Dependencies between the packages are minimums.** A `ProjectReference` becomes a NuGet dependency on the same version or later: `OpenUrzednik.Nbp` 1.1.0 depends on `OpenUrzednik.Core` >= 1.1.0. Update all OpenUrzednik packages together.
- **1.0.0 is the one exception**: that release changes the number (`"0.1.0-preview.{height}"` → `"1.0.0"`) instead of removing the suffix. The versions published before this scheme (`0.1.0-preview`, `1.0.0-preview.N`, `1.0.N-preview.g<commit>`) are unlisted on nuget.org, and the `v0.1.0-preview` tag from that time is ignored. Unlisting doesn't change how versions sort: a project already on a `1.0.N` preview must update to 1.0.0 explicitly.

## Day-to-day flow

1. Work happens on feature branches → PR (merge commit) into `develop`.
2. Merge into `develop` → if the merge changes a package input, `nuget-pre-release.yml` publishes a preview of every package, all with the same, new version. Its `paths` filter is the `pathFilters` list, so merges that change only docs, tests or CI publish nothing, and each published preview sorts above the one before. A Dependabot bump of a test-only package still publishes a preview, because `Directory.Packages.props` also holds the versions of the packages' own dependencies.
3. A release follows the steps below.

## Releasing

With Claude Code, `/prepare-release` runs steps 1–5 with extra checks, and `/prepare-release after` checks the published packages and does step 7. The steps below are the same process by hand.

1. Create `release/X.Y` from the latest `develop`.
2. In the root `version.json`, remove the preview suffix:

   ```diff
   - "version": "1.1.0-preview.{height}",
   + "version": "1.1.0",
   ```

   In the same commit, mark the ADRs the release ships ([ADR-0022](adr/0022-record-architecture-decisions-frozen-at-release.md)): every `accepted` ADR gets `status: released` and `released-in: v1.1.0`, and its row in `docs/adr/README.md` says `released (v1.1.0)`. From the moment the branch is cut they are frozen, also on `develop` until the back-merge.

3. Check the packages locally. Builds of the release branch still get `1.1.0-g<commit id>`, because `release/X.Y` isn't a public ref, so pack with `PublicRelease=true` into a folder that doesn't exist yet (delete `artifacts/release` first):

   ```bash
   dotnet pack OpenUrzednik.Packages.slnf -c Release -o artifacts/release -p:ContinuousIntegrationBuild=true -p:PublicRelease=true
   ```

   Every `.nupkg` in `artifacts/release` must have the version `1.1.0`.
4. Push the branch, then write the release notes: run `/release-notes 1.1.0`, review `docs/release-notes/v1.1.0.md`, commit and push it. They come from the release labels of the PRs since the last release; the packages' `releaseNotes` link to the GitHub Release made from this file.
5. Open a PR from `release/X.Y` into `main` (label `skip-changelog`; its contents are already listed by their own PRs), once the release's milestone has no open issues ([`GITHUB-SETUP.md`](GITHUB-SETUP.md#milestones)). The `build (net10.0)` job fails the PR if `version.json` has a prerelease suffix or the notes file is missing. The PR also runs the mutation tests: the `mutation` check is required on `main` ([ADR-0020](adr/0020-mutation-testing-of-every-target-branch.md)). If a shard fails, its job summary names the undetected mutants; fix the tests on `develop` and update the release branch, or on the release branch when the gap is the release's own.
6. Merge (merge commit) → `nuget-release.yml` publishes every package as `1.1.0`, tags the repo `v1.1.0` and creates one GitHub Release with the notes file as its body. It stops before publishing if the version still has a prerelease suffix, if the packages don't share one version, if that version was already released from another commit, or if the notes file is missing.
7. Back-merge (below), setting `develop`'s `version.json` to the next minor (`"1.2.0-preview.{height}"`).

## Hotfix procedure (main = 1.0, develop = 1.1)

We assume full API compatibility between `1.0` and `1.1` (per SemVer — a minor bump must not
break compatibility), so within one major version there is **no need for a separate support line or for NBGV
branch-naming**. For the previous major version, see [Support for the previous major version](#support-for-the-previous-major-version). The full process:

1. Create a branch from the tag of the last released version:

   ```bash
   git checkout -b hotfix/nbp-bug123 v1.0.0
   ```

2. Set the root `version.json` to the next patch, `"1.0.1"`. NBGV doesn't raise the patch by itself: a version with three parts is used as written, and the git height goes only into the assembly file version. Set `PackageValidationBaselineVersion` in `src/Directory.Build.props` to the tag's version (`1.0.0`): at the tag it still names the release before, because the back-merge raises it only on `develop`. Then refresh the suppression files (see [the back-merge](#back-merge-after-a-release-or-hotfix)).
3. Fix the bug and open a PR **hotfix → `main`** (required, since `main` is protected), labelled `bug`. An ADR the hotfix adds or ships is marked `released` with `released-in: v1.0.1` in the same PR ([ADR-0022](adr/0022-record-architecture-decisions-frozen-at-release.md)). Then run `/release-notes 1.0.1`, and commit and push `docs/release-notes/v1.0.1.md`: the notes list the hotfix PR itself, so it must exist first, and the PR's `build (net10.0)` job fails until the file is there. The PR waits for the required `mutation` check too ([ADR-0020](adr/0020-mutation-testing-of-every-target-branch.md)): the fix comes with tests that kill its mutants.
4. Merge (merge commit) → `nuget-release.yml` publishes `1.0.1`, tags `v1.0.1`, creates the GitHub Release.
5. Back-merge (below), so the fix reaches `develop`. `develop` keeps its own version (e.g. `"1.1.0-preview.{height}"`).

## Back-merge after a release or hotfix

After every release or hotfix, `main` goes back into `develop`, so `develop` always contains `main` and the next release PR shows only new work:

```bash
git fetch origin
git switch -c merge/v1.1.0 origin/main
git merge --no-commit origin/develop   # on a version.json conflict, keep develop's side (--theirs here)
# after a release: set version.json to the next minor, e.g. "1.2.0-preview.{height}"
#   (unless develop already carries a higher version, e.g. "2.0.0-preview.{height}")
git diff origin/develop -- version.json   # after a hotfix: no output; after a release: only the next-minor change
# set PackageValidationBaselineVersion in src/Directory.Build.props to the released version (e.g. 1.1.0 or 1.0.1)
dotnet pack OpenUrzednik.Packages.slnf -c Release -p:ApiCompatGenerateSuppressionFile=true   # drops suppressions the new baseline no longer needs
git diff -- src   # the suppression files may only lose entries (or be deleted); a new entry means a real break
git commit
git push -u origin merge/v1.1.0
```

Check `version.json` with that `git diff` even when git reports no conflict: after a release, git takes `main`'s stable version silently.

The package-validation baseline moves with each release: from then on, every pack on `develop` compares the packages with the version just published ([ADR-0011](adr/0011-compatibility-policy.md)). The release must be on nuget.org first: the pack downloads the baseline packages. Raising it makes some suppressions unnecessary (the CP0006 of a client-interface method that is now in the baseline, the baseline suppressions of a major just released), and an unnecessary suppression fails the pack. Regenerating the files with `ApiCompatGenerateSuppressionFile` removes exactly those and keeps the ones `develop` still needs (a method added after the release branch was cut, a major in progress). Delete a `CompatibilitySuppressions.xml` that ends up empty.

Open a PR from `merge/v1.1.0` into `develop`, label it `skip-changelog`, and merge it with a merge commit.

**Never open a PR straight from `main` into `develop`:** `develop` would get the stable version from `main`'s `version.json` and publish `1.1.0-g<commit>` previews, and resolving a conflict in GitHub's web editor would commit to `main`.

## Security fix

A vulnerability is reported privately ([`SECURITY.md`](../SECURITY.md)): acknowledge it within 14 days, and aim to publish the fix within 90 days of confirming it.

1. Create a draft **GitHub Security Advisory** for the report (Security → Advisories), and prepare and review the fix privately, e.g. in the advisory's **temporary private fork**, so nothing is public too early.
2. While the previous major is supported and affected, prepare its backport privately too ([below](#support-for-the-previous-major-version)): once the latest line's fix is public, the old line is exposed until its patch ships.
3. When the fixes are ready to ship, push the latest line's fix as a `hotfix/…` branch and release it with the hotfix procedure above. The hotfix PR is public and runs the required checks, so open it only then (`hotfix/…` from the last release tag, the next patch version, release notes, back-merge). Open and release the backport right after it.
4. Publish the advisory once every affected line is patched: ecosystem NuGet, every affected package, the affected and patched version ranges (one patched version per line), a CVE when it is warranted, and credit for the reporter unless they decline.
5. Deprecate the affected versions of every affected package on nuget.org (Manage package → Deprecation, the critical-bugs reason, alternate package: the patched version of the same line).
6. Tell the reporter that the fix and the advisory are out.

## Support for the previous major version

Fixes go to the latest minor of the latest major only ([ADR-0013](adr/0013-version-support-policy.md)). When a new major is released (e.g. `2.0.0`), the previous one (`1.x`) still gets **security fixes for 6 months**, from a support branch `release/1.x` ([ADR-0008](adr/0008-branching-versioning-and-release.md)).

The repository is set up for any `release/N.x` branch: `version.json` makes it a public ref (`^refs/heads/release/\d+\.x$`), so its builds get stable versions; `nuget-release.yml` publishes on a push to it; the CI workflows, CodeQL, the release-notes check and the mutation tests (`mutation`, required by the ruleset `release-mutation`, [ADR-0020](adr/0020-mutation-testing-of-every-target-branch.md)) run on PRs into it; the rulesets protect it and the `nuget-release` environment allows it ([`GITHUB-SETUP.md`](GITHUB-SETUP.md)). A release from it gets a tag and a GitHub Release like any other, but is never marked **Latest**: that stays the newest major.

**When the new major is released**, create the support branch from the previous major's last tag (no PR: nothing changes; the `nuget-release` run of that push is skipped), on GitHub (Branches → New branch, source: the tag) or with git. The ruleset lets it be created although the tagged commit has no status checks (`do_not_enforce_on_create`, [`GITHUB-SETUP.md`](GITHUB-SETUP.md)):

```bash
git fetch origin --tags
git push origin "$(git tag --list 'v1.*' --sort=-v:refname | head -n 1)^{commit}:refs/heads/release/1.x"
```

**A security fix** lands on the latest line first, with the [security fix](#security-fix) steps (a hotfix into `main`; when the vulnerable code isn't in any release of the latest major, a PR into `develop` is enough). Then it is backported:

1. Create a branch from the support branch, and cherry-pick only the commits of the fix itself (not the latest line's `version.json` or release-notes commits). If the vulnerability exists only in `1.x`, make the fix on this branch instead.

   ```bash
   git switch -c hotfix/1.x-cve-2027-1234 origin/release/1.x
   git cherry-pick -x <commit>…   # on a conflict, resolve it and keep the "cherry picked from" line
   ```

2. Set the root `version.json` to the next patch of that line (e.g. `"1.4.3"` after `v1.4.2`), and `PackageValidationBaselineVersion` in `src/Directory.Build.props` to that line's latest release (`1.4.2`). Then refresh the suppression files (see [the back-merge](#back-merge-after-a-release-or-hotfix)).
3. Open a PR **hotfix → `release/1.x`**, labelled `bug`. An ADR the backport adds is marked `released` with `released-in: v1.4.3` in the same PR ([ADR-0022](adr/0022-record-architecture-decisions-frozen-at-release.md)). Then run `/release-notes 1.4.3`, and commit and push `docs/release-notes/v1.4.3.md`; the PR's `build (net10.0)` job fails until the file is there.
4. Merge (merge commit) → `nuget-release.yml` publishes `1.4.3`, tags `v1.4.3` and creates the GitHub Release (not marked Latest).
5. **No back-merge:** `release/1.x` is never merged into `main` or `develop`, which carry the newer major and already have the fix. Its release notes stay on `release/1.x` and in the GitHub Release.

Only security fixes go into the support branch. A CI or tooling change needed to keep it releasable (e.g. an action that no longer runs) goes into the same backport PR: a merge without a new version would fail `nuget-release.yml` as "already released". After the 6 months the branch stays, but gets no more fixes.

## Dev-only dependencies vs. the published package

`Nerdbank.GitVersioning` must have `PrivateAssets="all"` so it doesn't end up as a dependency of the published package. It is referenced once for all libraries in `src/Directory.Build.props`; the version is managed centrally in `Directory.Packages.props`:

```xml
<PackageReference Include="Nerdbank.GitVersioning" PrivateAssets="all" />
```

## Repository protections

Branch protection, required status checks and publishing environments are described in [`GITHUB-SETUP.md`](GITHUB-SETUP.md). The decision behind this flow is [ADR-0008](adr/0008-branching-versioning-and-release.md).
