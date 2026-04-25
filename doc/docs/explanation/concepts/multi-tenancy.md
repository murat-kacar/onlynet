# Multi-Tenancy

TabFlow uses a control-plane and runtime split rather than treating the
whole system as one flat application.

## Core Idea

The platform and tenants are different things.

- The platform owns registry state and lifecycle orchestration.
- Each tenant owns runtime business state.

The platform is not a tenant and should not behave like one.

## Why This Matters

This separation keeps:

- tenant business data isolated
- provisioning visible and explicit
- runtime incidents local to a tenant where possible
- control-plane logic free from tenant business-table coupling

## Database View

The split is strict: the platform database owns control-plane state
(registry, provisioning jobs, platform identity, platform audit); each
tenant database owns one venue's runtime state (tenant identity,
catalog, floor, tables and devices, customer sessions, orders and
bills, stations, tenant audit).

The authoritative per-table list lives in
[`../../reference/database/schema.md`](../../reference/database/schema.md).
This document intentionally does not duplicate it so the two stay in
lockstep.

## Runtime View

The platform host handles:

- tenant creation
- tenant status changes
- provisioning visibility
- control-plane operations
- platform audit

Each tenant host handles:

- customer menu access
- floor and cash operations
- station and kitchen flows
- waiter and mobile workflow
- tenant-local operational behavior
- tenant audit

Each tenant runs its own ASP.NET Core host process against its own database.
See [`../../reference/architecture/system-overview.md`](../../reference/architecture/system-overview.md)
and [`../../reference/architecture/decisions.md`](../../reference/architecture/decisions.md)
AD-0001 and AD-0003.

## Provisioning As The Bridge

Provisioning is the bridge between the two worlds.

It takes platform-owned registry state and turns it into tenant-owned
runtime state without collapsing the two into one application boundary.

## Related

- [`./tenant-lifecycle.md`](./tenant-lifecycle.md)
- [`./customer-session-model.md`](./customer-session-model.md)
- [`./operational-surfaces.md`](./operational-surfaces.md)
- [`./authorization.md`](./authorization.md)
