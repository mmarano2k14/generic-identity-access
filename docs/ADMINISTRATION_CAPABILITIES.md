# Administration Capabilities

The administration API declares authorization metadata through a centralized capability
catalog.

The canonical resource is:

```text
identity-access
```

Supported administration features currently include:

```text
user
tenant
tenant-membership
group
group-membership
credential
policy
policy-statement
policy-binding
security-model
scope-type
resource-scope
session
security-audit
mfa-policy
mfa-authenticator
scope-authority-group
scope-authority-membership
scope-authority-policy
scope-authority-statement
scope-authority-binding
```

Supported actions are:

```text
read
write
```

Controllers reference `IdentityAccessAdministrationCapabilities` instead of duplicating
string literals.

The catalog is authorization metadata only. A constant does not authenticate a caller,
grant a permission, or evaluate a wildcard. Runtime authorization remains delegated to
the configured administration authorizer and the RBAC integration path.

The `security-model` feature protects application security-model discovery (`read`) and immutable manifest registration (`write`). Registration changes the durable capability catalog but does not grant permissions or evaluate RBAC decisions.

The `security-audit` feature is read-only in the current administration surface. It uses the `read` action and does not define a write endpoint. Its records are diagnostic evidence; possessing audit-read permission does not grant any capability represented by an audited event.
