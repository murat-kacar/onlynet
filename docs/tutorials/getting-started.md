# Getting Started

A suggested reading order for someone new to the TabFlow design. Each
step points at the normative document that owns the subject; follow
the links in sequence.

## 1. The Big Split

TabFlow has two host shapes, a platform (control plane) and one
tenant host per venue. Both are Blazor Web App on .NET 10.

Read:

- [`../reference/architecture/system-overview.md`](../reference/architecture/system-overview.md)
- [`../explanation/concepts/multi-tenancy.md`](../explanation/concepts/multi-tenancy.md)

## 2. Runtime Surfaces And Render Modes

Every HTML surface has a route ID, a role gate, and a render mode.

Read:

- [`../reference/architecture/runtime-surfaces.md`](../reference/architecture/runtime-surfaces.md)
- [`../reference/architecture/render-modes.md`](../reference/architecture/render-modes.md)
- [`../explanation/concepts/operational-surfaces.md`](../explanation/concepts/operational-surfaces.md)

## 3. Architecture Decisions

The active architectural decisions that constrain the design.

Read:

- [`../reference/architecture/decisions.md`](../reference/architecture/decisions.md)

## 4. Tenant Lifecycle And Provisioning

How a tenant is created, archived, or deleted; how provisioning is
observed.

Read:

- [`../explanation/concepts/tenant-lifecycle.md`](../explanation/concepts/tenant-lifecycle.md)
- [`../how-to/provision-tenant.md`](../how-to/provision-tenant.md)

## 5. Authentication, Roles, And The Customer Session

Identity stores, role matrix, and the QR-driven customer session.

Read:

- [`../explanation/concepts/authorization.md`](../explanation/concepts/authorization.md)
- [`../explanation/concepts/customer-session-model.md`](../explanation/concepts/customer-session-model.md)

## 6. The API And Error Model

The public HTTP surface and the error-code vocabulary.

Read:

- [`../reference/api/tenant-api.md`](../reference/api/tenant-api.md)
- [`../reference/api/error-codes.md`](../reference/api/error-codes.md)

## 7. The Database And Migrations

Schema ownership, naming convention, and migration authority.

Read:

- [`../reference/database/schema.md`](../reference/database/schema.md)

## 8. The Device Contract

Hardware profile, runtime contract, and generated-artifact rules.

Read:

- [`../reference/firmware.md`](../reference/firmware.md)

## 9. What Is Required To Pass

Invariants, accessibility baseline, and the release gate.

Read:

- [`../reference/acceptance-criteria.md`](../reference/acceptance-criteria.md)
- [`../explanation/concepts/accessibility.md`](../explanation/concepts/accessibility.md)
- [`../meta/release-gate.md`](../meta/release-gate.md)

## 10. The Glossary

When in doubt about a term, this is the canonical vocabulary.

Read:

- [`../reference/glossary.md`](../reference/glossary.md)
