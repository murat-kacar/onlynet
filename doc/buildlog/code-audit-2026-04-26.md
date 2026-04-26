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

The reference tree has 19 documents totalling ~3 982 lines; Phase B is
sequenced into three sub-passes:

- **B-1** — the five self-consistency tables that
  [`/doc/docs/meta/contributing.md`](../docs/meta/contributing.md#self-consistency)
  names: capability matrix, acceptance criteria, glossary, runtime
  surfaces, SLOs.
- **B-2** — `decisions.md` (the ADR set).
- **B-3** — the remaining reference documents: `api/*`, `database/*`,
  `health-checks.md`, `system-overview.md`, `render-modes.md`,
  `firmware.md`.

This Section records B-1 findings; B-2 and B-3 land in subsequent PRs.

### B-1.1 — Capability matrix carries 8 stale rows after the PR #6–#16 cluster

- **Bucket:** `clean`.
- **Claim:** the capability matrix at
  [`/doc/docs/reference/architecture/capability-matrix.md`](../docs/reference/architecture/capability-matrix.md)
  is the single document that tracks the implementation status of
  every capability against the baseline architecture; per
  [`/doc/docs/reference/glossary.md`](../docs/reference/glossary.md#capability-matrix)
  a capability is `Implemented` only when all three Done criteria
  (tested, observable, documented) hold.
- **Evidence:** at pass open the matrix described 8 capabilities in
  language predating the PR #6–#16 cluster:
  - **Platform Identity store** — claimed "bootstrap admin command
    pending" although `BootstrapAdminCommand` landed in PR #9 and
    the must-change-password redirect in PR #16.
  - **Bootstrap platform admin via CLI** — claimed `Target` although
    the CLI exists in source; should read `In progress` (operator
    half pending).
  - **Tenant schema via EF Core migrations** — claimed "design-time
    factories pending" although the factories live at
    `/src/infra/postgres/DesignTime/` and the tenant `InitialCreate`
    scaffold (586 lines, 64 `CreateTable`) landed in PR #10.
  - **Customer session with server-side cart** — did not mention the
    device-binding (TD-0017) and idempotency (TD-0018) gates that
    landed in PRs #11 and #12.
  - **Fresh-QR checkout proof on submit** — claimed `Target`
    although `OrderService.SubmitAsync` enforces the four halves of
    AC-030..AC-036 since PR #7 (TD-0015 step 4).
  - **Structured logging via Serilog** — did not mention the
    `LoggerMessage` adoption that landed in PR #15 (TD-0014 step 3).
  - **English-first lint enforcement** — claimed `Target` although
    the `TabFlow.Analyzers` project ships `TF0001` since PR #14
    (TD-0009 steps 1–3).
  - **GitHub Actions CI workflows** — did not mention the Unit /
    Integration test split that landed in PR #13 (TD-0010 step 3).
  Additionally the row for **Test taxonomy via xUnit Traits** did
  not exist at all even though TD-0010 steps 1–3 closed in PR #13.
- **Conflict:** documentation lagged code by one full PR cluster.
  Constitution III.2 requires architectural change to land in docs
  first or alongside the code, never after; the gap here is the
  knock-on effect of PRs #6–#16 not refreshing this row each time.
- **Constitution anchor:** II.4 (Done = tested + observable +
  documented), III.1 (documentation reflects reality), III.2.
- **Resolution:** PR #18 rewrote the 8 stale rows to name the
  shipping PR and TD, and added a 9th row for `Test taxonomy via
  xUnit Traits`. Going forward, the PR template at
  [`/doc/docs/meta/amendment-template.md`](../docs/meta/amendment-template.md)
  already lists the capability matrix in its self-consistency
  checklist; reviewers MUST tick that row when a PR moves a
  capability forward.

### B-1.2 — Customer-tier HTTP endpoints declared on `/api/public/*` prefix but routed under `/api/<noun>`

- **Bucket:** `correct` (the runtime-surfaces map was the aspirational
  shape; the shipping code is the real shape; the gap is its own TD).
- **Claim:**
  [`/doc/docs/reference/architecture/runtime-surfaces.md`](../docs/reference/architecture/runtime-surfaces.md#tenant-host--http-endpoints)
  listed four customer-tier endpoints under `/api/public/*`:
  `profile`, `catalog`, `session`, `orders`.
- **Evidence:** a grep over `/src/apps/tenant/Controllers/Api/*.cs`
  showed only `PublicOrdersController` mounted at `/api/public/orders`.
  The other three customer surfaces ship as `/api/menu`,
  `/api/cart`, and `/api/sessions/open` / `/api/sessions/{ticketId}`,
  separated from the staff tier by `[AllowAnonymous]` attributes
  per TD-0015 step 2 rather than by route prefix. The audit closure
  for re-review finding RR-C2 ("tenant API controllers expose every
  endpoint anonymously") consequently relied on attribute audit, not
  route audit.
- **Conflict:** `runtime-surfaces.md` is the single document the rest
  of the docs read back into for routes; an aspirational shape
  presented as current routes is a constitution III.1 problem.
- **Constitution anchor:** III.1 (documentation reflects reality),
  III.4 (stable contracts have a deprecation path; the prefix
  migration MUST honour this).
- **Resolution:** PR #18 (a) rewrote the runtime-surfaces HTTP
  endpoints table to reflect the shipping route map (8 endpoint
  groups split into customer / staff tiers, each citing the
  controller and the `[Authorize]` policy), and (b) opened TD-0021
  with a four-step migration to `/api/public/*` (shim controllers,
  Blazor caller switch, deprecation HTTP 410 on legacy routes,
  `tenant-api.md` + OpenAPI update).

### B-1.3 — `acceptance-criteria.md` is now aligned with the shipping behaviour for AC-005, AC-006, AC-030–AC-036

- **Bucket:** `aligned`.
- **Claim:** AC-005 ("first platform admin … MUST be forced through
  `/change-password` on first authenticated request"), AC-006
  (bootstrap-admin refuses on populated `AspNetUsers`), AC-030
  (open customer session for the submitting device), AC-031 (fresh
  QR proof), AC-032 (consumed proof rejected), AC-035 (empty cart
  rejected), AC-036 (successful submit closes the session).
- **Evidence:** AC-005 closed by PR #16 (TD-0002 step 3,
  `PasswordChangeRequiredMiddleware`). AC-006 closed by PR #9
  (TD-0002 step 1, `BootstrapAdminCommand` AnyAsync gate). AC-030
  closed by PR #11 (TD-0017 device-binding cookie). AC-031 / AC-032
  closed by PR #7 (TD-0015 step 4 token consumption). AC-035 closed
  by `OrderService.SubmitAsync` empty-cart guard. AC-036 closed by
  PR #7 (session-close + token-consume in same `SaveChangesAsync`).
- **Constitution anchor:** II.4 (capability matrix tracks these as
  `In progress` until the integration tests in TD-0015 step 6,
  TD-0017 step 4, and TD-0018 step 3 land).
- **Resolution:** none required for the AC text; the matrix
  evidence for these ACs is addressed in B-1.1.

### B-1.4 — `slos.md` surface ID references resolve

- **Bucket:** `aligned`.
- **Claim:** the SLO table cites surface IDs `P-02`..`P-07` and
  `T-06`..`T-16`, plus `T-13` and `T-16` by name in the event-push
  SLI.
- **Evidence:** `runtime-surfaces.md` declares P-01..P-08 (platform)
  and T-01..T-16 (tenant); every SLO reference resolves into the
  declared range.
- **Constitution anchor:** III.3 (one fact, one place — surface IDs
  declared in `runtime-surfaces.md`, cited from `slos.md`).
- **Resolution:** none required.

### B-1.5 — `glossary.md` cross-references resolve and the post-Phase-A `/doc/buildlog/spikes/` link now points to a real path

- **Bucket:** `aligned`.
- **Claim:** every cross-reference inside the glossary resolves,
  including the previously text-vs-href-divergent
  `/doc/buildlog/spikes/` mention.
- **Evidence:** glossary line 264 cites `/doc/buildlog/spikes/`; the
  Phase A resolution (A-2) created the `spikes/README.md` stub, so
  the path is now a real directory and the cited href falls onto a
  real document. The same is true for the constitution's II.1 / VI.3
  references and the data-protection / amendment-template / branch-
  protection mentions of `postmortems/`.
- **Constitution anchor:** III.1, III.3.
- **Resolution:** none required (closure inherited from Phase A).

### Phase B-2 — `decisions.md` ADR conformance

`decisions.md` carries 15 ADRs (`AD-0001` through `AD-0015`). Phase
B-2 walks each ADR and asks whether the shipping code, schema, and
tooling are consistent with the decision text. The findings below are
recorded against the ADR's actual claim, not against the audit's
prior interpretation of it (the previous pass at
[`./code-audit-2026-04-25.md`](./code-audit-2026-04-25.md#section-6)
mis-attributed several ADR topics; this pass re-reads each ADR
end-to-end before classifying).

### B-2.1 — ADR status taxonomy is intact and unanimous

- **Bucket:** `aligned`.
- **Claim:** the documentation charter
  ([`/doc/docs/meta/documentation-charter.md`](../docs/meta/documentation-charter.md#adr-status-lifecycle))
  defines five legal statuses (`Proposed`, `Accepted`, `Rejected`,
  `Deprecated`, `Superseded`); `decisions.md` line 14 declares the
  same five.
- **Evidence:** all 15 ADRs carry `Accepted` (no `Proposed` drafts,
  no `Deprecated`, no `Superseded` chains). This is the expected
  shape for a pre-1.0 single-author repository where every ADR
  recorded so far has been adopted in the same PR that introduced
  it.
- **Constitution anchor:** III.6 (charter governs subordinate
  trees), I.1 (architecture changes go through ADRs).
- **Resolution:** none required.

### B-2.2 — AD-0001 / AD-0002 / AD-0005 / AD-0006 / AD-0007 / AD-0009 / AD-0011 / AD-0012 / AD-0013 / AD-0014 are aligned with shipping code

- **Bucket:** `aligned`.
- **Evidence per ADR:**
  - **AD-0001 (Platform / Tenant separation)** — `/src/apps/platform/`
    and `/src/apps/tenant/` are independent host projects with
    independent `DbContext` types (`PlatformDbContext`,
    `TenantDbContext`); no cross-database references in either
    direction.
  - **AD-0002 (ASP.NET Core 10 + Blazor Web App)** — `Directory.Build.props`
    pins `<TargetFramework>net10.0</TargetFramework>` repository-wide;
    both hosts are Blazor Web App projects.
  - **AD-0005 (ASP.NET Core Identity)** — both hosts call
    `AddIdentity<ApplicationUser, ...>` and use the framework's
    `UserManager` / `SignInManager`; the bootstrap-admin command
    landed in PR #9 with `UserManager.CreateAsync`.
  - **AD-0006 (in-process event bus)** — `EventSubscriptionService`
    and the channel-backed dispatcher ship under
    `/src/apps/tenant/Services/EventSubscriptionService.cs` and
    `/src/apps/tenant/WebSocket/`.
  - **AD-0007 (PostgreSQL 17)** — Npgsql connection strings; no
    other database driver referenced.
  - **AD-0009 (standalone migrations project + design-time
    factories)** — `/src/infra/postgres/TabFlow.Migrations.csproj`
    exists with `DesignTime/PlatformDbContextFactory.cs` and
    `DesignTime/TenantDbContextFactory.cs`. Both hosts and the
    platform worker reference the assembly.
  - **AD-0011 (SemVer + tagged commits + Keep-a-Changelog format)** —
    no release tags exist yet (pre-1.0); `CHANGELOG.md` carries an
    `[Unreleased]` block in Keep-a-Changelog 1.1.0 format. Aligned
    by absence; the rule activates at the first release tag.
  - **AD-0012 (Apache 2.0)** — `LICENSE` and `NOTICE` exist at the
    repository root.
  - **AD-0013 (GitHub Actions CI)** — `.github/workflows/`
    contains `pr.yml`, `main.yml`, and `release.yml`; the previous
    pass at
    [`./code-audit-2026-04-25.md`](./code-audit-2026-04-25.md#section-6)
    listed `release.yml` as missing, which was incorrect — the file
    landed before that pass was opened. This pass treats the prior
    note as an audit-side error, not a code-side one.
  - **AD-0014 (`.editorconfig` + `Directory.Build.props`)** — both
    files exist and ship the analyzer-warnings-as-errors rule;
    `TF0001` (English-first identifier analyzer) sits on top of
    that contract per AD-0015 / TD-0009.
- **Resolution:** none required.

### B-2.3 — AD-0004 mixed render modes: declared baseline holds, code-side `@rendermode` annotation tracked under TD-0016

- **Bucket:** `aligned with caveat`.
- **Claim:** AD-0004 baseline — Static SSR for tenant customer and
  authentication surfaces, Interactive Server for staff surfaces —
  is the contract.
- **Evidence:** the contract holds in `runtime-surfaces.md`'s render
  mode column. The code-side `@rendermode InteractiveServer`
  annotation is missing from the staff Razor pages (TD-0016); the
  audit re-review finding RR-H1 ("AD-0004 not exercised") tracks the
  same gap.
- **Resolution:** no ADR-text change needed. TD-0016 owns the code
  fix.

### B-2.4 — AD-0008 EF Core schema authority: TD-0003 covers the unmet half

- **Bucket:** `aligned with caveat`.
- **Claim:** "Tenant bootstrap applies the committed migration
  history rather than executing handwritten idempotent ALTER
  scripts."
- **Evidence:** the committed migration tree exists for both contexts
  (PR #8 platform, PR #10 tenant scaffold per TD-0003 step 1–2).
  The platform worker's `MigrateAsync()` call against tenant
  databases (TD-0003 step 3) and the drop+apply+verify recipe
  (step 4) remain open.
- **Resolution:** no ADR-text change needed. TD-0003 owns the code
  fix.

### B-2.5 — AD-0010 bootstrap CLI: TD-0002 closure path tracks the unmet half

- **Bucket:** `aligned with caveat`.
- **Claim:** "EF Core migrations never INSERT into `AspNetUsers`";
  the first admin is created by a `bootstrap-admin` command that
  refuses on populated databases, generates a CSPRNG password,
  writes an `auth.bootstrap` audit row, and forces a password change
  on first sign-in.
- **Evidence:** PR #9 (TD-0002 step 1) shipped the command exactly
  as the ADR describes; PR #16 (TD-0002 step 3) shipped the
  must-change-password redirect via
  `PasswordChangeRequiredMiddleware`. The operator-action half
  (`drop+apply+verify` + run command on the deployed host) remains
  open as TD-0002 step 4 / step 5.
- **Resolution:** no ADR-text change needed. TD-0002 owns the
  operator-action work.

### B-2.6 — AD-0015 English-first: TF0001 ships, IStringLocalizer half tracked under TD-0011

- **Bucket:** `aligned with caveat`.
- **Claim:** "Translation happens at the presentation layer only";
  identifiers in `/src/` and `/tests/` are ASCII-only English; the
  markdown lint workflow flags non-English content in `/doc/`.
- **Evidence:** PR #14 (TD-0009 step 1–3) shipped `TF0001` (the
  English-first identifier analyzer) — controller, action, type,
  property, field, event, and parameter declarations now break the
  build on a non-ASCII letter. The Phase A finding A-3 caught and
  removed one Turkish word ("fiilen") that had slipped into a ledger
  comment, confirming the analyzer covers source identifiers but
  not free prose. The `IStringLocalizer<T>` + `*.resx` half is
  tracked as TD-0011.
- **Resolution:** no ADR-text change needed. TD-0009 step 4–5 cover
  the analyzer's regression tests and `AnalyzerReleases` files;
  TD-0011 covers the translation half.

### B-2.7 — AD-0003 trade-off: read-only API controllers bypass the application service layer

- **Bucket:** `implement`.
- **Claim:** AD-0003's tradeoffs paragraph requires that "the
  internal layer boundary (host → application service → domain)
  must remain explicit in code."
- **Evidence:** of the seven controllers in
  `/src/apps/tenant/Controllers/Api/`:
  - 3 inject application services (`CartController` →
    `ICartService`, `PublicOrdersController` → `IOrderService`,
    `SessionsController` → `ICustomerSessionService`); these are
    post-TD-0015 / post-TD-0017 work.
  - 4 inject `TenantDbContext` directly and run LINQ inline
    (`KitchenController`, `MenuController`, `OrdersController`,
    `TablesController`).
  The 4-of-7 ratio means a future contributor reading the dominant
  pattern picks raw `_context` and re-creates the gap. AD-0003 does
  not say "every controller goes through a service" but does say
  the boundary "must remain explicit"; today it is observed
  inconsistently.
- **Conflict:** the ADR is correct as written; the gap is on the
  code side. This is `implement` rather than `clean` because
  bringing the four controllers in line is a non-trivial code
  change with its own integration-test obligations.
- **Constitution anchor:** III.5 (architectural decisions land in
  ADRs and the code follows them).
- **Resolution:** PR #19 (this pass) opened TD-0022 with a
  four-step migration: introduce three read services
  (`IKitchenReadService`, `IMenuReadService`, `ITableReadService`)
  and fold the order detail / by-session reads into the existing
  `IOrderService`; rewrite the four controllers to depend on the
  read services; ship a unit test per service (transactional
  fixture from TD-0010 step 5); add a Roslyn analyzer rule
  (extending the `TabFlow.Analyzers` project from TD-0009) that
  flags `DbContext` injection on `ControllerBase` derivatives.
  PR #20 (Phase B-3 cross-check) extended the entry to cover the
  two platform-side controllers (`TenantsController`,
  `JobsController`) that follow the same anti-pattern; the payoff
  plan now lists `ITenantRegistryService` and
  `IProvisioningJobReadService` alongside the tenant-side services
  and rewrites the controller count from "four" to "six".

### Phase B-3 — `api/`, `database/`, and remaining `architecture/`

Phase B-3 walks the seven remaining reference documents:
[`/doc/docs/reference/api/internal-api.md`](../docs/reference/api/internal-api.md),
[`/doc/docs/reference/api/tenant-api.md`](../docs/reference/api/tenant-api.md),
[`/doc/docs/reference/api/error-codes.md`](../docs/reference/api/error-codes.md),
[`/doc/docs/reference/database/schema.md`](../docs/reference/database/schema.md),
[`/doc/docs/reference/architecture/system-overview.md`](../docs/reference/architecture/system-overview.md),
[`/doc/docs/reference/architecture/health-checks.md`](../docs/reference/architecture/health-checks.md),
[`/doc/docs/reference/architecture/render-modes.md`](../docs/reference/architecture/render-modes.md),
[`/doc/docs/reference/firmware.md`](../docs/reference/firmware.md).

### B-3.1 — `internal-api.md` mixes public and staff-tier surfaces; lists routes that no longer ship

- **Bucket:** `clean` (the document is wrong; the rewrite is large
  enough to track as its own TD rather than land in this pass).
- **Claim:** `internal-api.md` documents "internal HTTP endpoints —
  the admin and staff API used by the platform admin UI and the
  tenant admin console" and excludes "the public, externally
  addressable HTTP surface".
- **Evidence:** the document has four structural problems:
  1. Sections "Sessions API", "Cart API", and the customer half of
     "Orders API" describe customer-tier endpoints that belong in
     [`tenant-api.md`](../docs/reference/api/tenant-api.md).
  2. The customer order path is listed as `POST /api/orders/submit`;
     the real shipping route is `POST /api/public/orders` per
     `PublicOrdersController` (PR #6, TD-0015 step 3).
  3. The actually shipping staff-tier surface (`/api/orders/{id}`,
     `/api/orders/session/{sessionId}`, `/api/kitchen/orders`,
     `/api/kitchen/items/{id}/status`,
     `/api/sessions/{sessionId}/close`, `/api/tables`,
     `/api/tables/{id}`) is not documented at all.
  4. Every entry says `Policy: None` without explaining whether
     this is a public-tier `[AllowAnonymous]` (TD-0015 step 2) or a
     missing authorisation contract.
- **Conflict:** the document is the first hit for a reviewer asking
  "what HTTP do we expose internally"; a drift this large means the
  answer is wrong. Constitution III.1 (documentation reflects
  reality) and III.2 (architectural change lands in docs first or
  alongside) both fail here.
- **Constitution anchor:** III.1, III.2.
- **Resolution:** PR #20 added a TD-0023 banner at the top of
  `internal-api.md` pointing readers at `runtime-surfaces.md` /
  `tenant-api.md` until the rewrite lands. Opened TD-0023 with a
  five-step plan: banner (done), move customer-tier sections to
  `tenant-api.md`, replace the stale order-submit entry, document
  the actually shipping staff endpoints, and close with the AD-0003
  HTTP-is-the-exception note.

### B-3.2 — `tenant-api.md` aspirational `/api/public/*` triple, plus `Idempotency-Key` mis-located on the header

- **Bucket:** `correct`.
- **Claim:** `tenant-api.md` lists four customer-tier endpoints
  under `/api/public/*` (`profile`, `catalog`, `session`, `orders`)
  and states that "the request MUST carry an `Idempotency-Key`
  header".
- **Evidence:**
  - **Aspirational triple.** Phase B-1 finding B-1.2 already showed
    that only `POST /api/public/orders` ships under the prefix
    today; the other three are routed under `/api/menu`, `/api/cart`,
    `/api/sessions/*`. TD-0021 owns the migration. `tenant-api.md`
    inherited the same aspirational shape and did not link the
    follow-up.
  - **Idempotency contract location.** The shipping code at
    [`PublicOrdersController.SubmitOrder`](/src/apps/tenant/Controllers/Api/PublicOrdersController.cs)
    binds `[FromBody] SubmitOrderRequest` and the request record
    declares an `IdempotencyKey` field; TD-0018 enforces uniqueness
    via a unique index over `(SessionId, IdempotencyKey)` on the
    `orders` table (migration
    `20260425214627_AddOrderIdempotencyKey`). The body field is the
    contract. The "header" claim was aspirational and never landed.
- **Conflict:** III.1 (documentation reflects reality). The header
  claim would require a server-side change to read
  `HttpContext.Request.Headers["Idempotency-Key"]`, which the
  shipping code does not do.
- **Constitution anchor:** III.1, III.4 (a stable contract has a
  clear shape; the body field is that shape).
- **Resolution:** PR #20 (a) added a "Migration status (TD-0021)"
  callout to each of the three aspirational customer-tier sections
  naming the actual shipping route, and (b) rewrote the order
  submission section to describe the body's `idempotencyKey` field,
  cite TD-0017 (device-binding cookie verification) and TD-0018
  (unique index), and explicitly say "not from an `Idempotency-Key`
  HTTP header".

### B-3.3 — `schema.md` did not document the TD-0017 / TD-0018 columns

- **Bucket:** `clean`.
- **Claim:** the schema reference is "the high-level schema map for
  TabFlow" and the per-section tables list every column.
- **Evidence:** the migrations
  `20260425214408_AddCustomerAccessTicketDeviceCookie` (PR #11,
  TD-0017) and `20260425214627_AddOrderIdempotencyKey` (PR #12,
  TD-0018) ship two new columns and one unique index that were not
  reflected in `schema.md`'s "Customer Session And Cart" and
  "Orders And Bills" sections.
- **Conflict:** III.1 (documentation reflects reality). The schema
  reference was the right place to surface the new columns; their
  absence meant a reader skimming `schema.md` for the order shape
  would not see the idempotency contract that TD-0018 enforces.
- **Constitution anchor:** III.1, III.2.
- **Resolution:** PR #20 rewrote both section bullets to name the
  new columns, cite the migration filenames, link the relevant TDs,
  and (for the orders row) describe the unique-index semantics.

### B-3.4 — `health-checks.md` advanced probes declared, owned by TD-0013

- **Bucket:** `aligned with caveat`.
- **Claim:** `health-checks.md` lists the platform probe set as
  `platform-db:ping`, `platform-db:migrations`, `worker-heartbeat`
  and the tenant probe set as `tenant-db:ping`,
  `tenant-db:migrations`, `event-bus:capacity`, `tenant-context`.
- **Evidence:** PR #5 (or earlier) shipped `*-db:ping` for both
  hosts; the four advanced probes (`migrations`,
  `worker-heartbeat`, `event-bus:capacity`, `tenant-context`)
  remain open under TD-0013. The capability matrix already names
  TD-0013 as the owner of the gap, and this document is the
  canonical spec for what each probe checks once landed.
- **Constitution anchor:** III.1 (the spec is explicit; the gap is
  in the code, not in the doc).
- **Resolution:** none required for the doc text. TD-0013 owns the
  code work.

### B-3.5 — `system-overview.md`, `render-modes.md`, `firmware.md`, `error-codes.md` are aligned

- **Bucket:** `aligned`.
- **Evidence per document:**
  - **`system-overview.md`** — stack table (.NET 10, ASP.NET Core
    10 + Blazor Web App, EF Core 10 + Npgsql, PostgreSQL 17,
    Identity, Channels, ESP32-C3) matches the shipping
    `Directory.Build.props`, the host project references, and the
    firmware source. Source tree map matches `/src/apps/{platform,
    platform-worker, tenant}`, `/src/packages/{shared-dotnet,
    firmware}`, `/src/infra/postgres`. The "API Surface" block
    declares `/api/public/**` as the customer surface, which is
    the aspirational shape under TD-0021; this document inherits
    the same caveat as `tenant-api.md` but states it correctly
    ("Customer-facing contracts that require explicit HTTP
    semantics") and points at `tenant-api.md` for the full
    reference.
  - **`render-modes.md`** — surface-family-to-mode table matches
    `runtime-surfaces.md`'s per-route render mode column. The
    code-side `@rendermode InteractiveServer` annotation is owned
    by TD-0016; this document is the family-level spec, not the
    per-route enforcement point.
  - **`firmware.md`** — ESP32-C3 hardware profile, runtime
    contract (`auth_ok`, `new_token`, `refresh`, `ping`/`pong`,
    backend-produced QR matrix), generated-artifacts policy, and
    pin map all match the firmware source under
    `/src/packages/firmware/arduino/tabflow-table-display/`.
  - **`error-codes.md`** — the four-table vocabulary (Common,
    Session And Access Ticket, Order Submission, Device WebSocket)
    is consistent with `tenant-api.md`'s per-endpoint error-code
    list and with the WebSocket close-code conventions.
- **Resolution:** none required.

## 6. Phase C — Explanation Tree Findings

Phase C walks all 11 explanation documents under
`/doc/docs/explanation/concepts/` plus the two README index files. The
explanation tree is the home of "why" content; its findings are
about reasoning that has drifted from the shipping behaviour or the
constitution rather than missing reference data.

### C-1 — `implementation-patterns.md` carries 4 stale code patterns

- **Bucket:** `clean`.
- **Claim:** the explainer is "common implementation patterns" — a
  prescriptive guide that contributors copy from when adding new
  controllers, services, or tests.
- **Evidence:** four patterns drifted from the shipping behaviour:
  1. **`Order.Create` signature** — listed as
     `Order.Create(tableId, sessionId, ticketId, items, note)`. The
     real signature is
     `Order.Create(tableId, sessionId, ticketId, idempotencyKey, items, note)`
     after PR #12 / TD-0018. A contributor copying the doc shape
     calls a non-existent overload.
  2. **Common Pitfalls list** missed the two TD-driven properties:
     `CustomerAccessTicket.DeviceCookieValue` (TD-0017) and
     `Order.IdempotencyKey` (TD-0018).
  3. **Unit testing pattern** showed a `Mock<TenantDbContext>`
     example, recommending the very pattern that
     [`./test-taxonomy.md`](../docs/explanation/concepts/test-taxonomy.md#tier-1-unit)
     forbids ("we do not use a mocking framework"). See C-3 for the
     parallel divergence in csprojs.
  4. **Controller Structure** showed the AD-0003 anti-pattern
     (`PlatformDbContext` injected directly into the controller),
     which the next ADR call-out and TD-0022 would flag in review.
- **Conflict:** III.1 (documentation reflects reality), III.2 (doc
  lands first or alongside the change). PR #12 (TD-0018) and PR #11
  (TD-0017) shipped without refreshing this explainer.
- **Constitution anchor:** III.1, III.2.
- **Resolution:** PR #21 rewrote all four patterns. The
  `Order.Create` signature now lists `idempotencyKey` as the 4th
  positional argument. The Common Pitfalls list names
  `DeviceCookieValue` (TD-0017) and `IdempotencyKey` (TD-0018). The
  Unit testing example uses a hand-written `InMemoryTenantDbContext`
  fake and links the TD-0025 caveat. The Controller Structure
  example uses `ITenantRegistryService` per the AD-0003 trade-off
  and links TD-0022.

### C-2 — `data-protection.md` claims `[DataClass]` enforcement and lists four "TBD how-to" DSR procedures without TD links

- **Bucket:** `clean`.
- **Claim:** the explainer makes two contract-shaped claims:
  - "every personal-data column carries a comment classifying it.
    The comment is generated from `[DataClass]` attributes on the
    entity properties; CI fails the build if a `Sensitive` or
    `Restricted` column has no comment in the schema dump."
  - The Data Subject Rights table promises "operator runs the access
    export procedure (TBD how-to)" and similar for erasure,
    restriction, and portability.
- **Evidence:**
  - **`[DataClass]`.** The capability matrix at
    [`/doc/docs/reference/architecture/capability-matrix.md`](../docs/reference/architecture/capability-matrix.md)
    tracks "Personal-data classification on schema" as `Target` and
    points at TD-0007. The attribute, the schema-comment generator,
    and the build-time check do not exist; the doc states the
    behaviour as if it ships.
  - **DSR procedures.** The four "TBD how-to" rows (access, erasure,
    restriction, portability) had no TD link. A tenant operator who
    receives a real KVKK Article 11 / GDPR Article 15 request had
    no checklist and no ledger entry tracking the gap.
- **Conflict:** III.1 (documentation reflects reality), II.3 (every
  acknowledged compromise has a TD entry).
- **Constitution anchor:** III.1, II.3.
- **Resolution:** PR #21 (a) rewrote the `[DataClass]` paragraph
  with an "Implementation status (TD-0007)" callout that names the
  capability-matrix row and AC-122; (b) opened **TD-0024** and
  rewrote each DSR row to link the TD-0024 step that owns the
  procedure, plus added an interim instruction ("write a postmortem-
  style record under `/doc/buildlog/postmortems/` until the
  how-to lands") so the first real DSR informs the procedure.

### C-3 — `test-taxonomy.md` says "no mocking framework" while every test csproj references NSubstitute

- **Bucket:** `clean`.
- **Claim:** the test taxonomy states "Test doubles are written by
  hand. We do not use a mocking framework".
- **Evidence:** every test project at `/tests/<Name>.Tests/*.csproj`
  carries `<PackageReference Include="NSubstitute" />`:
  `E2E.Tests`, `Tenant.Tests`, `PlatformWorker.Tests`,
  `Platform.Tests`, `Shared.Tests`. The implementation-patterns
  explainer (see C-1) compounds the divergence by showing a
  `Mock<TenantDbContext>` example.
- **Conflict:** III.1 (documentation reflects reality). Either path
  (officially adopt NSubstitute or remove it) is fine on its own;
  carrying both is a coin flip for any reviewer choosing what shape
  to require.
- **Constitution anchor:** III.1.
- **Resolution:** PR #21 reframed the test-taxonomy paragraph as a
  historical preference and added a TD-0025 callout. Opened
  **TD-0025** with a two-exit payoff plan: adopt NSubstitute
  officially (rewrite the doc, list its allowed scope) or remove it
  from the csprojs. Until either resolves, hand-written fakes remain
  the default for new tests.

### C-4 — `threat-model.md` carries 3 stale or unsupported claims

- **Bucket:** `clean`.
- **Claim:** three mitigations in Boundary C / Boundary D promise
  enforcement that does not yet ship:
  - "missing policy is a build error per AD-0014" (Boundary C, T row)
  - "analyzer flags `IQueryable.ToList()` without `Take()`"
    (Boundary D, D row)
  - "Backups encrypted at rest; access via deploy-time secret manager
    only (deferred — TD when first backup ships)" (Boundary D, I row)
- **Evidence:**
  - **Missing-policy build error.** AD-0014 covers `.editorconfig` +
    `Directory.Build.props`; it does not generate a build error for
    a missing authorisation policy on a Razor route. ASP.NET Core's
    `FallbackPolicy` rejects the request at startup, but no analyzer
    fails the build.
  - **`IQueryable.ToList()` analyzer.** `TabFlow.Analyzers` ships
    only `TF0001` (English-first identifier rule, TD-0009 step 3).
    The unbounded-query rule is a future addition; it is not on the
    triage queue today.
  - **Backup encryption "TD when first backup ships".** No TD
    number was named; the deferral was a paragraph-level promise
    not tracked in the ledger.
- **Conflict:** III.1 (documentation reflects reality), II.3 (every
  acknowledged compromise has a TD entry).
- **Constitution anchor:** III.1, II.3.
- **Resolution:** PR #21 rewrote each of the three mitigations:
  - The missing-policy mitigation now cites AD-0005 and the
    pending Identity-policy registration test under TD-0010 step 5
    (analyzer-time enforcement is named as future TD-0009 follow-
    up).
  - The `IQueryable.ToList()` mitigation says the analyzer is not
    yet shipped, cites TD-0009 as the analyzer baseline, and
    explicitly says the rule is enforced in code review today.
  - The backup-encryption mitigation now links the capability-matrix
    "Encrypted backup with off-site copy" row (`Target`) and the
    backup-and-restore how-to.

### C-5 — `customer-session-model.md` Submit Flow did not list the TD-0017 / TD-0018 checks

- **Bucket:** `aligned with caveat` → resolved.
- **Claim:** the Submit Flow numbered list described the order
  submission as "validate QR proof → verify ticket → convert cart →
  consume proof", four steps.
- **Evidence:** PR #11 (TD-0017) added the device-binding cookie
  verification step in `OrderService.SubmitAsync`; PR #12 (TD-0018)
  added the idempotency-key check against the unique index. Neither
  showed up in the explainer.
- **Conflict:** III.1, III.2.
- **Constitution anchor:** III.1, III.2.
- **Resolution:** PR #21 rewrote the Submit Flow as a 9-step list
  that names the device-binding cookie check (step 5, TD-0017) and
  the idempotency-key check (step 7, TD-0018) explicitly.

### C-6 — `multi-tenancy.md`, `tenant-lifecycle.md`, `accessibility.md`, `internationalization.md`, `authorization.md`, `operational-surfaces.md`, and the two READMEs are aligned

- **Bucket:** `aligned`.
- **Evidence per document:**
  - **`multi-tenancy.md`** — AD-0001 / AD-0003 separation is the
    decision the doc explains; both hosts and DB contexts ship that
    way. References to the schema reference and runtime overview
    resolve.
  - **`tenant-lifecycle.md`** — tenant-code shape, primary-domain
    rules, runtime seed baseline match the platform host's
    provisioning code; the operational playbook delegate to
    `provision-tenant.md`.
  - **`accessibility.md`** — WCAG 2.2 AA baseline cross-references
    AC-110..AC-116 in `acceptance-criteria.md`; both sides match.
  - **`internationalization.md`** — AD-0015 (English-first), the
    `LanguageCode` field on `TenantRegistration`, and the rejection
    error code `tenant.create.unsupported_language` (AC-121) are the
    contract. The `IStringLocalizer` + `*.resx` half is tracked
    under TD-0011; the doc states the contract correctly.
  - **`authorization.md`** — `Console:ManageUsersBelowOwner` (AC-014),
    `Platform:Read|Write|Self` policies, and the station-device
    deferral all match the runtime-surfaces map and AD-0005.
  - **`operational-surfaces.md`** — purely conceptual; product
    reasoning behind the surface family. Delegate every concrete
    fact to `runtime-surfaces.md`.
  - **`/doc/docs/explanation/README.md`** and
    **`/doc/docs/explanation/concepts/README.md`** — index files;
    every listed document exists on disk.
- **Resolution:** none required.

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
| B-1.1 | `clean` | PR #18 — rewrote 8 stale capability-matrix rows to name the shipping PR / TD; added the missing Test taxonomy row for TD-0010. | 2026-04-26 |
| B-1.2 | `correct` | PR #18 — rewrote runtime-surfaces HTTP table to reflect the shipping route map; opened TD-0021 with a four-step `/api/public/*` migration plan. | 2026-04-26 |
| B-1.3 | `aligned` | No AC text change required; matrix evidence handled in B-1.1. | 2026-04-26 |
| B-1.4 | `aligned` | No action required. | 2026-04-26 |
| B-1.5 | `aligned` | Closure inherited from A-2 (subtree stubs created in PR #17). | 2026-04-26 |
| B-2.1 | `aligned` | All 15 ADRs carry `Accepted`; status taxonomy intact. No action required. | 2026-04-26 |
| B-2.2 | `aligned` | 10 ADRs (AD-0001/2/5/6/7/9/11/12/13/14) verified against shipping code; `release.yml` confirmed present (correcting prior pass mis-attribution). No action required. | 2026-04-26 |
| B-2.3 | `aligned with caveat` | AD-0004 baseline holds; code-side `@rendermode` annotation owned by TD-0016. | 2026-04-26 |
| B-2.4 | `aligned with caveat` | AD-0008 schema authority intact; worker `MigrateAsync()` + drop+apply+verify owned by TD-0003. | 2026-04-26 |
| B-2.5 | `aligned with caveat` | AD-0010 bootstrap CLI shipped (PR #9 + #16); operator-action half owned by TD-0002. | 2026-04-26 |
| B-2.6 | `aligned with caveat` | AD-0015 enforced via `TF0001` (PR #14); IStringLocalizer half owned by TD-0011, analyzer release files by TD-0009. | 2026-04-26 |
| B-2.7 | `implement` | PR #19 — opened TD-0022 with four-step migration: 3 read services + fold order reads into `IOrderService`, controller rewrite, unit tests, Roslyn `DbContext`-on-`ControllerBase` analyzer. PR #20 extended TD-0022 to the platform-side controllers (`TenantsController`, `JobsController`); count: 6 controllers, 5 services. | 2026-04-26 |
| B-3.1 | `clean` | PR #20 — added a TD-0023 banner to `internal-api.md`; opened TD-0023 with a five-step rewrite plan. | 2026-04-26 |
| B-3.2 | `correct` | PR #20 — rewrote `tenant-api.md` order-submission section: idempotency key on the request body (not the `Idempotency-Key` header) per TD-0018; added "Migration status (TD-0021)" callouts to `/api/public/profile`, `/api/public/catalog`, `/api/public/session`. | 2026-04-26 |
| B-3.3 | `clean` | PR #20 — rewrote `schema.md` "Customer Session And Cart" and "Orders And Bills" bullets to name `device_cookie_value` (TD-0017) and `idempotency_key` + the unique index over `(session_id, idempotency_key)` (TD-0018), citing the migration filenames. | 2026-04-26 |
| B-3.4 | `aligned with caveat` | No doc-text change needed; advanced probes owned by TD-0013. | 2026-04-26 |
| B-3.5 | `aligned` | No action required. | 2026-04-26 |
| C-1 | `clean` | PR #21 — refreshed `implementation-patterns.md` with the shipping `Order.Create` signature, the TD-0017 / TD-0018 properties in Common Pitfalls, the hand-written-fake testing example (TD-0025 callout), and the service-layer controller example (TD-0022 callout). | 2026-04-26 |
| C-2 | `clean` | PR #21 — added a TD-0007 callout to the `[DataClass]` paragraph in `data-protection.md`; opened TD-0024 and linked each DSR row in the Data Subject Rights table to the TD-0024 step that owns the procedure. | 2026-04-26 |
| C-3 | `clean` | PR #21 — reframed the `test-taxonomy.md` "no mocking framework" rule as a historical preference and opened TD-0025 with a two-exit payoff plan (adopt NSubstitute officially or remove it from the five csprojs). | 2026-04-26 |
| C-4 | `clean` | PR #21 — rewrote the three stale mitigations in `threat-model.md`: missing-policy build error (AD-0005 + TD-0010 step 5), `IQueryable.ToList()` analyzer (TD-0009 future addition), backup encryption (capability-matrix `Target` + how-to link). | 2026-04-26 |
| C-5 | `clean` | PR #21 — rewrote `customer-session-model.md` Submit Flow as a 9-step list naming the TD-0017 device-binding cookie check (step 5) and the TD-0018 idempotency-key check (step 7). | 2026-04-26 |
| C-6 | `aligned` | No action required (multi-tenancy, tenant-lifecycle, accessibility, internationalization, authorization, operational-surfaces, READMEs). | 2026-04-26 |

## 11. Sign-Off

Open. The pass remains in progress until every phase Section above is
non-empty and every finding has a closure-log row.

- Auditor: Cascade pair-programming session, opened 2026-04-26.
- Method: top-down horizontal sweep, four-question protocol,
  four-bucket classification.
- Reference passes: [`./code-audit-2026-04-25.md`](./code-audit-2026-04-25.md)
  (original audit + Section 11 re-review).
