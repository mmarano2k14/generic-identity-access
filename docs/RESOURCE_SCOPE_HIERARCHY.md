# Generic Resource Scope Hierarchy

**Source version: 0.15.0. Date: September 22, 2026.**

Version 0.15.0 introduces application-defined resource-scope types, tenant-linked resource hierarchies,
and scope-aware policy bindings.

## Core rule

`Tenant` remains the identity/account boundary. A consuming application may attach one or more generic root
resource scopes to that tenant and define a hierarchy below them.

The identity core does not contain hard-coded business types such as organization, business, department,
project, site or environment. Those are application-defined scope type keys.

Example application manifest shape:

```text
organization  (root, attaches to tenant)
    |
    +-- business
```

Another application may declare:

```text
organization
    |
    +-- department
            |
            +-- project
```

The engine remains unchanged.

## Versioned scope types

Scope types belong to an `ApplicationSecurityModelReference` version. A type declares:

- key;
- display name;
- optional parent type;
- whether it can attach directly to the tenant.

Root types must attach to the tenant. Child types must name an existing parent type from the same model.
Type definitions are immutable in this version.

## Resource scopes

A resource scope is identified by:

```text
IdentityScopeId
TenantId
ApplicationKey
ResourceScopeId
```

It additionally records the model version, type key, external application resource id, display name,
optional parent resource-scope id, status and optimistic row version.

A root resource scope is linked to the tenant by its own `TenantId`; no separate business-specific link table
is required.

## Scoped policy bindings

A group-to-policy binding can now target:

- `null`: tenant-wide;
- one exact resource scope;
- one resource scope with `IncludeDescendants = true`.

The same group and policy may therefore be bound independently at multiple resource targets.

Tenant-wide bindings continue to apply without a resource target and preserve the behavior of bindings created
before this migration.

## Authorization projection

The assignment reader filters scope applicability before materializing TRNs:

- tenant-wide bindings apply to every resource target inside the tenant;
- exact bindings apply only to their exact target;
- ancestor bindings apply to descendants only when `IncludeDescendants` is true;
- inactive or missing requested scopes produce no scoped grants;
- RBAC wildcard semantics are still evaluated only by the external RBAC engine.

Resource scope identifiers are not inserted into the external TRN format. They are a separate authorization
boundary used to select which grants are eligible before the external engine evaluates capability wildcards.

## Concurrency and isolation

Resource scopes use the existing `row_version` optimistic-concurrency contract. Each operation resolves and
retains one database route. No current resource scope is stored in global mutable state.

## API

Swagger exposes MVC controllers for:

```text
scope type definitions
resource scopes
scoped group-policy bindings
```

All operations remain behind fail-closed administration authorization.
