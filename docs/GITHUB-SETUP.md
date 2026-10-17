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
| Merge methods | merge commits only (squash and rebase disabled) |
| Default merge commit message | GitHub's default ("Merge pull request #N from …") |
| Automatically delete head branches | on |
| Actions: default `GITHUB_TOKEN` permissions | read |
| Actions: allow GitHub Actions to approve PRs | off |

## Ruleset `protected-branches` (id 20632755)

Targets `refs/heads/main` and `refs/heads/develop`. Enforcement: active.

| Rule | Configuration |
|---|---|
| Restrict deletions | on |
| Block force pushes | on |
| Require a pull request | 0 approvals, CODEOWNERS review, dismiss stale approvals, resolve all conversations, allowed merge method: merge |
| Required status checks (strict, up to date) | `build (net8.0)`, `build (net10.0)`, `test (net472)`, `format`, `dependency-review` |
| Code scanning | CodeQL: alerts threshold `errors`, security alerts `high_or_higher` |
| Bypass | Repository role **Admin**, mode **pull requests only** (the maintainer can merge their own PRs, but cannot push directly) |

> ⚠️ Required status checks are matched by **job name**. Job names are defined in `.github/workflows/build.yml` (`build (<tfm>)`, from the matrix, and `test (net472)`) `format.yml` (`format`) and `dependency-review.yml` (`dependency-review`). Renaming a job or changing the TFM matrix (e.g. adding netstandard2.0 or dropping net8.0) requires updating this ruleset, or every PR is blocked:
>
> ```bash
> gh api repos/milten89/OpenUrzednik.NET/rulesets/20632755 > ruleset.json   # edit required_status_checks, then:
> gh api -X PUT repos/milten89/OpenUrzednik.NET/rulesets/20632755 --input ruleset.json
> ```

`format`, `dependency-review` and CodeQL run on PRs to both `develop` and `main`, so release PRs (`release/X.Y` → `main`) can satisfy every required check.

Not used:
* **Restrict updates:** it made every merge an admin bypass, because only bypass actors could update the branches. The pull request rule already blocks direct pushes.
* **Code quality rule:** GitHub Code Quality isn't available for this repository (`gh api repos/milten89/OpenUrzednik.NET/code-quality/setup` returns 404), so the rule never blocked anything. CodeQL runs the `security-and-quality` query suite instead, and the code scanning rule enforces its results.
* **Require linear history:** it forbids merge commits, which every PR uses ([ADR-0008](adr/0008-branching-versioning-and-release.md)).
* **Require signed commits:** tried on 2026-10-04 and removed the same day: with it, PRs whose branch commits weren't signed couldn't be merged.

## Environments

| Environment | Deployment branches | Protection |
|---|---|---|
| `nuget-pre-release` | `develop` | none (a merge to `develop` that changes a package input publishes previews) |
| `nuget-release` | `main` | required reviewer: `milten89` |

Both use NuGet trusted publishing (OIDC via `NuGet/login`). The secret `NUGET_USER` holds the nuget.org user name.

> ⚠️ The nuget.org policies below match the **workflow file name** and the **environment name**. Renaming `nuget-pre-release.yml`, `nuget-release.yml` or either environment breaks publishing until the policy is updated on nuget.org.

## nuget.org trusted publishing

Set on nuget.org → account menu → **Trusted Publishing**. All packages are owned by the nuget.org account `milten89`.

| Policy | Workflow | Environment | Packages | Scope |
|---|---|---|---|---|
| OpenUrzednik.net pre-release | `nuget-pre-release.yml` | `nuget-pre-release` | `OpenUrzednik.*` | push new packages and package versions |
| OpenUrzednik.net release | `nuget-release.yml` | `nuget-release` | `OpenUrzednik.*` | push new packages and package versions |

Both trust publisher GitHub Actions, repository owner `milten89` (id 9992006) and repository `OpenUrzednik.NET` (id 1304343799). Neither may unlist or relist versions; unlisting is done by hand with a short-lived API key.

## Security

| Setting | Value |
|---|---|
| Private vulnerability reporting | on (linked from `SECURITY.md` and the issue chooser) |
| Dependabot alerts | on |
| Dependabot security updates | on |
| Dependabot version updates | `.github/dependabot.yml` (weekly, NuGet + GitHub Actions, target `develop`); version updates of `*.Abstractions` are ignored (ADR-0004); a vulnerability in one always shows as a Dependabot alert |
| Secret scanning | on |
| Push protection | on |
| Secret scanning: non-provider patterns, validity checks | not available (`disabled` in `security_and_analysis`): the settings page of a personal account's public repository doesn't offer them; they need GitHub Secret Protection on an organization |
| Code scanning | CodeQL Advanced workflow (`.github/workflows/codeql.yml`), query suite `security-and-quality` |
| Dependency review | `.github/workflows/dependency-review.yml`: fails a PR that adds a package with a known vulnerability of moderate severity or higher |

Workflow hardening: all third-party actions are pinned to a commit SHA, with the version in a trailing comment (Dependabot updates both). Workflows declare least-privilege `permissions`. Publishing workflows queue instead of cancelling.

## Labels

Release notes (`.github/release.yml`) group PRs by the **release labels**; every PR has at least one. Issues use the same labels to forecast the PR's category.

| Label | Color | Use |
|---|---|---|
| `breaking-change` | `#B60205` | Release label: public API or behaviour break, needs a major version |
| `enhancement` | `#a2eeef` | Release label: new feature (GitHub default) |
| `bug` | `#d73a4a` | Release label: something doesn't work (GitHub default) |
| `documentation` | `#0075ca` | Release label: docs only (GitHub default) |
| `dependencies` | `#0366D6` | Release label: dependency updates (Dependabot) |
| `ci` | `#5319E7` | Release label: CI workflows and tooling (Dependabot's Actions updates) |
| `skip-changelog` | `#EEEEEE` | Release label: exclude the PR from release notes |
| `maintainer-only` | `#D93F0B` | Only the maintainer can do it; agents don't start, change or close it |
| `area:core` | `#1D76DB` | Affects `OpenUrzednik.Core` |
| `area:http` | `#1D76DB` | Affects `OpenUrzednik.Http` |
| `area:release` | `#1D76DB` | Versioning, packaging, NuGet publishing, release notes |
| `area:repo` | `#1D76DB` | GitHub settings, community files, agent instructions |
| `area:telemetry` | `#1D76DB` | Telemetry contract, `OpenUrzednik.Diagnostics`, `OpenUrzednik.Extensions.Logging` |
| `provider:nbp` | `#FBCA04` | Affects `OpenUrzednik.Nbp` |
| `provider-proposal` | `#C5DEF5` | Proposal for a new provider (issue form) |
| `question` | `#d876e3` | A question rather than a change (GitHub default) |
| `good first issue`, `help wanted`, `duplicate`, `invalid`, `wontfix` | GitHub defaults | As GitHub describes them |

## Milestones

A release is a milestone named after its version (`1.0.0`). It holds only the issues that must be closed before that release PR opens, all of them Priority P0 or P1; nice-to-haves stay outside. A bug with Priority P0 or P1 goes into the next release's milestone. The release PR opens only when the milestone has no open issues:

```bash
gh api repos/milten89/OpenUrzednik.NET/milestones --jq '.[] | select(.title == "1.0.0") | .open_issues'
```

## Project

Work is tracked in the public project [OpenUrzednik.NET](https://github.com/users/milten89/projects/2) (user project 2 of `milten89`, linked to the repository). The rules for agents are in `CLAUDE.md` ("Issues and the project"). Fields, items and view filters can be set through the API; view grouping, sorting and the built-in workflows only in the web UI, so they are recorded here to make the project recreatable.

```bash
gh project view 2 --owner milten89
gh project field-list 2 --owner milten89
gh api graphql -f query='query{user(login:"milten89"){projectV2(number:2){views(first:20){nodes{name layout filter groupByFields(first:5){nodes{... on ProjectV2FieldCommon{name}}} verticalGroupByFields(first:5){nodes{... on ProjectV2FieldCommon{name}}} sortByFields(first:5){nodes{direction field{... on ProjectV2FieldCommon{name}}}}}} workflows(first:20){nodes{name enabled}}}}}'
```

Reading or changing the project needs the `project` scope on the `gh` token (`gh auth refresh -s project`).

### Fields

| Field | Type | Values |
|---|---|---|
| Status | single select | `Triage` (new; not sorted yet), `Backlog` (accepted; blocked or not next), `Ready` (approved to start by the maintainer; no open blockers), `In progress` (branch or PR open, or waiting on an outside party), `Done` (merged, closed or completed) |
| Priority | single select | `P0` (urgent: harm now or a calendar deadline), `P1` (needed for the next release), `P2` (should do; may slip), `P3` (later) |
| Phase | single select | `Phase 0` … `Phase 6`: the stages of the 1.0 plan |
| Seq | number | Execution order within the plan, in steps of 10, leaving room to insert |
| Plan | text | The plan id (e.g. `P0-0`, `M5`) |
| Size | single select | Maintainer effort to review or do it: `XS` (< 15 min), `S` (< 30 min), `M` (about 1 h), `L` (several hours) |
| Due | date | Only for calendar deadlines |

Ids used by the agent commands in `CLAUDE.md`: project `PVT_kwHOAJh3Rs4BnebI`, Status field `PVTSSF_lAHOAJh3Rs4BnebIzhmC35A`, option `In progress` `e0ec58a2`.

Dependencies between issues use GitHub's native "blocked by" relationships, not a field.

### Views

| # | View | Layout | Filter | Group / columns | Sort |
|---|---|---|---|---|---|
| 1 | Next | Table | `is:issue is:open status:Ready,"In progress"` | Status | Priority, Seq |
| 2 | Board | Board | `is:issue -label:provider-proposal` | columns: Status | Priority, Seq |
| 3 | Maintainer | Table | `is:open label:maintainer-only` | Status | Due, Size |
| 4 | Review queue | Table | `is:pr is:open` | — | Updated ↑ |
| 5 | Release 1.0 | Table | `has:phase -phase:"Phase 6"` | Phase | Seq |
| 6 | Deadlines | Table | `is:open has:due` | — | Due |
| 7 | Triage | Table | `is:issue is:open status:Triage` | — | Created ↓ |

### Workflows

Set in the web UI (project → ⋯ → Workflows). The Free plan allows one auto-add workflow.

| Workflow | Setting |
|---|---|
| Auto-add to project | Repository `milten89/OpenUrzednik.NET`, filter `is:issue,pr is:open` |
| Item added to project | Issues → Status `Triage` |
| Item reopened | Issues → Status `Backlog` |
| Item closed | Issues and pull requests → Status `Done` |
| Pull request merged | Status `Done` |
| Pull request linked to issue | The issue → Status `In progress` |
| Auto-close issue | Status changed to `Done` → close the issue |
| Auto-archive items | `is:pr is:closed updated:<@today-2w` |
| Auto-add sub-issues to project | On (default) |
| Code changes requested, Code review approved | Off |

### Intake

Issues (from the forms, or opened by agents) arrive through auto-add with Status `Triage`. PRs, Dependabot's included, are added without a Status and show in the Review queue view. Security reports never arrive (they stay private, see `SECURITY.md`). The maintainer sets labels and Priority, puts a P0/P1 bug into the next milestone, and moves provider proposals to `Backlog` with Priority P3. Only the maintainer moves an issue to `Ready`.
