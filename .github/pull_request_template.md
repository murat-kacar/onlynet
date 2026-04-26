# Pull Request

## Summary

What does this PR change? One paragraph; written so the reviewer
understands the intent before reading the diff.

## Linked Issues / ADRs / TDs

- Issue:
- ADR (if architectural):
- Tech-debt ledger entry (if relevant): `TD-NNNN`

## Working Mode

Primary mode:

- [ ] Documentation
- [ ] Implementation
- [ ] Review

Secondary modes, if this PR intentionally crosses modes:

- [ ] Documentation
- [ ] Implementation
- [ ] Review

Mode handoff / output:

```text
Explain what this PR produces for each selected mode, or why a mode changed.
```

## Type Of Change

- [ ] Bug fix (non-breaking)
- [ ] New capability (non-breaking)
- [ ] Breaking change to a stable contract — requires major bump per
  AD-0011 and a deprecation window per
  [`/doc/docs/reference/api/README.md`](../doc/docs/reference/api/README.md#governance)
- [ ] Documentation only
- [ ] Tooling / CI

## Constitution Check

A self-check against [`/doc/docs/constitution.md`](../doc/docs/constitution.md):

- [ ] **II.4 Done = tested + observable + documented** — every change
  to a production capability includes a test, a log/metric, and an
  updated reference document
- [ ] **III. Documentation** — touched contracts have updated reference
  docs in the same PR
- [ ] **IV.3 Code style follows the framework** — `dotnet format`
  clean; analyzer warnings clean; no new `:warning:disable` without an
  ADR snippet
- [ ] **V.4 Security review trigger** — if this PR touches auth,
  authorization, secrets, payment, or personal data, a `security:
  reviewed` note is in this description below
- [ ] **VIII. Working Modes** — primary mode is declared; secondary modes
  and handoffs are listed when applicable

## Documentation Knock-On Effects

If this PR changes a contract, list every document updated in the same
commit:

- [ ] `path/to/file.md` — [what changed]
- [ ] `path/to/another.md` — [what changed]

If the change affects the constitution or charter, the
[amendment template](../doc/docs/meta/amendment-template.md) MUST be
used.

## Testing

- [ ] Unit tests added or updated for the changed logic
- [ ] Integration tests added or updated where the change crosses a
  process boundary
- [ ] E2E tests added or updated where the change is user-visible
- [ ] Manual verification steps recorded below if any check could not
  be automated

Manual verification (if any):

```text
[steps performed]
```

## Tech-Debt Ledger

- [ ] Any compromise this PR introduces is recorded in
  [`/doc/buildlog/tech-debt-ledger.md`](../doc/buildlog/tech-debt-ledger.md)
  as a `[TRIAGE]` or `[OPEN]` entry, and referenced by `TD-NNNN` in
  the affected code

## Security Note

If V.4 applies, summarise what was reviewed and by whom. Otherwise:
"N/A — no security-sensitive surface touched."

```text
security: [reviewed | n/a]
```

## Reviewer Checklist (Reviewer Fills In)

- [ ] Diff matches the summary
- [ ] Constitution self-check is honest
- [ ] Working mode declaration and output are reviewable
- [ ] Documentation knock-on list is complete
- [ ] Test tier placement is correct per
  [`/doc/docs/explanation/concepts/test-taxonomy.md`](../doc/docs/explanation/concepts/test-taxonomy.md)
- [ ] No orphan `TD-NNNN` references
