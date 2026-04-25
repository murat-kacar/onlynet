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
  1. Add `IdempotencyKey` to the `Order` entity with a unique index
     scoped to `(SessionId, IdempotencyKey)`.
  2. In `OrderService.SubmitAsync`, look up the existing order by
     `(SessionId, IdempotencyKey)` before any other gate; if found,
     return its `SubmitOrderResult` instead of inserting again.
  3. Add an integration test that issues two `SubmitAsync` calls with
     the same `IdempotencyKey` and asserts a single `Order` row plus
     a single `OrderResult`.
- Linked: AC-031, AC-032,
  [`/src/apps/tenant/Services/OrderService.cs`](/src/apps/tenant/Services/OrderService.cs)

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
  1. Issue a server-set, `HttpOnly`, table-scoped cookie when
     `Sessions.OpenSession` succeeds. The cookie value MUST be
     opaque (independent random GUID, not the session id).
  2. Persist the cookie value alongside the customer session row.
  3. In `OrderService.SubmitAsync`, accept an `HttpContext` parameter
     (or read the cookie via an injected accessor) and reject
     submissions where the cookie does not match the session row.
  4. Add an integration test that submits with the wrong cookie and
     asserts `403`.
- Linked: AC-030, AC-036,
  [`/doc/docs/explanation/concepts/customer-session-model.md`](/doc/docs/explanation/concepts/customer-session-model.md),
  [`/src/apps/tenant/Services/OrderService.cs`](/src/apps/tenant/Services/OrderService.cs)

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
  1. Annotate each Razor component family with the render mode it
     needs per the matrix in AD-0004 (customer surfaces → Static SSR,
     staff surfaces → InteractiveServer, station boards →
     InteractiveServer with explicit prerendering off).
  2. Add a release-gate smoke check that, for one representative
     component per family, asserts the rendered HTML carries the
     expected `blazor-server` script reference (or its absence for
     Static SSR).
  3. Update the capability matrix row for "Mixed render modes" once
     step 1 is in.
- Linked: AD-0004,
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
  AC-008, AC-043, and AC-051 cannot be satisfied. The
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
- Linked: AC-008, AC-030, AC-031, AC-043, AC-051,
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
  3. Sweep `LoggerMessage` adoption (CA1848 / CA1873) across hot
     logging paths in tenant + platform hosts; remove from NoWarn.
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

### [TRIAGE] TD-0013 — Advanced health-check probes still missing

- Opened: 2026-04-25
- Owner: TBD
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
  2. Implement `MigrationHeadHealthCheck<TContext>` and register it
     under tag `ready` for both hosts.
  3. Implement `WorkerHeartbeatHealthCheck` reading `worker_heartbeats`
     (rows newer than 30s) and register it on the platform host with
     `failureStatus: HealthStatus.Degraded`.
  4. Implement `EventBusCapacityHealthCheck` against the in-process
     event bus per AD-0006 and register it on the tenant host.
  5. Implement `TenantContextHealthCheck` resolving
     `TABFLOW_TENANT_CODE` against the platform's `tenant_registry`
     and register it on the tenant host.
  6. Add a release-gate smoke check that hits all three endpoints on
     the staging tenant.
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
- Payoff plan: introduce solution folders or project suffixes for the
  four tiers, move existing tests into the correct tier, add a
  static-analysis rule that fails any `Unit`-tiered test importing
  `Npgsql`, `HttpClient`, `System.IO.File`, or `DateTime.Now`.
- Linked: AC-131, AC-132, AC-133, AC-134,
  [`/doc/docs/explanation/concepts/test-taxonomy.md`](/doc/docs/explanation/concepts/test-taxonomy.md)

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
- Payoff plan: author a Roslyn analyzer or adopt one that flags
  non-ASCII identifiers; wire it into `Directory.Build.props`.
- Linked: AD-0015, AC-117, AC-118, AC-119,
  [`/.editorconfig`](/.editorconfig)

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
  1. Drop the empty `InitialCreate` and the orphan `SeedInitialData`
     migration files.
  2. Re-run `dotnet ef migrations add InitialCreate --context
     TenantDbContext --output-dir Migrations/Tenant` so EF Core
     scaffolds a real `Up` body from the model.
  3. If demo seed data is still wanted, add a separate `SeedDemoData`
     migration **after** `InitialCreate` and translate the seed values
     to English (see RR-M1 — the existing seed contains the Turkish
     strings "Yemekler", "İçecekler", "Tatlılar", "Köfte",
     "Lahmacun", and "Dana eti, marul, domates", which is at minimum
     a borderline AD-0015 signal).
  4. Have the worker apply the migration set during `tenant.create`.
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
- Payoff plan: implement `bootstrap-admin` CLI on the platform host as
  specified in AD-0010 and `/doc/docs/how-to/bootstrap-platform.md`.
- Linked: AD-0010, AC-005, AC-006,
  [`/doc/docs/how-to/bootstrap-platform.md`](/doc/docs/how-to/bootstrap-platform.md),
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
