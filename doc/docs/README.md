# TabFlow Engineering Documentation

The `docs/` tree is one of five documentation trees in TabFlow. It is the
**engineering reference** for software engineers, SREs, and platform
operators. The full tree map and cross-tree rules live in
[`meta/documentation-charter.md`](./meta/documentation-charter.md).

This tree's folders are grouped by reader intent (Diátaxis):

- `tutorials/` — reading paths for someone new
- `how-to/` — task-oriented operational guides
- `reference/` — contracts, facts, invariants
- `explanation/` — concepts and reasoning
- `meta/` — rules for editing this tree

Documents in `reference/` and `meta/` use **RFC 2119** keywords
(**MUST**, **MUST NOT**, **SHALL**, **SHOULD**, **MAY**) per
[RFC 2119](https://www.rfc-editor.org/rfc/rfc2119) and
[RFC 8174](https://www.rfc-editor.org/rfc/rfc8174). Documents in
`explanation/` and `tutorials/` are informative.

## Start Here

- New to the project: [`tutorials/getting-started.md`](./tutorials/getting-started.md)
- Need to perform a task: [`how-to/`](./how-to/README.md)
- Need exact system facts: [`reference/`](./reference/README.md)
- Need to understand a choice: [`explanation/`](./explanation/README.md)

## Fast Paths

- Project constitution (process and culture): [`constitution.md`](./constitution.md)
- Active architecture decisions: [`reference/architecture/decisions.md`](./reference/architecture/decisions.md)
- System overview: [`reference/architecture/system-overview.md`](./reference/architecture/system-overview.md)
- Runtime surface map: [`reference/architecture/runtime-surfaces.md`](./reference/architecture/runtime-surfaces.md)
- Render-mode strategy: [`reference/architecture/render-modes.md`](./reference/architecture/render-modes.md)
- Capability matrix: [`reference/architecture/capability-matrix.md`](./reference/architecture/capability-matrix.md)
- Authorization model: [`explanation/concepts/authorization.md`](./explanation/concepts/authorization.md)
- Customer QR and session model: [`explanation/concepts/customer-session-model.md`](./explanation/concepts/customer-session-model.md)
- Tenant runtime surfaces (product view): [`explanation/concepts/operational-surfaces.md`](./explanation/concepts/operational-surfaces.md)
- Accessibility baseline: [`explanation/concepts/accessibility.md`](./explanation/concepts/accessibility.md)
- Database schema: [`reference/database/schema.md`](./reference/database/schema.md)
- Tenant public HTTP and device WebSocket: [`reference/api/tenant-api.md`](./reference/api/tenant-api.md)
- Error code vocabulary: [`reference/api/error-codes.md`](./reference/api/error-codes.md)
- Firmware runtime contract: [`reference/firmware.md`](./reference/firmware.md)
- Glossary: [`reference/glossary.md`](./reference/glossary.md)
- Provision a tenant: [`how-to/provision-tenant.md`](./how-to/provision-tenant.md)
- Deploy or update a host runtime: [`how-to/deploy-to-production.md`](./how-to/deploy-to-production.md)
- Release gate: [`meta/release-gate.md`](./meta/release-gate.md)
