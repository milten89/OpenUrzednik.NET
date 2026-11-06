---
name: triage
description: Sort new OpenUrzednik.NET project items (Status Triage) - verify each issue against develop, propose Priority, Phase, Size, Seq, Plan, labels and milestone in one table, and apply it only after the maintainer approves. Use whenever the user asks to triage, sort or groom the Triage column or new issues, decide "what to do with" incoming issues, or right after an agent opens an issue the maintainer asked for.
argument-hint: "[issue number ...]"
---

# Triage new project items

New issues land in the project [OpenUrzednik.NET](https://github.com/users/milten89/projects/2) with Status `Triage` ([`docs/GITHUB-SETUP.md`](../../../docs/GITHUB-SETUP.md#intake)). This skill sorts them: it checks each one against the code, proposes the project fields for all of them in one table, and writes nothing until the maintainer approves that table. CLAUDE.md lets agents change only an issue's Status to `In progress`; the approval in step 4 is what allows the other fields here.

Guardrails, for every step:

- Never triage, change or start an issue labelled `maintainer-only`.
- Never close an issue (as a duplicate, as fixed or otherwise), edit an issue body or post a comment without approval. Propose the text in the table's notes instead.
- Never move an item to `Ready` unless the maintainer says so in the approval: only the maintainer decides what starts.
- PRs aren't triaged: they arrive without a Status and wait in the Review queue view.

With issue numbers as arguments, triage only those issues (whatever their Status) and skip the list query in step 1; otherwise the whole Triage column. An issue that already has Seq and Plan keeps them unless they are wrong (say why in the notes). Repository: `milten89/OpenUrzednik.NET`; examples use bash. In Git Bash, run `git show` with `MSYS_NO_PATHCONV=1`: otherwise it rewrites `origin/develop:<path>` as a Windows path list.

## 1. Collect

```bash
gh project item-list 2 --owner milten89 --format json --limit 200 --jq '.totalCount'
gh project item-list 2 --owner milten89 --format json --limit 200 \
  --jq '[.items[] | select(.content.type == "Issue" and (.status == "Triage" or .status == null) and ((.labels // []) | index("maintainer-only") | not))] | .[] | "#\(.content.number) \(.id) \(.title)"'
```

If `totalCount` reaches the limit, raise `--limit`. An issue without a Status was missed by the auto-add workflow; triage it too. If the list is empty, say so and stop. For an issue given as an argument, find its project item id (step 5 needs it); if it prints nothing, the issue isn't in the project, and step 5 adds it with `gh project item-add 2 --owner milten89 --url <issue url>` (a write, so only after approval):

```bash
gh project item-list 2 --owner milten89 --format json --limit 200 --jq '.items[] | select(.content.number == <n>) | "\(.id) \(.status)"'
```

For each issue, read it in full, including the comments and the issues it links:

```bash
gh issue view <n> -R milten89/OpenUrzednik.NET --json state,title,body,labels,milestone,author,createdAt,comments,blockedBy \
  --jq '{state, title, author: .author.login, createdAt, labels: [.labels[].name], milestone: .milestone.title, blockedBy: [.blockedBy.nodes[] | "#\(.number) \(.state)"], body, comments: [.comments[] | "\(.author.login): \(.body)"]}'
```

Skip closed issues (report them: a closed issue in Triage means a workflow didn't run).

## 2. Verify against `develop`

Work from the latest `origin/develop` (`git fetch origin`; read files with `git show origin/develop:<path>` when the checkout is on another branch). For each claim in the issue, check the code and docs it names:

- **Still true?** A bug may be fixed already, or a snippet may have moved (an earlier run found a broken README snippet that had moved into the packed Core README).
- **Duplicate?** Search open issues: `gh issue list -R milten89/OpenUrzednik.NET --state open --search "<keywords>"`.
- **Blocked?** Note issues it depends on; a blocked issue stays in `Backlog`.
- **Against an ADR?** An issue that contradicts an accepted ADR needs `/adr` first; say so in the notes.

## 3. Propose one table

Read the context the fields depend on:

```bash
# Items in order: Seq, Plan, Priority, Phase, Status
gh project item-list 2 --owner milten89 --format json --limit 200 \
  --jq '[.items[] | select(.status != "Done" and .seq != null)] | sort_by(.seq) | .[] | "\(.seq) \(.plan // "-") \(.priority // "-") \(.phase // "-") \(.status) #\(.content.number) \(.title)"'
# The next free follow-up id; count up from it for several items
gh project item-list 2 --owner milten89 --format json --limit 200 \
  --jq '[.items[].plan // "" | capture("^F(?<n>[0-9]+)$") | .n | tonumber] | "F\((max // 0) + 1)"'
gh label list -R milten89/OpenUrzednik.NET --limit 100 --json name --jq '.[].name'
gh api repos/milten89/OpenUrzednik.NET/milestones --jq '.[] | "\(.title) open=\(.open_issues)"'
gh issue list -R milten89/OpenUrzednik.NET --milestone <title> --state open --json number,title --jq '.[] | "#\(.number) \(.title)"'
```

Rules (definitions in [`docs/GITHUB-SETUP.md`](../../../docs/GITHUB-SETUP.md#fields)):

- **Priority:** `Urgent` (harm now or a calendar deadline), `High` (needed for the next release), `Medium` (should do; may slip), `Low` (later, or after the release).
- **Milestone:** the next release's milestone only for what that release needs, all of it Urgent or High; an Urgent or High `bug` always goes in. Nice-to-haves get none ([Milestones](../../../docs/GITHUB-SETUP.md#milestones)).
- **Phase:** the phase of the 1.0 plan the work belongs to; `Phase 6` for work after 1.0.
- **Seq:** a free number between the neighbours in the intended order (the plan uses steps of 10; follow-ups take free numbers in between). When no neighbour gives a clear place, take one more than the highest Seq of the item's Phase that is still free.
- **Plan:** the next free `F<n>`, computed from all items as above, never from a few neighbours.
- **Size:** an estimate of the maintainer's effort to review or do it, not the agent's: `XS` (< 15 min), `S` (< 30 min), `M` (about 1 h), `L` (several hours).
- **Labels:** one release label from `.github/release.yml` as a forecast (`breaking-change`, `enhancement`, `bug`, `documentation`, `dependencies`, `ci`, `skip-changelog`) plus area labels (`area:*`, `provider:*`). Only labels that exist; propose a new one in the notes instead of creating it.
- **Provider proposals:** `Backlog`, Priority `Low`, no milestone.
- **Due:** only for a calendar deadline.

Show one table for all items, in the recommended order, then the notes:

| Issue | Priority | Phase | Seq | Plan | Size | Labels | Milestone | Status | Reason |
|---|---|---|---|---|---|---|---|---|---|
| #n title | Low | Phase 6 | 437 | F8 | XS | `area:core`, `skip-changelog` | — | Backlog | one line |

Notes list what step 2 found (already fixed, moved, duplicate, blocked by), with proposed comment or closing text where one would help.

## 4. Ask

Ask the maintainer once to approve or change the table. Write nothing before a clear answer. Apply exactly the approved values; if they change something, show the changed rows again only when the change affects other rows (Seq order, Plan ids).

## 5. Apply and report

Read the field and option ids, then set each approved value:

```bash
gh project field-list 2 --owner milten89 --format json \
  --jq '.fields[] | "\(.name) \(.id) \([.options[]? | "\(.name)=\(.id)"] | join(" "))"'
gh project item-edit --project-id PVT_kwHOAJh3Rs4BnebI --id <item id> --field-id <field id> --single-select-option-id <option id>  # Status, Priority, Phase, Size
gh project item-edit --project-id PVT_kwHOAJh3Rs4BnebI --id <item id> --field-id <field id> --number <seq>                       # Seq
gh project item-edit --project-id PVT_kwHOAJh3Rs4BnebI --id <item id> --field-id <field id> --text <plan>                       # Plan
gh project item-edit --project-id PVT_kwHOAJh3Rs4BnebI --id <item id> --field-id <field id> --date <YYYY-MM-DD>                 # Due, only for a deadline
gh issue edit <n> -R milten89/OpenUrzednik.NET --add-label <label>      # --remove-label for a wrong form label
gh issue edit <n> -R milten89/OpenUrzednik.NET --milestone <title>      # only when the table gives a milestone
```

Status becomes `Backlog` (or `Ready` where the maintainer said so). Approved comments or closings come last. Then show the result from the project, not from memory:

```bash
gh project item-list 2 --owner milten89 --format json --limit 200 \
  --jq '.items[] | select(.content.number == <n>) | {status, priority, phase, seq, plan, size, labels, milestone: .milestone.title}'
```

Report every failed write.

## Issues an agent opens for the maintainer

When the maintainer asks an agent for work that needs a new issue (one issue per PR), the agent opens it and triages it right away with the rules of step 3, without the table and approval: the request is the approval. Status is `Ready`, or stays `In progress` if a linked PR already moved it there. The one exception is the milestone: a milestone gates the release, so set one only when the maintainer's request names the release; otherwise leave it out and say so in the report. Issues an agent opens on its own initiative (problems found in passing) stay in `Triage` for this skill.
