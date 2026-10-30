---
name: release-notes
description: Draft the release notes of an OpenUrzednik.NET release or hotfix as docs/release-notes/v<version>.md, from GitHub's generated notes grouped by the release labels, with migration notes for breaking changes. Use on a release/X.Y or hotfix/… branch before its PR into main, whenever the user asks for release notes, a changelog or "what's in this release", and whenever a release PR fails the "Check the release notes" step.
argument-hint: "<version, e.g. 1.1.0> [branch, default: the current branch]"
---

# Draft release notes

The notes are the body of the GitHub Release and the page every package's `releaseNotes` links to (`…/releases/tag/v<version>`). `nuget-release.yml` publishes nothing without `docs/release-notes/v<version>.md`, and the `build (net10.0)` job checks the file on every PR into `main` (`docs/RELEASE-PROCESS.md`). Write them for people who use the packages: what changed for them and what they must do.

## 1. Range

- **Version:** the argument, or `version` in the branch's `version.json`. It must be stable (no `-preview`); a release branch has already removed the suffix.
- **Branch:** the argument, or the current branch (`release/X.Y` or `hotfix/…`), pushed to `origin`: the API reads GitHub, not the local checkout.
- **Previous release:** the latest `v*` tag that is a release, ignoring `v0.1.0-preview` (from the old version scheme):

  ```bash
  git fetch --tags origin
  git tag -l "v*" --sort=-v:refname | grep -v -- '-' | head -1   # skips prerelease tags, v0.1.0-preview included
  ```

  For a hotfix, it is the tag the branch was made from. None before 1.0.0: the notes then cover the whole history.

## 2. Draft from GitHub

```bash
gh api -X POST repos/milten89/OpenUrzednik.NET/releases/generate-notes \
  -f tag_name=v<version> -f target_commitish=<branch> -f previous_tag_name=<previous tag> \
  --jq .body
```

Leave out `previous_tag_name` when there is no previous release. The API lists only merged PRs: a **hotfix**'s fix is still in its own open PR, so add that PR's line by hand under its category (`* <title> by @<author> in <PR url>`, as the generated lines); a release branch contains only merged PRs. The call creates nothing; it uses `.github/release.yml`, so the draft is already grouped by release label (💥 Breaking changes, 🚀 Features, 🐞 Bug fixes, 📚 Documentation, 📦 Dependencies, 🛠️ CI and tooling) and leaves out `skip-changelog` PRs.

## 3. Check the labels

PRs under **Other changes** have no release label. The `release-label` check should have stopped them, so each one is a gap (an old PR, or the check was skipped): list them to the user with a proposed label, and after they agree, add it (`gh pr edit <n> --add-label <label>`) and draft again. Don't file a PR under a category yourself without the label: the next generated notes would disagree with the file.

For 1.0.0 this step doesn't apply: the range is the whole preview history, and its early PRs (#1–#50) predate the release labels. The 1.0.0 notes replace the generated list with a summary (below), so leave those PRs as they are.

## 4. Write `docs/release-notes/v<version>.md`

```markdown
# OpenUrzednik.NET <version>

<one or two sentences: what this release is about>

## Upgrading

<only if needed: steps a user must take, most important first>

<the generated categories, edited>

**Full changelog:** https://github.com/milten89/OpenUrzednik.NET/compare/<previous tag>...v<version>
```

- Keep the generated lines (`* <title> by @<author> in <PR url>`); shorten or clarify a title only when it says nothing to a user. Keep the "New Contributors" section, but remove bots (`dependabot[bot]`) from it, and the whole section if nobody else is left.
- **Breaking changes:** for each PR in that category, read its body (`gh pr view <n> --json body`), especially "Breaking changes / notes for reviewers", and add what the user has to change under **Upgrading**: the old and new API, the new error code, the renamed tag. A breaking change outside a major version is a policy violation ([ADR-0011](../../../docs/adr/0011-compatibility-policy.md)): stop and tell the user.
- Mention changes to the dependency minimums and the targets (a dropped .NET target, a raised `Microsoft.Extensions.*` floor) under **Upgrading** too: they decide whether an app can update.
- No internal detail users can't act on (refactorings, test-only PRs); those PRs carry `skip-changelog` or `ci`.

### 1.0.0 only

The first stable release follows a long preview period: the generated list has about a hundred PRs of preview churn, many without a label. Its notes therefore have, instead of that list:

- **Changes since the previews:** the user-visible changes of the preview period, grouped like the categories above (breaking changes first), one line each with the PR links. Build it from the done items in `docs/BACKLOG.md` (item 28 lists the early API changes made before release notes existed) and the merged PRs (`gh pr list --state merged --base develop --limit 300 --json number,title,labels`), not from commits. Point to the package READMEs instead of repeating them.
- **Full changelog:** `https://github.com/milten89/OpenUrzednik.NET/commits/v1.0.0` (there is no previous tag to compare with).
- Under **Upgrading**, these two notes, in substance:
  - **Update all OpenUrzednik packages together, with an explicit version.** The earlier previews were numbered `1.0.N-preview`, and NuGet sorts `1.0.0` below `1.0.36-preview`, so it won't offer 1.0.0 as an update. Mixing 1.0.0 with a preview can cause NU1605 (package downgrade) errors, or silently keep a preview. Those previews are unlisted on nuget.org.
  - **Dates are `DateTime` on every target** ([ADR-0005](../../../docs/adr/0005-target-frameworks.md)). Calls with a `DateOnly` still compile on .NET 8 and later, through extension methods, but two things break: a mock setup like `client.GetAsync(Arg.Any<DateOnly>())` compiles against the extension method and no longer matches the interface method (set up `Arg.Any<DateTime>()`), and positional deconstruction of a model into `DateOnly` variables no longer compiles.

## 5. Finish

- Show the user the file. Run `/stop-slop` on it (keep PR titles, links and version numbers as they are), then commit it to the release branch with an explicit path: `git add docs/release-notes/v<version>.md`.
- Don't create the tag or the GitHub Release: `nuget-release.yml` does both after the merge, with this file as the body (`--notes-file`).
