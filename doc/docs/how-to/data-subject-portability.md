# Respond to a Data-Subject Portability Request

This guide is the operator procedure for the **Right to data
portability** that
[`../explanation/concepts/data-protection.md`](../explanation/concepts/data-protection.md#data-subject-rights)
commits TabFlow to under **GDPR Article 20**. KVKK has no direct
equivalent of Article 20; TabFlow honours portability under the
superset rule (the strictest of the regulations the deployment is
subject to applies, irrespective of jurisdiction).

The output is a structured, machine-readable JSON export in the same
shape as the access procedure
([`./data-subject-access.md`](./data-subject-access.md#output)),
delivered to the requester in a format suitable for transmission to a
different data controller.

## When This Applies

A portability request applies when:

- The data subject submits a request that maps to GDPR Article 20:
  "I would like to receive my data in a portable format" or "I would
  like you to transmit my data to another controller", or any phrase
  with the same intent.
- The processing is based on **consent** (Article 6(1)(a)) or on a
  **contract** (Article 6(1)(b)). For TabFlow that is every
  customer-session record (consent) and every order record
  (contract). Audit-log entries written under legitimate-interest
  processing are **not** in scope and are excluded.
- Identity verification has succeeded as for the access procedure.

A portability request **may** be combined with a transmission
request ("send my data to controller X"). When combined, the
procedure runs as below and the output is delivered to the third
party named by the data subject through a verified secure channel.

## Required Information

Same as the access procedure
([`./data-subject-access.md`](./data-subject-access.md#required-information))
plus:

- **Output format preference.** Portability defaults to JSON;
  data subjects who prefer CSV receive the same content with one
  CSV file per source table inside a ZIP archive.
- **Recipient (optional).** Either the requester themselves or a
  named third-party controller. When a third-party controller is
  named, the operator obtains and verifies a delivery channel
  (typically a public GPG key or a controller-published delivery
  endpoint).

## Procedure

1. **Open a secure DSR case record** in the operator's access-controlled
   case system. Record the request opening, subject identifier, format
   preference, and recipient. Repository documentation may reference only
   the redacted request id (`dsr-NNNN`).

2. **Run the access export.** Follow steps 2–3 of
   [`./data-subject-access.md`](./data-subject-access.md#procedure).
   The portability export starts from the same base.

3. **Filter to portability-eligible rows.** Remove every entry whose
   processing is **not** based on consent or contract:

   - Audit-log rows are removed (legitimate-interest processing).
   - Restriction-related rows are removed (Article 18 processing).
   - Erasure-related rows are removed (Article 17 processing).
   - The `omitted` array is rebuilt to name the (table, column)
     tuples removed under this filter, with the legal basis
     `Article 20 scope: not consent or contract`.

4. **Format.** The default JSON shape is identical to the access
   export. For CSV preference, write one CSV per top-level table
   key in the JSON, naming the file
   `<top-level-key>__<table>.csv`. The CSV files are bundled into
   `dsr-portability-<subject-id>-<yyyymmdd>.zip` along with a
   `manifest.json` that lists the files and the SHA-256 digest of
   each.

5. **Sign and seal.** SHA-256 the deliverable (the JSON file or the
   ZIP). Record the digest in the secure DSR case record from step 1.

6. **Deliver.**
   - **To the requester.** Same channel as the access procedure
     (out-of-band password for a password-protected ZIP, age-encrypted
     archive when the requester has a GPG key).
   - **To a named third-party controller.** Verify the recipient's
     identity via their published delivery endpoint or GPG key.
     Encrypt the deliverable to that key. Send.

7. **Audit.** Write a `tenant_audit_log` row (or `platform_audit_log`
   for platform-admin subjects):

   ```sql
   INSERT INTO tenant_audit_log (
     id, occurred_at, action, actor_user_id, resource_id, payload
   ) VALUES (
     gen_random_uuid(),
     now(),
     'dsr.portability.delivered',
     '<operator-user-id>',
     '<subject-id>',
     jsonb_build_object(
       'request_id',  'dsr-NNNN',
       'format',      'json|csv-zip',
       'recipient',   'subject|controller:<name>',
       'sha256',      '<sha-256-from-step-5>',
       'channel',     '<gpg|password-zip|controller-endpoint>'
     )
   );
   ```

## Output

A single JSON file named
`dsr-portability-<subject-id>-<yyyymmdd>.json` (default) or a ZIP
archive `dsr-portability-<subject-id>-<yyyymmdd>.zip` (CSV
preference). The shape is the access export filtered per step 3.

## Audit Trail

A single `dsr.portability.delivered` row per delivery. Audit rows
written to satisfy this procedure itself are **not** included in any
future portability export of the same subject (legitimate-interest
processing).

## Regulatory Timing

Same 30-day clock as the access procedure under
GDPR Article 12(3). KVKK does not set a portability-specific
deadline but the access deadline applies by the superset rule (30
days for KVKK 13, 30 days for GDPR 12(3); they coincide).

## Related

- [`./data-subject-access.md`](./data-subject-access.md)
- [`./data-subject-erasure.md`](./data-subject-erasure.md)
- [`./data-subject-restriction.md`](./data-subject-restriction.md)
- [`../explanation/concepts/data-protection.md`](../explanation/concepts/data-protection.md)
