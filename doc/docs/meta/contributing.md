# Contributing To Docs

Rules for editing the `docs/` tree specifically. Cross-tree rules live in
[`./documentation-charter.md`](./documentation-charter.md).

## Where Does This Belong?

Before opening a file in `docs/`, run the test from
[`./documentation-charter.md`](./documentation-charter.md#what-goes-where-the-decision-test).
A piece of writing that fits another tree (`userdocs/`, `apidocs/`,
`buildlog/`) does not belong in `docs/`.

For content that does belong in `docs/`, write into the existing
intent-named folders (`tutorials/`, `how-to/`, `reference/`,
`explanation/`, `meta/`). Each folder answers a distinct reader
question; mixing types across folders degrades discoverability.

## Editing Rules

- Prefer updating an existing source-of-truth document before opening
  a new file.
- Keep titles short, clear, and noun-based.
- Architecture decisions live in
  [`../reference/architecture/decisions.md`](../reference/architecture/decisions.md).
  Longer reasoning for a decision lives alongside it in an
  explanation document where the ADR alone cannot carry the context.
- Link with relative paths inside this tree. Cross-tree links use
  absolute paths from the repository root (see the charter).
- Avoid embedding credentials, secrets, or real customer data in
  examples.

## What Belongs Here, What Doesn't

We keep:

- decision rationale (why this, not that)
- trade-offs (what we lose by this choice)
- constraints (what forced the choice)
- rejected alternatives (so they aren't re-proposed)
- current contracts and invariants

We don't keep:

- historical narrative ("we used to do X")
- deprecated code snippets outside their deprecation window
- past API formats outside their deprecation window
- meeting notes
- personal opinions

Test before writing: *"Without this information, can a future
contributor make the same wrong decision, miss the same constraint, or
fail the same task?"* Yes → keep. No → it belongs in `buildlog/` or
nowhere.

## Self-Consistency

Every reference document MUST remain self-consistent with the
authoritative tables it cites:

- the surface-ID table in
  [`../reference/architecture/runtime-surfaces.md`](../reference/architecture/runtime-surfaces.md)
- the ADR list in
  [`../reference/architecture/decisions.md`](../reference/architecture/decisions.md)
- the criteria list in
  [`../reference/acceptance-criteria.md`](../reference/acceptance-criteria.md)
- the SLI list in
  [`../reference/architecture/slos.md`](../reference/architecture/slos.md)
- the capability matrix in
  [`../reference/architecture/capability-matrix.md`](../reference/architecture/capability-matrix.md)
- the strongest nearby example for the same contract, structure, or
  document type

Before merging, confirm:

- the strongest nearby example was checked; there is no avoidable
  divergence in structure, naming, or contract shape
- the document shape is conventional for its audience and problem type

Every architectural change MUST land in the relevant reference or
explanation document **before or alongside** any implementation change
that affects the same contract. When implementation and docs disagree,
treat it as a documentation bug first.
