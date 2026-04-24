# Architecture Decisions

This document is the single record of active architecture decisions that shape
the TabFlow repository. Each entry captures the context, the decision itself,
and the consequences that follow.

## Practice

- One document, short entries. New files are created only when a decision
  requires depth that would overwhelm this index.
- Status lifecycle: `Proposed` → `Accepted` → `Superseded` or `Deprecated`.
  Superseded entries keep a forward pointer so the decision trail stays
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

---

## AD-0001 Platform And Tenant Remain Architecturally Separate

### Status

Accepted.

### Context

TabFlow is a multi-tenant cafe operations product. Control-plane concerns
(tenant registry, provisioning, global audit) and tenant runtime concerns
(menu, orders, floor, stations, devices) have different failure domains,
different authorization models, and different audiences.

Collapsing them into one application historically causes the control plane to
depend on tenant business tables, makes provisioning an implicit consequence of
request handling, and blurs operational incidents across tenants.

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
model and committed under `src/infra/postgres/migrations/platform` and
`src/infra/postgres/migrations/tenant`.

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
