# TabFlow

Multi-tenant cafe and restaurant operations platform built with .NET 10,
Blazor Web App, and PostgreSQL 17.

## Repository Layout

```
src/
  apps/
    platform/          Control-plane host (ASP.NET Core Blazor)
    platform-worker/   Background worker for provisioning jobs
    tenant/            Per-tenant runtime host (ASP.NET Core Blazor)
  packages/
    shared-dotnet/     Shared domain and application services
    firmware/          ESP32 firmware source and templates
  infra/
    postgres/          EF Core migrations project (platform + tenant)
tests/                 Unit, integration, and end-to-end tests
deploy/                Deployment scripts, nginx, systemd configs
tools/                 CLI tools for firmware generation and migrations
doc/                   All documentation, split into four trees (see below)
```

## Documentation Trees

All documentation lives under `doc/`, split by audience and lifecycle.
The [documentation charter](./doc/docs/meta/documentation-charter.md)
defines the shape and rules of each tree.

| Tree | Audience | Lifecycle |
| --- | --- | --- |
| [`doc/docs/`](./doc/docs/) | Engineers, operators | Durable; the engineering reference |
| [`doc/userdocs/`](./doc/userdocs/) | End-users (owner, manager, cashier, station, customer) | Versioned with product release |
| [`doc/apidocs/`](./doc/apidocs/) | External developers | Versioned with public API contract |
| [`doc/buildlog/`](./doc/buildlog/) | The future team | Append-only; never edited or deleted |

## Start Here

- New contributor: [`doc/docs/tutorials/getting-started.md`](./doc/docs/tutorials/getting-started.md)
- Project process and culture: [`doc/docs/constitution.md`](./doc/docs/constitution.md)
- Documentation charter: [`doc/docs/meta/documentation-charter.md`](./doc/docs/meta/documentation-charter.md)
- Pull-request review policy: [`doc/docs/meta/review-policy.md`](./doc/docs/meta/review-policy.md)
- Threat model: [`doc/docs/explanation/concepts/threat-model.md`](./doc/docs/explanation/concepts/threat-model.md)
- Tech debt ledger: [`doc/buildlog/tech-debt-ledger.md`](./doc/buildlog/tech-debt-ledger.md)
- Architecture decisions: [`doc/docs/reference/architecture/decisions.md`](./doc/docs/reference/architecture/decisions.md)
- Local development: [`doc/docs/tutorials/local-development.md`](./doc/docs/tutorials/local-development.md)
- Deploy or operate: [`doc/docs/how-to/`](./doc/docs/how-to/)

## Project Files

- [`./LICENSE`](./LICENSE) — Apache License 2.0 (per AD-0012)
- [`./NOTICE`](./NOTICE) — Apache 2.0 attribution
- [`./CHANGELOG.md`](./CHANGELOG.md) — release history (per AD-0011)
- [`./CODE_OF_CONDUCT.md`](./CODE_OF_CONDUCT.md) — Contributor Covenant 2.1
- [`./SECURITY.md`](./SECURITY.md) — vulnerability disclosure policy
- [`./.editorconfig`](./.editorconfig) — coding standards (per AD-0014)
