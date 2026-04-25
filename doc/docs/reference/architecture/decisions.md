# Architecture Decisions

This document is the single record of active architecture decisions that shape
the TabFlow repository. Each entry captures the context, the decision itself,
and the consequences that follow.

## Practice

- One document, short entries. New files are created only when a decision
  requires depth that would overwhelm this index.
- Status taxonomy is defined by the documentation charter
  ([`../../meta/documentation-charter.md`](../../meta/documentation-charter.md#adr-status-lifecycle)).
  The five legal statuses are `Proposed`, `Accepted`, `Rejected`,
  `Deprecated`, `Superseded`.
- A `Proposed` ADR is the home for forward-looking architectural work.
  Drafts live here, not in a separate scratch tree.
- Superseded entries keep a forward pointer so the decision trail stays
  traceable.

## Index

- [AD-0001 Platform And Tenant Remain Architecturally Separate](#ad-0001-platform-and-tenant-remain-architecturally-separate)
- [AD-0002 Use ASP.NET Core 10 And Blazor Web App As The Unified Stack](#ad-0002-use-aspnet-core-10-and-blazor-web-app-as-the-unified-stack)
- [AD-0003 One Host Process Per Side](#ad-0003-one-host-process-per-side)
- [AD-0004 Mixed Render Modes Per Surface Family](#ad-0004-mixed-render-modes-per-surface-family)
- [AD-0005 ASP.NET Core Identity As The Single Authentication Model](#ad-0005-aspnet-core-identity-as-the-single-authentication-model)
- [AD-0006 In-Process Event Bus For Real-Time Surfaces](#ad-0006-in-process-event-bus-for-real-time-surfaces)
- [AD-0007 PostgreSQL 17 As The Storage Baseline](#ad-0007-postgresql-17-as-the-storage-baseline)
- [AD-0008 EF Core As Schema And Migration Authority](#ad-0008-ef-core-as-schema-and-migration-authority)
- [AD-0009 Migrations Live In A Standalone Project With Design-Time Factories](#ad-0009-migrations-live-in-a-standalone-project-with-design-time-factories)
- [AD-0010 The Bootstrap Platform Admin Is Created By A CLI Command Not A Migration](#ad-0010-the-bootstrap-platform-admin-is-created-by-a-cli-command-not-a-migration)
- [AD-0011 Semantic Versioning With Tagged Commits On `main`](#ad-0011-semantic-versioning-with-tagged-commits-on-main)
- [AD-0012 Apache License 2.0](#ad-0012-apache-license-20)
- [AD-0013 GitHub Actions As The Continuous Integration Platform](#ad-0013-github-actions-as-the-continuous-integration-platform)
- [AD-0014 Coding Standards Live In `.editorconfig` And `Directory.Build.props`](#ad-0014-coding-standards-live-in-editorconfig-and-directorybuildprops)
- [AD-0015 English-First For Internal Contracts](#ad-0015-english-first-for-internal-contracts)

---

## AD-0001 Platform And Tenant Remain Architecturally Separate

### Status

Accepted.

### Context

TabFlow is a multi-tenant cafe operations product. Control-plane concerns
(tenant registry, provisioning, global audit) and tenant runtime concerns
(menu, orders, floor, stations, devices) have different failure domains,
different authorization models, and different audiences.

Collapsing them into one application tends to cause the control plane to
depend on tenant business tables, to make provisioning an implicit consequence
of request handling, and to blur operational incidents across tenants.

### Decision

The platform and each tenant remain architecturally separate:

- The platform owns tenant registry, provisioning jobs, global domains, and
  platform-level audit.
- Each tenant owns its own runtime business state in its own database.
- The platform is not a tenant and must not behave like one.

This separation exists independently of any single-stack decision.

### Consequences

- Platform and tenant host processes stay distinct even when both use the same
  stack and deployment tooling.
- Provisioning is an explicit bridge, not a byproduct of request handling.
- A tenant incident must not be able to take the platform down through shared
  runtime state.

### Related

- [`../../explanation/concepts/multi-tenancy.md`](../../explanation/concepts/multi-tenancy.md)
- [`./system-overview.md`](./system-overview.md)

---

## AD-0002 Use ASP.NET Core 10 And Blazor Web App As The Unified Stack

### Status

Accepted.

### Context

TabFlow needs a full-stack framework that covers static rendering for
customer-facing surfaces, interactive rendering for staff surfaces,
authentication, authorization, antiforgery, model binding, localization, and
validation, with a data-access story that stays consistent across platform
and tenant hosts.

A two-ecosystem setup (one language for the web tier and another for the
API tier) would require two package managers, two test runners, two
linters, two build pipelines, and an always-on discipline to keep shared
contracts in sync. For a small team targeting a single product family, that
duplication outweighs the framework-level benefits.

The team standardizes on the .NET ecosystem for long-term durability and
for a single release cadence to track.

### Decision

Use ASP.NET Core 10 and Blazor Web App as the single full-stack platform for
both platform and tenant hosts:

- Blazor components render both static HTML and interactive UI in one project.
- C# records, nullability, pattern matching, and source generators serve both
  UI and domain concerns.
- EF Core serves as the data access baseline.
- PostgreSQL 17 remains the storage baseline ([AD-0007](#ad-0007-postgresql-17-as-the-storage-baseline)).

### Consequences

Positive:

- one language, one package manager, one build, one test runner
- shared contracts live as plain .NET types, not duplicated across ecosystems
- framework-level features (antiforgery, model binding, localization,
  validation, Identity) come as first-class primitives
- supply chain shrinks substantially

Tradeoffs:

- developers familiar only with React must learn Blazor component authoring
- some ecosystem conveniences from the JavaScript world do not have one-for-one
  equivalents and must be solved with Razor components or minimal JS interop
- Blazor Server interactivity introduces a new operational concern
  ([AD-0004](#ad-0004-mixed-render-modes-per-surface-family) limits where this
  applies)

### Related

- [`./system-overview.md`](./system-overview.md)

---

## AD-0003 One Host Process Per Side

### Status

Accepted.

### Context

A common industry pattern is to split each side of a web product into a
separate backend API process and a separate frontend web process that
proxies to the backend. That shape is justified when the two processes use
different runtimes or when the web tier faces load characteristics that the
API tier does not. It carries costs: a backend-for-frontend proxy layer,
forwarded actor identity, duplicated request validation, twice as many
deployment units, and twice as many health probes.

With Blazor ([AD-0002](#ad-0002-use-aspnet-core-10-and-blazor-web-app-as-the-unified-stack))
the technical reasons for the split do not apply: one ASP.NET Core host
can render HTML, host interactive components, and expose the small remaining
external HTTP surface.

### Decision

Each side of the system runs as a single ASP.NET Core host:

- Platform host exposes the platform admin UI and any endpoints required for
  health, operational probes, and future external integrations.
- Tenant host exposes every tenant-facing surface (customer menu, admin
  console, floor/cash, station boards, waiter PDA) and the ESP32 device
  WebSocket endpoint within one process.

Shared domain logic lives in referenced packages so hosts stay focused on
composition, routing, authentication, and Razor entry points.

### Consequences

Positive:

- the backend-for-frontend proxy layer is absent by design
- authentication is native cookie auth; signed actor-forwarding headers
  are not used
- deployment collapses to one unit per side and one nginx upstream
- Blazor components call domain services directly through dependency injection
  rather than through HTTP round trips

Tradeoffs:

- a future native mobile or third-party integration requires exposing a
  deliberate external API surface. This is an additive project, not a
  retrofit; the domain layer is already isolated.
- host process shape now carries both UI and API concerns; the internal layer
  boundary (host → application service → domain) must remain explicit in code.

### Related

- [`./system-overview.md`](./system-overview.md)
- [`./runtime-surfaces.md`](./runtime-surfaces.md)

---

## AD-0004 Mixed Render Modes Per Surface Family

### Status

Accepted.

### Context

Blazor Web App offers four render modes: Static SSR, Interactive Server,
Interactive WebAssembly, and Interactive Auto. Each has different runtime
costs.

TabFlow surfaces serve audiences with different expectations:

- Customer menu surfaces run on the customer's own phone over mobile data. The
  session is short, the device is not trusted, and JavaScript weight directly
  affects first contentful paint.
- Staff surfaces (console, floor/cash, waiter PDA, station board) run on cafe
  hardware over local Wi-Fi. They are high-interaction, stateful, and benefit
  from live push.

Forcing one mode across all surfaces either wastes a SignalR connection on
every phone scan or requires every staff surface to write polling and
revalidation logic by hand.

### Decision

Each surface declares its render mode explicitly. The baseline:

- Platform host: Interactive Server for every surface.
- Tenant host: Static SSR for public customer and authentication
  surfaces; Interactive Server for admin console, floor and cash
  workspace, waiter PDA, and station boards.

The authoritative per-route render-mode table lives in
[`./runtime-surfaces.md`](./runtime-surfaces.md). Changes to a specific
route's render mode are applied there, not duplicated into this ADR.

Interactive WebAssembly and Interactive Auto are not used in the
current baseline. Offline-capable staff surfaces are not a baseline
product requirement.

### Consequences

Positive:

- customer phones do not open a SignalR connection for menu browsing
- staff surfaces get first-class push-to-render semantics without hand-rolled
  polling
- render mode is a per-component property, visible in source, and easy to
  change if a surface's needs shift

Tradeoffs:

- the render mode baseline must stay documented because the choice affects
  hosting capacity planning
- any component authored for the public surfaces cannot rely on SignalR state
  and must be written to the Static SSR contract

### Related

- [`./render-modes.md`](./render-modes.md)
- [`./runtime-surfaces.md`](./runtime-surfaces.md)

---

## AD-0005 ASP.NET Core Identity As The Single Authentication Model

### Status

Accepted.

### Context

A product with platform admins, tenant staff, and customer sessions could
take on three handwritten HMAC-signed cookie protocols
(platform admin, tenant admin, customer access ticket), plus a static
shared-key header scheme for internal service-to-service calls. Each protocol
was reimplemented in both the web tier and the API tier, with its own cookie
format, rotation story, and failure modes.

With a unified host ([AD-0003](#ad-0003-one-host-process-per-side)), most of
that machinery becomes unnecessary.

### Decision

ASP.NET Core Identity with cookie authentication is the single authentication
model for human actors inside both hosts:

- Platform admin identities are stored in the platform database and managed by
  Identity primitives (`UserManager`, `SignInManager`, password hasher,
  lockout policy).
- Tenant admin, manager, cashier, and station-device identities are stored in
  the tenant database with the same Identity primitives, scoped to that
  tenant.
- Authorization is expressed through `[Authorize(Roles = "...")]` and named
  authorization policies defined at host startup.

Customer access is not an Identity user. Customer sessions remain a
tenant-local domain concept rooted in the QR token lifecycle and a server-side
cart bound to the live table session.

The ESP32 device authentication contract stays out of Identity; the device
WebSocket handshake uses its table id and device key pairing. The firmware
contract is tracked separately from the identity model on purpose so a
future firmware change does not require rewriting the identity layer.

### Consequences

Positive:

- handwritten HMAC cookie code, actor-forwarding headers, and shared admin-key
  headers are not part of the stack
- authorization reads as standard ASP.NET Core idioms
- password hashing, lockout, two-factor extension points, and cookie rotation
  are framework-provided and covered by the .NET security release cadence

Tradeoffs:

- Identity's default schema assumes one user table per database. Platform and
  tenant hosts therefore each own an independent Identity store; cross-host
  identity federation is out of scope.
- the station device access model is still open. It will either reuse Identity
  with a synthetic user row or remain a tenant-local device token, depending
  on the station hardware choice, which is tracked separately.

### Related

- [`../../explanation/concepts/authorization.md`](../../explanation/concepts/authorization.md)
- [`../../explanation/concepts/customer-session-model.md`](../../explanation/concepts/customer-session-model.md)

---

## AD-0006 In-Process Event Bus For Real-Time Surfaces

### Status

Accepted.

### Context

Staff-facing surfaces need to react to business events without polling:

- the floor and cash workspace reflects open-check state and bill transitions
- the station board reflects order-state transitions produced by customers,
  waiters, and other stations

The unified tenant host runs in a single process per tenant. A fan-out event
bus with an external broker (Redis pub/sub, RabbitMQ, Kafka) would add
operational surface area that the current scale does not justify.

### Decision

Real-time fan-out inside one tenant host runs through an in-process event bus
backed by bounded `System.Threading.Channels` instances. Blazor Interactive
Server components subscribe through a hosted service that owns the channel
topology.

Event types are a small closed set described in `reference/architecture/
runtime-surfaces.md`, for example `order.submitted`,
`order.status_changed`, `bill.closed`, `table.opened`, `device.connected`,
`device.disconnected`.

Events are published by the domain service layer inside the same transaction
boundary that commits the underlying state change. Cross-process fan-out is
not required because each tenant host process is the authoritative owner of
its own real-time state.

### Consequences

Positive:

- zero external broker dependency for real-time UI
- event authoring stays inside the domain service, next to the state change
  it describes
- Blazor component subscription is a plain `IAsyncEnumerable<TenantEvent>`,
  easy to read and test

Tradeoffs:

- the bus is per-process and will not scale to a multi-instance tenant host
  without an external broker. When that scale is needed, the channel layer is
  replaced by a broker-backed implementation behind the same interface.
- events are best-effort within their subscription window; late-joining
  components read current state through a normal query and then subscribe.

### Related

- [`./runtime-surfaces.md`](./runtime-surfaces.md)

---

## AD-0007 PostgreSQL 17 As The Storage Baseline

### Status

Accepted.

### Context

TabFlow needs separate platform and tenant databases, predictable relational
modeling, strong transaction semantics, mature .NET support, and
straightforward host-level deployment.

### Decision

PostgreSQL 17 is the chosen storage baseline for both the platform database
and every tenant database.

### Consequences

Positive:

- strong relational integrity for registry, order, bill, and session state
- mature .NET integration through Npgsql and EF Core
- clean separation between platform and tenant databases
- operationally familiar on Linux hosts

Tradeoffs:

- database administration remains an explicit operational responsibility
- per-tenant database creation must be handled carefully in provisioning
- any future cross-tenant analytics must be designed deliberately rather than
  assumed from one giant shared database

### Related

- [`../database/schema.md`](../database/schema.md)

---

## AD-0008 EF Core As Schema And Migration Authority

### Status

Accepted.

### Context

A schema managed through embedded SQL blocks and idempotent `ALTER` scripts
applied at startup makes schema drift hard to describe, down-migration
difficult to generate cleanly, and model-to-schema validation difficult to
run in CI. Those are the problems an explicit migrations toolchain solves.

### Decision

EF Core is the authoritative schema and migration path for both the platform
database and tenant databases. Migrations are generated from the `DbContext`
model and committed under `src/infra/postgres/Migrations/Platform` and
`src/infra/postgres/Migrations/Tenant`. The migrations project layout is
covered by [AD-0009](#ad-0009-migrations-live-in-a-standalone-project-with-design-time-factories).

Tenant bootstrap applies the committed migration history rather than executing
handwritten idempotent `ALTER` scripts.

### Consequences

Positive:

- schema is expressed in one place as model code plus generated migration
  history
- CI can validate that the model and migrations match
- tenant provisioning gains deterministic migration behavior

Tradeoffs:

- any schema change becomes a model change followed by a generated migration;
  ad-hoc SQL is no longer acceptable during normal development
- handwritten SQL is reserved for data shape changes that EF Core cannot
  express, and those migrations must be reviewed explicitly

### Related

- [`../database/schema.md`](../database/schema.md)

---

## AD-0009 Migrations Live In A Standalone Project With Design-Time Factories

### Status

Accepted.

### Context

EF Core's design-time tooling (`dotnet ef migrations add`,
`dotnet ef database update`) needs to construct a `DbContext` instance
without booting a host. Two common shapes for this are:

- Co-locating migrations inside the host project that uses the context.
  `dotnet ef` then boots `Program.cs` to discover the context. This couples
  schema authority to host startup wiring (logging, dependency injection,
  configuration sources) and makes the platform worker's runtime migration
  application brittle when host startup changes.
- A standalone migrations project that exposes
  `IDesignTimeDbContextFactory<T>` for each context. The tooling
  instantiates the factory directly; no host startup runs.

The first shape produces empty migrations when host startup fails to
register the context (silent schema drift) and forces operators to provide
host-time configuration to design-time tools.

### Decision

Migrations live in a single standalone class library,
`src/infra/postgres/TabFlow.Migrations.csproj`, that:

- references `TabFlow.Shared` for both `PlatformDbContext` and
  `TenantDbContext`,
- provides one `IDesignTimeDbContextFactory<T>` per context under
  `DesignTime/`,
- holds one `Migrations/Platform/` and one `Migrations/Tenant/` tree, each
  with its own `<Context>ModelSnapshot.cs`.

Both hosts and the platform worker reference this assembly so the same
migration history applies in development, in CI, and at runtime.

### Consequences

Positive:

- design-time tooling never boots a host, so migration generation is
  insensitive to host wiring changes
- the platform worker calls
  `TenantDbContext.Database.MigrateAsync()` against tenant databases using
  the same compiled migration assembly, eliminating the gap between
  generated and applied schema
- empty-migration accidents (where the snapshot disagrees with the model)
  surface immediately because the factory must succeed before the tool can
  diff

Tradeoffs:

- the migrations project is one more `.csproj` in the solution
- design-time factories rely on environment variables for the scratch
  database connection string. Defaults exist for a fresh local install;
  CI overrides them.

### Related

- [`../database/schema.md`](../database/schema.md)
- [`/doc/docs/how-to/setup-migrations.md`](/doc/docs/how-to/setup-migrations.md)
- [AD-0008](#ad-0008-ef-core-as-schema-and-migration-authority)

---

## AD-0010 The Bootstrap Platform Admin Is Created By A CLI Command Not A Migration

### Status

Accepted.

### Context

A platform with no users cannot serve `/login` and therefore cannot create
its first user through the UI. The first admin must come from somewhere.
Two shapes for this are:

- Seed the admin row inside an EF Core migration with a hard-coded
  password hash. The hash is static, leaks through git history, and cannot
  be rotated without authoring a new migration. The hash format is also
  tied to ASP.NET Core Identity's password hasher version; changes to the
  hasher invalidate seeded credentials silently.
- Provide a one-shot CLI command that uses the same `UserManager`
  primitives the running platform uses, generates a high-entropy password
  at run time, prints it to stdout exactly once, and refuses to run a
  second time.

The first shape couples credential lifecycle to schema lifecycle and
embeds a credential artefact in source control. AD-0005 ties human
identities to the framework-managed Identity primitives; manual hash
seeding violates that contract.

### Decision

The first platform admin is created by a `bootstrap-admin` command on the
platform host. The command:

- refuses to run if any user already exists in the platform database,
- generates a CSPRNG-backed password,
- calls `UserManager.CreateAsync` so the hash uses Identity's current
  hasher,
- assigns the `owner` role,
- writes a `auth.bootstrap` row to `platform_audit_log`,
- prints the generated password to stdout exactly once.

The operator captures the printed password, signs in, and is forced
through `/change-password` on first authenticated request.

EF Core migrations never INSERT into `AspNetUsers`.

### Consequences

Positive:

- credential never appears in git history
- credential rotation is independent of schema changes
- the hasher version always matches because the running framework
  generates the hash
- bootstrap is observable in the audit log

Tradeoffs:

- bootstrap is a runtime concern, so it cannot run before the platform
  binary is deployed; the operator runs it once from the deployed host
- recovery from a lost admin credential is a separate procedure, not a
  re-bootstrap. See [`../../how-to/rotate-secrets.md`](../../how-to/rotate-secrets.md).

### Related

- [`../../how-to/bootstrap-platform.md`](../../how-to/bootstrap-platform.md)
- [AD-0005](#ad-0005-aspnet-core-identity-as-the-single-authentication-model)
- [AD-0008](#ad-0008-ef-core-as-schema-and-migration-authority)

---

## AD-0011 Semantic Versioning With Tagged Commits On `main`

### Status

Accepted.

### Context

The project needs a deterministic way to identify which code is in
production and to communicate the size of a change to operators and
external API consumers. Without a versioning rule, every change looks
the same to a reader of the commit history; there is no signal that a
deployment will require operator attention versus a routine refresh.

### Decision

TabFlow adopts [Semantic Versioning 2.0](https://semver.org/spec/v2.0.0.html).
Releases are tagged commits on `main` in the form `vMAJOR.MINOR.PATCH`.

- **MAJOR** — a breaking change to a stable contract (HTTP route, event
  payload, DB column, config key, or accepted ADR being superseded).
- **MINOR** — additive, non-breaking changes.
- **PATCH** — bug fixes, internal refactors, doc-only updates.

The current major before 1.0.0 (`0.y.z`) treats minor bumps as the
breaking-change channel and patch bumps as additive; this changes when
1.0.0 ships.

Every release tag corresponds to one section in
[`/CHANGELOG.md`](/CHANGELOG.md), which follows the
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) format.

### Consequences

Operators can read a tag and know whether to expect a routine restart
(patch), a feature addition (minor), or a planned migration window
(major). External API consumers gain a predictable deprecation
contract.

The release gate
([`../../meta/release-gate.md`](../../meta/release-gate.md)) verifies
the CHANGELOG entry exists for every release tag.

### Related

- [`/CHANGELOG.md`](/CHANGELOG.md)
- [`../../meta/release-gate.md`](../../meta/release-gate.md)
- [Semantic Versioning 2.0](https://semver.org/spec/v2.0.0.html)
- [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/)

---

## AD-0012 Apache License 2.0

### Status

Accepted.

### Context

TabFlow is open source. The license choice affects who may use, modify,
and redistribute the code, and how patent claims are handled. The
candidates were a permissive licence (MIT, BSD), a more comprehensive
permissive licence (Apache 2.0), or a copyleft licence (AGPL).

For a multi-tenant operations platform that may be deployed by third
parties, two concerns matter:

- explicit patent grant from contributors so downstream users are not
  exposed to patent litigation;
- a NOTICE mechanism for attribution that scales beyond a single
  copyright line.

### Decision

TabFlow is licensed under the [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0).

- The full licence text is at [`/LICENSE`](/LICENSE).
- Attribution lives in [`/NOTICE`](/NOTICE).
- Every source file MAY carry the short Apache header; new files SHOULD
  use the boilerplate from the LICENSE appendix.

Contributions are accepted under the same licence (Apache 2.0
Section 5: inbound = outbound). No separate CLA is required at this
stage; if one becomes necessary later, it will be introduced via an
amending ADR.

### Consequences

External users may build on TabFlow, modify it, and redistribute under
their own terms, provided they keep the licence and NOTICE text and
state significant modifications. The patent grant terminates if a
recipient sues over patent claims related to the Work.

The licence does not extend trademark rights; the project name and any
logos remain governed by trademark law and require separate permission.

### Related

- [`/LICENSE`](/LICENSE)
- [`/NOTICE`](/NOTICE)
- [Apache License 2.0 reference](https://www.apache.org/licenses/LICENSE-2.0)
- [Apache 2.0 FAQ](https://www.apache.org/foundation/license-faq.html)

---

## AD-0013 GitHub Actions As The Continuous Integration Platform

### Status

Accepted.

### Context

The release gate
([`../../meta/release-gate.md`](../../meta/release-gate.md)) lists
multiple automated checks (test suite, formatting, static analysis,
markdown lint, dead-link, SAST, dependency audit). These checks need a
single CI runner that:

- triggers on every PR and on `main` after merge;
- has access to .NET 10 SDK + PostgreSQL 17 + Node tooling;
- can execute the full release-gate checklist on a release branch;
- produces a single visible "gate green" signal a reviewer can trust.

The repository lives on GitHub. Self-hosting a separate CI plane (TeamCity,
GitLab CI, Drone) would be additional infrastructure with no offsetting
benefit at this scale.

### Decision

CI runs on **GitHub Actions**. Workflow definitions live in
`.github/workflows/`.

Required workflows:

- `pr.yml` — runs on every pull request: build, test, lint, dead-link,
  SAST, dependency audit. Required for merge.
- `main.yml` — runs on every push to `main`: full PR set plus capability
  matrix consistency check and release-gate dry run.
- `release.yml` — triggered by a tag matching `v*.*.*`: full
  release-gate, then publishes artefacts and creates a GitHub Release
  using the matching `[Unreleased]` block from `/CHANGELOG.md`.

Workflows MUST use pinned action versions (`@vMAJOR` is acceptable; SHA
pinning is preferred for third-party actions). Secrets used by
workflows are defined in repository or environment scope and never
written to logs.

### Consequences

The CI plane is part of GitHub. If the project ever moves off GitHub,
workflow definitions need to be ported. The `.github/workflows/` tree
is the single source of truth for CI behaviour.

Operators reading the release tag see the same green check the reviewer
saw at merge.

### Related

- [`../../meta/release-gate.md`](../../meta/release-gate.md)
- [`../../meta/review-policy.md`](../../meta/review-policy.md)

---

## AD-0014 Coding Standards Live In `.editorconfig` And `Directory.Build.props`

### Status

Accepted.

### Context

Constitution IV.3 requires that "code style follows the framework" and
that "custom abstractions need a justification an ADR can carry". This
is unmeasurable without an enforced configuration. Two contributors can
follow framework idioms differently and disagree on naming, var usage,
or brace placement, and a reviewer has no objective rule to point at.

### Decision

Coding standards are encoded as machine-checkable configuration:

- [`/.editorconfig`](/.editorconfig) defines whitespace, line length,
  C#-specific style, and naming rules. Every editor and the CI build
  honour it.
- [`/Directory.Build.props`](/Directory.Build.props) raises analyzer
  warnings to errors for the rules that protect contracts (nullable
  reference types, async correctness, dispose patterns).
- Style violations marked `:error` block the build; `:warning` shows up
  in PR review noise; `:suggestion` appears only in the editor.

Naming rules:

- Types, methods, properties, public fields → `PascalCase`.
- Private fields → `_camelCase` with underscore prefix.
- Constants → `PascalCase`.
- Interfaces → prefix `I`.
- Local variables, parameters → `camelCase`.

Format-on-save is expected for every contributor. CI runs `dotnet format`
in `--verify-no-changes` mode; a non-clean diff fails the gate.

### Consequences

Reviewers no longer debate style. Style violations either block the
build (analyzer error) or appear as a single deterministic diff that
can be applied with one command. Naming drift across the codebase is
caught at build time, not in review.

The `.editorconfig` is itself a contract. Changes follow ADR amendment
rules; a contributor cannot silently relax a rule by editing the file
without review.

### Related

- [`/.editorconfig`](/.editorconfig)
- [`/Directory.Build.props`](/Directory.Build.props)
- [`../../constitution.md`](../../constitution.md) Section IV.3
- [Microsoft .NET coding conventions](https://learn.microsoft.com/dotnet/csharp/fundamentals/coding-style/coding-conventions)

---

## AD-0015 English-First For Internal Contracts

### Status

Accepted.

### Context

TabFlow's primary user-facing market is Turkish-speaking. The natural
temptation is to write code, schema, and documentation in the language
of the operators who will use the product. This temptation surfaces
early: tenant onboarding documents in Turkish, schema columns named
`musteri_adi`, log message templates with Turkish vocabulary.

Doing so has predictable downstream costs:

- search and tooling that expect English casing (`PascalCase`,
  `camelCase`) on .NET identifiers fragment when mixed with Turkish
  diacritics;
- onboarding any non-Turkish contributor becomes a translation task
  before any actual work begins;
- the documentation tree, ADRs, and API references must all pick one
  language; mixing two creates two parallel and inevitably divergent
  truths;
- third-party libraries, error messages, and the .NET base class
  library are entirely English, producing jarring switches mid-stack.

The decision applies regardless of the team's current composition. A
single-developer Turkish team today does not change the cost of
mixed-language identifiers tomorrow.

### Decision

**English is the only language used for internal contracts.** This
covers:

- Source code identifiers (types, methods, fields, parameters,
  variables, namespaces).
- Database schema (table names, column names, enum members, constraint
  names, sequence names).
- HTTP API surfaces (route segments, request and response field names,
  error `code` strings, OpenAPI documents).
- ADRs, acceptance criteria, runtime surfaces, glossary, and every
  document under [`/doc/`](/doc/) including the constitution, charter,
  and tech-debt ledger.
- Log message templates and structured-log property names.
- Audit log `event_key` values.
- Commit messages, PR titles, branch names, and code-review comments.

**Translation happens at the presentation layer only.** A
customer-facing or staff-facing surface renders an English neutral
resource key into the active language at request time, using
`IStringLocalizer<T>` and `*.resx` files. The neutral `*.resx` is
always English; per-language files (`*.tr.resx`, `*.de.resx`, ...)
sit beside it.

The default tenant `LanguageCode` is `en`. A tenant explicitly opting
into Turkish receives the Turkish translation set; the underlying data
and contracts stay English.

### Consequences

This rule is enforced in code review and (where possible) by tooling:

- analyzer rules in
  [`/.editorconfig`](/.editorconfig) reject identifiers containing
  non-ASCII letters in the projects under
  [`/src/`](/src/);
- the markdown lint workflow flags non-English content in
  [`/doc/`](/doc/);
- a doc reviewer rejects user-visible string literals that are not
  routed through `IStringLocalizer<T>`.

The visible cost is that a Turkish-speaking contributor writing a new
feature must name it in English first. The benefit is that every
search, every grep, every analyzer, and every external contributor
sees a single coherent codebase.

The product is not English-first; **the contracts are**. Operators
running a Turkish tenant continue to see Turkish text everywhere a
human reads it.

### Related

- [`../../explanation/concepts/internationalization.md`](../../explanation/concepts/internationalization.md)
- [`../../constitution.md`](../../constitution.md) Section IV.3 (code
  style follows the framework — English casing is part of that)
- [`../glossary.md`](../glossary.md) — defines every term in English
- AD-0014 — coding standards in `.editorconfig` and
  `Directory.Build.props`
