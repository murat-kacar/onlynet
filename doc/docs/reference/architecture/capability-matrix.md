# Capability Matrix

This matrix is the current implementation status for the repository against
the baseline architecture described in
[`./system-overview.md`](./system-overview.md),
[`./runtime-surfaces.md`](./runtime-surfaces.md), and
[`./decisions.md`](./decisions.md).

Status values:

- `Target` — the capability is part of the baseline and is pending
  implementation.
- `In progress` — implementation has started.
- `Implemented` — the capability is *done* in the constitution sense
  ([`../../constitution.md`](../../constitution.md), II.4): tested
  (automated coverage), observable (production metric/log/trace), and
  documented (reference document is current). All three. None is
  optional.
- `Deferred` — the capability is intentionally not part of the current
  baseline and will land later.

| Capability | Status | Notes |
| --- | --- | --- |
| Platform host unified Blazor project | In progress | Host boots, serves dashboard skeleton; admin console surfaces incomplete. |
| Tenant host unified Blazor project | In progress | Host boots, serves customer/menu/order/tables/kitchen pages; admin console surfaces incomplete. |
| Platform Identity store | In progress | Identity tables present in `tabflow_platform`; bootstrap admin command pending — see [`../../how-to/bootstrap-platform.md`](../../how-to/bootstrap-platform.md). |
| Tenant Identity store | In progress | Identity tables present in tenant DB; tenant role seeding pending. |
| Tenant role matrix (owner, manager, cashier, station_device) | Target | Schema present; seeded on tenant bootstrap. |
| Platform admin console surfaces | Target | `/`, `/tenants`, `/tenants/new`, `/tenants/{id}`, `/jobs`, `/audit`, `/login`, `/change-password`. |
| Tenant admin console surfaces | Target | `/console`, `/console/catalog`, `/console/stations`, `/console/tables`, `/console/users`, `/console/firmware`, `/console/audit`. |
| Tenant customer surfaces (Static SSR) | In progress | `/menu` and `/order/{id}` implemented; `/g/{token}` join flow pending. |
| Tenant floor and cash workspace | Target | `/service` on Interactive Server with server push. |
| Tenant waiter PDA | Target | `/pda` on Interactive Server. |
| Tenant station board | In progress | `/kitchen` exists; multi-station `/stations/{stationCode}` routing pending. |
| Platform tenant registry | Target | Create, list, get, status update, regional settings, runtime visibility, jobs. |
| Platform provisioning worker | Target | Polls `tenant.create` jobs, writes runtime artifacts, coordinates host and runtime activation. |
| Tenant schema via EF Core migrations | In progress | Migrations project exists; design-time factories pending — see [`/doc/docs/how-to/setup-migrations.md`](/doc/docs/how-to/setup-migrations.md). |
| Bootstrap platform admin via CLI | Target | Single-shot `bootstrap-admin` command — see [`../../how-to/bootstrap-platform.md`](../../how-to/bootstrap-platform.md). |
| Customer session with server-side cart | In progress | `customer_sessions`, `customer_access_tickets`, `customer_session_cart_items`, `qr_tokens` schema present; submit-order path implemented. |
| Fresh-QR checkout proof on submit | Target | Required for every order submission. |
| In-process event bus for real-time surfaces | In progress | Channel-backed dispatcher implemented for `order.*`, `bill.*`, `table.*`, `device.*`; SignalR fan-out wired for tenant host. |
| Tenant audit log | In progress | `tenant_audit_log` table present; write path on hot actions pending. |
| Health check endpoints (`/health`, `/health/live`, `/health/ready`) | Implemented | Three endpoints wired on both platform and tenant hosts; `platform-db:ping` / `tenant-db:ping` probes registered via `AddDbContextCheck<T>`; IETF `application/health+json` body produced by `TabFlow.Shared.Infrastructure.Diagnostics.HealthJsonWriter`; contract-tested by 8 unit tests in `Shared.Tests`. Advanced probes (migration head, worker heartbeat, event-bus capacity, tenant-context) tracked under TD-0013. Spec: [`/doc/docs/reference/architecture/health-checks.md`](/doc/docs/reference/architecture/health-checks.md). |
| Structured logging via Serilog | In progress | Console + file sinks wired in platform and tenant hosts. |
| OpenTelemetry tracing | In progress | ASP.NET Core + HttpClient instrumentation enabled in both hosts; exporter pending. |
| Process supervision via systemd | Target | Reference unit set in [`/doc/docs/how-to/supervise-processes.md`](/doc/docs/how-to/supervise-processes.md); not yet enabled in production. |
| Device WebSocket token push | In progress | `/ws/tables/{tableNumber}` endpoint exists in tenant host; firmware-side contract pending validation. |
| Firmware generation per table | Target | Produces flash-ready single-file sketches with tenant-specific defines. |
| Station device authentication mechanism | Deferred | Depends on station hardware choice; placeholder `StationDevice` policy is in place so the rest of the stack is not blocked. |
| Advanced payment lifecycle | Deferred | Richer payment metadata and reconciliation flows remain future work. |
| Native mobile or third-party external API | Deferred | AD-0003 accepts that a second host project is added when a concrete need appears. |
| Encrypted backup with off-site copy | Target | Spec lives in [`/doc/docs/how-to/backup-and-restore.md`](/doc/docs/how-to/backup-and-restore.md); production wiring pending. |
| Quarterly disaster-recovery drill | Target | Procedure documented; first drill not yet run. Required by release gate within 90 days of first production deploy. |
| Personal-data classification on schema | Target | `[DataClass]` attribute and schema-comment generation per [`/doc/docs/explanation/concepts/data-protection.md`](/doc/docs/explanation/concepts/data-protection.md); not yet wired. |
| Retention sweep jobs | Target | Sweep job types listed in [`data-protection.md`](/doc/docs/explanation/concepts/data-protection.md#retention-schedule); platform-worker job handlers pending. |
| i18n via `IStringLocalizer<T>` and `*.resx` | Target | Strategy in [`/doc/docs/explanation/concepts/internationalization.md`](/doc/docs/explanation/concepts/internationalization.md); first English-neutral resx files pending. |
| English-first lint enforcement | Target | AD-0015 accepted; analyzer rule for non-ASCII identifiers pending. |
| GitHub Actions CI workflows | In progress | Workflow files committed in `.github/workflows/`; first run on a real PR pending. |
| Branch protection on `main` | Target | Spec lives in [`/doc/docs/how-to/configure-branch-protection.md`](/doc/docs/how-to/configure-branch-protection.md); GitHub configuration not yet applied. |

Rows move to `In progress` and then `Implemented` as each capability lands.
