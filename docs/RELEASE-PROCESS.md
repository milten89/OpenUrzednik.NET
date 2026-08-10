# Versioning and release process

## Branch structure

- **`main`** — protected, PR-only. Always contains the code of the currently supported release version.
- **`develop`** — protected, PR-only. All feature work lands here; publishes pre-release packages.
- PR merge strategy: **squash**.

## `version.json` files

- `/version.json` — fallback, inherited by projects that don't define their own settings.
- `/src/<Library>/version.json` — one file per library, independent version number and status (preview / stable) controlled by the presence of the `-preview` suffix in the `"version"` field.
- `pathFilters` in each file determines which changes (in that folder and in its dependencies, e.g. `Core`) affect that library's version number. No changes in the relevant paths = no increment = same version as last time = `--skip-duplicate` rejects the push without erroring.

## Day-to-day flow

1. Work happens on feature branches → PR (squash) into `develop`.
2. Merge into `develop` → `preview.yml` builds and publishes to NuGet.org only the libraries whose version actually changed (`pathFilters` + `--skip-duplicate`). Since `develop` is not listed in `publicReleaseRefSpec`, versions get a commit hash appended (guaranteeing uniqueness per build).
3. When a library is ready for an official release: open a PR from `develop` into `main` where you **deliberately edit** that library's `version.json` (drop `-preview` and/or bump `minor`).
4. Merge into `main` → `release.yml` publishes the stable version, then automatically:
   - tags the repo as `<Library>/v<version>`,
   - creates a GitHub Release for that tag.

## Promoting preview → stable

In the library's `version.json`:

```diff
- "version": "0.2-preview",
+ "version": "0.2",
```

You can do this by hand, or generate the change locally with a helper command (this only edits the file — the actual release is still handled by `release.yml`):

```bash
nbgv prepare-release --project src/OpenUrzednik.CepikClient --versionIncrement minor
```

Commit the result and open it as a normal PR into `main`.

## Hotfix procedure (main = 1.0, develop = 1.1)

We assume full API compatibility between `1.0` and `1.1` (per SemVer — a minor bump must not
break compatibility), so there is **no need for a separate support line or for NBGV
branch-naming**. The full process:

1. Create a branch from the tag of the last released version:

   ```bash
   git checkout -b hotfix/nbp-client-bug123 nbp-client/v1.0.0
   ```

   `version.json` on this branch inherits the state from `main` at the time of the tag — if `main` is still on `1.0` (i.e. the `1.1` work on `develop` hasn't been merged yet), NBGV will compute the version as `1.0.1` with no extra configuration needed.
2. Fix the bug, open a PR **hotfix → `main`** (required, since `main` is protected).
3. Merge → `release.yml` publishes `1.0.1`, tags it, creates the GitHub Release.
4. Open a separate PR / cherry-pick of the same fix **into `develop`**, so the bug doesn't resurface in the upcoming `1.1` (this is not a release — it's just propagating the change; `develop` will publish another preview build containing the fix).

> If a breaking change (`2.0`) is introduced in the future and parallel support for an older `1.x` line becomes necessary, that's the point to revisit NBGV's branch-naming mechanism (`release.branchName`). It's deliberately left out for now as unneeded complexity.

## Dev-only dependencies vs. the published package

`Nerdbank.GitVersioning` must have `PrivateAssets="all"` in every `.csproj` so it doesn't end up as a dependency of the published package:

```xml
<PackageReference Include="Nerdbank.GitVersioning" Version="3.6.*" PrivateAssets="all" />
```
