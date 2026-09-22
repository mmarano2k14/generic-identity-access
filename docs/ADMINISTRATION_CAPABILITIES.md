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
scope-type
resource-scope
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
