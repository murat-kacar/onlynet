# Restart A Tenant

This guide restarts a single tenant host without affecting other
tenants or the platform control plane.

A tenant restart is a safe, common operation. It can clear a wedged
WebSocket pool, apply configuration changes, or recover from a
transient fault. It does not alter any data.

## When To Restart

Typical triggers:

- configuration change in the tenant host's `appsettings.*.json`
- transient memory or connection pool pressure
- post-deploy verification when a rolling restart is not configured
- device WebSocket pool appears wedged and reconnects are not clearing

A restart is not the right remediation for data corruption, schema
drift, or provisioning-state mismatches. Those live in
[`./inspect-provisioning-job.md`](./inspect-provisioning-job.md) and
the database how-to set.

## Pre-Checks

Before restarting:

- Confirm the tenant code from the platform admin console tenant
  list (`P-03`) or the tenant detail view (`P-05`).
- Confirm the systemd unit name from the deployment layout in
  [`./deploy-to-production.md`](./deploy-to-production.md). The
  canonical pattern is `tabflow-tenant@<tenant-code>.service`.
- Notify staff if the restart is during operational hours; existing
  SignalR sessions will reconnect and in-flight form state will be
  lost.

## Procedure

```bash
systemctl restart tabflow-tenant@<tenant-code>.service
```

Verify the tenant came back up:

```bash
systemctl status tabflow-tenant@<tenant-code>.service
curl -fsS https://<tenant-domain>/health/ready
```

`/health/ready` returns `200` once the tenant database connection is
healthy.

## Post-Checks

- Watch `journalctl -u tabflow-tenant@<tenant-code>.service -f` for
  startup errors.
- Confirm a representative device WebSocket reconnects
  (`/ws/tables/...`) in the host logs.
- Confirm the floor and cash workspace (`T-13`) loads without console
  errors.

## Rolling Restart Of All Tenants

Only the platform operator does this, and usually only after a host
binary upgrade. The canonical approach is a serial loop:

```bash
for tenant in $(systemctl list-units --no-legend \
                    'tabflow-tenant@*.service' \
                    | awk '{print $1}' | sed 's/tabflow-tenant@//;s/\.service//'); do
  systemctl restart "tabflow-tenant@${tenant}.service"
  sleep 5
done
```

Parallel restarts are not used because PostgreSQL connection churn is
easier to reason about serially.

## Related

- [`./deploy-to-production.md`](./deploy-to-production.md)
- [`./inspect-provisioning-job.md`](./inspect-provisioning-job.md)
- [`../reference/architecture/system-overview.md`](../reference/architecture/system-overview.md)
