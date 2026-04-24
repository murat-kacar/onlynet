# Service-Level Objectives

This document records the target service levels for TabFlow platform and
tenant hosts. It is the single reference for SLIs (service-level
indicators), SLOs (service-level objectives), and the error-budget policy
for this repository.

The targets below are the current working baseline. They are deliberately
conservative until real production telemetry exists; they will be revised
based on measured performance during the first operational quarter after
the first production deploy.

## Naming

- **SLI** — a directly measurable signal (for example
  `api_public_catalog_p95_latency_ms`).
- **SLO** — a target over a rolling window for a given SLI (for example
  `< 300 ms over 28 days`).
- **Error budget** — the fraction of the rolling window during which the
  SLI may fall outside the SLO before an action is taken.

## SLIs And SLOs

Rolling window is 28 days unless noted otherwise.

### Platform Host

| SLI | Measurement Source | SLO | Error Budget |
| --- | --- | --- | --- |
| `platform_availability` | Uptime of the `/health/ready` probe | `>= 99.9%` | `0.1%` |
| `platform_login_p95_latency_ms` | Server-side latency for `POST /login` end-to-end | `< 500 ms` | `1%` |
| `platform_console_interaction_p95_latency_ms` | SignalR round-trip latency on `P-02` through `P-07` | `< 500 ms` | `1%` |

### Tenant Host

| SLI | Measurement Source | SLO | Error Budget |
| --- | --- | --- | --- |
| `tenant_availability` | Uptime of the `/health/ready` probe per tenant | `>= 99.9%` | `0.1%` |
| `public_catalog_p95_latency_ms` | Server-side latency for `GET /api/public/catalog` | `< 300 ms` | `1%` |
| `qr_join_p95_latency_ms` | Server-side latency for `GET /g/{token}` including session bootstrap | `< 400 ms` | `1%` |
| `order_submit_p95_latency_ms` | Server-side latency for `POST /api/public/orders` including proof validation and event publish | `< 600 ms` | `1%` |
| `staff_interaction_p95_latency_ms` | SignalR round-trip latency on `T-06` through `T-16` | `< 500 ms` | `1%` |
| `tenant_event_push_p95_latency_ms` | Delay from `order.submitted` publish on the in-process event bus to reflection on floor workspace (`T-13`) and station boards (`T-16`) | `< 500 ms` | `1%` |
| `device_ws_handshake_p95_latency_ms` | `/ws/tables/...` handshake latency | `< 800 ms` | `1%` |

### Provisioning

| SLI | Measurement Source | SLO | Error Budget |
| --- | --- | --- | --- |
| `tenant_provisioning_success_ratio` | Ratio of successful `tenant.create` jobs to total attempts | `>= 99%` | `1%` |
| `tenant_provisioning_p95_duration_ms` | Duration from job claim to `active` status | `< 120 000 ms` | `1%` |

## Measurement

Latency SLIs are measured server-side, from request acceptance to
response flush. Interactive Server latency is measured from SignalR
event receive to DOM-diff send.

Availability is measured with an external pinger against the per-host
`/health/ready` probe at a one-minute cadence. Ping failures are
considered unavailability for the window.

Metrics and traces are exported through the OpenTelemetry .NET SDK.
Dashboards and alerts consume the OpenTelemetry output rather than
scraping application logs.

## Error-Budget Policy

When an SLO's error budget is exhausted within the rolling window:

- New non-critical feature work pauses for the affected host until the
  SLO recovers.
- An incident write-up lands in the tenant audit log and in the
  team's operational runbook.
- The next review of this document evaluates whether the SLO needs
  adjustment or whether the implementation needs targeted work.

## Review Cadence

SLOs are reviewed once per quarter and after any incident that exhausts
an error budget. Reviews can:

- tighten an SLO that was routinely over-met
- loosen an SLO that turned out to be unrealistic for the current
  hardware class or traffic pattern
- retire an SLI that no longer reflects real user experience

Any change to an SLI or SLO lands in this document; the document
itself is the canonical record.

## Related

- [`./system-overview.md`](./system-overview.md)
- [`./runtime-surfaces.md`](./runtime-surfaces.md)
