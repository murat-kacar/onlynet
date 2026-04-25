# Tenant Lifecycle

This document explains the lifecycle model for tenants as a product and
control-plane concept.

## Core Rule

Tenant lifecycle operations are platform jobs.

The UI may ask for a tenant, but infrastructure changes must remain
observable, retryable, and explicit through provisioning state.

## Tenant Identity

Tenant code baseline:

- lowercase `a-z`, `0-9`, and hyphen
- 3 to 63 characters
- no leading or trailing hyphen
- globally unique in the platform database

Primary domain baseline:

- normalized to lowercase
- no scheme
- no trailing dot
- globally unique in the platform database

The platform does not impose a fixed parent domain. Tenants may run on any
domain they own; the platform's job is to route to the tenant host that
serves that domain.

## Create

Creating a tenant is a platform job, not an inline request. Registry
state is reserved synchronously; database creation, migration, seeding,
and runtime activation run asynchronously inside a `tenant.create`
provisioning job so each step is retryable, observable, and auditable.

The step-by-step operator procedure lives in
[`../../how-to/provision-tenant.md`](../../how-to/provision-tenant.md);
the single-responsibility principle for this document keeps the
conceptual model here and the operational playbook there.

## Runtime Seed Baseline

Tenant bootstrap seeds a minimal runtime:

- tenant profile
- starter tables (`000` and `999`)
- starter catalog baseline
- default tenant owner user

The default tenant owner's initial password is generated at
provisioning time, shown exactly once to the operator, and never
persisted anywhere except as a hash. No fixed default password is ever
issued. First successful login forces a password change. The full
default-owner contract (email derivation, display rules, hashing) lives
in [`../../how-to/provision-tenant.md`](../../how-to/provision-tenant.md).

## Archive

Archive is the preferred non-destructive lifecycle step.

Archive direction:

- tenant status becomes `archived`
- database remains
- historical business state remains
- active runtime usage should stop

## Delete

Delete is destructive and should require explicit intent.

Expected cleanup direction:

- tenant database and database user are removed
- generated runtime artifacts are removed
- platform registry rows are removed

> [!WARNING]
> Delete is destructive and irreversible. Prefer archive first. Use
> delete only after an explicit operator decision and an audit-trail
> note.

## Collision Handling

Provisioning must fail safely when:

- tenant code already exists
- primary domain already exists
- generated database name already exists
- generated database user already exists

Registry collisions should fail fast. Runtime collisions should surface
through job failure details and deliberate retry.

## Related

- [`./multi-tenancy.md`](./multi-tenancy.md)
- [`./authorization.md`](./authorization.md)
- [`../../how-to/provision-tenant.md`](../../how-to/provision-tenant.md)
