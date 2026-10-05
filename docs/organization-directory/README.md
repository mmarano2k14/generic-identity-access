# Organization Directory module

Organization Directory is integrated into the Generic Identity & Access repository and solution.

```text
Solution:              IdentityAccess.sln
PostgreSQL database:   generic_identity_access_default
Identity schema:       identity_access
Organization schema:   organization_directory
```

The schemas share one database but keep independent ownership and migration metadata.

## PostgreSQL Persistence commands

```powershell
.\scripts\postgresql\apply-default-schema.ps1
.\scripts\organization-directory\postgresql\apply-schema.ps1
.\scripts\organization-directory\postgresql\verify-migration-integrity.ps1
.\scripts\organization-directory\postgresql\verify-organizations.ps1
```

Live store probe:

```powershell
$env:IDENTITY_ACCESS_POSTGRES_DEFAULT = "Host=127.0.0.1;Port=5432;Database=generic_identity_access_default;Username=postgres;Password=<password>"
.\scripts\organization-directory\postgresql\verify-store.ps1
```

Repository module gate:

```powershell
.\scripts\organization-directory\verify.ps1 -Configuration Release
```

Organization API and Hierarchy will expose Organization endpoints through the existing `IdentityAccess.Api`; there is no second web host.

## Organization API and Hierarchy — Organization API and hierarchy

Organization lifecycle is exposed through the existing Identity Access API host:

```text
/api/v1/identity-scopes/{identityScopeId}
/applications/{applicationKey}
/tenants/{tenantId}
/organizations
```

Available operations:

```text
GET  /organizations
GET  /organizations/{organizationId}
GET  /organizations/tree
GET  /organizations/{organizationId}/children

POST /organizations
PUT  /organizations/{organizationId}

POST /organizations/{organizationId}/disable
POST /organizations/{organizationId}/enable
```

Organization keys are stable after creation. Mutable definition state consists of display name, organization type, parent and lifecycle status.

All routes are fail-closed and require:

```text
identity-access / organization / read
identity-access / organization / write
```

The development scope administrator's wildcard authority can exercise these routes. Delegated tenant policy administration for this dedicated capability is completed in the later security milestone.

## Organization Membership — OrganizationMembership

Organization belonging is explicit:

```text
User
 ↓
TenantMembership                Identity Access
 ↓
OrganizationMembership          Organization Directory
 ↓
Organization
```

This relation does **not** grant authorization.

Authorization remains:

```text
TenantMembership
 ↓
GroupMembership
 ↓
Managed Policy
 ↓
Resource Scope
 ↓
RBAC
```

Administration endpoints:

```text
GET    .../organizations/{organizationId}/memberships
GET    .../organizations/{organizationId}/memberships/{tenantMembershipId}
POST   .../organizations/{organizationId}/memberships
POST   .../organizations/{organizationId}/memberships/{tenantMembershipId}/suspend
POST   .../organizations/{organizationId}/memberships/{tenantMembershipId}/activate
DELETE .../organizations/{organizationId}/memberships/{tenantMembershipId}

GET    .../tenant-memberships/{tenantMembershipId}/organizations
```

Database ownership:

```text
organization_directory.organization_memberships
    -> organization_directory.organizations
    -> identity_access.tenant_memberships
```

Apply and verify:

```powershell
.\scripts\organization-directory\postgresql\apply-schema.ps1
.\scripts\organization-directory\postgresql\verify-migration-integrity.ps1
.\scripts\organization-directory\postgresql\verify-memberships.ps1
.\scripts\organization-directory\postgresql\verify-store.ps1
```

## Identity Access Resource Scope Integration — Identity Access ResourceScope integration

An Organization may now be linked to one existing Identity Access ResourceScope per application:

```text
Organization
     │
     └── application-aware link
                  │
                  v
Identity Access ResourceScope
```

Example:

```text
Organization = Urban Flower
Application  = consumer-app
ResourceScope = Urban Flower
Scope type   = organization
Model version = 2
```

The caller submits only the `resourceScopeId`. Identity Access remains authoritative for:

```text
Tenant
Application
Scope type
Security-model version
Scope status
```

The administration API is:

```text
GET    .../organizations/{organizationId}/resource-scope-link
POST   .../organizations/{organizationId}/resource-scope-link
PUT    .../organizations/{organizationId}/resource-scope-link
DELETE .../organizations/{organizationId}/resource-scope-link
```

Apply and verify:

```powershell
.\scripts\organization-directory\postgresql\apply-schema.ps1
.\scripts\organization-directory\postgresql\verify-migration-integrity.ps1
.\scripts\organization-directory\postgresql\verify-resource-scope-links.ps1
```

This link expresses the authorization boundary corresponding to an Organization. It does not itself grant any capability.

## Administration Security and Audit — Administration security and audit

Organization Directory administration now has three explicit capability families:

```text
identity-access / organization / read|write
identity-access / organization-membership / read|write
identity-access / organization-scope-link / read|write
```

The administration security manifest is versioned forward to:

```text
modelVersion = 3
```

This is intentional. Application security manifests are immutable once registered, so the Organization Directory capability additions must not rewrite model version 2.

After applying this milestone in a development environment, re-run the supported administrator bootstrap so model version 3 and the corresponding local administration policy version are registered:

```powershell
$env:PGPASSWORD = "<postgres-password>"
.\scripts\authentication\bootstrap-dev-admin.ps1
```

No Organization Directory PostgreSQL schema migration is introduced by Administration Security and Audit.

Semantic security events are emitted for:

```text
Organization create/update/status
OrganizationMembership add/status/remove
Organization ResourceScope link/relink/unlink
```

They are written through the existing routed Identity Access security-audit sink. Audit sink failure is best-effort and does not replace the result of a successful primary mutation.

## TypeScript Connector and Identity Membership UI — TypeScript connector and Identity Membership UI

Organization Directory is integrated into the existing Identity Access administration host.
There is no second Next.js application and no project selector.

The existing page remains the entry point:

```text
/identity/memberships
```

Inside one selected tenant it now composes:

```text
Identity Membership
├── Tenant members
├── Group assignments
├── Member Organizations
└── Organization directory
    ├── hierarchy
    ├── lifecycle
    └── ResourceScope mapping
```

The TypeScript client exposes:

```text
client.administration.organizations
client.administration.organizationMemberships
client.administration.organizationResourceScopeLinks
```

Member assignment remains semantically strict:

```text
OrganizationMembership = belonging
GroupMembership + Managed Policy + ResourceScope + RBAC = authorization
```

All UI reads are capability-gated before protected endpoints are called. Mutations remain server actions and the .NET API re-evaluates authorization on every request.

### Administration reference selection

ResourceScope link/relink controls use the shared server-backed entity autocomplete. The browser does not preload the tenant ResourceScope catalog and never accepts a free-form ResourceScope identifier.

## Separation and qualification

Organization Directory administration is intentionally composed from focused services rather
than a shared god service:

```text
IdentityAccessAdminMembershipOverviewService
IdentityAccessAdminOrganizationOverviewService

IdentityAccessAdminMutationService
IdentityAccessAdminOrganizationMutationService
IdentityAccessAdminOrganizationMembershipMutationService
IdentityAccessAdminOrganizationScopeLinkMutationService
```

The Organization UI remains embedded in `/identity/memberships`, but its create, table, and
ResourceScope-link responsibilities are separate components.

Run the module release gate with:

```powershell
.\scripts\organization-directory\verify-qualification.ps1 -Configuration Release
```

See [`QUALIFICATION.md`](QUALIFICATION.md) for the exact database prerequisites, full gate,
partial-gate semantics, and browser-qualification handoff.
