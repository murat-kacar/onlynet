# TabFlow Build Log

Append-only record of production incidents, retrospectives, completed
spikes, abandoned approaches, and accepted technical debt.

This tree's shape, audience, and lifecycle are defined by
[`/doc/docs/meta/documentation-charter.md`](/doc/docs/meta/documentation-charter.md#buildlog--lessons-learned).

## Subtrees

| Subtree / File | Content | Filename Format |
| --- | --- | --- |
| `postmortems/` | Production or preview incidents | `YYYY-MM-DD-<slug>.md` |
| `retrospectives/` | Release / milestone / sprint retros; recovery drill records | `YYYY-Qn-<topic>.md` |
| `spikes/` | Completed spike outcomes worth keeping | `<topic>.md` |
| `abandoned/` | Approaches tried and rejected | `<topic>.md` |
| `tech-debt-ledger.md` | Current ledger of accepted unfinished work | flat file |

## Append-Only Rule

Buildlog entries are never edited after merge except for typo fixes.
When a fact becomes outdated, publish a new buildlog entry that
references the old one.

The technical debt ledger is current-state documentation: an entry may
move from `[TRIAGE]` to `[OPEN]`, `[CLOSED]`, `[ACCEPTED]`, or
`[ABANDONED]`, and its payoff plan is kept accurate while the debt
exists.

## What Goes Here

- Postmortems with timeline, contributing factors, and follow-ups
- Retrospective findings worth carrying forward
- Recovery drill records required by the release gate
- Spike findings that do not become an ADR but contain useful insight
- Approaches tried and rejected
- Accepted unfinished work that needs an owner, risk, and payoff path

## What Does NOT Go Here

- Current contracts; those go to `/doc/docs/`
- End-user help; that goes to `/doc/userdocs/`
- External integration reference; that goes to `/doc/apidocs/`
- Draft architectural decisions; those live in
  [`/doc/docs/reference/architecture/decisions.md`](/doc/docs/reference/architecture/decisions.md)
  with `Status: Proposed`
- Personal opinions, blame, or non-blameless framing

## Status Today

TabFlow v1.0.0 starts with no production incidents, retrospectives,
completed spikes, or abandoned approaches recorded. Current unfinished
work is tracked in
[`./tech-debt-ledger.md`](./tech-debt-ledger.md).

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
- HH:MM UTC - what happened
- HH:MM UTC - what we noticed
- HH:MM UTC - what we did
- HH:MM UTC - resolution

## Contributing Factors
Blameless. Name failure modes, not people.

## What Worked
What helped us resolve this faster.

## What Did Not
Where the recovery was slower than necessary.

## Follow-Ups
- [ ] Tracked work item, owner, target date
- [ ] ...

## Related
- ADR or `docs/` page that changed because of this incident
```
