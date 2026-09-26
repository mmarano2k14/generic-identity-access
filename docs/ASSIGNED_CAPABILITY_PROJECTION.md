# Assigned Capability Projection

**Source version: 0.62.6. Date: September 27, 2026.**

## Purpose

The assigned-capability projection is the provenance-preserving bridge between stored managed-policy assignments and the external RBAC evaluator for one exact identity-scope, tenant and application boundary.

It deliberately stops before RBAC evaluation.

```text
Subject
  -> active TenantMembership
  -> active UserGroup
  -> ManagedGroupPolicyBinding
  -> active ManagedPolicy
  -> published ManagedPolicyVersion
  -> ManagedPolicyStatement
  -> versioned ApplicationCapability
  -> AssignedCapabilityGrant
  -> external RBAC
```

An `AssignedCapabilityGrant` is evidence of the current stored assignment path. It is **not** an Allow/Deny decision, a credential, a TRN, or proof that a resource instance belongs to the requested scope.

## Managed-only contract

`AssignedCapabilityGrant` requires one concrete `ManagedPolicyVersionReference`. Tenant-owned `PermissionPolicyReference` provenance is no longer accepted by the authorization application contract.

`IAssignedCapabilityReader.ListAsync` receives:

- the immutable `ResolvedDatabaseRoute` for the operation;
- one `TenantReference`;
- one `SubjectReference`;
- one `ApplicationKey`;
- an optional exact `ResourceScopeReference`;
- an explicit `CancellationToken`.

The result preserves:

- subject;
- tenant;
- application;
- source group;
- managed policy id and pinned policy version;
- statement id;
- security-model version;
- structured capability pattern;
- optional resource-scope provenance and descendant semantics.

Duplicate capability patterns are intentionally not collapsed when their provenance differs. The authorization bridge may need the complete origin chain for explanation and revocation while the external RBAC evaluator remains authoritative for the final decision.

## PostgreSQL projection

`PostgreSqlAssignedCapabilityReader` performs one operation-scoped query through the already resolved destination. It does not resolve another route and cannot switch databases during the read.

The active authorization query reads only:

```text
users
 tenants
 tenant_memberships
 group_memberships
 user_groups
 managed_group_policy_bindings
 managed_policies
 managed_policy_versions
 managed_policy_statements
 resource_scopes (when scoped evaluation is requested)
```

The query requires active user, tenant, membership, group and managed-policy state, and requires `managed_policy_versions.published_at IS NOT NULL`.

Historical `permission_policies`, `policy_statements`, and `group_policy_bindings` are not read by this projection. Their schema remains in migration history, but rows in those tables cannot create an authorization grant.

## Resource-scope behavior

A tenant-wide managed binding applies when no resource scope is attached. For a scoped authorization request:

- tenant-wide bindings remain applicable;
- an exact resource-scope binding applies to the exact target;
- an ancestor binding applies only when `IncludeDescendants` is true;
- inactive or foreign scopes do not become grants.

Wildcard capability evaluation remains outside the projection and inside the external RBAC boundary.

## Concurrency independence

The reader has no process-global mutable state. Every call receives its own immutable route snapshot, opens its own logical database connection, uses only operation parameters and returns a new result list. Npgsql may pool physical connections, but no subject, tenant, application or route state is stored on a shared connection object by this component.

## Historical schema

Previously published migrations are not rewritten or deleted during compatibility closure. In particular, the original permission-policy and assignment-projection migrations remain part of the database history. Closing compatibility changes active application/runtime composition, not historical migration identity.

## Validation

`scripts/postgresql/validate-assigned-capability-projection.sql` now exercises the managed-only query shape in one rolled-back transaction and proves:

1. tenant-wide bindings apply without a resource target;
2. an exact scoped binding applies only to its exact target;
3. an ancestor binding with descendant expansion applies to both children while a sibling-only exact binding does not leak;
4. suspended managed policies and suspended groups do not contribute grants;
5. after all managed bindings are removed, intentionally preserved active legacy policy/binding rows produce **zero** authorization grants for tenant-wide and scoped targets.

Run the live PostgreSQL proof with:

```powershell
.\scripts\postgresql\verify-assigned-capability-projection.ps1
```

The repository source gate `verify-managed-policy-compatibility-closure-source-consistency.ps1` additionally rejects reintroduction of the legacy SQL fallback or legacy grant provenance.
