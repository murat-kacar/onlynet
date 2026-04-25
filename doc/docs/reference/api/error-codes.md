# API Error Codes

This document is the enumerated vocabulary of error codes returned by
the HTTP surfaces described in
[`./tenant-api.md`](./tenant-api.md). The error envelope uses
[RFC 7807 Problem Details][rfc7807] with TabFlow's `code` and `traceId`
extensions; see the **Error Model** section of that document.

[rfc7807]: https://www.rfc-editor.org/rfc/rfc7807

## Code Naming

- Codes are `snake_case`, stable once published, and scoped to their
  problem domain with a short prefix where helpful.
- Codes describe the **condition**, not the HTTP status, so that the
  same condition may travel across transports (HTTP, WebSocket close
  reasons, in-process result types).
- Clients key off the `code` field. The `title` and `detail` fields
  are human-readable and MAY change without notice.
- A new code MUST be added through a documentation-first pull request
  (see [`../../meta/contributing.md`](../../meta/contributing.md)).

## Common

| Code | Typical Status | Meaning |
| --- | --- | --- |
| `invalid_request` | `400` | Request schema failed validation. |
| `unauthenticated` | `401` | No valid session or access ticket. |
| `forbidden` | `403` | Authenticated caller is out of scope for the resource. |
| `not_found` | `404` | Resource does not exist in the tenant scope. |
| `conflict` | `409` | Generic state conflict not covered by a more specific code. |
| `gone` | `410` | Resource existed and is now retired. |
| `rate_limited` | `429` | Caller exceeded the rate budget for the endpoint. |
| `internal_error` | `500` | Unexpected server failure. |
| `service_unavailable` | `503` | Dependency not ready (database, event bus). |

## Session And Access Ticket

| Code | Typical Status | Meaning |
| --- | --- | --- |
| `invalid_token` | `401` | QR token did not validate. |
| `token_used` | `409` | QR token has already opened another session. |
| `token_expired` | `410` | QR token lifetime elapsed before use. |
| `session_expired` | `401` | Customer session lifetime elapsed. |
| `session_closed` | `401` | Customer session was closed server-side (bill closed, device moved, operator override). |

## Order Submission

| Code | Typical Status | Meaning |
| --- | --- | --- |
| `cart_empty` | `400` | Submission attempted with no items. |
| `catalog_stale` | `409` | Cart references a catalog version no longer served. |
| `checkout_proof_missing` | `400` | Request omitted the checkout proof field. |
| `checkout_proof_invalid` | `401` | Checkout proof did not validate against the table session. |
| `checkout_proof_expired` | `410` | Checkout proof lifetime elapsed before submit. |
| `order_duplicate` | `409` | Idempotency key already consumed by a prior submission. |

## Device WebSocket

For the `/ws/tables/{tableNumber}` handshake, failures surface as a
WebSocket close frame. The numeric close code follows
[RFC 6455][rfc6455] conventions; the textual reason carries the code
vocabulary for parity with HTTP responses:

- `4401 device_unauthenticated` — handshake query carried no or invalid `deviceKey`.
- `4403 device_forbidden` — device key does not match the addressed table.
- `4409 device_already_connected` — another connection holds the slot.
- `4429 device_rate_limited` — caller reconnected too frequently.

[rfc6455]: https://www.rfc-editor.org/rfc/rfc6455

## Governance

- Adding a code is a non-breaking change.
- Renaming a code is a breaking change and moves the surface forward on
  the API versioning policy in [`./README.md`](./README.md).
- Retiring a code is a breaking change and follows the same policy.
- Changing the `status` that a code typically maps to is a breaking
  change unless the previous status is retained as an accepted fallback
  for at least one prior major.
