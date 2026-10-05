# Organization Directory — Integrated architecture

Organization Directory is a module of the Identity Access repository. It shares the same PostgreSQL database while owning the `organization_directory` schema.

```text
generic_identity_access_default
├── identity_access
└── organization_directory
```

Every persisted Organization must reference an existing Identity Access Tenant. This is enforced by a cross-schema foreign key. Parent/child organization relationships are tenant-local.

Authorization remains owned by Identity Access. Organization API and Hierarchy will use the existing `IdentityAccess.Api` host rather than introduce a second API.

## Organization lifecycle API

The existing `IdentityAccess.Api` host owns the HTTP boundary. Organization Directory does not add a second server process.

```text
HTTP
 ↓
OrganizationsController
 ↓
IOrganizationAdministrationService
 ↓
OrganizationAdministrationService
 ↓
IOrganizationStore
 ↓
organization_directory.organizations
```

The service validates parent existence and descendant-cycle moves before persistence. PostgreSQL remains the final hierarchy integrity authority through tenant-local foreign keys and the cycle trigger.

Organization deletion remains an internal store capability only. The administration API retains durable identity and exposes explicit `enable` / `disable` lifecycle operations instead.

The dedicated administration capability is:

```text
resource = identity-access
feature  = organization
actions  = read | write
```

This delivery introduces the protected API metadata. Broader delegated-administration policy design remains a later security concern.

## Organization membership boundary

`OrganizationMembership` is an explicit organizational relationship, not an authorization grant.

```text
identity_access.tenant_memberships
          │
          │ same identity scope + tenant
          v
organization_directory.organization_memberships
          │
          v
organization_directory.organizations
```

The relation stores only:

```text
Organization
TenantMembership
Status
RowVersion
Timestamps
```

It deliberately stores no role, policy, capability or permission.

An active OrganizationMembership means that the tenant member belongs to the Organization. Authorization remains delegated to Generic Identity & Access through groups, published managed policies and resource scopes.

## ResourceScope integration boundary

Organization Directory stores only the durable association between an Organization and an Identity Access ResourceScope.

```text
organization_directory.organizations
        │
        v
organization_directory.organization_resource_scope_links
        │
        v
identity_access.resource_scopes
```

The durable link stores:

```text
IdentityScopeId
TenantId
OrganizationId
ApplicationKey
ResourceScopeId
Status
RowVersion
Timestamps
```

It deliberately does **not** duplicate `scope_type` or security-model version. Those values remain owned by Identity Access and are joined/read when the link is materialized.

This avoids stale duplicated security-model metadata if an Identity Access ResourceScope definition changes.

One ResourceScope can map to only one Organization inside the same Identity Scope, Tenant and application. One Organization can have one mapped ResourceScope per application.

The mapping does not imply that Organization hierarchy and ResourceScope hierarchy are identical. Hierarchy synchronization, if ever required, must be an explicit application policy rather than an implicit side effect.

## Administration security boundary

Organization Directory does not introduce a second authorization engine.

All administration requests remain inside the existing Identity Access pipeline:

```text
Authenticated administration request
        ↓
Trusted Administration Context
        ↓
RequireAdministrationCapability
        ↓
Identity-scope authority or tenant managed-policy grants
        ↓
External RBAC
        ↓
Allow / Deny
```

Organization Directory uses dedicated capability features:

```text
organization
organization-membership
organization-scope-link
```

ResourceScope-link mutation is intentionally distinct from Organization definition mutation because it changes the authorization boundary associated with the Organization.

## Semantic audit boundary

Organization Directory domain/application assemblies remain independent from Identity Access audit contracts.

The API host bridges successful security-relevant mutations into the existing audit subsystem:

```text
Organization controller mutation
        ↓
IOrganizationSecurityAuditWriter
        ↓
Identity Access route resolver
        ↓
ISecurityAuditWriter
        ↓
identity_access.security_events
```

Only categorical event type and safe identifiers are recorded. Organization audit events contain no credentials, tokens, connection strings or arbitrary payloads.

## Integrated administration composition

The Organization Directory does not own a separate administration application. Its TypeScript connector is composed under the existing `IdentityAccessAdministrationClient`, and its user interface is embedded in the existing Identity Membership page.

```text
IdentityAccessClient
  -> administration
     -> organizations
     -> organizationMemberships
     -> organizationResourceScopeLinks

Next.js admin
  -> /identity/memberships
     -> tenant members
     -> member Organizations
     -> Organization directory
     -> ResourceScope mapping
```

This keeps authentication, trusted administration context, OIDC session handling, capability filtering, server actions, and error presentation in one existing host. Browser-side visibility remains presentation only; every mutation is re-authorized by `IdentityAccess.Api`.

## Focused administration composition

The existing Identity Membership workspace remains the presentation boundary, but the
implementation is explicitly split by responsibility.

Read composition:

```text
IdentityAccessAdminMembershipOverviewService
    tenant selection
    tenant members
    groups
    group assignments

IdentityAccessAdminOrganizationOverviewService
    Organizations
    OrganizationMembership reads
    selected Organization ResourceScope link
    Organization-specific capability checks
```

Mutation composition:

```text
IdentityAccessAdminMutationService
    existing generic Identity Access mutations

IdentityAccessAdminOrganizationMutationService
    Organization definition + lifecycle

IdentityAccessAdminOrganizationMembershipMutationService
    OrganizationMembership reconciliation

IdentityAccessAdminOrganizationScopeLinkMutationService
    Organization ↔ ResourceScope linkage
```

UI composition:

```text
AdminOrganizationDirectoryPanel
    ├── AdminOrganizationCreateDialog
    ├── AdminOrganizationTable
    │     └── AdminOrganizationRowActions
    └── AdminOrganizationScopeLinkPanel
```

The composition panel contains no mutation actions directly. Organization definition,
membership reconciliation, and ResourceScope linkage cannot accumulate into one god service
or one god component.

These boundaries are protected by executable architecture tests and the
`qualification/verify-separation.ps1` gate.
