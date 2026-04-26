# Respond to a Data-Subject Restriction Request

This guide is the operator procedure for the **Right to restriction of
processing** that
[`../explanation/concepts/data-protection.md`](../explanation/concepts/data-protection.md#data-subject-rights)
commits TabFlow to under
**KVKK Articles 7(2) and 11(1)(f)** and **GDPR Article 18**. The
outcome is a `restricted = true` flag on the data subject's record;
processing pauses until the flag is cleared.

Restriction is **not** erasure: the data stays, the audit trail stays,
but no operational query, audit-derived metric, or admin action may
read or modify the restricted row except to lift the restriction or
to satisfy a separate legal obligation.

## When This Applies

A restriction request applies when one of the four GDPR Article 18(1)
grounds (or its KVKK Article 7(2) equivalent) is met:

- The accuracy of the personal data is contested; restriction holds
  while the operator verifies the contested claim.
- The processing is unlawful but the data subject opposes erasure and
  asks for restriction instead.
- The operator no longer needs the data but the data subject needs
  it to establish, exercise, or defend a legal claim.
- The data subject has objected to processing under Article 21 and
  the operator has not yet resolved the objection.

A restriction is **time-bound**: every restriction has an expected
review date by which the operator either lifts the restriction or
records why the restriction is extended.

## Required Information

Same as the access procedure
([`./data-subject-access.md`](./data-subject-access.md#required-information))
plus:

- **Restriction ground.** One of the four Article 18(1) clauses (or
  KVKK 7(2) clause).
- **Review date.** A concrete ISO date by which the restriction is
  reviewed. Typical values are 30, 60, or 90 days from the
  restriction date.

## Procedure

1. **Open a postmortem-style record** at `/doc/buildlog/dsr-NNNN.md`.
   Record the request opening, subject identifier, restriction
   ground, and review date.

2. **Set the restriction flag.** TabFlow's tenant schema does not
   yet ship a dedicated `restricted` column; until
   [TD-0007](/doc/buildlog/tech-debt-ledger.md) lands the
   `[DataClass]` schema rule that makes restriction a first-class
   column on every personal-data table, the operator records the
   restriction in the tenant audit log and enforces it via review
   discipline:

   ```sql
   BEGIN;
   INSERT INTO tenant_audit_log (
     id, occurred_at, action, actor_user_id, resource_id, payload
   ) VALUES (
     gen_random_uuid(),
     now(),
     'dsr.restriction.applied',
     '<operator-user-id>',
     '<subject-id>',
     jsonb_build_object(
       'request_id',     'dsr-NNNN',
       'ground',         '<Article 18(1)(a) | (b) | (c) | (d) | KVKK 7(2)>',
       'review_date',    '<yyyy-mm-dd>',
       'scope',          '<staff | customer-session | order-history>',
       'effective_until_lifted', true
     )
   );
   COMMIT;
   ```

3. **Suspend operational paths.** For staff restrictions, deactivate
   the Identity user so further sign-ins are refused:

   ```sql
   UPDATE "AspNetUsers"
   SET "LockoutEnd"     = '9999-12-31T23:59:59Z',
       "LockoutEnabled" = true
   WHERE "Id" = '<subject-id>';
   ```

   For customer-session restrictions, close any active session and
   refuse new tickets:

   ```sql
   UPDATE customer_sessions SET state = 'closed', closed_at = now()
   WHERE id IN (
     SELECT session_id FROM customer_access_tickets
     WHERE id = '<subject-id>'
   );
   UPDATE customer_access_tickets SET is_consumed = true
   WHERE id = '<subject-id>';
   ```

4. **Notify dependent flows.** When the restriction covers an order
   that has not yet been served, the staff console must surface the
   restriction so a kitchen ticket is not pulled. Until the
   admin-console restriction view ships, the operator notifies the
   tenant manager out-of-band (email, channel) and pins the
   notification to the postmortem in step 1.

5. **Confirm to the requester.** Send a confirmation that:
   - acknowledges the restriction,
   - names the ground from step 1,
   - states the review date from step 1,
   - explains that no further processing happens during the
     restriction except (a) storage, (b) consent-based processing,
     (c) processing necessary to establish or defend a legal claim,
     and (d) processing to protect another natural or legal person.

6. **Schedule the review.** Add a calendar reminder for the review
   date. On that date, either:
   - Lift the restriction by inserting an `dsr.restriction.lifted`
     audit row and reversing step 3.
   - Extend the restriction by inserting another
     `dsr.restriction.applied` row with a new review date and the
     extension reason.

## Audit Trail

Two row patterns:

- `tenant_audit_log` (or `platform_audit_log`) row with
  `action = 'dsr.restriction.applied'` per step 2.
- `tenant_audit_log` row with `action = 'dsr.restriction.lifted'`
  on the eventual review.

Both rows carry the request id and the ground.

## Regulatory Timing

Same 30-day clock as the access procedure for the **initial** confirm
(step 5). The review schedule from step 6 is enforced by the operator;
KVKK and GDPR do not set a maximum restriction duration but treat an
indefinite restriction as functionally equivalent to erasure under
Article 17.

## Related

- [`./data-subject-access.md`](./data-subject-access.md)
- [`./data-subject-erasure.md`](./data-subject-erasure.md)
- [`../explanation/concepts/data-protection.md`](../explanation/concepts/data-protection.md)
