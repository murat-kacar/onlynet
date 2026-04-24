# OpenAPI

This folder holds committed OpenAPI documents for the external HTTP
surfaces of TabFlow.

Planned artifacts:

- `tenant-public-v1.yaml` — tenant host public endpoints
  (`/api/public/**`, `/health*`) as described in
  [`../api/tenant-api.md`](../api/tenant-api.md).

The documents are generated from ASP.NET Core endpoint metadata and
committed so contract drift is visible in review. A CI check re-runs the
generator against the source and fails the build if the committed document
does not match.

The platform host does not maintain a public OpenAPI document because its
external HTTP surface is only `/health*`. If a deliberate third-party
platform API is introduced later, a dedicated `platform-public-v1.yaml`
document is added here at that point.
