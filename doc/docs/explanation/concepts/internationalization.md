# Internationalization

This document explains how TabFlow handles language and locale across
its surfaces. It is the single source of truth for the **English-first**
principle, the supported languages, the `LanguageCode` field on tenant
registration, how operator settings persist, and how staff and customer
surfaces choose a language at runtime.

## English-First Principle

TabFlow's internal contracts are **English only**:

- Source code identifiers (types, methods, fields, parameters,
  variables) — English.
- Database schema (table names, column names, enum members) —
  English.
- HTTP API surfaces (route segments, request and response field names,
  error `code` strings) — English.
- ADRs, acceptance criteria, runtime surfaces, glossary, all
  documentation in `/doc/` — English.
- Log message templates and structured-log property names — English.
- Audit log `event_key` values — English.
- Commit messages, PR titles, code-review comments — English.

This is enforced because mixed-language identifiers fragment search,
defeat tooling that expects English casing, and make onboarding
non-Turkish contributors impossible. AD-0015 records the decision.

Translation happens at the **presentation layer only**: a
customer-facing or staff-facing surface renders an English neutral key
into the active language at request time, using `IStringLocalizer<T>`
and `*.resx` files. Nothing else in the system is translated.

## Supported Languages

TabFlow ships with first-class support for two languages at the
presentation layer:

| Language | Code | Status |
| --- | --- | --- |
| English | `en-GB` | **Default.** First class. The neutral language for every `*.resx` resource and the only language for internal contracts. |
| Turkish | `tr-TR` | First class. Primary market language. Complete translation set required to ship. |

Additional languages are added per-tenant on demand. A new language
ships only when:

1. A complete translation set exists for every customer-facing string.
2. A complete translation set exists for every staff-facing string a
   tenant who selects this language can encounter.
3. Locale-specific formatting (currency, date, number) is verified.
4. The language is added to the table in this document via an amending
   PR.

A language is **not** added because one screen is partially translated.
Partial coverage is a worse experience than English fallback.

## Where Language Lives

### Tenant Default Language

Each tenant carries a `LanguageCode` on its registration:

- Stored on `TenantRegistration.LanguageCode`
  (`/src/packages/shared-dotnet/Domain/Entities/Platform/TenantRegistration.cs`).
- Set at provisioning time from the `tenant.create` job payload; if
  omitted, defaults to `en-GB`.
- Validated against the supported language list above; an unsupported
  code rejects the job with `tenant.create.unsupported_language`.

This is the **default** for the tenant. It determines:

- the rendering language of the staff console when a staff user has no
  personal preference set;
- the rendering language of the customer ordering surface when no
  `Accept-Language` hint is present;
- the rendering language of staff-facing notifications.

It does **not** affect anything stored in the database other than the
field itself. Audit log entries, system events, and tenant-authored
data remain language-neutral (see "How Strings Are Stored" below).

### Per-User Language Override

Authenticated operators MAY override the surface default in their
profile. This applies to:

- platform operators (`owner`, `admin`, `viewer`) on the platform host;
- tenant staff (`owner`, `manager`, `cashier`) on tenant hosts.

The override is stored in the database and respected on every request.
The source of truth is:

- platform host: `platform_user_preferences`
- tenant host: tenant-local user preference storage attached to the
  Identity user record or its host-local extension

Browser storage (`localStorage`, IndexedDB) and non-auth cookies are
**not** preference stores in TabFlow. They may cache ephemeral UI state
inside one live page session, but any setting that survives navigation,
sign-out, or device change MUST live in the database.

Customers do not have an account; their language is chosen by, in
priority order:

1. an explicit selector on the customer ordering page (persists in
   `customer_sessions.language_code` for the life of the session);
2. the `Accept-Language` HTTP header on the first request, mapped to
   the closest supported language;
3. the tenant default.

### Per-Surface Constraint

Some surfaces have their own constraint:

| Surface | Language Source |
| --- | --- |
| Platform admin console | Platform operator preference → default `en-GB`. |
| Tenant staff console | Tenant default, overridable per user. |
| Customer ordering | Customer choice → header → tenant default. |
| Station board | Tenant default. Stations are usually fixed to one device with one operator. |
| Audit logs | Strings stored in English. Display layer translates known event keys. |
| API error responses | English `code` + `messageKey`; client maps `messageKey` to a localized string. |
| OpenAPI / API reference | English only. |
| Documentation | English only (this tree). |

The English-only choices are deliberate for machine-readable contracts.
Operator-facing surfaces may localize at render time, but their storage
and protocol contracts remain English-first.

## Operator Settings Persistence

Operator settings are account-backed, not browser-backed.

Required rule set:

- language, time zone, and UI density are persisted in the database;
- the authentication cookie proves identity only;
- preference resolution happens server-side on each request;
- sign-in on a second browser MUST reproduce the same settings;
- clearing browser storage MUST NOT reset operator preferences.

For the platform host, the baseline record shape is:

- `UserId`
- `LanguageCode`
- `TimeZone`
- `Density`
- `CreatedAt`
- `UpdatedAt`

## How Strings Are Stored

Three categories, each with its own storage:

### 1. UI Strings (.NET resources)

Customer-facing and staff-facing UI strings live in standard .NET
resource files (`*.resx` and `*.{lang}.resx`) per project. Localization
is wired through `IStringLocalizer<T>` and Blazor's
`<Resource>`-aware components.

Convention:

- Resource files live next to the consuming component or service.
- **The neutral resource is always English** (`Foo.resx` contains the
  English text). Per-language files follow (`Foo.tr.resx`).
- Resource keys are stable English identifiers in `PascalCase.WithDots`
  form (e.g. `Order.SubmitButton`, `Cart.EmptyMessage`). A new key
  must be expressible in clean English first; if you cannot name it in
  English without ambiguity, the concept is not yet ready to ship.
- A missing key in a non-English resource falls through to the English
  neutral; CI fails the build if the English neutral is missing.

### 2. Domain-Generated Strings

Strings produced by domain code (audit messages, system notifications)
are stored as **language-neutral event keys** plus structured payloads:

```text
event_key:    order.submitted
payload_json: { "orderId": 42, "tableNumber": 7, "total": "120.00" }
```

The display layer renders this with a translation table. The database
never stores a translated sentence; that would couple the data layer
to a language choice.

### 3. Tenant-Authored Content

Menu items, station labels, and similar tenant-authored fields are
stored once per tenant in the tenant default language. A future
extension may allow per-language variants of menu items; that is a
deliberate non-goal of the current major.

## Locale Formatting

| Aspect | Source |
| --- | --- |
| Currency | Tenant `RegionalSettings.CurrencyCode` (ISO 4217). |
| Decimal separator | Derived from tenant language code via .NET `CultureInfo`. |
| Date and time display | Derived from language code; storage is always UTC `timestamptz`. |
| Time zone | Tenant `RegionalSettings.TimeZoneId` (IANA). |
| Phone number format | Tenant `RegionalSettings.CountryCode` (ISO 3166). |

Storage is always normalised: UTC for time, ISO codes for currency and
country. Display is culture-aware at render time. A change to a tenant's
regional settings or an operator's personal settings affects display
only; stored business data is unaffected.

## Adding A Language

1. Open a PR adding the language to the table at the top of this
   document.
2. Translate every key in every `*.resx` file in the codebase. CI
   compares against English to verify no missing key.
3. Translate every event-key entry in the translation table for
   audit and notification rendering.
4. Verify locale formatting end-to-end with a smoke pass on a fresh
   tenant.
5. Add an acceptance criterion that the new language renders the
   tutorials walkthrough end-to-end.

A partial translation is **not** merged. Hold the PR open with a
checklist; merge when the checklist is complete.

## Out Of Scope For The Current Major

- Right-to-left language support. Adding RTL requires component-level
  layout review.
- Per-language menu item variants.
- Per-language receipt templates beyond the supported set.
- Plural-form expressions beyond what `IStringLocalizer` supports.

These return as separate ADRs when a tenant requires them.

## Related

- [`../../reference/architecture/decisions.md`](../../reference/architecture/decisions.md)
  AD-0015 — English-first internal contracts
- [`../../reference/architecture/decisions.md`](../../reference/architecture/decisions.md)
  AD-0007 — PostgreSQL collation defaults
- [`../../reference/database/schema.md`](../../reference/database/schema.md)
  — `TenantRegistration.LanguageCode`, `platform_user_preferences`,
  `customer_sessions.language_code`
- [`../../reference/api/error-codes.md`](../../reference/api/error-codes.md)
  — `messageKey` field convention
- [`./data-protection.md`](./data-protection.md) — language preference
  is not personal data when not tied to an identified user
