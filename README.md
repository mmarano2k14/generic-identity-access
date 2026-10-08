# Generic Identity & Access

Generic Identity & Access is a reusable, application-neutral identity, authentication, authorization, administration, and security platform built on .NET, PostgreSQL, TypeScript, React, and Next.js.

The repository provides a server-authoritative identity core together with a categorized public SDK that consumer applications can integrate without copying authentication logic, RBAC evaluation, tenant routing, or security administration behavior.

## Core responsibilities

Generic Identity & Access owns:

```text
Identity scopes
Users and credentials
Tenants and tenant memberships
Authentication and session validation
Authorization and RBAC evaluation
Groups and managed policies
Resource scopes
Delegated identity-scope administration
Application security models and capability catalogs
MFA administration and authenticator lifecycle metadata
Session containment operations
Security audit evidence
Generic organization identity and membership integration
```

Consumer applications retain ownership of their routes, navigation, branding, business semantics, and application-specific capability declarations.

## Architecture

```text
Consumer application
        │
        ├── @generic-identity/next
        │       │
        │       ├── server-only session and administration workflows
        │       └── consumer-owned routes and Server Actions
        │
        ├── @generic-identity/react
        │       └── reusable presentation components and administration pages
        │
        ├── @generic-identity/auth
        │       └── framework-neutral authentication / authorization facade
        │
        └── @generic-identity/contracts
                └── passive public contracts
                         │
                         v
              @identity-access/client
              transitional transport bridge
                         │
                         v
                 ASP.NET Core API
                         │
        ┌────────────────┼────────────────┐
        │                │                │
   PostgreSQL      Authentication       RBAC
                    / MFA / OIDC      evaluation
```

The public package dependency direction is strictly one-way:

```text
contracts
   ↑
auth
   ↑
react
   ↑
next
```

The four public Generic Identity packages are aligned at version `1.5.0`. The transitional `@identity-access/client` transport remains at `0.26.0` and should not be imported directly by new consumer applications.

## Public SDK categories

The categorized SDK is organized by responsibility:

1. **Account & Authentication** — sign-in/out, current session, password lifecycle, recovery, and step-up authentication.
2. **Directory** — users, tenants, tenant memberships, membership candidates, and tenant projections.
3. **Organizations** — organization hierarchy, membership, lifecycle, and ResourceScope linkage.
4. **Access Control** — groups, managed policies, policy bindings, resource scopes, delegated authority, and authorization evaluation.
5. **Application Security** — security models, manifests, scope types, capability catalogs, and trusted administration context.
6. **Security Operations** — MFA administration, authenticator lifecycle, session containment, and security audit.
7. **Protocol & Diagnostics** — protocol and diagnostic surfaces that remain optional for the main administration SDK.

See [`docs/shared-identity/FULL_SDK_CATEGORY_MODEL.md`](docs/shared-identity/FULL_SDK_CATEGORY_MODEL.md) and [`docs/shared-identity/FULL_SDK_FEATURE_MATRIX.md`](docs/shared-identity/FULL_SDK_FEATURE_MATRIX.md).

## Administration integration status

As of 2026-10-07, the reusable administration workflows have been qualified for:

```text
Authentication and session establishment
Authorization / RBAC
Tenants
Application Security
Users
Tenant memberships
Server-backed entity autocomplete
Organizations
Groups
Managed Policies
Resource Scopes
Delegated Authority
Security Audit
Session security and containment
```

The MFA administration workflow is implemented and source-qualified. Its dedicated live functional acceptance remains intentionally pending.

The current administration architecture follows three rules:

```text
1. The backend remains authoritative.
2. The SDK owns reusable workflows and presentation contracts.
3. Consumer applications remain thin route / action adapters.
```

See [`docs/shared-identity/ADMINISTRATION_INTEGRATION.md`](docs/shared-identity/ADMINISTRATION_INTEGRATION.md).

## Important authorization boundaries

Tenant-scoped authorization and identity-scope administration are separate models:

```text
Tenant Group
  -> Managed Policy
  -> Resource Scope
  -> application / tenant authorization
```

```text
Scope Authority Group
  -> Scope Authority Policy
  -> identity-scope administrative authority
```

A Super Administrator is therefore represented through Delegated Authority rather than being duplicated into tenant Groups or Managed Policies.

Organization membership is also distinct from authorization:

```text
OrganizationMembership
= organizational belonging

GroupMembership + Managed Policy + ResourceScope
= authorization
```

## Security operations semantics

The repository intentionally does not manufacture backend capabilities that do not exist.

In particular:

- there is no administrative active-session listing endpoint;
- the Sessions administration workspace uses bounded security-audit evidence plus server-confirmed revocation operations;
- TOTP enrollment, WebAuthn registration, and recovery-code generation remain unavailable as public administration APIs where no public backend endpoint exists;
- security audit remains read-only and bounded;
- provider secrets, passwords, session credentials, refresh tokens, and private authenticator material are never part of shared administration presentation contracts.

## Repository structure

```text
src/
  IdentityAccess.Api/
  IdentityAccess.Application/
  IdentityAccess.Authorization/
  IdentityAccess.Contracts/
  IdentityAccess.Domain/
  IdentityAccess.Infrastructure.*
  IdentityAccess.Mfa.*
  IdentityAccess.Rbac*/
  OrganizationDirectory.*/
  OrganisationProfile.*/

clients/
  typescript/

packages/
  contracts/
  auth/
  react/
  next/

examples/
  nextjs/admin/

docs/
  shared-identity/
  organization-directory/
  organisation-profile/

scripts/
  shared-identity/
  authentication/
  postgresql/
```

`OrganisationProfile` remains outside the categorized Generic Identity SDK boundary. It is co-located but owns application-semantic profile concerns rather than core identity or authorization semantics.

## Verification

Run the repository qualification from the repository root:

```powershell
.\scripts\verify.ps1
```

For the shared Generic Identity package boundary:

```powershell
.\scripts\shared-identity\verify.ps1
```

The validation suite covers public contracts, authentication and authorization, React/Next.js boundaries, consumer integration, application security, directory administration, organizations, access control, security operations, workflow composition, package boundaries, and release qualification.

## Development principles

- fail closed on authorization and configuration errors;
- never infer permission from UI visibility;
- never silently widen tenant visibility;
- preserve optimistic concurrency through backend versions;
- use server-backed bounded lookup for relational administration fields;
- keep authentication credentials and security secrets outside browser-visible contracts;
- do not duplicate RBAC, TRN construction, or database-routing logic in consumers;
- keep reusable SDK source consumer-neutral;
- classify missing backend capabilities explicitly rather than inventing public APIs.

## Documentation

Start with:

- [`docs/shared-identity/ADMINISTRATION_INTEGRATION.md`](docs/shared-identity/ADMINISTRATION_INTEGRATION.md)
- [`docs/shared-identity/FULL_SDK_CATEGORY_MODEL.md`](docs/shared-identity/FULL_SDK_CATEGORY_MODEL.md)
- [`docs/shared-identity/FULL_SDK_FEATURE_MATRIX.md`](docs/shared-identity/FULL_SDK_FEATURE_MATRIX.md)
- [`docs/shared-identity/SECURITY_OPERATIONS.md`](docs/shared-identity/SECURITY_OPERATIONS.md)
- [`docs/shared-identity/APPLICATION_SECURITY.md`](docs/shared-identity/APPLICATION_SECURITY.md)
- [`docs/shared-identity/ACCESS_CONTROL.md`](docs/shared-identity/ACCESS_CONTROL.md)
- [`docs/shared-identity/ACCOUNT_DIRECTORY_SDK.md`](docs/shared-identity/ACCOUNT_DIRECTORY_SDK.md)
- [`docs/shared-identity/ORGANIZATIONS.md`](docs/shared-identity/ORGANIZATIONS.md)
