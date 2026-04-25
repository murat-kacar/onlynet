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
| `cashier` | Service floor and cashier surfaces. Orders, bills, table operations. |
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
| P-07 | `/audit` | policy `Platform:Write` | interactive | Platform audit log |
| P-08 | `/change-password` | policy `Platform:Self` (any authenticated platform user) | static | Password change |

### Tenant Host — Public

| ID | Route | Roles | Render mode | Purpose |
| --- | --- | --- | --- | --- |
| T-01 | `/` | anonymous | static | Welcome and QR prompt |
| T-02 | `/menu` | customer session | static | Customer menu, cart, order submission |
| T-03 | `/g/{token}` | anonymous | static | QR token verification, table-session bootstrap, access-cookie issue |

### Tenant Host — Authentication

| ID | Route | Roles | Render mode | Purpose |
| --- | --- | --- | --- | --- |
| T-04 | `/login` | anonymous | static | Tenant identity sign-in |
| T-05 | `/change-password` | any tenant user | static | Password change |

### Tenant Host — Console

| ID | Route | Roles | Render mode | Purpose |
| --- | --- | --- | --- | --- |
| T-06 | `/console` | `owner`, `manager` | interactive | Overview, metrics, attention queue |
| T-07 | `/console/catalog` | `owner`, `manager` | interactive | Catalog management |
| T-08 | `/console/stations` | `owner`, `manager` | interactive | Station management |
| T-09 | `/console/tables` | `owner`, `manager` | interactive | Floor layout and table management |
| T-10 | `/console/users` | `owner`, `manager` | interactive | Tenant user and role management, gated by `Console:ManageUsersBelowOwner` so managers cannot edit owner rows |
| T-11 | `/console/firmware` | `owner`, `manager` | interactive | ESP32 firmware defaults |
| T-12 | `/console/audit` | `owner`, `manager` | interactive | Tenant audit log |

### Tenant Host — Operations

| ID | Route | Roles | Render mode | Purpose |
| --- | --- | --- | --- | --- |
| T-13 | `/service` | `cashier`, `manager`, `owner` | interactive | Floor and cash workspace |
| T-14 | `/pda` | `cashier`, `manager`, `owner` | interactive | Mobile waiter workspace |

### Tenant Host — Station

| ID | Route | Roles | Render mode | Purpose |
| --- | --- | --- | --- | --- |
| T-15 | `/stations` | `station_device` | interactive | Station selection |
| T-16 | `/stations/{stationCode}` | `station_device` | interactive | Station fulfillment board |

### Tenant Host — Device Endpoint

| ID | Route | Auth | Purpose |
| --- | --- | --- | --- |
| D-01 | `wss://<tenant-domain>/ws/tables/{tableNumber}?deviceKey={deviceKey}` | Device key | ESP32 token push and rotation |

### Tenant Host — HTTP Endpoints

| Group | Routes | Purpose |
| --- | --- | --- |
| Health | `GET /health`, `/health/live`, `/health/ready` | Probes |
| Public | `GET /api/public/profile`, `GET /api/public/catalog`, `GET /api/public/session`, `POST /api/public/orders` | Customer-facing HTTP contracts. See [`../api/tenant-api.md`](../api/tenant-api.md). |

Administrative HTTP endpoints are not part of the runtime surface. Admin
and staff interactions run through Blazor components that depend on
application services directly. See [`./decisions.md`](./decisions.md)
AD-0003.

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

Ticket-card anchors on T-13 (floor and cash) and T-15 / T-16 (station
board):

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
| `order.submitted` | Customer order submit, waiter order submit | T-13 floor and cash, T-16 station board |
| `order.status_changed` | Station board status transitions, waiter actions | T-13, T-16, relevant PDA views |
| `bill.opened` | Order submission opens a new bill on a table | T-13 |
| `bill.closed`, `bill.moved`, `bill.merged`, `bill.split` | Cashier bill operations | T-13 |
| `table.opened`, `table.closed` | QR join flow, cashier actions | T-13 |
| `device.connected`, `device.disconnected` | ESP32 WebSocket lifecycle | T-06 dashboard, T-13 table cards |

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

### Floor And Cash Workspace (T-13)

Purpose:

- Table state
- Open check handling
- Manual payment flow
- Move, merge, split, and close actions

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
- move table or check
- merge tables or checks
- split check
- inspect live order detail

Interaction principles:

- normal mode is operational
- layout editing is explicit and separate
- move, merge, split, close actions are quick but still deliberate
- closing a check requires a stronger confirmation than normal table
  selection

### Station Board (T-15 and T-16)

Purpose:

- Station-scoped fulfillment
- Fast ticket progression
- Urgency visibility

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

### Waiter / Mobile PDA (T-14)

Purpose:

- Mobile, table-side order taking
- One-handed use

Direction:

- quick table selection
- fast note entry
- minimal navigation chrome
- protected actions run under the waiter's authenticated tenant
  identity, not through a customer QR session

### Customer Menu (T-01, T-02, T-03)

Purpose:

- QR landing through `/g/{token}`
- Customer browsing
- Open-check visibility
- Order composition

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

Station operators access only T-15 `/stations` and T-16
`/stations/{stationCode}`.

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
