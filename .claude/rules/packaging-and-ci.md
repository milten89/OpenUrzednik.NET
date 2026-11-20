---
paths:
  - ".github/**"
  - ".claude/hooks/**"
  - "tests/Scripts/**"
  - "version.json"
  - "OpenUrzednik.Packages.slnf"
  - "OpenUrzednik.slnx"
  - "src/**/*.csproj"
  - "assets/branding/**"
---

# Packaging and CI

- The NuGet workflows build and pack `OpenUrzednik.Packages.slnf` (the `src/` projects only).
  - **A new `src/` project must be added to it**, or it isn't published (a new provider: work through `/new-provider`, which lists this and the other places); until its first release it sets an empty `PackageValidationBaselineVersion` (there is no baseline to download), and the back-merge after that release removes it. `.github/scripts/package_guard.py` (`build (net10.0)`; locally `uv run .github/scripts/package_guard.py`) checks the baseline, the empty overrides, the suppressions, the package-validation properties and the strong-name key.
  - Every package gets the version of the root `version.json` (lockstep, ADR-0008); don't add a `version.json` to a project.
  - A new package input outside `src/` goes into both `pathFilters` in `version.json` and the `paths` filter of `nuget-pre-release.yml` (a directory is `dir` in the first and `dir/**` in the second).
- **Public API files:** every `src/` package has `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt` with a `#nullable enable` first line (a missing file fails the build, CS2001), and `PublicAPI/net/` when the .NET builds have members netstandard2.0 lacks (`#if NET`). A new or changed public member goes into `PublicAPI.Unshipped.txt` (the IDE's RS0016 fix writes the line; a removal is a `*REMOVED*<line>` entry there, never an edit of Shipped). Only `/prepare-release` edits `PublicAPI.Shipped.txt` (`ship_public_api.py`, then the back-merge takes `main`'s file); a package that isn't packable yet (`IsPackable=false`) keeps its entries in Unshipped until a release ships it; `package_guard.py` fails a stable `version.json` with unshipped entries. RS0026/RS0027 are off in `src/.editorconfig` because they count the `CancellationToken` default; `OptionalParametersTest` checks the same rule without it.
- Package icons follow `assets/branding/BRANDING.md`: a provider package sets `OpenUrzednikPackageIcon` in its csproj, and its PNG joins both publish lists above.
- **CI job names are required status checks in the GitHub ruleset.** If you rename a job in `.github/workflows/build.yml`, `format.yml`, `dependency-review.yml` or `release-label.yml`, say so: the ruleset must be updated (see `docs/GITHUB-SETUP.md`). The same holds for the `mutation` job of `.github/workflows/mutation.yml`: it runs on every PR into `develop` (only the shards the PR affects, required through the ruleset `develop-mutation`), `main` and `release/*.x`, and becomes required on the last two once the ruleset `release-mutation` exists: the maintainer creates it as `docs/GITHUB-SETUP.md` describes, once a release that contains `mutation.yml` (1.1.0) is merged into `main`. From then on, every release, hotfix and backport PR waits for it.
- **Scripts and workflow steps with logic have edge-case tests** in `tests/Scripts/edge-tests.sh` (`scripts.yml` runs them on PRs that change a workflow, `.github/scripts`, `.claude/hooks`, the `/prepare-release` scripts, `tests/Mutation` or the files the `package_guard.py` cases copy). A new or changed step or script adds its cases there, in the same PR: the normal case, then empty or missing input, unusual names, re-runs, failing tools. Run it on Linux or WSL 2 (CONTRIBUTING, "Testing scripts and workflow steps"). A known, accepted failure goes into `KNOWN_FAILURES` with its reason, never to make a run pass.
