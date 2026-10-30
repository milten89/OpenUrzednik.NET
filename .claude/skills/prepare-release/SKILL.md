---
name: prepare-release
description: Prepare an OpenUrzednik.NET release (the release/X.Y branch, version, release notes, package checks and the PR into main), or, with "after", verify a published release and open the back-merge into develop. This is the only way releases are prepared. Use whenever the user asks to release, cut a release, prepare version X.Y.Z, ship 1.0, or check a release that just published. It never publishes: the maintainer merges the PR and approves the nuget-release deployment.
argument-hint: "[version, e.g. 1.1.0] | after [version]"
---

# Prepare a release

A release goes from `develop` through `release/X.Y` into `main`; merging that PR runs `nuget-release.yml`, which publishes every package, tags `v<version>` and creates the GitHub Release ([`docs/RELEASE-PROCESS.md`](../../../docs/RELEASE-PROCESS.md), [ADR-0008](../../../docs/adr/0008-branching-versioning-and-release.md)). This skill does everything up to that PR, and with `after`, everything after the publish. **It never publishes**: never run `dotnet nuget push` (denied), never create the tag or the Release, never merge into `main` or approve a deployment.

Hotfixes aren't covered: follow the hotfix procedure in `docs/RELEASE-PROCESS.md` by hand.

Stop and report at the first failed check, with what failed and what would fix it. Don't work around a check. Repository: `milten89/OpenUrzednik.NET`; examples use bash.

## `/prepare-release [version]`

### 1. Starting point

```bash
git fetch origin --tags
git merge-base --is-ancestor origin/main origin/develop && echo "develop contains main"
gh run list -R milten89/OpenUrzednik.NET --commit "$(git rev-parse origin/develop)" --json workflowName,status,conclusion
gh api "repos/milten89/OpenUrzednik.NET/dependabot/alerts?state=open" --jq length
```

All of these must hold:

- `develop` contains `main`: the last back-merge is done.
- Every workflow run on `origin/develop`'s commit is `completed` with `success` (wait for one still running, such as CodeQL). `build.yml` runs only on PRs (the ruleset requires it before a merge) and inside `nuget-pre-release.yml`, so a merge that changed a package input shows "NuGet - pre-release"; one that changed only docs or CI may show just CodeQL.
- No open Dependabot alerts (`0`).
- The milestone named after the version has no open issues, and no open `bug` issue has Priority Urgent or High in the project:

  ```bash
  gh api repos/milten89/OpenUrzednik.NET/milestones --jq '.[] | select(.title == "<version>") | .open_issues'
  gh project item-list 2 --owner milten89 --format json --limit 200 \
    --jq '[.items[] | select(.content.type == "Issue" and .status != "Done" and ((.labels // []) | index("bug")) and (.priority == "Urgent" or .priority == "High" or .priority == "P0" or .priority == "P1")) | .content.number]'
  ```

  The first prints `0` (nothing if the milestone doesn't exist: ask the user whether that's intended), the second `[]`. Confirm each number the second lists with `gh issue view <n> --json state`: only open issues count.

### 2. Version

- **The version** is the argument, or `version` in `origin/develop`'s `version.json` without the suffix (`"1.1.0-preview.{height}"` → `1.1.0`). It has three parts and no suffix, and it is at least `1.0.0`. **The first release is the exception** ([`docs/RELEASE-PROCESS.md`](../../../docs/RELEASE-PROCESS.md#versioning)): `develop` carries `0.1.0-preview.{height}`, and the release is `1.0.0`. With no previous release tag, the default is `1.0.0`; any other version stops the skill and goes to the user.
- **Previous release:** `git tag -l "v*" --sort=-v:refname | grep -v -- '-' | head -1` (skips prerelease tags such as `v0.1.0-preview`). None before 1.0.0.
- **Breaking changes:** if there is a previous release, list the merged PRs since then with `breaking-change`:

  ```bash
  gh pr list -R milten89/OpenUrzednik.NET --state merged --base develop --label breaking-change --limit 100 \
    --search "merged:>$(git log -1 --format=%cI <previous tag>)" --json number,title
  ```

  If any are listed, the version must raise the major (`1.4.0` → `2.0.0`). Otherwise stop and ask the user: a breaking change in a minor violates [ADR-0011](../../../docs/adr/0011-compatibility-policy.md).
- **Above nuget.org:** the version must sort above every **listed** version of every published package (the five packages in `OpenUrzednik.Packages.slnf`):

  ```bash
  python .claude/skills/prepare-release/scripts/check_nuget.py before <version>
  ```

  It reads the registration API and compares in SemVer order (`1.1.0-preview.3` sorts below `1.1.0`). Any listed version that isn't below the release version stops the skill: it usually means an old preview is still listed, and unlisting is the maintainer's job.

### 3. Release branch

```bash
git switch -c release/<major>.<minor> origin/develop
```

- `version.json`: set `"version"` to the release version; change nothing else.
- Root `README.md` and `README.pl.md` (ADR-0009: the table is an informational part), the package table: `🧪 Preview` → `✅ Stable` for each published package, and drop text that only fits a preview ("The public API may still change", "API publiczne może się jeszcze zmienić", a "(preview)" after a client list). The package READMEs aren't edited here: their wording is checked in step 4.
- Commit with explicit paths, `git add` and `git commit` as separate commands: `git add version.json README.md README.pl.md`, message `Set the version to <version>`.
- **Release notes:** run `/release-notes <version> <commit>`, where `<commit>` is the full id of `origin/develop` the branch was made from: the branch isn't pushed yet, and the merged PRs are the same. Then commit `docs/release-notes/v<version>.md` (explicit path). `/release-notes` also checks that every PR in the range has a release label.

### 4. Checks on the branch

Run them in order; on Windows, add the net472 test run.

```bash
dotnet build OpenUrzednik.slnx
dotnet test OpenUrzednik.slnx -f net10.0
dotnet test OpenUrzednik.slnx -f net472            # Windows only
dotnet format OpenUrzednik.slnx --verify-no-changes
rm -rf artifacts/release
dotnet pack OpenUrzednik.Packages.slnf -c Release -o artifacts/release \
  -p:ContinuousIntegrationBuild=true -p:PublicRelease=true
python .claude/skills/prepare-release/scripts/check_packages.py artifacts/release <version>
```

- The pack also runs package validation; it must pass with no new suppression.
- `check_packages.py` checks every `.nupkg`: the exact version, `releaseNotes` linking `…/releases/tag/v<version>`, the icon, a Description starting with "Unofficial", no preview wording in the packed README, and repository links on `main`. Each line it prints is a problem; fix it on `develop` (a normal PR), not on the release branch, unless the fix is the release's own (version, README table). Before 1.0.0, the issues of milestone `1.0.0` cover them: the "Unofficial" Description (P4-1), the preview sections of the package READMEs (P4-2), the icon (P4-5) and the `blob/develop` → `blob/main` links (P4-10). If one is still open, stop and name it.
- `git diff origin/develop --stat` lists only `version.json`, `README.md`, `README.pl.md` and the notes file.

### 5. Confirm, push and open the PR

Show the user, in one message: the version, the previous release, the check results, the diff summary and the notes file. Ask **once** for confirmation. After a clear yes:

```bash
git push -u origin release/<major>.<minor>
gh pr create -R milten89/OpenUrzednik.NET --base main --head release/<major>.<minor> \
  --label skip-changelog --title "Release <version>" --body-file <scratch body file>
```

The body lists, as a checklist for the maintainer: the milestone, the notes file, the check results, and the steps after the merge (merge with a merge commit; approve the `nuget-release` deployment; then `/prepare-release after`). Release PRs don't close issues (`Closes` works only on the default branch): the maintainer moves the milestone's issues to Done.

### 6. Stop

Tell the user the PR link and that the next steps are theirs: merge (merge commit) and approve the deployment in Actions.

## `/prepare-release after [version]`

Run after `nuget-release.yml` finished. The version is the argument, or the one in `origin/main`'s `version.json`.

1. **Published:** `python .claude/skills/prepare-release/scripts/check_nuget.py after <version>` reports the version listed for each of the five packages. The index can lag a few minutes behind the publish: wait and retry before reporting a failure.
2. **Smoke test** in a new folder in the scratchpad (not in the repo), before the back-merge is merged, while `develop` previews still sort below the release:

   ```bash
   dotnet new console -o <scratch>/smoke && cd <scratch>/smoke
   dotnet add package OpenUrzednik.Nbp --prerelease
   ```

   The resolved version in the `.csproj` must be the release version, and `dotnet build` must succeed. Another version means a higher one is still listed or resolvable (for 1.0.0, an old `1.0.N-preview`): report it to the maintainer.
3. **Tag and Release:** `git ls-remote --tags origin v<version>` points at the release PR's merge commit (`gh pr view <n> --json mergeCommit`), and `gh release view v<version> -R milten89/OpenUrzednik.NET` has the notes file as its body.
4. **Back-merge**, exactly as in `docs/RELEASE-PROCESS.md` ("Back-merge after a release or hotfix"): `merge/v<version>` from `origin/main`, merge `origin/develop` without committing, set `version.json` to the next minor (`"<major>.<minor+1>.0-preview.{height}"`, unless `develop` already carries a higher version), and check `git diff origin/develop -- version.json` shows only that change. Commit, then ask the user once before pushing and opening the PR into `develop` (label `skip-changelog`, merged with a merge commit).

Report each step's result. Failed steps are the maintainer's to decide: never re-publish, delete a tag or edit the Release.
