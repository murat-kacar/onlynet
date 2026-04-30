# Glossary

This glossary defines the durable terms used across TabFlow
documentation. When a doc says "access ticket" or "checkout proof" it
means the exact concept defined here.

Terms are grouped by domain. Cross-references point to the canonical
reference or explanation document for each term.

## Hosting And Tenancy

### Platform Host

The single Blazor Web App process that serves the platform control
plane: tenant registry, platform admin users, provisioning jobs, and
platform audit review. One instance for the whole deployment. See
[`./architecture/system-overview.md`](./architecture/system-overview.md).

### Platform Worker

The background service that claims `tenant.create` jobs from the
platform database and orchestrates tenant runtime setup. Runs as its
own process; shares the platform database with the platform host.

### Tenant Host

One Blazor Web App process per cafe. Bound to exactly one tenant
database. Serves the customer menu, the admin console, the floor and
cash workspace, the waiter PDA, and the station boards.

### Tenant

A single cafe's runtime: one tenant database, one tenant host process,
one primary domain. Created by the platform; its lifecycle is described
in
[`../explanation/concepts/tenant-lifecycle.md`](../explanation/concepts/tenant-lifecycle.md).

### Tenant Code

Stable, short identifier for a tenant (for example `demo-cafe`). Used
as the suffix for the tenant database name and as a routing key in
platform administration.

## Runtime Surfaces And Identities

### Runtime Surface

A family of related screens (for example the admin console or the
station board). The full list and per-surface render mode live in
[`./architecture/runtime-surfaces.md`](./architecture/runtime-surfaces.md).

### Platform User

An ASP.NET Core Identity user that authenticates against the platform
host. Platform users have platform-level roles and do not exist in any
tenant database.

### Tenant User

An ASP.NET Core Identity user that authenticates against a specific
tenant host. Has one tenant role: `owner`, `manager`, `cashier`, or
`station_device`. Lives only in that tenant's database.

### Station Device Identity

A non-Identity identity type used by station boards. The concrete auth
mechanism is deferred until hardware selection; see
[`../explanation/concepts/authorization.md`](../explanation/concepts/authorization.md).

## Customer Session Model

### QR Token

A short-lived, single-use join proof embedded in the table QR code.
Becomes invalid the moment it is consumed. Details in
[`../explanation/concepts/customer-session-model.md`](../explanation/concepts/customer-session-model.md).

### Table Session

The canonical live customer session for one table. Starts when the
first fresh QR is consumed; ends when the store closes the check.
Multiple browsers may attach to one table session.

### Access Ticket

One browser participant's attachment to a table session. Carried in a
first-party `HttpOnly` cookie. Not a hard device identity; refresh and
private tabs can produce new access tickets.

### Server-Side Cart

The customer's current cart lines, stored in
`customer_session_cart_items` and bound to an access ticket and table
session. Not stored in `localStorage` or the access cookie.

### Checkout Proof

A second, fresh QR token that must accompany every order submission.
Verified and consumed inside `POST /api/public/orders`; there is no
separate verify endpoint.

## Catalog And Fulfillment

### Station

A fulfillment unit (kitchen, bar, barista, hookah, dessert, dispatch).
Products route to stations; one order may split across several
stations.

### Fallback Station

The one station every tenant must declare as the catch-all when product
routing does not resolve explicitly. Ensures items never disappear
operationally because of a routing gap.

### Order

The submitted result of converting a server-side cart into persisted
order lines, attributed to a table session and an access ticket.
Carries an `order.submitted` event on the in-process event bus so staff
surfaces react immediately.

### Bill

The open or closed customer check for a table session. The v1.0.0 UI
exposes close bill only. Move, merge, and split are future bill-lifecycle
capabilities and must not appear as available actions in the first
release UI.

## Device Layer

### Table Device

The ESP32 display at each table that renders the current QR matrix.
Connects to the tenant host over the device WebSocket. Described in
[`./firmware.md`](./firmware.md).

### Device Key

The long-lived shared secret stored on a table device. Compared against
the tenant's stored `device_key_hash` using constant-time comparison.
Rotatable.

### Device WebSocket

`wss://<tenant-domain>/ws/tables/{tableNumber}?deviceKey=...`. One
connection per table. Delivers `new_token`, `refresh`, and heartbeat
messages to the device.

## Infrastructure

### In-Process Event Bus

A `Channel<T>`-backed publish and subscribe mechanism inside a tenant
host. Used for events such as `order.submitted`,
`order.status_changed`, and `bill.mutated`. Scoped to one tenant host
process; no cross-host broker is used.

### Provisioning Job

A row in the platform database that represents an ongoing tenant
lifecycle operation such as `tenant.create`. Claimed and advanced by
the platform worker; visible from the platform admin console.

### Tenant Audit Log

A per-tenant append-only log of significant actions (login, role
changes, bill mutations, station device pair and revoke events).
Stored in `{tenant}_audit_log`; reviewable from the tenant admin
console.

### Platform Audit Log

The platform-level equivalent: records tenant CRUD, platform user
changes, and provisioning job triggers.

## Runtime And UI

### Render Mode

The Blazor rendering strategy a component uses: Static SSR or
Interactive Server. Full reasoning in
[`./architecture/render-modes.md`](./architecture/render-modes.md).

### Static SSR

Blazor's server-side rendering without a persistent connection. Each
request returns a full HTML document plus enhanced navigation and
enhanced forms, which intercept same-origin navigation and form
submission to patch the DOM without a client-side interactive
runtime. Used for every customer-facing surface.

### Interactive Server

Blazor's interactive mode where component state lives in the host
process and a SignalR circuit streams events and DOM diffs between
client and server. Used for every staff surface. Each connected
client costs one WebSocket and one server-side circuit; see
capacity notes in
[`./architecture/render-modes.md`](./architecture/render-modes.md).

### Enhanced Navigation

The Blazor mechanism that intercepts same-origin links in a Static
SSR page, fetches the next page, and patches the DOM in place
instead of triggering a full browser navigation. No SignalR circuit
required.

### Enhanced Forms

The Blazor mechanism that intercepts Static SSR form submissions,
posts them in the background, and patches the response into the DOM
in place of a full navigation. No SignalR circuit required.

### Circuit

A Blazor Interactive Server connection between one client and the
server: one WebSocket, one server-held component tree, one
per-client state set. Circuits reconnect after brief network loss;
they are re-established after long loss, which re-runs component
lifecycles.

### Fresh QR

A QR token generated by the tenant host and displayed by the table
device that has not yet been consumed and has not yet expired. Join
requires a fresh QR; order submission requires a **second** fresh
QR as checkout proof.

### Access Cookie

The first-party `HttpOnly` cookie that carries a customer browser's
access ticket after a successful fresh-QR join at `/g/{token}`.
Scoped to the tenant host; becomes invalid when the parent table
session closes.

### Workspace

A runtime surface that a staff role occupies for long-form work
(the floor and cash workspace, the PDA workspace, the station
board). Distinct from a "console" surface, which is typically a
CRUD screen occupied briefly.

## Process And Culture

### One-Way Door

A decision whose reversal is expensive or destructive — typically
schema migrations on production data, public API contracts, persistent
file layouts, cross-tenant boundaries, irreversible data deletions.
One-way decisions require an ADR and a peer review per
[`../constitution.md`](../constitution.md) Section I.

### Two-Way Door

A decision whose reversal is cheap — typically internal helpers,
unreleased UI changes, build scripts. Two-way decisions are tried,
measured, and reverted if wrong. When in doubt, a decision is treated
as one-way.

### Conventional Fit

The degree to which a change follows broadly accepted, current, and
recognisable practice for its problem type, framework, and audience.

### Global Fit

The degree to which a locally-correct change also agrees with the
product's wider contracts, naming, structure, architecture, and
operational model.

### Strongest Nearby Example

The best already-available reference point in the repository,
framework guidance, or adjacent product surface that solves a similar
problem more clearly, more conventionally, or with lower complexity.

### Spike

A time-boxed exploration of an unknown. A spike has a stated question,
a budget in hours or days, and an expected artefact: an ADR, a tracer
bullet, or a documented "no". Spike outcomes that don't become an ADR
land in [`/doc/buildlog/spikes/`](/doc/buildlog/) so the question and answer
are preserved.

### Tracer Bullet

The thinnest end-to-end slice of a new capability — real request, real
database, real event, real test. Hardening (error paths, observability,
performance, accessibility) follows once the slice works end-to-end.
Per [`../constitution.md`](../constitution.md) Section II.

### Tech Debt

Any temporary or compromised work that is acknowledged as not the
final shape. Tech debt is recorded in
[`/doc/buildlog/tech-debt-ledger.md`](/doc/buildlog/tech-debt-ledger.md) with a
`TD-NNNN` identifier, an owner, and a payoff plan. The ledger is
append-only.

### Architectural Change

Any change to a public contract: HTTP route, event payload, DB column,
config key, runtime surface, or accepted ADR. Architectural changes
land in documentation first or alongside the code, never after, per
[`../constitution.md`](../constitution.md) Section III.

### Stop The Line

The state when `main` is broken — build red, smoke test red,
release-gate red, or a production incident open. While the line is
stopped, no contributor starts new work; the team converges on the fix
until `main` is green again. Per
[`../constitution.md`](../constitution.md) Section VI.

### Working Mode

The declared mode of a work item: `documentation`, `implementation`, or
`review`. A work item has one primary mode and can list secondary modes
when it intentionally crosses modes. Per
[`../constitution.md`](../constitution.md) Section VIII.

### Documentation Mode

A working mode whose output is a source-of-truth document or a documented
deletion. It starts with the documentation charter's decision test.

### Implementation Mode

A working mode whose output is code plus the tests, observability, and
documentation required by `Done`. It starts from the governing ADR,
reference document, acceptance criterion, capability-matrix row, and
tech-debt entry.

### Review Mode

A working mode whose output is actionable findings or an explicit approval
against the review policy. It starts with risk: correctness, security,
contract drift, missing tests, missing observability, missing
documentation, and untracked technical debt.

### Done

A capability is *done* when it is tested, observable, and documented.
All three. None is optional. The capability matrix in
[`./architecture/capability-matrix.md`](./architecture/capability-matrix.md)
honours this definition for the `Implemented` status.

### DORA Metrics

Four metrics from the *Accelerate* DevOps research that summarise
delivery health: deployment frequency, lead time from commit to
production, change failure rate, and mean time to recovery. Reported
at every release-gate review per
[`../constitution.md`](../constitution.md) Section VII.

### Release Gate

The last-stop verification a candidate commit MUST pass before being
tagged for release. The full checklist lives in
[`../meta/release-gate.md`](../meta/release-gate.md). A single
unchecked gate item blocks the release.

### Capability Matrix

The single document that tracks the implementation status of every
capability against the baseline architecture. Lives in
[`./architecture/capability-matrix.md`](./architecture/capability-matrix.md).
A capability is `Implemented` only when all three Done criteria
(tested, observable, documented) hold.

### Deprecation Window

A stated period during which a deprecated stable contract remains
addressable in parallel with its replacement. Constitution III.4
requires that breaking a stable reference comes with a deprecation
window stated in the same PR that introduces the replacement.

### Smoke Check

A short, end-to-end verification run after a deploy or as part of the
release gate. It confirms the most basic invariants (process up,
health probes pass, key surface returns 200) without exercising the
full test suite.

### Tenant.Create Job

The provisioning job type emitted to the platform database when a new
tenant is being created. Claimed by the platform worker; runs the
provisioning steps documented in
[`../how-to/provision-tenant.md`](../how-to/provision-tenant.md).

### English-First

The principle that internal contracts (code identifiers, schema,
HTTP surfaces, ADRs, glossary, log keys, audit `event_key` values) are
written in English only, while user-facing text is translated at the
presentation layer. AD-0015 records the decision; the
[internationalization explainer](../explanation/concepts/internationalization.md)
explains where each kind of string lives.

### RTO

Recovery Time Objective. The maximum acceptable wall-clock time from
declaring an incident to restoring service. Targets per scope live in
[`./architecture/slos.md`](./architecture/slos.md#recovery-objectives).

### RPO

Recovery Point Objective. The maximum acceptable data loss measured
from the last consistent recovered state to the moment of failure.
Targets per scope live in
[`./architecture/slos.md`](./architecture/slos.md#recovery-objectives).

### Recovery Drill

A real restore performed against a non-production target to verify
that the documented backup and restore procedures actually meet the
RTO and RPO targets. Run quarterly per
[`../how-to/backup-and-restore.md`](../how-to/backup-and-restore.md#quarterly-recovery-drill).

### KVKK

Kişisel Verilerin Korunması Kanunu (Law No. 6698 of the Republic of
Türkiye), the Turkish data-protection regulation. TabFlow's coverage
of KVKK obligations is documented in the
[data-protection explainer](../explanation/concepts/data-protection.md).

### GDPR

General Data Protection Regulation (Regulation (EU) 2016/679), the
European Union data-protection regulation. TabFlow's coverage of GDPR
obligations is documented in the
[data-protection explainer](../explanation/concepts/data-protection.md).

### Data Class

The personal-data classification of a field: `Public`, `Internal`,
`Sensitive`, or `Restricted`. Storage, access, and audit requirements
follow from the class. See
[`../explanation/concepts/data-protection.md`](../explanation/concepts/data-protection.md#data-classification).

### Data Controller

Under KVKK and GDPR, the party who decides why and how personal data
is processed. In TabFlow, this is the tenant (the cafe operator).

### Data Processor

Under KVKK and GDPR, the party who processes personal data on behalf
of a controller. In TabFlow, this is the operator running the
platform.

### Test Tier

One of `Unit`, `Integration`, `E2E`, or `Smoke`. Each tier has a
defined scope, time budget, and execution context per
[`../explanation/concepts/test-taxonomy.md`](../explanation/concepts/test-taxonomy.md).

## Documentation

### ADR

Architectural Decision Record. Each ADR has an ID (`AD-0001`,
`AD-0002`, …) and states context, decision, and consequences. The
current set lives in
[`./architecture/decisions.md`](./architecture/decisions.md).

### Surface ID

The stable identifier for a runtime surface used across documents.
Platform surfaces use `P-##`; tenant surfaces use `T-##`; device
endpoints use `D-##`. Declared in
[`./architecture/runtime-surfaces.md`](./architecture/runtime-surfaces.md).

### SLI / SLO

Service-Level Indicator (a measurable signal) and Service-Level
Objective (a target over a rolling window). Targets live in
[`./architecture/slos.md`](./architecture/slos.md).

## Related

- [`./architecture/runtime-surfaces.md`](./architecture/runtime-surfaces.md)
- [`./architecture/system-overview.md`](./architecture/system-overview.md)
- [`./architecture/decisions.md`](./architecture/decisions.md)
- [`./database/schema.md`](./database/schema.md)
