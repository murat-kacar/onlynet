# Frontend/UI Implementation Plan

## Purpose

This document turns the accepted frontend/UI alignment review into the
first implementation sequence.

It is the handoff from documentation mode to implementation mode. It does
not replace the route map, design system, or stakeholder analysis. Those
remain the sources of truth.

Related documents:

- [`./ui-stakeholder-surface-analysis.md`](./ui-stakeholder-surface-analysis.md)
- [`./frontend-ui-alignment-review.md`](./frontend-ui-alignment-review.md)
- [`../../reference/architecture/runtime-surfaces.md`](../../reference/architecture/runtime-surfaces.md)
- [`../../reference/design-system.md`](../../reference/design-system.md)
- [`../../reference/frontend-rewrite-charter.md`](../../reference/frontend-rewrite-charter.md)

## Accepted Implementation Decisions

These decisions are binding for the first frontend/UI implementation
slice.

| ID | Decision | Impact |
| --- | --- | --- |
| FE-UI-001 | `/service` uses a table-grid center surface. | The live floor and cash workspace starts from table state, not an order-only list or kanban. |
| FE-UI-002 | `/table/{tableNumber}` behaves as a deep link into `/service`. | It opens the same service surface with the addressed table selected and its details shown in the right inspector. It is not a separate page experience. |
| FE-UI-003 | Table CRUD and live table actions are inspector-led. | Selecting a table opens table detail in the right panel. Actions appear according to permission. Large create/edit tasks may expand into drawers; confirmations use modals. |
| FE-UI-004 | v1 station planning assumes two station codes: `kitchen` and `bar`. | `/stations/kitchen` and `/stations/bar` establish the station-scoped pattern. `/kitchen` remains the manager all-station overview. |
| FE-UI-005 | v1 visible bill lifecycle remains close-only. | The service inspector exposes close-bill affordances only. Move, merge, and split are not visible controls. |

## Surface Contracts

### `/tables`

Audience:

- tenant owner
- tenant manager

Purpose:

- table setup and configuration
- table identity, label, code, color, type, active state, sort order
- device/setup readiness

Must not own:

- live cashier workflow
- close-bill workflow
- station fulfillment
- customer order progress

Interaction pattern:

- center: table setup table or grid
- right inspector: selected setup record
- drawer: create/edit table
- modal: destructive confirmation

### `/service`

Audience:

- manager
- cashier
- floor staff where authorized

Purpose:

- live floor and cash workspace
- table state at a glance
- open session, cart, submitted orders, bill state, checkout proof,
  ready items, elapsed time, and attention flags
- close bill

Center surface:

- table grid
- each table card shows table identity, service state, open bill/session
  state, ready-item signal, and attention state
- filters may narrow by state, but the default view remains table-first

Right inspector:

- selected table detail
- current session summary
- current bill summary
- cart preview
- submitted order summary
- checkout-proof action where allowed
- close-bill action where allowed
- device/QR health where available

Overlay behavior:

- close-bill confirmation uses a modal
- larger create/edit tasks use drawers
- basic inspection stays in the right panel

### `/table/{tableNumber}`

Audience:

- manager
- cashier
- floor staff where authorized

Purpose:

- link directly to one table's live service context

Behavior:

- loads `/service` semantics
- selects the addressed table
- opens the table's details in the right inspector
- does not introduce separate page chrome or a separate interaction model

Invalid or inactive table behavior:

- the service surface still renders
- the right inspector shows a clear missing/inactive-table state
- the center grid remains available for recovery

### `/stations/{stationCode}`

Audience:

- station operator
- station device
- manager when acting in station mode

v1 station codes:

- `kitchen`
- `bar`

Purpose:

- focused station-scoped fulfillment queue
- progress items through station-owned states
- prevent station users from acting on unrelated work

Center surface:

- station queue grouped by urgency and order/table context
- each item shows item name, quantity, note, table, order reference,
  elapsed time, and current status

Right inspector:

- selected item or selected order detail
- allowed status actions
- note and timing context
- related items only when they help avoid mistakes

Must not own:

- all-station manager overview
- bill actions
- table setup

### `/kitchen`

Audience:

- owner
- manager
- cross-station operator where authorized

Purpose:

- all-station operational overview
- station filters and attention summary
- urgent or blocked items across `kitchen` and `bar`

Must not be:

- the default focused station route
- the only route for fulfillment work

## Role And Permission Direction

The first implementation slice must separate product roles from broad
read/write capability.

Target product roles:

| Role | Primary surfaces |
| --- | --- |
| `owner` | setup, overview, governance, all-station overview |
| `manager` | setup, `/service`, `/kitchen`, `/stations/{stationCode}` |
| `cashier` | `/service`, `/table/{tableNumber}`, close bill |
| `station_device` | `/stations/{stationCode}` only |

Route visibility is a usability feature. It does not replace server-side
authorization. Every mutation still needs a server-side policy.

## First Implementation Slice

The first slice is the thinnest useful route and shell alignment that
exercises the real UI pattern.

### Slice 1: Service Deep-Link Tracer Bullet

Goal:

- introduce `/service`
- make `/table/{tableNumber}` select a table inside the same service
  model
- prove table-grid center plus right inspector behavior

Includes:

- `/service` route
- table-grid center surface
- selected table state
- right inspector table detail
- `/table/{tableNumber}` deep-link selection behavior
- permission-aware action visibility
- localized labels for new user-facing strings

Excludes:

- new visual design polish beyond the existing design-system direction
- bill move, merge, or split
- customer order progress
- station routes
- frontend package changes

Done means:

- direct navigation to `/service` works
- direct navigation to `/table/{tableNumber}` opens the same service
  surface with that table selected
- selecting another table changes the right inspector without a route jump
- cashier-facing live actions are not shown on `/tables`
- tests cover route access and selection behavior where practical

### Slice 2: Close-Bill Language And Action Boundary

Goal:

- replace visible close-session checkout language with close-bill
  language and flow

Includes:

- close-bill copy in English and Turkish
- close-bill confirmation modal contract
- permission-aware visibility in the service inspector
- no visible move/merge/split controls

Done means:

- operator UI says close bill, not close session, for checkout
- action context identifies table and total
- unauthorized users cannot see or execute the mutation

### Slice 3: Station-Scoped Queue

Goal:

- introduce station-scoped fulfillment with two v1 station codes

Includes:

- `/stations/kitchen`
- `/stations/bar`
- station-scoped queue
- station title in the fixed top bar
- right inspector for selected item/order
- status actions allowed by station role

Done means:

- station route does not show unrelated station work by default
- `/kitchen` remains manager overview
- station scope is enforced by backend query or policy, not only by UI

### Slice 4: Customer Progress

Goal:

- make post-submit customer status visible

Includes:

- order progress state on `/order-complete/{orderId}` or a linked
  order-progress surface
- customer-safe status language
- Turkish and English copy

Done means:

- customer can see `submitted`, `preparing`, `ready`, `served`, or
  `cancelled`
- staff-only notes and internals are not exposed
- stale or unauthorized access fails safely

## Design Guardrails

- Keep operator surfaces inside the `Three-Pane Operational Console`.
- Selection detail belongs in the right inspector.
- Drawers start larger create/edit tasks.
- Modals confirm destructive, financial, or compact interrupting tasks.
- Do not add a route when selection plus inspector is enough.
- Do not put live service work back into `/tables`.
- Do not expose future bill controls in the v1 UI.
- Design English and Turkish strings together.

## Open Implementation Notes

- `kitchen` and `bar` are the v1 station codes unless a tenant-specific
  setup model later overrides station naming.
- `/table/{tableNumber}` may update the browser URL for shareability, but
  the rendered experience remains the service surface.
- The existing runtime route map remains authoritative. If code needs a
  different route shape, update the route map first.
