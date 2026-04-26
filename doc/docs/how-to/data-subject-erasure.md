# Respond to a Data-Subject Erasure Request

This guide is the operator procedure for the **Right to erasure** (also
known as the right to be forgotten) that
[`../explanation/concepts/data-protection.md`](../explanation/concepts/data-protection.md#data-subject-rights)
commits TabFlow to under
**KVKK Articles 7 and 11(1)(e–f)** and **GDPR Article 17**. The output
is a hard-delete of every record that names the data subject **except**
entries needed to satisfy a separate legal obligation, which are
anonymised in place.

## When This Applies

An erasure request applies when:

- A data subject submits a request that asks for "the data you hold
  about me" to be deleted, the equivalent in Turkish, or any phrase
  that maps to KVKK 7 / GDPR 17.
- No competing legal obligation requires retention. The most common
  competing obligation under Turkish law is the
  **6-year accounting record retention** (Türk Ticaret Kanunu / Vergi
  Usul Kanunu) for sales transactions; in the GDPR space, a
  competing obligation often arises from member-state tax law.
- Identity verification has succeeded as for the access procedure
  ([`./data-subject-access.md`](./data-subject-access.md#required-information)).

If a competing obligation **does** apply, the procedure replaces hard
delete with anonymisation in place; see step 5.

## Required Information

Same as the access procedure
([`./data-subject-access.md`](./data-subject-access.md#required-information))
plus:

- **Reason for erasure.** Map to the matching KVKK 7(1) clause
  (a–c) or GDPR 17(1) clause (a–f). The reason determines whether
  step 5 (competing-obligation review) is mandatory.
- **Cut-off timestamp.** Records created **before** the cut-off are
  in scope; records after it (e.g. an audit row written by this
  procedure itself) are not.

## Procedure

The procedure runs against the live primary in a single transaction
per database. The transaction is the rollback boundary if the
verification step (step 6) fails.

1. **Open a secure DSR case record** in the operator's access-controlled
   case system (same convention as the access procedure). Record the
   request opening, subject identifier, tenant scope, reason for erasure,
   and cut-off timestamp. Repository documentation may reference only
   the redacted request id (`dsr-NNNN`).

2. **Run a dry-run access export** per the access how-to. The export
   is the rollback artefact: if step 6 fails or a competing
   obligation later surfaces, the export is the only remaining
   evidence the data ever existed.

3. **Identify competing-obligation rows.** Inside the tenant
   database, separate rows that the 6-year accounting retention
   keeps from rows that erase outright:

   ```sql
   -- Orders submitted by the subject's customer session — kept,
   -- anonymised in place under step 5
   SELECT id FROM orders
   WHERE session_id IN (
     SELECT session_id FROM customer_access_tickets
     WHERE id = '<subject-id>'
   );

   -- Order items, payments, and the accounting projection rows
   -- that link back to those orders are kept the same way.
   ```

   Cart rows, customer-session rows, customer-access-ticket rows,
   and audit rows are in scope for hard delete.

4. **Hard-delete in scope.** Inside an explicit transaction:

   ```sql
   BEGIN;
   DELETE FROM customer_session_cart_items
   WHERE session_id IN (
     SELECT session_id FROM customer_access_tickets
     WHERE id = '<subject-id>'
   );
   DELETE FROM customer_access_tickets WHERE id = '<subject-id>';
   DELETE FROM customer_sessions       WHERE id = '<session-id>';
   ```

   For staff erasures, also delete the Identity rows:

   ```sql
   DELETE FROM "AspNetUserRoles"  WHERE "UserId" = '<subject-id>';
   DELETE FROM "AspNetUserClaims" WHERE "UserId" = '<subject-id>';
   DELETE FROM "AspNetUsers"      WHERE "Id"     = '<subject-id>';
   ```

5. **Anonymise competing-obligation rows in place.** For every order
   and order-item row identified in step 3, replace personal-data
   columns with their anonymised counterparts:

   ```sql
   UPDATE orders SET
       customer_display_name = NULL,
       customer_phone        = NULL,
       device_cookie_value   = NULL,
       idempotency_key       = NULL
   WHERE id = '<order-id>';
   ```

   The accounting-relevant columns (totals, line items, timestamps)
   stay; the personal-data columns are nulled. After the update, no
   row in the system can be traced back to the subject by any
   identifier from the original request.

6. **Verify.** Run the access procedure's collection queries again.
   The result must be **empty** for every table except the
   anonymised orders, where every personal-data column must be
   `NULL`.

7. **Commit and audit.**

   ```sql
   INSERT INTO tenant_audit_log (
     id, occurred_at, action, actor_user_id, resource_id, payload
   ) VALUES (
     gen_random_uuid(),
     now(),
     'dsr.erasure.completed',
     '<operator-user-id>',
     '<subject-id>',
     jsonb_build_object(
       'request_id', 'dsr-NNNN',
       'reason',     '<KVKK 7(1)(a) | GDPR 17(1)(b) | ...>',
       'export_sha256', '<sha-256-from-step-2>',
       'rows_deleted',     <count>,
       'rows_anonymised',  <count>
     )
   );
   COMMIT;
   ```

8. **Notify the requester.** Send a confirmation that names the
   reason clause from step 1 and the count of rows deleted and
   anonymised. Reference the request id; do **not** send the export
   from step 2 (it is the rollback artefact, not the deliverable).

## Output

No separate deliverable beyond the confirmation message in step 8.
The audit row from step 7 is the lasting evidence the procedure ran.
The export from step 2 is retained on operator-controlled storage
under the secure DSR case record from step 1 for the regulatory dispute window
(typically 5 years for KVKK; 6 years for GDPR-implementing
member-state law).

## Audit Trail

A single row per erasure run, written to:

- `tenant_audit_log` for tenant-scope erasures.
- `platform_audit_log` for platform-admin erasures.

The action is `dsr.erasure.completed`. The payload carries the
request id, the reason clause, the SHA-256 of the export from step
2, and the row counts.

## Regulatory Timing

Same 30-day clock as the access procedure. The dry-run export and
verification steps add a few hours; the regulator counts that as
part of the response, not as an extension.

## Related

- [`./data-subject-access.md`](./data-subject-access.md)
- [`./data-subject-restriction.md`](./data-subject-restriction.md)
- [`../explanation/concepts/data-protection.md`](../explanation/concepts/data-protection.md)
