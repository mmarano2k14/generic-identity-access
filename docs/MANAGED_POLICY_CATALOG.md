# Managed Policy Catalog

**Introduced in repository version 0.62.0.**

## Purpose

Managed policies are reusable permission definitions owned by an identity scope and application security model, not by a tenant. They are intended to provide a shared catalog comparable to a managed-policy library: one definition can later be bound independently into many tenant-scoped authorization contexts without cloning the policy definition for every tenant.

The ownership split is:

```text
Application security model
    -> managed policy catalog
        -> managed policy
            -> managed policy versions
                -> capability statements

Tenant
    -> groups
    -> resource scopes
    -> policy bindings
```

A tenant never owns a managed-policy definition. Tenant isolation remains the responsibility of the binding, group, membership, resource scope, request context, and RBAC evaluation paths.

## Stable Identity

A managed policy is identified by:

```text
IdentityScopeId
ApplicationKey
PolicyId
```

It also has a stable `ManagedPolicyKey` such as:

```text
iam-read-only
storage-read-only
billing-administrator
```

`ManagedPolicyReference` intentionally contains no `TenantId` or `TenantReference`.

## Versions

Managed policy definitions are versioned independently from optimistic-concurrency `row_version` metadata.

Each managed-policy version is pinned to one immutable `ApplicationSecurityModelReference`:

```text
ManagedPolicy
    -> PolicyVersion 1 -> ApplicationSecurityModel version N
    -> PolicyVersion 2 -> ApplicationSecurityModel version N+1
```

The policy metadata may select one positive `DefaultVersion`. A new policy is created without a default version because the referenced version must exist before it can become the default.

Statements are stored against a concrete managed-policy version and concrete security-model version. Capability foreign keys therefore continue to prevent statements from referring to capabilities absent from the registered application security model. A version starts as a draft. Publishing records `PublishedAt`, freezes the version identity/model and its statements, and makes that concrete version eligible for tenant bindings and authorization projection. A policy `DefaultVersion` may reference only a published version.

## PostgreSQL

Migration `0023_managed_policy_catalog.sql` adds:

```text
managed_policies
managed_policy_versions
managed_policy_statements
```

None of these tables contains `tenant_id`.

Historical tenant-scoped `permission_policies`, `policy_statements`, and `group_policy_bindings` remain in the previously published schema, but repository version `0.62.6` removes them from active authorization and administration composition. Runtime grant projection now reads only published managed-policy versions. Retaining the old tables does not make their rows effective grants.

## Security Boundary

Creating a managed policy, draft version, statement, or publishing a version does not grant any tenant permission.

A tenant binding must establish a concrete tenant boundary and pass the existing authorization pipeline. Bindings may reference only published managed-policy versions. `All authorized tenants` remains a collection/navigation context only and is never policy ownership.

## Tenant-Scoped Managed Policy Bindings

Repository version `0.62.1` adds the parallel managed-policy binding path without switching the existing administration UI yet. A managed binding combines:

```text
Tenant-scoped UserGroup
    +
Shared ManagedPolicyVersion
    +
Optional tenant-scoped ResourceScope
```

The managed policy foreign key is intentionally:

```text
IdentityScopeId / ApplicationKey / PolicyId / PolicyVersion
```

and contains no tenant dimension. The binding itself retains `TenantId` because the grant is tenant-scoped. Group and resource-scope foreign keys continue to include that tenant.

Migration `0024_managed_policy_bindings.sql` adds `managed_group_policy_bindings` as a parallel tenant-scoped path. Migration `0025_managed_policy_publication.sql` adds explicit publication state and database guards that freeze published versions/statements, prevent unpublished versions from becoming defaults, and reject bindings to unpublished versions. During migration, a version already selected as a default or referenced by an existing managed binding is marked published at its original creation timestamp so previously expressed active intent is preserved before the guards are installed.

Managed binding creation validates atomically that the group and optional resource scope are active in the requested tenant, the referenced managed policy is active, and the pinned managed-policy version is published. Repository version `0.62.2` introduced parallel runtime projection; repository version `0.62.6` closes the former legacy source so published managed bindings are now the only tenant policy path projected into runtime authorization.



## Administration API

Repository version `0.62.3` exposes the shared managed-policy catalog through an identity-scope/application route:

```text
/api/v1/identity-scopes/{identityScopeId}/applications/{applicationKey}/managed-policies
```

The route deliberately contains no tenant identifier. Managing shared policy definitions requires identity-scope administration authority for the existing policy/policy-statement capabilities; tenant-scoped capability grants do not become authority to mutate the shared catalog.

The API supports bounded policy discovery, metadata create/update, draft version creation, statement add/remove while a version remains unpublished, and explicit publication. Publication may select the published version as the policy default. Published version content remains immutable under the PostgreSQL guards introduced earlier.

The public TypeScript client exposes the same surface through `administration.managedPolicies` using `IdentityAdministrationContext`, never `IdentityTenantAdministrationContext`. This administration surface changes policy definitions only; tenant permissions still require a separate tenant-scoped managed binding.

## Tenant Binding Administration

Repository version `0.62.4` exposes tenant-scoped managed-policy binding administration without moving policy ownership into the tenant. The binding route is:

```text
/api/v1/identity-scopes/{identityScopeId}/tenants/{tenantId}/applications/{applicationKey}/managed-policy-bindings
```

`available-policies` is a tenant-authorized consumption view over the shared catalog. Its PostgreSQL query returns only active managed policies whose `DefaultVersion` is published, applies search filtering before paging, and never creates tenant-owned copies of those policies. Creating a binding pins the explicit requested version or, when omitted, the policy's published default version.

The group administration UI creates, lists and removes grants only through the shared managed-policy binding path. Historical tenant-owned policy bindings are no longer surfaced by the administration UI and do not participate in runtime authorization. Resource scopes remain tenant-scoped and are validated against the binding group's tenant.


## Shared Administration Workspace

Repository version `0.62.5` moves the Policies administration workspace onto the managed-policy catalog. The workspace is identity-scope/application scoped and intentionally has no tenant selector or `All authorized tenants` mode because a tenant never owns a managed-policy definition.

The UI supports the complete managed-policy lifecycle:

```text
Managed policy metadata
    -> create draft version pinned to one registered security-model version
        -> add/remove exact catalog capabilities while draft
            -> publish version
                -> immutable published content
                -> optionally select as DefaultVersion
```

Published versions may be selected as the default for future tenant bindings, but changing the default never rewrites existing bindings because every managed group-policy binding stores one concrete `PolicyId + PolicyVersion`. Tenant administrators consume the shared catalog through the tenant-authorized managed-binding surface; they do not gain authority to mutate shared definitions.

Repository version `0.62.6` completes compatibility cleanup. The Policies workspace, Groups workspace, public TypeScript composition, API dependency injection and assigned-capability projection are managed-policy only. Historical permission-policy source files and database migrations remain for compatibility with previously constructed schemas, but are dormant.
