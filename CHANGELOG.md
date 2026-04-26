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

- **Bootstrap admin forced through `/change-password` on first sign-in
  (TD-0002 step 3).** A new `PasswordChangeRequiredMiddleware`,
  registered in the platform `Program.cs` after `UseAuthorization`
  and before route mapping, redirects any authenticated principal
  carrying the `tabflow:must_change_password` claim to
  `/change-password` unless the request path falls in an explicit
  exemption list (`/change-password`, `/login`, `/logout`,
  `/_blazor`, `/_framework`, `/_content`, `/health`, `/api`, `/lib`,
  `/css`, `/js`). `BootstrapAdminCommand` now stamps this claim on
  the user it creates between role assignment and the audit-log
  write (new exit code 6 reserved for the claim-add failure mode).
  The `ChangePassword` Razor page now carries `[Authorize]` and, on
  a successful `UserManager.ChangePasswordAsync`, enumerates and
  removes every `tabflow:must_change_password` claim on the user
  before calling `RefreshSignInAsync`. The claim is the single piece
  of state the middleware reads, so the loop tolerates duplicates
  defensively. Stock `IdentityUser<Guid>` is preserved unchanged;
  the design avoids a custom subclass and the schema migration that
  would imply, keeping the TD-0001 / TD-0003 migration trees stable.
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

### Tools

- **TD-0010 step 4: TF0002 Unit-tier test purity analyser
  (PR #26).** AC-133 ("No test in the `Unit` tier MAY touch the
  file system, the network, the system clock, or a database") is
  now enforced by a Roslyn analyser, not by review.
  - **Analyser.** New
    [`/tools/analyzers/TabFlow.Analyzers/UnitTierTestPurityAnalyzer.cs`](./tools/analyzers/TabFlow.Analyzers/UnitTierTestPurityAnalyzer.cs)
    declares rule `TF0002` (category `Testing`, default severity
    `Warning` → build break under `TreatWarningsAsErrors=true`).
    The analyser walks every `IdentifierName` syntax node, skips
    `using` directives and the contextual `var` keyword, and
    reports the diagnostic only when:
      - the containing class carries
        `[Trait("Category", "Unit")]`, **and**
      - the identifier resolves to a banned target.
    Banned targets cover all four AC-133 dimensions: database
    (`Npgsql.*`), network (`System.Net.Sockets.*`,
    `System.Net.Http.HttpClient`), file system (`System.IO.File`,
    `System.IO.Directory`, `System.IO.FileStream`), and system
    clock (`System.DateTime.Now`, `System.DateTimeOffset.Now` —
    `UtcNow` remains allowed because it is deterministic).
  - **Release tracking.** TF0002 declared in
    [`/tools/analyzers/TabFlow.Analyzers/AnalyzerReleases.Unshipped.md`](./tools/analyzers/TabFlow.Analyzers/AnalyzerReleases.Unshipped.md);
    RS2008 stays satisfied.
  - **Regression suite.** New
    [`/tests/Analyzers.Tests/UnitTierTestPurityAnalyzerTests.cs`](./tests/Analyzers.Tests/UnitTierTestPurityAnalyzerTests.cs)
    ships 7 Unit-tier cases:
      - allowed-types-only baseline (no diagnostic),
      - positive cases for `DateTime.Now`, `DateTimeOffset.Now`,
        `System.IO.File`, `HttpClient`,
      - negative scoping case: `[Trait("Category", "Integration")]`
        class can use the same forbidden types,
      - negative scoping case: untagged class is silent.
    14 of 14 tests in `Analyzers.Tests` now pass.
  - **No regression on existing tests.** `dotnet build`: 0 errors,
    16 pre-existing MSB4121 warnings, no TF0002 diagnostics in
    current Unit-tier suites (`Shared.Tests`, `Analyzers.Tests`).
  - **Capability matrix.** "Test taxonomy" row updated to record
    that step 4 is done in PR #26; steps 5 (transactional fixture)
    and 6 (Playwright bootstrap) remain open.

### Architecture

- **TD-0027 closed + TD-0016 step 1 + 3: Blazor Web App migration
  with per-page render modes (PR #25).** AD-0004 (mixed render
  modes per surface family) was a paper contract until PR #25:
  every Blazor component ran on a single, repository-wide,
  always-Interactive SignalR circuit because the hosts used the
  legacy standalone Blazor Server composition. PR #25 migrates the
  three hosts to the Blazor Web App composition and ships the
  per-page `@rendermode` opt-ins that AD-0004 demands.
  - **Composition migration (closes TD-0027).**
    `src/apps/platform/Program.cs` and
    `src/apps/tenant/Program.cs` swap
    `AddServerSideBlazor()` for
    `AddRazorComponents().AddInteractiveServerComponents()` and
    swap `MapBlazorHub() + MapFallbackToPage("/_Host")` for
    `MapRazorComponents<App>().AddInteractiveServerRenderMode()`.
    The legacy `App.razor` (router) is now
    `Components/Routes.razor`; a new `Components/App.razor`
    carries the HTML document root that `MapRazorComponents<App>()`
    requires. `src/apps/{platform,tenant}/Pages/_Host.cshtml`
    removed. `_Imports.razor` extended with
    `@using static Microsoft.AspNetCore.Components.Web.RenderMode`
    so component-level `@rendermode InteractiveServer` resolves
    without a fully-qualified type name.
  - **Render-mode opt-ins (closes TD-0016 step 1).** 13
    `.razor` pages now carry `@rendermode InteractiveServer`:
      - **Platform host (6 staff):** `Dashboard.razor`,
        `Tenants.razor`, `TenantsNew.razor`,
        `TenantsDetail.razor`, `Jobs.razor`, `Audit.razor`.
      - **Tenant host (3 staff):** `Kitchen.razor`,
        `Tables.razor`, `TableView.razor`.
      - **Tenant host (4 customer, provisional):** `Cart.razor`,
        `Menu.razor`, `Order.razor`, `ScanQr.razor`. AD-0004
        assigns these to Static SSR but their current
        implementation depends on `@onclick` and `IJSRuntime`
        calls that only work under Interactive Server. The
        Static SSR conversion is tracked under TD-0028.
  - **Smoke verification.** `dotnet run` on each host followed by
    `curl /health/live` returned
    `{"status":"pass",...}` from both hosts under the new
    composition. The release-gate smoke check that fetches each
    route and asserts the rendered HTML carries (or omits) the
    `_framework/blazor.web.js` interactive marker — TD-0016
    step 2 — is blocked on TD-0010 step 6 (Playwright bootstrap
    for the E2E / Smoke tier).
  - **Documentation (closes TD-0016 step 3).**
    [`/doc/docs/reference/architecture/render-modes.md`](./doc/docs/reference/architecture/render-modes.md)
    grew an "Implementation Status" section that names the 9 staff
    pages, the 4 customer pages, the migration PR, and the
    blocked smoke check.
    [`/doc/docs/reference/architecture/capability-matrix.md`](./doc/docs/reference/architecture/capability-matrix.md):
    customer-surfaces row updated to record the
    Interactive-Server-during-migration status; new "Mixed render
    modes per surface family (AD-0004)" row added.
  - **New tech debt:**
    - TD-0027 — opened and closed in PR #25 (Blazor Web App
      migration).
    - TD-0028 — opened: customer pages still Interactive Server;
      AD-0004 mandates Static SSR.

### Tools

- **TD-0009 step 4–5: TF0001 regression suite + AnalyzerReleases
  files (PR #24).** The English-first identifier analyser
  (`TabFlow.Analyzers.EnglishFirstIdentifierAnalyzer`, rule
  `TF0001`) is now backed by an xUnit regression test project and
  the `AnalyzerReleases.{Shipped,Unshipped}.md` release-tracking
  files that RS2008 requires.
  - **Test project.** New
    [`/tests/Analyzers.Tests/`](./tests/Analyzers.Tests/) project
    drives `CSharpAnalyzerTest<EnglishFirstIdentifierAnalyzer, DefaultVerifier>`
    over 7 cases:
      - ASCII baseline (no diagnostic),
      - positive cases for `NamedType`, `Method`, `Property`,
        `Field`, `Parameter` (one TF0001 per declaration),
      - compiler-generated names baseline (auto-property accessors
        do not surface separately).
    The suite carries `[Trait("Category", "Unit")]` so the existing
    PR workflow `Run unit tests` step picks it up; no workflow
    change required.
  - **Analyser bug fixed.** The harness surfaced one bug:
    property and event accessor methods (`get_X`, `set_X`,
    `add_X`, `remove_X`, `raise_X`) inherited the property's
    non-ASCII name and reported a diagnostic each, so a single
    Turkish property emitted three TF0001 lines instead of one.
    `EnglishFirstIdentifierAnalyzer.AnalyzeSymbol` now early-exits
    on `IMethodSymbol` whose `MethodKind` is one of the five
    accessor kinds; each non-ASCII identifier is reported exactly
    once at the user's declaration site.
  - **Release-tracking files.** New
    [`/tools/analyzers/TabFlow.Analyzers/AnalyzerReleases.Shipped.md`](./tools/analyzers/TabFlow.Analyzers/AnalyzerReleases.Shipped.md)
    (empty until the first tagged release per AD-0011) and
    [`/tools/analyzers/TabFlow.Analyzers/AnalyzerReleases.Unshipped.md`](./tools/analyzers/TabFlow.Analyzers/AnalyzerReleases.Unshipped.md)
    (declares TF0001 with rule ID, category, severity, notes).
    `TabFlow.Analyzers.csproj` feeds both files via
    `<AdditionalFiles>` and the `RS2008` `NoWarn` suppression has
    been removed; the release-tracking analyser now validates that
    every `TFxxxx` ID declared in the analyser project is
    accounted for in one of the two files.
  - **CPM growth.**
    [`/Directory.Packages.props`](./Directory.Packages.props):
    pinned `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing.XUnit`
    at 1.1.2 (matches the 4.13.0 Roslyn compiler API consumed by
    `TabFlow.Analyzers.csproj`).
  - **Capability matrix promotion.** "English-first lint
    enforcement" moves from `In progress` to **`Implemented`**
    (constitution II.4): tested (7 regression cases), observable
    (every PR fails on a non-ASCII identifier; build error
    surfaces in the IDE), documented (`test-taxonomy.md`,
    `threat-model.md`, AC-117, AC-118, AC-119,
    `internationalization.md`).

### Operations

- **TD-0026 systemd lifetime hook wired into all three hosts
  (PR #23).** Closes the `Type=notify` supervision contract gap that
  the 2026-04-26 alignment pass discovered (Phase D-2). All three
  host projects now register `Microsoft.Extensions.Hosting.Systemd`
  and signal readiness to systemd via `sd_notify("READY=1")` only
  after ASP.NET Core / `HostApplicationBuilder` completes startup:
  - `Directory.Packages.props`: pinned
    `Microsoft.Extensions.Hosting.Systemd` at 10.0.7 (matches the
    rest of the `Microsoft.Extensions.*` family).
  - `src/apps/platform/TabFlow.Platform.csproj`,
    `src/apps/tenant/TabFlow.Tenant.csproj`,
    `src/apps/platform-worker/TabFlow.PlatformWorker.csproj`: added
    a `<PackageReference Include="Microsoft.Extensions.Hosting.Systemd" />`
    line.
  - `src/apps/platform/Program.cs` and
    `src/apps/tenant/Program.cs`: call
    `builder.Host.UseSystemd()` immediately after
    `builder.Host.UseSerilog()`.
  - `src/apps/platform-worker/Program.cs`: calls
    `builder.Services.AddSystemd()` immediately after the builder
    is created. The platform worker uses `HostApplicationBuilder`,
    not `WebApplicationBuilder`, so the API shape is the
    `IServiceCollection` extension rather than the `IHostBuilder`
    one.
  Both extensions are no-ops when `INVOCATION_ID` is unset (i.e.
  outside systemd), so `dotnet run`, the existing unit tests, and
  any future Integration tier are unaffected.
  Capability matrix row "Process supervision via systemd" moves
  from `Target` to `In progress`. Operator enablement
  (`systemctl enable --now ...`) is out of scope; the
  composition-root regression test (TD-0026 step 3) lands with the
  TD-0010 step 5 transactional fixture.

### Documentation

- **Alignment pass closed: Phases D, E, F (PR #22).** Final three
  phases of the 2026-04-26 alignment pass — how-to tree, tutorials
  tree, and buildlog cross-reference — close in this PR. Eleven
  findings, one new ledger row, three buildlog cross-reference fixes.
  - **D-1 (`clean`)** — `setup-migrations.md` listed the design-time
    factory class names as `PlatformDesignTimeDbContextFactory` /
    `TenantDesignTimeDbContextFactory`; the shipping classes are
    `PlatformDbContextFactory` / `TenantDbContextFactory`.
    Renamed both code-block headings and class declarations.
  - **D-2 (`implement`)** — `supervise-processes.md` declares the
    host invariant as `Type=notify` and states the hosts MUST call
    `UseSystemd()`; a grep over `/src/apps/*/Program.cs` returned
    no matches. Opened **TD-0026** with a four-step payoff plan
    (add `Microsoft.Extensions.Hosting.Systemd` package, wire
    `builder.Host.UseSystemd()` into each `Program.cs`, ship a
    composition-root unit test, then close the supervise-processes
    caveat); added a TD-0026 callout under the `Type=notify`
    requirement section.
  - **D-3 (`clean`)** — `configure-branch-protection.md` cited
    "currently a tech-debt ledger entry" without naming the TD.
    Replaced the parenthetical with an explicit TD-0006 link.
  - **D-4 (`aligned with caveat`)** — `provision-tenant.md` Step 11
    aligned; the worker `MigrateAsync()` half is owned by TD-0003.
  - **D-5 (`aligned`)** — `bootstrap-platform.md`, `restart-tenant.md`,
    `inspect-provisioning-job.md`, `rotate-secrets.md`,
    `backup-and-restore.md`, `deploy-to-production.md`, and the
    how-to README all aligned with shipping code, surface IDs, and
    AD-0003 / AD-0005 / AC-127..AC-130.
  - **E-1 (`clean`)** — `tutorials/README.md` listed only
    `getting-started.md`; the second tutorial,
    `local-development.md`, was on disk but invisible to the index.
    Rewrote the README to list both with one-line descriptions.
  - **E-2 (`aligned`)** — `getting-started.md` reading order and
    `local-development.md` reserved-identifier table both aligned.
  - **F-1 (`clean`)** — `AC-008` cited in three ledger paragraphs
    (TD-0023 risk-if-unpaid, TD-0015 symptom + Linked footer) but
    undefined in `acceptance-criteria.md` (the AC range jumps from
    AC-006 to AC-010). Replaced the three references with **AC-010**
    ("tenant routes reject unauthenticated traffic"). The
    `code-audit-2026-04-25.md` reference is a closed audit pass and
    was left intact for trail integrity.
  - **F-2 (`aligned`)** — TD-0001..TD-0026 IDs continuous; every
    cited TD has a corresponding ledger entry. No orphans.
  - **F-3 (`aligned`)** — AD-0001..AD-0015 IDs continuous; every
    cited ADR is defined in `decisions.md`. No orphans.
  - **F-4 (`aligned`)** — buildlog subtree stubs (`postmortems/`,
    `spikes/`, `retrospectives/`, `abandoned/`) populated in PR #17
    (A-2); every doc that links into them resolves.
- **Tech-debt ledger growth.**
  [`/doc/buildlog/tech-debt-ledger.md`](./doc/buildlog/tech-debt-ledger.md):
  Opened **TD-0026** — `Type=notify` supervision contract requires
  `UseSystemd()` but neither host calls it (four-step payoff plan).
- **Audit pass closed.**
  [`/doc/buildlog/code-audit-2026-04-26.md`](./doc/buildlog/code-audit-2026-04-26.md)
  Sign-Off section now reads `Closed 2026-04-26`. Outcome
  summary: 6 phases walked, 27 findings closed, 8 new TDs (TD-0019
  through TD-0026), 1 existing TD scope-extended (TD-0022),
  6 PRs landed (#17 Phase A, #18 B-1, #19 B-2, #20 B-3, #21 C,
  #22 D + E + F).
- **Phase C of the alignment pass closed (PR #21).** Six findings
  in the explanation tree (`/doc/docs/explanation/concepts/`):
  - **C-1 (`clean`)** — `implementation-patterns.md` carried 4 stale
    patterns: `Order.Create` signature missed the `idempotencyKey`
    parameter (PR #12 / TD-0018), Common Pitfalls list missed
    `DeviceCookieValue` and `IdempotencyKey`, the Unit testing
    example used `Mock<TenantDbContext>` (against
    `test-taxonomy.md`'s "no mocks" rule), and the Controller
    Structure example showed the AD-0003 anti-pattern. All four
    rewritten with TD-0017 / TD-0018 / TD-0022 / TD-0025 callouts.
  - **C-2 (`clean`)** — `data-protection.md` declared `[DataClass]`
    schema-comment enforcement (the capability-matrix row is
    `Target` per **TD-0007**) and listed "TBD how-to" for four DSR
    procedures (access, erasure, restriction, portability) with no
    ledger reference. Added a TD-0007 callout to the `[DataClass]`
    paragraph; opened **TD-0024** with a five-step payoff plan
    (four how-to guides + the table-update step) and rewrote each
    DSR row to link the TD-0024 step that owns it.
  - **C-3 (`clean`)** — `test-taxonomy.md` stated "we do not use a
    mocking framework"; every test csproj
    (`E2E.Tests`, `Tenant.Tests`, `PlatformWorker.Tests`,
    `Platform.Tests`, `Shared.Tests`) references **NSubstitute**.
    Reframed the doc as a historical preference; opened **TD-0025**
    with a two-exit payoff plan (adopt NSubstitute officially or
    remove it from the csprojs).
  - **C-4 (`clean`)** — `threat-model.md` carried three mitigations
    that promised enforcement that does not yet ship:
    "missing policy is a build error per AD-0014" (AD-0014 does not
    generate that error), "analyzer flags `IQueryable.ToList()`
    without `Take()`" (the analyzer is not in `TabFlow.Analyzers`
    today), and "Backups encrypted at rest ... (deferred — TD when
    first backup ships)" with no TD number. All three rewritten
    with the actual current state and TD links (AD-0005 +
    TD-0010 step 5; TD-0009 future addition; capability-matrix
    `Target` row).
  - **C-5 (`clean`)** — `customer-session-model.md` Submit Flow
    did not list the TD-0017 device-binding cookie check or the
    TD-0018 idempotency-key check. Rewritten as a 9-step list
    naming both new checks explicitly.
  - **C-6 (`aligned`)** — `multi-tenancy.md`, `tenant-lifecycle.md`,
    `accessibility.md`, `internationalization.md`,
    `authorization.md`, `operational-surfaces.md`, and the two
    READMEs (`explanation/README.md`,
    `explanation/concepts/README.md`) confirmed aligned.
- **Tech-debt ledger growth.**
  [`/doc/buildlog/tech-debt-ledger.md`](./doc/buildlog/tech-debt-ledger.md):
  - Opened **TD-0024** — Data-subject-rights operator procedures
    (KVKK / GDPR) not yet documented (five-step plan: four how-to
    guides at `/doc/docs/how-to/data-subject-{access,erasure,
    restriction,portability}.md` plus the table-update step).
  - Opened **TD-0025** — `test-taxonomy.md` says "no mocking
    framework"; every test project references NSubstitute (two-exit
    payoff plan: adopt or remove).
- **Phase B-3 of the alignment pass closed (PR #20).** Five findings
  in the seven remaining reference documents (`api/internal-api.md`,
  `api/tenant-api.md`, `api/error-codes.md`, `database/schema.md`,
  `architecture/system-overview.md`, `architecture/health-checks.md`,
  `architecture/render-modes.md`, `firmware.md`):
  - **B-3.1 (`clean`)** — `internal-api.md` mixes customer-tier
    endpoints (Sessions, Cart, Orders) with the staff-tier surface,
    lists the stale `POST /api/orders/submit` route (the real path
    is `POST /api/public/orders` per `PublicOrdersController`), and
    omits the actually shipping staff endpoints. Added a TD-0023
    banner at the top of the document; opened **TD-0023** with a
    five-step rewrite plan (banner done, customer sections move to
    `tenant-api.md`, fix the order-submit entry, add the staff
    endpoints, close with the AD-0003 HTTP-is-the-exception note).
  - **B-3.2 (`correct`)** — `tenant-api.md` listed three
    aspirational customer-tier endpoints (`/api/public/profile`,
    `/api/public/catalog`, `/api/public/session`) and stated that
    the order submission requires an `Idempotency-Key` *header*; the
    shipping `PublicOrdersController.SubmitOrder` reads the
    `IdempotencyKey` from the request *body* per TD-0018. Added a
    "Migration status (TD-0021)" callout to each of the three
    aspirational sections naming the actual shipping route, and
    rewrote the order-submission section to describe the body
    field, cite TD-0017 (device-binding cookie verification) and
    TD-0018 (unique index), and explicitly say "not from an
    `Idempotency-Key` HTTP header".
  - **B-3.3 (`clean`)** — `schema.md` did not document the
    `device_cookie_value` column (TD-0017, migration
    `20260425214408_AddCustomerAccessTicketDeviceCookie`) or the
    `idempotency_key` column + unique index over
    `(session_id, idempotency_key)` (TD-0018, migration
    `20260425214627_AddOrderIdempotencyKey`). Rewrote the
    "Customer Session And Cart" and "Orders And Bills" bullets to
    name the new columns and cite the migration filenames.
  - **B-3.4 (`aligned with caveat`)** — `health-checks.md` advanced
    probes (`*-db:migrations`, `worker-heartbeat`,
    `event-bus:capacity`, `tenant-context`) are declared but not
    yet implemented; owned by **TD-0013**. No doc-text change
    needed.
  - **B-3.5 (`aligned`)** — `system-overview.md`, `render-modes.md`,
    `firmware.md`, and `error-codes.md` confirmed aligned with the
    shipping stack, surface map, and error vocabulary.
- **TD-0022 scope extended to platform-side.** PR #20's Phase B-3
  cross-check found the same `DbContext`-on-`ControllerBase`
  anti-pattern in the platform host's `TenantsController` and
  `JobsController`. The TD-0022 ledger entry was rewritten to list
  six controllers (4 tenant-side + 2 platform-side) and to add
  `ITenantRegistryService` (writes emit a `tenant.create` job and a
  `tenant.create` audit row inside the same transaction) and
  `IProvisioningJobReadService` to the payoff plan.
- **Documentation deltas in PR #20:**
  - [`/doc/docs/reference/api/internal-api.md`](./doc/docs/reference/api/internal-api.md):
    TD-0023 banner added at the top of the file.
  - [`/doc/docs/reference/api/tenant-api.md`](./doc/docs/reference/api/tenant-api.md):
    three "Migration status (TD-0021)" callouts; order-submission
    section rewritten for the body-field idempotency contract.
  - [`/doc/docs/reference/database/schema.md`](./doc/docs/reference/database/schema.md):
    `device_cookie_value` and `idempotency_key` columns documented
    with migration filenames and TD links.
- **Tech-debt ledger growth.**
  [`/doc/buildlog/tech-debt-ledger.md`](./doc/buildlog/tech-debt-ledger.md):
  Opened **TD-0023** — `internal-api.md` rewrite (five-step plan).
  Updated **TD-0022** — extended scope to the two platform-side
  controllers; controller count rewritten "four → six".
- **Phase B-2 of the alignment pass closed (PR #19).** Seven findings
  in `decisions.md` (15 ADRs walked end-to-end against shipping code,
  schema, and tooling):
  - **B-2.1 (`aligned`)** — ADR status taxonomy intact; all 15 ADRs
    carry `Accepted` (no `Proposed` / `Deprecated` / `Superseded`
    chains, expected for a pre-1.0 single-author repository).
  - **B-2.2 (`aligned`)** — 10 ADRs verified against shipping code:
    AD-0001 (Platform / Tenant separation), AD-0002 (ASP.NET Core 10
    + Blazor Web App), AD-0005 (Identity), AD-0006 (in-process bus),
    AD-0007 (PostgreSQL 17), AD-0009 (standalone migrations
    project), AD-0011 (SemVer + Keep-a-Changelog), AD-0012 (Apache
    2.0; `LICENSE` + `NOTICE` present), AD-0013 (GitHub Actions;
    `pr.yml` / `main.yml` / `release.yml` all present —
    `release.yml`'s prior "missing" note in
    [`/doc/buildlog/code-audit-2026-04-25.md`](./doc/buildlog/code-audit-2026-04-25.md)
    Section 6 was an audit-side error), AD-0014 (`.editorconfig` +
    `Directory.Build.props`).
  - **B-2.3 (`aligned with caveat`)** — AD-0004 mixed render modes
    baseline holds; code-side `@rendermode InteractiveServer` on
    staff Razor pages owned by **TD-0016**.
  - **B-2.4 (`aligned with caveat`)** — AD-0008 EF Core schema
    authority intact; worker `MigrateAsync()` + drop+apply+verify
    owned by **TD-0003**.
  - **B-2.5 (`aligned with caveat`)** — AD-0010 bootstrap CLI
    shipped per ADR text (PRs #9 + #16); operator-action half
    owned by **TD-0002**.
  - **B-2.6 (`aligned with caveat`)** — AD-0015 English-first
    enforced via `TF0001` (PR #14); `IStringLocalizer` + `*.resx`
    half owned by **TD-0011**; `AnalyzerReleases.{Shipped,Unshipped}.md`
    files owned by **TD-0009 step 4–5**.
  - **B-2.7 (`implement`)** — AD-0003 trade-off ("internal layer
    boundary [host → application service → domain] must remain
    explicit in code") observed inconsistently: 3 of 7 tenant API
    controllers go through application services
    (`CartController` → `ICartService`, `PublicOrdersController` →
    `IOrderService`, `SessionsController` →
    `ICustomerSessionService`), 4 inject `TenantDbContext`
    directly and run LINQ inline (`KitchenController`,
    `MenuController`, `OrdersController`, `TablesController`).
    Opened **TD-0022** with a four-step migration plan.
- **Tech-debt ledger growth.**
  [`/doc/buildlog/tech-debt-ledger.md`](./doc/buildlog/tech-debt-ledger.md):
  Opened **TD-0022** — Read-only tenant-side API controllers bypass
  the application service layer (4 of 7 controllers inject
  `TenantDbContext` directly). Four-step payoff plan: introduce
  `IKitchenReadService`, `IMenuReadService`, `ITableReadService` and
  fold order reads into the existing `IOrderService`; rewrite the
  four controllers to depend on the read services; ship a unit test
  per service against TD-0010's transactional fixture; add a Roslyn
  analyzer rule to `TabFlow.Analyzers` (extending the TD-0009
  project) that flags `DbContext` injection on `ControllerBase`
  derivatives.
- **Phase B-1 of the alignment pass closed (PR #18).** Five findings
  in the five self-consistency tables that
  [`/doc/docs/meta/contributing.md`](./doc/docs/meta/contributing.md#self-consistency)
  names (capability matrix, acceptance criteria, glossary, runtime
  surfaces, SLOs):
  - **B-1.1 (`clean`)** — capability matrix carried 8 stale rows
    after the PR #6–#16 cluster; rewrote them to name the shipping
    PR / TD per row and added a 9th row for `Test taxonomy via xUnit
    Traits` (TD-0010). Affected capabilities: Platform Identity
    store, Bootstrap admin CLI, Tenant migrations, Customer session,
    Fresh-QR checkout proof, Structured logging, English-first lint,
    GitHub Actions CI.
  - **B-1.2 (`correct`)** — `runtime-surfaces.md` declared four
    customer-tier endpoints under `/api/public/*` but only
    `/api/public/orders` ships there today; the other three
    customer surfaces are routed under `/api/menu`, `/api/cart`,
    `/api/sessions/*` and gated by `[AllowAnonymous]` attributes
    per TD-0015 step 2. Rewrote the runtime-surfaces HTTP table to
    reflect the shipping route map (8 endpoint groups split into
    customer-tier and staff-tier rows, each citing the controller
    and `[Authorize]` policy) and opened TD-0021 with a four-step
    migration plan.
  - **B-1.3 (`aligned`)** — `acceptance-criteria.md` AC-005, AC-006,
    AC-030..AC-036 confirmed aligned with the PR #7 / #9 / #11 /
    #12 / #16 closures.
  - **B-1.4 (`aligned`)** — `slos.md` surface-ID references
    (P-02..P-07, T-06..T-16) resolve into `runtime-surfaces.md`.
  - **B-1.5 (`aligned`)** — `glossary.md` cross-refs resolve;
    `/doc/buildlog/spikes/` text-vs-href divergence inherited
    closure from A-2.
- **Capability matrix refresh.**
  [`/doc/docs/reference/architecture/capability-matrix.md`](./doc/docs/reference/architecture/capability-matrix.md):
  9 rows updated to reflect what shipped in PRs #6–#16. Each row
  now names the PR, the TD, and any open tail steps. The matrix is
  back in sync with the constitution II.4 definition of `Done`.
- **Runtime-surface map refresh.**
  [`/doc/docs/reference/architecture/runtime-surfaces.md`](./doc/docs/reference/architecture/runtime-surfaces.md):
  the Tenant Host HTTP Endpoints table replaced. The new table has
  a `Tier` column (`customer` / `staff` / `n/a`) and lists every
  shipping route with its controller and `[Authorize]` policy. The
  prefix-level customer-tier separation (`/api/public/*`) is
  documented as TD-0021 follow-up work.
- **Tech-debt ledger growth.**
  [`/doc/buildlog/tech-debt-ledger.md`](./doc/buildlog/tech-debt-ledger.md):
  Opened **TD-0021** — Customer-tier HTTP endpoints not on the
  `/api/public/*` prefix (four-step migration plan: shim
  controllers, Blazor caller switch, HTTP 410 on legacy routes,
  `tenant-api.md` + OpenAPI update).
- **Code ↔ documentation alignment pass opened (PR #17).** New
  buildlog entry at
  [`/doc/buildlog/code-audit-2026-04-26.md`](./doc/buildlog/code-audit-2026-04-26.md)
  conducts a top-down horizontal sweep over every `/doc/` tree
  through the lens of the constitution and the documentation
  charter, classifying findings into four buckets (`aligned`,
  `implement`, `clean`, `correct`). Phase A (Meta tree) closed in
  this PR with five findings:
  - **A-1 (`correct`)** — `Diataxis` mis-spelt in the charter (two
    sites) and in `/doc/docs/README.md` (one site); rewritten to
    `Diátaxis`.
  - **A-2 (`correct`)** — the four `buildlog/` subtree directories
    (`postmortems/`, `spikes/`, `retrospectives/`, `abandoned/`)
    were named in the charter and the buildlog README but absent on
    disk; six documents (constitution, glossary, data-protection,
    amendment-template, configure-branch-protection) linked into
    them. Created stub READMEs for each subtree, parallel to the
    existing `/doc/userdocs/` and `/doc/apidocs/` stub-tree pattern.
  - **A-3 (`clean`)** — opened TD-0019 with the inventory of 12
    placeholder TODO comments in `/src/` lacking a `TD-NNNN`
    reference (constitution II.3 violation). Rewrote each comment as
    `TODO(TD-0019): ...` so a grep for `TD-0019` returns the ledger
    entry. Bare TODO count after rewrite: 0.
  - **A-4 (`correct`)** — opened TD-0020 documenting that the
    constitution's V.2 (review-pair) and V.4 (security review)
    invariants cannot be met during the pre-1.0 single-author
    window, with a three-exit payoff plan (add a maintainer, amend
    the constitution, or retroactively review the pre-1.0 PRs).
  - **A-5 (`aligned`)** — meta tree internal cross-references all
    resolve.
- **Charter spelling fix.**
  [`/doc/docs/meta/documentation-charter.md`](./doc/docs/meta/documentation-charter.md)
  and [`/doc/docs/README.md`](./doc/docs/README.md): `Diataxis` →
  `Diátaxis` (Daniele Procida's framework name carries an acute
  accent on the first `a`).
- **Four buildlog stub READMEs landed.** New files:
  [`/doc/buildlog/postmortems/README.md`](./doc/buildlog/postmortems/README.md),
  [`/doc/buildlog/spikes/README.md`](./doc/buildlog/spikes/README.md),
  [`/doc/buildlog/retrospectives/README.md`](./doc/buildlog/retrospectives/README.md),
  [`/doc/buildlog/abandoned/README.md`](./doc/buildlog/abandoned/README.md).
  Each carries the subtree's filename format, append-only rule,
  what-goes-here / does-not, document skeleton, and a `Status Today`
  line. The charter's buildlog tree map and the buildlog README's
  subtree table are now both backed by the file system.
- **Tech-debt ledger growth.**
  [`/doc/buildlog/tech-debt-ledger.md`](./doc/buildlog/tech-debt-ledger.md):
  - Opened **TD-0019** — Pre-1.0 placeholder TODOs lacking ledger
    entries (12 source-side `TODO` comments, full inventory in the
    entry, payoff plan steps 1 done in this PR).
  - Opened **TD-0020** — Pre-1.0 single-author review-pair shortfall
    (constitution V.2/V.4 cannot be met today, three-exit payoff
    plan).
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
  `auth.bootstrap` row to `platform_audit_log`, stamps the
  `tabflow:must_change_password` claim on the freshly created admin
  (TD-0002 step 3, see `### Security` below), and prints the
  generated password to stdout exactly once. Closes TD-0002 steps
  1 and 3 in source. The operator-action half (running the command
  on a fresh deployment per `/doc/docs/how-to/bootstrap-platform.md`)
  remains pending. Smoke check:
  `dotnet run --project src/apps/platform/TabFlow.Platform.csproj --no-build -- bootstrap-admin`
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
