# Generic Organization Directory

Reusable, application-agnostic organization identity and membership foundation built on .NET 10 and PostgreSQL.

The repository models stable Organizations inside externally owned Tenant boundaries, supports hierarchical organization references, explicit `OrganizationMembership`, and application-aware links to external authorization Resource Scopes.

## Core principle

```text
OrganizationMembership
= organizational belonging

GroupMembership + Managed Policy + ResourceScope
= authorization
```

The directory does not authenticate users or evaluate permissions. Those responsibilities remain with Generic Identity & Access or another compatible identity/authorization system.

## Current state

Version `0.2.0` adds the first durable PostgreSQL implementation for Organizations:

- checksum-protected SQL migration metadata;
- tenant-isolated `organization_directory.organizations`;
- tenant-local parent foreign keys;
- database-enforced deep hierarchy-cycle rejection;
- tenant-local organization-key uniqueness;
- optimistic concurrency through `row_version`;
- `IOrganizationStore` and `PostgreSqlOrganizationStore`;
- deterministic paging and key lookup;
- controlled stale-write, duplicate-key and hierarchy-conflict errors;
- PostgreSQL schema, migration-integrity and hierarchy validation scripts.

`OrganizationMembership` and ResourceScope-link persistence remain domain contracts only until their dedicated implementation increments.

## Architecture

```text
External Identity System
  Tenant
  TenantMembership
        │
        v
Generic Organization Directory
  Organization
  Organization hierarchy
  OrganizationMembership
  OrganizationResourceScopeLink
        │
        v
External Authorization System
  ResourceScope
  Groups / Policies / RBAC
```

See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md).

## Repository structure

```text
src/
  OrganizationDirectory.Api/
  OrganizationDirectory.Application/
  OrganizationDirectory.Contracts/
  OrganizationDirectory.Domain/
  OrganizationDirectory.Infrastructure.PostgreSql/
    Migrations/

tests/
  OrganizationDirectory.Tests/

scripts/
  postgresql/
  verify.ps1

docs/
  ARCHITECTURE.md
  IMPLEMENTATION_ROADMAP.md
  POSTGRESQL_SCHEMA_AND_CONCURRENCY.md
  VALIDATION.md
```

## Build and test

Requirements:

- .NET 10 SDK
- PostgreSQL client tools for live database validation

Run repository verification:

```powershell
.\scripts\verify.ps1 -Configuration Release
```

## Local PostgreSQL

Default development database:

```text
generic_organization_directory_default
```

Create it once:

```powershell
psql -U postgres -d postgres -f .\scripts\postgresql\create-default-database.sql
```

Apply migrations:

```powershell
.\scripts\postgresql\apply-default-schema.ps1
```

Validate migration integrity and organization hierarchy/concurrency:

```powershell
.\scripts\postgresql\verify-migration-integrity.ps1
.\scripts\postgresql\verify-organizations.ps1
```

Run the Npgsql persistence contract against the same migrated database:

```powershell
$env:ORGANIZATION_DIRECTORY_POSTGRES_DEFAULT = "Host=127.0.0.1;Port=5432;Database=generic_organization_directory_default;Username=postgres;Password=<password>"
.\scripts\postgresql\verify-store.ps1
```

Environment overrides:

```text
ORGANIZATION_DIRECTORY_POSTGRES_DATABASE
ORGANIZATION_DIRECTORY_POSTGRES_USER
```

Use standard PostgreSQL mechanisms such as `PGPASSFILE` or `PGPASSWORD` for local credentials; do not commit passwords.

See [`docs/POSTGRESQL_SCHEMA_AND_CONCURRENCY.md`](docs/POSTGRESQL_SCHEMA_AND_CONCURRENCY.md).

## Development API

The current host exposes:

```text
GET /health/live
GET /api/v1/system/info
```

Run it with:

```powershell
dotnet run --project src\OrganizationDirectory.Api
```

Organization administration endpoints are introduced in the next implementation increment after persistence has been qualified.

## Scope boundaries

The core intentionally does not define:

```text
BusinessProfile
Commerce
Finance
Inventory
Hospitality
Providers
BusinessEntity
Evidence
Agents
Authentication
OIDC
MFA
RBAC wildcard evaluation
```

Consumer applications own application-specific semantics. Identity and authorization systems own authentication and permission decisions.
