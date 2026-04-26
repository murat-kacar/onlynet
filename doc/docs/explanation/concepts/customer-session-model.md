# Customer Session Model

This document explains the customer access and sessionization model for
QR-driven tenant menu access.

The goal is to keep customer browsing lightweight while concentrating
security controls at the point where an order is actually submitted.

## Product Goal

TabFlow should allow customers to:

- scan a table QR
- join the live table experience quickly
- browse the menu and open check comfortably
- submit orders only with fresh proof of physical presence at the table

TabFlow should prevent:

- replaying expired QR codes
- using screenshots of old QR codes
- submitting orders remotely without access to the current table display

## Core Model

There is one canonical live table session.

This session:

- starts when a customer scans a fresh QR for a table that has no open
  session
- remains open while the table check is open
- ends when the check is closed by the store

Every participating browser receives its own access ticket, but all access
tickets attach to the same live table session.

This means:

- one table session per active table check
- multiple browser participants may join the same table session
- each browser has its own signed access cookie
- all participants see the same live table context
- orders can still be attributed to the specific access ticket that
  created them

## Official Runtime Concepts

### QR Token

The QR token is a short-lived join proof.

Rules:

- only one fresh QR is considered active for a table at a time
- QR TTL is short
- every QR is single-use
- as soon as it is scanned successfully, it is consumed
- immediately after successful scan, the table display rotates to a new QR

QR tokens are only for joining the live table session. They are not the
long-lived customer session.

### Table Session

The table session is the canonical live customer-facing session for a
table.

Rules:

- created by the first successful fresh QR join
- remains active while the table check stays open
- closed by the store-side operational flow when the check is closed
- all customer access becomes invalid when the table session closes

The table session is the backend source of truth.

### Access Ticket

An access ticket represents one browser participant attached to the live
table session.

Rules:

- issued after a successful QR join
- carried in a first-party `HttpOnly` cookie
- browser-scoped, not a guaranteed physical-device identity
- survives refresh as long as the cookie survives
- becomes invalid as soon as the parent table session closes

Important:

- access tickets are not real device IDs
- private tabs, cookie clearing, or different browsers may create new
  tickets
- this is acceptable
- security decisions must not rely on same-physical-person guarantees

### Server-Side Cart

The customer's cart lives on the server, bound to the access ticket and
the live table session. The browser only carries the access cookie; cart
contents never live in `localStorage` or in the cookie itself.

Rules:

- cart rows are stored in `customer_session_cart_items`
- add, update, and remove run as normal HTTP form submissions over the
  Static SSR customer menu surface
- refresh or reconnect restores the cart without client-side state
- when the table session closes, the cart rows close with it

### Checkout Proof

The critical attack surface is order submission, not menu reading.

Therefore, every order submission must require a fresh QR proof.

Rules:

- menu and check can remain visible while an access ticket is valid
- `Submit order` must always require a fresh QR challenge
- the fresh QR used for checkout must be single-use
- after a successful order submit, the checkout proof is consumed
- a later order submit requires a new fresh QR challenge again

## Customer Flow

### Join Flow

1. Customer opens `/g/{token}` by scanning the current table QR.
2. Backend validates that the QR token is fresh and unused.
3. Backend consumes the QR token.
4. Backend opens the table session if one does not already exist.
5. Backend creates a new access ticket for this browser.
6. Tenant host stores the access ticket in a first-party `HttpOnly`
   cookie.
7. Customer is redirected to `/menu`.
8. Table display rotates to a new QR immediately.

### Read Flow

While the table session remains open and the access ticket is valid, the
customer may:

- browse the menu
- review the open check
- inspect previously submitted order lines attributed to the current
  ticket
- inspect the full table order and check state as allowed by the product
  surface

Menu and check reading should not require repeated QR scans.

### Submit Flow

1. Customer prepares the cart normally on `/menu`.
2. Customer presses `Submit order`.
3. UI requests a fresh QR scan from the current table display.
4. Backend validates and consumes that fresh QR proof.
5. Backend verifies that the device-binding cookie on the request
   matches the access ticket's `DeviceCookieValue` (per
   [TD-0017](/doc/buildlog/tech-debt-ledger.md#td-0017); a different
   browser on the same access cookie cannot submit an order).
6. Backend verifies that the access ticket belongs to the still-open
   table session.
7. Backend checks the request's `idempotencyKey` body field against the
   `(SessionId, IdempotencyKey)` unique index on `orders` (per
   [TD-0018](/doc/buildlog/tech-debt-ledger.md#td-0018)); a replay
   returns the prior result.
8. Backend converts the server-side cart into the order and attributes it
   to the table session and the access ticket.
9. The fresh checkout QR proof is consumed and cannot be reused.

Every later order submission must request another fresh QR proof.

### End Flow

When the table check is closed by the store:

- the table session is closed
- all linked access tickets become invalid
- customer cookies may remain physically present in the browser, but
  backend validation must reject them
- the next menu or order interaction must require joining again with a
  fresh QR

## Identity Model

TabFlow distinguishes between:

- operational session identity
- persistent pseudonymous customer identity

### Operational Identity

Operational identity is strictly necessary for the table flow:

- QR token
- table session
- access ticket
- server-side cart
- checkout proof

This layer exists to make the product function and to defend order
submission.

### Persistent Customer Identity

Separately, TabFlow may maintain a longer-lived first-party customer
identity for analytics, learning, and future recommendation hooks.

This identity is:

- pseudonymous
- not guaranteed to represent the same physical person forever
- not used as the security control for order submission

Suggested building blocks:

- first-party persistent cookie
- server-side alias records
- coarse helper signals such as browser family, locale, and time zone

Important:

- this is not a hard device fingerprint
- this is not a substitute for fresh QR proof
- this identity layer must follow consent boundaries

## Consent Boundary

TabFlow collects the maximum defensible signal set, not the maximum
possible signal set.

### Strictly Necessary Layer

May be used to operate the table flow and defend abuse:

- QR token events
- table session state
- access tickets
- server-side cart
- checkout proof validation
- order submission metadata
- coarse abuse and security telemetry

### Consent-Gated Layer

Enabled only according to the selected consent model:

- persistent customer identity
- long-lived behavior stitching across visits
- recommendation features
- advanced analytics or profiling signals

The consent banner must present at least:

- `Reject`
- `Minimum`
- `Accept all`

`Reject` still allows strictly necessary operational cookies.

## Related

- [`../../reference/architecture/runtime-surfaces.md`](../../reference/architecture/runtime-surfaces.md)
- [`../../reference/api/tenant-api.md`](../../reference/api/tenant-api.md)
- [`./authorization.md`](./authorization.md)
