# Runtime Surfaces

This document is the reference map for every runtime surface served by the
platform host and the tenant host.

It is the authoritative runtime topology for the repository and the
document every other reference reads back into. When a route, role, render
mode, or runtime anchor is listed somewhere else in the docs, it is a
pointer to this file.

## Hosts

TabFlow runs two host shapes:

- **Platform host** — one process, serves the control-plane admin surface
  and the platform health probes. Example: `https://admin.example.com`.
- **Tenant host** — one process per tenant, serves every tenant-facing
  surface and the ESP32 device endpoint. Example:
  `https://<tenant-domain>`.

Each host is a Blazor Web App backed by ASP.NET Core 10. See
[`./system-overview.md`](./system-overview.md) for the host shape and
[`./decisions.md`](./decisions.md) for the underlying decisions.

## Roles

### Platform Host

| Role | Scope |
| --- | --- |
| `owner` | Platform owner. Full control including role assignment. |
| `admin` | Platform admin. Tenant lifecycle, audit, provisioning. Cannot edit platform owners. |
| `viewer` | Read-only access to platform dashboards and audit. |

### Tenant Host

| Role | Scope |
| --- | --- |
| `owner` | Tenant owner. Full control including role assignment. |
| `manager` | Tenant administrator. Menu, floor layout, stations, staff users below owner, reports. |
| `cashier` | Live service floor and cashier surfaces. Orders, close bill, table operations. |
| `station_device` | A single station terminal. Station-scoped fulfillment board only. |

The authorization model and the station-device identity decision are
described in
[`../../explanation/concepts/authorization.md`](../../explanation/concepts/authorization.md).

## Route Map

Route IDs are assigned sequentially within each family for readability.
They are stable references across the docs tree.

Render-mode column values:

- `static` — Blazor Static SSR with enhanced forms and navigation. No
  SignalR connection is opened.
- `interactive` — Blazor Interactive Server. Component state lives in the
  host process and is synchronized over SignalR.

### Platform Host

| ID | Route | Roles | Render mode | Purpose |
| --- | --- | --- | --- | --- |
| P-01 | `/login` | anonymous | static | Platform admin sign-in |
| P-02 | `/` | policy `Platform:Read` (`owner`, `admin`, `viewer`) | interactive | Dashboard, tenant summary, latest jobs |
| P-03 | `/tenants` | policy `Platform:Read` | interactive | Tenant list and filters |
| P-04 | `/tenants/new` | policy `Platform:Write` (`owner`, `admin`) | interactive | Create tenant |
| P-05 | `/tenants/{id}` | policy `Platform:Read` | interactive | Tenant detail, status, regional settings, jobs |
| P-06 | `/jobs` | policy `Platform:Read` | interactive | Provisioning jobs |
| P-07 | `/audit` | policy `Platform:Read` | interactive | Platform audit log |
| P-09 | `/settings` | policy `Platform:Self` | interactive | Platform user preferences and password controls |
| P-08 | `/change-password` | policy `Platform:Self` (any authenticated platform user) | static | Password change |

### Tenant Host — Public

| ID | Route | Roles | Render mode | Purpose |
| --- | --- | --- | --- | --- |
| T-01 | `/` | anonymous | static | Welcome / route shell |
| T-02 | `/menu` | customer session | interactive today; target static | Customer menu and add-to-cart flow |
| T-03 | `/g/{token}` | anonymous | target static | QR token verification, table-session bootstrap, access-cookie issue; not yet implemented as a route |
| T-17 | `/cart` | customer session | interactive today; target static | Customer cart and checkout submission |
| T-18 | `/scan-qr` | customer session | interactive today; target static | Camera-assisted customer session open flow |
| T-19 | `/order-complete/{orderId}` | customer session | static | Customer order completion confirmation |

### Tenant Host — Authentication

| ID | Route | Roles | Render mode | Purpose |
| --- | --- | --- | --- | --- |
| T-04 | `/login` | anonymous | static | Tenant identity sign-in |
| T-04a | `/login-2fa` | anonymous | static | Tenant two-factor challenge |
| T-04b | `/activate` | anonymous | static | Tenant admin activation |
| T-05 | `/change-password` | any tenant user | static | Password change |

### Tenant Host — Console

| ID | Route | Roles | Render mode | Purpose |
| --- | --- | --- | --- | --- |
| T-06 | `/settings` | policy `Tenant:Self` | interactive | Tenant user preferences and password controls |
| T-07 | `/tables` | policy `Tenant:Read` (`owner`, `manager`) | interactive | Floor layout and table setup/configuration |

### Tenant Host — Operations

| ID | Route | Roles | Render mode | Purpose |
| --- | --- | --- | --- | --- |
| T-08 | `/service` | policy `Tenant:Read` (`manager`, `cashier`) | interactive target | Live floor and cash workspace |
| T-09 | `/table/{tableNumber}` | policy `Tenant:Read` (`manager`, `cashier`) | interactive | Per-table service workspace |
| T-13 | `/order/{orderId}` | policy `Tenant:Read` | interactive today | Staff order detail view |

### Tenant Host — Station

| ID | Route | Roles | Render mode | Purpose |
| --- | --- | --- | --- | --- |
| T-15 | `/kitchen` | policy `Tenant:Read` (`owner`, `manager`) | interactive | All-station kitchen overview |
| T-16 | `/stations/{stationCode}` | policy `Tenant:Read` (`manager`, `station_device`) | interactive target | Station-scoped fulfillment queue |

### Tenant Host — Device Endpoint

| ID | Route | Auth | Purpose |
| --- | --- | --- | --- |
| D-01 | `wss://<tenant-domain>/ws/tables/{tableNumber}?deviceKey={deviceKey}` | Device key | ESP32 token push and rotation |

<a id="tenant-host--http-endpoints"></a>

### Tenant Host — HTTP Endpoints

The tenant host carries two tiers of HTTP endpoint: customer-tier
(anonymous, called from the Static SSR customer surfaces and the
table device) and staff-tier (authenticated, called from the
Interactive Server staff surfaces). The two tiers are separated by
authorisation (controller-level `[AllowAnonymous]` vs
`[Authorize(Policy = "Tenant:Read|Write")]`) per TD-0015 step 2. The
canonical customer tier is mounted under `/api/public/*`; the legacy
`/api/menu`, `/api/cart`, and customer slice of `/api/sessions/*` remain
only for the temporary compatibility window tracked by
[TD-0021](/doc/buildlog/tech-debt-ledger.md#td-0021).

| Group | Routes | Tier | Purpose |
| --- | --- | --- | --- |
| Health | `GET /health`, `GET /health/live`, `GET /health/ready` | n/a | Probes (anonymous, AC-101). |
| Customer order submission | `POST /api/public/orders` | customer | Submission gate per AC-030..AC-036; routed by `PublicOrdersController`. |
| Customer menu read | `GET /api/public/catalog`, `GET /api/public/catalog/category/{categoryId}` | customer | Catalog reads via `PublicCatalogController`. Legacy `/api/menu*` remains temporarily under [TD-0021](/doc/buildlog/tech-debt-ledger.md#td-0021). |
| Customer cart mutation | `POST /api/public/cart`, `DELETE /api/public/cart/{id}`, `PUT /api/public/cart/{id}/quantity`, `GET /api/public/cart/session/{sessionId}` | customer | Cart manipulation against an open access ticket via `PublicCartController`. Legacy `/api/cart*` remains temporarily under [TD-0021](/doc/buildlog/tech-debt-ledger.md#td-0021). |
| Customer session lifecycle | `POST /api/public/session/open`, `GET /api/public/session/{ticketId}` | customer | QR-token consumption + access-cookie issue, and ticket state read via `PublicSessionController`. Legacy customer actions on `/api/sessions/*` remain temporarily under [TD-0021](/doc/buildlog/tech-debt-ledger.md#td-0021). |
| Staff session close | `POST /api/sessions/{sessionId}/close` | staff | Closes a customer session. `[Authorize(Policy = "Tenant:Write")]`. AC-044. |
| Staff orders read | `GET /api/orders/{id}`, `GET /api/orders/session/{sessionId}` | staff | Read-only views for the floor and cash workspace. `[Authorize(Policy = "Tenant:Read")]`. |
| Staff kitchen board | `GET /api/kitchen/orders`, `PUT /api/kitchen/items/{id}/status` | staff | Station-board reads and item-state transitions. The mutation action carries `[Authorize(Policy = "Tenant:Write")]` per AC-052. |
| Staff tables | `GET /api/tables`, `GET /api/tables/{id}`, `POST /api/tables`, `PUT /api/tables/{id}`, `DELETE /api/tables/{id}`, `GET /api/tables/{id}/workspace`, `POST /api/tables/{id}/checkout-proof` | staff | Floor layout read/write, workspace snapshot, and checkout proof issuance. Mutations carry `Tenant:Write`; reads inherit `Tenant:Read`. |

The staff-tier HTTP endpoints above are deliberately exposed (rather
than running through Blazor application services) because the staff
Blazor pages call them via `HttpClient` rather than through DI; this
is an internal implementation choice. They are still "administrative
HTTP endpoints" in the sense of AD-0003 and are not
considered part of the public contract surface. Administrative
mutations served from Blazor components without an HTTP boundary
remain the architectural target.

## Shared Runtime Language

All tenant runtime surfaces speak the same operational language.

Order state:

- `submitted`
- `preparing`
- `ready`
- `served`
- `cancelled`

Operational anchors, consumed on every staff surface:

- table number
- order id
- item name
- quantity
- notes
- station
- open-check status
- device or QR health
- timing or elapsed time

Ticket-card anchors on T-08 / T-09 (floor and cash) and T-15 / T-16
(station board):

- table number
- order id
- item name and quantity
- item note and order note
- elapsed time

Urgency bands on T-15 / T-16:

- 0–3 minutes: normal
- 3–7 minutes: warning
- 7+ minutes: urgent

## Real-Time Event Bus

The tenant host publishes domain events to an in-process event bus after
the originating transaction commits. Interactive Server components
subscribe and re-render without polling. See
[`./decisions.md`](./decisions.md) AD-0006.

| Event | Published by | Consumed by |
| --- | --- | --- |
| `order.submitted` | Customer order submit, waiter order submit | T-08 / T-09 floor and cash, T-16 station board |
| `order.status_changed` | Station board status transitions, waiter actions | T-08 / T-09, T-16, relevant PDA views |
| `bill.opened` | Order submission opens a new bill on a table | T-08 / T-09 |
| `bill.closed` | Cashier close-bill operation | T-08 / T-09 |
| `table.opened`, `table.closed` | QR join flow, cashier actions | T-08 / T-09 |
| `device.connected`, `device.disconnected` | ESP32 WebSocket lifecycle | T-06 dashboard, T-08 / T-09 table cards |

Event types stay a closed enumeration. New events are added through a
small commit that covers the event record, publication point, and
subscriber surfaces in one change.

## Surface Notes

### Platform Admin

Purpose:

- Tenant lifecycle oversight
- Provisioning visibility
- Audit

Baseline navigation:

- `Overview`
- `Tenants`
- `Jobs`
- `Audit`

### Tenant Admin Console (T-06 through T-12)

Purpose:

- Setup and governance
- Menu, stations, floor layout, users, firmware defaults, audit
- Exception surface-up

Baseline navigation:

- `Overview`
- `Catalog`
- `Stations`
- `Tables`
- `Users`
- `Firmware defaults`
- `Audit`

Overview expectations:

- top band for active tables, open checks, ready orders, offline devices
- attention queue for fallback-station items, unhealthy devices, delayed
  stations
- station health panel
- quick setup actions for stations, catalog coverage, and devices

Station management expectations:

- station cards show name, code, color, type, active state, product
  count, operator count, and fallback status
- station detail supports reorder, disable, fallback selection, and
  product coverage review
- product routing is item-level; category-level routing may act as a
  default helper

### Floor And Cash Workspace (T-08 and T-09)

Purpose:

- Live table service
- Checkout-proof issuance
- Close-bill flow
- Ready-order awareness

`/tables` is not the live service workspace. It is the table
setup/configuration surface for tenant owner and manager work. The live
service route is `/service`, with `/table/{tableNumber}` as the
per-table workspace.

v1.0.0 exposes close bill as the only bill lifecycle action. Move,
merge, and split are outside the first-release UI contract.

Baseline views:

- `Floor`
- `Open checks`
- `Payment queue`
- `Closed checks`

Primary mental model:

- one workspace reveals both physical floor flow and bill and payment
  flow
- operators should not need to jump between a decorative floor planner
  and a separate cashier screen to understand live table state

Table-card anchors:

- table number
- occupancy state
- open-check presence
- order intensity
- ready-to-serve signal
- device or QR health

Primary actions from a selected table:

- mark payment received
- close check
- inspect live order detail

Interaction principles:

- normal mode is operational
- layout editing is explicit and separate
- close-bill actions are quick but still deliberate
- closing a check requires a stronger confirmation than normal table
  selection

### Station Board (T-15 and T-16)

Purpose:

- Station-scoped fulfillment
- All-station manager overview
- Fast ticket progression
- Urgency visibility
- Order progress source of truth

Station operators land on `/stations/{stationCode}` and see only the
queue they can act on. `/kitchen` is the all-station overview for
managers and cross-station operators.

Station status transitions drive customer-visible order progress:
`submitted`, `preparing`, `ready`, `served`, and `cancelled`.

Variants include kitchen, barista, bar, hookah, fastfood, and dispatch
stations. The ticket-card anchors and urgency bands live in
[Shared Runtime Language](#shared-runtime-language).

Status columns:

- `new`
- `preparing`
- `ready`

Operator actions:

- start preparing
- mark ready
- mark remake or rework
- cancel when authorized

Visual direction:

- high contrast
- dark board background
- large timers and action buttons
- readable from distance and under pressure

### Waiter / Mobile PDA

Purpose:

- Mobile, table-side order taking
- One-handed use

Direction:

- quick table selection
- fast note entry
- minimal navigation chrome
- protected actions run under the waiter's authenticated tenant
  identity, not through a customer QR session

### Customer Ordering (T-01, T-02, T-03, T-17, T-18, T-19)

Purpose:

- QR landing through `/g/{token}`
- Customer browsing
- Open-check visibility
- Order composition
- Order progress after submission

Security direction:

- browsing remains lightweight while the access ticket stays valid
- order submission remains the critical security boundary and requires a
  fresh QR checkout proof

Customer surfaces are Static SSR. They do not open a SignalR connection.
The cart lives on the server, bound to the table session. See
[`../../explanation/concepts/customer-session-model.md`](../../explanation/concepts/customer-session-model.md).

## Station-First Fulfillment

TabFlow is station-first rather than kitchen-only.

- Products route to stations.
- Stations are the fulfillment unit.
- One order may split operationally across different stations.
- Admins may view all stations. Station-device identities are scoped to a
  single station.

Each tenant maintains one fallback station so routing failures do not
hide items operationally. Item-level station assignment is the final
routing source. Category-level station assignment may remain as a default
helper only.

## Floor Layout Model

Floor and cash operation is not a flat table list.

- One tenant may own multiple layouts (main floor, balcony, upper floor,
  garden, dispatch, takeaway).
- Each layout may own multiple zones.
- Tables hold placement metadata per layout.
- Fixed floor objects (cashier bank, entrance, kitchen pass) act as
  edit-friendly anchors rather than runtime order or billing entities.

Edit mode is explicit and separate from normal operations mode.
Placement records hold coordinates, size, shape, rotation, and z-order
per layout.

## Station-Device Access

Station operators access T-16 `/stations/{stationCode}`. T-15
`/kitchen` is the all-station manager overview.

The authentication pattern for the `station_device` role is still open
and depends on the station hardware decision. The identity
abstraction is stable: a `station_device` role exists, its routes are
authorization-protected, and the rest of the stack is written against
that abstraction.

Among the candidates listed in
[`../../explanation/concepts/authorization.md`](../../explanation/concepts/authorization.md),
a pairing-code plus device-cookie flow is the most hardware-independent
option: it works on any device with a browser and a cookie store, which
covers every plausible station terminal short of a fully custom firmware.
The final decision still depends on the hardware class, but that
candidate is the safe default if the project has to land something
before the hardware is chosen.

## Web Indexing Posture

TabFlow has no outward marketing surface. Every HTML route on every
host is an operational surface served to a specific authenticated or
table-bound visitor, so none of them MUST appear in a search index.

- Every HTML response MUST carry
  `X-Robots-Tag: noindex, nofollow, noarchive`.
- Every HTML document MUST carry
  `<meta name="robots" content="noindex,nofollow,noarchive">`.
- `GET /robots.txt` on every host MUST return a fully-disallowed
  policy for all user agents.
- A `sitemap.xml` listing application routes MUST NOT be served.
- **Accessibility is table stakes**, not ranked. Interactive
  Server staff surfaces and Static SSR customer surfaces MUST meet the
  baseline described in
  [`../../explanation/concepts/accessibility.md`](../../explanation/concepts/accessibility.md).
- Beyond accessibility, the priority order for competing non-functional
  concerns is **security > privacy > performance**. Search-engine
  visibility is intentionally absent.

This posture is verified in the release gate
([`../../meta/release-gate.md`](../../meta/release-gate.md)) and is
tracked as acceptance criteria AC-090 to AC-092 in
[`../acceptance-criteria.md`](../acceptance-criteria.md).
