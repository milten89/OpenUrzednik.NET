# Release notes

One file per release or hotfix, `v<version>.md` (e.g. `v1.1.0.md`). It is the body of the GitHub Release `v<version>`, which every package's `releaseNotes` links to.

- Write it with `/release-notes <version>` on the release or hotfix branch, before the PR into `main` ([`RELEASE-PROCESS.md`](../RELEASE-PROCESS.md)).
- The PR's `build (net10.0)` job and `nuget-release.yml` check that the file exists; nothing is published without it.
- The notes come from the release labels of the merged PRs (`.github/release.yml`), plus migration steps for breaking changes.
