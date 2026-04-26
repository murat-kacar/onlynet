# Spikes

Append-only record of completed spike outcomes that are worth keeping
but did not produce an architectural decision record (ADR).

This subtree's shape, audience, and lifecycle are defined by
[`/doc/docs/meta/documentation-charter.md`](/doc/docs/meta/documentation-charter.md#buildlog--lessons-learned).

## What A Spike Is

A spike is a time-boxed exploration of an unknown. Per
[`/doc/docs/constitution.md`](/doc/docs/constitution.md) Section II.1
and the glossary entry at
[`/doc/docs/reference/glossary.md`](/doc/docs/reference/glossary.md#spike),
every spike has:

- a stated question,
- a budget in hours or days, and
- an expected artefact — an ADR, a tracer-bullet branch, or a
  documented "no".

Spike outcomes that **become an ADR** live in
[`/doc/docs/reference/architecture/decisions.md`](/doc/docs/reference/architecture/decisions.md).
Spike outcomes that **become a tracer bullet** live on a feature
branch and graduate via PR. Everything else — useful findings that
are not yet a decision and not yet a feature — lands here so the
question and the answer are preserved.

## Filename Format

```
<topic>.md
```

A noun phrase describing the spike topic (e.g.
`event-bus-throughput.md`), all-lowercase, hyphen-separated. No date
prefix — the spike *outcome* is what is preserved; the date the
spike ran is recorded inside the document.

## Append-Only Rule

Spike outcomes are **never edited after merge** except for typo
fixes, and **never deleted**. If a later spike or ADR supersedes a
finding, publish a new spike outcome (or an ADR) that links back; the
original stays.

## Document Skeleton

```markdown
# Spike: <one-line topic>

## Question
The exact question the spike was time-boxed to answer.

## Budget
Hours or days actually spent.

## Method
What was tried, in chronological order, briefly.

## Findings
What we learned. Numbers, code snippets, citations.

## Outcome
- ADR opened? Link.
- Tracer bullet opened? Link to branch / PR.
- Documented "no"? State it explicitly.

## Open Questions
What we did not answer. Useful for the next spike on the same topic.
```

## What Goes Here

- Completed exploratory work whose findings deserve preservation
- "We tried this for a day; it does not pay back at our scale" notes
- Performance characterisations that did not become an SLO
- Library or framework evaluations that did not become an ADR

## What Does NOT Go Here

- Architectural decisions (those go to
  [`/doc/docs/reference/architecture/decisions.md`](/doc/docs/reference/architecture/decisions.md)
  with `Status: Proposed` first, then `Status: Accepted` once the
  decision is reviewed and accepted)
- Active backlog items (those go to
  [`/doc/buildlog/tech-debt-ledger.md`](../tech-debt-ledger.md))
- Code: spike code lives on a branch, not in `docs/` or here

## Status Today

No spikes have closed yet. This file is a stub README that activates
the moment the first spike outcome lands.
