# Operational Surfaces

This document explains how TabFlow thinks about tenant runtime surfaces
and why they are split the way they are.

The concrete route map, render mode, role matrix, event-bus topology,
ticket-card anchors, urgency bands, and shared runtime language all live
in
[`../../reference/architecture/runtime-surfaces.md`](../../reference/architecture/runtime-surfaces.md).
This document explains the product shape behind that map and does not
repeat its tables.

## Core Surface Family

The tenant runtime surface family has five members:

1. customer menu
2. tenant admin console
3. floor and cash workspace
4. station board
5. waiter and mobile PDA workspace

These surfaces share one runtime contract, one operational status
language, and one family resemblance in tone and interaction quality.
They should not feel like the same screen with different tabs.

## Why Multiple Surfaces Exist

Cafe operations have different attention modes:

- oversight and setup
- floor supervision and cash handling
- production fulfillment
- mobile, table-side service
- customer browsing and ordering

Trying to force those into one generic admin screen creates noise and
weakens each role.

## Surface Roles

### Tenant Admin Console

Role:

- live visibility
- setup and governance
- exception detection
- station, catalog, device, staff, and settings control
- tenant audit review

Target feel:

- premium operational control center
- IoTable-branded workbench rather than generic SaaS chrome
- compact, scan-first, and selection-driven

Console surfaces run under Interactive Server
([`../../reference/architecture/render-modes.md`](../../reference/architecture/render-modes.md)).
Component state lives server-side; inspectors, overlays, and live
indicators run without hand-written polling.

Admin-console visual direction:

- calm operator shell with a fixed top bar, left navigation rail, and right inspector
- white or near-white work surfaces with strong state accents
- compact operational detail under clear workspace titles
- high-contrast status chips for device, station, and fallback warnings

Admin-console interaction direction:

- dense registries should prefer `table + inspector`
- task-starting actions should prefer drawer or modal flows
- route changes should be the exception, not the default

The exact navigation baseline and overview content live in
[`../../reference/architecture/runtime-surfaces.md`](../../reference/architecture/runtime-surfaces.md)
under the console surface notes.

### Floor And Cash Workspace

Role:

- physical table state visibility
- open check handling
- manual payment flow
- close-bill action

The floor and cash workspace is operated by the `cashier` role (and
by `manager` or `owner` when they step in). It is explicitly not the
station operator's surface; fulfillment lives on the station board.
"Waiter" in the product vocabulary refers to the mobile PDA
*workspace* (T-14 `/pda`), not to a distinct role; PDA sessions also
run under the `cashier`, `manager`, or `owner` identity.

Target feel:

- live
- tactical
- table-centric
- capable of showing both physical floor flow and bill flow in one
  workspace

Primary mental model:

- one workspace reveals both physical floor flow and bill and payment
  flow
- operators should not have to jump between a decorative floor planner
  and a separate cash-only screen to understand the live state of a
  table
- normal mode is operational; layout editing is explicit and separate
- close-bill actions are quick but still deliberate
- closing a check requires a stronger confirmation than normal table
  selection

The floor and cash workspace runs under Interactive Server with
subscription to the in-process event bus; new submitted orders, status
changes, and bill mutations appear without polling. The table-card
anchors, top-level views, and action list live in the runtime-surfaces
reference.

### Station Board

Role:

- single-station fulfillment board
- progression through `new`, `preparing`, and `ready`
- urgency visibility with minimal distraction

The station board is operated by the station operator. It is a distinct
role, a distinct URL family, and a distinct identity (`station_device`).
It is not the cashier's surface and must not expose cashier actions.

Target feel:

- high contrast
- fast-glance readable
- more like a production board than an admin panel

Station board is a family concept, not just one kitchen screen. Variants
may include kitchen, barista, bar, hookah, fastfood, and dispatch.

Design principles:

- the default single-station view emphasizes `new`, `preparing`, and
  `ready` columns
- a supervisor view may add station switching or multi-station summary
- cards prioritize distance readability over dense data presentation

Ticket-card anchors, urgency bands, operator actions, and visual
direction live in the runtime-surfaces reference rather than being
restated here.

The station board runs under Interactive Server and subscribes to the
order event stream so newly submitted items appear immediately and
urgency bands update without polling.

### Waiter And PDA Web

Role:

- fast mobile order taking
- one-handed use
- table-side operation

This surface behaves like a focused field tool rather than a compressed
admin screen.

PDA direction:

- one-handed use
- quick table selection
- fast note entry
- minimal navigation chrome
- protected waiter actions run under the waiter's authenticated tenant
  identity, not through a customer QR session

### Customer Menu

Role:

- table-side browsing
- open check visibility
- low-friction order composition

Security note:

- browsing stays lightweight while the access ticket is valid
- final order submission remains the critical boundary and requires a
  fresh QR proof

Experience direction:

- category-first browsing
- strong mobile-first layout
- lightweight session continuity while the table session is open
- final order submission as the only deliberately high-friction step

Customer menu surfaces run under Static SSR. They do not open a SignalR
connection. The server-side cart model is described in
[`./customer-session-model.md`](./customer-session-model.md).

## Station-First Fulfillment

TabFlow is station-first rather than kitchen-only. Products route to
stations, stations are the fulfillment unit, and one order may split
operationally across different stations. This is what lets the same
product model fit kitchen, bar, barista, hookah, dessert, and dispatch
without hard-coding one narrow restaurant structure forever.

The routing rules and fallback-station invariants live in
[`../../reference/architecture/runtime-surfaces.md`](../../reference/architecture/runtime-surfaces.md).

## Floor Layout Model

Floor and cash operation is not just a flat table list. A tenant may own
multiple layouts (main floor, balcony, upper floor, garden, dispatch or
takeaway). Each layout may own zones; tables hold placement metadata per
layout; fixed floor objects act as edit-friendly anchors rather than
runtime billing entities. Edit mode is distinct from normal operations
mode.

The data model for layouts, zones, and placements lives in
[`../../reference/database/schema.md`](../../reference/database/schema.md).

## UX Principle

The same business state is visible through role-appropriate surfaces,
not through one giant shared UI. This is a core TabFlow product
principle, not a visual afterthought.

## First Design Emphasis

Product language sharpens first around:

1. tenant admin overview
2. floor and cash workspace
3. station board

If those three surfaces feel operational, fast, and premium, the rest of
the runtime family can inherit the same language without becoming
repetitive.

## Related

- [`../../reference/architecture/runtime-surfaces.md`](../../reference/architecture/runtime-surfaces.md)
- [`../../reference/architecture/render-modes.md`](../../reference/architecture/render-modes.md)
- [`./customer-session-model.md`](./customer-session-model.md)
- [`./authorization.md`](./authorization.md)
