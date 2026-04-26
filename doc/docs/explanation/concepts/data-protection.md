# Data Protection

This document describes how TabFlow handles personal data under
**KVKK** (Kişisel Verilerin Korunması Kanunu, Türkiye, Law No. 6698)
and **GDPR** (Regulation (EU) 2016/679). It is the single source of
truth for data classification, lawful basis, retention periods, and
the operational procedures that satisfy data-subject rights.

This document is a **baseline contract**. Tenants whose deployments
require additional or stricter handling (PCI DSS, HIPAA, sector-specific
rules) extend it via a tenant-specific addendum, not by editing this
document.

## Roles

| Role | Definition | TabFlow Mapping |
| --- | --- | --- |
| Data subject | The natural person the data is about | A diner ordering at a tenant cafe; a tenant staff member |
| Data controller | The party who decides why and how data is processed | The **tenant** (the cafe operator). They own their customers' data. |
| Data processor | The party who processes data on behalf of the controller | **TabFlow** (the platform operator). Processing is bounded by this document and the tenant's deployment configuration. |
| Joint controllers | Two parties who jointly determine purposes and means | Not applicable in the current major. If TabFlow gains analytics or marketing features that act on customer data across tenants, it becomes a joint controller and this section is amended. |

## Data Classification

Every field in the system falls into one of four classes. The class
determines storage, access, retention, and audit requirements.

| Class | Definition | Examples In TabFlow |
| --- | --- | --- |
| **Public** | Safe to expose without restriction | Tenant trading name shown on the customer ordering page; public menu items |
| **Internal** | Operational data, not personal, but not for public exposure | Order totals, table numbers, station identifiers, audit `event_key` values |
| **Sensitive** | Personal data within KVKK / GDPR scope | Staff email, staff name, audit log actor IP and user-agent, customer session language preference once tied to a returning customer identifier |
| **Restricted** | Special-category personal data, payment data, or authentication secrets | Password hashes, session cookies, payment tokens (when the payment integration ships), any health or dietary data a customer voluntarily provides |

Schema rule (target shape): every personal-data column carries a
comment classifying it. The comment is generated from `[DataClass]`
attributes on the entity properties; CI fails the build if a
`Sensitive` or `Restricted` column has no comment in the schema dump.

> **Implementation status (TD-0007, PR #32).** The `[DataClass]`
> attribute and the schema-comment generator shipped in PR #32:
> [`DataClassification`](/src/packages/shared-dotnet/Domain/DataProtection/DataClassification.cs)
> enum, [`DataClassAttribute`](/src/packages/shared-dotnet/Domain/DataProtection/DataClassAttribute.cs)
> on `AttributeTargets.Property`, and the
> [`ApplyDataClassComments()`](/src/packages/shared-dotnet/Infrastructure/Data/ModelBuilderExtensions.cs)
> ModelBuilder extension wired into both DbContexts. Sample
> annotations land on the audit-log entities and on
> `CustomerAccessTicket.DeviceCookieValue`. The corresponding
> acceptance criterion is **AC-122**. The full annotation sweep
> across every personal-data property (TD-0007 step 3) and the
> release-gate "every Sensitive/Restricted column has a comment"
> check (TD-0007 step 4) remain open.
>
> This explainer states the
> contract; the wiring lands under
> [TD-0007](/doc/buildlog/tech-debt-ledger.md#td-0007).
>
> Until then, the schema reference
> ([`../../reference/database/schema.md`](../../reference/database/schema.md))
> documents which tables hold personal data so reviewers can apply
> the classification by hand.

## Lawful Basis For Processing

Each processing activity in TabFlow has one named lawful basis. We
do not rely on consent for activities that are necessary to deliver
the service; consent is reserved for genuinely optional processing.

| Activity | Personal Data | Lawful Basis (GDPR) | KVKK Equivalent |
| --- | --- | --- | --- |
| Authenticating staff user | Email, password hash, login IP | Article 6(1)(b) — performance of a contract | Art. 5(2)(c) — sözleşmenin kurulması veya ifası |
| Recording an order under a customer session | Session ID, optional contact handle | Article 6(1)(b) — performance of a contract | Art. 5(2)(c) |
| Writing audit log entries | Actor email, IP, user-agent | Article 6(1)(c) — legal obligation; Article 6(1)(f) — legitimate interest in fraud prevention | Art. 5(2)(ç) — kanuni yükümlülük; Art. 5(2)(f) — meşru menfaat |
| Sending operational notifications to staff | Email | Article 6(1)(b) | Art. 5(2)(c) |
| Marketing or analytics across tenants | n/a | n/a — not performed | n/a |

A new processing activity requires:

1. an entry in this table,
2. an updated retention schedule (below), and
3. an ADR if it changes the controller / processor relationship.

## Retention Schedule

The default retention windows for the current major. A tenant may
elect a **shorter** window via configuration; longer windows require a
documented legal obligation.

| Data | Default Retention | Trigger To Delete |
| --- | --- | --- |
| Staff account record | Active life of staff role + 12 months after deactivation | Staff `deactivated_at + 12 months` job sweep |
| Customer session | 90 days from last activity | Daily sweep; closed sessions older than 90 days hard-deleted |
| Customer cart contents on a closed session | Deleted on session close | Real time |
| Audit log entries (tenant) | 24 months | Monthly sweep |
| Audit log entries (platform) | 60 months | Monthly sweep |
| Order and bill records | Per tenant tax-law obligation; default 60 months | Annual sweep |
| Database backups | 30 days encrypted retention; older copies destroyed | See [`../../how-to/backup-and-restore.md`](../../how-to/backup-and-restore.md) |
| Application logs | 30 days | Logging sink retention |

A retention sweep is a scheduled platform-worker job. Each sweep
writes an audit log entry summarising what was deleted; it never
deletes audit log entries about itself.

## Data Subject Rights

KVKK and GDPR grant overlapping but not identical rights. TabFlow
supports the **superset**: a request that satisfies one regulation is
honoured under both.

| Right | KVKK Article | GDPR Article | TabFlow Procedure |
| --- | --- | --- | --- |
| Right to be informed | 10 | 13–14 | Privacy notice published per tenant; link rendered on customer ordering surface |
| Right of access | 11(1)(a–c) | 15 | Operator runs [`/doc/docs/how-to/data-subject-access.md`](/doc/docs/how-to/data-subject-access.md) and delivers the JSON export to the requester within 30 days |
| Right to rectification | 11(1)(d–e) | 16 | Staff edit; customer correction via operator request |
| Right to erasure | 11(1)(e–f), 7 | 17 | Operator runs [`/doc/docs/how-to/data-subject-erasure.md`](/doc/docs/how-to/data-subject-erasure.md); data is hard-deleted except entries needed to satisfy a legal obligation, which are anonymised in place |
| Right to restriction | 11(1)(f), 7(2) | 18 | Operator runs [`/doc/docs/how-to/data-subject-restriction.md`](/doc/docs/how-to/data-subject-restriction.md); processing pauses pending the time-bound review the procedure schedules |
| Right to data portability | n/a (no direct equivalent in KVKK) | 20 | Operator runs [`/doc/docs/how-to/data-subject-portability.md`](/doc/docs/how-to/data-subject-portability.md); the export is the access export filtered to consent / contract scope |
| Right to object | 11(1)(g) | 21 | Recorded; processing for the objection's scope ceases |
| Right not to be subject to automated decision-making | n/a | 22 | TabFlow performs no automated individual decision-making in the current major |

The procedures referenced above live as **how-to guides** in
[`/doc/docs/how-to/`](/doc/docs/how-to/). The four DSR procedures
(access, erasure, restriction, portability) ship as separate how-to
guides under PR #28. Each procedure writes its request evidence to the
operator's access-controlled DSR case system or audit log, not to the
repository. Public documentation may carry only redacted request ids and
procedure lessons; real subject identifiers, tenant-specific payloads,
or delivered exports never belong in any documentation tree.

## Data Locality And Transfer

| Data | Where It Lives | Transfer Outside Original Region |
| --- | --- | --- |
| Tenant operational database | The region declared in `RegionalSettings` at provisioning time | None by default |
| Backups | Same region as the source database | None by default |
| Application logs | Same region as the host emitting them | None by default |
| Aggregated platform metrics | Platform region | None |

Cross-border transfer requires:

1. a documented lawful basis (GDPR Article 46 or KVKK Article 9),
2. a tenant-level configuration flag enabling it, and
3. an audit-log entry on every transfer.

The current major does **not** transfer personal data outside its
origin region. A future feature that needs to (e.g. a centralised
analytics product) lands behind an ADR and a per-tenant opt-in.

## Sub-Processors

A sub-processor is a third party that processes personal data on
TabFlow's behalf as a processor. The current major has none beyond the
infrastructure providers running tenant hosts and storage.

| Sub-processor | Purpose | Region | Status |
| --- | --- | --- | --- |
| (none) | — | — | The current major runs on tenant-or-operator-controlled infrastructure |

Adding a sub-processor requires updating this table, notifying tenants
in advance, and obtaining objections per the operator agreement.

## Breach Notification

A data breach is unauthorised access, loss, alteration, or disclosure
of personal data.

Procedure on detection:

1. **Stop the line** — declare an incident per
   [`../../constitution.md`](../../constitution.md) Section VI.
2. **Contain** — revoke compromised credentials, rotate cookie keys,
   isolate the affected host. See
   [`../../how-to/rotate-secrets.md`](../../how-to/rotate-secrets.md).
3. **Assess** — determine which data classes are affected and how many
   data subjects.
4. **Notify the data controller(s)** — the affected tenant(s) — within
   24 hours of detection. The notice includes nature, scope, and
   recommended action.
5. **Coordinate regulatory notification** — KVKK Article 12(5) requires
   notification to the Authority and the data subjects "within the
   shortest time possible"; the Board's guidance is **72 hours**. GDPR
   Article 33 likewise requires notification to the supervisory
   authority within 72 hours when feasible. The data controller (the
   tenant) decides whether to notify; TabFlow provides the technical
   detail required to support that decision.
6. **Postmortem** — write a blameless postmortem in
   [`/doc/buildlog/postmortems/`](/doc/buildlog/) within 5 working days.

## Operator Responsibilities

The tenant (data controller) is responsible for:

- publishing a privacy notice to their data subjects,
- maintaining a record of processing activities,
- responding to data-subject rights requests within the regulatory
  timeline (typically 30 days under both KVKK and GDPR),
- choosing a retention window appropriate for their legal context,
- retaining counsel familiar with the data subject's jurisdiction.

TabFlow provides the technical mechanism. It does not provide legal
advice.

## Updates

This document is a `Status: Accepted` decision artefact. Material
changes (new processing activity, new sub-processor, expanded
retention, cross-border transfer) follow the constitution's amendment
rule and update the related sections atomically.

The release gate verifies that every accepted ADR with a personal-data
implication has a corresponding row in this document.

## Related

- [`./threat-model.md`](./threat-model.md) — STRIDE coverage that
  protects the data classes in this document
- [`../../constitution.md`](../../constitution.md) Section V.4 —
  security review trigger; Section VI — incident process
- [`../../meta/release-gate.md`](../../meta/release-gate.md) — gate
  verification of personal-data ADRs
- [`/SECURITY.md`](/SECURITY.md) — vulnerability disclosure
- [`../../reference/database/schema.md`](../../reference/database/schema.md)
  — column-level data classification
- [KVKK (Law 6698)](https://www.mevzuat.gov.tr/MevzuatMetin/1.5.6698.pdf)
- [GDPR (Regulation 2016/679)](https://eur-lex.europa.eu/eli/reg/2016/679/oj)
