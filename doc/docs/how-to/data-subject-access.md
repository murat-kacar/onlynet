# Respond to a Data-Subject Access Request

This guide is the operator procedure for the **Right of access** that
[`../explanation/concepts/data-protection.md`](../explanation/concepts/data-protection.md#data-subject-rights)
commits TabFlow to under
**KVKK Article 11(1)(a–c)** and **GDPR Article 15**. The output is a
machine-readable JSON export covering every record that names the data
subject across the platform and tenant databases plus the audit-log
trail.

The same export shape satisfies the **Right to data portability**
(GDPR Article 20); see
[`./data-subject-portability.md`](./data-subject-portability.md) for
the portability-specific framing.

## When This Applies

A data-subject access request applies when:

- A staff member, customer, or other identifiable person submits a
  request that names them and asks for "the data you hold about me",
  the equivalent in Turkish, or any phrase that maps to KVKK 11(1)(a–c)
  / GDPR 15.
- The requester proves identity sufficient to associate them with one
  of TabFlow's identifiers (staff Identity user id, customer email,
  customer access ticket id, station device key fingerprint).

Requests received by the data controller (the tenant operator) flow
through to TabFlow as the processor; the operator acknowledges the
request to the requester and then runs this procedure.

## Required Information

Before starting:

1. **Subject identifier.** One of:
   - Tenant staff: `AspNetUsers.Id` (Guid) or unique `UserName`.
   - Customer: customer-session id (Guid) or customer access ticket
     id (Guid).
   - Station device: device key fingerprint (the SHA-256 of the
     persisted `device_key_hash` column).
2. **Tenant scope.** The tenant code (`tenants.code` on the platform
   database) whose records the request covers. Platform-only data
   subjects (platform admins) skip this.
3. **Lawful-basis confirmation.** A brief note recording that the
   requester's identity has been verified and naming the lawful basis
   the response relies on (typically: "Article 11(1) request from
   verified data subject; response prepared under Article 11(1)(a–c)").

## Procedure

The procedure runs against a read replica or a backup-restored copy
when one exists; against the live primary when no replica exists. The
queries below are read-only and safe to run against the primary.

1. **Open a secure DSR case record.** Until the operator-side audit
   log columns ship under [TD-0008](/doc/buildlog/tech-debt-ledger.md#td-0008)
   (retention sweep jobs), record the request opening in the operator's
   access-controlled case system, not in this repository. Include:
   request received-at timestamp, subject identifier, tenant scope, and
   the operator's identity. Repository documentation may reference only
   the redacted request id (`dsr-NNNN`) and must never contain real
   customer, staff, or device identifiers.

2. **Collect from the platform database.** For platform admin data
   subjects only — staff who hold a `Platform:*` policy:

   ```sql
   -- Identity surface
   SELECT * FROM "AspNetUsers"     WHERE "Id" = '<subject-id>';
   SELECT * FROM "AspNetUserRoles" WHERE "UserId" = '<subject-id>';
   SELECT * FROM "AspNetUserClaims" WHERE "UserId" = '<subject-id>';

   -- Audit log entries that name the subject
   SELECT * FROM platform_audit_log
   WHERE actor_user_id = '<subject-id>'
      OR resource_id   = '<subject-id>'::text
   ORDER BY occurred_at;
   ```

3. **Collect from the tenant database.** For staff or customers in the
   tenant scope. Replace `<tenant-code>` and `<subject-id>` as
   appropriate.

   ```sql
   -- Staff (Identity surface)
   SELECT * FROM "AspNetUsers"     WHERE "Id" = '<subject-id>';
   SELECT * FROM "AspNetUserRoles" WHERE "UserId" = '<subject-id>';

   -- Customer-session surface (no Identity row; keyed by ticket)
   SELECT * FROM customer_sessions
   WHERE id IN (
     SELECT session_id FROM customer_access_tickets
     WHERE id = '<subject-id>'
   );
   SELECT * FROM customer_session_cart_items
   WHERE session_id IN (
     SELECT session_id FROM customer_access_tickets
     WHERE id = '<subject-id>'
   );

   -- Order history bound to the customer session
   SELECT * FROM orders
   WHERE session_id IN (
     SELECT session_id FROM customer_access_tickets
     WHERE id = '<subject-id>'
   );

   -- Audit log entries that name the subject
   SELECT * FROM tenant_audit_log
   WHERE actor_user_id = '<subject-id>'
      OR resource_id   = '<subject-id>'::text
   ORDER BY occurred_at;
   ```

4. **Format the output.** The export is a single JSON document with
   one top-level key per source table. Each value is the rows the
   query returned, serialised with column names preserved. Personal
   data classified as `Sensitive` per
   [`../explanation/concepts/data-protection.md`](../explanation/concepts/data-protection.md#data-classification)
   (passwords, device-key hashes) is **omitted** even when the
   subject's row contains it; the export carries a top-level
   `omitted` array naming each (table, column) tuple that was
   redacted and the legal basis for the redaction.

5. **Sign and seal.** Compute a SHA-256 of the JSON document. Record
   the digest in the secure DSR case record opened in step 1. The digest
   plus the delivery channel forms a tamper-evident handoff trail.

6. **Deliver.** Send the JSON file plus the SHA-256 digest to the
   verified contact address the requester supplied. Encrypt at rest
   (e.g. an age-encrypted archive keyed to the requester's GPG
   public key when one is available; otherwise a password-protected
   ZIP whose password is delivered out-of-band).

## Output

A single JSON file named `dsr-access-<subject-id>-<yyyymmdd>.json`
with this shape (fields trimmed for brevity):

```json
{
  "request_id": "dsr-NNNN",
  "subject_identifier": "0bf1...",
  "tenant_code": "demo",
  "exported_at": "2026-04-26T12:00:00Z",
  "platform": {
    "AspNetUsers": [...],
    "platform_audit_log": [...]
  },
  "tenant": {
    "AspNetUsers": [...],
    "customer_sessions": [...],
    "orders": [...],
    "tenant_audit_log": [...]
  },
  "omitted": [
    { "table": "AspNetUsers", "column": "PasswordHash", "basis": "Sensitive class — Article 11(1) does not require disclosure of access credentials" }
  ]
}
```

## Audit Trail

Every access export writes a row to the audit log of every database
the export touched:

- Platform: `platform_audit_log` row with
  `action = 'dsr.access.exported'`, `resource_id = subject-id`,
  `payload` carrying the request id and SHA-256 of the export.
- Tenant: `tenant_audit_log` row with
  `action = 'dsr.access.exported'`, same payload shape.

Until the audit-log helpers ship under
[TD-0008](/doc/buildlog/tech-debt-ledger.md#td-0008), the operator
inserts the row by hand in the same transaction that wraps step 6.

## Regulatory Timing

The 30-day clock under both KVKK Article 13 and GDPR Article 12(3)
starts the moment the request is received. Aim to deliver within 14
days; the second 14 days are reserved for cross-border transfer
review and for handling extension notices when a request is
"complex" within the meaning of Article 12(3).

## Related

- [`../explanation/concepts/data-protection.md`](../explanation/concepts/data-protection.md)
- [`./data-subject-erasure.md`](./data-subject-erasure.md)
- [`./data-subject-restriction.md`](./data-subject-restriction.md)
- [`./data-subject-portability.md`](./data-subject-portability.md)
