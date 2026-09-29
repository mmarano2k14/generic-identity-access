# Organization Directory Qualification

Organization Directory is integrated into the existing Generic Identity & Access repository,
API host, PostgreSQL database, and Identity Membership administration workspace.

Qualification is intentionally split by responsibility. No single test class, service, or
script owns every concern.

## Architecture gates

The reusable module remains separated as:

```text
OrganizationDirectory.Domain
        ↓
OrganizationDirectory.Contracts
        ↓
OrganizationDirectory.Application
        ↓
OrganizationDirectory.Infrastructure.PostgreSql
```

The existing `IdentityAccess.Api` is the composition host.

Administration UI composition is also separated:

```text
Membership read model
Organization read model

Generic mutation service
Organization mutation service

Organization directory panel
├── create dialog
├── organization table
└── ResourceScope-link panel
```

The qualification gate rejects regression of Organization operations into the broad generic
mutation/read services and rejects creation of a separate `/identity/organizations` app page.

## Database prerequisites

The shared PostgreSQL database must already contain the Identity Access schema.

Set the same local values used by the Identity Access development environment, for example:

```powershell
$env:PGPASSWORD = "sa"
$env:IDENTITY_ACCESS_POSTGRES_DEFAULT = "Host=127.0.0.1;Port=5432;Database=generic_identity_access_default;Username=postgres;Password=sa"
```

Apply Organization Directory migrations before qualification:

```powershell
.\scripts\organization-directory\postgresql\apply-schema.ps1
```

## Full module qualification

Run:

```powershell
.\scripts\organization-directory\verify-qualification.ps1 -Configuration Release
```

The full gate executes:

```text
1. Repository verification
2. Organization Directory .NET build/tests
3. Organization Directory PostgreSQL probe build
4. Source/layer separation gate
5. Migration integrity
6. Organization hierarchy validation
7. OrganizationMembership validation
8. ResourceScope-link validation
9. Live PostgreSQL store probe
```

Expected final line:

```text
Organization Directory qualification: GREEN
```

Using `-SkipPostgreSql` is useful for source-only work but is explicitly partial and must not
be reported as full qualification.

## Administration-browser qualification

Organization Directory uses the existing Identity Access administration host rather than a
second frontend. Final release-candidate administration evidence therefore continues through
the repository's existing browser qualification:

```powershell
.\scripts\verify-administration-qualification.ps1 `
  -RbacReferenceDirectory "<multiplexed-rbac-release-directory>" `
  -Configuration Release
```

The API must run on `http://127.0.0.1:5080` and the existing Next.js administration host on
`http://127.0.0.1:3000`.

## Security invariants

A GREEN result must preserve all of the following:

```text
TenantMembership != permission
OrganizationMembership != permission

Organization hierarchy != automatic ResourceScope hierarchy
Organization-to-ResourceScope link != capability grant

Managed Policy + ResourceScope + external RBAC = authorization

No cross-tenant Organization membership
No cross-tenant/application ResourceScope link
No caller-trusted scope type/model version
No anonymous Organization administration
No second Organization administration application
```

## Release interpretation

This module qualification establishes that the Organization Directory integration passes its
repository, architecture, persistence, security-contract, and live-store gates.

It does not by itself replace the repository-wide production qualification, backup/restore
qualification, external RBAC compatibility validation, or real-browser administration
qualification.
