# Retrospectives

Append-only record of retrospective findings on a release, milestone,
or sprint.

This subtree's shape, audience, and lifecycle are defined by
[`/doc/docs/meta/documentation-charter.md`](/doc/docs/meta/documentation-charter.md#buildlog--lessons-learned).

## What A Retrospective Is

A reflective document written after a unit of delivery work — a
release, a milestone, a sprint — naming what worked, what did not,
and what changes to process or architecture follow. Retrospectives
differ from postmortems in that they are not triggered by an
incident; they are triggered by the cadence of delivery.

## Filename Format

```
YYYY-Qn-<topic>.md
```

Where `YYYY-Qn` is the calendar quarter the retrospective covers
(e.g. `2026-Q2`) and `<topic>` is a noun phrase describing the unit
of work (e.g. `2026-Q2-bootstrap-and-first-tenants.md`),
all-lowercase, hyphen-separated.

## Append-Only Rule

Retrospectives are **never edited after merge** except for typo
fixes, and **never deleted**. A later retrospective may revisit
findings from an earlier one; it links back, the original stays.

## Document Skeleton

```markdown
# Retrospective: <release / milestone / sprint name>

## Window
Start date — end date covered by this retrospective.

## What Shipped
The capability matrix rows that moved to `Implemented` in this
window; the tech-debt entries closed; the ADRs accepted.

## What Worked
What we did that we should keep doing. Be specific.

## What Did Not
Where the process or architecture cost more than it should have.
Failure modes, not people.

## Changes Adopted
Concrete process or architecture changes that follow from this
retrospective. Each is either:
- an ADR (linked), or
- a tech-debt entry (TD-NNNN), or
- a constitution / charter amendment PR (linked).

## Metrics Snapshot
DORA metrics for the window per
[`/doc/docs/constitution.md`](/doc/docs/constitution.md) Section VII.1:
deployment frequency, lead time, change failure rate, MTTR.

## Follow-Ups
- [ ] Tracked work item, owner, target date
```

## What Goes Here

- Per-release retrospectives following a release tag
- Per-milestone retrospectives following a major capability shipping
- Sprint retrospectives if the team adopts a sprint cadence

## What Does NOT Go Here

- Postmortems (those go to
  [`/doc/buildlog/postmortems/`](../postmortems/) — they are
  incident-triggered, not cadence-triggered)
- Plans for the next window (those go to
  [`/doc/buildlog/tech-debt-ledger.md`](../tech-debt-ledger.md) or to
  a GitHub issue)
- Personal opinions framed as findings

## Status Today

No retrospectives have closed yet. This file is a stub README that
activates the moment the first retrospective lands — typically
following the first release tag.
