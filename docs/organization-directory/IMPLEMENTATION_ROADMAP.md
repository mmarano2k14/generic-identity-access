# Generic Organization Directory — Architecture and Implementation Roadmap

**Version:** 1.0  
**Status:** Architecture baseline and implementation plan  
**Repository location:** integrated module inside `generic-identity-access`  
**Primary stack:** .NET 10 / ASP.NET Core / PostgreSQL / TypeScript  
**Purpose:** Provide a reusable, application-agnostic organization directory that can model hierarchical organizations and explicit organization membership while integrating cleanly with Generic Identity & Access for authentication and authorization.

---

## 1. Objective

The project introduces a generic organizational layer that is intentionally separate from both:

- **Generic Identity & Access**, which owns identities, tenant membership, groups, managed policies, resource scopes, authentication and RBAC;
- **consumer applications**, which own domain-specific concepts such as business profiles, business domains, providers, operational entities and automation.

The new module answers:

```text
What organization is this?
Which tenant owns it?
Where does it sit in the organization hierarchy?
Which tenant members belong to it?
Which Identity Access resource scope represents it for authorization?
```

It does **not** decide what a user is allowed to do.

---

## 2. Core Architectural Separation

```text
GENERIC IDENTITY & ACCESS
-------------------------
IdentityScope
User
Tenant
TenantMembership
UserGroup
GroupMembership
ManagedPolicy
Capability
ResourceScope
Authentication
RBAC


GENERIC ORGANIZATION DIRECTORY
------------------------------
Organization
Organization hierarchy
OrganizationMembership
Organization <-> ResourceScope linkage
Organization lifecycle


CONSUMER APPLICATION
--------------------
BusinessProfile
Domains
Providers
Business entities
Application-specific semantics
Application workflows
```

Central rule:

> **Organization membership establishes belonging. Authorization remains owned by Generic Identity & Access.**

Therefore:

```text
OrganizationMembership
!=
Permission grant
```

and:

```text
GroupMembership + ManagedPolicy + ResourceScope
=
authorization
```

---

## 3. Example

For a tenant named `URBAN GROUP`:

```text
Tenant: URBAN GROUP
│
└── Organization: Urban Group
    │
    ├── Organization: Urban Flower
    │   └── organization_type = business
    │
    └── Organization: Urban Cafe
        └── organization_type = business
```

A consumer application may later attach application semantics:

```text
Urban Flower
├── BusinessProfile = ecommerce
├── Domains
│   ├── Commerce
│   ├── Inventory
│   ├── Procurement
│   └── Finance
└── Providers
    ├── Shopify
    ├── Stripe
    └── Xero
```

Those concepts do **not** belong in Generic Organization Directory.

---

## 4. Organization Membership

The current Identity & Access model already distinguishes:

```text
User
   ↓
TenantMembership
   ↓
Tenant
```

and:

```text
TenantMembership
   ↓
GroupMembership
   ↓
UserGroup
```

Generic Organization Directory adds a separate relation:

```text
TenantMembership
   ↓
OrganizationMembership
   ↓
Organization
```

This means the organizational model does not attach a raw `UserId` directly to an organization.

Target contract:

```text
OrganizationMembership
{
    IdentityScopeId
    TenantId
    OrganizationId
    TenantMembershipId
    Status
    RowVersion
    CreatedAt
    UpdatedAt
}
```

### Important distinction

```text
Marco belongs to Urban Flower
```

is represented by:

```text
OrganizationMembership
```

while:

```text
Marco can read invoices for Urban Flower
```

is represented through Identity Access:

```text
TenantMembership
    ↓
GroupMembership
    ↓
UserGroup
    ↓
Managed Policy Binding
    ↓
Resource Scope = Urban Flower
    ↓
Capability = finance / invoice / read
```

---

## 5. Organization Model

Proposed domain model:

```text
Organization
{
    IdentityScopeId
    TenantId
    OrganizationId

    OrganizationKey
    DisplayName
    OrganizationType

    ParentOrganizationId?

    Status
    RowVersion

    CreatedAt
    UpdatedAt
}
```

### Identity

Canonical organization identity:

```text
IdentityScopeId
+
TenantId
+
OrganizationId
```

Human-readable stable key:

```text
OrganizationKey
```

Examples:

```text
urban-group
urban-flower
urban-cafe
factory-bangkok
europe-division
```

### Organization Type

`OrganizationType` is application-defined metadata.

Examples:

```text
organization
business
division
department
branch
site
brand
legal-entity
facility
```

The core does not attach business semantics to these values.

---

## 6. PostgreSQL Schema

Proposed owned schema:

```text
organization_directory
```

### 6.1 `organizations`

```sql
organization_directory.organizations
```

Candidate columns:

```text
identity_scope_id        uuid
tenant_id                uuid
organization_id          uuid
organization_key         text
display_name             text
organization_type        text
parent_organization_id   uuid NULL
status                    smallint
row_version               bigint
created_at                timestamptz
updated_at                timestamptz
```

Primary key:

```text
(identity_scope_id, tenant_id, organization_id)
```

Unique key:

```text
(identity_scope_id, tenant_id, organization_key)
```

Hierarchy FK is tenant-local:

```text
(identity_scope_id, tenant_id, parent_organization_id)
    -> organizations
```

No parent may cross a tenant boundary.

### 6.2 `organization_memberships`

```sql
organization_directory.organization_memberships
```

Candidate columns:

```text
identity_scope_id       uuid
tenant_id               uuid
organization_id         uuid
tenant_membership_id    uuid
status                   smallint
row_version              bigint
created_at               timestamptz
updated_at               timestamptz
```

Primary key:

```text
(identity_scope_id, tenant_id, organization_id, tenant_membership_id)
```

The module must validate that the referenced tenant membership belongs to the same Identity Scope and Tenant.

If Identity Access and Organization Directory are stored in separate PostgreSQL databases, this validation is performed through the integration contract rather than a cross-database FK.

### 6.3 `organization_resource_scope_links`

Resource-scope linkage should not be stored as a single `resource_scope_id` directly on `organizations`.

An organization may be consumed by more than one application/security model, so linkage is application-aware.

```sql
organization_directory.organization_resource_scope_links
```

Candidate columns:

```text
identity_scope_id      uuid
tenant_id              uuid
organization_id        uuid
application_key        text
resource_scope_id      uuid
scope_type             text
model_version          integer
status                  smallint
row_version             bigint
created_at              timestamptz
updated_at              timestamptz
```

Unique organization/application mapping:

```text
(identity_scope_id, tenant_id, organization_id, application_key)
```

Unique mapped scope:

```text
(identity_scope_id, tenant_id, application_key, resource_scope_id)
```

This allows:

```text
Organization: Urban Flower

admin-web
    -> ResourceScope A

consumer-app
    -> ResourceScope B
```

if the applications intentionally use separate security models.

---

## 7. Core Invariants

### Identity and tenancy

1. An Organization belongs to exactly one Tenant.
2. An Organization parent must belong to the same Identity Scope and Tenant.
3. Organization keys are unique within a Tenant.
4. Physical database placement is never part of public organization identity.

### Hierarchy

5. Parent/child cycles are forbidden.
6. An Organization cannot become its own ancestor.
7. Deleting an Organization with children is blocked unless an explicit controlled lifecycle operation exists.
8. No blind cascading delete across organization membership or authorization linkage.

### Membership

9. OrganizationMembership references a valid TenantMembership.
10. Tenant membership and organization membership remain separate lifecycle states.
11. Removing OrganizationMembership does not delete the User or TenantMembership.
12. Adding OrganizationMembership does not grant permissions.

### Authorization integration

13. ResourceScope linkage is explicit.
14. A linked ResourceScope must belong to the same Identity Scope, Tenant and expected application context.
15. Organization hierarchy does **not** automatically imply ResourceScope hierarchy.
16. Authorization inheritance exists only when explicitly represented and supported by Identity Access.
17. Generic Organization Directory never evaluates wildcard permissions itself.

### Concurrency

18. Mutable records use optimistic concurrency.
19. Stale updates are rejected.
20. Membership mutation and resource-scope-link mutation are auditable.

---

## 8. Resource Scope Integration

Generic Organization Directory and Generic Identity & Access are independent modules.

The organization module may run without Identity Access linkage.

When integration is enabled:

```text
Organization
      │
      └── OrganizationResourceScopeLink
                    │
                    v
        Identity Access ResourceScope
```

Example:

```text
Tenant: Urban Group

Organization:
Urban Flower

OrganizationResourceScopeLink:
application_key = consumer-app
scope_type      = organization
resource_scope  = Urban Flower
```

Authorization remains:

```text
User
 ↓
TenantMembership
 ↓
GroupMembership
 ↓
ManagedPolicy
 ↓
Capability
 ↓
ResourceScope: Urban Flower
 ↓
ALLOW / DENY
```

Organization membership may be used by the consumer application to decide which organizations to present to a user, but it must never replace an authorization decision.

---

## 9. API Surface

Suggested API base:

```text
/api/v1/identity-scopes/{identityScopeId}/tenants/{tenantId}
```

### Organizations

```text
GET    /organizations
POST   /organizations

GET    /organizations/{organizationId}
PATCH  /organizations/{organizationId}

GET    /organizations/tree
GET    /organizations/{organizationId}/children
```

Lifecycle operations should be explicit rather than destructive by default:

```text
POST /organizations/{organizationId}/disable
POST /organizations/{organizationId}/enable
```

### Organization memberships

```text
GET    /organizations/{organizationId}/memberships
POST   /organizations/{organizationId}/memberships
DELETE /organizations/{organizationId}/memberships/{tenantMembershipId}
```

Optional member-centric read:

```text
GET /tenant-memberships/{tenantMembershipId}/organizations
```

### Resource-scope links

```text
GET    /organizations/{organizationId}/resource-scopes
POST   /organizations/{organizationId}/resource-scopes
DELETE /organizations/{organizationId}/resource-scopes/{applicationKey}
```

---

## 10. Integration Contract with Generic Identity & Access

The new repository must not access Identity Access PostgreSQL tables directly.

Use a public integration abstraction such as:

```csharp
public interface IIdentityDirectoryReferenceService
{
    Task<TenantMembershipReference?> GetTenantMembershipAsync(...);
    Task<bool> TenantExistsAsync(...);
}
```

and:

```csharp
public interface IResourceScopeReferenceService
{
    Task<ResourceScopeReference?> GetResourceScopeAsync(...);
}
```

Production adapters may call Generic Identity & Access over its supported API/client.

Test doubles remain local to this project.

### Failure behavior

```text
Identity Access says membership does not exist
    -> reject OrganizationMembership

Identity Access unavailable
    -> technical failure
    -> do not silently create membership

ResourceScope incompatible
    -> reject link

Authorization service unavailable
    -> fail closed
```

---

## 11. Administration Model

A future administration UI should show an organization tree:

```text
URBAN GROUP
│
├── Urban Flower
│   ├── Members
│   ├── Access scopes
│   └── Children
│
└── Urban Cafe
    ├── Members
    ├── Access scopes
    └── Children
```

For an Organization:

```text
Overview
Members
Access
Children
Audit
```

### Members

```text
Organization members

Marco Marano
Alice Example
...

[ Add tenant member ]
```

The picker lists valid TenantMemberships rather than arbitrary global Users.

### Access

This section displays ResourceScope linkage only.

It does not duplicate Managed Policy administration.

Example:

```text
Application     Scope Type      Resource Scope
------------------------------------------------
consumer-app        organization    Urban Flower
```

Policies and groups continue to be managed by Identity Access.

---

## 12. Consumer Model Example

The consumer owns domain semantics.

Example:

```text
Organization:
Urban Flower

Consumer application:
BusinessProfile = ecommerce

Domains:
Commerce
Inventory
Procurement
Finance
Customers
Growth
Logistics
Payments
```

Another:

```text
Organization:
Urban Cafe

Consumer application:
BusinessProfile = restaurant

Domains:
Hospitality
Inventory
Procurement
Finance
Customers
Payments
Workforce
Scheduling
```

Generic Organization Directory sees only:

```text
Urban Flower
Urban Cafe
```

and their organizational relationships.

---

## 13. Suggested Repository Structure

```text
generic-organization-directory/
│
├── src/
│   ├── OrganizationDirectory.Api/
│   ├── OrganizationDirectory.Application/
│   ├── OrganizationDirectory.Contracts/
│   ├── OrganizationDirectory.Domain/
│   ├── OrganizationDirectory.Infrastructure.PostgreSql/
│   └── OrganizationDirectory.Integration.IdentityAccess/
│
├── tests/
│   ├── OrganizationDirectory.Tests/
│   └── OrganizationDirectory.IntegrationTests/
│
├── clients/
│   └── typescript/
│
├── examples/
│   └── nextjs/
│
├── scripts/
│   ├── postgresql/
│   └── verify.ps1
│
├── docs/
│   ├── ARCHITECTURE.md
│   ├── POSTGRESQL_SCHEMA_AND_CONCURRENCY.md
│   ├── IDENTITY_ACCESS_INTEGRATION.md
│   └── VALIDATION.md
│
├── Directory.Build.props
├── Directory.Packages.props
├── OrganizationDirectory.sln
├── README.md
└── CHANGELOG.md
```

C# source should retain the same quality rule already used by Generic Identity & Access:

```text
one declared top-level type per source file
file name == declared type name
block-scoped namespace
```

---

## 14. Technology Baseline

Recommended initial baseline:

```text
.NET 10
ASP.NET Core
PostgreSQL
Npgsql
xUnit
TypeScript client
Next.js administration example
```

Initial PostgreSQL target:

```text
generic_identity_access_default
```

Owned schema:

```text
organization_directory
```

The first implementation may use one default development destination while keeping persistence contracts independent enough to support configurable placement later.

Do not introduce distributed transactions between Organization Directory and Identity Access.

---

# 15. Implementation Plan

The work is split into bounded implementation milestones. Estimates represent focused engineering effort and are not delivery guarantees.

| Milestone | Scope | Estimate | Exit criterion |
|---|---|---:|---|
| **1 — Foundation and contracts** | Repository, solution, core IDs, Organization domain, status, contracts, source-layout gates | **6–8 h** | Build/tests GREEN; core model frozen |
| **2 — PostgreSQL persistence** | Schema metadata, `organizations`, migrations, store, optimistic concurrency, hierarchy constraints | **8–12 h** | CRUD persistence + migration integrity GREEN |
| **3 — Organization API and hierarchy** | Controllers/services, create/read/update/disable, tree reads, cycle protection | **8–10 h** | Organization lifecycle and hierarchy tests GREEN |
| **4 — OrganizationMembership** | Membership table/domain/service/API, TenantMembership-reference validation, member-centric reads | **8–12 h** | Explicit organization membership GREEN |
| **5 — Identity Access integration** | Public adapter contracts, TenantMembership lookup, ResourceScope linkage, compatibility validation | **10–14 h** | Real IA integration and failure-mode tests GREEN |
| **6 — Security and administration authorization** | Administration capabilities, fail-closed server authorization, audit events | **8–12 h** | Unauthorized/forbidden/technical-failure matrix GREEN |
| **7 — TypeScript client + Next.js administration** | Typed client, tree UI, organization members, access-scope linkage | **10–14 h** | Browser administration scenarios GREEN |
| **8 — Qualification and documentation** | Full verify gate, PostgreSQL gates, backup/restore, concurrency/adversarial tests, README/docs | **8–12 h** | Release-candidate qualification GREEN |

Estimated total focused engineering effort:

```text
66–94 hours
```

Expected size:

```text
8 implementation stages
```

The project should remain useful after Organization Membership even without Identity Access integration:

```text
Organization hierarchy
+
OrganizationMembership
```

Identity Access Resource Scope Integration adds governed linkage to Generic Identity & Access.

---

# 16. Foundation — Start Here

## Objective

Create the repository and freeze the minimal public domain contracts before persistence or UI work begins.

### Deliverables

```text
OrganizationDirectory.Domain
OrganizationDirectory.Contracts
OrganizationDirectory.Application
OrganizationDirectory.Api
OrganizationDirectory.Infrastructure.PostgreSql
OrganizationDirectory.Tests
```

### Core types

```text
OrganizationId
OrganizationKey
OrganizationStatus
Organization
OrganizationMembershipStatus
OrganizationResourceScopeLink
```

### First domain tests

1. Organization key cannot be empty.
2. Display name cannot be empty.
3. Organization cannot parent itself.
4. Parent identity is tenant-local.
5. Status transitions are explicit.
6. RowVersion cannot be negative.
7. OrganizationMembership cannot cross Tenant identity.
8. Organization membership grants no authorization semantics.
9. Resource-scope linkage is application-aware.
10. Domain model contains no consumer application, ecommerce, restaurant, Finance or provider-specific types.

### Source-layout gate

Enforce:

```text
one top-level declared type per .cs file
file name == declared type name
block-scoped namespace
```

### Foundation exit gate

```text
dotnet restore
dotnet build -c Release
dotnet test -c Release
```

all GREEN.

---

# 17. Validation Matrix

| Area | Scenario | Expected result |
|---|---|---|
| Tenant isolation | Parent Organization belongs to another Tenant | Rejected |
| Hierarchy | Organization becomes its own parent | Rejected |
| Hierarchy | Deep cycle A → B → C → A | Rejected |
| Keys | Duplicate OrganizationKey in same Tenant | Rejected |
| Keys | Same OrganizationKey in different Tenant | Allowed |
| Membership | Valid TenantMembership joins Organization | Accepted |
| Membership | TenantMembership from another Tenant | Rejected |
| Membership | Duplicate active membership | Idempotent/rejected by contract |
| Membership | Remove organization membership | User/TenantMembership preserved |
| Authorization | Add OrganizationMembership | No permission granted automatically |
| Scope linkage | Matching Tenant/application ResourceScope | Accepted |
| Scope linkage | ResourceScope from another Tenant | Rejected |
| Scope linkage | Wrong application | Rejected |
| Scope linkage | Identity Access unavailable | Technical failure, no silent success |
| Concurrency | Stale update | Rejected |
| Delete/lifecycle | Disable Organization | Children/members retained according to explicit lifecycle |
| Audit | Membership/link mutation | Auditable |
| Backup/restore | Restore qualification DB | Schema and durable state preserved |

---

# 18. Explicitly Out of Scope for Initial Version

Do not introduce:

```text
BusinessProfile
Commerce
Finance
Inventory
Hospitality
Providers
BusinessEntity
Evidence
Facts
Findings
Agents
Workflows
AI reasoning
RBAC wildcard evaluation
password authentication
OIDC
MFA
```

Authentication and authorization remain external responsibilities of Generic Identity & Access.

Consumer-specific semantics remain external responsibilities of the consuming application.

---

# 19. Definition of Done

The initial project is complete when:

```text
Organization identity                    GREEN
Organization hierarchy                   GREEN
Tenant isolation                         GREEN
Organization lifecycle                   GREEN
OrganizationMembership                   GREEN
Identity Access membership validation    GREEN
Application-aware ResourceScope linkage  GREEN
Optimistic concurrency                   GREEN
Administration authorization             GREEN
TypeScript client                        GREEN
Administration UI                        GREEN
PostgreSQL migration integrity           GREEN
Backup / restore                         GREEN
Documentation                            GREEN
```

Final invariant:

> **A Tenant owns Organizations. TenantMembership establishes identity within the Tenant. OrganizationMembership establishes organizational belonging. Groups, Managed Policies and Resource Scopes establish authorization. Consumer applications provide the business meaning of each Organization.**
