# Keep the known issues current

Part of `/stryker` ([SKILL.md](../SKILL.md), section 7). [known-issues.md](known-issues.md) is a snapshot (its "Reviewed" line has the date). Update it:

- **When Stryker.NET changes version** (`.config/dotnet-tools.json`, by hand or in a Dependabot PR): in that PR, before it is merged, because the workarounds may change with the version.
- **When a shard misbehaves** and the symptom isn't in the list: search upstream first (`gh issue list -R stryker-mutator/stryker-net --state all --search "<symptom>"`).
- **On request** ("update the Stryker known issues").

Steps:

1. Read the release notes of every Stryker.NET version since the one in the skill (`gh release list -R stryker-mutator/stryker-net`, `gh release view <tag> -R stryker-mutator/stryker-net`). For each row marked "fixed after 5.0.0" or "open", check whether the release fixes it; then re-check the workaround that depends on it. Known ones to revisit: `concurrency: 1` (sn#3832, PR #3897), `run.py`'s per-shard `TEMP` (PR #3724), the static-initializer disable comments (PR #3897), the `Count` local (sn#2762, PR #3601), false timeouts (PRs #3957, #3917). Change a workaround only with a run that shows it is no longer needed, and in its own PR (ADR-0018 governs the configs).
2. Review the issues opened or closed since the "Reviewed" date: `gh issue list -R stryker-mutator/stryker-net --state all --search "updated:>=YYYY-MM-DD" --limit 200 --json number,title,state,labels,closedAt`. Keep those that can affect this setup (the first paragraph of known-issues.md lists it); skip the excluded areas listed there.
3. For each kept issue, add or update a row: number and link, state (and the version that fixes it), the symptom as it shows in a report or log, whether this repository's Stryker version is affected, and the workaround used or recommended here. Mark how it was verified (**source**, **here**, **repro**, **unverified**); verify in the source of the pinned tag or with a small repro where it is cheap.
4. Update the version, tag and commit in the introduction of SKILL.md and in known-issues.md, the "Reviewed" line (date, how many issues), and every file of the skill whose facts changed (source line numbers move between versions: SKILL.md and the files in `references/`).
5. A bug that affects this repository and isn't reported upstream (or was closed without its cost being understood): write a draft issue (title, minimal repro, expected vs actual, version and config) and give it to the maintainer. Never post upstream yourself; the maintainer decides.
6. Outside a version change, the update is its own PR (`skip-changelog`); commit and open it only as CLAUDE.md allows.
