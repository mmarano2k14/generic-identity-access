# Access Control — Access Control SDK/UI

**Status:** categorized Generic Identity Access Control public surface  
**Package version:** `1.3.0`

## Scope

Access Control promotes the active Access Control surfaces into the categorized Generic Identity SDK without changing RBAC semantics or backend persistence.

Public category:

```text
GenericIdentityClient.accessControl
├── groups
├── managedPolicies
├── managedPolicyBindings
├── resourceScopes
├── delegatedAuthority
└── authorization
```

The existing root `authorization` and read-focused `administration` surfaces remain available for compatibility.

## Managed-policy compatibility closure

The historical tenant-policy model is retired compatibility only.

The following legacy surfaces are deliberately **not** reintroduced:

```text
IdentityAccessAdministrationClient.policies
GenericIdentityAccessControlClient.tenantPolicies
legacy policy statements/bindings in the categorized public SDK
active TenantPoliciesPage / TenantPolicyDetailsPage exports
```

The historical source files remain present where required for compatibility/history, but they are not composed into the active administration client and are not exported by the categorized Access Control SDK/UI.

Active authorization state uses:

```text
managed policies
immutable/published managed-policy versions
managed policy statements
managed group policy bindings
resource scopes
```

## Groups

The public category exposes group lifecycle, templates and membership operations.

## Managed policies

The public category exposes the full versioned catalog lifecycle:

```text
policy CRUD
version list/get/create
publication
statement list/add/remove
tenant group bindings
```

## Resource scopes

The categorized facade exposes the proven ResourceScope CRUD hierarchy. Resource scopes narrow authorization grants; they do not grant access by themselves.

## Delegated authority

Identity-scope administration authority is exposed through the proven groups/policies/members/bindings lifecycle. No consumer-specific bypass or administrator shortcut is introduced.

## Missing backend capabilities

Access Control deliberately does not synthesize:

```text
effective permission listing
permission explanation / grant provenance
```

They remain `BACKEND_MISSING` until a real backend contract exists.

## Shared UI

Access Control provides generic forms/pages for:

```text
group lifecycle and membership
managed policies and versions
managed policy bindings
resource scopes
delegated authority
```

Routes and server actions remain consumer-owned.

## Compatibility

No active public surface is removed. Historical tenant-policy files remain as inert compatibility tombstones rather than being deleted.
