---
name: Feature Request
about: Propose a new capability or a meaningful change to an existing one
title: "[Feature] "
labels: ["feature", "needs-triage"]
assignees: []
---

## Problem

What problem does this feature solve? Describe the user or operator
pain in one paragraph. If a postmortem or a recurring support thread
motivated this request, link it.

## Proposed Capability

What should exist that does not exist today? Describe the visible
behaviour, not the implementation.

## Working Mode

Primary mode expected after triage:

- [ ] Documentation
- [ ] Implementation
- [ ] Review

Secondary modes, if already known:

- [ ] Documentation
- [ ] Implementation
- [ ] Review

## Affected Audience

- [ ] Customer (diner)
- [ ] Cashier
- [ ] Manager
- [ ] Owner
- [ ] Station operator
- [ ] Platform admin
- [ ] External developer (API consumer)
- [ ] Operator running TabFlow

## Likely Architectural Impact

- [ ] No architectural impact (within an existing capability)
- [ ] New runtime surface — needs a Surface ID and an entry in
  [`/doc/docs/reference/architecture/runtime-surfaces.md`](../../doc/docs/reference/architecture/runtime-surfaces.md)
- [ ] New external HTTP contract — needs an OpenAPI artefact in
  [`/doc/apidocs/`](../../doc/apidocs/)
- [ ] New ADR — needs `Status: Proposed` entry in
  [`/doc/docs/reference/architecture/decisions.md`](../../doc/docs/reference/architecture/decisions.md)
- [ ] Personal data implication — needs an update in
  [`/doc/docs/explanation/concepts/data-protection.md`](../../doc/docs/explanation/concepts/data-protection.md)
- [ ] New SLI / SLO target — needs an update in
  [`/doc/docs/reference/architecture/slos.md`](../../doc/docs/reference/architecture/slos.md)

## Acceptance Criteria

What is true when this feature ships? List concrete invariants. Each
becomes an `AC-NNN` entry in
[`/doc/docs/reference/acceptance-criteria.md`](../../doc/docs/reference/acceptance-criteria.md).

- [ ]
- [ ]
- [ ]

## Out Of Scope

What this feature explicitly does not do. List the natural neighbours
that someone might assume are included; calling them out prevents
scope creep during implementation.

## Open Questions

What is still unclear? Triage closes these before the issue is
ready-for-implementation.
