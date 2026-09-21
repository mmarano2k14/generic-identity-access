# Changelog

## 0.2.0 - 2026-09-21

### Added

- Server-side routing contracts for application, identity scope and identity-directory placement.
- Immutable resolved destinations with configuration revision, route version and opaque secret references.
- Configuration-backed route resolver and registered pre-authentication directory locator.
- Strict JSON configuration validation, including duplicate properties, required fields,
  unsupported fields, bounded input size and catalogue cardinality limits.
- Destination and route uniqueness checks, referential validation and rejection of
  conflicting physical authorities for one identity scope.
- Explicit route, destination and authentication-context administrative disabling without fallback.
- Optional API startup registration from one server-selected file, with fail-fast validation.
- Synthetic configuration showing one application across two destinations and distinct scopes
  sharing one destination without implicit account merging.
- 87 additional declared .NET test cases for contracts, configuration, routing, bootstrap and API integration.

### Changed

- Server source version advanced to 0.2.0; public diagnostic contract version remains v1.
- Readiness removes only the database-routing blocker after successful file activation.
- Existing API tests explicitly disable externally selected routing configuration.
- Documentation records initial whole-directory placement and startup-only configuration lifetime.

### Compatibility

- Public DTOs and TypeScript client sources remain unchanged.
- Existing NuGet dependency versions remain unchanged; the routing provider adds no package dependencies.
- Physical placement remains separate from subject identity, tenant membership and authorization.
- Runtime RBAC, TRN grammar, context rotation and execution components are not modified or simulated.

### Limitations

- Routing does not open PostgreSQL connections, resolve secret values or provide persistence.
- Tenant data within one identity scope is not split across physical identity databases.
- Bootstrap lookup does not authenticate users or establish trust in HTTP or OIDC clients.
- Configuration changes require a controlled restart; no live reload or cross-instance propagation is provided.
- Route changes do not migrate data or provide durable configuration rollback protection.
- Authentication, OIDC, MFA, RBAC integration and production readiness remain unimplemented.

### Validation

- TypeScript compilation and strict type checking passed.
- 26 TypeScript transport tests passed with no skipped tests, including a local Node HTTP fixture.
- XML/JSON parsing, project references, dependency preservation and C# lexical delimiter checks passed.
- .NET restore, build, tests and live API smoke were not executed because the SDK was unavailable.
- PostgreSQL integration and Next.js application build were not executed.


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
