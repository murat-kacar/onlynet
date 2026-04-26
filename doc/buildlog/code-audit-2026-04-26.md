# Code ↔ Documentation Alignment Pass — 2026-04-26

A horizontal sweep of every `/doc/` tree from the most binding contract
(constitution) to the most concrete artefact (buildlog cross-references),
testing each document against the code that is supposed to honour it
through the lens of the constitution and the documentation charter.

This pass is **not** a replacement for
[`./code-audit-2026-04-25.md`](./code-audit-2026-04-25.md). That file —
including its Section 11 re-review — remains the historical record.
This pass adds a *second*, sequenced examination after the closure of
PRs #6–#16 (TD-0002 step 3, TD-0003, TD-0009, TD-0010, TD-0014 step 3,
TD-0015 partial, TD-0017, TD-0018) so that the working tree as it
stands today is measured against every document that claims something
about it.

## 1. Scope

In scope:

- Every Markdown document under `/doc/docs/**` (54 files at pass open)
  and under `/doc/buildlog/**` (3 files at pass open).
- Every code path that a `/doc/docs/` document claims exists, claims
  to behave a certain way, or names as a contract.
- Every cross-tree link inside `/doc/`. Broken links and orphan
  `TD-NNNN` / `AC-NNN` / `AD-NNNN` references are findings.

Out of scope:

- `/doc/userdocs/` and `/doc/apidocs/`. The charter
  ([`/doc/docs/meta/documentation-charter.md`](../docs/meta/documentation-charter.md))
  records both as stub trees that activate with the first end-user
  persona shipping or the first public API surface; neither has shipped.
- Performance, load, and security pen-test work.
- Anything already covered by an `[OPEN]` row in the tech-debt ledger
  whose payoff plan has not changed since the previous pass.

The pass is conducted by a single auditor (Cascade pair-programming
session) and the changes it lands are committed in tree-sized PRs
(one per phase A–F). No code change lands in a single PR with a
documentation change to a different tree.

## 2. Method

The pass is a **top-down horizontal sweep**. Every document gets a
four-question check; every finding falls into one of four buckets.

### 2.1 The Four Questions

For each document or section under examination:

1. **Claim** — what does this document assert is true?
2. **Evidence** — does the code (build, grep, test, runtime
   behaviour) demonstrate the claim?
3. **Conflict** — is there a contradiction between the document and
   the code? If so, which side is right?
4. **Constitution anchor** — which numbered constitution rule does
   the claim attach to (II.4 capability, III documentation, V review,
   VI.3 ledger, etc.)?

### 2.2 The Four Buckets

Every finding falls into exactly one of these:

- **`aligned`** — claim, evidence, and constitution anchor agree.
  Recorded with a single line; no PR required.
- **`implement`** — claim is real, evidence is missing. The fix is
  code. If the fix is small (under one hour, one file) it lands in
  the same PR as the audit phase that found it; if larger, a new
  `TD-NNNN` is opened with an owner.
- **`clean`** — evidence exists in code, claim is missing or stale.
  The fix is documentation; if documentation is wrong-by-omission,
  the missing claim is added to the right tree per the charter.
- **`correct`** — claim and evidence disagree. Constitution III.1
  ("documentation reflects reality") forces an investigation: which
  side is right? Document errors are corrected in place; code errors
  open a TD or a same-PR fix.

### 2.3 Sequence

The sweep proceeds top-down through the doc tree because more-binding
documents must align before less-binding ones are evaluated against
them. A claim in a how-to that contradicts the constitution is a
constitution finding *first*, a how-to finding second.

| Phase | Tree | Why this order |
| --- | --- | --- |
| A | `meta/` (constitution, charter, release-gate, review-policy, contributing, amendment-template) | Most binding: every other tree is measured against these. |
| B | `reference/` (capability-matrix, decisions ADRs, health-checks, slos, system-overview, runtime-surfaces, render-modes, api/*, database/*, acceptance-criteria, glossary, firmware) | The contracts themselves. |
| C | `explanation/concepts/*` | The *why* of contracts; code is tested against this. |
| D | `how-to/*` | What operators are promised the code does. |
| E | `tutorials/*` | Onboarding; the least dangerous, evaluated last. |
| F | `buildlog/` (audit, ledger, README) | Cross-reference consistency: every `TD-NNNN`, `AC-NNN`, `AD-NNNN`, `RR-*` link must resolve. |

Each phase produces a Section in this document (Sections 4–9) listing
its findings and their buckets. The Closure Log (Section 10) names the
PR or TD that resolved each finding.

### 2.4 Constitution Lens

The constitution
([`/doc/docs/constitution.md`](../docs/constitution.md)) supplies the
top-level grammar of the pass. The rules cited most often:

- **II.3** — Temporary work is technical debt and is visible. Anything
  the audit finds as a compromise without an open ledger row is a
  finding.
- **II.4** — Done means tested, observable, and documented. A
  capability-matrix row claiming `Implemented` without all three is a
  finding.
- **III.1** — Documentation reflects reality. Drives the `correct`
  bucket above.
- **III.2** — Architectural change lands in docs first or alongside
  the code, never after. A code path that exists without a doc claim
  is a finding (`clean` bucket).
- **III.3** — One fact, one place. Duplicated facts across trees are
  a finding.
- **VI.3** — Every incident produces a blameless postmortem. The
  audit tracks whether the postmortem tree has been activated when
  the first incident lands.
- **VII.3** — The tech debt ledger is reviewed at the same cadence as
  the release gate. Stale `[TRIAGE]` rows are a release-gate concern.

The charter
([`/doc/docs/meta/documentation-charter.md`](../docs/meta/documentation-charter.md))
supplies the second-level grammar:

- **Tree boundaries** (`docs/`, `userdocs/`, `apidocs/`, `buildlog/`).
- **Diátaxis** layout inside `docs/` (`tutorials/`, `how-to/`,
  `reference/`, `explanation/`, `meta/`).
- **One fact, one place** (charter > Boundaries section).
- **ADR Status Lifecycle** (`Proposed` / `Accepted` / `Rejected` /
  `Deprecated` / `Superseded`).
- **What goes where: the decision test** (11 ordered questions).

## 3. Repository Inventory

Snapshot at pass open (2026-04-26):

| Tree | File count | Notes |
| --- | --- | --- |
| `docs/constitution.md` | 1 | 7 numbered sections, Scope, Amendment. |
| `docs/meta/` | 6 | README, constitution sayılmaz. amendment-template, contributing, documentation-charter, release-gate, review-policy. |
| `docs/reference/architecture/` | 8 | README, capability-matrix, decisions (ADR index), health-checks, render-modes, runtime-surfaces, slos, system-overview. |
| `docs/reference/api/` | 4 | README, error-codes, internal-api, tenant-api. |
| `docs/reference/database/` | 2 | README, schema. |
| `docs/reference/` (root) | 5 | README, acceptance-criteria, firmware, glossary, plus the four sub-tree READMEs counted above. |
| `docs/explanation/concepts/` | 11 | accessibility, authorization, customer-session-model, data-protection, implementation-patterns, internationalization, multi-tenancy, operational-surfaces, tenant-lifecycle, test-taxonomy, threat-model. |
| `docs/explanation/` (root) | 2 | README + concepts/README. |
| `docs/how-to/` | 11 | backup-and-restore, bootstrap-platform, configure-branch-protection, deploy-to-production, inspect-provisioning-job, provision-tenant, restart-tenant, rotate-secrets, setup-migrations, supervise-processes, README. |
| `docs/tutorials/` | 3 | listed in Section 8 of this pass. |
| `buildlog/` | 3 | README, code-audit-2026-04-25, tech-debt-ledger. |
| **Total under `/doc/docs/`** | **54** | |
| **Total under `/doc/buildlog/`** | **3** | |

`/doc/userdocs/` and `/doc/apidocs/` are stub trees per charter; not
counted.

## 4. Phase A — Meta Tree Findings

Phase opened 2026-04-26. Inputs:
[`/doc/docs/constitution.md`](/doc/docs/constitution.md),
[`/doc/docs/meta/documentation-charter.md`](/doc/docs/meta/documentation-charter.md),
[`/doc/docs/meta/release-gate.md`](/doc/docs/meta/release-gate.md),
[`/doc/docs/meta/review-policy.md`](/doc/docs/meta/review-policy.md),
[`/doc/docs/meta/contributing.md`](/doc/docs/meta/contributing.md),
[`/doc/docs/meta/amendment-template.md`](/doc/docs/meta/amendment-template.md),
[`/doc/docs/meta/README.md`](/doc/docs/meta/README.md).

### A-1 — Charter and `docs/` README mis-spell Diátaxis

- **Bucket:** `correct`.
- **Claim:** the `docs/` tree organises content per the Diátaxis
  framework
  ([`/doc/docs/meta/documentation-charter.md:62`](../docs/meta/documentation-charter.md#docs--engineering-reference),
  [`/doc/docs/meta/documentation-charter.md:93`](../docs/meta/documentation-charter.md#userdocs--end-user-help),
  [`/doc/docs/README.md:8`](../docs/README.md)).
- **Evidence:** all three sites wrote `Diataxis`. The framework name
  ([`https://diataxis.fr`](https://diataxis.fr)) carries an acute
  accent on the first `a`.
- **Conflict:** charter is a `meta/` document; spelling drift in a
  charter undermines its role as a binding contract on subordinate
  trees.
- **Constitution anchor:** III.1 (documentation reflects reality) and
  III.6 (charter governs tree boundaries — the charter is the
  authority that needs to be spelt right).
- **Resolution:** rewritten in PR #17 (this pass) at all three sites.
  No knock-on effect.

### A-2 — Buildlog subtree directories listed in charter and README, but absent on disk

- **Bucket:** `correct`.
- **Claim:** the `buildlog/` tree carries four subtrees per the
  charter
  ([`/doc/docs/meta/documentation-charter.md:142-151`](../docs/meta/documentation-charter.md#buildlog--lessons-learned))
  and the buildlog README
  ([`/doc/buildlog/README.md:10-19`](./README.md)):
  `postmortems/`, `retrospectives/`, `spikes/`, `abandoned/`. Six
  documents link into these subtrees by path
  (`/doc/docs/constitution.md` II.1 and VI.3,
  `/doc/docs/explanation/concepts/data-protection.md`,
  `/doc/docs/meta/amendment-template.md`,
  `/doc/docs/reference/glossary.md`,
  `/doc/docs/how-to/configure-branch-protection.md`).
- **Evidence:** at pass open, none of the four subtree directories
  existed on disk. Every link target in the six documents above
  resolved through Markdown only because the link href was the
  buildlog README (`/doc/buildlog/`) — the *text* claimed a subtree
  that did not exist, while the *href* fell back to the parent. A
  reader who copies the path text into `cd` lands in nothing.
- **Conflict:** charter and README assert subtrees the file system
  does not carry. Constitution III.1 ("documentation reflects
  reality") forces an investigation; the resolution is to materialise
  the subtrees rather than weaken the charter, because the charter
  defines the buildlog's structural pattern (parallel to the
  `userdocs/` and `apidocs/` stub-tree pattern, both of which are
  already populated with `README.md` stubs).
- **Constitution anchor:** III.1, III.3 (one fact, one place — the
  buildlog README is now the single index of the four subtrees), and
  charter > Tree Definitions > buildlog/.
- **Resolution:** PR #17 created four stub README files at
  `/doc/buildlog/postmortems/README.md`,
  `/doc/buildlog/spikes/README.md`,
  `/doc/buildlog/retrospectives/README.md`, and
  `/doc/buildlog/abandoned/README.md`. Each carries the subtree's
  filename format, append-only rule, what-goes-here / does-not, a
  document skeleton (postmortems and spikes) or a writing template
  (retrospectives, abandoned), and a `Status Today` line stating the
  subtree is a stub until the first occurrence. The pattern matches
  the existing `/doc/userdocs/README.md` and
  `/doc/apidocs/README.md` stub-tree shape.

### A-3 — `src/` carries 12 placeholder TODOs without a tech-debt ledger reference

- **Bucket:** `clean` (documentation-side fix is a new ledger entry;
  source-side fix is a comment rewrite that links the ledger entry).
- **Claim:** constitution II.3 forbids "we'll fix it later" without
  a `TD-NNNN` ledger entry; the cross-reference rule in
  [`/doc/buildlog/tech-debt-ledger.md`](./tech-debt-ledger.md#cross-reference-rule)
  states "code or documents that contain a known compromise MUST
  link the relevant ledger entry".
- **Evidence:** a grep for `TODO|FIXME|XXX|HACK` over
  `/src/**/*.{cs,razor,cshtml}` returned 12 distinct comment lines
  (full inventory in TD-0019). None carried a `TD-NNNN` reference.
- **Conflict:** none — the rule is unambiguous; the source-side TODOs
  predate the constitution amendment that adopted II.3 in its
  current form, and were not rewritten when the rule was tightened.
- **Constitution anchor:** II.3, plus the ledger's own cross-
  reference rule.
- **Resolution:** PR #17 opened TD-0019 (this pass added it to the
  triage queue) with the full TODO inventory, and rewrote each of
  the 12 comments to carry a `TODO(TD-0019): ...` prefix. A grep for
  `\bTODO\b` against `src/` now returns the same 13 lines (Cart.razor
  carries three TODOs on adjacent lines), all of which include the
  ledger reference. Bare `TODO` count after the rewrite: 0.

### A-4 — Constitution V.2 / V.4 review-pair rules cannot be met during the pre-1.0 single-author phase

- **Bucket:** `correct` (constitution claim is real; the gap is a
  ledger entry, not a code change).
- **Claim:** constitution V.2 ("every PR has at least one reviewer
  who is not the author") and V.4 ("security-sensitive changes
  require a security review … the reviewer notes 'security: reviewed'
  in the PR") are unconditional invariants on every merge.
- **Evidence:** the repository has had a single active maintainer
  through PRs #6, #7, #11, #12, #16. None of these PRs carries a
  non-author approval and none of the security-sensitive ones (#6,
  #7, #11, #12, #16) carries a `security: reviewed` note. Every merge
  during the pre-1.0 window was effectively a stop-the-line solo
  merge, but the PR bodies do not declare this explicitly per the
  stop-the-line exception in
  [`./review-policy.md`](../docs/meta/review-policy.md#stop-the-line-exception).
- **Conflict:** the constitution rule and the operational reality
  diverge. Charter > Amendment ("if a rule is being routinely
  ignored, that is a constitution bug, fixed by amendment, not by
  silence") forces the gap into a ledger entry until either a second
  active maintainer joins or the constitution is amended with an
  explicit pre-1.0 single-author bypass.
- **Constitution anchor:** V.2, V.4, plus the constitution's own
  Amendment section forbidding silent drift.
- **Resolution:** PR #17 opened TD-0020 (this pass added it to the
  triage queue) with a payoff plan that lists three exits: (1) add a
  second maintainer, (2) amend the constitution per the amendment
  template at
  [`/doc/docs/meta/amendment-template.md`](../docs/meta/amendment-template.md),
  or (3) retroactively review the pre-1.0 PRs in a single follow-up.
  Until step 1 lands, every PR opened during the pre-1.0 window MUST
  carry an explicit `stop-the-line: pre-1.0 single-author` line in
  its body so the bypass is auditable in git history.

### A-5 — Meta tree internal cross-references are intact

- **Bucket:** `aligned`.
- **Claim:** every cross-reference inside `/doc/docs/meta/*.md`
  resolves and the documents are mutually consistent.
- **Evidence:** the README at
  [`/doc/docs/meta/README.md`](../docs/meta/README.md) lists the five
  sibling documents (charter, contributing, review-policy,
  release-gate, amendment-template) and every link target exists.
  The release gate's Documentation block at
  [`/doc/docs/meta/release-gate.md:14-41`](../docs/meta/release-gate.md#documentation)
  cites every reference document by relative path; every cited path
  exists in the working tree per Section 3 of this pass. The
  amendment template's self-consistency checklist
  ([`/doc/docs/meta/amendment-template.md:62-68`](../docs/meta/amendment-template.md))
  enumerates surface IDs, ADR list, AC list, SLI list, capability
  matrix, glossary, and release gate; each of these tables exists
  in the repository (verification of internal consistency belongs to
  Phase B).
- **Constitution anchor:** III.3 (one fact, one place).
- **Resolution:** none required.

## 5. Phase B — Reference Tree Findings

*Pass-in-progress.*

## 6. Phase C — Explanation Tree Findings

*Pass-in-progress.*

## 7. Phase D — How-To Tree Findings

*Pass-in-progress.*

## 8. Phase E — Tutorials Tree Findings

*Pass-in-progress.*

## 9. Phase F — Buildlog Cross-Reference Findings

*Pass-in-progress.*

## 10. Closure Log

Append-only. Each entry names the finding ID, the bucket, the PR or
TD that resolved it, and the date.

| Finding | Bucket | Resolution | Date |
| --- | --- | --- | --- |
| A-1 | `correct` | PR #17 — `Diataxis` → `Diátaxis` rewritten in 3 sites (charter:62, charter:93, docs/README:8). | 2026-04-26 |
| A-2 | `correct` | PR #17 — created stub READMEs at `/doc/buildlog/{postmortems,spikes,retrospectives,abandoned}/README.md`; charter and buildlog README assertions now backed by file system. | 2026-04-26 |
| A-3 | `clean` | PR #17 — opened TD-0019; rewrote 13 TODO comment occurrences across 11 source files to carry `TODO(TD-0019): ...` prefix. Bare TODO count: 0. | 2026-04-26 |
| A-4 | `correct` | PR #17 — opened TD-0020 with three-exit payoff plan; pre-1.0 PRs flagged for retroactive review or constitutional amendment. | 2026-04-26 |
| A-5 | `aligned` | No action required. | 2026-04-26 |

## 11. Sign-Off

Open. The pass remains in progress until every phase Section above is
non-empty and every finding has a closure-log row.

- Auditor: Cascade pair-programming session, opened 2026-04-26.
- Method: top-down horizontal sweep, four-question protocol,
  four-bucket classification.
- Reference passes: [`./code-audit-2026-04-25.md`](./code-audit-2026-04-25.md)
  (original audit + Section 11 re-review).
