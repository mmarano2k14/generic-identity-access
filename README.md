# Identity & Access

Independent and reusable .NET identity and access foundation with no dependency on consuming application assemblies.

This repository provides a reusable architecture for identity, multi-tenant access control, RBAC, TRN-based authorization, configurable PostgreSQL multi-database routing, and TypeScript / Next.js integration.

> **Status:** foundation implementation in progress. The current codebase includes the initial domain model, routing contracts, ASP.NET Core host, validation infrastructure, and TypeScript client foundations. Authentication, PostgreSQL persistence, OIDC, MFA, and full RBAC integration are introduced incrementally and must not be assumed complete unless explicitly validated in the repository documentation.

## Stack and Scope

| Area | Choice | Scope |
|---|---|---|
| Server | .NET 10 / ASP.NET Core targeting `net10.0` | Identity and access API |
| Domain | Custom User, Tenant, Membership, Group, Policy, and Binding models | Reusable identity and authorization model |
| Storage | PostgreSQL | Configurable multi-database placement behind one logical API |
| Routing | Server-side database route resolver | Trusted routing with no client-supplied connection strings |
| Authorization | Existing .NET RBAC engine | TRN-based authorization and capability enforcement |
| Client | TypeScript | Public client contracts and transport |
| Next.js | Server-side integration | Reusable application integration layer |
| Authentication | ASP.NET Core Identity / OpenID Connect direction | To be integrated incrementally |
| MFA | TOTP / passkey-capable security model | Planned security layer |

## Repository Structure

```text
identity-access/
├── IdentityAccess.sln
├── global.json
├── Directory.Build.props
├── Directory.Packages.props
├── src/
│   ├── IdentityAccess.Contracts/
│   ├── IdentityAccess.Domain/
│   ├── IdentityAccess.Application/
│   └── IdentityAccess.Api/
├── tests/
│   └── IdentityAccess.Tests/
├── clients/
│   └── typescript/
├── examples/
│   └── nextjs/
├── scripts/
│   ├── verify.ps1
│   ├── verify.sh
│   └── run-api.ps1
├── docs/
│   ├── ARCHITECTURE_FOUNDATION.md
│   ├── VALIDATION.md
│   └── DEPENDENCIES.md
└── CHANGELOG.md
```

## Getting Started on Windows / PowerShell

Install the **.NET 10 SDK**, then verify the available SDKs:

```powershell
dotnet --list-sdks
```

The `global.json` accepts stable .NET 10 feature bands and patches starting from `10.0.100`.

A recent .NET 10 SDK is required.

Test dependencies are versioned in `Directory.Packages.props`.

From the directory containing `IdentityAccess.sln`:

```powershell
dotnet restore IdentityAccess.sln
dotnet build IdentityAccess.sln -c Release --no-restore
dotnet test IdentityAccess.sln -c Release --no-build --no-restore
```

The verification script runs the same sequence, stops on the first failure, and writes TRX results under `artifacts/test-results/`:

```powershell
.\scripts\verify.ps1
```

## Running the API

```powershell
dotnet run --project src/IdentityAccess.Api --launch-profile http
```

The local development profile uses:

```text
http://127.0.0.1:5080
```

Do not expose a development host publicly by forcing the `Development` environment.

## Diagnostic Endpoints

| Request | Expected behavior |
|---|---|
| `GET /health/live` | Process liveness |
| `GET /health/ready` | Readiness based on configured capabilities |
| `GET /api/v1/system/info` | Module and configuration information |

Security-sensitive account, group, authorization, OIDC, session, and MFA endpoints must only be introduced when their backend guarantees are implemented and validated.

## TypeScript Client

From `clients/typescript`:

```powershell
npm install
npm test
```

The TypeScript client is designed as a reusable integration layer.

Applications must never receive PostgreSQL connection strings, database credentials, internal RBAC stores, or privileged infrastructure details.

After starting the .NET API, a smoke test can be executed against the local host:

```powershell
npm run smoke -- http://127.0.0.1:5080
```

The Next.js example is located under:

```text
examples/nextjs/
```

## Identity Model

The foundation keeps the following concepts separate:

```text
User
Tenant
Tenant Membership
User Group
Group Membership
Application Context
Permission Policy
Policy Binding
Authentication Context
Access Context
Database Route
```

An identity scope is not a tenant, application, or physical database.

A database location is infrastructure placement, not identity and not authorization.

Moving data between physical databases must not redefine the logical identity of a user, tenant, policy, or membership.

## RBAC and TRN-Based Authorization

The architecture reuses a .NET RBAC engine and preserves capability-oriented authorization.

Typical declarative usage:

```csharp
[RequireCapability("observability", "ledger", "read")]
```

Programmatic authorization remains available through an injected authorization engine:

```csharp
private readonly IAuthorizationEngine _auth;
```

Example:

```csharp
if (!_auth.IsAllowed("billing", "invoice", "refund"))
{
    return;
}
```

The TypeScript integration does **not** reimplement the RBAC decision engine.

It provides client-side and server-side integration contracts while the .NET backend remains the authorization authority.

UI visibility is never considered sufficient access control.

## Groups, Policies, and Bindings

The authorization model separates membership from permissions.

```text
User
  ↓
Tenant Membership
  ↓
Group Membership
  ↓
Permission Policy
  ↓
Policy Binding
  ↓
Capabilities + Scope
  ↓
TRN representation
  ↓
RBAC evaluation
```

A group organizes subjects.

A policy describes permissions.

A binding grants a policy to a subject or group within a defined scope.

Generating or knowing a TRN never grants permission by itself.

## PostgreSQL Multi-Database Routing

The architecture uses one logical Identity & Access API capable of working with multiple PostgreSQL databases.

Physical placement is resolved server-side.

```text
Application / Identity Scope
            ↓
Database Route Resolver
            ↓
Registered Destination
            ↓
PostgreSQL
```

Routing rules:

- clients never provide connection strings;
- a database name is never a public identity;
- missing routes do not fall back to another database;
- ambiguous routes are rejected;
- disabled destinations are not silently replaced;
- route selection remains stable for the duration of an operation;
- secrets are resolved by trusted server infrastructure;
- route caching must not become authorization caching.

A single application may use multiple PostgreSQL destinations.

A destination may host more than one identity scope when explicitly configured.

## Authentication and Sessions

The target architecture supports real account lifecycle and authentication flows.

The planned security surface includes:

- account creation or invitation;
- credential verification;
- login and logout;
- account recovery;
- session management;
- session revocation;
- OAuth 2.0 / OpenID Connect;
- Authorization Code + PKCE where applicable;
- application-specific audiences;
- secure server-side integration for Next.js.

Shared infrastructure does not automatically imply shared accounts, shared sessions, or SSO.

Those are explicit identity-boundary decisions.

## MFA and Sensitive Operations

The architecture is designed to support stronger authentication for sensitive operations.

Planned capabilities include:

- TOTP;
- passkeys / WebAuthn;
- recovery codes;
- verified factor enrollment;
- factor replacement;
- factor removal;
- step-up authentication;
- stronger controls for privileged administration.

Recovery mechanisms must not become weaker bypass paths around the normal security policy.

## Security Boundaries

The backend is the security authority.

The following principles apply:

- authorization is enforced server-side;
- UI restrictions are not security boundaries;
- credentials and MFA secrets are never part of profile responses;
- PostgreSQL connection information is never returned to clients;
- a TRN is not a credential;
- a database route is not a permission;
- an application key received from a client is a requested context, not proof of authorization;
- failures in RBAC, storage, routing, or supporting infrastructure fail closed for protected operations;
- authorization failures and technical failures remain distinct.

## Application Integration

Each consuming application integrates the reusable client and UI components while preserving its own application-specific capability model.

The identity service remains generic.

Applications define their capabilities and scopes without moving business-specific domain concepts into the identity core.

The backend validates all scopes and authorization decisions independently from the UI.

## Development Roadmap

The implementation is developed incrementally.

Main areas include:

1. identity and tenant foundations;
2. routing contracts;
3. server-side routing provider;
4. PostgreSQL destination resolution;
5. Npgsql / EF Core persistence;
6. schema and migrations;
7. custom directory and membership operations;
8. groups and policies;
9. TRN generation and bindings;
10. RBAC integration;
11. real authentication;
12. OpenID Connect / OAuth 2.0;
13. sessions and revocation;
14. MFA and recovery;
15. TypeScript integration;
16. Next.js integration;
17. isolation, concurrency, failure, and recovery validation.

## Validation Principles

Every increment should distinguish:

- code that exists;
- code that compiles;
- tests that were actually executed;
- integrations that were actually exercised;
- features that are architectural targets only.

A test that was not executed must never be presented as successful.

A successful login alone is not evidence of correct multi-tenant isolation.

A working route alone is not evidence of correct authorization.

## Documentation

Architecture, validation notes, dependencies, operational limits, and implementation decisions are maintained under `docs/`.

`CHANGELOG.md` records technical changes for this project only.
