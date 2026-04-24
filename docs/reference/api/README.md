# API Reference

This folder documents the HTTP surfaces that are external to each host.

Most server logic in TabFlow runs through Blazor components calling
dependency-injected application services. That surface is not an HTTP API
and is documented under
[`../architecture/runtime-surfaces.md`](../architecture/runtime-surfaces.md)
and the surrounding architecture references rather than here.

## Remaining External HTTP Surfaces

Only two HTTP contracts stay externally visible:

- Tenant host public endpoints and the ESP32 device WebSocket. Documented
  in [`./tenant-api.md`](./tenant-api.md).
- Platform host health probes. Not documented as a separate reference
  because the surface is only `/health`, `/health/live`, and
  `/health/ready`, all covered in
  [`../architecture/system-overview.md`](../architecture/system-overview.md).

A dedicated `platform-api.md` would duplicate those two lines and encourage
growth of an HTTP surface that no external caller needs. If a platform
external API is ever introduced for deliberate third-party integration, a
dedicated reference document returns at that point.

## Governance

- External contracts stay unversioned on the current major. The current
  major is treated as `v1`.
- Additive, non-breaking changes are allowed within the current major.
- Breaking changes introduce a new major surface in parallel, for example
  `/api/v2/public/**`, and the old major stays online through a
  deprecation window.

## OpenAPI

OpenAPI documents are planned as committed artifacts under
[`../openapi/`](../openapi/README.md) once the tenant public surface
stabilizes on Blazor:

- [`../openapi/tenant-public-v1.yaml`](../openapi/README.md)

The document will be generated from ASP.NET Core endpoint metadata and
committed with a diff check in CI so contract drift is visible in review.

## Deprecation Rule

Any endpoint marked deprecated must carry:

- deprecation date
- replacement contract reference
- sunset date

Deprecations are noted in the document that owns the endpoint and in
the repository-root `CHANGELOG.md` at the time they land.
