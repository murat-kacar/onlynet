# Abandoned Approaches

Append-only record of approaches we tried and rejected, with the
reasoning preserved so the same idea is not re-proposed without new
context.

This subtree's shape, audience, and lifecycle are defined by
[`/doc/docs/meta/documentation-charter.md`](/doc/docs/meta/documentation-charter.md#buildlog--lessons-learned).

## When To Write An Abandonment Note

When a tech-debt-ledger entry transitions to `[ABANDONED]` per the
status vocabulary in
[`/doc/buildlog/tech-debt-ledger.md`](../tech-debt-ledger.md#status-vocabulary),
its closure block MUST cite an abandonment note in this folder. The
note carries the reasoning that does not fit on a ledger row.

Spike outcomes that ended in "we tried this and rejected it" may also
land here rather than in
[`/doc/buildlog/spikes/`](../spikes/) when the rejection itself is
the point.

## Filename Format

```
<topic>.md
```

A noun phrase describing what was abandoned (e.g.
`raw-sql-migrations.md`), all-lowercase, hyphen-separated. No date
prefix — the abandonment is what is preserved; the date the approach
was tried is recorded inside the document.

## Append-Only Rule

Abandonment notes are **never edited after merge** except for typo
fixes, and **never deleted**. If a later context revives the
approach, the right move is a new ADR (with `Status: Proposed`) that
explicitly cites the abandonment note and explains what changed —
not an edit to the original.

## Document Skeleton

```markdown
# Abandoned: <one-line approach>

## What We Tried
The approach itself. Briefly.

## Why It Looked Good
The reasoning at the time. Honest.

## Why We Rejected It
What we measured, hit, or realised that turned the reasoning around.
Cite numbers, code snippets, ADRs, or postmortems where applicable.

## Conditions Under Which We Would Reconsider
The smallest change in context that would justify a new spike or
ADR on the same idea. Be precise: "if X drops below Y" rather than
"if things change".

## Cross-References
- Tech-debt entry that closed `[ABANDONED]` (TD-NNNN)
- ADR that documented the alternative we did adopt (AD-NNNN)
- Spike outcome, if any
```

## What Goes Here

- Approaches whose ledger entries closed `[ABANDONED]`
- Spike outcomes whose answer was a structured "no"
- Architectural alternatives reviewed and rejected with enough detail
  that a future contributor can avoid re-running the analysis

## What Does NOT Go Here

- Postmortems (those go to
  [`/doc/buildlog/postmortems/`](../postmortems/))
- Decisions still under discussion (those live in
  [`/doc/docs/reference/architecture/decisions.md`](/doc/docs/reference/architecture/decisions.md)
  with `Status: Proposed` until they are accepted or rejected)
- Hot-take dismissals — every entry must be specific enough to be
  useful when the same idea returns six months later

## Status Today

TabFlow v1.0.0 starts with no abandoned approaches recorded. This
README defines the format for the first `[ABANDONED]` ledger note.
