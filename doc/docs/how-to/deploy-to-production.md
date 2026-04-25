# Deploy To Production

This guide describes the **deployment invariants** TabFlow expects.
Specific tooling, paths, and commands are the operator's choice; the
invariants below are not.

## Host Topology

- One **platform host** process per deployment — serves the platform
  admin surfaces and platform health probes.
- One **platform worker** process per deployment — claims provisioning
  jobs from the platform database.
- One **tenant host** process per tenant — serves every tenant-facing
  surface (customer, staff, admin, station) and the ESP32 device
  WebSocket.

Each side runs as a single ASP.NET Core host. Splitting a side into
separate API and UI processes is out of scope for the baseline
([AD-0003](../reference/architecture/decisions.md#ad-0003-one-host-process-per-side)).

## Separation Of Concerns

- **Source tree** holds code and documentation only.
- **Published host artifacts** live outside the source tree.
- **Host-owned configuration** (connection strings, keys, per-tenant
  environment) lives outside the source tree.
- **Runtime-owned output** (generated firmware sketches, logs) lives
  outside the source tree.

Operators may choose any layout that honours these separations.

## Reverse Proxy

A reverse proxy terminates TLS and routes by host to the target
process:

- `https://<platform-host>` routes to the platform host.
- `https://<tenant-domain>` routes to that tenant's host.
- WebSocket upgrade MUST be passed through on `/ws/tables/...`.
- `/api/public/**`, `/health*`, and UI routes all reach the same
  process per host; a separate `/api/` location block is not
  required.

Per-tenant reverse-proxy configuration is generated during
provisioning and reloaded when the tenant goes active.

## Process Supervision

Each host process is supervised so that:

- It restarts on crash.
- It reads configuration from a host-owned source outside the source
  tree.
- It is addressed individually per side (platform, worker) and per
  tenant (tenant host instances).

The baseline example is systemd with a templated unit for the tenant
host; the operator may use any supervisor that honours the
invariants.

## First Deployment

The very first deployment of a fresh host requires
[`./bootstrap-platform.md`](./bootstrap-platform.md) to run before the
flow below applies. Bootstrap is single-shot per deployment; subsequent
deployments follow the standard flow.

## Standard Deployment Flow

1. Update the source checkout.
2. Produce publish-ready artifacts for the platform host, the platform
   worker, and the tenant host.
3. Apply configuration changes only when source layout or runtime
   contracts have changed.
4. Restart affected processes.
5. Reload the reverse proxy when host-level routing changes.
6. Run smoke checks (see below).

## Automated Tenant Provisioning

Once DNS exists for a tenant host, the platform worker completes the
tenant runtime lifecycle automatically:

1. Create the tenant database and database user.
2. Write the tenant-specific host configuration.
3. Ensure a tenant-host process instance exists for the tenant code.
4. Generate and enable a reverse-proxy vhost for the tenant domain.
5. Obtain TLS for the tenant domain.
6. Start the tenant host.
7. Apply EF Core migrations and seed tenant defaults.
8. Verify health and mark the tenant `active`.

The full provisioning contract, including initial owner password
handling, lives in [`./provision-tenant.md`](./provision-tenant.md).

## Smoke Checks

At minimum, after any deployment:

- `GET /health/ready` on the platform host returns `200`.
- `GET /health/ready` on at least one tenant host returns `200`.
- Platform admin sign-in page returns `200`.
- Tenant customer entry page returns `200` on a known tenant domain.
- Expected static assets are reachable.
- The platform worker is up and polling the provisioning queue.

## Operating Principle

The repository stays source-first:

- No active production secrets inside the source tree.
- No host-owned environment files inside the source tree.
- Published host artifacts live outside the source tree.
- Host-specific state is explicit in host-owned configuration, not
  hidden inside source documents.
