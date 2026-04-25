# Render Modes

Blazor Web App exposes four render modes: Static SSR, Interactive Server,
Interactive WebAssembly, and Interactive Auto. TabFlow uses only two of them.

This document is the reference for which mode each surface family uses and
why. The decision itself is recorded in
[`./decisions.md`](./decisions.md) AD-0004.

## Modes In Use

### Static SSR (with enhanced navigation and forms)

The server renders HTML per request. There is no SignalR connection. Forms
submit through normal HTTP `POST`. Blazor's enhanced navigation intercepts
same-origin navigation to patch the DOM and avoid full-page reloads, without
requiring an interactive runtime on the client.

Characteristics:

- Minimal JavaScript payload
- First paint is a full HTML document
- Per-request state; the server holds no per-user component instance
- Works with mobile data and short-lived sessions
- No reconnect logic required because there is no persistent connection

### Interactive Server

The first response is rendered on the server. The client then opens a
SignalR connection and keeps it open while the component tree is live.
Events such as clicks, input, and form submissions travel over SignalR to the
server, which re-renders the component and sends back DOM diffs.

Characteristics:

- Component state lives in the host process
- Low-latency reactivity without hand-written fetch and cache code
- Server-push is a natural capability; an `IAsyncEnumerable<TEvent>` feeds a
  component directly
- Each connected client costs an open WebSocket and a server-side circuit

### Not Used

Interactive WebAssembly and Interactive Auto are not in the baseline. They
would only pay off if a surface needed offline capability or if the
component bundle were stable enough to cache aggressively. Neither applies to
the current surface family.

## Assignment

### Platform Host

Every platform surface is `Interactive Server`. The platform admin
audience is small, desktop-based, and always online. Interactive Server
minimizes the code needed for CRUD screens, modals, and job monitoring.
The reconnect cost is negligible because client count is low.

### Tenant Host

Render mode is assigned by surface family:

| Surface family | Mode | Why |
| --- | --- | --- |
| Public customer (welcome, QR landing, menu and cart) | Static SSR | Mobile data, short session, wide fanout, no interactive payoff to justify SignalR per phone |
| Authentication (login, change-password) | Static SSR | Single-form surfaces |
| Admin console | Interactive Server | Forms, modals, drag-and-drop, live metrics |
| Floor and cash workspace | Interactive Server with server push | Live table state, push-driven updates, rich interaction |
| Waiter PDA | Interactive Server | Mobile staff device on tenant Wi-Fi; Interactive Server gives the same code ergonomics as the console surfaces |
| Station board | Interactive Server with server push | Push-driven fulfillment display; urgency, new-ticket alerts, status progression |

The per-route render-mode column lives in
[`./runtime-surfaces.md`](./runtime-surfaces.md). That table is the
authoritative source; changes land there and this document describes
the family-level reasoning only.

Customer surfaces never open a SignalR connection. Staff surfaces do.

## Authoring Rules

- A Razor component declares its mode with `@rendermode InteractiveServer`
  at the top of the file. Static SSR requires no annotation.
- Components authored for Static SSR must not rely on `@onclick` handlers
  that require interactivity; they must express state through form
  submission, query parameters, or navigation.
- Components authored for Interactive Server may use event handlers, state
  containers, and `IAsyncEnumerable` subscriptions.
- A shared component that needs to work in both modes is split into a Static
  SSR shell and an Interactive Server island. Mixing is scoped, not
  automatic.

## Capacity Planning

Interactive Server circuits cost memory and an open WebSocket per connected
client. Tenant hosts should size for:

- Concurrent staff circuits: `console` plus `service` plus `pda` plus
  `stations` sessions open at the same time.
- Concurrent customer sessions: served over Static SSR, so their cost is
  request-scoped, not connection-scoped.

A tenant handling 20 staff circuits and 100 simultaneous customer sessions
runs within ordinary ASP.NET Core host capacity. Interactive Server capacity
guidance in the official .NET documentation applies directly.

## Change Policy

Changing a surface's render mode is not a free decision. A change requires:

- an update to this document
- confirmation that the component authoring contract for the target mode is
  respected
- a note on the capacity impact, especially when moving from Static SSR to
  Interactive Server for a customer-facing surface

## Related

- [`./decisions.md`](./decisions.md) — AD-0004 records the decision
- [`./runtime-surfaces.md`](./runtime-surfaces.md) — per-route render mode
