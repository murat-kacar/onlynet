# UI Stakeholder Surface Analysis

This document maps TabFlow's people, devices, and operational moments to
the product surfaces they should see. It is the input document for UI
planning before frontend implementation work starts.

Read it before changing routes, page composition, navigation, or the
design system.

Related references:

- [`/doc/docs/reference/architecture/runtime-surfaces.md`](/doc/docs/reference/architecture/runtime-surfaces.md)
- [`/doc/docs/reference/design-system.md`](/doc/docs/reference/design-system.md)
- [`/doc/docs/reference/frontend-rewrite-charter.md`](/doc/docs/reference/frontend-rewrite-charter.md)
- [`/doc/docs/reference/glossary.md`](/doc/docs/reference/glossary.md)

## Analysis Rule

UI planning starts from work, not components.

For every surface, ask:

- Who arrives here?
- What decision or action must they complete?
- What information must be visible before they act?
- What must be hidden to prevent mistakes?
- What happens if the screen is slow, confusing, or wrong?

Frontend route boundaries, render modes, and component boundaries follow
from those answers.

## Accepted UI Planning Decisions

These decisions are current v1.0.0 planning contracts. They are not
historical notes.

| ID | Decision | Reason | Documentation impact |
| --- | --- | --- | --- |
| UI-001 | `/tables` is the table setup/configuration surface; `/service` is the live floor and cash workspace. | Setup work and shift work have different users, risk profiles, density, and action cadence. Separating them prevents cashiers from operating inside configuration UI and prevents managers from treating live service state as setup data. | `runtime-surfaces.md` owns the route map; this document owns the stakeholder rationale. |
| UI-002 | Station operators land on `/stations/{stationCode}` by default. `/kitchen` is the all-station overview for managers or cross-station operators. | A station operator should see only the queue they can act on. Managers need an overview, but that view must not be the default for focused station work. | Station-scoped routing, role checks, and station context must be reflected in runtime surfaces and tests. |
| UI-003 | Tenant onboarding uses both a guided setup checklist and a dashboard attention queue. | First-run setup needs explicit guidance; ongoing operations need persistent visibility into incomplete or unhealthy setup. | Tenant overview and setup-console design must support both checklist progress and operational attention cards. |
| UI-004 | The v1.0.0 bill lifecycle exposes `close bill` only. Move, merge, and split are deferred. | Close bill is the minimum operational checkout path. Move/merge/split carry high error risk and should not dilute the first-release service workflow. | Runtime docs and UI copy must not imply move/merge/split are available in v1.0.0. Any placeholder belongs in the tech debt ledger or future capability planning, not visible UI. |
| UI-005 | Customers can track order progress after submission. Status progression is driven by station workflow. | Customer trust improves when order state is visible, and the station board is the source of truth for preparing/ready/served transitions. | Customer order-complete/status surfaces must reflect station-driven `submitted`, `preparing`, `ready`, `served`, and `cancelled` states. |
| UI-006 | Turkish and English are designed together for user-facing UI. | Text length, tone, and operational terms affect layout and comprehension. Retrofitting Turkish later would create avoidable UI churn. | Copy, empty states, validation text, and status labels must be planned in both `en-GB` and `tr-TR`. |

## Stakeholder Map

| Stakeholder | Primary goal | Default device | Session pattern | Risk if underserved |
| --- | --- | --- | --- | --- |
| Platform owner | Govern the whole TabFlow deployment | Desktop | Infrequent, high-impact | Tenant lifecycle mistakes, weak operational oversight |
| Platform admin / support | Provision tenants and investigate jobs | Desktop | Episodic, support-driven | Slow tenant onboarding, poor incident response |
| Tenant owner | Configure the cafe and trust the system | Desktop or tablet | Setup-heavy, periodic | Bad initial setup, weak adoption |
| Tenant manager | Maintain menu, staff, stations, tables, and settings | Desktop or tablet | Repeated operational admin | Configuration drift, staff friction |
| Cashier / floor staff | Keep the dining room moving | Tablet or desktop terminal | Continuous shift work | Wrong table action, slow checkout, missed ready items |
| Kitchen / station operator | Progress items quickly and accurately | Tablet or station display | Continuous, time-sensitive | Late orders, wrong item status, missed notes |
| Customer / diner | Join, browse, order, and understand status | Phone | Short, distraction-prone | Abandoned order, wrong table/order, trust loss |
| Technical operator | Deploy, supervise, recover, inspect health | Desktop / terminal | Rare, high-stakes | Downtime, bad recovery, unsafe workaround |
| Table device | Display and rotate QR tokens | ESP32 display | Always-on | Stale QR, unauthorized remote ordering |

## Surface Families

| Family | Audience | Current routes | v1.0.0 target routes | Target UI posture |
| --- | --- | --- | --- | --- |
| Platform control plane | Platform owner, platform admin, support | `/`, `/tenants`, `/tenants/new`, `/tenants/{id}`, `/jobs`, `/audit`, `/settings` | Same routes | Dense admin console: table + inspector, overlays for create/edit, job/audit visibility |
| Tenant setup console | Tenant owner, manager | `/tables`, `/settings` | `/tables`, `/settings`, later `/console/**` modules as setup scope grows | Guided setup plus dense management tools; inspector-first for records |
| Floor and cash workspace | Cashier, floor staff, manager | `/table/{tableNumber}` | `/service`, `/table/{tableNumber}` | Shift console optimized for table state, checkout proof, close bill, and realtime feedback |
| Station board | Kitchen / station operator | `/kitchen` | `/stations/{stationCode}` for station operators; `/kitchen` for all-station manager overview | Station-scoped work queue by default; all-station overview for managers |
| Customer ordering | Customer / diner | `/menu`, `/cart`, `/scan-qr`, `/order-complete/{orderId}` | `/g/{token}`, `/menu`, `/cart`, `/order-complete/{orderId}` plus an order-progress model | Phone-first Static SSR flow with order progress and clear physical-presence gates |
| Identity and activation | Platform user, tenant user, tenant owner | `/login`, `/change-password`, tenant `/activate`, `/login-2fa` | Same routes | Static, focused, low-decoration task pages |
| Technical operations | Technical operator | `/health`, `/health/live`, `/health/ready`, docs/how-to | Same surfaces | Not a product UI; status must be machine-readable and operator-readable |
| Table device endpoint | ESP32 table device | `/ws/tables/{tableNumber}` | Same endpoint | Firmware protocol surface; no human UI |

## Stakeholder Details

### Platform Owner

#### Work

- See whether the deployment is healthy.
- Know which tenants exist and what state they are in.
- Delegate platform administration without giving away owner authority.
- Review platform audit records for sensitive actions.

#### Surfaces

| Surface | Must see | Must not distract with |
| --- | --- | --- |
| Platform dashboard `/` | tenant count, unhealthy jobs, recent provisioning changes, audit attention | marketing-style hero content, duplicate navigation |
| Tenants `/tenants` | tenant identity, status, primary domain, region settings, last activity | per-tenant operational noise better owned by tenant console |
| Tenant detail `/tenants/{id}` | lifecycle state, provisioning history, runtime anchors, owner email, database identifiers | raw secrets, unfiltered logs |
| Jobs `/jobs` | failed/claimed/running jobs, retry context, failure detail | low-level worker chatter unless selected |
| Audit `/audit` | actor, action, resource, timestamp, correlation | unrelated tenant business events |
| Settings `/settings` | own language, density, password/security controls | platform-wide configuration unrelated to self-management |

#### Impact

- **Business impact:** high. This role controls tenant onboarding and
  platform trust.
- **Operational risk:** high on tenant creation and job recovery.
- **Security/privacy risk:** high because owner email, audit IP, and
  database identifiers may be visible.
- **UX complexity:** medium; primarily dense tables and inspectors.
- **Frontend complexity:** medium; Interactive Server is appropriate.
- **Backend dependency:** tenant registry, provisioning job reads,
  platform audit reads, platform identity.
- **Test need:** authorization checks, empty/failure states, table +
  inspector selection, create/edit overlay validation.

### Platform Admin / Support

#### Work

- Create tenants from a request.
- Inspect failed provisioning jobs.
- Answer "is this tenant live?" quickly.
- Escalate suspicious platform actions.

#### Surfaces

| Surface | Must see | Must not do |
| --- | --- | --- |
| `/tenants` | fast filtering by code/domain/status | edit platform owners |
| `/tenants/new` | one focused tenant creation workflow | expose database implementation fields before creation |
| `/jobs` | job state, claim owner, last update, failure detail | hide failure reason behind multiple clicks |
| `/audit` | searchable recent platform changes | mix tenant staff actions into the platform audit stream |

#### Impact

- **Business impact:** high during onboarding and support.
- **Operational risk:** high when retrying or interpreting job state.
- **Security/privacy risk:** medium-high; support sees owner contact
  data and platform audit records.
- **UX complexity:** medium; support needs speed and precision.
- **Frontend complexity:** medium; table + inspector plus retry action
  overlays are enough.
- **Backend dependency:** provisioning job detail, tenant detail, audit
  read service.
- **Test need:** failure-state rendering, permission-gated actions,
  no secret leakage in detail panels.

### Tenant Owner

#### Work

- Activate the tenant admin account.
- Complete first setup: business settings, tables, stations, menu,
  staff, firmware defaults.
- Verify the cafe is ready to accept customer orders.
- Review audit and operational health.

#### Surfaces

| Surface | Must see | Must not distract with |
| --- | --- | --- |
| `/activate` | email, expiry, password setup, MFA/security requirement | tenant console chrome |
| Tenant overview target | setup progress, missing critical configuration, active issues | deep reports before setup is complete |
| Catalog target | categories, menu items, availability, station routing gaps | kitchen execution details |
| Stations target | station code, active state, color, fallback station | customer-facing copy |
| Tables `/tables` | table identity, QR/device state, active session marker, configuration controls | order-line density better owned by service view |
| Users target | staff roles, invitations, MFA/security status | platform users |
| Audit target | tenant-sensitive changes | platform tenant provisioning internals |
| `/settings` | own preference and security controls | tenant-wide setup controls that need owner/manager context |

#### Impact

- **Business impact:** very high; first impression and setup quality
  decide adoption.
- **Operational risk:** high if fallback station, table identity, or
  staff roles are unclear.
- **Security/privacy risk:** high around users, audit, and activation.
- **UX complexity:** high because first-run guidance and dense admin
  tools must coexist.
- **Frontend complexity:** high; needs setup progress, overlays, record
  inspectors, validation, and strong empty states.
- **Backend dependency:** tenant identity, station/table/menu/user
  management, audit, firmware defaults.
- **Test need:** role visibility, setup completion gates, destructive
  confirmation flows, localization readiness.

### Tenant Manager

#### Work

- Keep daily configuration accurate.
- Adjust menu availability and station routing.
- Manage floor layout and staff below owner.
- Inspect operational exceptions without platform access.

#### Surfaces

| Surface | Must see | Must not do |
| --- | --- | --- |
| Catalog target | item availability, price, category, station route, missing route warnings | platform billing/provisioning |
| Stations target | queue health, active/inactive station, fallback coverage | owner-only user/security operations |
| Tables `/tables` | active/inactive tables, device health, labels/codes | low-level device key secrets |
| Users target | staff list and role assignment within policy | edit tenant owner without explicit owner policy |
| Audit target | tenant business/security actions | platform audit |
| `/settings` | own preferences | global destructive setup unless explicitly authorized |

#### Impact

- **Business impact:** high; manager errors become service errors.
- **Operational risk:** high for station routing and availability.
- **Security/privacy risk:** medium-high for staff data and audit.
- **UX complexity:** high; needs dense editing but safe defaults.
- **Frontend complexity:** high; tables, inspectors, drawers,
  optimistic feedback, validation.
- **Backend dependency:** catalog/station/table/user APIs still need
  depth beyond current table and kitchen endpoints.
- **Test need:** role matrix, validation, empty states, auditability of
  mutations.

### Cashier / Floor Staff

#### Work

- See the current room state at a glance.
- Open the live service workspace.
- Issue checkout proof.
- Close bills.
- React to ready orders and table/device issues.

#### Surfaces

| Surface | Must see | Must not distract with |
| --- | --- | --- |
| `/service` target | live table grid, open session, elapsed time, cart/order state, ready items, close-bill affordance | setup forms and owner-level settings |
| `/table/{tableNumber}` | current table workspace, cart, orders, checkout-proof action, close-bill action, device/QR status | unrelated tables unless summarized |
| `/order/{orderId}` | order lines, item status, table context | menu management |
| Close-bill overlay target | payment context, final total, confirmation, success/failure feedback | future bill-lifecycle controls in v1.0.0 |

#### Impact

- **Business impact:** very high during service.
- **Operational risk:** very high; wrong table or wrong bill action is
  expensive.
- **Security/privacy risk:** medium; sees customer notes and payments.
- **UX complexity:** very high; speed, touch ergonomics, and realtime
  clarity matter.
- **Frontend complexity:** very high; this is the core Interactive
  Server operator surface.
- **Backend dependency:** table workspace, checkout proof, close-bill
  lifecycle, order reads, event bus.
- **Test need:** realtime updates, destructive confirmations,
  wrong-table prevention, role-gated mutations.

### Kitchen / Station Operator

#### Work

- See items that need action now.
- Progress item status without hunting.
- Read notes accurately.
- Avoid seeing work for other stations unless explicitly scoped.

#### Surfaces

| Surface | Must see | Must not distract with |
| --- | --- | --- |
| `/stations/{stationCode}` target | station-scoped submitted/preparing items, table, quantity, notes, elapsed time, urgency, status controls | all-station noise unless manager mode |
| `/kitchen` | all-station overview, station filters, queue health, blocked/urgent items | billing, tenant setup, platform information |

#### Impact

- **Business impact:** very high; directly affects service speed.
- **Operational risk:** very high for missed notes, wrong status, stale
  queue state.
- **Security/privacy risk:** medium; dietary notes can be sensitive.
- **UX complexity:** high; visual hierarchy must survive pressure.
- **Frontend complexity:** high; realtime push and large touch targets.
- **Backend dependency:** kitchen order read/update endpoint, event bus,
  station routing.
- **Test need:** status transitions, stale event recovery,
  accessibility contrast and touch target checks.

### Customer / Diner

#### Work

- Join the correct table.
- Browse quickly on a phone.
- Build a cart without losing state.
- Understand when a fresh QR scan is required.
- Submit once and trust the result.
- Track order progress after submission.

#### Surfaces

| Surface | Must see | Must not distract with |
| --- | --- | --- |
| `/g/{token}` target | table identity, join progress, error if QR is stale | internal token details |
| `/scan-qr` today | camera flow, manual fallback, clear failure recovery | staff/tenant setup concepts |
| `/menu` | categories, item availability, price, cart affordance | dense admin controls |
| `/cart` | line items, quantities, notes, total, submit action | station routing internals |
| Checkout QR step | why another scan is needed, success/failure recovery | security jargon |
| `/order-complete/{orderId}` | confirmation, order id, current order progress, next useful action | raw payment/reconciliation internals |
| Order-progress target | `submitted`, `preparing`, `ready`, `served`, `cancelled` in customer language | station routing internals or staff-only controls |

#### Impact

- **Business impact:** very high; customer friction directly affects
  order conversion.
- **Operational risk:** high; wrong table/order erodes trust.
- **Security/privacy risk:** high around access tickets, dietary notes,
  and physical-presence proof.
- **UX complexity:** very high; phone-first, low patience, unreliable
  camera/network conditions.
- **Frontend complexity:** high; target is Static SSR with enhanced
  forms, but current implementation still uses Interactive Server.
- **Backend dependency:** public session/cart/catalog/orders APIs,
  QR/token/device-cookie enforcement.
- **Test need:** mobile layout, stale QR, missing cookie, duplicate
  submit, progress updates, no-localStorage cart, Static SSR conversion
  checks, Turkish and English copy fit.

### Technical Operator

#### Work

- Deploy and supervise hosts.
- Check health and readiness.
- Bootstrap platform and tenants.
- Restore from backup and rotate secrets.

#### Surfaces

| Surface | Must see | Must not become |
| --- | --- | --- |
| `/health`, `/health/live`, `/health/ready` | machine-readable status, named failing probe | a human dashboard replacement |
| Operational how-tos | exact commands, evidence to capture, rollback points | product UI copy |
| Platform jobs `/jobs` | provisioning state when support needs UI visibility | full worker-control plane unless designed |

#### Impact

- **Business impact:** high during deploy/recovery.
- **Operational risk:** very high; bad recovery can worsen incidents.
- **Security/privacy risk:** high; secrets and backup data are involved.
- **UX complexity:** low in product UI, high in procedural docs.
- **Frontend complexity:** low; health is backend/API-first.
- **Backend dependency:** health checks, logs, systemd, backup scripts,
  migration state.
- **Test need:** smoke checks, health response contract, recovery drill
  evidence.

### Table Device

#### Work

- Connect to tenant host.
- Authenticate with its device key.
- Receive current QR token.
- Refresh display and heartbeat state.

#### Surfaces

| Surface | Must expose | Must not expose |
| --- | --- | --- |
| `/ws/tables/{tableNumber}` | token payload, refresh, heartbeat, disconnect behavior | tenant admin or customer APIs |
| Firmware reference | protocol, payloads, reconnect rules | human UI controls |

#### Impact

- **Business impact:** high; stale QR breaks ordering.
- **Operational risk:** high; offline devices create table confusion.
- **Security/privacy risk:** high; device key misuse can affect ordering.
- **UX complexity:** indirect; the human UI must surface device health.
- **Frontend complexity:** medium in staff console; none on the device
  endpoint itself.
- **Backend dependency:** WebSocket handler, device key validation,
  event bus/device events.
- **Test need:** auth rejection, reconnect, token rotation, visible
  offline/online state.

## Cross-Stakeholder Conflicts

| Conflict | Example | UI rule |
| --- | --- | --- |
| Setup depth vs shift speed | Manager needs editable table metadata; cashier needs fast table action | `/tables` is setup/configuration; `/service` is live service |
| Customer simplicity vs security | Customer dislikes second scan; backend needs physical-presence proof | Explain the action as "scan the table QR to submit" without exposing token jargon |
| Dense admin data vs safe action | Tenant list and job table are dense; create/update actions are risky | Use table + inspector for review, overlay for mutations |
| Station focus vs manager oversight | Cook needs only their queue; manager may need all stations | `/stations/{stationCode}` is station default; `/kitchen` is all-station overview |
| Realtime confidence vs server cost | Staff needs push; customer does not | Interactive Server for staff, Static SSR target for customer |
| Support visibility vs privacy | Support needs owner email and job failures; tenant data is private | Platform surfaces show platform lifecycle data, not tenant business detail |
| Fast v1 checkout vs full bill operations | Cashier must close bills; move/merge/split adds high-risk scope | v1.0.0 exposes close bill only |
| English layout vs Turkish copy | Turkish labels can be longer than English labels | Design and test both languages from the start |

## Impact Scale

Use this scale when prioritizing UI work.

| Score | Business impact | Operational risk | UX complexity | Frontend complexity |
| --- | --- | --- | --- | --- |
| 1 | Cosmetic or rare | No workflow risk | Simple static content | Static page or copy change |
| 2 | Small workflow gain | Recoverable confusion | Basic form/table | One page, local state |
| 3 | Daily workflow | Mistakes cost minutes | Empty/error/loading states matter | Shared component or API dependency |
| 4 | Shift-critical | Mistakes affect service | Realtime/touch/mobile constraints | Multi-surface state and event handling |
| 5 | Trust-critical | Mistakes affect money, security, or production | Must be obvious under stress | Cross-cutting architecture and test work |

## Initial Priority View

| Priority | Surface | Why |
| --- | --- | --- |
| P0 | Customer ordering flow | Highest conversion/trust impact; also drives Static SSR conversion and public API shape |
| P0 | Floor and cash workspace | Shift-critical operator surface; determines table, bill, and checkout UX |
| P0 | Station board | Direct service-speed impact; needs realtime clarity |
| P1 | Tenant setup console | Required for successful onboarding and clean operations |
| P1 | Platform tenant/job console | Required for provisioning confidence and support |
| P2 | Audit/reports/deeper admin | Important, but should follow core operational flow clarity |
| P2 | External/public integration docs | No external developer API in v1.0.0 baseline |

## Documentation Outputs From This Analysis

This analysis should feed these documents:

- `runtime-surfaces.md`: route, role, and render-mode updates.
- `design-system.md`: layout, component, state, and density rules.
- `frontend-rewrite-charter.md`: rewrite sequence and acceptance bar.
- `capability-matrix.md`: status changes for UI capability rows.
- `userdocs/`: persona folders once stable workflows are ready.
- `tech-debt-ledger.md`: any accepted temporary UI compromise.

## Open UI Planning Questions

These questions should be answered before implementation slices are
planned:

- What is the exact route and information model for customer order
  progress after `/order-complete/{orderId}`?
- Should station progress updates be pushed to customer status pages,
  polled from Static SSR, or refreshed by enhanced navigation?
- Which tenant setup checklist items are release-blocking for v1.0.0?
- Which backend endpoint shape is required for close-bill v1.0.0?
- Which UI strings are part of the shared glossary and must be reviewed
  in both Turkish and English before implementation?
