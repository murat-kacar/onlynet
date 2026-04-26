# Threat Model

This document is the baseline threat model for TabFlow. It enumerates
the assets we protect, the actors who may attack them, the trust
boundaries between system components, and the threats that follow from
crossing each boundary. It uses the [STRIDE](https://en.wikipedia.org/wiki/STRIDE_model)
classification (Spoofing, Tampering, Repudiation, Information disclosure,
Denial of service, Elevation of privilege).

The model is updated whenever a new component, a new external boundary,
or a new asset class is introduced. It is reviewed at every release gate
([`../../meta/release-gate.md`](../../meta/release-gate.md)).

## Assets

The things we protect, in priority order:

| Asset | Why It Matters | Where It Lives |
| --- | --- | --- |
| Customer payment data and order intent | Financial loss, fraud, compliance | Tenant DB `customer_sessions`, `orders`; in-memory carts |
| Cross-tenant isolation | One cafe must never see another's data | Tenant DB boundary (AD-0001), tenant host process boundary (AD-0003) |
| Platform admin credentials | Full provisioning + tenant control | `tabflow_platform.AspNetUsers` |
| Tenant staff credentials | Full tenant control | Per-tenant `AspNetUsers` |
| Tenant operational data | Menu, orders, audit log | Per-tenant DB |
| QR tokens and customer access tickets | Customer session integrity | Tenant DB `qr_tokens`, `customer_access_tickets` |
| Device WebSocket integrity | Table device authenticity | `/ws/tables/{tableNumber}` channel |
| Audit logs | Forensics, regulatory, incident response | Both audit log tables |

## Actors

The people and systems that can interact with TabFlow:

| Actor | Channel | Trust |
| --- | --- | --- |
| Platform admin | `https://<platform-host>/login` cookie session | High — `owner` role on the platform |
| Tenant staff (owner, manager, cashier) | `https://<tenant-domain>/login` cookie session | Medium — full control of one tenant |
| Customer (diner) | `https://<tenant-domain>/g/{token}` then `/menu` | Low — anonymous, scoped to one bill |
| ESP32 table device | `wss://<tenant-domain>/ws/tables/{tableNumber}` | Low (deferred to AD-0005 successor) — placeholder authentication |
| Platform worker | Loopback to platform DB | Trusted system component |
| Reverse proxy | `127.0.0.1:<port>` to host | Trusted infrastructure |
| External attacker on the public internet | TLS endpoint of a tenant or platform host | Untrusted |

## Trust Boundaries

A trust boundary is a place where data crosses from a less-trusted zone
into a more-trusted zone. Each boundary is a place where we MUST
validate, authenticate, or authorise.

```
                   internet
                      |
                      v
  ┌──────────────────────────────────────────────┐
  │ Reverse proxy (TLS termination)              │  Boundary A
  └──────────────────────────────────────────────┘
                      |
                      v
  ┌──────────────────────────────────────────────┐
  │ Platform host  |  Tenant host  |  WS upgrade │  Boundary B
  │ (cookie auth)  | (cookie auth) | (table auth)│
  └──────────────────────────────────────────────┘
                      |
                      v
  ┌──────────────────────────────────────────────┐
  │ Application services (per host)              │  Boundary C
  │  - role policy gates                         │
  │  - tenant code resolution (tenant host)      │
  │  - audit write                               │
  └──────────────────────────────────────────────┘
                      |
                      v
  ┌──────────────────────────────────────────────┐
  │ PostgreSQL (per database)                    │  Boundary D
  │  - per-host DB role with table-level rights  │
  │  - no cross-database access                  │
  └──────────────────────────────────────────────┘
```

- **Boundary A** — public internet to TLS-terminated traffic.
- **Boundary B** — HTTP/WS to authenticated host code.
- **Boundary C** — host code to application services (where role checks
  and tenant resolution happen).
- **Boundary D** — application services to the database (where
  per-tenant or per-host DB role enforces the tenancy contract).

## STRIDE Per Boundary

### Boundary A: Internet → Reverse Proxy

| Threat | Mitigation |
| --- | --- |
| **S** Spoofing of host identity (DNS hijack) | TLS via certificate pinning at the reverse proxy; CAA records on tenant domains |
| **T** Tampering with traffic (MITM) | TLS 1.3 only; HSTS preload |
| **D** Volumetric DoS | Rate limiting at proxy; CDN/WAF in front for high-traffic tenants (deferred until needed) |
| **I** TLS downgrade | Disable TLS < 1.2; reject SSLv3 |

### Boundary B: Proxy → Host (HTTP and WS)

| Threat | Mitigation |
| --- | --- |
| **S** Cookie theft, session fixation | `Secure`, `HttpOnly`, `SameSite=Strict`; ASP.NET Core Identity rotates cookies on auth state change |
| **S** WebSocket impersonation by stolen device key | Each table device key bound to one `tableNumber`; reuse triggers `device.disconnected` audit + admin alert |
| **T** Cookie forgery | Server-signed encrypted cookies; key rotated per `rotate-secrets.md` |
| **R** Repudiation of admin actions | Every state-changing admin call writes a `platform_audit_log` or `tenant_audit_log` row |
| **I** Sensitive header leakage | No secrets in URLs or query strings; reverse proxy strips `X-Forwarded-*` from untrusted sources |
| **D** Slow-loris and request flood | Kestrel limits configured; per-IP rate limit on `/login`, `/g/{token}`, and `/api/public/orders` |
| **E** Privilege escalation by login replay | Identity lockout policy after 5 failed attempts in 15 min |

### Boundary C: Host → Application Services

| Threat | Mitigation |
| --- | --- |
| **S** Tenant context confusion (tenant A code reaching tenant B's data) | `TABFLOW_TENANT_CODE` env var resolved once at boot; every request runs against `TenantDbContext` bound to that one tenant |
| **T** Cross-role action (cashier triggering owner-only flow) | ASP.NET Core authorization policies on every controller and Blazor route. A missing policy is rejected at startup by `AddAuthorization`'s `FallbackPolicy` (see AD-0005) and surfaces in the Identity policy registration test pending under [TD-0010 step 5](/doc/buildlog/tech-debt-ledger.md#td-0010); making the missing-policy case a Roslyn-time error remains future work tracked under TD-0009 follow-up. |
| **R** Repudiation of customer actions | Customer access tickets persist; orders carry the originating ticket ID |
| **I** Unintended PII exposure in logs | Serilog filters strip known PII fields (email, password hash, payment fields) before sink write |
| **D** Event-bus saturation | Bounded `Channel<T>` per topic; `event-bus:capacity` health probe degrades to `warn` at threshold |
| **E** Bypass of policy via untyped binder | Strongly-typed DTOs at boundaries; raw `Dictionary<string,object>` is forbidden by analyzer rule |

### Boundary D: Service → Database

| Threat | Mitigation |
| --- | --- |
| **S** Connection-string substitution | Connection strings come from host-owned config only; never from user input |
| **T** SQL injection | EF Core parameterised queries only; raw SQL requires `[SuppressMessage("SQL.RawSql.Justified")]` and an ADR snippet |
| **T** Cross-tenant write via direct DB connection | Each tenant DB has its own role with grants only on that DB; the platform role has no grants on any tenant DB |
| **R** Repudiation of DB writes | `platform_audit_log` and `tenant_audit_log` are append-only and capture actor email + IP + UA |
| **I** Backup / dump leakage | Backups encrypted at rest; access via deploy-time secret manager only. Implementation deferred under the capability-matrix "Encrypted backup with off-site copy" row (`Target`); the wiring lands when the first backup ships per [`/doc/docs/how-to/backup-and-restore.md`](/doc/docs/how-to/backup-and-restore.md). |
| **D** Unbounded query | Every read endpoint paginates. The analyzer that would flag `IQueryable.ToList()` without `Take()` is not yet shipped (see [TD-0009](/doc/buildlog/tech-debt-ledger.md#td-0009) for the analyzer baseline; the unbounded-query rule is a future addition to `TabFlow.Analyzers`). Today the rule is enforced in code review. |
| **E** Database role escalation | Roles created `NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION`; verified at bootstrap and provisioning |

## Out-Of-Scope For This Baseline

These are real concerns but not addressed by this iteration. Each gets a
tech-debt ledger entry the moment it becomes load-bearing:

- Hardware tampering of ESP32 devices in the cafe
- Insider threats from operators with shell access
- Dependency supply-chain attacks beyond the current SAST + dependency
  audit gate
- Multi-region failover and the threat surface a second region adds

## Updates

This document is a `Status: Accepted` decision artefact. Material
changes (new boundary, new asset class, retired mitigation) follow the
constitution's amendment rule:

1. PR with the change and the motivating experience.
2. Reviewed by every active maintainer.
3. Linked from the relevant ADR.

The release gate verifies that every accepted ADR with a security
implication has a corresponding row in this document.

## Related

- [`../../constitution.md`](../../constitution.md) Section V.4 — security
  review trigger
- [`../../meta/review-policy.md`](../../meta/review-policy.md) — review
  checklist for security-sensitive PRs
- [`../../reference/architecture/decisions.md`](../../reference/architecture/decisions.md)
  AD-0001, AD-0003, AD-0005
- [`../../reference/acceptance-criteria.md`](../../reference/acceptance-criteria.md)
  — invariants the threat model protects
- [`/SECURITY.md`](/SECURITY.md) — vulnerability disclosure policy
