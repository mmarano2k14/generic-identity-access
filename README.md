# Generic Identity & Access

Reusable identity, authentication, authorization, and access-control foundation built on .NET 10 and PostgreSQL.

The service provides a generic multi-tenant directory, configurable PostgreSQL routing, resource-scope hierarchies, policy assignment, external RBAC integration, local password authentication, opaque sessions, and an ASP.NET Core MVC administration API.

The repository is application-agnostic. Consuming systems define their own resource-scope types and capability models without introducing product-specific concepts into the core.

## Capabilities

- ASP.NET Core MVC API with Swagger / OpenAPI.
- Stable identity scopes, users, tenants, memberships, groups, and group membership.
- Server-controlled PostgreSQL multi-database routing.
- Optimistic concurrency through explicit row versions.
- Application-defined resource-scope hierarchies.
- Permission policies, statements, and scoped group-policy bindings.
- Persistent whole-segment wildcard capability patterns.
- Neutral RBAC adapter boundary with external wildcard evaluation.
- Local password credential management, lockout, opaque sessions, and registered redirect URIs.
- Provider-neutral MFA policy and authenticator lifecycle with pluggable TOTP, recovery-code, and WebAuthn providers, policy-enforced provider execution, effective user MFA state, and hardened lost-factor administration.
- Class-composed TypeScript / Next.js connector with separated system, authentication, OIDC, authorization, and administration responsibility classes, bounded core collection reads, and a premium multi-page server-first administration control center.

## Next.js Administration Module

`examples/nextjs/admin` contains a copyable App Router administration structure with separate pages for users, tenants, memberships, groups, policies, resource scopes, MFA, sessions, and identity-scope authority. Protected data loading, bearer provenance, validation, and API mutations stay server-side. Client Components are limited to presentation interaction and framework UI state. `IdentityAccessAdminUiBuilder` supplies permission-filtered route metadata but never replaces server-side authorization.

Functional administration includes server-confirmed create flows, loading/error/empty states, dialogs, path revalidation after successful mutations, explicit confirmation for destructive session revocation, a real `/identity` overview, structured record details, automatic light/dark presentation, and responsive workspace composition. Framework-required Server Action functions remain thin adapters over the class-based `IdentityAccessAdminMutationService`.

All custom administration CSS is owned by one file: `examples/nextjs/admin/styles/identity-access-admin.css`. Do not introduce `*.module.css`, component-local stylesheet files, or inline style objects. The premium design system, dark-mode tokens, responsive rules, and motion/accessibility treatment all remain centralized in this one stylesheet.

The administration example is also a runnable standalone Next.js host. It contains a real `/login` route, server-side password -> OIDC Authorization Code + PKCE exchange, HTTP-only cookie handling, explicit sign-out, and the protected `/identity` workspace. The host keeps the existing class-based connector and authorization boundaries; it does not move Identity Access tokens or authorization decisions into browser code.

Run it after configuring a real registered public authentication/OIDC client:

```powershell
cd examples\nextjs\admin
Copy-Item .env.local.example .env.local
npm install --package-lock=false
npm run dev
```

Then open `http://127.0.0.1:3000/login`. The configured redirect URI must exactly match the URI registered by the Identity Access API.

For a complete local administration root-of-trust setup, create the development administrator first:

```powershell
$env:PGPASSWORD = "<local-postgres-password>"
.\scripts\authentication\bootstrap-dev-admin.ps1
```

The bootstrap uses login `admin`, securely prompts for the password, creates the required local user/tenant/credential/security-model/authority records, and writes the non-secret Next.js `.env.local` values. Start the API with the matching local authentication/OIDC/RBAC profile:

```powershell
$env:IDENTITY_ACCESS_POSTGRES_DEFAULT = "Host=127.0.0.1;Port=5432;Database=generic_identity_access_default;Username=postgres;Password=<password>"
$env:IDENTITY_ACCESS_RBAC_REFERENCE_DIRECTORY = "<path-to-external-rbac-release-directory>"
.\scripts\authentication\run-dev-admin-api.ps1
```

See [`docs/DEVELOPMENT_ADMIN_BOOTSTRAP.md`](docs/DEVELOPMENT_ADMIN_BOOTSTRAP.md).

## Architecture

```text
ASP.NET Core API
      |
      v
MVC Controllers
      |
      v
Application Services
      |
      +-------------------+-------------------+
      |                   |                   |
      v                   v                   v
Directory             Authorization       Authentication
      |                   |                   |
      v                   v                   v
Routing / Stores     RBAC Adapter       Credentials / Sessions
      |                   |                   |
      v                   v                   v
PostgreSQL         External RBAC       PostgreSQL
```

Identity, database placement, authentication, authorization, and application resource hierarchy remain separate concerns.

See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) for the architectural contracts and invariants.

See [`docs/MFA_PROVIDER_ARCHITECTURE.md`](docs/MFA_PROVIDER_ARCHITECTURE.md) for the generic MFA/provider boundary, [`docs/TOTP_PROVIDER.md`](docs/TOTP_PROVIDER.md) for TOTP, [`docs/RECOVERY_PROVIDER.md`](docs/RECOVERY_PROVIDER.md) for recovery codes, [`docs/WEBAUTHN_REGISTRATION.md`](docs/WEBAUTHN_REGISTRATION.md) plus [`docs/WEBAUTHN_AUTHENTICATION.md`](docs/WEBAUTHN_AUTHENTICATION.md) for passkey/WebAuthn, and [`docs/MFA_INTEGRATION_HARDENING.md`](docs/MFA_INTEGRATION_HARDENING.md) for cross-provider policy and administration hardening; [`docs/MFA_SESSION_ASSURANCE_OIDC.md`](docs/MFA_SESSION_ASSURANCE_OIDC.md) for durable session assurance, MFA step-up, and OIDC integration.

## Repository Structure

```text
src/
  IdentityAccess.Api/
  IdentityAccess.Application/
  IdentityAccess.Authorization/
  IdentityAccess.Contracts/
  IdentityAccess.Domain/
  IdentityAccess.Infrastructure.Authentication/
  IdentityAccess.Infrastructure.ConfigurationRouting/
  IdentityAccess.Infrastructure.PostgreSql/
  IdentityAccess.Mfa.Totp/
  IdentityAccess.Mfa.Recovery/
  IdentityAccess.Mfa.WebAuthn/
  IdentityAccess.Rbac/
  IdentityAccess.Rbac.MultiplexedAdapter/

tests/
  IdentityAccess.Tests/
  IdentityAccess.Rbac.MultiplexedIntegrationTests/

clients/
  typescript/

examples/
  nextjs/

docs/
```

C# source uses block-scoped namespaces and one declared top-level type per file. See [`docs/SOURCE_LAYOUT.md`](docs/SOURCE_LAYOUT.md).

## Requirements

- .NET 10 SDK
- PostgreSQL 18 or another validated compatible PostgreSQL release
- Node.js for the TypeScript client tests
- external RBAC binaries only when running the dedicated RBAC compatibility suite

Current centrally managed .NET package versions are documented in [`docs/DEPENDENCIES.md`](docs/DEPENDENCIES.md).

## Build and Test

From the repository root:

```powershell
.\scripts\verify.ps1
```

The standard verification now also runs TypeScript source-consistency, build, tests, and strict type checking.

Equivalent commands:

```powershell
dotnet restore IdentityAccess.sln
dotnet build IdentityAccess.sln -c Release --no-restore
dotnet test IdentityAccess.sln -c Release --no-build --no-restore
```

Validation procedures are documented in [`docs/VALIDATION.md`](docs/VALIDATION.md).

## Local PostgreSQL

The default local development database is:

```text
generic_identity_access_default
```

The owned schema is:

```text
identity_access
```

Create the database:

```powershell
psql -U postgres -d postgres -f .\scripts\postgresql\create-default-database.sql
```

Apply schema migrations:

```powershell
$env:PGPASSWORD = "<postgres-password>"
.\scripts\postgresql\apply-default-schema.ps1
Remove-Item Env:PGPASSWORD
```

Database routing is server-controlled. Clients never provide connection strings, secret references, destination keys, or database names.

See [`docs/ROUTING_CONFIGURATION.md`](docs/ROUTING_CONFIGURATION.md).

## Running the API

```powershell
dotnet run --project src\IdentityAccess.Api --launch-profile http
```

Development URL:

```text
http://127.0.0.1:5080
```

Swagger UI:

```text
http://127.0.0.1:5080/swagger
```

OpenAPI document:

```text
http://127.0.0.1:5080/swagger/v1/swagger.json
```

No route is currently mapped to `/`; a `404` at the root URL is expected.

## Authorization

Authorization data is resolved from group membership, permission policies, policy statements, resource-scope bindings, and assigned capability grants.

TRN materialization is owned by `IdentityAccess.Rbac`. Wildcard authorization is not reimplemented by this repository; the external RBAC engine remains the decision authority for the supported wildcard forms.

The external compatibility suite can be run with:

```powershell
.\scripts\verify-multiplexed-rbac.ps1 `
  -ReferenceDirectory "<path-to-external-rbac-release-directory>"
```

See [`docs/RBAC_EXTERNAL_ADAPTER.md`](docs/RBAC_EXTERNAL_ADAPTER.md).

## Authentication

The service currently provides a local authentication foundation with:

- password hashing;
- credential metadata;
- failed-attempt tracking and lockout;
- opaque random session tokens;
- hashed session-token persistence;
- session validation and logout;
- registered login and post-logout redirect URIs.

OAuth 2.0 / OpenID Connect Authorization Code + PKCE, rotating refresh tokens, process-pinned multi-key RSA signing-key rotation, and Bearer access-token validation for protected administration APIs are implemented for registered public clients. Bearer authentication revalidates the referenced local session and current active user before entering the existing RBAC pipeline. The provider-neutral MFA policy/authenticator foundation, optional TOTP provider, hash-only single-use recovery-code provider, and WebAuthn/passkey registration/authentication provider are implemented. Concrete provider execution is constrained by the current application MFA policy, administration exposes effective user MFA state, normal revocation protects the final non-recovery verification factor required by a Required policy, and explicit lost-factor revocation revokes active local sessions. Local sessions now persist password-only or multi-factor assurance, exact-session TOTP/recovery/WebAuthn step-up can raise that assurance, and OIDC authorization enforces current Required-MFA freshness while pinning `auth_time`, `acr`, and `amr` into issued grants and tokens. A generic declarative step-up requirement for arbitrary non-OIDC business operations remains application policy rather than a repository-wide controller attribute.

See [`docs/LOCAL_AUTHENTICATION_FOUNDATION.md`](docs/LOCAL_AUTHENTICATION_FOUNDATION.md).

## Security Principles

- Authentication and authorization fail closed.
- A database route is not an authorization decision.
- A TRN is not a credential.
- Resource-scope filtering occurs before external RBAC evaluation.
- Wildcard evaluation is delegated to the external RBAC engine.
- Mutable records use optimistic concurrency; stale writes are rejected.
- Operation state is not shared globally between concurrent requests.
- Secrets and connection strings are never exposed through public API contracts.
- Redirect URIs are registered and matched server-side.

## Documentation

- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — current architecture and invariants.
- [`docs/ROUTING_CONFIGURATION.md`](docs/ROUTING_CONFIGURATION.md) — routing configuration contract.
- [`docs/POSTGRESQL_SCHEMA_AND_CONCURRENCY.md`](docs/POSTGRESQL_SCHEMA_AND_CONCURRENCY.md) — persistence and concurrency model.
- [`docs/PERMISSION_POLICY_FOUNDATION.md`](docs/PERMISSION_POLICY_FOUNDATION.md) — permission and policy model.
- [`docs/RESOURCE_SCOPE_HIERARCHY.md`](docs/RESOURCE_SCOPE_HIERARCHY.md) — generic resource hierarchy and scoped bindings.
- [`docs/RBAC_EXTERNAL_ADAPTER.md`](docs/RBAC_EXTERNAL_ADAPTER.md) — external RBAC boundary.
- [`docs/LOCAL_AUTHENTICATION_FOUNDATION.md`](docs/LOCAL_AUTHENTICATION_FOUNDATION.md) — local authentication and sessions.
- [`docs/MFA_PROVIDER_ARCHITECTURE.md`](docs/MFA_PROVIDER_ARCHITECTURE.md) — generic MFA/provider ownership boundary.
- [`docs/TOTP_PROVIDER.md`](docs/TOTP_PROVIDER.md) — TOTP provider security and persistence model.
- [`docs/RECOVERY_PROVIDER.md`](docs/RECOVERY_PROVIDER.md) — recovery-code generation, hash-only storage, replacement, and single-use consumption.
- [`docs/WEBAUTHN_REGISTRATION.md`](docs/WEBAUTHN_REGISTRATION.md) — WebAuthn/passkey registration, challenge validation, and public credential persistence.
- [`docs/WEBAUTHN_AUTHENTICATION.md`](docs/WEBAUTHN_AUTHENTICATION.md) — WebAuthn/passkey assertion verification, signature counters, backup state, and atomic challenge consumption.
- [`docs/MFA_INTEGRATION_HARDENING.md`](docs/MFA_INTEGRATION_HARDENING.md) — cross-provider policy enforcement, effective user state, safe revocation, and lost-factor administration.
- [`docs/MFA_SESSION_ASSURANCE_OIDC.md`](docs/MFA_SESSION_ASSURANCE_OIDC.md) — durable local-session assurance, provider step-up, OIDC MFA freshness, and `auth_time`/`acr`/`amr` propagation.
- [`docs/CONTROLLER_API_AND_SWAGGER.md`](docs/CONTROLLER_API_AND_SWAGGER.md) — controller and OpenAPI conventions.
- [`docs/SOURCE_LAYOUT.md`](docs/SOURCE_LAYOUT.md) — C# source conventions.
- [`docs/VALIDATION.md`](docs/VALIDATION.md) — validation procedures.
- [`docs/DEPENDENCIES.md`](docs/DEPENDENCIES.md) — toolchain and package versions.
- [`docs/PRODUCTION_QUALIFICATION.md`](docs/PRODUCTION_QUALIFICATION.md) — release-candidate gates, backup/restore validation, and operational hardening.

## Status

The repository is under active development and is not represented as production-certified. Production deployment requires completed security qualification, operational observability, backup/restore validation, and deployment-specific hardening.

## Public API documentation

.NET builds emit XML documentation for the supported public API surface. Missing public API documentation is treated as a build failure. See `docs/PUBLIC_API_DOCUMENTATION.md`.


## Health and Diagnostics

The API exposes controller-based health and diagnostics backed by ASP.NET Core health
checks:

```text
GET /health/live
GET /health/ready
GET /api/v1/system/info
```

Readiness and service metadata are derived from the services configured in the running
host. Module version information comes from the API assembly rather than a hardcoded
diagnostic value.

See `docs/HEALTH_AND_DIAGNOSTICS.md`.


## Observability and Security Audit

HTTP responses expose a server-generated `X-Correlation-ID`, request-completion logs use
structured correlation scopes, and security-relevant operations can be persisted to the
PostgreSQL `identity_access.security_events` audit table.

See `docs/OBSERVABILITY_AND_SECURITY_AUDIT.md`.


## Session Lifecycle

Local sessions are validated against current account state. User suspension invalidates
existing sessions, password change revokes subject sessions, and administration endpoints
support user-wide and registered-client-wide revocation.

See `docs/SESSION_LIFECYCLE.md`.


## Trusted Administration Context

Administrative requests can establish a server-validated per-request identity from a local
session without trusting caller-supplied user, scope, application, tenant, or permission
claims.

The trusted context is attached to the current `HttpContext` only. Capability authorization
remains fail-closed until the authorization service and external RBAC adapter are connected.

See `docs/TRUSTED_ADMINISTRATION_CONTEXT.md`.


## Administration RBAC Authorization

Tenant-scoped administration routes can delegate capability decisions through
`IdentityAuthorizationService` to the external RBAC engine.

RBAC project, namespace, and adapter location are trusted server configuration. Routes
without a tenant target remain fail-closed until a separate identity-scope administration
authority model is defined.

See `docs/ADMINISTRATION_RBAC_AUTHORIZATION.md`.


## Identity-Scope Administration Authority

Administration routes without a tenant target use dedicated identity-scope administration
groups and policies. Tenant permissions are never promoted into scope-wide authority.

Both tenant and identity-scope grant paths delegate final wildcard decisions to the same
external RBAC engine.

See `docs/IDENTITY_SCOPE_ADMINISTRATION_AUTHORITY.md`.


## Identity-Scope Authority Administration API

After explicit first-admin bootstrap, scope-authority groups, memberships, policies,
statements, and bindings are managed through RBAC-protected MVC endpoints rather than
direct SQL.

See `docs/IDENTITY_SCOPE_AUTHORITY_ADMINISTRATION.md`.


## Transactional Security Mutation Ledger

Security-sensitive PostgreSQL state changes are captured by an append-only mutation ledger
inside the same transaction as the source mutation. Semantic security events remain
higher-level enrichment.

See `docs/TRANSACTIONAL_SECURITY_MUTATION_LEDGER.md`.


## External RBAC Compatibility Hardening

The runtime-only external RBAC adapter validates and process-pins the external binary
contract before administration authorization is enabled. Compatibility reports expose
assembly versions and SHA-256 fingerprints without leaking external types into the generic
core.

See `docs/RBAC_EXTERNAL_ADAPTER.md`.


## OAuth 2.0 / OpenID Connect

The authentication module supports a strict public-client Authorization Code + PKCE S256
OpenID Connect flow with exact registered redirects, one-time hashed authorization codes,
RS256 access/ID tokens, process-pinned active-key rotation with multi-key JWKS publication, rotating SHA-256-persisted refresh-token families, consumed-token replay revocation, strict Bearer validation for protected administration APIs, current session/user continuity checks, discovery, and JWKS.

The authorization endpoint consumes an already validated local session; browser/login UI
remains a separate host concern.

See `docs/OIDC_AUTHORIZATION_CODE_PKCE.md`.

## Production Qualification

Release-candidate qualification is consolidated in:

```powershell
.\scripts\verify-production-qualification.ps1 `
  -RbacReferenceDirectory "<multiplexed-rbac-release-directory>"
```

The full gate includes repository build/tests, external RBAC compatibility, PostgreSQL security and
concurrency validation, OIDC live database fixtures, and a disposable backup/restore cycle. Run it
against a dedicated qualification database. See `docs/PRODUCTION_QUALIFICATION.md`.

