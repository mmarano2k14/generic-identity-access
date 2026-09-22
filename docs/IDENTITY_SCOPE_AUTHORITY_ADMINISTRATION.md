# Identity-Scope Authority Administration

After the first administrator is provisioned explicitly, identity-scope authority is
administered through the authenticated and RBAC-protected MVC API.

The bootstrap script remains only the root-of-trust provisioning mechanism.

## Routes

Groups:

```text
GET  /api/v1/identity-scopes/{identityScopeId}/applications/{applicationKey}/scope-authority/groups/{groupId}
POST /api/v1/identity-scopes/{identityScopeId}/applications/{applicationKey}/scope-authority/groups
PUT  /api/v1/identity-scopes/{identityScopeId}/applications/{applicationKey}/scope-authority/groups/{groupId}
```

Memberships:

```text
GET    .../scope-authority/groups/{groupId}/members
POST   .../scope-authority/groups/{groupId}/members
DELETE .../scope-authority/groups/{groupId}/members/{userId}
```

Policies and statements:

```text
GET  .../scope-authority/policies/{policyId}
POST .../scope-authority/policies
PUT  .../scope-authority/policies/{policyId}

GET    .../scope-authority/policies/{policyId}/statements
POST   .../scope-authority/policies/{policyId}/statements
DELETE .../scope-authority/policies/{policyId}/statements/{statementId}
```

Bindings:

```text
GET    .../scope-authority/groups/{groupId}/policy-bindings
POST   .../scope-authority/groups/{groupId}/policy-bindings
DELETE .../scope-authority/groups/{groupId}/policy-bindings/{policyId}
```

## Capability Model

Authority administration itself is protected by identity-scope RBAC capabilities:

```text
identity-access / scope-authority-group / read|write
identity-access / scope-authority-membership / read|write
identity-access / scope-authority-policy / read|write
identity-access / scope-authority-statement / read|write
identity-access / scope-authority-binding / read|write
```

A scope administrator therefore does not automatically receive permission to modify
authority assignments. The bootstrap or an existing authority policy must explicitly grant
these capabilities.

## Mutation Invariants

Adding a group member uses one PostgreSQL statement and requires:

```text
group is Active
user is Active
same IdentityScopeId
same ApplicationKey
```

Adding a group-policy binding uses one PostgreSQL statement and requires:

```text
group is Active
policy is Active
same IdentityScopeId
same ApplicationKey
```

Policy-statement insertion remains protected by the migration-0010 trigger, including
security-model pinning and wildcard-pattern validation.

## Optimistic Concurrency

Group and policy metadata retain `row_version` optimistic concurrency.

A stale update raises the standard identity concurrency conflict and is translated by the
central API exception handler.

## Audit

Scope-authority mutations emit typed security audit events for:

```text
group create/update
member add/remove
policy create/update
statement add/remove
binding add/remove
```

No raw session token, password, or connection information enters audit payloads.

## Bootstrap Boundary

The first administrator is still provisioned explicitly with:

```powershell
.\scripts\postgresql\bootstrap-identity-scope-authority.ps1 ...
```

After that operation, normal authority lifecycle management should use the RBAC-protected
HTTP API rather than direct SQL.

## Validation

```powershell
.\scripts\postgresql\verify-identity-scope-authority-administration.ps1
```

The validation fixture proves the active-user/group and active-group/policy creation
invariants and rolls back all fixture data.
