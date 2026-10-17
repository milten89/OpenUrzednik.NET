# Versioning and release process

## Branch structure

- **`main`** — protected, PR-only. Always contains the code of the currently supported release version.
- **`develop`** — protected, PR-only. All feature work lands here; publishes pre-release packages.
- **`release/X.Y`** — made from `develop` for a release; its PR goes into `main`.
- **`hotfix/…`** — made from a release tag for a fix to the released version; its PR goes into `main`.
- PR merge strategy: **squash**.

## Versioning

All published packages share one version (lockstep, [ADR-0008](adr/0008-branching-versioning-and-release.md)). It comes from the root `version.json`, which [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning) (NBGV) reads; projects have no `version.json` of their own.

- **`develop`** carries the number of the next release plus a preview suffix: `"version": "0.1.0-preview.{height}"`. `{height}` is the git height: the number of commits from the one that set this version up to the one being built, both included, counting only commits that change a package input listed in `pathFilters` (`src`, `version.json`, `Directory.Build.props`, `Directory.Packages.props`, `OpenUrzednik.Packages.slnf`, and the packed icons `assets/branding/icon.png` and `assets/branding/icon-nbp.png`).
- **Only `main` is a public ref** (`publicReleaseRefSpec`). Every other branch, `develop` and release branches included, appends the commit id: `develop` publishes versions like `0.1.0-preview.12.g1a2b3c4d5e`.
- **A release removes the suffix**: `"1.1.0-preview.{height}"` → `"1.1.0"`. SemVer sorts a prerelease below the release with the same number, so every release sorts above its previews:

  `1.1.0-preview.1 < … < 1.1.0-preview.37 < 1.1.0 < 1.1.1 < 1.2.0-preview.1`

- **After a release**, `develop` moves to the next minor (`"1.2.0-preview.{height}"`). A PR that adds a breaking change moves it to the next major instead (`"2.0.0-preview.{height}"`). Until 1.0.0, `develop` stays at `"0.1.0-preview.{height}"`, and breaking changes don't change the version.
- **Dependencies between the packages are minimums.** A `ProjectReference` becomes a NuGet dependency on the same version or later: `OpenUrzednik.Nbp` 1.1.0 depends on `OpenUrzednik.Core` >= 1.1.0. Update all OpenUrzednik packages together.
- **1.0.0 is the one exception**: that release changes the number (`"0.1.0-preview.{height}"` → `"1.0.0"`) instead of removing the suffix. The versions published before this scheme (`0.1.0-preview`, `1.0.0-preview.N`, `1.0.N-preview.g<commit>`) are unlisted on nuget.org, and the `v0.1.0-preview` tag from that time is ignored. Unlisting doesn't change how versions sort: a project already on a `1.0.N` preview must update to 1.0.0 explicitly.

## Day-to-day flow

1. Work happens on feature branches → PR (squash) into `develop`.
2. Merge into `develop` → if the merge changes a package input, `nuget-pre-release.yml` publishes a preview of every package, all with the same, new version. Its `paths` filter is the `pathFilters` list, so merges that change only docs, tests or CI publish nothing, and each published preview sorts above the one before. A Dependabot bump of a test-only package still publishes a preview, because `Directory.Packages.props` also holds the versions of the packages' own dependencies.
3. A release follows the steps below.

## Releasing

1. Create `release/X.Y` from the latest `develop`.
2. In the root `version.json`, remove the preview suffix:

   ```diff
   - "version": "1.1.0-preview.{height}",
   + "version": "1.1.0",
   ```

3. Check the packages locally. Builds of the release branch still get `1.1.0-g<commit id>`, because only `main` is public, so pack with `PublicRelease=true` into a folder that doesn't exist yet (delete `artifacts/release` first):

   ```bash
   dotnet pack OpenUrzednik.Packages.slnf -c Release -o artifacts/release -p:ContinuousIntegrationBuild=true -p:PublicRelease=true
   ```

   Every `.nupkg` in `artifacts/release` must have the version `1.1.0`.
4. Open a PR from `release/X.Y` into `main`, once the release's milestone has no open issues ([`GITHUB-SETUP.md`](GITHUB-SETUP.md#milestones)).
5. Merge → `nuget-release.yml` publishes every package as `1.1.0`, tags the repo `v1.1.0` and creates one GitHub Release. It stops before publishing if the version still has a prerelease suffix, if the packages don't share one version, or if that version was already released from another commit.
6. Bring `main` back into `develop`, and set `develop`'s `version.json` to the next minor (`"1.2.0-preview.{height}"`).

## Hotfix procedure (main = 1.0, develop = 1.1)

We assume full API compatibility between `1.0` and `1.1` (per SemVer — a minor bump must not
break compatibility), so there is **no need for a separate support line or for NBGV
branch-naming**. The full process:

1. Create a branch from the tag of the last released version:

   ```bash
   git checkout -b hotfix/nbp-bug123 v1.0.0
   ```

2. Set the root `version.json` to the next patch, `"1.0.1"`. NBGV doesn't raise the patch by itself: a version with three parts is used as written, and the git height goes only into the assembly file version.
3. Fix the bug, open a PR **hotfix → `main`** (required, since `main` is protected).
4. Merge → `nuget-release.yml` publishes `1.0.1`, tags `v1.0.1`, creates the GitHub Release.
5. Open a separate PR / cherry-pick of the same fix **into `develop`**, so the bug doesn't resurface in the upcoming `1.1` (this is not a release — it's just propagating the change; `develop` will publish another preview build containing the fix).

> If a breaking change (`2.0`) is introduced in the future and parallel support for an older `1.x` line becomes necessary, that's the point to revisit NBGV's branch-naming mechanism (`release.branchName`). It's deliberately left out for now as unneeded complexity.

## Dev-only dependencies vs. the published package

`Nerdbank.GitVersioning` must have `PrivateAssets="all"` so it doesn't end up as a dependency of the published package. It is referenced once for all libraries in `src/Directory.Build.props`; the version is managed centrally in `Directory.Packages.props`:

```xml
<PackageReference Include="Nerdbank.GitVersioning" PrivateAssets="all" />
```

## Repository protections

Branch protection, required status checks and publishing environments are described in [`GITHUB-SETUP.md`](GITHUB-SETUP.md). The decision behind this flow is [ADR-0008](adr/0008-branching-versioning-and-release.md).
