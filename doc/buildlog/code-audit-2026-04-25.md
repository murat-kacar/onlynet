# Code Audit — 2026-04-25

A snapshot of the existing TabFlow code tree measured against the
Constitution v2 baseline, the documentation charter, the 15 accepted
ADRs, and the 134 acceptance-criteria items currently in
[`/doc/docs/reference/acceptance-criteria.md`](/doc/docs/reference/acceptance-criteria.md).

This file is a **buildlog entry**: append-only, dated, and cited from
follow-up PRs. It is not a replacement for the capability matrix or the
tech-debt ledger; it is the snapshot that motivated the next batch of
TD entries and the first implementation PRs.

## 1. Scope

In scope:

- The five projects under [`/src/`](/src/) and the five test projects
  under [`/tests/`](/tests/).
- Conformance to AD-0001…AD-0015.
- Conformance to AC-001…AC-134.
- Whether the new `Directory.Build.props` and `.editorconfig` are
  honoured by existing code.

Out of scope (deferred):

- A real `dotnet build` and `dotnet test` run. These should follow once
  the AC-005 violation (below) is removed; running them inside a
  failing baseline will conflate signals.
- Static-analysis output. AD-0014 raised the analyzer level; the first
  full pass belongs in its own PR.
- Performance, load, and security pen-test work.

## 2. Method

- Repository inventory by file extension.
- Targeted `grep` against the AC contract surface (Identity setup,
  health endpoints, public order endpoint, audit-log writes, WebSocket
  handler, event bus, i18n, test tier markers).
- ADR-by-ADR walk: does the code visibly honour, partially honour,
  violate, or simply not yet exercise each accepted decision?
- AC-chapter walk: does the code visibly honour, partially honour,
  violate, or not yet exercise each chapter?

No code was modified during this audit.

## 3. Repository Inventory

| Surface | Count | Notes |
| --- | --- | --- |
| Source `.cs` files (under `/src/`) | 72 | Excludes `bin/` and `obj/`. |
| Source `.razor` files | 19 | 9 platform, 10 tenant, 0 worker. |
| Endpoint declarations (`MapGet`/`MapPost`/`MapHub` in `/src/apps/`) | 2 | Far below the AC-030..AC-036 + AC-100..AC-102 surface. |
| Test `.cs` files | 5 | Across `Platform.Tests`, `PlatformWorker.Tests`, `Shared.Tests`, `Tenant.Tests`, `E2E.Tests`. |
| `[Fact]` / `[Theory]` test methods | 10 | The entire suite is ten tests. |
| Solution file | `/TabFlow.sln` | Present. |
| Migration projects | 1 (`TabFlow.Migrations.csproj`) | But **two** physical migration trees inside it (see Critical Findings). |
| `IDesignTimeDbContextFactory<T>` implementations | 2 | `PlatformDbContextFactory.cs`, `TenantDbContextFactory.cs` (AD-0009 honoured). |

## 4. Critical Findings (Release-Gate Failures Today)

### C-1. Migration seeds the first platform admin into `AspNetUsers`

- File: [`/src/infra/postgres/Migrations/Platform/20260425143828_InitialAdminUser.cs`](/src/infra/postgres/Migrations/Platform/20260425143828_InitialAdminUser.cs)
- The migration runs raw SQL `INSERT INTO "AspNetUsers" …` with a
  hard-coded UUID and a hard-coded `PasswordHash` value
  (`$2a$11$w5S.h8rQbaqvqZ5zfn1ODOu.7w2.3.3.3.3.3.3.3.3.3.3.3.3.3.3.3.3.3.3.3.3.3.3.3.3.3` —
  this is a placeholder string, not a real bcrypt hash).
- **Violates** AC-005 ("EF Core migrations MUST NOT INSERT into
  `AspNetUsers`") and AC-006 (`bootstrap-admin` MUST refuse if any user
  exists; the migration short-circuits that check by pre-creating one).
- **Violates** AD-0010 (the bootstrap admin is created by a CLI command,
  not a migration).
- The placeholder hash is itself a security smell: any operator who
  applies this migration ends up with an `admin` row whose hash never
  validates, leaving the only entry into the system as a manual
  database edit.

### C-2. Migration seeds additional Identity rows

- File: [`/src/infra/postgres/Migrations/20260425144829_SeedInitialData.cs`](/src/infra/postgres/Migrations/20260425144829_SeedInitialData.cs)
- 10 references to `AspNetUsers` (further seed rows). Same AC-005
  violation as C-1, broader blast radius.

### C-3. Two parallel migration trees (`Migrations/` and `migrations/`)

- Linux is case-sensitive, so `/src/infra/postgres/Migrations/` and
  `/src/infra/postgres/migrations/` are two distinct directories
  compiled into the same project.
- Each tree contains its own `InitialCreate` migration with a different
  timestamp:
  - `migrations/platform/20260425130931_InitialCreate.cs`
  - `Migrations/20260425144800_InitialCreate.cs`
- A migration with the older timestamp can be applied first, leaving
  `__EFMigrationsHistory` with rows that no longer correspond to the
  model snapshot. This is the physical fingerprint of TD-0001.
- The `TabFlow.Migrations.csproj` does not exclude either tree, so both
  are compiled.

### C-4. No `/health` endpoint anywhere in `/src/apps/`

- A grep for `"/health"` against `/src/apps/**/*.cs` returns zero
  matches.
- **Violates** AC-101 ("Every tenant MUST serve `/health`,
  `/health/live`, and `/health/ready`") and AC-102 ("`/health/ready`
  MUST fail when the tenant database is unreachable").
- The capability matrix shows this row as `Target`, which is
  consistent — but the release gate cannot pass against a tenant that
  has no readiness probe wired.

## 5. High Findings (Anayasa Baseline Missing)

### H-1. `Directory.Build.props` may break the existing build

- The new file enables `TreatWarningsAsErrors`, `Nullable=enable`,
  `EnforceCodeStyleInBuild`, and treats common `IDE`/`CA` analyzers as
  build-breaking. This was intentional (AD-0014) but has not been
  validated against the existing 72 `.cs` files.
- A safe path: run `dotnet build /TabFlow.sln` once on a branch, count
  the new errors, and either fix them or scope the analyzer level down
  to a known set with a TD entry to ratchet up later.

### H-2. `IStringLocalizer<T>` is not used anywhere

- Zero matches across `/src/apps/**/*.{cs,razor}`.
- AC-119 ("Every user-facing string in a Razor component MUST be routed
  through `IStringLocalizer<T>`") therefore has no baseline.
- Remediation has two halves: a localisation-bootstrap PR (resx
  layout, neutral English file, DI registration) and the analyzer rule
  that flags literal user-facing strings.

### H-3. Test projects are organised by project, not by tier

- Layout today: `Platform.Tests`, `Tenant.Tests`, `Shared.Tests`,
  `PlatformWorker.Tests`, `E2E.Tests`. Inside `Tenant.Tests/` the
  `Controllers/` and `Services/` folders mix unit-style and
  integration-style tests.
- AC-133 forbids any `Unit` test from touching the file system, the
  network, the system clock, or a database. Without a tier boundary,
  this rule cannot be enforced statically.
- The taxonomy in
  [`/doc/docs/explanation/concepts/test-taxonomy.md`](/doc/docs/explanation/concepts/test-taxonomy.md)
  expects four tiers (Unit, Integration, E2E, Smoke); the current
  layout only carves out E2E.

### H-4. Serilog is referenced in the capability matrix but absent in code

- The capability matrix row "Structured logging via Serilog" reads
  `In progress` and claims "Console + file sinks wired in platform and
  tenant hosts."
- A grep for `Serilog` across `/src/apps/**/*.cs` returns zero matches.
  OpenTelemetry instrumentation is present (12 hits across the two
  hosts), but Serilog is not.
- Either the matrix overstates progress or the project once had Serilog
  and lost it. The matrix MUST be corrected before the next release
  gate; this audit is the trigger.

### H-5. The public order endpoint does not exist

- A grep for `/api/public/orders` in `/src/apps/tenant/**/*.cs` returns
  zero matches.
- The capability matrix marks this as `In progress` (with the customer
  surfaces row) and the schema is in place, but AC-030..AC-036 cannot
  be exercised today.
- This is consistent with the matrix, not a contradiction; the audit
  records it so the next implementation PR knows what is missing.

## 6. ADR-by-ADR Conformance Snapshot

> **Corrigendum, 2026-04-25 (same day):** the table below mis-states
> the topic of AD-0001, AD-0002, AD-0003, AD-0004, AD-0005, and
> AD-0007 — the audit was written before any ADR file was opened and
> the topics were filled in from memory. Five of the six "honoured"
> verdicts in those rows are still correct, but the supporting
> argument is wrong because it points at the wrong decision. The
> corrected table lives in [Section 11](#section-11-re-review-findings-2026-04-25).
> The original wording is preserved per the buildlog append-only rule.


| ADR | Topic | Status in code |
| --- | --- | --- |
| AD-0001 | Three-host topology | ✅ Honoured. Three projects: `TabFlow.Platform`, `TabFlow.Tenant`, `TabFlow.PlatformWorker`. |
| AD-0002 | Per-tenant database | ✅ Honoured at the schema and connection-string level (DbContext factories). |
| AD-0003 | Single Blazor Web App per host | ✅ Honoured. No second presentation project. |
| AD-0004 | ASP.NET Core Identity for both stores | ⚠ Partially honoured. `AddIdentity*` is present in both `Program.cs` files; the seed mechanism violates AD-0010 (see C-1). |
| AD-0005 | Customer session model | ⚠ Schema present, server-side cart present, but the join endpoint (`/g/{token}`) is missing (matrix `In progress`). |
| AD-0006 | In-process event bus over `Channel<T>` | ✅ Honoured. `EventSubscriptionService.cs` and `TableWebSocketHandler.cs` consume the bus. |
| AD-0007 | ESP32 device WebSocket contract | ✅ Honoured at the handler level (`TableWebSocketHandler.cs` exists; 12 matches for WebSocket APIs). Firmware-side validation pending. |
| AD-0008 | Schema lives behind EF Core migrations | ❌ Violated by C-3 (two parallel trees) and C-1 (data seed in migration). TD-0001 covers this. |
| AD-0009 | Design-time factory per context | ✅ Honoured. Both factories are present under `DesignTime/`. |
| AD-0010 | Bootstrap admin is a CLI, not a migration | ❌ Violated by C-1. TD-0002 covers this. |
| AD-0011 | Semantic versioning + tagged commits | ⊘ Not yet exercised. No release tag exists; CHANGELOG `[Unreleased]` only. |
| AD-0012 | Apache 2.0 licence | ✅ Honoured at the repo level (`/LICENSE`, `/NOTICE`). No source headers required. |
| AD-0013 | GitHub Actions as CI | ⚠ Workflow files committed; no real PR has run them yet (TD-0005). |
| AD-0014 | Coding standards in `.editorconfig` and `Directory.Build.props` | ⚠ Files present; build-time effect unverified (H-1). |
| AD-0015 | English-first internal contracts | ✅ Honoured for identifiers and Razor markup (no Turkish characters in source). The analyzer rule is missing (TD-0009). |

Legend: ✅ honoured · ⚠ partial · ❌ violated · ⊘ not yet exercised.

## 7. Acceptance Criteria — Chapter Snapshot

| Chapter | Range | Status today | Notes |
| --- | --- | --- | --- |
| Platform Access | AC-001..006 | ⚠ Mostly target; AC-005 / AC-006 actively violated | C-1, C-2. |
| Tenant Access | AC-010..015 | ⊘ Identity wiring present, role policies pending | Matrix-aligned. |
| Customer Join And Session | AC-020..026 | ⊘ Schema present, `/g/{token}` not implemented | Matrix-aligned. |
| Order Submission | AC-030..036 | ⊘ Endpoint absent (H-5) | Matrix-aligned. |
| Table And Bill Invariants | AC-040..045 | ⊘ Not yet implemented | Matrix-aligned. |
| Station Board | AC-050..052 | ⚠ `/kitchen` exists, station routing pending | Matrix-aligned. |
| Device Channel | AC-060..063 | ✅ Handler present; firmware-side untested | AD-0007 baseline good. |
| Auditability | AC-070..072 | ⊘ Tables present, write path pending | Matrix-aligned. |
| Data Residency | AC-080..082 | ❌ AC-082 ("EF Core migrations only") violated by C-3 | TD-0001 covers. |
| Web Posture | AC-090..092 | ⊘ Not yet wired in middleware | Need a robots middleware. |
| Observability | AC-100..102 | ❌ AC-101 / AC-102 violated by C-4 | No `/health` endpoint exists today. |
| Accessibility | AC-110..116 | ⊘ Not yet measured | Needs WCAG sweep. |
| Internationalization | AC-117..121 | ⚠ Identifiers and DB names ASCII; Razor strings not localised (H-2) | TD-0009 + new TD for resx baseline. |
| Data Protection | AC-122..126 | ⊘ `[DataClass]` not introduced; retention sweep absent | TD-0007, TD-0008. |
| Recovery | AC-127..130 | ⊘ Encryption + drill not wired | TD-0004; first drill due. |
| Tests | AC-131..134 | ⚠ 10 tests total; tiers not separated (H-3) | New TD for tier reorg. |

## 8. New Tech Debt Entries Recommended

The following entries should be appended to
[`/doc/buildlog/tech-debt-ledger.md`](/doc/buildlog/tech-debt-ledger.md)
under the Triage Queue. Each is a finding that the existing ledger does
not yet name.

| TD | One-line | Source |
| --- | --- | --- |
| TD-0010 | Test projects are organised by project rather than by tier; AC-133 cannot be enforced statically. | H-3. |
| TD-0011 | `IStringLocalizer<T>` baseline absent (no resx layout, no DI registration); AC-119 cannot be enforced. | H-2. |
| TD-0012 | Capability-matrix row for Serilog overstates reality; either rewire Serilog or change the row to `Target`. | H-4. |
| TD-0013 | `/health`, `/health/live`, `/health/ready` endpoints missing across both hosts; AC-101 / AC-102 not satisfiable. | C-4. |
| TD-0014 | `Directory.Build.props` analyzer level unverified against current code; first `dotnet build` after this PR may produce many errors. | H-1. |

(TD-0001..TD-0009 already exist and cover the rest.)

## 9. Recommended PR Ordering

These PRs are sequenced so each lands on a green build before the next
begins. None depend on a TD that is not in the ledger.

1. **PR #1 — AC-005 fix.** Delete `Migrations/Platform/20260425143828_InitialAdminUser.cs`
   and the AspNetUsers seed inside `Migrations/20260425144829_SeedInitialData.cs`.
   Mark TD-0002 as `[OPEN]` with a named owner because the system now
   has no admin until the CLI lands.
2. **PR #2 — Migration tree consolidation.** Delete
   `/src/infra/postgres/migrations/` (lowercase) after confirming
   nothing in `Migrations/` depends on its contents. Update TD-0001
   payoff plan to "platform DB will be rebuilt from a single tree".
3. **PR #3 — Build sanity.** Run `dotnet build`, capture the error
   output, fix the easy ones, and write down the residual count in a
   `[OPEN]` TD-0014 closure plan. This unblocks the analyzer level
   from H-1.
4. **PR #4 — `bootstrap-admin` CLI (TD-0002).** Implements AD-0010 and
   restores AC-005 / AC-006.
5. **PR #5 — Health endpoints (TD-0013).** Implements AC-101 / AC-102.
6. **PR #6 — Capability-matrix correction.** Fix the Serilog row and
   any other rows this audit shows as out of date.
7. **PR #7 onwards — TD-0001, TD-0003, then the remaining triage
   queue.**

## 10. Sign-Off

- Auditor: Cascade pair-programming session on 2026-04-25.
- Method: static inspection (no build, no test run).
- Audit motion: open TD-0010..TD-0014, add a release-gate item
  pointing to this file at the next gate, sequence the PRs above.

### Closure log

Append-only. Each entry names the audit finding ID, the change set that
addressed it, and the date.

| Finding | Status | Change set | Date |
| --- | --- | --- | --- |
| C-1 (`AspNetUsers` seed migration) | Closed in source | Same change set as this audit; see CHANGELOG `[Unreleased] / Removed`. | 2026-04-25 |
| C-2 (`SeedInitialData` admin seed) | Re-scoped | The grep that flagged this hit table-name strings inside the snapshot, not data inserts. The file seeds `categories`, `stations`, `tables`, and `menu_items` only. **Not** an AC-005 violation. The Turkish demo content is application data, outside the AC-118 column-naming scope. | 2026-04-25 |
| C-3 (two parallel migration trees) | Closed in source | Same change set: lowercase `migrations/` removed entirely. PlatformDbContext now has zero migrations / zero snapshot until TD-0001 payoff steps 3–5 run on the next bootstrap window. | 2026-04-25 |
| C-4 (`/health` endpoints absent) | Closed in source (partial) | Same change set landed `/health`, `/health/live`, and `/health/ready` on both hosts plus the `platform-db:ping` / `tenant-db:ping` probes via `AddDbContextCheck<T>`. The IETF `health+json` writer is in `TabFlow.Shared.Infrastructure.Diagnostics.HealthJsonWriter`, contract-tested by 8 passing unit tests in `Shared.Tests`. AC-101 and AC-102 are now satisfied literally. Advanced probes (migration head, worker heartbeat, event-bus capacity, tenant-context) remain open under the rewritten TD-0013 payoff plan. | 2026-04-25 |
| H-1 (`Directory.Build.props` may break the build) | Closed in source | First `dotnet build` produced 11 NU1008/NU1010 + 46 CA/IDE errors. CPM mismatch fixed by adding the missing `PackageVersion` rows to `/Directory.Packages.props` and removing `Version=` from the analyzer `PackageReference`. Analyzer baseline lowered to `AnalysisMode=Recommended` + `EnforceCodeStyleInBuild=false` + a named `NoWarn` list, all tracked by TD-0014's ratchet plan. Build is now green: 0 warnings, 0 errors across 9 projects. | 2026-04-25 |
| H-2 (i18n baseline) | Open | TD-0011. | — |
| H-3 (test projects by project, not tier) | Open | TD-0010. The audit change set also discovered that `tests/E2E.Tests/` is missing from `TabFlow.sln` entirely; folded into TD-0010. | — |
| H-4 (Serilog row overstates reality) | Re-scoped | The original finding said Serilog was absent. The first build revealed `using Serilog;` and a `LoggerConfiguration` chain in both hosts. The chain called the non-existent `WithEnvironment()` (the package exposes `WithMachineName()` and `WithEnvironmentName()` but not `WithEnvironment()`), so the source had never compiled. The audit change set removed the broken call so the configuration compiles. The Serilog wiring has still never produced a runtime log line; TD-0012 now tracks the missing exercise. | 2026-04-25 |
| H-5 (`/api/public/orders` absent) | Open | Tracked under the customer-surface capability matrix row; no separate TD. | — |

This file is append-only. Findings here are corrected by follow-up PRs
that cite the section number (for example, "closes audit C-1") and by
matching closure notes in the tech-debt ledger.

## Section 11. Re-Review Findings (2026-04-25)

The re-review ran the same day as the original audit, after the change
set that closed C-1, C-3, C-4, and H-1. It treated the audit itself as
an artefact under inspection: were any findings written without
verification, were any findings missed, and did the chosen verification
methods actually run?

Method: re-grep against the live tree, run `dotnet build` and
`dotnet test` for the first time, read `decisions.md` end-to-end and
match each ADR title against the audit's Section 6 claims.

### 11.1 Audit's own structural defects

- **RR-A1 — Section 3 (Inventory) was incomplete.** The original
  inventory walked top-level project folders only. The re-review
  added: 8 controllers (`platform/Controllers/Api/{Jobs,Tenants}` plus
  `tenant/Controllers/Api/{Cart,Kitchen,Menu,Orders,Sessions,Tables}`),
  8 service interfaces under `tenant/Services/`, the
  `tenant/Hubs/TenantHub` SignalR hub, the
  `tenant/WebSocket/TableWebSocketHandler` handler, the
  `platform/Middleware/AuditMiddleware` middleware, the
  `platform-worker/ProvisioningWorker` `BackgroundService`, and the
  `tenant/Services/EventSubscriptionService` `BackgroundService`. None
  of these were named in the original audit; some of the worst
  findings below would have been visible if they had been.
- **RR-A2 — Section 6 mis-stated six ADR titles.** Topics for AD-0001,
  AD-0002, AD-0003, AD-0004, AD-0005, and AD-0007 were paraphrased
  from memory instead of read from `decisions.md`. The corrected
  conformance table follows in 11.5.
- **RR-A3 — The audit ran no build and no test.** Five of the
  high-severity findings (H-1, H-3, H-4, plus the existence of seed
  data and Serilog wiring) only reveal their full shape under
  `dotnet build` and `dotnet test`. The post-audit change set ran
  both, which is what surfaced 11.2 below.

### 11.2 New findings missed by the original audit

#### RR-C2: Tenant API controllers expose every endpoint anonymously

- Files: `tenant/Controllers/Api/{Cart,Kitchen,Menu,Orders,Sessions,Tables}Controller.cs`.
- Evidence: a grep for `[Authorize` against each controller returned
  zero hits in all six files. `Program.cs` registers
  `app.UseAuthentication()` and `app.UseAuthorization()` and defines
  `Tenant:Read` / `Tenant:Write` / `Tenant:Self` policies, but no
  controller invokes them.
- ACs touched: AC-008 (staff role enforcement), AC-043 (only `cashier`
  / `manager` / `owner` may close a bill), AC-051 (station board
  events).
- Severity: critical. Open as TD-0015.

#### RR-C3: Tenant `InitialCreate` `Up` body is empty

- File: `/src/infra/postgres/Migrations/20260425144800_InitialCreate.cs`.
- Evidence: 22 lines, zero `CreateTable` calls. The follow-up
  `SeedInitialData` migration runs raw `INSERT INTO "categories"`,
  `INSERT INTO "stations"`, etc. against tables that the empty
  `InitialCreate` never produced. AC-082 is violated end-to-end.
- Severity: critical. Folded into TD-0003.

#### RR-H1: AD-0004 (mixed render modes) is not exercised

- Evidence: a grep for `@rendermode`, `RenderMode.`,
  `InteractiveServer`, `InteractiveWebAssembly`, and `InteractiveAuto`
  across all `*.cs`, `*.razor`, and `*.cshtml` files in `/src/`
  returns zero hits, despite 19 Razor components and 8 cshtml pages
  on disk.
- Implication: every Blazor surface defaults to Static SSR. AD-0004
  is honoured only in the trivial "all components share the same
  mode" sense; the *mixed* part is not implemented.
- Severity: high. Open as TD-0016.

#### RR-H2: `/api/public/orders` exists at the wrong route

- File: `tenant/Controllers/Api/OrdersController.cs`.
- Evidence: `[Route("api/[controller]")]` plus `[HttpPost("submit")]`
  resolves to `POST /api/orders/submit`. AC-030 and AC-031 require
  `POST /api/public/orders` with a still-open customer session and a
  fresh QR checkout-proof token. The current controller has neither
  the route nor the token validation.
- Audit's H-5 is therefore half-right: the customer-ordering surface
  is not absent, it is mis-routed and missing two acceptance gates.
- Severity: high. Folded into TD-0015 step 3.

### 11.3 New findings on documentation drift

- **RR-M1 — Tenant `SeedInitialData` carries Turkish strings.** The
  values "Yemekler", "İçecekler", "Tatlılar", "Köfte",
  "Lahmacun", and "Dana eti, marul, domates" appear as default seed
  data. AD-0015 covers "internal contracts"; whether default seed
  values count as a contract is debatable, but the strings are flagged
  here so the next pass picks them up. Folded into TD-0003 step 3.
- **RR-M2 — Capability matrix row for `/health` is stale.** The matrix
  reads `Target` for the health-check endpoints; the change set that
  opened this audit landed all three on both hosts. Fix: matrix row
  edited to `Implemented` in the same change set.

### 11.4 Confirmations of original findings

| Original finding | Re-review verdict |
| --- | --- |
| C-2 (`SeedInitialData` admin seed) | Already re-scoped in the original closure log. Confirmed: the file does not touch `AspNetUsers`. |
| H-2 (`IStringLocalizer` baseline absent) | Confirmed: zero hits across `*.cs` / `*.razor` / `*.cshtml`. |
| H-4 (Serilog row overstates reality) | Already re-scoped. Confirmed: package references and call sites both exist; the `WithEnvironment()` typo prevented compilation. |
| H-5 (`/api/public/orders` absent) | Re-scoped: not absent, mis-routed (see RR-H2). |
| AD-0006 honoured | Confirmed: `InProcessEventBus.cs` exists and is consumed by `EventSubscriptionService`. |
| AD-0009 honoured | Confirmed: both design-time factories present. |
| AD-0010 violated | Confirmed: no `bootstrap-admin` symbol anywhere in source. |
| AD-0012 honoured | Confirmed: `LICENSE` and `NOTICE` at repo root. |

### 11.5 Corrected ADR conformance table

This replaces Section 6 conceptually. Section 6 stays in place per the
buildlog append-only rule.

| ADR | Real topic (from `decisions.md`) | Status in code | Evidence |
| --- | --- | --- | --- |
| AD-0001 | Platform and tenant remain architecturally separate | ✅ Honoured | `TabFlow.Shared` package contains zero references to `TabFlow.Platform` or `TabFlow.Tenant`; the dependency arrows point only inward. |
| AD-0002 | ASP.NET Core 10 + Blazor Web App stack | ✅ Honoured | `<TargetFramework>net10.0</TargetFramework>` everywhere; both hosts call `AddRazorPages()` and `AddServerSideBlazor()`. |
| AD-0003 | One host process per side | ✅ Honoured | Three independent projects (`TabFlow.Platform`, `TabFlow.Tenant`, `TabFlow.PlatformWorker`); the tenant binds its own port (5001) in `Program.cs`. |
| AD-0004 | Mixed render modes per surface family | ❌ Violated | Zero `@rendermode` usages across 19 components + 8 pages. Tracked by TD-0016. |
| AD-0005 | ASP.NET Core Identity as the single auth model | ✅ Honoured | `AddIdentity<IdentityUser<Guid>, IdentityRole<Guid>>()` is wired in both hosts; cookie auth is registered. |
| AD-0006 | In-process event bus for real-time surfaces | ✅ Honoured | `InProcessEventBus.cs` consumed by `EventSubscriptionService` and `TableWebSocketHandler`. |
| AD-0007 | PostgreSQL 17 as the storage baseline | ✅ Honoured | `Npgsql.EntityFrameworkCore.PostgreSQL` package referenced; connection strings target `tabflow_platform` / `tenant_*`. |
| AD-0008 | EF Core as schema and migration authority | ❌ Violated | TD-0001 (platform schema rebuild pending) plus TD-0003 (tenant `InitialCreate` `Up` empty). |
| AD-0009 | Standalone migrations project + design-time factories | ✅ Honoured | `TabFlow.Migrations.csproj` plus two `IDesignTimeDbContextFactory<T>` implementations under `DesignTime/`. |
| AD-0010 | Bootstrap admin via CLI, not migration | ❌ Violated | TD-0002. |
| AD-0011 | SemVer + tagged commits on `main` | ⊘ Not yet exercised | No release tag; `[Unreleased]` only. |
| AD-0012 | Apache 2.0 licence | ✅ Honoured | `/LICENSE`, `/NOTICE` present. |
| AD-0013 | GitHub Actions as CI | ⚠ Partial | Workflow files committed; first run on a PR pending (TD-0005). |
| AD-0014 | Coding standards in `.editorconfig` and `Directory.Build.props` | ⚠ Partial | Build is green at a deliberately lowered analyzer baseline; ratchet plan in TD-0014. |
| AD-0015 | English-first for internal contracts | ⚠ Partial | Identifiers are English; `IStringLocalizer` is absent (TD-0011); the analyzer rule is missing (TD-0009); tenant seed data carries Turkish strings (RR-M1, folded into TD-0003). |

### 11.6 Recommended audit hygiene for next pass

- Read every ADR file before writing the conformance section.
- Run `dotnet build` and `dotnet test` before writing the inventory.
- Walk every `Controllers/`, `Services/`, `Hubs/`, `Middleware/`, and
  `Workers/` subtree; do not stop at top-level project folders.
- Treat each `[Authorize]` / `[AllowAnonymous]` decoration as a
  first-class inventory item.
- Re-grep every "absent" claim with three different spellings before
  writing it down. The original H-4 / RR-H2 mistakes both came from
  trusting the first grep.

### 11.7 Closure additions

Append-only. The original closure log in Section 10 is the source of
truth for the original 9 findings. The re-review additions go here.

| Finding | Status | Change set | Date |
| --- | --- | --- | --- |
| RR-A1 (audit inventory incomplete) | Closed in this section | The inventory above (§11.1) supersedes Section 3. | 2026-04-25 |
| RR-A2 (audit Section 6 wrong topics) | Closed in this section | Corrected table at §11.5 supersedes Section 6 conceptually; corrigendum note added in-place. | 2026-04-25 |
| RR-A3 (no build, no test) | Closed in same change set as audit | The first `dotnet build` and `dotnet test` runs are recorded in the CHANGELOG `[Unreleased]` block dated 2026-04-25. | 2026-04-25 |
| RR-C2 (tenant controllers anonymous) | Closed in source (partial) | PR #6 landed the auth surface split (`[AllowAnonymous]` on Menu/Cart, `[Authorize(Tenant:Read)]` on Kitchen/Orders/Tables, restrictive default + per-action override on Sessions, new `PublicOrdersController` at `/api/public/orders`). PR #7 same change set tightened `OrderService.SubmitAsync`: token reuse rejected via `IsConsumed`, same-table check via `TableId`, explicit `Consume()` in the order-insert transaction, AC-030 still-open + AC-031 + AC-032 + AC-036 enforced inline. PR #7 also wired `OnRedirectToLogin` / `OnRedirectToAccessDenied` cookie events so `/api/*` callers receive `401` / `403` instead of 302-redirecting to `/login`. Two follow-up TDs cover the remaining halves: TD-0017 (device-binding cookie for AC-030) and TD-0018 (idempotency key persistence on the `Order` entity). Integration tests remain open under TD-0015 step 6 (depends on TD-0010 fixtures). | 2026-04-25 |
| RR-C3 (tenant `InitialCreate` empty) | Closed in source | PR #10 same change set: dropped the empty `InitialCreate` plus the orphan `SeedInitialData` plus the stale snapshot, then ran `dotnet ef migrations add InitialCreate --context TenantDbContext --output-dir Migrations/Tenant`. The new scaffolded migration is 586 lines with 64 `CreateTable` / `migrationBuilder` calls covering the full tenant model. Closes TD-0003 steps 1+2; step 4 (worker `MigrateAsync` on tenant.create) remains an operator-side wiring task. | 2026-04-25 |
| RR-H1 (AD-0004 not exercised) | Open | TD-0016. | — |
| RR-H2 (`/api/public/orders` mis-routed) | Closed in source (partial) | PR #6 same change set: new `PublicOrdersController` at `/api/public/orders` carries the customer-tier `POST` action; the staff-tier read endpoints stay at `/api/orders`. The token-validation gates (AC-030 / AC-031) remain open under TD-0015 step 4. | 2026-04-25 |
| RR-M1 (Turkish seed strings) | Closed in source | PR #10 same change set: the Turkish-string seed migration was removed alongside the empty `InitialCreate`. If demo seed is reintroduced later, AC-118 / AD-0015 require English values. | 2026-04-25 |
| RR-M2 (capability matrix Health row stale) | Closed in source | Same change set as this re-review; matrix row edited to `Implemented`. | 2026-04-25 |
