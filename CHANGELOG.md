# Changelog

## 0.1.0 - 2026-09-21

### Added

- Independent .NET 10 solution with domain, public contracts, application and API projects.
- Immutable scoped subject, tenant and application-specific group references.
- Structural membership validation and separate account, tenant, group and membership states.
- Explicit profile projections without credential or physical storage fields.
- Local diagnostic endpoints with non-ready service status and production startup rejection.
- TypeScript diagnostic HTTP transport with response validation, per-request cancellation,
  bounded timeout, redirect rejection and sanitized error categories.
- Server-only Next.js integration example, local verification scripts and contract smoke command.
- Domain, profile and API test sources plus executable TypeScript transport tests.

### Compatibility

- No direct or transitive source project reference to runtime engine assemblies.
- No dependency on the .NET SDK or CLR contracts in the TypeScript package.
- No upper Node.js version restriction; TypeScript is compiled before execution.
- Application, identity scope, tenant and physical storage placement remain separate concepts.

### Limitations

- PostgreSQL routing, connections, migrations and persistence are not implemented.
- Authentication, OIDC, MFA, permissions, TRN generation and existing RBAC integration are not implemented.
- Scope assignment, account sharing, pre-authentication bootstrap and multi-database placement remain open decisions.
- The diagnostic client does not provide sessions, token refresh or access-context rotation.
- No production readiness or security qualification is asserted.

### Validation

- TypeScript compilation passed with compiler 5.8.3.
- 26 TypeScript transport tests passed on Node.js 22.16.0, with no skipped tests.
- One passing test used a real local HTTP fixture hosted by Node.js, not the .NET API.
- .NET restore, compilation, tests and live API smoke validation were not executed because the SDK was unavailable.
- Next.js application build and PostgreSQL integration were not executed.
