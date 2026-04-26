# Documentation Charter

This document defines the **shape, audience, and lifecycle** of every
documentation tree in the repository. It is the constitution for
documentation. The project constitution
([`../constitution.md`](../constitution.md), Section III) refers to this
charter for all rules that govern documentation structure.

This charter applies to every contributor. When in doubt about where a
piece of writing belongs, this is the deciding document.

## Why Multiple Trees

A single tree forces a single audience. TabFlow has at least four distinct
audiences whose information needs, lifecycle expectations, and access
patterns conflict:

- engineers and operators building and running the product
- end-users using the product (multiple personas)
- external developers integrating with the product
- the future team learning from past decisions

Mixing these in one tree produces three failure modes:

1. **Audience confusion** — an end-user lands on a database schema page.
2. **Lifecycle drift** — stable contracts and end-user help decay at
   different rates, but coexisting in one tree they decay together.
3. **Search noise** — every reader sees every other reader's pages.

Multiple trees, with explicit boundaries, fix all three.

## The Four Trees

| Tree | Audience | Lifecycle | Direction |
| --- | --- | --- | --- |
| [`docs/`](/doc/docs/) | Engineers, operators | Durable; breaking changes via deprecation | Now (contracts and proposals) |
| [`userdocs/`](/doc/userdocs/) | End-users (owner, manager, cashier, station, customer) | Versioned with product release | Now (guides) |
| [`apidocs/`](/doc/apidocs/) | External developers | Versioned with public API contract | Now (integration) |
| [`buildlog/`](/doc/buildlog/) | The future team | Append-only; never edited or deleted | Backward (lessons) |

Each tree owns one direction in time and one audience. No tree owns two.

Forward-looking work — plans, drafts, spike findings, and experiments —
does **not** get its own tree. Plans live in the tech debt ledger or as
GitHub issues; draft architectural decisions live in `docs/` with
`Status: Proposed` (see
[ADR Status Lifecycle](#adr-status-lifecycle) below); experiments live
on a branch and review happens through PRs; completed exploration that
did not become an ADR is recorded in `buildlog/spikes/` after the
spike closes.

## Tree Definitions

### `docs/` — Engineering Reference

**Audience.** Software engineers, SREs, platform operators, security
reviewers, anyone who is *building* or *running* TabFlow.

**Purpose.** The single source of truth for what TabFlow is, how it
behaves, why it was designed this way, and how to operate it.

**Structure (Diátaxis).**

- `tutorials/` — learning paths
- `how-to/` — task-oriented procedures
- `reference/` — exact contracts (API, DB schema, surfaces)
- `explanation/` — concepts and decision rationale
- `meta/` — rules for editing the tree itself
- `constitution.md` — project-wide process and culture

**Stability.** Stable. A breaking change to a `reference/` document
requires an ADR and a deprecation window in the same PR.

**What goes here.** Decision rationale, contracts, invariants,
operational procedures, architectural concepts.

**What does NOT go here.** End-user click-through guides, exploratory
drafts, postmortems, marketing copy.

### `userdocs/` — End-User Help

**Audience.** Five personas, each in its own subfolder:

- `userdocs/owner/` — cafe owner (commercial controls, billing, staff)
- `userdocs/manager/` — manager (operational controls, reports)
- `userdocs/cashier/` — cashier on `/service`
- `userdocs/station/` — station operator on `/stations/{stationCode}`
- `userdocs/customer/` — diner using `/g/{token}` and `/menu`

**Purpose.** Step-by-step, screenshot-bearing help written in the
language of the persona. Not engineering language.

**Structure.** Per persona, organised by task. No shared "Diátaxis"
folders — each persona's tree is flat enough to be findable.

**Stability.** Versioned with product release. Changes follow product
release cadence, not engineering merges.

**What goes here.** "How do I close a bill?", "How do I add a menu
item?", "Why is my QR code not working?".

**What does NOT go here.** Database schema, API specs, deployment
procedures.

### `apidocs/` — External Integration Reference

**Audience.** External developers integrating with TabFlow's public
contracts (REST, WebSocket, Webhook).

**Purpose.** Authoritative integration reference. OpenAPI / AsyncAPI
specs, code samples, authentication flows.

**Structure.**

- `apidocs/rest/` — REST API reference (generated from OpenAPI where
  possible)
- `apidocs/websocket/` — WebSocket protocol reference
- `apidocs/webhooks/` — outbound webhook payloads and signing

**Stability.** Versioned with public API contract. Breaking changes
follow API deprecation policy (separate document when API goes public).

**What goes here.** Endpoint catalogues, request/response schemas,
auth flows, rate-limit semantics, webhook signing.

**What does NOT go here.** Internal endpoints, control-plane APIs,
implementation notes.

**Status today.** TabFlow has no public API. This tree starts as a
stub and activates the moment a public endpoint exists.

### `buildlog/` — Lessons Learned

**Audience.** The future team — engineers who will face problems we
have already faced.

**Purpose.** Append-only record of what happened, what we learned,
and what changed because of it.

**Structure.**

- `buildlog/postmortems/YYYY-MM-DD-<incident>.md` — production or
  preview incidents
- `buildlog/retrospectives/YYYY-Qn-<topic>.md` — retrospective notes
  on a release, milestone, or sprint
- `buildlog/spikes/<topic>.md` — completed spike outcomes that didn't
  become an ADR but contain useful findings
- `buildlog/abandoned/<topic>.md` — approaches we tried and rejected,
  with the reasoning preserved
- `buildlog/tech-debt-ledger.md` — single append-only ledger of every
  accepted technical-debt entry (open, closed, accepted, abandoned)

**Stability.** Append-only. Documents are never edited after merge
(except for typo fixes). They are never deleted. New facts produce
new documents that link the old. The tech debt ledger is append-only
by row: an entry's `[OPEN]` status transitions to `[CLOSED]`,
`[ACCEPTED]`, or `[ABANDONED]` and a closure block is appended; the
original entry is never edited or deleted.

**What goes here.** Postmortems, retrospectives, abandoned approaches,
spike findings worth preserving.

**What does NOT go here.** Active plans (those go to
`buildlog/tech-debt-ledger.md` or a GitHub issue), draft architectural
decisions (those live in `docs/reference/architecture/decisions.md`
with `Status: Proposed`), contracts (those go to `docs/`), end-user
help.

**Status today.** TabFlow has no incidents to postmortem yet. This
tree starts as a stub and activates with the first incident.

## Boundaries: One Fact, One Place

Every fact lives in exactly one tree. The other trees link to it.

Concrete examples:

- The list of tenant database tables → `docs/reference/database/schema.md`.
  `userdocs/manager/` does not duplicate it; it does not need it.
- A cashier's "how to close a bill" walkthrough → `userdocs/cashier/`.
  `docs/` does not duplicate it; engineers don't read it.
- The reasoning for choosing PostgreSQL → `docs/reference/architecture/decisions.md`
  AD-0007. `buildlog/` does not duplicate it; the rationale is a
  current decision, not a historical artefact.
- An incident on 2026-05-01 → `buildlog/postmortems/`.
  `docs/` may *link* to it from a "Known Failure Modes" section but
  does not include the postmortem narrative.

When a fact appears to need duplication, the right move is almost always
to refactor: extract the fact to its tree-of-record and link from the
others.

## Lifecycle Per Tree

| Tree | Edit Policy | Delete Policy | Versioning |
| --- | --- | --- | --- |
| `docs/` | Edit freely; breaking changes via deprecation | Stable contracts deleted only after deprecation window | Tracks code (no separate version) |
| `userdocs/` | Edit per release | Deprecated walkthroughs archived to `buildlog/` | Tracks product release |
| `apidocs/` | Edit per API version | Deprecated endpoints follow API deprecation policy | Tracks public API contract |
| `buildlog/` | Append only (typo fixes excepted) | Never delete | Date in filename |

## ADR Status Lifecycle

Forward-looking architectural work lives in `docs/reference/architecture/decisions.md`
as an ADR carrying a `Status` field. The five legal statuses are:

| Status | Meaning |
| --- | --- |
| `Proposed` | Draft under discussion. The ADR is written, linked in PRs, and reviewed; it is **not** binding until accepted. |
| `Accepted` | The decision is live. Code, contracts, and other docs MUST conform. |
| `Rejected` | The proposal was reviewed and declined. The ADR stays in place so the same idea is not re-proposed without new context. |
| `Deprecated` | An accepted decision that is being phased out. A successor ADR (also `Accepted`) MUST exist and be referenced. |
| `Superseded` | An accepted decision replaced by a successor. The successor's `Status: Accepted` ADR links back; this ADR's `Status: Superseded` block links forward. |

A `Proposed` ADR is the canonical home for forward-looking design work
that could otherwise tempt a contributor to open a draft elsewhere. It
is **not** noise: a clearly-marked `Proposed` row in the ADR index is
easier to find, harder to lose, and naturally migrates to `Accepted`
or `Rejected` through the same review channel as every other ADR.

## Migration Rules

Information moves between trees in defined ways:

- `docs/` (`Status: Proposed` ADR) → `docs/` (`Status: Accepted` ADR)
  when the decision is reviewed and accepted. The same file simply
  flips status.
- `docs/` (`Status: Proposed` ADR) → `docs/` (`Status: Rejected` ADR)
  when the decision is reviewed and declined. The ADR stays so the
  reasoning is preserved.
- Spike work on a branch → `buildlog/spikes/` after the spike closes
  if the findings are worth keeping but did not produce an ADR.
- Spike work on a branch → `buildlog/abandoned/` if the approach was
  rejected and the reasoning is worth preserving without an ADR.
- `docs/` → `buildlog/` **never**. A superseded `docs/` decision stays
  in `docs/` (with `Status: Superseded`) so the trail is one click
  away from current readers.
- `userdocs/` → `buildlog/` for retired user-facing flows whose
  walkthroughs no longer apply but whose existence is interesting.
- Anything → public-facing tree (`userdocs/`, `apidocs/`) requires
  review for sensitive details (no internal hostnames, no operator
  contact info, no implementation noise).

## What Goes Where: The Decision Test

For any piece of writing, ask in order:

1. **Is it about a current contract or invariant?** → `docs/reference/`
2. **Is it about how to perform an operational task?** → `docs/how-to/`
3. **Is it about understanding a concept or rationale?** → `docs/explanation/`
4. **Is it a learning path for a newcomer engineer?** → `docs/tutorials/`
5. **Is it for an end-user persona?** → `userdocs/<persona>/`
6. **Is it for an external API consumer?** → `apidocs/`
7. **Is it a draft architectural decision?** → `docs/reference/architecture/decisions.md` with `Status: Proposed`
8. **Is it a plan or backlog item?** → `buildlog/tech-debt-ledger.md` with a `TD-NNNN` identifier
9. **Is it work-in-progress on a branch?** → stays on the branch; the PR review is its home
10. **Is it a record of what happened or what we learned?** → `buildlog/`
11. **None of the above?** → it probably should not be written. Re-ask
    what audience needs it.

## What Does NOT Belong Anywhere

These never go into any tree:

- Personal opinions framed as facts
- Meeting transcripts
- Vendor marketing material
- Credentials, secrets, real customer data, DSR case records, or DSR exports
- Half-finished sentences left as "TODO" without an owner
- Historical narrative ("we used to do X, then Y, now Z") — the
  current state is what we document; a `Status: Superseded` ADR is
  not narrative, it is a current decision marker

## The Decision Test (Inverse)

To decide if existing content should stay or go:

> **"Without this, can a future contributor make the same wrong
> decision, miss the same constraint, or fail the same task?"**
>
> - Yes → keep, possibly move to a more discoverable tree.
> - No → delete. It is biography, not documentation.

## Cross-Tree Linking

- Every cross-tree link uses an absolute path from the repository
  root: e.g. `/doc/docs/reference/database/schema.md`. Relative paths
  break when documents move between trees.
- `buildlog/` may link out to anywhere; other trees link *into*
  `buildlog/` only for `docs/` "Known Failure Modes" sections,
  release-gate evidence records (for example recovery drill
  retrospectives), and ledger references (`TD-NNNN`).

## Amendment

This charter is a `docs/meta/` document. Changes follow the constitution's
amendment rule:

- a PR that explains the motivating experience,
- reviewed by every active maintainer,
- amends both the charter and any tree's `README.md` that is affected.

A change to the charter is a one-way door (Section I of the constitution).
Treat it accordingly.
