# Review Policy

This document specifies what reviewers check when approving a pull
request. The constitution
([`../constitution.md`](../constitution.md), Section V) requires every
change to land via PR with at least one non-author reviewer; this
document fills in *what* the reviewer is responsible for.

## Review Checklist

Every PR is reviewed against the items below. The reviewer marks a PR
approved only when all applicable items pass.

### Correctness

- The change does what its description says.
- Existing tests still pass; new tests cover new invariants.
- Edge cases relevant to the changed surface are exercised.

### Method Fit

- The chosen approach is not only functional; it is also appropriately
  conventional for the problem.
- A simpler, clearer, or better-proven pattern is not being skipped
  without reason.
- The change aligns with current framework guidance, repository
  direction, and nearby surface conventions.
- The implementation or document does not introduce avoidable
  duplication, indirection, coupling, speculative abstraction, or
  private structure.
- The local optimisation does not damage the global shape of the
  product.

### Constitution Conformance

- **Reversibility.** If the change is one-way (per
  [`../constitution.md`](../constitution.md), Section I), an ADR is
  cited and the irreversibility is justified.
- **Done definition.** The change is tested, observable, and documented
  (constitution II.4). A capability moves to `Implemented` in the
  capability matrix only when all three hold.
- **Tech debt.** Any temporary or compromised work added by the change
  appears in
  [`/doc/buildlog/tech-debt-ledger.md`](/doc/buildlog/tech-debt-ledger.md) with
  a `TD-NNNN` identifier.
- **Documentation.** Architectural changes (per constitution III.2)
  update the relevant document in the same PR.
- **Working mode.** The PR declares its primary working mode
  (`documentation`, `implementation`, or `review`), lists any secondary
  modes, and produces the reviewable output required by constitution VIII.5.

### Architecture

- The change does not violate an accepted ADR. If it does, the PR
  proposes the new ADR and links the predecessor as
  `Status: Superseded`.
- New abstractions, dependencies, and indirections are justified.
- The change respects the platform/tenant boundary (AD-0001) and the
  one-host-process-per-side rule (AD-0003).
- The solution follows the best-fit shape already established by
  accepted ADRs, authoritative reference documents, framework
  conventions, and the strongest nearby examples. A deviation is
  reviewed as a design decision, not hidden as an implementation
  detail.

### Self-Consistency

References the same authoritative tables that
[`./contributing.md`](./contributing.md) names: surface IDs, ADR list,
acceptance criteria, SLI list, capability matrix. Cross-document drift
is a review failure.
- The change is consistent not only with the repository's written
  contracts, but also with the repository's established structural
  direction. A correct-but-directionally-wrong change is a review
  failure.

## Security Review

A PR is **security-sensitive** when it touches any of:

- authentication (sign-in, session, password handling, token issuance)
- authorization (policies, role checks, route guards)
- secrets handling (connection strings, API keys, webhook signing)
- data-protection surfaces (PII, payment data, audit log writes)
- the public HTTP surface (route, request body, response body, error
  shape)
- the device WebSocket contract

Security-sensitive PRs require a reviewer with a security focus. The
reviewer notes `security: reviewed` in the PR body and confirms:

1. The change does not weaken an existing invariant in
   [`../reference/acceptance-criteria.md`](../reference/acceptance-criteria.md)
   under "Platform Access", "Tenant Access", or "Auditability".
2. The change does not introduce a credential, secret, or PII into
   logs, error messages, or commit history.
3. The change preserves cross-tenant isolation (AD-0001).
4. The change preserves the customer-session and fresh-QR proof
   invariants when it touches the customer surface.

If a security-sensitive change has no available security-focused
reviewer, the PR is held until one is available. Solo-merge is **not**
permitted for security-sensitive changes.

## Stop-the-Line Exception

When `main` is broken (build red, smoke red, release-gate red, or a
production incident is open), the constitution authorises a contributor
to ship a build-fix PR with a single approving reviewer or, if no other
maintainer is available, a self-merge. The change description MUST state
"stop-the-line" and link the broken signal. A retroactive review is
opened as a follow-up within one working day.

## Branching

- `main` is the protected branch. All changes land on `main` via PR.
- Feature branches are named `<short-topic>` or `<author>/<topic>`.
  Long-lived release branches are not used; releases are tagged
  commits on `main`.
- Force-push to `main` is prohibited. Force-push to a feature branch
  is permitted before review starts; once review has begun, prefer
  additional commits.

## Reviewer Responsibilities

A reviewer who approves a PR takes co-ownership of the change in
production. If the change later causes an incident, the reviewer
participates in the postmortem alongside the author.

Reviewers respond within one working day. A PR awaiting review for
longer is escalated by the author in the team channel.

## Anti-Patterns

- "LGTM" without checks against the items above.
- Approving a PR for a domain the reviewer does not understand instead
  of requesting a domain reviewer.
- Approving a change because it works, while ignoring that a more
  conventional and lower-complexity approach was available.
- Approving a PR that adds a "TODO" without an owner or a tech-debt
  ledger entry.
- Approving a PR that ships a feature without an updated capability
  matrix row.
- Approving a security-sensitive PR without a `security: reviewed`
  note.
- Approving a PR whose declared working mode does not match its output.
- Approving unnecessary novelty in naming, structure, workflow, or
  abstraction where a standard pattern would be clearer.
- Approving a locally clean change that makes the overall product
  shape less coherent.

## Related

- [`../constitution.md`](../constitution.md) — the rules this policy
  enforces
- [`./contributing.md`](./contributing.md) — how the docs themselves
  evolve
- [`./release-gate.md`](./release-gate.md) — the gate every release PR
  passes
- [`./documentation-charter.md`](./documentation-charter.md) — the
  cross-tree rules every PR honours
