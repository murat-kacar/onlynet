# Changelog

All notable changes to TabFlow are recorded here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and the project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

Each release tag on `main` corresponds to one section below. Unreleased
work accumulates under `[Unreleased]` and is moved into a versioned
section when a release tag is cut.

## Versioning Rule

- **MAJOR** — a breaking change to a stable contract: HTTP route, event
  payload shape, DB column, config key, or accepted ADR with
  `Status: Superseded`.
- **MINOR** — additive, non-breaking changes: new capability, new
  endpoint, new optional field.
- **PATCH** — bug fixes, internal refactors, doc updates that do not
  touch a contract.

Pre-1.0.0 the public surface is considered unstable; minor bumps may
carry breaking changes if explicitly noted under a `### Breaking`
heading.

Detailed rules live in
[`doc/docs/reference/architecture/decisions.md`](./doc/docs/reference/architecture/decisions.md)
AD-0011.

## [Unreleased]

### Added

- Project constitution
  ([`doc/docs/constitution.md`](./doc/docs/constitution.md)) and
  documentation charter
  ([`doc/docs/meta/documentation-charter.md`](./doc/docs/meta/documentation-charter.md)).
- Tech debt ledger
  ([`doc/buildlog/tech-debt-ledger.md`](./doc/buildlog/tech-debt-ledger.md))
  with `[TRIAGE] [OPEN] [CLOSED] [ACCEPTED] [ABANDONED]` status flow,
  seeded with TD-0001…TD-0009 covering schema rebuild, bootstrap CLI,
  tenant migrations, backup encryption, CI validation, branch
  protection, data classification, retention sweeps, and English-first
  lint.
- Pull-request review policy
  ([`doc/docs/meta/review-policy.md`](./doc/docs/meta/review-policy.md))
  and an expanded release gate
  ([`doc/docs/meta/release-gate.md`](./doc/docs/meta/release-gate.md))
  with disaster-recovery, data-protection, test-coverage, DORA, and
  tech-debt-triage sections.
- Apache 2.0 license, NOTICE file, Contributor Covenant Code of Conduct,
  and security disclosure policy at the repository root.
- Internal API reference
  ([`doc/docs/reference/api/internal-api.md`](./doc/docs/reference/api/internal-api.md))
  and health-check endpoint reference
  ([`doc/docs/reference/architecture/health-checks.md`](./doc/docs/reference/architecture/health-checks.md)).
- ADRs AD-0011 (semantic versioning), AD-0012 (Apache 2.0 licence),
  AD-0013 (GitHub Actions CI), AD-0014 (`.editorconfig` +
  `Directory.Build.props` for coding standards), and AD-0015
  (English-first internal contracts).
- Concept explainers for internationalization
  ([`doc/docs/explanation/concepts/internationalization.md`](./doc/docs/explanation/concepts/internationalization.md)),
  data protection — KVKK + GDPR
  ([`doc/docs/explanation/concepts/data-protection.md`](./doc/docs/explanation/concepts/data-protection.md)),
  and the four-tier test taxonomy
  ([`doc/docs/explanation/concepts/test-taxonomy.md`](./doc/docs/explanation/concepts/test-taxonomy.md)).
- Recovery objectives (RTO/RPO) section in
  [`doc/docs/reference/architecture/slos.md`](./doc/docs/reference/architecture/slos.md)
  and an encryption + off-site + quarterly drill section in
  [`doc/docs/how-to/backup-and-restore.md`](./doc/docs/how-to/backup-and-restore.md).
- Acceptance criteria AC-117…AC-134 covering internationalization,
  data protection, recovery, and tests.
- GitHub repository configuration: `CODEOWNERS`,
  `pull_request_template.md`, three issue templates (bug, feature,
  tech-debt), and CI workflows (`pr.yml`, `main.yml`, `release.yml`).
- `.editorconfig` and `Directory.Build.props` enforcing the AD-0014
  coding-standards baseline (analyzer level, nullable, warnings as
  errors, treat `IDE`/`CA` rules as build-breaking).

### Changed

- Documentation tree consolidated under a single `doc/` umbrella with
  four trees (`docs/`, `userdocs/`, `apidocs/`, `buildlog/`); the
  earlier `templatedocs/` tree was retired and its content migrated.
- ADR status taxonomy formalised: `Proposed`, `Accepted`, `Rejected`,
  `Deprecated`, `Superseded` (per the documentation charter).
- Capability matrix `Implemented` definition tightened to require all
  three of *tested*, *observable*, and *documented* per constitution
  II.4. New capability rows for encrypted backup, recovery drill,
  data classification, retention sweeps, i18n, English-first lint,
  GitHub Actions, and branch protection.
- Glossary expanded with KVKK, GDPR, RTO, RPO, Recovery Drill, Data
  Class, Data Controller, Data Processor, Test Tier, and English-First
  entries.

### Security

- **Order idempotency key persisted with a unique index (TD-0018).**
  `Order` now carries an `IdempotencyKey` column with
  `[Index(nameof(SessionId), nameof(IdempotencyKey), IsUnique = true)]`;
  a backing migration `AddOrderIdempotencyKey` adds the column and the
  unique index. `OrderService.SubmitAsync` performs an early lookup on
  `(SessionId, IdempotencyKey)`; if a previous order matches, the
  service returns the original `SubmitOrderResult` instead of inserting
  a second one. Duplicate `POST /api/public/orders` calls (e.g. a
  customer tapping Submit twice on a flaky network) now collapse to a
  single order row. Closes TD-0018 steps 1–2 in source; step 3
  (integration test) remains open and depends on TD-0010 fixtures.
- **Customer session device-binding enforced (TD-0017, AC-030 second
  half).** A successful `POST /api/sessions/open` now sets an
  HttpOnly cookie named `tabflow_session_device` carrying an opaque
  server-issued GUID. The cookie value is also persisted on the new
  `CustomerAccessTicket.DeviceCookieValue` column (migration
  `AddCustomerAccessTicketDeviceCookie`). `PublicOrdersController.SubmitOrder`
  reads the cookie and forwards it to `OrderService.SubmitAsync`,
  which looks up the ticket, verifies the ticket is still valid and
  belongs to the requested session, and constant-time-compares the
  persisted cookie value against the one the browser presented
  (`CryptographicOperations.FixedTimeEquals`). A missing cookie
  yields `403`; a mismatched cookie aborts the submit. The binding is
  per-ticket so that multiple customer devices on the same table
  session each carry their own device secret. Closes TD-0017 steps
  1–3 in source; step 4 (integration test) remains open and depends
  on TD-0010 fixtures.
- **Customer order submission gates tightened.** `OrderService.SubmitAsync`
  now enforces three previously-missing halves of AC-030..AC-036 in the
  same `SaveChangesAsync` transaction as the order insert:
  - The checkout-proof token query rejects already-consumed tokens
    (`IsConsumed == false`), tokens from other tables (`TableId ==
    request.TableId`), and uses the entity's `IsExpired` property so
    the comparison is always `DateTimeOffset` against
    `DateTimeOffset.UtcNow` (the previous code mixed `DateTime`).
  - On success the token is consumed via `checkoutToken.Consume()`
    before `SaveChangesAsync`, which closes AC-032 (a checkout proof
    cannot be reused).
  - The originating session is closed and the token is consumed in
    the same `SaveChangesAsync` so a duplicate submit cannot race
    through between validation read and order write.
  Two follow-up TDs opened for the remaining halves:
  TD-0017 (device-binding cookie for AC-030) and TD-0018
  (idempotency key persistence on the `Order` entity).
- **Cookie-auth challenge returns 401/403 for API paths.** Both hosts
  now short-circuit `OnRedirectToLogin` and `OnRedirectToAccessDenied`
  for any request whose path starts with `/api/`, returning the
  status code an AJAX caller expects. HTML routes continue to redirect
  to `LoginPath` / `AccessDeniedPath`. Closes TD-0015 step 5.
- **Tenant API controllers now fail closed.** All six tenant API
  controllers were anonymous before this change. After this change:
  - `KitchenController`, `OrdersController`, `TablesController` carry
    `[Authorize(Policy = "Tenant:Read")]` at the controller level;
    `Kitchen.UpdateItemStatus` and the previous `Orders.SubmitOrder`
    raise to `Tenant:Write` where appropriate.
  - `MenuController` and `CartController` carry an explicit
    `[AllowAnonymous]` at the controller level (customer-facing).
  - `SessionsController` keeps a restrictive default
    (`Tenant:Read`); the customer actions `Open` and `GetSessionState`
    opt out with `[AllowAnonymous]`; `CloseSession` raises to
    `Tenant:Write` per AC-043. The default-restrictive ordering avoids
    the ASP0026 trap.
  - New `PublicOrdersController` at `/api/public/orders` carries the
    customer-tier `POST` action that AC-030 and AC-031 require to be
    routed at that path. The token-validation half of those ACs is
    enforced inside `IOrderService.SubmitAsync` and tracked under
    TD-0015 step 4.
  Closes audit re-review finding RR-C2 in source (partial); closes
  the routing half of RR-H2.

### Documentation

- `/doc/buildlog/code-audit-2026-04-25.md` extended with **Section 11
  (Re-Review Findings)**. The re-review treated the original audit as
  an artefact under inspection and recorded:
  - Three structural defects in the audit itself (incomplete
    inventory, wrong ADR topics in Section 6, no build / test had been
    run).
  - Two new critical findings (RR-C2: tenant API controllers
    anonymous; RR-C3: tenant `InitialCreate` `Up` empty).
  - Two new high findings (RR-H1: AD-0004 not exercised; RR-H2:
    `/api/public/orders` mis-routed inside `OrdersController`).
  - Two new medium findings (RR-M1: tenant seed data carries Turkish
    strings; RR-M2: capability matrix Health row stale).
  - A corrected ADR conformance table that supersedes Section 6
    conceptually (Section 6 stays in place per the append-only rule).
  - Recommended audit hygiene for the next pass.
- `/doc/buildlog/tech-debt-ledger.md`:
  - Opened **TD-0015** — tenant API controllers expose every endpoint
    anonymously (highest-priority security gap currently in the
    ledger).
  - Opened **TD-0016** — AD-0004 mixed render modes never exercised.
  - Rewrote **TD-0003** payoff plan with the empty-`Up` finding and
    the Turkish-seed flag.
- `/doc/docs/reference/architecture/capability-matrix.md` — Health
  check endpoints row promoted from `Target` to `Implemented` and
  evidence list expanded.

### CLI

- **`bootstrap-admin` command on the platform host (AD-0010).** The
  platform `Program.cs` now dispatches to `TabFlow.Platform.Cli.BootstrapAdminCommand`
  before the web host starts when `args[0] == "bootstrap-admin"`. The
  command refuses to run on a populated `AspNetUsers` table, generates
  a CSPRNG-backed 24-character password, calls
  `UserManager.CreateAsync` so the hash uses Identity's current hasher,
  ensures the `owner` role exists and assigns it, writes an
  `auth.bootstrap` row to `platform_audit_log`, and prints the
  generated password to stdout exactly once. Closes TD-0002 step 1 in
  source. The operator-action half (running the command on a fresh
  deployment per `/doc/docs/how-to/bootstrap-platform.md`) and the
  force-redirect-through-`/change-password` follow-up remain pending.
  Smoke check: `dotnet run --project src/apps/platform/TabFlow.Platform.csproj --no-build -- bootstrap-admin`
  prints `usage: bootstrap-admin --email <address>` and returns
  without starting the web host.

### Tools

- **`LoggerMessage` adoption across hot logging paths (TD-0014 step 3).**
  All 19 `ILogger.LogX(...)` call sites in `/src/apps/{tenant,platform-worker}/**/*.cs`
  were rewritten as `[LoggerMessage]` source-generated extension
  methods. Two new files carry the definitions:
  - `src/apps/platform-worker/PlatformWorkerLogMessages.cs` —
    EventIds 1–5 (provisioning worker)
  - `src/apps/tenant/TenantLogMessages.cs` — EventIds 101–109
    (`EventSubscriptionService`) and 201–208
    (`TableWebSocketHandler`)
  Each method is an extension on `ILogger` so call sites read as
  `_logger.OrderSubmitted(orderId, tableId)` instead of
  `_logger.LogInformation("Order submitted: {OrderId} for table
  {TableId}", orderId, tableId)`. EventIds are stable across builds
  so log search by ID stays meaningful.
  - `Directory.Build.props`: `CA1848` and `CA1873` removed from
    `NoWarn`. The TD-0014 ratchet plan now stands at five remaining
    NoWarn entries (`CS1591`, `CA1716`, `CA1305`, `CA1304`, `CA1311`,
    `CA1822`, `CA1816`, `CA1707`).
  - Build status: 0 errors, 0 warnings; Shared.Tests Unit tier still
    8/8 passing.
- **First-party Roslyn analyser `TF0001` enforces AD-0015 (TD-0009).**
  New project at `/tools/analyzers/TabFlow.Analyzers/` (netstandard2.0,
  consumes `Microsoft.CodeAnalysis.CSharp` 4.13.0) ships
  `EnglishFirstIdentifierAnalyzer`: any identifier whose name
  contains a code unit greater than `0x7F` raises a build-time
  warning, which `TreatWarningsAsErrors` promotes to a hard error.
  Compiler-generated names (those starting with `<` or `$`) are
  skipped so backing fields and lambda closures do not false-fire.
  `Directory.Build.props` wires the analyser into every consumer
  project via an `OutputItemType="Analyzer"` ProjectReference,
  guarded by a `'$(MSBuildProjectName)' != 'TabFlow.Analyzers'`
  condition that prevents the analyser project from referencing
  itself. Smoke-checked: a temporary file with a Turkish identifier
  produced two `error TF0001` lines (one for the property, one for
  its compiler-generated getter) and the build returned to 0/0
  after the file was removed. Closes TD-0009 steps 1–3 in source;
  step 4 (Microsoft.CodeAnalysis.Testing xUnit suite) and step 5
  (`AnalyzerReleases.{Shipped,Unshipped}.md` per RS2008) remain
  open.

### Tests

- **Test taxonomy enforced via xUnit `[Trait("Category", T)]`
  (TD-0010).** Every test class now carries a class-level trait that
  routes it into one of the four tiers documented at
  `/doc/docs/explanation/concepts/test-taxonomy.md#xunit-trait-convention`:
  `Unit`, `Integration`, `E2E`, `Smoke`. Existing tests classified:
  `HealthJsonWriterTests` → Unit (8 tests, all passing);
  `CartServiceTests`, `CustomerSessionServiceTests`,
  `OrdersControllerTests` → Integration (PostgreSQL-bound today);
  `PlatformE2ETests`, `TenantE2ETests` → E2E.
- **`tests/E2E.Tests/` is now part of `TabFlow.sln`.** It was
  silently absent from every CI build until this commit; the
  re-review's RR-A1 / TD-0010 follow-up flagged it. `Microsoft.Playwright`
  1.49.0 added to `Directory.Packages.props`; legacy `Playwright`
  package reference removed. Both hosts now expose `public partial
  class Program {}` so the E2E project can resolve the dual-host
  collision via `extern alias PlatformHost` / `extern alias TenantHost`
  and reference `PlatformHost::Program` / `TenantHost::Program`
  explicitly (CS0433 fix).
- **PR workflow runs Unit and Integration tiers separately.**
  `.github/workflows/pr.yml` splits `dotnet test` into a Unit
  fast-path (no PostgreSQL) and an Integration step (with PostgreSQL
  service container); a broken unit test cancels the whole job
  before the slower step pays for the DB fixture. The E2E tier is
  intentionally excluded from the PR workflow until a browser
  bootstrap step lands.
- **CA1001 NoWarn scoped to test projects** so xUnit's
  `IAsyncLifetime` disposal pattern stops tripping the analyser
  ("type owns disposable fields but is not IDisposable" — false
  positive for any test class that owns a `WebApplicationFactory`
  through `IAsyncLifetime`).

### Schema

- **Tenant `InitialCreate` migration regenerated, Turkish seed
  removed.** The previous Tenant migration tree contained two files:
  `20260425144800_InitialCreate.cs` had an empty `Up` body
  (zero `CreateTable` calls — RR-C3) and `20260425144829_SeedInitialData.cs`
  ran raw `INSERT INTO "categories"` etc. against tables that the
  empty `InitialCreate` never produced (RR-M1 also flagged that the
  seed values were Turkish: "Yemekler", "İçecekler", "Tatlılar",
  "Köfte", "Lahmacun", "Dana eti, marul, domates"). All four files
  plus the stale snapshot were dropped, then `dotnet ef migrations add
  InitialCreate --project src/infra/postgres/TabFlow.Migrations.csproj
  --context TenantDbContext --output-dir Migrations/Tenant`. The new
  scaffolded migration is 586 lines with 64 `CreateTable` /
  `migrationBuilder` calls covering Identity + customer-session +
  QR-token + table + station + menu + cart + order + bill + audit-log
  tables. The tenant database now starts empty by design; reintroducing
  demo seed data is a deliberate future decision and any reintroduced
  values MUST be English (AD-0015, AC-118). Closes TD-0003 steps 1
  and 2 in source; step 3 (English demo seed) is deferred; step 4
  (worker `MigrateAsync` on `tenant.create`) is an operator-side wiring
  task. Also closes audit re-review finding RR-C3 in source and the
  RR-M1 Turkish-string flag.
- **Platform `InitialCreate` migration regenerated.** Ran
  `dotnet ef migrations add InitialCreate --project
  src/infra/postgres/TabFlow.Migrations.csproj --context
  PlatformDbContext --output-dir Migrations/Platform`. The previous
  Platform migration tree was deleted in the same audit change set
  that opened TD-0001; PlatformDbContext was therefore migrationless
  on disk. The new scaffolded migration is 361 lines with 38
  `CreateTable` / `migrationBuilder` calls covering the full Identity
  + tenant-registry model. The accompanying
  `PlatformDbContextModelSnapshot.cs` is regenerated so the next
  `dotnet ef migrations add` produces a correct delta. Closes
  TD-0001 step 3 in source. Steps 2, 4, 5 are operator actions
  (drop the existing `tabflow_platform` database, run `database
  update`, verify `__ef_migrations_history` matches the snapshot)
  and remain pending.

### Added

- `/health`, `/health/live`, and `/health/ready` endpoints on both
  the platform host and the tenant host (AC-101). `/health/ready`
  fails (HTTP `503`, `status: "fail"`) when the host's DbContext
  cannot reach its PostgreSQL database (AC-102), via the
  `platform-db:ping` and `tenant-db:ping` probes registered with
  `AddDbContextCheck<T>(...)`. All three endpoints are
  `[AllowAnonymous]`, return `application/health+json` per the IETF
  draft, and serialise via the new shared
  `TabFlow.Shared.Infrastructure.Diagnostics.HealthJsonWriter`.
- `TabFlow.Shared.Infrastructure.Diagnostics.HealthJsonWriter` —
  shared response writer that renders `Microsoft.Extensions.Diagnostics.HealthChecks.HealthReport`
  in the IETF `health+json` shape so both hosts produce
  byte-equivalent bodies.
- 8 unit tests in `Shared.Tests/Infrastructure/Diagnostics/HealthJsonWriterTests.cs`
  covering status-string mapping, content-type, component-type
  classification, observed duration, and version/release-id presence.
  This is the first passing test in the `Shared.Tests` project.

### Fixed

- `Serilog.Enrichers.Environment` `WithEnvironment()` call in
  `src/apps/{platform,tenant}/Program.cs`. The method does not exist in
  the 3.x package; the call site had never compiled. Removed so the
  Serilog configuration now compiles. `WithMachineName()` provides
  equivalent host context. Runtime log emission is still untested
  (TD-0012).
- `Directory.Build.props` analyzer baseline calibrated against the
  existing code. `AnalysisMode` lowered from `All` to `Recommended`,
  `EnforceCodeStyleInBuild` set to `false`, and `NoWarn` extended with
  the named CA / CS rules listed in the file. AD-0014's strict
  baseline is restored by TD-0014's seven-step ratchet plan.
- `Directory.Packages.props` extended with the previously-missing
  `PackageVersion` rows for `Serilog.AspNetCore`, `Serilog.Sinks.Console`,
  `Serilog.Sinks.File`, `Serilog.Enrichers.Environment`, and
  `Microsoft.CodeAnalysis.Analyzers`. Central Package Management is now
  consistent: `dotnet build TabFlow.sln` reports 0 warnings, 0 errors
  across 9 projects.

### Removed

- `templatedocs/` tree and all its files (content migrated to `docs/`
  or absorbed into the tech debt ledger).
- Migration `Migrations/Platform/20260425143828_InitialAdminUser.cs`
  (and its Designer + the orphan `PlatformDbContextModelSnapshot.cs`).
  The migration ran a raw SQL `INSERT INTO "AspNetUsers"` with a
  hard-coded placeholder hash, violating AC-005 and AD-0010. The
  platform now has no seeded admin until the `bootstrap-admin` CLI
  lands (TD-0002).
- The parallel lowercase `src/infra/postgres/migrations/` tree
  (`InitialCreate` and `AddTenantDatabaseConnection` for
  `PlatformDbContext`). Two case-different migration directories were
  compiled into the same project; the lowercase tree was the older of
  the two and lacked a model snapshot. Platform schema now has zero
  migrations in source and will be rebuilt by step 3 of TD-0001's
  payoff plan on the next bootstrap window.

## Release Procedure

1. Ensure `[Unreleased]` accurately reflects every change since the
   previous release tag.
2. Run the release gate
   ([`doc/docs/meta/release-gate.md`](./doc/docs/meta/release-gate.md)).
3. Rename the `[Unreleased]` heading to `[<MAJOR>.<MINOR>.<PATCH>] -
   <YYYY-MM-DD>` and open a fresh empty `[Unreleased]` block above it.
4. Tag the release commit with `v<MAJOR>.<MINOR>.<PATCH>`.
5. Publish the release notes by copying the new section into the GitHub
   release page.
