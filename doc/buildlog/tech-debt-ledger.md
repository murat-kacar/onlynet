# Technical Debt Ledger

This is the single, append-only ledger of every piece of technical debt
TabFlow has accepted. The constitution
([`/doc/docs/constitution.md`](/doc/docs/constitution.md), II.3 and VI.3) requires
that any temporary or compromised work appears here with an owner and a
payoff plan.

The ledger is **append-only**: rows are never deleted. When debt is paid,
its status changes from `[OPEN]` to `[CLOSED]` and the closure date is
recorded. The history of accepted compromises is itself a project asset.

## Format

Each entry is a section. The headline is `[STATUS] TD-NNNN — <one line>`.

```markdown
## [OPEN] TD-0001 — Hand-applied platform schema instead of EF Core migration

- Opened: 2026-04-25
- Owner: <github-handle>
- Origin: docs review session of 2026-04-25; AD-0008 was not yet honoured.
- Symptom: `tabflow_platform` schema was created via `psql` rather than
  `dotnet ef database update`. Migration history is empty.
- Risk if unpaid: schema drift; release-gate cannot verify migration
  history matches model.
- Payoff plan: implement `IDesignTimeDbContextFactory<T>` per AD-0009,
  drop and recreate `tabflow_platform` from migrations on the next
  bootstrap window.
- Linked: AD-0008, AD-0009, [`/doc/docs/how-to/setup-migrations.md`](/doc/docs/how-to/setup-migrations.md)
```

When closed, append a closure block:

```markdown
- Closed: 2026-05-10
- Resolution: TD-0001 paid by PR #42; design-time factories landed and
  platform DB rebuilt from migration history.
```

## Status Vocabulary

| Status | Meaning |
| --- | --- |
| `[TRIAGE]` | Newly recorded debt without a named owner yet. Triaged at the next release-gate review and either claimed (becomes `[OPEN]`), accepted (`[ACCEPTED]`), or abandoned (`[ABANDONED]`). |
| `[OPEN]` | Active debt with a named owner and a payoff plan. |
| `[CLOSED]` | Debt paid; resolution recorded. Row stays. |
| `[ACCEPTED]` | Debt acknowledged as permanent (very rare); MUST cite an ADR. |
| `[ABANDONED]` | Plan to pay was rejected; cite a `buildlog/abandoned/` entry. |

A `[TRIAGE]` entry that is not claimed or escalated by the next
release-gate review is a release-gate failure
([release gate, Tech Debt Ledger Triage section](../docs/meta/release-gate.md#tech-debt-ledger-triage)).

## Identifier

Each entry has a stable `TD-NNNN` identifier. New entries take the next
free number; numbers are not reused.

## Cross-Reference Rule

Code or documents that contain a known compromise MUST link the relevant
ledger entry. The link form is the absolute path with the anchor:
`/doc/buildlog/tech-debt-ledger.md#td-0001`.

A grep for `TD-` across the repo MUST return only links into this
ledger; orphan `TD-` references are a documentation bug.

---

## Triage Queue

<!-- Newly recorded debt awaiting an owner. Resolved at the next
     release-gate review per the Tech Debt Ledger Triage section. -->

### [TRIAGE] TD-0028 — Customer-facing Razor pages still Interactive Server; AD-0004 mandates Static SSR

- Opened: 2026-04-26
- Owner: TBD
- Origin: PR #25 Blazor Web App migration. Closing TD-0016
  required a render-mode opt-in on every interactive page; the
  customer-facing pages (`Cart.razor`, `Menu.razor`, `Order.razor`,
  `ScanQr.razor`) currently rely on `@onclick`, `@bind`, and
  `IJSRuntime` calls that only work under `InteractiveServer`. They
  shipped that way to keep customer behaviour unchanged through
  the migration, but AD-0004 explicitly assigns customer-facing
  surfaces to Static SSR.
- Symptom: four customer-facing components carry
  `@rendermode InteractiveServer` even though AD-0004 requires
  Static SSR. Each interactive customer session opens a SignalR
  circuit that AD-0004 expects only staff-facing surfaces to use,
  inflating the per-customer memory and CPU footprint and breaking
  the capacity model in `capability-matrix.md`.
- Risk if unpaid: customer load multiplies the SignalR circuit
  count by roughly the number of concurrent diners (vs the staff
  population); the latency target in AD-0006 was sized against
  staff population only and will be missed under busy-day customer
  load.
- Payoff plan:
  1. Convert customer flows to a Static-SSR-friendly shape:
     replace `@onclick` cart manipulation with `<form method="post">`
     handlers, replace IJSRuntime `confirm()`/`alert()` with
     server-rendered confirmation pages, and surface server push
     (table updates) via a small partial-page refresh primitive
     instead of full SignalR.
  2. Remove `@rendermode InteractiveServer` from `Cart.razor`,
     `Menu.razor`, `Order.razor`, and `ScanQr.razor` once the
     conversion lands. Capability matrix row "Tenant customer
     surfaces (Static SSR)" advances from `In progress` to
     `Implemented`.
  3. Add a release-gate smoke check that fetches each customer
     route and asserts the rendered HTML does not include
     `_framework/blazor.web.js` references inside an interactive
     marker (i.e. the page is fully static).
- Linked: AD-0004, AD-0006,
  [`/doc/docs/reference/architecture/render-modes.md`](/doc/docs/reference/architecture/render-modes.md),
  [`/doc/docs/reference/architecture/capability-matrix.md`](/doc/docs/reference/architecture/capability-matrix.md)

### [CLOSED] TD-0027 — Hosts use standalone Blazor Server; AD-0004 contract assumes Blazor Web App

- Opened: 2026-04-26
- Closed: 2026-04-26
- Owner: closed in PR #25 by single-author pre-1.0 (TD-0020).
- Origin: code-audit-2026-04-26 follow-up while implementing
  TD-0016 step 1. The first audit pass observed that no `.razor`
  file carried a `@rendermode` directive; the deeper cause was
  that the three hosts ran in **standalone Blazor Server** mode
  (`AddServerSideBlazor()` + `MapBlazorHub()` + `_Host.cshtml`
  fallback), not the **Blazor Web App** model that AD-0004's
  mixed-mode matrix presumes. `@rendermode` is a no-op outside
  Blazor Web App, so TD-0016 step 1 could not land without first
  migrating the hosts.
- Symptom: every component ran on a single, repository-wide,
  always-Interactive SignalR circuit; AD-0004's matrix
  ("customer Static SSR, staff Interactive Server") could not be
  honoured by any annotation alone.
- Risk if unpaid: AD-0004 stays a paper contract; release-gate
  smoke checks that key off the rendered HTML's interactivity
  marker (TD-0028 step 3, future TD-0010 step 6) cannot
  distinguish per-route render modes.
- Resolution (PR #25):
  - `src/apps/platform/Program.cs` and
    `src/apps/tenant/Program.cs` swapped
    `builder.Services.AddServerSideBlazor()` for
    `builder.Services.AddRazorComponents().AddInteractiveServerComponents()`
    and replaced `app.MapBlazorHub()` +
    `app.MapFallbackToPage("/_Host")` with
    `app.MapRazorComponents<App>().AddInteractiveServerRenderMode()`.
  - The legacy `App.razor` (Router) is now `Components/Routes.razor`;
    a new `Components/App.razor` carries the HTML document root
    that `MapRazorComponents<App>()` requires.
  - `src/apps/{platform,tenant}/Pages/_Host.cshtml` removed.
  - `_Imports.razor` extended with
    `@using static Microsoft.AspNetCore.Components.Web.RenderMode`
    so component-level `@rendermode InteractiveServer` resolves
    without a fully-qualified type name.
  - Smoke verification: `/health/live` returns
    `{"status":"pass",...}` from both hosts under the new
    composition.
- Follow-up: customer-page Static SSR alignment tracked under
  TD-0028 (above).
- Linked: AD-0004, AD-0006, TD-0016, TD-0028,
  [`/doc/docs/reference/architecture/render-modes.md`](/doc/docs/reference/architecture/render-modes.md)

### [TRIAGE] TD-0026 — `Type=notify` supervision contract requires `UseSystemd()`; neither host calls it

- Opened: 2026-04-26
- Owner: TBD
- Origin: code-audit-2026-04-26 alignment pass
  ([`./code-audit-2026-04-26.md`](./code-audit-2026-04-26.md), Phase
  D finding D-2).
- Symptom: the supervision how-to at
  [`/doc/docs/how-to/supervise-processes.md`](/doc/docs/how-to/supervise-processes.md#typenotify-requirement)
  declares the host invariant as `Type=notify` and states that the
  hosts **MUST** call `UseSystemd()` on the host builder so systemd
  considers them started only after ASP.NET Core signals readiness.
  The shipping `Program.cs` for both hosts (and the platform worker)
  has no `UseSystemd()` call:
    `src/apps/platform/Program.cs`
    `src/apps/platform-worker/Program.cs`
    `src/apps/tenant/Program.cs`
  A `Type=notify` unit deployed against a binary that never signals
  ready hangs at `systemctl start` until the unit's
  `TimeoutStartSec` elapses, then the supervisor marks the unit
  `failed`. This is benign in development (the doc says
  `UseSystemd()` is a no-op when not run under systemd) but breaks
  the production deploy contract.
- Risk if unpaid: the first production rollout that follows the
  reference unit set times out at start. Operators downgrade to
  `Type=simple`, lose the readiness handshake, and find out about a
  bad start only when `/health/ready` fails — which is what the
  `Type=notify` choice was meant to prevent.
- Payoff plan:
  1. (Done in PR #23) Pinned
     `Microsoft.Extensions.Hosting.Systemd` 10.0.7 in
     `Directory.Packages.props` and added the package reference to
     all three host csprojs (`TabFlow.Platform`, `TabFlow.Tenant`,
     `TabFlow.PlatformWorker`).
  2. (Done in PR #23) Wired the systemd lifetime into every
     `Program.cs`:
       - `src/apps/platform/Program.cs`: `builder.Host.UseSystemd()`
         after `UseSerilog()`.
       - `src/apps/tenant/Program.cs`: `builder.Host.UseSystemd()`
         after `UseSerilog()`.
       - `src/apps/platform-worker/Program.cs`:
         `builder.Services.AddSystemd()` (the
         `HostApplicationBuilder` equivalent).
     Both extensions are no-ops when `INVOCATION_ID` is unset, so
     `dotnet run`, the existing unit tests, and the future
     integration tier are unaffected.
  3. (Open) Add a composition-root regression test that resolves
     `IHostLifetime` from each Program's `IServiceProvider` and
     asserts the runtime type is `SystemdLifetime`. The test
     depends on the Integration-tier transactional fixture from
     TD-0010 step 5 (so the host can be built without a live
     PostgreSQL connection). Until then, the contract is enforced
     by code review.
  4. (Done in PR #23) Updated
     [`/doc/docs/how-to/supervise-processes.md`](/doc/docs/how-to/supervise-processes.md#typenotify-requirement)
     to record the closure: the "Implementation status (TD-0026)"
     callout now lists the three hosts that call the systemd
     lifetime hook and points at TD-0010 step 5 for the regression
     test that closes step 3.
- Linked: AD-0003,
  [`/doc/docs/how-to/supervise-processes.md`](/doc/docs/how-to/supervise-processes.md),
  [`./code-audit-2026-04-26.md`](./code-audit-2026-04-26.md#7-phase-d--how-to-tree-findings)

### [TRIAGE] TD-0025 — `test-taxonomy.md` says "no mocking framework"; every test project references NSubstitute

- Opened: 2026-04-26
- Owner: TBD
- Origin: code-audit-2026-04-26 alignment pass
  ([`./code-audit-2026-04-26.md`](./code-audit-2026-04-26.md), Phase
  C finding C-3).
- Symptom: the test taxonomy at
  [`/doc/docs/explanation/concepts/test-taxonomy.md`](/doc/docs/explanation/concepts/test-taxonomy.md)
  states "Test doubles are written by hand. We do not use a mocking
  framework; hand-written fakes are easier to read in failures and
  survive refactoring better." But every test project references
  the **NSubstitute** mocking framework:
    `tests/E2E.Tests/E2E.Tests.csproj`
    `tests/Tenant.Tests/Tenant.Tests.csproj`
    `tests/PlatformWorker.Tests/PlatformWorker.Tests.csproj`
    `tests/Platform.Tests/Platform.Tests.csproj`
    `tests/Shared.Tests/Shared.Tests.csproj`
  The implementation-patterns explainer at
  [`/doc/docs/explanation/concepts/implementation-patterns.md`](/doc/docs/explanation/concepts/implementation-patterns.md#unit-testing-services)
  also shows a `Mock<TenantDbContext>` example, leaning into the
  framework.
- Risk if unpaid: the rule and the practice diverge. A reviewer who
  takes the doc at face value rejects an NSubstitute-based PR; a
  reviewer who reads the csproj allows one. Either resolution is
  fine on its own, but having both in the repository is a coin flip.
- Payoff plan (operator chooses one of two exits):
  1. **Adopt NSubstitute officially.** Rewrite the test-taxonomy
     "Test Doubles" paragraph to acknowledge NSubstitute as the
     mocking framework, list its allowed scope (unit-tier service
     boundaries, never integration-tier), and keep the
     implementation-patterns mock example. Add a Roslyn analyzer
     hint that flags hand-rolled fakes that could be NSubstitute
     stubs (lower-priority).
  2. **Remove NSubstitute.** Drop the `<PackageReference>` from all
     five test projects and rewrite any existing call site to use a
     hand-written fake. Update the implementation-patterns example
     to show a hand-written fake instead.
- Linked:
  [`/doc/docs/explanation/concepts/test-taxonomy.md`](/doc/docs/explanation/concepts/test-taxonomy.md),
  [`/doc/docs/explanation/concepts/implementation-patterns.md`](/doc/docs/explanation/concepts/implementation-patterns.md),
  TD-0010 (test taxonomy bootstrap),
  [`./code-audit-2026-04-26.md`](./code-audit-2026-04-26.md#6-phase-c--explanation-tree-findings)

### [CLOSED] TD-0024 — Data-subject-rights operator procedures (KVKK / GDPR) not yet documented

- Opened: 2026-04-26
- Closed: 2026-04-26
- Owner: closed in PR #28 by single-author pre-1.0 (TD-0020).
- Origin: code-audit-2026-04-26 alignment pass
  ([`./code-audit-2026-04-26.md`](./code-audit-2026-04-26.md), Phase
  C finding C-2). Constitutional anchor: III.1 (documentation
  reflects reality), AC-126 (breach notification within 24 hours),
  AC-129 (recovery drill within 90 days — separate but parallel
  pattern).
- Symptom: the data-protection explainer at
  [`/doc/docs/explanation/concepts/data-protection.md`](/doc/docs/explanation/concepts/data-protection.md#data-subject-rights)
  declares concrete operator procedures for the four data-subject
  rights it commits to (Right of access, Right to erasure, Right to
  restriction, Right to data portability), but the "TabFlow
  Procedure" cells reference how-to guides that do not yet exist
  (`(TBD how-to)`). A tenant who receives a KVKK Article 11 / GDPR
  Article 15 request today has no procedure to follow.
- Risk if unpaid: a real DSR arrives in production and the operator
  has no checklist. The 30-day regulatory clock starts the moment
  the request is received; an undocumented procedure means the
  first request becomes a fire drill.
- Resolution (PR #28): the four how-to guides shipped, each with
  identity-verification, dry-run, hard transaction, audit-row, and
  delivery sections; the explainer's Data Subject Rights table no
  longer carries `(TD-0024 step N)` parentheticals.
  1. (Done in PR #28)
     [`/doc/docs/how-to/data-subject-access.md`](/doc/docs/how-to/data-subject-access.md)
     — operator procedure for the Right of access. Output: a JSON
     export with one top-level key per source table plus an
     `omitted` array naming redacted (table, column) tuples and the
     legal basis. Audit row written with
     `action = 'dsr.access.exported'`.
  2. (Done in PR #28)
     [`/doc/docs/how-to/data-subject-erasure.md`](/doc/docs/how-to/data-subject-erasure.md)
     — operator procedure for the Right to erasure. Hard-delete
     for in-scope rows; anonymisation in place for orders kept
     under the 6-year accounting retention. Audit row written with
     `action = 'dsr.erasure.completed'`.
  3. (Done in PR #28)
     [`/doc/docs/how-to/data-subject-restriction.md`](/doc/docs/how-to/data-subject-restriction.md)
     — operator procedure for the Right to restriction. Records
     the restriction in the audit log, suspends operational paths
     (Identity lockout for staff; session close for customer
     sessions), schedules a review. The dedicated `restricted`
     column on every personal-data table waits on TD-0007.
  4. (Done in PR #28)
     [`/doc/docs/how-to/data-subject-portability.md`](/doc/docs/how-to/data-subject-portability.md)
     — operator procedure for the Right to data portability.
     Same shape as the access export filtered to consent / contract
     scope; default JSON, optional CSV-in-ZIP. Audit row written
     with `action = 'dsr.portability.delivered'`.
  5. (Done in PR #28)
     [`data-protection.md`](/doc/docs/explanation/concepts/data-protection.md#data-subject-rights)
     updated: every "TabFlow Procedure" cell that previously named
     `(TD-0024 step N)` now links the matching how-to guide; the
     prose paragraph below the table records that the procedures
     shipped in PR #28.
- Linked: AC-126, AC-129,
  [`/doc/docs/explanation/concepts/data-protection.md`](/doc/docs/explanation/concepts/data-protection.md),
  [`./code-audit-2026-04-26.md`](./code-audit-2026-04-26.md#6-phase-c--explanation-tree-findings)

### [CLOSED] TD-0023 — `internal-api.md` mixes public and staff-tier surfaces; lists routes that no longer ship

- Opened: 2026-04-26
- Closed: 2026-04-26
- Owner: closed in PR #27 by single-author pre-1.0 (TD-0020).
- Origin: code-audit-2026-04-26 alignment pass
  ([`./code-audit-2026-04-26.md`](./code-audit-2026-04-26.md), Phase
  B-3 finding B-3.1).
- Symptom: the document at
  [`/doc/docs/reference/api/internal-api.md`](/doc/docs/reference/api/internal-api.md)
  was written before the customer / staff tier split landed in
  TD-0015 and before the public order surface moved to
  `/api/public/orders` in PR #6. The current document carries
  three structural defects:
  1. **Public endpoints described as internal.** Sections "Sessions
     API", "Cart API", and the customer half of "Orders API"
     describe customer-tier endpoints that belong in
     [`/doc/docs/reference/api/tenant-api.md`](/doc/docs/reference/api/tenant-api.md).
  2. **Stale route for order submission.** The document lists
     `POST /api/orders/submit` as the customer order path; the real
     shipping route is `POST /api/public/orders` per
     `PublicOrdersController` (PR #6, TD-0015 step 3).
  3. **Missing staff endpoints.** The actual staff-tier surface
     under `/api/orders/{id}`, `/api/orders/session/{sessionId}`,
     `/api/kitchen/orders`,
     `/api/kitchen/items/{id}/status`, `/api/sessions/{sessionId}/close`,
     `/api/tables`, `/api/tables/{id}` is not documented at all.
  4. **Cross-tier authorisation note absent.** Each entry says
     "Policy: None" without explaining whether that is a public-tier
     `[AllowAnonymous]` (TD-0015 step 2) or a missing
     authorisation contract.
- Risk if unpaid: the document is the first hit a reviewer reads
  when they want to know "what HTTP do we expose internally"; a
  drift this large means the answer is wrong. A future PR adding a
  staff endpoint copies the dominant `Policy: None` pattern and
  re-creates the AC-010 / AC-043 violation that TD-0015 closed.
- Resolution (PR #27): replaced the entire document at
  [`/doc/docs/reference/api/internal-api.md`](/doc/docs/reference/api/internal-api.md)
  with a staff-tier-only reference. The new document:
  1. Drops every customer-tier endpoint. Customer-tier surfaces
     (`/menu`, `/cart`, `/sessions/open`, `/sessions/{ticketId}`,
     `/api/public/orders`) are now documented exclusively in
     [`tenant-api.md`](/doc/docs/reference/api/tenant-api.md) and
     `internal-api.md` defers to it for the public surface.
  2. Removes the stale `POST /api/orders/submit` entry. The public
     order submit lives at `POST /api/public/orders` per
     `PublicOrdersController` (PR #6, TD-0015 step 3) and is
     covered in `tenant-api.md`.
  3. Adds the staff-tier sections that the previous draft omitted:
     `Tenants`, `Jobs` on the platform host;
     `Orders` (read-only — `GET {id}`, `GET session/{sessionId}`),
     `Kitchen` (`GET orders`, `PUT items/{id}/status` with
     `Tenant:Read` / `Tenant:Write` per-action policies),
     `Tables` (`GET`, `GET {id}`), and the `Sessions` close action
     (`POST {sessionId}/close` with `Tenant:Write`).
  4. Names every action's `[Authorize]` policy explicitly and
     calls out the default-restrictive ordering on
     `SessionsController` per AC-043.
  5. Adds a Conventions section (auth, status codes, content
     types, internal vs public) and a Migration Notes section
     that points at TD-0015 step 6 (integration test for the
     401/403 split), TD-0021 (`/api/public/*` shim
     migration), and TD-0022 (controller-to-service refactor).
- Linked: AD-0003,
  [`/doc/docs/reference/api/internal-api.md`](/doc/docs/reference/api/internal-api.md),
  [`/doc/docs/reference/api/tenant-api.md`](/doc/docs/reference/api/tenant-api.md),
  TD-0015 step 2 (attribute audit), TD-0021 (prefix migration),
  TD-0022 (controller-to-service migration overlaps),
  [`./code-audit-2026-04-26.md`](./code-audit-2026-04-26.md#5-phase-b--reference-tree-findings)

### [CLOSED] TD-0022 — Read-only API controllers bypass the application service layer

- Opened: 2026-04-26
- Closed: 2026-04-26
- Owner: closed in PR #29 by single-author pre-1.0 (TD-0020).
- Origin: code-audit-2026-04-26 alignment pass
  ([`./code-audit-2026-04-26.md`](./code-audit-2026-04-26.md), Phase
  B-2 finding B-2.7 and Phase B-3 cross-check). AD-0003 trade-off
  ([`/doc/docs/reference/architecture/decisions.md`](/doc/docs/reference/architecture/decisions.md#ad-0003-one-host-process-per-side))
  states: "host process shape now carries both UI and API concerns;
  the internal layer boundary (host → application service → domain)
  must remain explicit in code." Today the boundary is observed
  inconsistently across the controllers under
  `/src/apps/tenant/Controllers/Api/` and
  `/src/apps/platform/Controllers/Api/`:
  - **Through service layer** (3, all tenant-side): `CartController`
    → `ICartService`, `PublicOrdersController` → `IOrderService`,
    `SessionsController` → `ICustomerSessionService`. These
    controllers are post-TD-0015 and post-TD-0017 work.
  - **Direct `TenantDbContext`** (4 tenant-side):
    `KitchenController`, `MenuController`, `OrdersController`,
    `TablesController`. These inject `TenantDbContext` and run LINQ
    queries inline.
  - **Direct `PlatformDbContext`** (2 platform-side):
    `TenantsController` (`GET / GET {id} / POST / PUT {id}` over
    `_context.Tenants`) and `JobsController` (`GET / GET {id}` over
    `_context.ProvisioningJobs`). The application service layer for
    these read paths does not exist; the write paths
    (`POST /api/tenants`, `PUT /api/tenants/{id}`) call the entity
    factories `TenantRegistration.Create(...)` directly from the
    controller, which is acceptable shape for a thin write but
    leaves no seam for transactional discipline (audit log writes
    per AC-071, provisioning-job emission, validation policy).
- Symptom: a controller that runs LINQ over `TenantDbContext`
  inside an action method couples the HTTP transport surface to the
  EF Core query shape. Three knock-on effects:
  1. The "host → application service → domain" boundary AD-0003
     calls out is invisible to a reader of the controller.
  2. Tests for these read paths cannot exercise the read query in
     isolation; an integration test against a controller pulls in
     ASP.NET Core routing, model binding, and `WebApplicationFactory`
     even when the only thing under test is a SQL projection.
  3. When a future read path needs caching, observability spans, or
     authorization-shaped filtering ("only the open bills on tables
     the cashier owns"), there is no service-layer seam to add it
     to.
- Risk if unpaid: as the read surface grows, every new feature
  copies the dominant pattern (raw `_context.X.Where(...).ToList()`
  inside a controller action). Re-aligning later requires touching
  every read path simultaneously, which is a much bigger change
  than fixing four controllers today.
- Resolution (PR #29):
  1. (Done in PR #29) Five new application-service interfaces
     introduced, plus an extension of the existing `IOrderService`:
     - **Tenant-side, new** under
       `/src/packages/shared-dotnet/Application/Services/`:
       `IKitchenReadService` (orders-in-progress + item-status
       mutation), `IMenuReadService`
       (menu-items + filtered-by-category), `ITableReadService`
       (tables + per-table detail with open-session count).
     - **Tenant-side, extended:** `IOrderService` gains
       `GetOrderDetailAsync` and `GetOrdersBySessionAsync`. The
       customer-tier `SubmitAsync` is unchanged.
     - **Platform-side, new** under the same path:
       `ITenantRegistryService` (tenant CRUD), and
       `IProvisioningJobReadService` (job list + detail).
     The audit-row half of the registry write actions (AC-071)
     waits on TD-0019; the read paths surface a TODO that names
     it.
  2. (Done in PR #29) Six controllers rewritten to depend on the
     services rather than on `TenantDbContext` / `PlatformDbContext`:
     `KitchenController`, `MenuController`, `TablesController`,
     `OrdersController` (tenant-side), `TenantsController`,
     `JobsController` (platform-side). Controller actions are now
     thin: parameter binding, `await`, return. Each action keeps
     the same authorise / route / status-code contract that
     `internal-api.md` documents (PR #27).
  3. (Open) Service-level tests for the new read paths land with
     the Integration-tier transactional fixture in TD-0010 step 5.
     Until then, the contract is enforced by the smoke check (each
     controller route returns the same HTTP shape it did before
     the refactor) and by the new Roslyn rule below.
  4. (Done in PR #29) Authored a third Roslyn analyser
     `TabFlow.Analyzers.ControllerDbContextAnalyzer` (rule
     `TF0003`, category `Design`, default severity `Warning`).
     The analyser fires on any class derived from
     `Microsoft.AspNetCore.Mvc.ControllerBase` that holds a
     `Microsoft.EntityFrameworkCore.DbContext` as a field, property,
     or constructor parameter; compiler-generated auto-property
     backing fields are skipped so a single property declaration
     produces one diagnostic. Backed by 4 Unit-tier regression
     tests at
     `tests/Analyzers.Tests/ControllerDbContextAnalyzerTests.cs`.
     Released in
     `tools/analyzers/TabFlow.Analyzers/AnalyzerReleases.Unshipped.md`.
- Linked: AD-0003,
  [`/doc/docs/reference/architecture/decisions.md`](/doc/docs/reference/architecture/decisions.md#ad-0003-one-host-process-per-side),
  TD-0010 step 5 (integration fixture for service-level tests),
  TD-0021 (the prefix migration calls the same services),
  [`./code-audit-2026-04-26.md`](./code-audit-2026-04-26.md#5-phase-b--reference-tree-findings)

### [OPEN] TD-0021 — Customer-tier HTTP endpoints not on the `/api/public/*` prefix

- Opened: 2026-04-26
- Owner: closed steps 1, 2, 4 in PR #30; step 3 (legacy 410 + remove) is the remaining work and stays open for the deprecation window declared below.
- Origin: code-audit-2026-04-26 alignment pass
  ([`./code-audit-2026-04-26.md`](./code-audit-2026-04-26.md), Phase
  B-1 finding B-1.2). The runtime surface map at
  [`/doc/docs/reference/architecture/runtime-surfaces.md`](/doc/docs/reference/architecture/runtime-surfaces.md#tenant-host--http-endpoints)
  declared four customer-tier endpoints under the `/api/public/*`
  prefix (`profile`, `catalog`, `session`, `orders`); only `orders`
  ships under that prefix today. The other three customer-tier
  surfaces ship as `/api/menu`, `/api/cart`, and `/api/sessions/open`
  / `/api/sessions/{ticketId}` and are gated by `[AllowAnonymous]`
  attributes rather than by a route prefix. Phase B-1 of the
  alignment pass updated the runtime-surfaces map to reflect the
  shipping reality and pointed the prefix migration at this entry.
- Symptom: `/api/public/*` was meant to be the unambiguous
  customer-tier surface — readable in logs, easy to tag in WAF /
  rate-limit rules, easy to grep for in security reviews. Today the
  customer-tier surface is mixed with the staff-tier surface under
  generic `/api/*` paths, separated only by attribute. A reviewer
  who searches for "customer surface" by route grep finds only one
  controller (`PublicOrdersController`); the other three are
  invisible to that search. The asymmetry also leaks into the audit
  finding RR-C2 ("tenant API controllers expose every endpoint
  anonymously") closed in TD-0015: the closure relies on attribute
  audit, not route audit.
- Risk if unpaid: a future contributor adding a new customer-tier
  endpoint follows the dominant pattern (mount under `/api/<noun>`)
  and inherits the attribute-only authorisation. A drift here
  re-creates the AC-010 / AC-043 violation that TD-0015 closed.
- Payoff plan:
  1. (Done in PR #30) Three shim controllers mounted under
     `/api/public/*`, each carrying `[AllowAnonymous]` at the
     controller level and delegating to the existing application
     service:
     - `/api/public/catalog` (`GET`, `GET category/{categoryId}`) —
       [`PublicCatalogController`](/src/apps/tenant/Controllers/Api/PublicCatalogController.cs)
       → `IMenuReadService`.
     - `/api/public/cart` (`POST`, `DELETE {id}`,
       `PUT {id}/quantity`, `GET session/{sessionId}`) —
       [`PublicCartController`](/src/apps/tenant/Controllers/Api/PublicCartController.cs)
       → `ICartService`.
     - `/api/public/session` (`POST open`, `GET {ticketId}`) —
       [`PublicSessionController`](/src/apps/tenant/Controllers/Api/PublicSessionController.cs)
       → `ICustomerSessionService`. The `open` action sets the
       same `tabflow_session_device` HttpOnly cookie that
       `SessionsController` already sets (TD-0017).
     The fourth originally-listed shim, `/api/public/profile`, is
     **not** part of PR #30 because no real per-tenant profile
     surface ships today; it is folded into the migration when a
     consumer requires it.
     `PublicOrdersController` was already at `/api/public/orders`
     (PR #6, TD-0015 step 3).
     The legacy `/api/menu`, `/api/cart`, and
     `/api/sessions/{open,{ticketId}}` routes stay operational
     during the deprecation window declared in step 3.
  2. (Done in PR #30) Customer-facing Blazor components updated to
     call the new prefix:
     - `Menu.razor`: `/api/menu` → `/api/public/catalog`;
       `/api/cart` → `/api/public/cart`.
     - `Cart.razor`: `/api/cart/session/{id}` →
       `/api/public/cart/session/{id}`; `/api/cart/{id}` →
       `/api/public/cart/{id}`. The submit call also moved from
       the **stale** `/api/orders/submit` (the route never shipped;
       `PublicOrdersController` lives at `/api/public/orders`) to
       the actual shipping route, so a previously-broken submit
       path is fixed in the same PR.
     - `ScanQr.razor`: `/api/sessions/open` →
       `/api/public/session/open`.
     `Order.razor` continues to call the staff-tier
     `/api/orders/{id}` route; introducing a customer-tier
     order-detail surface is out of scope for TD-0021 and is left
     as future work (no TD opened — track with the AD-0003
     follow-up that adds a customer-tier read for the bill view).
  3. (Open) After the deprecation window — one minor release per
     AD-0011 — return `Gone` (HTTP 410) from
     `MenuController` (full controller), `CartController` (full
     controller), and the customer-tier actions of
     `SessionsController` (`POST open`, `GET {ticketId}`). The
     staff-tier close action stays at
     `POST /api/sessions/{sessionId}/close`. Then remove the
     410-bodied actions in a subsequent PR. Constitution III.4
     requires the deprecation window to be stated in the same PR
     that introduces the replacement; this entry's step 1 is that
     statement.
  4. (Done in PR #30)
     [`/doc/docs/reference/api/tenant-api.md`](/doc/docs/reference/api/tenant-api.md)
     updated: the two Migration status callouts under "Public
     Catalog" and "Customer Session" now record that PR #30
     mounted the shim controllers, that the Blazor pages call the
     new prefix, and that the legacy routes stay through the
     deprecation window. The OpenAPI export is not yet in place;
     when it lands, it will declare the `/api/public/*` prefix as
     the canonical customer-tier shape.
- Linked: AC-010, AC-030, AC-043,
  [`/doc/docs/reference/architecture/runtime-surfaces.md`](/doc/docs/reference/architecture/runtime-surfaces.md#tenant-host--http-endpoints),
  [`/doc/docs/reference/api/tenant-api.md`](/doc/docs/reference/api/tenant-api.md),
  TD-0015 step 2 (closure relied on attribute audit; this entry adds
  the prefix audit),
  [`./code-audit-2026-04-26.md`](./code-audit-2026-04-26.md#5-phase-b--reference-tree-findings)

### [TRIAGE] TD-0020 — Pre-1.0 single-author phase: review-pair and security-review rules effectively suspended

- Opened: 2026-04-26
- Owner: TBD
- Origin: code-audit-2026-04-26 alignment pass
  ([`./code-audit-2026-04-26.md`](./code-audit-2026-04-26.md), Phase A
  finding A-4). The constitution
  ([`/doc/docs/constitution.md`](/doc/docs/constitution.md), V.2 and
  V.4) requires every PR to land with at least one non-author
  reviewer, and every security-sensitive PR to carry a
  `security: reviewed` note from a security-focused reviewer. With a
  single active maintainer through the pre-1.0 window, both rules are
  effectively suspended on every merge: no PR has carried a
  non-author approval, and no security-sensitive PR (PRs #6, #7,
  #11, #12, #16) has carried a `security: reviewed` note.
- Symptom: The constitution V.2 and V.4 invariants are visible in
  source but cannot be enforced today. Every merge during the pre-1.0
  window is effectively a stop-the-line solo merge per V.2's narrow
  exception, but PR bodies do not declare this explicitly, and there
  is no retroactive review PR scheduled per the
  [`./review-policy.md`](/doc/docs/meta/review-policy.md#stop-the-line-exception)
  one-working-day rule. The release-gate check at
  [`./release-gate.md`](/doc/docs/meta/release-gate.md#sign-off)
  step 3 ("every security-sensitive PR merged since the previous
  release carries a `security: reviewed` note") cannot pass on the
  first release.
- Risk if unpaid: V.2 and V.4 silently become aspirational rather
  than operational, repeating the failure mode the constitution's
  Amendment section explicitly forbids ("if a rule is being
  routinely ignored, that is a constitution bug, fixed by amendment,
  not by silence"). The first release-gate run hits a checklist that
  cannot be satisfied for the entire pre-1.0 history.
- Payoff plan:
  1. (Open) Add a second active maintainer with security focus, OR
     amend the constitution to add an explicit pre-1.0 single-author
     bypass (Section V.6 or equivalent) per the amendment template at
     [`/doc/docs/meta/amendment-template.md`](/doc/docs/meta/amendment-template.md).
     The amendment, if chosen, MUST cite this ledger entry as the
     motivating experience and MUST state the condition under which
     the bypass expires (e.g. first paying tenant, first release tag).
  2. (Open) Until step 1 lands, every PR opened during the pre-1.0
     window MUST carry an explicit "stop-the-line: pre-1.0
     single-author" line in the PR body so the bypass is auditable in
     git history rather than implicit.
  3. (Open) Once step 1 lands, retroactively review every PR merged
     during the pre-1.0 window in a single follow-up PR, OR open the
     first release-gate run with an explicit waiver line that lists
     the un-reviewed PRs by SHA.
- Linked: [`/doc/docs/constitution.md`](/doc/docs/constitution.md)
  Section V.2, V.4,
  [`/doc/docs/meta/review-policy.md`](/doc/docs/meta/review-policy.md),
  [`/doc/docs/meta/release-gate.md`](/doc/docs/meta/release-gate.md),
  [`./code-audit-2026-04-26.md`](./code-audit-2026-04-26.md#4-phase-a--meta-tree-findings)

### [TRIAGE] TD-0019 — Pre-1.0 placeholder TODOs lacking tech-debt ledger entries

- Opened: 2026-04-26
- Owner: TBD
- Origin: code-audit-2026-04-26 alignment pass
  ([`./code-audit-2026-04-26.md`](./code-audit-2026-04-26.md), Phase A
  finding A-3). A grep for `TODO|FIXME|XXX|HACK` across `/src/` and
  `/tests/` returns 12 placeholder comments, none of which carry a
  `TD-NNNN` reference. The constitution
  ([`/doc/docs/constitution.md`](/doc/docs/constitution.md) II.3)
  forbids the phrase "we'll fix it later" without a corresponding
  ledger entry.
- Inventory of un-ledgered TODOs (snapshot at pass open):
  - `src/apps/platform/Components/Pages/Audit.razor:39` — load audit
    list from database
  - `src/apps/platform/Components/Pages/Dashboard.razor:25` — load
    counters from database
  - `src/apps/platform/Components/Pages/TenantsDetail.razor:47` —
    load tenant detail from database
  - `src/apps/platform/Components/Pages/TenantsNew.razor:66` —
    create tenant + provisioning job
  - `src/apps/platform/Middleware/AuditMiddleware.cs:16` —
    automatic audit logging for mutating requests
  - `src/apps/tenant/Components/Pages/Cart.razor:105` — get TableId
    from session
  - `src/apps/tenant/Components/Pages/Cart.razor:106` — get
    checkout-proof token from QR token
  - `src/apps/tenant/Components/Pages/Cart.razor:116` — read
    actual order ID from `/api/public/orders` response
  - `src/apps/tenant/Components/Pages/ScanQr.razor:59` — implement
    real QR scanning with the camera
  - `src/apps/tenant/Components/Pages/TableView.razor:33` — load
    table state from database
  - `src/apps/tenant/Controllers/Api/OrdersController.cs:52` —
    expose actual order status instead of the literal `"Submitted"`
  - `src/apps/tenant/Services/EventSubscriptionService.cs:85` —
    notify kitchen staff on order submitted
  - `src/apps/tenant/WebSocket/TableWebSocketHandler.cs:116` —
    publish table-status events via the in-process bus
- Symptom: Constitution II.3 is violated by inspection: 12 untracked
  "we'll fix it later" markers. Each individual TODO is small, but
  the pattern is the failure mode II.3 is meant to prevent.
- Risk if unpaid: Each placeholder has a real implementation cost
  that is invisible to ledger-based planning; release-gate triage
  cannot find them; the first contributor to ship a UI surface in
  this area trips over a behaviour that "looked done" because it
  compiled.
- Payoff plan:
  1. (Done in PR #17) Inventoried the 12 TODOs above and rewrote the
     comments to carry a `TD-0019` reference, so a grep for
     `TD-0019` in source returns this ledger entry. Constitution II.3
     is back in compliance for the existing TODOs; the rule continues
     to forbid future TODOs without a `TD-NNNN` reference.
  2. (Open) For each TODO above, decide whether the gap is a
     ledgered-debt item that deserves its own `TD-NNNN` (because the
     resolution is non-trivial) or a hot-path implementation that
     should be closed before any UI surface in the same file ships.
     Resolution either splits TD-0019 into per-area TDs and closes
     this entry, or closes individual lines as their owners pick them
     up.
  3. (Open) Add a static-analysis rule that any remaining
     `TODO|FIXME|XXX|HACK` comment without a `TD-NNNN` reference is
     a build warning at minimum. The English-first analyser project
     at `/tools/analyzers/TabFlow.Analyzers/` is the natural home;
     this would be diagnostic ID `TF0003` (after `TF0001`
     non-ASCII identifiers and TD-0010 step 4's planned `TF0002`
     unit-tier-import rule).
- Linked: [`/doc/docs/constitution.md`](/doc/docs/constitution.md)
  Section II.3,
  [`./code-audit-2026-04-26.md`](./code-audit-2026-04-26.md#4-phase-a--meta-tree-findings)

### [TRIAGE] TD-0018 — Order idempotency key accepted on the wire but never persisted

- Opened: 2026-04-25
- Owner: TBD
- Origin: TD-0015 step 4 follow-up. `SubmitOrderRequest` carries an
  `IdempotencyKey` field that callers are expected to populate, and
  the customer-tier flow (PR #6 `PublicOrdersController` →
  `OrderService.SubmitAsync`) reads the field but never persists it,
  never queries against it, and never returns the prior result on a
  duplicate. The `Order` entity has no column for it.
- Symptom: a customer who taps "submit" twice on a flaky network can
  produce two distinct orders for the same cart against the same
  checkout-proof window. The token-consumption fix in TD-0015 step 4
  hides the worst case (the second submit fails on token reuse) but
  the idempotency contract is still unmet on its own terms.
- Risk if unpaid: occasional duplicate orders during connectivity
  blips; staff rely on manual reconciliation; complaints surface as
  "the customer was charged twice".
- Payoff plan:
  1. (Done in PR #12) Added `IdempotencyKey` (`string`, required) to
     the `Order` entity with `[Index(nameof(SessionId),
     nameof(IdempotencyKey), IsUnique = true)]`. The unique scope is
     the customer session so a different session can reuse the same
     key without collision. A backing migration
     `AddOrderIdempotencyKey` adds the column and the unique index.
  2. (Done in PR #12) `OrderService.SubmitAsync` now looks up
     `(SessionId, IdempotencyKey)` after the device-cookie gate but
     before the checkout-proof gate; if a matching order exists it
     returns the original `SubmitOrderResult` instead of inserting a
     second one. The unique index is the durable guard; the lookup
     is the cheap fast-path.
  3. (Open) Add an integration test that issues two `SubmitAsync`
     calls with the same `IdempotencyKey` and asserts a single
     `Order` row plus a single `OrderResult`. Depends on TD-0010
     fixtures.
- Linked: AC-031, AC-032,
  [`/src/apps/tenant/Services/OrderService.cs`](/src/apps/tenant/Services/OrderService.cs),
  [`/src/packages/shared-dotnet/Domain/Entities/Tenant/Order.cs`](/src/packages/shared-dotnet/Domain/Entities/Tenant/Order.cs)

### [TRIAGE] TD-0017 — Customer session device-binding not enforced (AC-030 second half)

- Opened: 2026-04-25
- Owner: TBD
- Origin: TD-0015 step 4 follow-up. AC-030 requires that
  `POST /api/public/orders` carries a still-open customer session
  *for the submitting device*. The token-consumption fix in TD-0015
  step 4 covers the still-open half; the device half is not
  enforced. The current `OrderService.SubmitAsync` validates that the
  session id from the request payload exists and is open, but does
  not check that the calling browser cookie corresponds to that
  session id.
- Symptom: a hostile network observer who captures the session id and
  checkout-proof token off the wire (or via JS console access on a
  shared device) can submit an order against another customer's
  session before the token expires.
- Risk if unpaid: AC-030 is half-implemented; the customer-session
  model in `customer-session-model.md` is documented in source but
  not enforced.
- Payoff plan:
  1. (Done in PR #11) Issue a server-set, `HttpOnly` cookie named
     `tabflow_session_device` when `Sessions.OpenSession` succeeds.
     The cookie value is an opaque server-issued GUID (`Guid.NewGuid().ToString("N")`),
     independent of the ticket id; cookie scope: `SameSite=Strict`,
     `Path=/`, `MaxAge=8h`, `Secure` keyed off `Request.IsHttps`.
  2. (Done in PR #11) Persist the cookie value alongside the
     `CustomerAccessTicket` row (new `DeviceCookieValue` column with
     a backing migration `AddCustomerAccessTicketDeviceCookie`). The
     binding is per-ticket rather than per-session because multiple
     customer devices can join the same table session and each
     device gets its own ticket.
  3. (Done in PR #11) In `OrderService.SubmitAsync`, look up the
     `CustomerAccessTicket` by `request.TicketId`, reject invalid /
     expired tickets, reject tickets that do not belong to
     `request.SessionId`, and constant-time-compare the persisted
     `DeviceCookieValue` against the cookie the controller forwarded
     (`CryptographicOperations.FixedTimeEquals`). The
     `PublicOrdersController` reads the cookie from the request and
     returns `403` if it is missing.
  4. (Open) Add an integration test that submits with the wrong
     cookie and asserts `403`. Depends on TD-0010 fixtures.
- Linked: AC-030, AC-036,
  [`/doc/docs/explanation/concepts/customer-session-model.md`](/doc/docs/explanation/concepts/customer-session-model.md),
  [`/src/apps/tenant/Services/OrderService.cs`](/src/apps/tenant/Services/OrderService.cs),
  [`/src/apps/tenant/Services/CustomerSessionCookie.cs`](/src/apps/tenant/Services/CustomerSessionCookie.cs)

### [TRIAGE] TD-0016 — AD-0004 mixed render modes never exercised

- Opened: 2026-04-25
- Owner: TBD
- Origin: re-review of the 2026-04-25 audit
  ([`./code-audit-2026-04-25.md`](./code-audit-2026-04-25.md), Section
  11, finding RR-H1). The first audit pass mis-attributed AD-0004's
  topic and therefore claimed it was honoured. AD-0004 is in fact
  about *Mixed Render Modes Per Surface Family*. A grep for
  `@rendermode`, `RenderMode.`, `InteractiveServer`,
  `InteractiveWebAssembly`, and `InteractiveAuto` across
  `/src/**/*.{cs,razor,cshtml}` returns zero hits across 19 Razor
  components and 8 cshtml pages.
- Symptom: every Blazor component runs in the default Static SSR mode.
  Customer-facing static pages happen to want exactly that, but
  staff-facing surfaces (kitchen board, table grid, station ticket
  state transitions) need `InteractiveServer` to deliver the SLO
  defined in AC-051 and the latency targets in AD-0006.
- Risk if unpaid: real-time surfaces silently degrade to full-page
  reloads; `tenant_event_push_p95_latency_ms` cannot be met regardless
  of the event bus speed.
- Payoff plan:
  1. (Done in PR #25) The hosts were first migrated from
     standalone Blazor Server to Blazor Web App (TD-0027); on top
     of that, every `.razor` page that needs interactivity now
     carries a `@rendermode InteractiveServer` directive:
       - **Platform host (6 staff pages):** `Dashboard.razor`,
         `Tenants.razor`, `TenantsNew.razor`, `TenantsDetail.razor`,
         `Jobs.razor`, `Audit.razor`.
       - **Tenant host (3 staff pages):** `Kitchen.razor`,
         `Tables.razor`, `TableView.razor`.
       - **Tenant host (4 customer pages):** `Cart.razor`,
         `Menu.razor`, `Order.razor`, `ScanQr.razor` carry the
         directive provisionally so the migration does not break
         existing `@onclick` / `IJSRuntime` behaviour. AD-0004
         requires Static SSR on those four pages; the conversion
         is tracked under TD-0028.
       Each `_Imports.razor` brings the
       `Microsoft.AspNetCore.Components.Web.RenderMode` static
       fields into scope so `@rendermode InteractiveServer`
       resolves without a fully-qualified type name.
  2. (Blocked) Add a release-gate smoke check that fetches one
     representative route per render-mode family and asserts the
     rendered HTML carries (or omits) the
     `_framework/blazor.web.js` interactive marker. Blocked on
     TD-0010 step 6 (Playwright bootstrap for the E2E /
     Smoke tier); landing the smoke check before that fixture is
     not feasible.
  3. (Done in PR #25) Capability matrix row "Tenant customer
     surfaces (Static SSR)" notes the four customer pages still
     run interactive and points at TD-0028; new row "Mixed render
     modes (AD-0004)" added with status `In progress`.
- Linked: AD-0004, TD-0027, TD-0028,
  [`./code-audit-2026-04-25.md`](./code-audit-2026-04-25.md#section-11-re-review-findings-2026-04-25)

### [TRIAGE] TD-0015 — Tenant API controllers expose every endpoint anonymously

- Opened: 2026-04-25
- Owner: TBD
- Origin: re-review of the 2026-04-25 audit
  ([`./code-audit-2026-04-25.md`](./code-audit-2026-04-25.md), Section
  11, finding RR-C2). The first audit pass omitted the entire
  `Controllers/` tree from its inventory and therefore did not look at
  authorisation. A grep showed:

  | Controller | `[Authorize]` count | HTTP methods |
  | --- | --- | --- |
  | `tenant/Controllers/Api/CartController` | 0 | 4 |
  | `tenant/Controllers/Api/KitchenController` | 0 | 2 |
  | `tenant/Controllers/Api/MenuController` | 0 | 2 |
  | `tenant/Controllers/Api/OrdersController` | 0 | 3 |
  | `tenant/Controllers/Api/SessionsController` | 0 | 3 |
  | `tenant/Controllers/Api/TablesController` | 0 | 2 |

  ASP.NET Core defaults to allow when no `[Authorize]` attribute is
  present, even with `app.UseAuthorization()` registered.
  Platform's two controllers (`JobsController`, `TenantsController`)
  are correctly decorated with `[Authorize(Policy = "Platform:Read")]`.
- Symptom: every staff-only endpoint on the tenant host (kitchen
  ticket state, table layout, raw cart manipulation, raw order
  submission, raw session open/close) responds to anonymous callers.
  AC-010, AC-043, and AC-051 cannot be satisfied. The
  `Tenant:Read` / `Tenant:Write` policies registered in `Program.cs`
  are never invoked.
- Risk if unpaid: any reachable tenant host leaks the full state of
  every active table and every open ticket to the public internet.
  This is the highest-priority security gap currently in the ledger.
- Payoff plan:
  1. (Done in PR #6) Decide per controller whether the surface is
     customer-facing (Menu, Cart, and the `open` / `get` actions on
     Sessions) or staff-only.
  2. (Done in PR #6) Customer-facing controllers carry `[AllowAnonymous]`
     at the controller level; staff-only controllers (`Kitchen`,
     `Orders`, `Tables`) carry `[Authorize(Policy = "Tenant:Read")]`
     at the controller level with action-level `[Authorize(Policy = "Tenant:Write")]`
     overrides on mutating endpoints (e.g. `Kitchen.UpdateItemStatus`,
     `Sessions.CloseSession`). `SessionsController` keeps a
     restrictive default (`Tenant:Read`) and explicitly opts the
     customer-tier actions out via `[AllowAnonymous]` to avoid the
     ASP0026 gotcha (a controller-level `[AllowAnonymous]` would
     silently override every action-level `[Authorize]`).
  3. (Done in PR #6) Customer-tier order submission split out into a
     dedicated `PublicOrdersController` mounted at `/api/public/orders`,
     closing the routing half of audit finding H-5.
  4. (Done in PR #7) Verified that `OrderService.SubmitAsync` enforces
     AC-030 (`session.IsOpen`) and AC-031 (`IsCheckoutProof` plus
     expiration). Tightened the path with three additional checks:
     (a) `IsConsumed == false` to reject token reuse (AC-032),
     (b) `TableId == request.TableId` to reject tokens from other
     tables (AC-031 freshness/same-table half), and (c) explicit
     `checkoutToken.Consume()` call in the same `SaveChangesAsync`
     transaction as the order insert. Two follow-up TDs opened for
     the remaining halves: TD-0017 (device-binding cookie for AC-030)
     and TD-0018 (idempotency key persistence on the `Order` entity).
  5. (Done in PR #7) Cookie-auth challenge on API endpoints now
     returns `401` / `403` instead of 302-redirecting to `/login`.
     `Program.cs` in both hosts wires `OnRedirectToLogin` and
     `OnRedirectToAccessDenied` events that short-circuit when the
     request path starts with `/api/`; HTML routes still redirect.
  6. (Open) Add an integration test per controller that asserts an
     anonymous request to a known staff endpoint yields HTTP `401` or
     `403`. Depends on TD-0010 (test taxonomy + fixture infrastructure
     for `Tenant.Tests`).
- Linked: AC-010, AC-030, AC-031, AC-043, AC-051,
  [`./code-audit-2026-04-25.md`](./code-audit-2026-04-25.md#rr-c2-tenant-api-controllers-expose-every-endpoint-anonymously)

### [TRIAGE] TD-0014 — `Directory.Build.props` analyzer baseline ratchet back up to `All`

- Opened: 2026-04-25
- Owner: TBD
- Origin: code audit of 2026-04-25
  ([`./code-audit-2026-04-25.md`](./code-audit-2026-04-25.md), finding
  H-1). The audit change set ran `dotnet build TabFlow.sln` for the
  first time against the new props file. The first run produced 11
  NU1008/NU1010 errors (Central Package Management mismatch) and a
  second pass exposed 26 IDE/CA errors at `AnalysisMode=All`. Both
  were reduced to a green build by lowering the analyzer baseline
  rather than fixing the existing code.
- Symptom: the green build at `Recommended` mode + `EnforceCodeStyleInBuild=false`
  + a named NoWarn list (`CA1716`, `CA1848`, `CA1873`, `CA1305`,
  `CA1304`, `CA1311`, `CA1822`, `CA1816`, `CA1707`, plus the existing
  `CS1591`) is the present baseline. AD-0014 expects the strictest
  level the project can hold; this lowered baseline is debt against
  that expectation.
- Risk if unpaid: every relaxed rule is a quality regression that
  ships unnoticed. `LoggerMessage` (CA1848 / CA1873) in particular
  matters at production log volumes.
- Payoff plan:
  1. (Done in audit change set) Resolve the CPM mismatch by removing
     the `Version` attribute from the
     `Microsoft.CodeAnalysis.Analyzers` PackageReference and pinning
     the version in `/Directory.Packages.props`.
  2. (Done in audit change set) Lower `AnalysisMode` to `Recommended`
     and disable `EnforceCodeStyleInBuild` until the existing baseline
     is swept.
  3. (Done in PR #15) Swept `LoggerMessage` adoption across all hot
     logging paths in the tenant host (`EventSubscriptionService`,
     `TableWebSocketHandler`) and the platform worker
     (`ProvisioningWorker`). Two new files
     `src/apps/tenant/TenantLogMessages.cs` and
     `src/apps/platform-worker/PlatformWorkerLogMessages.cs` carry
     `[LoggerMessage]` source-generated extension methods (EventId
     allocations: PlatformWorker 1–5, Tenant 101–109 + 201–208). All
     19 `_logger.LogX(...)` call sites in
     `/src/apps/{tenant,platform-worker}/**/*.cs` were rewritten to
     the generated extensions; `CA1848` and `CA1873` removed from
     `NoWarn` in `/Directory.Build.props`. Build remains green at
     0 / 0.
  4. Sweep culture-aware string conversions (CA1305 / CA1304 /
     CA1311) at externally-visible call sites; remove from NoWarn.
  5. Audit `static`-able members (CA1822) and finalise (CA1816);
     remove from NoWarn.
  6. Re-enable `EnforceCodeStyleInBuild=true` after a tree-wide
     `dotnet format` pass.
  7. Restore `AnalysisMode=All`.
- Linked: AD-0014, [`./code-audit-2026-04-25.md`](./code-audit-2026-04-25.md#h-1-directorybuildprops-may-break-the-existing-build),
  [`/Directory.Build.props`](/Directory.Build.props),
  [`/Directory.Packages.props`](/Directory.Packages.props)

### [OPEN] TD-0013 — Advanced health-check probes still missing

- Opened: 2026-04-25
- Owner: closed steps 1, 2, 4, 5 in PR #31; steps 3 (worker heartbeat — blocked on schema) and 6 (release-gate smoke check — blocked on TD-0010 step 6 Playwright bootstrap) remain open.
- Origin: code audit of 2026-04-25
  ([`./code-audit-2026-04-25.md`](./code-audit-2026-04-25.md), finding
  C-4). The audit change set landed the three endpoints (`/health`,
  `/health/live`, `/health/ready`) on both hosts plus the basic
  `*-db:ping` probe per host — enough to satisfy AC-101 and AC-102
  literally. The richer probe set described in
  [`/doc/docs/reference/architecture/health-checks.md`](/doc/docs/reference/architecture/health-checks.md)
  is **not** yet wired.
- Symptom: a deployed process answers `/health/ready` with `pass` even
  when (a) a migration is pending against an open DB connection, (b)
  the platform worker has died, (c) the in-process event bus is
  saturated, or (d) the tenant code resolves to a disabled row.
- Risk if unpaid: the readiness signal under-reports four real
  failure modes. Operators trust the green light too much.
- Payoff plan:
  1. (Done in audit change set) Wire `platform-db:ping` /
     `tenant-db:ping` via `AddDbContextCheck<T>` on both hosts. Wire
     the IETF `health+json` writer in `TabFlow.Shared`.
  2. (Done in PR #31)
     `TabFlow.Shared.Infrastructure.Diagnostics.MigrationHeadHealthCheck<TContext>`
     authored. Generic over the `DbContext`; calls
     `_context.Database.GetPendingMigrationsAsync(ct)` and reports
     `Healthy` with `"Database schema is at the migration head."`
     when the result is empty, otherwise `Unhealthy` with the
     pending-migration list. Registered on both hosts under the
     `ready` tag as `platform-db:migrations` /
     `tenant-db:migrations`. Smoke-tested: both hosts now emit the
     pass row alongside the existing `*-db:ping` probe.
  3. (Open, blocked on schema) `WorkerHeartbeatHealthCheck`
     reading `worker_heartbeats` (rows newer than 30s) is gated on
     the `worker_heartbeats` table, which does not yet exist in
     the platform schema. Lands alongside the worker-instrumentation
     work that introduces the table; the schema-spec docs at
     `/doc/docs/reference/architecture/health-checks.md` continue
     to declare the probe so the ratchet target is visible.
  4. (Done in PR #31) `IEventBus` extended with a `GetCapacityStats()`
     diagnostic method returning `(SubscriberCount, MaxQueueDepth,
     PerSubscriberCapacity)`. `InProcessEventBus` implements the
     method by locking the subscriber list and walking each
     channel's `Reader.Count`.
     `TabFlow.Shared.Infrastructure.Diagnostics.EventBusCapacityHealthCheck`
     authored: computes saturation as
     `MaxQueueDepth / PerSubscriberCapacity`; reports `Healthy`
     below 80%, `Degraded` at 80%–95%, `Unhealthy` at and above
     95%. Registered on the tenant host under the `ready` tag as
     `event-bus:capacity`. Smoke-tested: idle tenant reports
     `pass` with `"subscribers=1 max-depth=0/256 (0%)"`.
  5. (Done in PR #31)
     `TabFlow.Shared.Infrastructure.Diagnostics.TenantContextHealthCheck`
     authored. Reads the `TABFLOW_TENANT_CODE` environment variable
     and reports `Unhealthy` when the variable is unset or empty.
     The richer "resolve TABFLOW_TENANT_CODE against the platform's
     `tenant_registry`" lift waits on a platform→tenant connection
     contract that is not in scope for PR #31. Registered on the
     tenant host under the `ready` tag as `tenant-context`.
     Smoke-tested: a tenant host launched without
     `TABFLOW_TENANT_CODE` returns `/health/ready` with
     `status="fail"` and the env-var message; the platform host's
     readiness is unaffected.
  6. (Open, blocked on Playwright bootstrap) Add a release-gate
     smoke check that hits all three endpoints on the staging
     tenant and asserts the JSON shape. Blocked on TD-0010 step 6
     (the smoke-tier infrastructure that hosts the assertion).
- Linked: AC-101, AC-102,
  [`/doc/docs/reference/architecture/health-checks.md`](/doc/docs/reference/architecture/health-checks.md),
  [`./code-audit-2026-04-25.md`](./code-audit-2026-04-25.md#c-4-no-health-endpoints-on-either-host)

### [TRIAGE] TD-0012 — Serilog wiring exists in source but has never been exercised

- Opened: 2026-04-25
- Owner: TBD
- Origin: code audit of 2026-04-25
  ([`./code-audit-2026-04-25.md`](./code-audit-2026-04-25.md), finding
  H-4). Initial finding text claimed Serilog was absent because a grep
  for the literal `Serilog` against `/src/apps/**/*.cs` returned zero
  hits at the time. The audit change-set's first `dotnet build`
  revealed two `using Serilog;` directives, four `Serilog.*` package
  references, and a `Log.Logger = new LoggerConfiguration()` chain
  per host. The chain called `WithEnvironment()`, which does not
  exist in `Serilog.Enrichers.Environment` 3.x; the call site had
  therefore never compiled, let alone run.
- Symptom: the capability matrix row reads `In progress | Console +
  file sinks wired in platform and tenant hosts.` That is now
  literally true (build is green) but operationally untested — no
  log line has ever left a deployed process via Serilog.
- Risk if unpaid: a release-gate signer reads the matrix as
  "observability is wired" and skips the smoke check that would
  catch a misconfigured sink.
- Payoff plan:
  1. (Done in audit change set) Remove the broken `WithEnvironment()`
     call so the configuration compiles.
  2. Run the platform host once locally and confirm a log line
     reaches the configured Console sink.
  3. Run the platform host once with the configured file sink path
     accessible (`/var/log/tabflow/...`) and confirm the file is
     written, rotated, and readable.
  4. Add a smoke check to the release gate that a known startup line
     appears in the file sink.
- Linked: [`/doc/docs/reference/architecture/capability-matrix.md`](/doc/docs/reference/architecture/capability-matrix.md),
  [`./code-audit-2026-04-25.md`](./code-audit-2026-04-25.md#h-4-serilog-is-referenced-in-the-capability-matrix-but-absent-in-code)

### [TRIAGE] TD-0011 — `IStringLocalizer<T>` baseline absent

- Opened: 2026-04-25
- Owner: TBD
- Origin: code audit of 2026-04-25
  ([`./code-audit-2026-04-25.md`](./code-audit-2026-04-25.md), finding
  H-2). Zero matches for `IStringLocalizer` across
  `/src/apps/**/*.{cs,razor}`. AD-0015 and the i18n explainer assume a
  resx layout that does not yet exist.
- Symptom: AC-119 ("Every user-facing string in a Razor component
  MUST be routed through `IStringLocalizer<T>`") cannot be enforced;
  the analyzer in TD-0009 has no resx target to verify against.
- Risk if unpaid: the first localised string lands ad-hoc and sets a
  bad precedent that later strings copy.
- Payoff plan: introduce a `Resources/` folder per host, add the
  English neutral resx files, register `AddLocalization()` and the
  request-localisation middleware in both `Program.cs` files, and
  port one Razor component as the reference example.
- Linked: AD-0015, AC-119, AC-120, TD-0009,
  [`/doc/docs/explanation/concepts/internationalization.md`](/doc/docs/explanation/concepts/internationalization.md)

### [TRIAGE] TD-0010 — Test projects organised by project, not by tier

- Opened: 2026-04-25
- Owner: TBD
- Origin: code audit of 2026-04-25
  ([`./code-audit-2026-04-25.md`](./code-audit-2026-04-25.md), finding
  H-3). Today's layout is `Platform.Tests`, `Tenant.Tests`,
  `Shared.Tests`, `PlatformWorker.Tests`, `E2E.Tests`. Inside
  `Tenant.Tests/` the `Controllers/` and `Services/` folders mix
  unit-style and integration-style tests. The audit change-set's
  first `dotnet build` also revealed that `tests/E2E.Tests/` is
  **not** included in `TabFlow.sln`, so it is silently absent from
  every CI build today. The audit change set's first `dotnet test`
  (run while landing TD-0013) confirmed three further symptoms: (a)
  every test in `Tenant.Tests` fails by attempting to reach a real
  PostgreSQL instance via `EnsureCreatedAsync()`, so the project is
  effectively integration-tier mis-labelled as unit; (b)
  `Platform.Tests` and `PlatformWorker.Tests` contain zero tests —
  empty CI placeholders that report green; (c) `Shared.Tests` is the
  only project with passing tests today (8 tests, all from the
  TD-0013 change set).
- Symptom: AC-133 ("No test in the `Unit` tier MAY touch the file
  system, the network, the system clock, or a database") cannot be
  enforced statically; the four-tier taxonomy in
  [`/doc/docs/explanation/concepts/test-taxonomy.md`](/doc/docs/explanation/concepts/test-taxonomy.md)
  is a contract on paper only.
- Risk if unpaid: a unit test reaching into PostgreSQL becomes the
  norm; the CI signal "unit tests are green" stops meaning anything.
- Payoff plan:
  1. (Done in PR #13) Adopted xUnit's `[Trait("Category", T)]`
     convention as the tier discriminator (`Unit`, `Integration`,
     `E2E`, `Smoke`). Class-level traits applied to existing suites:
     `Shared.Tests/HealthJsonWriterTests` → `Unit`,
     `Tenant.Tests/{CartService,CustomerSessionService,OrdersController}Tests`
     → `Integration`,
     `E2E.Tests/{Platform,Tenant}E2ETests` → `E2E`. Convention
     documented at
     [`/doc/docs/explanation/concepts/test-taxonomy.md#xunit-trait-convention`](/doc/docs/explanation/concepts/test-taxonomy.md#xunit-trait-convention).
  2. (Done in PR #13) Added `tests/E2E.Tests/E2E.Tests.csproj` to
     `TabFlow.sln` (it was silently absent from every CI build until
     now). `Microsoft.Playwright` 1.49.0 added to
     `Directory.Packages.props`; the legacy `Playwright` package
     reference removed. Both hosts now expose `public partial class
     Program` so the E2E project can disambiguate via
     `extern alias PlatformHost;` / `extern alias TenantHost;` and
     reference `PlatformHost::Program` / `TenantHost::Program`
     explicitly (CS0433 fix).
  3. (Done in PR #13) PR workflow (`.github/workflows/pr.yml`)
     splits `dotnet test` into a Unit fast-path (no DB) and an
     Integration step (PostgreSQL service container). E2E tier is
     intentionally excluded from the PR workflow until a browser
     bootstrap step lands.
  4. (Done in PR #26) Authored a second first-party Roslyn analyser
     `TabFlow.Analyzers.UnitTierTestPurityAnalyzer` (rule `TF0002`,
     category `Testing`, default severity `Warning` — promoted to
     a build break by `TreatWarningsAsErrors=true`). The analyser
     fires only inside classes carrying
     `[Trait("Category", "Unit")]` and reports any identifier
     resolving to a forbidden type or member:
       - **Database (banned namespace).** Anything under `Npgsql.*`.
       - **Network.** `System.Net.Sockets.*` (every type),
         `System.Net.Http.HttpClient` (specific).
       - **File system.** `System.IO.File`,
         `System.IO.Directory`, `System.IO.FileStream`.
       - **System clock.** `System.DateTime.Now` and
         `System.DateTimeOffset.Now` (the `Now` properties only —
         `UtcNow` remains allowed because it is deterministic
         across timezones, which is what unit tests need).
     Tests that genuinely need any of those move to the
     Integration tier; the rule is silent outside the Unit tier.
     Released in
     `tools/analyzers/TabFlow.Analyzers/AnalyzerReleases.Unshipped.md`.
     Backed by 7 Unit-tier regression tests at
     `tests/Analyzers.Tests/UnitTierTestPurityAnalyzerTests.cs`.
  5. (Open) Move the existing `Tenant.Tests/Services/*` tests off a
     real PostgreSQL fixture and onto a per-test transactional
     fixture so the Integration step is hermetic.
  6. (Open) Wire the browser-bootstrap step
     (`microsoft.playwright.cli install`) into a separate
     `e2e.yml` workflow and unblock the E2E tier in CI.
- Linked: AC-131, AC-132, AC-133, AC-134,
  [`/doc/docs/explanation/concepts/test-taxonomy.md`](/doc/docs/explanation/concepts/test-taxonomy.md),
  [`/.github/workflows/pr.yml`](/.github/workflows/pr.yml)

### [TRIAGE] TD-0009 — English-first lint rule not yet enforced

- Opened: 2026-04-25
- Owner: TBD
- Origin: AD-0015 accepted; the analyzer rule that rejects non-ASCII
  identifiers under `/src/` and `/tests/` is referenced in the ADR but
  has not been authored.
- Symptom: nothing prevents a contributor from introducing a Turkish
  identifier in code today; AC-117, AC-118, AC-119 cannot be enforced
  by CI.
- Risk if unpaid: AD-0015 lives only in review discipline, not in
  tooling; first violation is harder to revert than to prevent.
- Payoff plan:
  1. (Done in PR #14) Authored first-party Roslyn analyser
     `TabFlow.Analyzers.EnglishFirstIdentifierAnalyzer` (rule
     `TF0001`). The analyser registers a symbol action across
     `NamedType`, `Method`, `Property`, `Field`, `Event`, and
     `Parameter` symbol kinds; flags any identifier whose name
     contains a code unit `> 0x7F`; skips compiler-generated names
     (those starting with `<` or `$`). Default severity is `Warning`
     which `TreatWarningsAsErrors=true` (Directory.Build.props)
     promotes to a build break.
  2. (Done in PR #14) New project at
     `/tools/analyzers/TabFlow.Analyzers/` targeting `netstandard2.0`
     (Roslyn analyser convention). `Directory.Build.props` references
     it as an `OutputItemType="Analyzer"` ProjectReference under a
     `'$(MSBuildProjectName)' != 'TabFlow.Analyzers'` condition so
     the analyser project does not reference itself.
  3. (Done in PR #14) Smoke check confirmed: a temporary file with
     `class deneme { public string açıklama => "x"; }` in
     `Shared.Tests` produced two `error TF0001` lines (one for the
     property, one for its compiler-generated `get_açıklama`); the
     temporary file was then removed and `dotnet build` returned to
     0 / 0.
  4. (Done in PR #24) Authored a `Microsoft.CodeAnalysis.Testing`
     xUnit suite at
     `tests/Analyzers.Tests/EnglishFirstIdentifierAnalyzerTests.cs`
     with 7 cases: ASCII baseline (no diagnostic), positive cases
     for `NamedType`, `Method`, `Property`, `Field`, and `Parameter`
     (each emits a single TF0001), and a compiler-generated names
     baseline (auto-property accessors). The suite runs in the
     **Unit** tier and is picked up by the existing PR workflow
     `Run unit tests` step (filter `Category=Unit`). The harness
     surfaced one analyser bug fixed in the same PR: property and
     event accessor methods (`get_X`, `set_X`, `add_X`, `remove_X`,
     `raise_X`) now skip the rule so a single non-ASCII property
     emits one diagnostic at the property declaration rather than
     three (property + get + set).
  5. (Done in PR #24) Authored
     `tools/analyzers/TabFlow.Analyzers/AnalyzerReleases.Shipped.md`
     (empty placeholder; populated at the first tagged release per
     AD-0011) and
     `tools/analyzers/TabFlow.Analyzers/AnalyzerReleases.Unshipped.md`
     (declares TF0001 with rule ID, category, severity, and notes).
     `TabFlow.Analyzers.csproj` now feeds both files via
     `<AdditionalFiles>` and removes the `RS2008` `NoWarn`
     suppression. Future TF* rules add a row to
     `AnalyzerReleases.Unshipped.md` in the same PR that authors
     the analyser; the next release PR moves the rows to
     `AnalyzerReleases.Shipped.md` under a version heading.
- Linked: AD-0015, AC-117, AC-118, AC-119,
  [`/.editorconfig`](/.editorconfig),
  [`/tools/analyzers/TabFlow.Analyzers/EnglishFirstIdentifierAnalyzer.cs`](/tools/analyzers/TabFlow.Analyzers/EnglishFirstIdentifierAnalyzer.cs)

### [TRIAGE] TD-0008 — Retention sweep jobs not implemented

- Opened: 2026-04-25
- Owner: TBD
- Origin: data-protection explainer accepted with a retention schedule;
  no platform-worker job handler implements the sweeps yet.
- Symptom: retention windows in
  [`/doc/docs/explanation/concepts/data-protection.md`](/doc/docs/explanation/concepts/data-protection.md#retention-schedule)
  are aspirational; AC-123 and AC-124 cannot be exercised.
- Risk if unpaid: KVKK and GDPR retention obligations unmet at the
  first production deploy.
- Payoff plan: implement one sweep job per data class with the cadence
  in the retention schedule; each writes a single audit-log entry per
  run.
- Linked: AC-123, AC-124,
  [`/doc/docs/explanation/concepts/data-protection.md`](/doc/docs/explanation/concepts/data-protection.md)

### [TRIAGE] TD-0007 — Personal-data classification not on the schema

- Opened: 2026-04-25
- Owner: TBD
- Origin: data-protection explainer requires a `[DataClass]` attribute
  on every personal-data property and a corresponding schema comment;
  neither the attribute nor the comment generation exists.
- Symptom: AC-122 cannot be verified.
- Risk if unpaid: schema reviewers cannot tell whether a new column
  contains personal data; data-protection contract drifts from the
  schema.
- Payoff plan: introduce `DataClassAttribute`, write a small EF Core
  convention that emits the comment to the schema, add a
  release-gate check that every `Sensitive` and `Restricted` column
  has a comment.
- Linked: AC-122,
  [`/doc/docs/explanation/concepts/data-protection.md`](/doc/docs/explanation/concepts/data-protection.md)

### [TRIAGE] TD-0006 — Branch protection not configured on `main`

- Opened: 2026-04-25
- Owner: TBD
- Origin: [`/doc/docs/how-to/configure-branch-protection.md`](/doc/docs/how-to/configure-branch-protection.md)
  spells out the required GitHub configuration; the configuration has
  not been applied to the repository.
- Symptom: `main` accepts direct pushes today; the constitution V.1
  rule "no unreviewed code lands on main" depends on an honour system,
  not a guarantee.
- Risk if unpaid: a single mistake bypasses the entire release gate;
  postmortem retroactively reviews every commit during the gap.
- Payoff plan: apply the configuration documented in the how-to;
  exercise the verification command; add the
  `branch-protection-check.yml` workflow.
- Linked: [`/doc/docs/how-to/configure-branch-protection.md`](/doc/docs/how-to/configure-branch-protection.md),
  [`/doc/docs/constitution.md`](/doc/docs/constitution.md) Section V.1

### [TRIAGE] TD-0005 — CI workflows not yet validated by a real PR

- Opened: 2026-04-25
- Owner: TBD
- Origin: AD-0013 accepted; `.github/workflows/pr.yml`, `main.yml`,
  and `release.yml` are committed but have not run against a real
  pull request or release tag.
- Symptom: workflow correctness is untested; an action version pin or
  a YAML expression error will surface only on the first PR.
- Risk if unpaid: first real PR breaks before merging; release-gate
  CI signals are absent at the moment they matter most.
- Payoff plan: open a trivial documentation PR to exercise `pr.yml`;
  cut a `v0.1.0` tag against an internal staging branch to exercise
  `release.yml`; fix any failures encountered.
- Linked: AD-0013,
  [`/.github/workflows/pr.yml`](/.github/workflows/pr.yml)

### [TRIAGE] TD-0004 — Backup encryption pipeline not implemented

- Opened: 2026-04-25
- Owner: TBD
- Origin: backup-and-restore how-to specifies LUKS volume + age
  re-encryption + off-site append-only credentials; none of these
  components is wired in deploy automation.
- Symptom: AC-127 and AC-128 cannot be verified; the documented
  encryption configuration is aspirational.
- Risk if unpaid: a backup leak exposes personal data unencrypted;
  KVKK / GDPR breach notification triggers immediately.
- Payoff plan: configure LUKS on the backup volume; introduce an `age`
  recipient key into the operator's secret manager; wrap `pg_dump`
  output through `age`; configure off-site replication with an
  append-only credential.
- Linked: AC-127, AC-128,
  [`/doc/docs/how-to/backup-and-restore.md`](/doc/docs/how-to/backup-and-restore.md#encryption)

### [TRIAGE] TD-0003 — Tenant migrations applied via ad-hoc tooling

- Opened: 2026-04-25
- Owner: TBD
- Origin: docs review session of 2026-04-25. The re-review of the
  same date
  ([`./code-audit-2026-04-25.md`](./code-audit-2026-04-25.md), Section
  11, finding RR-C3) sharpened the diagnosis: the lone tenant
  migration on disk
  (`/src/infra/postgres/Migrations/20260425144800_InitialCreate.cs`)
  is **22 lines long with zero `CreateTable` calls** — its `Up` body
  is empty. The follow-up `SeedInitialData` migration runs raw SQL
  `INSERT INTO "categories"`, `INSERT INTO "stations"`, `INSERT INTO
  "tables"`, and `INSERT INTO "menu_items"` against tables that the
  empty `InitialCreate` never created.
- Symptom: tenant database for `dev-local` was provisioned outside the
  worker; `Migrations/Tenant/` snapshot exists but no migration history
  has run against any tenant database. Running `MigrateAsync()` today
  would raise `relation "categories" does not exist` on the first seed
  statement.
- Risk if unpaid: tenant schema drift; provisioning worker cannot rely
  on `TenantDbContext.Database.MigrateAsync()` until the migration tree
  is real. AC-082 ("Every tenant database migration MUST be applied
  via committed EF Core migrations") is violated end-to-end.
- Payoff plan:
  1. (Done in PR #10) Dropped the empty `InitialCreate` and the
     orphan `SeedInitialData` migration files plus the stale
     `Migrations/Tenant/TenantDbContextModelSnapshot.cs`. The
     Turkish-string seed (RR-M1) was removed in the same step; the
     model is the single source of truth for the next scaffolded
     migration.
  2. (Done in PR #10) Ran `dotnet ef migrations add InitialCreate
     --project src/infra/postgres/TabFlow.Migrations.csproj
     --context TenantDbContext --output-dir Migrations/Tenant`. The
     scaffolded migration is 586 lines with 64 `CreateTable` /
     `migrationBuilder` calls covering Identity, customer-session,
     QR-token, table, station, menu, cart, order, bill, audit-log,
     and event-bus tables. The accompanying snapshot is regenerated.
  3. (Open, deferred) If demo seed data is reintroduced later, add a
     separate `SeedDemoData` migration **after** `InitialCreate` and
     keep all internal contract values English-only (AD-0015,
     AC-118). Until that decision is made deliberately the tenant
     database starts empty.
  4. (Operator action, pending) Have the worker apply the migration
     set during `tenant.create`. The provisioning worker should call
     `MigrateAsync()` against a fresh tenant database; the previous
     ad-hoc tooling has been removed.
- Linked: AD-0008, AD-0009, AD-0015, AC-082,
  [`/doc/docs/how-to/setup-migrations.md`](/doc/docs/how-to/setup-migrations.md),
  [`./code-audit-2026-04-25.md`](./code-audit-2026-04-25.md#rr-c3-tenant-initialcreate-up-body-is-empty)

### [TRIAGE] TD-0002 — Bootstrap admin not yet implemented

- Opened: 2026-04-25
- Owner: TBD
- Origin: docs review session of 2026-04-25; AD-0010 accepted but no
  CLI command exists.
- Symptom: there is no way to create the first platform admin without
  hand-inserting into `AspNetUsers`. The audit of 2026-04-25 (C-1)
  found a migration (`InitialAdminUser.cs`) that did exactly this with
  a hard-coded placeholder hash; the same change set that opened this
  audit removed that migration. Until this TD lands the system has no
  admin at all on a fresh bootstrap.
- Risk if unpaid: a fresh deployment cannot be administered. Operators
  may regress by hand-inserting into `AspNetUsers`, which would
  re-create the AC-005 violation outside the migration tree.
- Payoff plan:
  1. (Done in PR #9) Implement `bootstrap-admin` CLI on the platform
     host. The platform `Program.cs` dispatches to
     `TabFlow.Platform.Cli.BootstrapAdminCommand.RunAsync` before the
     web host starts when `args[0] == "bootstrap-admin"`. The command:
     - parses `--email <address>`; prints usage and returns exit 1 if
       missing;
     - builds a minimal Generic Host with `PlatformDbContext`,
       Identity for `IdentityUser<Guid>` / `IdentityRole<Guid>`, and
       `IPlatformAuditService`;
     - refuses to run if any user already exists in `AspNetUsers`
       (returns exit 2);
     - generates a 24-character CSPRNG password from a 73-char
       alphabet (~148 bits of entropy);
     - calls `UserManager.CreateAsync` so the password hash uses the
       framework's current `IPasswordHasher` defaults;
     - ensures the `owner` role exists and assigns it;
     - writes an `auth.bootstrap` row to `platform_audit_log`;
     - prints the password to stdout exactly once and returns 0.
  2. (Operator action, pending) Run the command once on a fresh
     deployment per the procedure in
     [`/doc/docs/how-to/bootstrap-platform.md`](/doc/docs/how-to/bootstrap-platform.md).
  3. (Done in PR #16) Force-redirect through `/change-password` on
     every request from a still-bootstrap-shaped principal.
     `BootstrapAdminCommand` now stamps a
     `tabflow:must_change_password` claim on the user (between role
     assignment and the audit-log write; new exit code 6 reserved for
     the claim-add failure). A new
     `PasswordChangeRequiredMiddleware`, registered after
     `UseAuthorization` and before route mapping in the platform
     `Program.cs`, redirects authenticated principals carrying the
     claim to `/change-password` unless the request path falls in an
     exemption list (`/change-password`, `/login`, `/logout`,
     `/_blazor`, `/_framework`, `/_content`, `/health`, `/api`,
     `/lib`, `/css`, `/js`). The `ChangePassword` page now carries
     `[Authorize]` and, on a successful
     `UserManager.ChangePasswordAsync`, enumerates the user's claims
     and removes every `tabflow:must_change_password` claim before
     calling `RefreshSignInAsync`. The claim is the single piece of
     state the middleware reads, so the loop tolerates duplicates
     defensively without a separate "already cleared" path.
- Linked: AD-0010, AC-005, AC-006,
  [`/doc/docs/how-to/bootstrap-platform.md`](/doc/docs/how-to/bootstrap-platform.md),
  [`/src/apps/platform/Cli/BootstrapAdminCommand.cs`](/src/apps/platform/Cli/BootstrapAdminCommand.cs),
  [`/src/apps/platform/Middleware/PasswordChangeRequiredMiddleware.cs`](/src/apps/platform/Middleware/PasswordChangeRequiredMiddleware.cs),
  [`/src/apps/platform/Pages/ChangePassword.cshtml.cs`](/src/apps/platform/Pages/ChangePassword.cshtml.cs),
  [`./code-audit-2026-04-25.md`](./code-audit-2026-04-25.md#c-1-migration-seeds-the-first-platform-admin-into-aspnetusers)

### [TRIAGE] TD-0001 — Hand-applied platform schema instead of EF Core migration

- Opened: 2026-04-25
- Owner: TBD
- Origin: docs review session of 2026-04-25; AD-0008 not honoured during
  early skeleton work.
- Symptom: `tabflow_platform` schema was created via `psql` and a hand
  migration that produced an empty `Up` body. `__EFMigrationsHistory`
  is inconsistent with the EF Core model. The audit of 2026-04-25 (C-3)
  also found two parallel migration trees on disk
  (`Migrations/` Pascal and `migrations/` lowercase) compiled into the
  same project. The same change set as the audit removed the lowercase
  tree entirely; PlatformDbContext now has zero migrations and zero
  snapshot in source.
- Risk if unpaid: schema drift; release-gate cannot verify migration
  history matches model. Until the rebuild step below runs, `dotnet ef
  database update --context PlatformDbContext` produces no schema at
  all.
- Payoff plan:
  1. (Done as part of the audit change set) Remove the lowercase
     `migrations/` tree.
  2. (Operator action, pending) Drop the existing `tabflow_platform`
     database on the next bootstrap window.
  3. (Done in PR #8) Ran `dotnet ef migrations add InitialCreate
     --project src/infra/postgres/TabFlow.Migrations.csproj
     --context PlatformDbContext --output-dir Migrations/Platform`.
     The scaffolded migration is 361 lines with 38
     `CreateTable` / `migrationBuilder` calls covering the full
     `PlatformDbContext` model (Identity tables plus `tenants`,
     `provisioning_jobs`, `tenant_database_connections`, etc.). The
     model snapshot at `Migrations/Platform/PlatformDbContextModelSnapshot.cs`
     is regenerated from scratch so the next `add` command produces a
     correct delta.
  4. (Operator action, pending) Run `dotnet ef database update
     --context PlatformDbContext` against the empty database to apply
     the new tree.
  5. (Operator action, pending) Verify
     `__ef_migrations_history` matches the model snapshot.
- Linked: AD-0008, AD-0009, AC-082,
  [`/doc/docs/how-to/setup-migrations.md`](/doc/docs/how-to/setup-migrations.md),
  [`./code-audit-2026-04-25.md`](./code-audit-2026-04-25.md#c-3-two-parallel-migration-trees-migrations-and-migrations)

## Open Debt

<!-- Active debt with a named owner. Newest entries appended at the
     top of this section once they leave the triage queue. -->

(none yet)

## Closed Debt

<!-- Closed entries kept for the historical record. -->

(none yet)

## Accepted Debt

<!-- Entries with [ACCEPTED] status — debt that will not be paid. Each
     MUST cite an ADR explaining why the compromise is permanent. -->

(none)
