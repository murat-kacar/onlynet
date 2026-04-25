# API Reference

This folder documents the HTTP surfaces of TabFlow.

Most server logic in TabFlow runs through Blazor components calling
dependency-injected application services. That surface is not an HTTP
API and is documented under
[`../architecture/runtime-surfaces.md`](../architecture/runtime-surfaces.md)
and the surrounding architecture references rather than here.

## HTTP Surfaces

| Surface | Audience | Reference |
| --- | --- | --- |
| Tenant public endpoints (`/api/public/**`, `/health/**`, `/ws/**`) | Customers, ESP32 devices | [`./tenant-api.md`](./tenant-api.md) |
| Internal admin endpoints (`/api/tenants`, `/api/jobs`, `/api/cart`, `/api/sessions`, `/api/orders`) | Platform admin UI, tenant staff UI | [`./internal-api.md`](./internal-api.md) |
| Error code vocabulary | All HTTP surfaces | [`./error-codes.md`](./error-codes.md) |
| Health probe responses | Supervisor, release gate | [`../architecture/health-checks.md`](../architecture/health-checks.md) |

If a platform-level external API is ever introduced for deliberate
third-party integration, its reference document and OpenAPI artefact
land in [`/doc/apidocs/`](/doc/apidocs/), not here.

## Governance

- External contracts stay unversioned on the current major. The current
  major is treated as `v1`.
- Additive, non-breaking changes are allowed within the current major.
- Breaking changes introduce a new major surface in parallel, for
  example `/api/v2/public/**`, and the old major stays online through a
  deprecation window.

## OpenAPI Artefacts

OpenAPI documents for **public** endpoints (the external API surface)
live in [`/doc/apidocs/`](/doc/apidocs/) per the
[documentation charter](../../meta/documentation-charter.md#apidocs--external-integration-reference).
This folder holds reference text only; generated YAML belongs in the
external-developer tree.

## Deprecation Rule

Any endpoint marked deprecated must carry:

- deprecation date
- replacement contract reference
- sunset date

Deprecations are noted in the document that owns the endpoint and in
the repository-root `CHANGELOG.md` at the time they land.
