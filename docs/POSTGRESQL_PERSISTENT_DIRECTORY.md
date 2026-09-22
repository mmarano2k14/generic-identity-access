# Persistent Identity Directory

**Source version: 0.5.0. Date: September 21, 2026.**

## Scope

This version extends the PostgreSQL foundation from user persistence to the complete initial directory model:
users, tenants, tenant memberships, user groups, and group-membership edges.

The persistence contracts remain internal server infrastructure. They do not expose public CRUD endpoints and do not
perform authentication or RBAC decisions.

## Persistence Contracts

The application layer now exposes operation-scoped stores for:

- `IUserDirectoryStore`;
- `ITenantDirectoryStore`;
- `ITenantMembershipStore`;
- `IUserGroupStore`;
- `IGroupMembershipStore`.

Every operation receives an already resolved `ResolvedDatabaseRoute`. Store implementations do not select a database,
fall back to another destination, or infer placement from record content.

## Concurrency Independence

Mutable records use explicit `row_version` compare-and-increment semantics.

A successful update requires the caller to provide the version previously observed by that logical operation. If a
concurrent writer commits first, the stale writer receives `IdentityConcurrencyException` instead of overwriting the
committed state.

Each persistence call opens its own logical Npgsql connection from the bounded destination pool. No mutable current-user,
current-tenant, current-scope, current-connection, or current-database singleton is introduced.

`identity_scope_id` remains part of every durable identity key and every relevant SQL predicate. Equal local identifiers in
different scopes therefore represent independent records.

Group membership is an immutable edge. Adding and removing an edge are exact operations under the full scoped key rather
than mutable row updates. Rehydrating an existing edge does not imply that the edge currently authorizes anything.
Current user, tenant, membership, group, policy, and resource state must still be evaluated by the authorization layer.

## Schema Evolution

`0002_directory_query_indexes.sql` adds scope-leading indexes for the initial directory access paths. It does not redefine
identity or loosen existing relational constraints.

The local schema script now applies all migration files in lexical order and explicitly uses a configurable PostgreSQL user,
defaulting to `postgres`.

Local defaults:

```text
database: generic_identity_access_default
schema:   identity_access
user:     postgres
```

These are local development conventions, not fallback routing rules.

## Live Validation

After applying migrations, run:

```powershell
.\scripts\postgresql\verify-default-database.ps1
.\scripts\postgresql\verify-directory-persistence.ps1
```

The second validation runs inside a PostgreSQL transaction and rolls back its fixtures. It verifies that equal local user,
tenant, membership, and group identifiers may coexist in different identity scopes, and that a scoped update affects only
the selected logical record.

## Security Boundary

Persistence constraints protect structural isolation but do not replace authorization.

No public account, tenant, group, membership, authentication, OIDC, MFA, policy, or RBAC mutation endpoint is introduced in
this version.
