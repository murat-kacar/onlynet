# Capability Matrix

This matrix is the current implementation status for the repository
against the baseline architecture described in
[`./system-overview.md`](./system-overview.md),
[`./runtime-surfaces.md`](./runtime-surfaces.md), and
[`./decisions.md`](./decisions.md).

Status values:

- `Target` - the capability is part of the baseline and is pending
  implementation.
- `In progress` - implementation has started.
- `Implemented` - the capability is done in the constitution sense
  ([`../../constitution.md`](../../constitution.md), II.4): tested,
  observable, and documented.
- `Deferred` - the capability is intentionally outside the current
  baseline.

| Capability | Status | Notes |
| --- | --- | --- |
| Platform host unified Blazor project | In progress | Host boots and serves the platform console skeleton; admin workflows remain incomplete. |
| Tenant host unified Blazor project | In progress | Host boots and serves customer, order, table, kitchen, and settings surfaces; tenant admin console depth remains incomplete. |
| Platform Identity store | In progress | Identity tables, bootstrap-admin command, audit event, and must-change-password enforcement exist. First-deployment operator verification remains open under [TD-0002](/doc/buildlog/tech-debt-ledger.md#td-0002). |
| Tenant Identity store | In progress | Identity schema exists; tenant role seeding remains part of tenant bootstrap work. |
| Tenant role matrix (`owner`, `manager`, `cashier`, `station_device`) | Target | Schema and policy names exist; complete seed and enforcement coverage remain pending. |
| Platform admin console surfaces | Target | `/`, `/tenants`, `/tenants/new`, `/tenants/{id}`, `/jobs`, `/audit`, `/login`, `/change-password`, `/settings`. |
| Tenant admin console surfaces | Target | `/console`, `/console/catalog`, `/console/stations`, `/console/tables`, `/console/users`, `/console/firmware`, `/console/audit`. |
| Tenant customer surfaces (Static SSR) | In progress | Customer flows exist, but `Cart.razor`, `Menu.razor`, `Order.razor`, and `ScanQr.razor` still require Interactive Server. Static SSR conversion is tracked under [TD-0028](/doc/buildlog/tech-debt-ledger.md#td-0028). |
| Mixed render modes per surface family (AD-0004) | In progress | Hosts use the Blazor Web App model; staff pages opt into Interactive Server. Customer pages and release-gate smoke coverage remain open under [TD-0028](/doc/buildlog/tech-debt-ledger.md#td-0028) and [TD-0016](/doc/buildlog/tech-debt-ledger.md#td-0016). |
| Tenant floor and cash workspace | Target | `/service` on Interactive Server with server push. |
| Tenant waiter PDA | Target | `/pda` on Interactive Server. |
| Tenant station board | In progress | `/kitchen` exists; multi-station `/stations/{stationCode}` routing remains pending. |
| Platform tenant registry | Target | Create, list, get, status update, regional settings, runtime visibility, and jobs. |
| Platform provisioning worker | Target | Polls `tenant.create` jobs, writes runtime artifacts, coordinates host activation, and applies tenant migrations. |
| Tenant schema via EF Core migrations | In progress | Migrations project and design-time factories exist. Provisioning-time migration application remains open under [TD-0003](/doc/buildlog/tech-debt-ledger.md#td-0003). |
| Bootstrap platform admin via CLI | In progress | Single-shot bootstrap command exists and refuses to run when users already exist. First-deployment execution evidence remains open under [TD-0002](/doc/buildlog/tech-debt-ledger.md#td-0002). |
| Customer session with server-side cart | In progress | Server-side sessions, tickets, cart items, QR tokens, device-cookie binding, and idempotent order submit exist. Wrong-device integration coverage remains open under [TD-0017](/doc/buildlog/tech-debt-ledger.md#td-0017). |
| Fresh-QR checkout proof on submit | In progress | Order submit consumes the checkout token in the order transaction and rejects stale or wrong-table tokens. Anonymous/cross-role integration coverage remains open under [TD-0015](/doc/buildlog/tech-debt-ledger.md#td-0015). |
| In-process event bus for real-time surfaces | In progress | Channel-backed dispatcher exists for `order.*`, `bill.*`, `table.*`, and `device.*`; SignalR fan-out is wired for tenant host surfaces. |
| Tenant audit log | In progress | `tenant_audit_log` table exists; hot-action write coverage remains pending. |
| Health check endpoints (`/health`, `/health/live`, `/health/ready`) | Implemented | Platform and tenant hosts expose the three endpoints, DB ping/migration probes, tenant-context checks, event-bus capacity checks, and IETF `application/health+json` responses. Worker heartbeat readiness remains open under [TD-0013](/doc/buildlog/tech-debt-ledger.md#td-0013). |
| Structured logging via Serilog | In progress | Console and file sinks are configured; hot-path logging uses source-generated logger methods. Deployed sink verification remains open under [TD-0012](/doc/buildlog/tech-debt-ledger.md#td-0012). |
| OpenTelemetry tracing | In progress | ASP.NET Core and HttpClient instrumentation are enabled in both hosts; exporter wiring remains pending. |
| Process supervision via systemd | Implemented | Platform, tenant, and platform-worker hosts register the systemd lifetime hook; operator enablement follows [`/doc/docs/how-to/supervise-processes.md`](/doc/docs/how-to/supervise-processes.md). |
| Device WebSocket token push | In progress | `/ws/tables/{tableNumber}` exists in the tenant host; firmware-side validation remains pending. |
| Firmware generation per table | Target | Produces flash-ready single-file sketches with tenant-specific defines. |
| Test taxonomy (Unit / Integration / E2E / Smoke) via xUnit Traits | In progress | Trait categories and unit-tier analyzer rules exist. Hermetic integration fixture and smoke tier bootstrap remain open under [TD-0010](/doc/buildlog/tech-debt-ledger.md#td-0010). |
| Station device authentication mechanism | Deferred | Depends on station hardware choice; placeholder `StationDevice` policy keeps the rest of the stack unblocked. |
| Advanced payment lifecycle | Deferred | Richer payment metadata and reconciliation flows remain future work. |
| Native mobile or third-party external API | Deferred | AD-0003 accepts adding another host project only when a concrete need appears. |
| Encrypted backup with off-site copy | Target | Spec lives in [`/doc/docs/how-to/backup-and-restore.md`](/doc/docs/how-to/backup-and-restore.md); production wiring remains open under [TD-0004](/doc/buildlog/tech-debt-ledger.md#td-0004). |
| Quarterly disaster-recovery drill | Target | Procedure is documented; first drill is required within 90 days of first production deployment. |
| Personal-data classification on schema | In progress | Classification primitives exist; full annotation sweep and release-gate checks remain open under [TD-0007](/doc/buildlog/tech-debt-ledger.md#td-0007). |
| Retention sweep jobs | Target | Sweep types are documented in [`data-protection.md`](/doc/docs/explanation/concepts/data-protection.md#retention-schedule); worker handlers remain open under [TD-0008](/doc/buildlog/tech-debt-ledger.md#td-0008). |
| i18n via `IStringLocalizer<T>` and `*.resx` | In progress | Platform console ships English and Turkish resources with account-backed operator preferences. Tenant staff and customer localization remain open under [TD-0011](/doc/buildlog/tech-debt-ledger.md#td-0011). |
| English-first lint enforcement | Implemented | `TabFlow.Analyzers.EnglishFirstIdentifierAnalyzer` enforces ASCII identifiers as `TF0001`; analyzer metadata and regression tests are part of the baseline. |
| GitHub Actions CI workflows | In progress | Workflow files exist and split fast-path/unit work from heavier tiers. Hosted PR/tag validation remains open under [TD-0005](/doc/buildlog/tech-debt-ledger.md#td-0005). |
| Branch protection on `main` | Target | Spec lives in [`/doc/docs/how-to/configure-branch-protection.md`](/doc/docs/how-to/configure-branch-protection.md); repository-host configuration remains open under [TD-0006](/doc/buildlog/tech-debt-ledger.md#td-0006). |

Rows move to `In progress` and then `Implemented` as each capability
meets the constitution's tested, observable, and documented bar.
