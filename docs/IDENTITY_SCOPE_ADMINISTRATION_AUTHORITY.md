# Identity-Scope Administration Authority

Identity-scope administration uses a dedicated authorization boundary rather than
borrowing authority from any tenant.

## Why a Separate Boundary Exists

Some operations belong to the identity scope itself:

```text
user administration
tenant creation
credential administration
scope-type administration
bulk session administration
```

They do not have a tenant target.

Using an arbitrary tenant membership to authorize these operations would allow tenant
authority to escape its boundary.

Identity Access therefore maintains two explicit authorization paths:

```text
tenant/resource operation
    -> tenant groups/policies
    -> IdentityAuthorizationService

identity-scope operation
    -> identity-scope administration groups/policies
    -> IdentityScopeAuthorizationService
```

Both paths delegate final wildcard evaluation to the same external RBAC engine.

## Persistence Model

Migration `0010_identity_scope_administration_authority.sql` adds:

```text
identity_scope_administration_groups
identity_scope_administration_group_memberships
identity_scope_administration_policies
identity_scope_administration_policy_statements
identity_scope_administration_group_policy_bindings
```

These tables deliberately contain no `tenant_id`.

Membership is directly between an active identity-scope user and an administration group
for one application.

Policies use the same:

```text
ApplicationSecurityModel
CapabilityPattern
```

semantics as tenant policies.

Exact and wildcard statements must match at least one concrete capability declared by the
pinned application security model.

## Authorization Flow

For routes with a tenant:

```text
AdministrationRequestContext
    -> TenantReference
    -> tenant grant projection
    -> external RBAC
```

For routes without a tenant:

```text
AdministrationRequestContext
    -> IdentityScopeId
    -> scope administration grant projection
    -> external RBAC
```

The HTTP authorizer never converts a tenant grant into scope authority.

## Bootstrap

The first identity-scope administrator must be provisioned explicitly out of band.

After migrations and application security-model capabilities exist:

```powershell
.\scripts\postgresql\bootstrap-identity-scope-authority.ps1 `
  -IdentityScopeId "<scope-guid>" `
  -UserId "<active-user-guid>" `
  -ApplicationKey "identity-access" `
  -ModelVersion 1
```

The default bootstrap capability pattern is:

```text
identity-access / * / *
```

The script generates and prints explicit:

```text
GroupId
PolicyId
StatementId
```

Retain these identifiers for lifecycle management.

The script is intentionally explicit and does not infer an administrator from tenant
membership or application activity.

## Security Properties

Identity-scope authority requires:

```text
active user
active scope administration group
active scope administration policy
bound policy statement
same IdentityScopeId
same ApplicationKey
```

The scope grant reader does not evaluate wildcard patterns.

TRN materialization and wildcard authorization remain delegated through:

```text
CapabilityGrantAuthorizationEvaluator
    -> IRbacAuthorizationAdapter
    -> external RBAC engine
```

## Validation

After applying migration 0010:

```powershell
.\scripts\postgresql\verify-identity-scope-authority.ps1
```

The fixture validates active grant projection and proves that disabling the authority group
removes the effective grant. All fixture data is rolled back.
