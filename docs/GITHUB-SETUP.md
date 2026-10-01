# GitHub repository configuration

The intended configuration of `milten89/OpenUrzednik.NET` on GitHub. The decision behind it is [ADR-0008](adr/0008-branching-versioning-and-release.md). Settings that live outside the repository are listed here so they can be checked and recreated. If you change a setting on GitHub, update this file in the same PR.

Check the live state with `gh`:

```bash
gh api repos/milten89/OpenUrzednik.NET --jq '{default_branch,allow_squash_merge,allow_merge_commit,allow_rebase_merge,delete_branch_on_merge,security_and_analysis}'
gh api repos/milten89/OpenUrzednik.NET/rulesets/20632755
gh api repos/milten89/OpenUrzednik.NET/environments
```

## General

| Setting | Value |
|---|---|
| Default branch | `develop` |
| Merge methods | squash only (merge commits and rebase disabled) |
| Squash commit title | PR title |
| Automatically delete head branches | on |
| Actions: default `GITHUB_TOKEN` permissions | read |
| Actions: allow GitHub Actions to approve PRs | off |

## Ruleset `protected-branches` (id 20632755)

Targets `refs/heads/main` and `refs/heads/develop`. Enforcement: active.

| Rule | Configuration |
|---|---|
| Restrict deletions | on |
| Block force pushes | on |
| Restrict updates | on (only bypass actors can merge) |
| Require a pull request | 0 approvals, CODEOWNERS review, dismiss stale approvals, resolve all conversations, allowed merge method: squash |
| Required status checks (strict, up to date) | `build (net8.0)`, `build (net9.0)`, `build (net10.0)`, `format` |
| Code scanning | CodeQL: alerts threshold `errors`, security alerts `high_or_higher` |
| Code quality | severity `errors` |
| Bypass | Repository role **Admin**, mode **pull requests only** (the maintainer can merge their own PRs, but cannot push directly) |

> ⚠️ Required status checks are matched by **job name**. Job names are defined in `.github/workflows/build.yml` (`build (<tfm>)`, from the matrix) and `format.yml` (`format`). Renaming a job or changing the TFM matrix (e.g. adding netstandard2.0 or dropping net8.0) requires updating this ruleset, or every PR is blocked:
>
> ```bash
> gh api repos/milten89/OpenUrzednik.NET/rulesets/20632755 > ruleset.json   # edit required_status_checks, then:
> gh api -X PUT repos/milten89/OpenUrzednik.NET/rulesets/20632755 --input ruleset.json
> ```

`format` and CodeQL run on PRs to both `develop` and `main`, so release PRs (`develop` → `main`) can satisfy every required check.

## Environments

| Environment | Deployment branches | Protection |
|---|---|---|
| `nuget-pre-release` | `develop` | none (every merge to `develop` publishes previews) |
| `nuget-release` | `main` | required reviewer: `milten89` |

Both use NuGet trusted publishing (OIDC via `NuGet/login`). The secret `NUGET_USER` holds the nuget.org user name.

## Security

| Setting | Value |
|---|---|
| Private vulnerability reporting | on (linked from `SECURITY.md` and the issue chooser) |
| Dependabot alerts | on |
| Dependabot security updates | on |
| Dependabot version updates | `.github/dependabot.yml` (weekly, NuGet + GitHub Actions, target `develop`) |
| Secret scanning | on |
| Push protection | on |
| Code scanning | CodeQL Advanced workflow (`.github/workflows/codeql.yml`) |

Workflow hardening: all third-party actions are pinned to a commit SHA, with the version in a trailing comment (Dependabot updates both). Workflows declare least-privilege `permissions`. Publishing workflows queue instead of cancelling.

## Labels

Used by release notes (`.github/release.yml`), Dependabot and issue forms:

| Label | Use |
|---|---|
| `breaking-change` | Public API or behaviour break, needs a major version |
| `bug`, `enhancement`, `documentation` | Default GitHub labels |
| `dependencies`, `ci` | Dependabot and workflow changes |
| `provider-proposal` | New provider proposals (issue form) |
| `provider:nbp` | Affects `OpenUrzednik.Nbp` |
| `area:core`, `area:http` | Affects `OpenUrzednik.Core` / `OpenUrzednik.Http` |
| `skip-changelog` | Exclude the PR from release notes |
