# Frontend/UI Alignment Review

## Purpose

This document records the current frontend/UI alignment findings against
the accepted v1.0.0 UI planning contracts.

It is a review output, not an implementation plan. Code changes start only
after this review is accepted and a work item enters implementation mode.

Sources of truth:

- [`./ui-stakeholder-surface-analysis.md`](./ui-stakeholder-surface-analysis.md)
- [`../../reference/architecture/runtime-surfaces.md`](../../reference/architecture/runtime-surfaces.md)
- [`../../reference/design-system.md`](../../reference/design-system.md)
- [`../../reference/frontend-rewrite-charter.md`](../../reference/frontend-rewrite-charter.md)

## Review Lens

The current UI is reviewed against these accepted contracts:

- `/tables` is setup/configuration.
- `/service` is live floor and cash work.
- `/stations/{stationCode}` is the default station-operator queue.
- `/kitchen` is the all-station overview for managers.
- v1.0.0 exposes `close bill` only for bill lifecycle.
- Customer order progress is visible after submission and is driven by
  station workflow.
- Turkish and English are designed together.
- Operator surfaces use the `Three-Pane Operational Console`:
  left navigation, fixed top bar, center work surface, right inspector,
  and overlays for task-starting work.

## Findings

### UI-ALIGN-001: `/service` Is Missing

Severity: high.

Current state:

- The tenant app has `/tables`, `/table/{tableNumber}`, `/kitchen`,
  `/order/{orderId}`, and customer routes.
- There is no `/service` surface.
- `/tables` currently carries setup information and live service actions.

Required alignment:

- Add `/service` as the live floor and cash workspace.
- Keep `/tables` focused on table setup, configuration, active/inactive
  state, labels, sort order, color, and device/setup readiness.
- Move live table operations out of `/tables` and into `/service` or
  `/table/{tableNumber}`.

Implementation mode must verify:

- cashier and floor staff do not need setup permissions to do shift work
- manager/owner setup work does not depend on live-service affordances
- navigation makes the setup/live distinction obvious

### UI-ALIGN-002: `close session` Is Not `close bill`

Severity: high.

Current state:

- The live table drawer exposes `CloseSession`.
- The v1.0.0 contract says the visible bill lifecycle exposes
  `close bill` only.

Required alignment:

- UI language, route action, service operation, confirmation copy, and
  success feedback must use `close bill` for checkout.
- Session closure remains an internal consequence only if the domain
  model requires it.
- Move, merge, and split bill actions stay absent from v1.0.0 UI.

Implementation mode must verify:

- closing a bill is permission-gated for the correct operator roles
- the confirmation identifies the table and final total
- the action is auditable and recoverable through existing records
- customer access tickets become invalid after checkout where required

### UI-ALIGN-003: Station Scope Is Missing

Severity: high.

Current state:

- `/kitchen` shows the active kitchen board.
- Station operators do not have a `/stations/{stationCode}` route.
- The current board is not scoped to a station code.

Required alignment:

- Add `/stations/{stationCode}` for station-scoped fulfillment.
- Keep `/kitchen` as an all-station manager overview.
- Station devices and focused station users see only the queue they can
  act on by default.

Implementation mode must verify:

- station scope is enforced server-side, not only by UI filtering
- station code is visible in the title bar and route
- all-station manager overview cannot be mistaken for focused station mode

### UI-ALIGN-004: Tenant Roles Are Too Coarse For The Target UI

Severity: high.

Current state:

- UI authorization is effectively built around broad `Tenant:Read` and
  `Tenant:Write` claims.
- The product contract names `owner`, `manager`, `cashier`, and
  `station_device` responsibilities.

Required alignment:

- Route visibility and mutation visibility must follow product roles,
  not only read/write capability.
- `/tables` is visible to setup roles.
- `/service` is visible to floor/cash roles.
- `/stations/{stationCode}` is visible to station-scoped roles.

Implementation mode must verify:

- policy names and role claims are documented before code changes
- UI navigation hides irrelevant surfaces but backend policies still
  enforce access
- tests cover denied route access and denied mutations

### UI-ALIGN-005: Customer Order Progress Is Not A Real Surface Yet

Severity: medium-high.

Current state:

- `/order-complete/{orderId}` confirms submission.
- It does not read current order or item progress.
- Customer-facing copy says staff can track the order, not the customer.

Required alignment:

- Add an order-progress model after submission.
- Show station-driven states: `submitted`, `preparing`, `ready`,
  `served`, and `cancelled`.
- Keep the phone surface focused and customer-safe.

Implementation mode must verify:

- the progress read is authorized by the customer access model
- status display does not expose staff-only notes or internal identifiers
- copy is clear in both Turkish and English

### UI-ALIGN-006: Customer Flow Still Depends On Interactive Server And Local Storage

Severity: medium.

Current state:

- `/menu`, `/cart`, and `/scan-qr` are Interactive Server surfaces.
- Customer session state is stored in browser local storage.
- The target customer posture is a phone-first Static SSR flow with
  server-side access-ticket enforcement.

Required alignment:

- Move customer ordering toward the documented Static SSR target.
- Treat browser storage as temporary compatibility only.
- Keep QR/session state anchored to the server access model.

Implementation mode must verify:

- cart and session APIs require the access-cookie model
- stale local browser state cannot authorize a cart or checkout action
- any unavoidable browser code follows the TypeScript-first rule for
  non-trivial custom browser logic

### UI-ALIGN-007: Inspector-First Is Partially Implemented

Severity: medium.

Current state:

- The tenant shell already has left navigation, a top bar, center
  content, and a right context panel.
- Some selected-record detail still opens as a right drawer.

Required alignment:

- Selection detail belongs in the right inspector.
- Create/edit workflows use drawers.
- Destructive or compact confirmations use modals.
- Route changes are reserved for standalone workflows.

Implementation mode must verify:

- selecting a table/order updates the right inspector immediately
- opening a drawer starts a task, not basic inspection
- collapsed inspector restores the same selection when reopened where
  possible

### UI-ALIGN-008: `/table/{tableNumber}` Is Not Release-Ready

Severity: medium.

Current state:

- The route exists.
- It still behaves like a placeholder and does not load the real table
  workspace from persistence.

Required alignment:

- Decide whether `/table/{tableNumber}` is a deep link into `/service` or
  a standalone per-table workspace.
- Load live table/session/order/bill state from the same source as
  `/service`.
- Keep it consistent with the close-bill contract.

Implementation mode must verify:

- route state cannot show stale hard-coded table information
- table identity is unambiguous
- missing or inactive table states are explicit

### UI-ALIGN-009: Turkish/English Coverage Has Literal Leaks

Severity: medium.

Current state:

- Localization infrastructure exists for tenant UI.
- Some Razor surfaces and overlay errors still contain literal English
  strings.

Required alignment:

- Every user-facing string belongs in the localization resources.
- Turkish and English strings are reviewed together.
- Long Turkish labels are checked against the target layout.

Implementation mode must verify:

- no user-facing literal remains in Razor components
- status labels and validation text exist in both languages
- dense tables and inspector panels do not clip Turkish strings

## Recommended Documentation-To-Implementation Sequence

1. Stabilize the role and route contract for tenant operator surfaces.
2. Define the `/service` and `/stations/{stationCode}` surface contracts
   before writing UI code.
3. Define the close-bill UI/API contract and remove visible
   close-session language from operator-facing checkout.
4. Convert table/order selection behavior to the right inspector model.
5. Define the customer order-progress model.
6. Clean localization coverage as each surface is implemented.

## Explicitly Out Of Scope For This Review

- No code changes.
- No visual redesign work.
- No frontend package or component-library decision.
- No bill move, merge, or split workflow.
- No station analytics or reporting surface beyond manager overview.

## Review Status

Status: accepted for documentation-mode alignment.

Next mode: implementation, only after the route/role/surface contracts
above are selected for a work slice.
