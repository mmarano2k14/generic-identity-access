# Generic Organization Directory — Architecture

## Responsibility

Generic Organization Directory owns stable organization identity, hierarchy, lifecycle, explicit organization membership and optional application-aware links to external authorization resource scopes.

It does not own authentication, tenant membership, groups, policies, wildcard evaluation, business profiles, domains, providers or application workflows.

## Core model

```text
Tenant (external reference)
  ↓
Organization
  ↓
OrganizationMembership
  ↓
TenantMembership (external reference)
```

Authorization remains separate:

```text
TenantMembership
  ↓
GroupMembership
  ↓
Managed Policy
  ↓
Resource Scope
  ↓
External RBAC
```

An `OrganizationMembership` expresses belonging only. It must never be interpreted as a permission grant.

## Organization hierarchy

Every organization is scoped by `IdentityScopeId + TenantId + OrganizationId`. Parent references must remain in the same identity scope and tenant. The persistence layer will additionally prevent deep hierarchy cycles.

## External resource-scope linkage

Resource-scope linkage is modeled separately from Organization so the directory can remain usable without Generic Identity & Access and so one Organization can participate in multiple application security models.

```text
Organization
  ↓
OrganizationResourceScopeLink
  ↓
ApplicationKey + ResourceScopeId + ScopeType + ModelVersion
```

## Concurrency

Mutable state carries an explicit non-negative `RowVersion`. Persistence introduced later must reject stale writes rather than silently overwrite concurrent changes.

## Source layout

C# source uses block-scoped namespaces and one declared top-level type per source file. The filename must match the declared type.


## Durable Organization persistence

PostgreSQL persistence is owned by `OrganizationDirectory.Infrastructure.PostgreSql`. The application layer depends only on `IOrganizationStore`; Npgsql types never leak into Domain, Contracts or Application.

The database enforces tenant-local hierarchy identity and deep cycle rejection independently from application validation. Optimistic concurrency is explicit through `row_version`.
