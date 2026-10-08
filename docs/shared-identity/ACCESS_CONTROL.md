# Generic Identity — Access Control

**Current package family:** `1.5.0`

## Scope

The categorized Access Control surface is:

```text
GenericIdentityClient.accessControl
├── groups
├── managedPolicies
├── managedPolicyBindings
├── resourceScopes
├── delegatedAuthority
└── authorization
```

## Tenant authorization

Tenant authorization uses Groups, Managed Policies, bindings, statements, and ResourceScopes.

Reusable workflows cover:

```text
group lifecycle and templates
group members
managed-policy binding
managed-policy lifecycle and versioning
statement administration
publication/default version
resource-scope hierarchy and lifecycle
```

Managed Policy statements are selected from the registered application-security capability catalog. The administration UI does not replace that catalog with unrestricted free-text capability coordinates.

## Delegated Authority

Delegated Authority is the identity-scope administration RBAC model:

```text
Scope Authority Group
  -> Member
  -> Policy Binding
  -> Scope Authority Policy
  -> Statement
```

It is intentionally separate from tenant Groups and Managed Policies.

A Super Administrator belongs here. Scope authority objects are not implicitly merged into tenant authorization catalogs.

Delegated-authority statements remain typed administrative coordinates and may use supported wildcard patterns. They are not converted into the Managed Policy capability selector because the authorization boundary is different.

## Resource scopes

ResourceScopes narrow grants and preserve hierarchy. Scope type selection is grounded in the registered Application Security model. Parent selection uses bounded server-backed lookup and enforces same-tenant hierarchy constraints.

## Retired compatibility

The historical tenant-policy administration model remains retired compatibility only. Active authorization administration uses Managed Policies.

## Missing backend capabilities

The SDK deliberately does not synthesize:

```text
effective permission listing
permission explanation / grant provenance
```

Authorization evaluation itself remains fully supported.
