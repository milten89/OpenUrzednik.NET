---
paths:
  - ".github/**"
  - "version.json"
  - "OpenUrzednik.Packages.slnf"
  - "OpenUrzednik.slnx"
  - "src/**/*.csproj"
  - "assets/branding/**"
---

# Packaging and CI

- The NuGet workflows build and pack `OpenUrzednik.Packages.slnf` (the `src/` projects only).
  - **A new `src/` project must be added to it**, or it isn't published (a new provider: work through `/new-provider`, which lists this and the other places); until its first release it sets an empty `PackageValidationBaselineVersion` (there is no baseline to download).
  - Every package gets the version of the root `version.json` (lockstep, ADR-0008); don't add a `version.json` to a project.
  - A new package input outside `src/` goes into both `pathFilters` in `version.json` and the `paths` filter of `nuget-pre-release.yml` (a directory is `dir` in the first and `dir/**` in the second).
- Package icons follow `assets/branding/BRANDING.md`: a provider package sets `OpenUrzednikPackageIcon` in its csproj, and its PNG joins both publish lists above.
- **CI job names are required status checks in the GitHub ruleset.** If you rename a job in `.github/workflows/build.yml`, `format.yml`, `dependency-review.yml` or `release-label.yml`, say so: the ruleset must be updated (see `docs/GITHUB-SETUP.md`). The same holds for the `mutation` job of `.github/workflows/mutation.yml`, required on PRs into `main` and `release/*.x` by the ruleset `release-mutation`: every release, hotfix and backport PR waits for it.
