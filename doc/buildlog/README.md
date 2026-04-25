# TabFlow Build Log

Append-only record of **what happened, what we learned, and what
changed because of it**. Read by the future team that will face problems
we have already faced.

This tree's shape, audience, and lifecycle are defined by
[`/doc/docs/meta/documentation-charter.md`](/doc/docs/meta/documentation-charter.md#buildlog--lessons-learned).

## Subtrees

| Subtree / File | Content | Filename Format |
| --- | --- | --- |
| `postmortems/` | Production or preview incidents | `YYYY-MM-DD-<slug>.md` |
| `retrospectives/` | Release / milestone / sprint retros | `YYYY-Qn-<topic>.md` |
| `spikes/` | Completed spike outcomes worth keeping | `<topic>.md` |
| `abandoned/` | Approaches we tried and rejected | `<topic>.md` |
| `code-audit-*.md` | Periodic constitution-versus-code audits | `code-audit-YYYY-MM-DD.md` |
| `tech-debt-ledger.md` | Single ledger of all accepted technical debt | flat file, append-only |

## Append-Only Rule

Documents in this tree are **never edited after merge**, except for typo
fixes. They are **never deleted**.

When a fact in a buildlog document becomes outdated, the right action is
to publish a new buildlog entry that references the old one — not to
edit the original.

The tech debt ledger is append-only by row: an entry's `[OPEN]` status
may transition to `[CLOSED]`, `[ACCEPTED]`, or `[ABANDONED]` and a
closure block is appended; the original entry is never edited or
deleted.

## What Goes Here

- Postmortems with timeline, contributing factors, follow-ups
- Retrospective findings worth carrying forward
- Spike findings that didn't become an ADR but contain useful insight
- Approaches we tried, why we tried them, why we rejected them

## What Does NOT Go Here

- Active plans — those go to
  [`/doc/buildlog/tech-debt-ledger.md`](./tech-debt-ledger.md) (with a
  `TD-NNNN` identifier) or to a GitHub issue, not into a separate
  scratch tree
- Draft architectural decisions — those live in
  [`/doc/docs/reference/architecture/decisions.md`](/doc/docs/reference/architecture/decisions.md)
  with `Status: Proposed`
- Current contracts — those go to `/doc/docs/`
- End-user help — that goes to `/doc/userdocs/`
- Personal opinions, blame, or non-blameless framing

## Status Today

TabFlow has no incidents to postmortem yet. The first buildlog entry
under the audit subtree is
[`./code-audit-2026-04-25.md`](./code-audit-2026-04-25.md), which
measures the existing code tree against Constitution v2 and the 134
acceptance-criteria items. Postmortems, retrospectives, and spike
outcomes will land alongside it as those events occur.

## Format Guide

When the first postmortem is written, it follows this skeleton:

```markdown
# <YYYY-MM-DD> Incident: <One-Line Summary>

## Status
Resolved | Mitigated | Ongoing

## Impact
- Who was affected
- For how long
- What they could not do

## Timeline
- HH:MM UTC — what happened
- HH:MM UTC — what we noticed
- HH:MM UTC — what we did
- HH:MM UTC — resolution

## Contributing Factors
Blameless. Name failure modes, not people.

## What Worked
What helped us resolve this faster.

## What Didn't
Where the recovery was slower than necessary.

## Follow-Ups
- [ ] Tracked work item, owner, target date
- [ ] ...

## Related
- ADR or `docs/` page that changed because of this incident
```
