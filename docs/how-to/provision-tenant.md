# Provision A Tenant

This guide describes the canonical tenant provisioning flow from the
platform control plane perspective.

## Inputs

Required:

- Tenant code (lowercase `a-z`, `0-9`, hyphen, 3 to 63 characters, no
  leading or trailing hyphen)
- Display name
- Primary domain (lowercase, no scheme, no trailing dot)
- Language code
- Currency code
- Time zone

Optional:

- Intended first tenant owner email

## Flow

1. Validate the tenant code and primary domain.
2. Capture the regional settings.
3. Optionally capture the intended first tenant owner email.
4. Reserve tenant registry state in the platform database.
5. Create a `tenant.create` provisioning job.
6. Allocate runtime identifiers: database name, database user, host-local
   port.
7. Prepare the runtime configuration and the runtime artifacts.
8. Generate per-table firmware sketches for the seeded tables.
9. Create the tenant database and the tenant-scoped database user.
10. Apply tenant EF Core migrations.
11. Seed tenant defaults:
    - Tenant profile
    - Starter tables (`000` and `999`)
    - Starter catalog baseline
    - Default tenant owner user
12. Verify tenant runtime health.
13. Mark the tenant `active`.

## Default Owner Baseline

When a tenant database is initialized:

- The initial owner email defaults to a tenant-scoped address derived from
  the configured platform email template, or the optional
  `initialOwnerEmail` input.
- The initial owner password is generated at provisioning time.
- The generated password is shown exactly once, at the end of the
  provisioning flow, to the operator who initiated the create.
- The generated password is never written to disk, to configuration, or to
  the audit log. Only a hashed version persists.
- First successful login forces a password change.

## Verification

Provisioning is considered successful when:

- The provisioning job completes successfully.
- Tenant host `GET /health/ready` responds `ok`.
- The tenant domain is reachable on HTTPS.
- Seeded defaults are present:
  - Starter tables `000` and `999`
  - Starter catalog baseline
  - Default tenant owner
  - Per-table ready-to-flash firmware sketches

## Collision Handling

Provisioning fails safely when:

- Tenant code already exists.
- Primary domain already exists.
- Generated database name already exists.
- Generated database user already exists.

Registry collisions fail fast. Runtime collisions surface through job
failure detail so they can be corrected and retried deliberately.

## Failure Model

Provisioning is job-based and stays observable.

- Duplicate code or domain fails fast.
- Runtime collisions and host-side faults are captured as job failures.
- Retryable steps stay retryable through the worker.
- Infrastructure side effects never run inside synchronous request
  handlers.

## Follow-Up

After a successful tenant create:

1. Verify that the tenant domain is reachable over HTTPS.
2. Sign in at `https://<tenant-domain>/login` with the generated owner
   password from provisioning.
3. Change the password at the forced `/change-password` prompt.
4. Verify the customer menu at `/`, the admin console at `/console`, and
   the station board at `/stations` through a station-device identity
   once one exists.

If the tenant is no longer needed, prefer archive over destructive delete.
See [`../explanation/concepts/tenant-lifecycle.md`](../explanation/concepts/tenant-lifecycle.md).
