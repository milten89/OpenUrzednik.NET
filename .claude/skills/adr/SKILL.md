---
name: adr
description: Create a new Architecture Decision Record (MADR) in docs/adr, or one that supersedes an accepted record, and update the ADR index. Use when a change introduces or alters an architectural decision, when the maintainer settles such a decision in chat, or when the user asks to record a decision.
argument-hint: "<decision title>"
---

# Create an ADR

The log is append-only ([ADR-0001](../../../docs/adr/0001-record-architecture-decisions.md), following Microsoft's guidance): an accepted or rejected ADR is never edited, except its status line. A changed decision gets a new ADR that supersedes the old one.

**When an ADR is needed:** a decision that shapes the architecture or is hard to reverse: public API shape, the error or telemetry contract, dependencies, target frameworks, the release and support flow. It comes in the same PR as the change, also when the maintainer settled the decision in chat. A change inside an existing decision (e.g. a new method that follows the client API ADR) needs none.

1. Read `docs/adr/README.md` and the ADRs related to the topic. If an accepted ADR already covers it, decide with the user whether this is a clarification (no ADR needed; it goes into `CLAUDE.md` or the linked docs, and open work into an issue) or a change (a new ADR that supersedes the old one).
2. Take the next free number `NNNN` (4 digits; never reuse a number). File name: `docs/adr/NNNN-kebab-case-title.md`.
3. Copy `docs/adr/template.md`. Fill in:
   - front matter: `status`, today's date, `confidence` (`high`, `medium` or `low`: how sure the maintainer is; a low-confidence decision is a candidate for reconsidering), `decision-makers: milten89`. Status is `proposed`; use `accepted` only when the maintainer approved the decision explicitly (in chat or in the issue).
   - context and problem statement (a question), decision drivers, at least two considered options, the outcome with honest consequences (good and bad), and a **Confirmation** section saying how compliance is checked (a test, an analyzer, a CI step or a review rule).
   - Keep it on topic and current: no design guide. Lists that change often (CI checks, path lists, release steps) go into `docs/GITHUB-SETUP.md` or `docs/RELEASE-PROCESS.md`, linked from the ADR. State decisions in general terms where a list will grow (e.g. "HTTP tags follow OpenTelemetry's conventions", with examples).
4. Don't invent the maintainer's preferences, options or rationale. If the decision or the rejected options aren't clear from the conversation, ask before writing them.
5. **Superseding:** the new ADR restates the whole current decision, so a reader never combines two records, and links to the old one. In the old file, change only `status: superseded by ADR-NNNN` and add one link line under **More Information**. Then `git grep` the old number and update the references that state current rules (CLAUDE.md, `.claude/agents/reviewer.md`, the skills, CONTRIBUTING, code comments that cite it) to the new one.
6. Add a row to the table in `docs/adr/README.md` (and change the old row's status when superseding).
7. If the decision changes rules summarized in `CLAUDE.md`, update `CLAUDE.md` in the same change, and open an issue for any follow-up work (the project adds it with Status `Triage`).
8. Write it in English (ADR-0009).
