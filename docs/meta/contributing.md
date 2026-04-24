# Contributing To Docs

Rules for editing this documentation tree.

- Write into the existing intent-named folders (`tutorials/`,
  `how-to/`, `reference/`, `explanation/`, `meta/`). Each folder
  answers a distinct reader question; mixing types across folders
  degrades discoverability.
- Prefer updating an existing source-of-truth document before opening
  a new file.
- Keep titles short, clear, and noun-based.
- Architecture decisions live in
  [`../reference/architecture/decisions.md`](../reference/architecture/decisions.md).
  Longer reasoning for a decision lives alongside it in an
  explanation document where the ADR alone cannot carry the context.
- Link with relative paths so the tree renders correctly on any host.
  Do not hard-code the tree root inside a document that lives inside
  the tree itself.
- Avoid embedding credentials, secrets, or real customer data in
  examples.
- Every reference document MUST remain self-consistent with the
  authoritative tables it cites: the surface-ID table in
  [`../reference/architecture/runtime-surfaces.md`](../reference/architecture/runtime-surfaces.md),
  the ADR list in
  [`../reference/architecture/decisions.md`](../reference/architecture/decisions.md),
  the criteria list in
  [`../reference/acceptance-criteria.md`](../reference/acceptance-criteria.md),
  and the SLI list in
  [`../reference/architecture/slos.md`](../reference/architecture/slos.md).
- Every architectural change MUST land in the relevant reference or
  explanation document **before or alongside** any implementation
  change that affects the same contract. When implementation and
  docs disagree, treat it as a documentation bug first.
