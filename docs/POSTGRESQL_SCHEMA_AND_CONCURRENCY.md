# PostgreSQL Schema and Concurrency Independence

**Source version: 0.4.0. Date: September 21, 2026.**

## Local Default Database

The local development database is:

```text
generic_identity_access_default
```

This name is a configured local destination only. It is **not** a resolver fallback.
A missing or invalid route still fails closed.

The owned PostgreSQL schema is:

```text
identity_access
```

## Initial Persistent Tables

Version 0.4.0 introduces the first owned schema migration for:

- `users`
- `tenants`
- `tenant_memberships`
- `user_groups`
- `group_memberships`
- `schema_migrations`

Every durable identity relation carries `identity_scope_id` in its key or foreign-key
boundary. A local identifier reused in another identity scope therefore remains a
different durable identity.

## Concurrency Independence

Concurrency independence is an explicit storage invariant.

Each repository operation:

1. receives one immutable `ResolvedDatabaseRoute` snapshot;
2. verifies that the record identity scope matches that route;
3. obtains its own logical `NpgsqlConnection` from the destination data source;
4. keeps command and transaction state local to that operation;
5. never writes a process-global `CurrentUser`, `CurrentTenant`, or `CurrentDatabase`;
6. never re-resolves a different destination between the read and write of one operation.

Npgsql pooling may reuse physical connections internally, but application operation state
is not shared between concurrent callers.

## Optimistic Concurrency

Mutable rows use an explicit positive `row_version`.

A write is conditional on the version observed by the caller:

```sql
UPDATE identity_access.users
SET display_name = @display_name,
    status = @status,
    row_version = row_version + 1,
    updated_at = transaction_timestamp()
WHERE identity_scope_id = @scope
  AND user_id = @user_id
  AND row_version = @expected_version
RETURNING row_version;
```

If another operation commits first, the stale update returns no row and the persistence
adapter raises `IdentityConcurrencyException`.

This prevents silent lost updates without using a mutable in-process lock.

Two operations against different identity scopes do not conflict merely because their
local `user_id` values are equal.

## Database Constraints

Composite keys and foreign keys enforce identity-scope containment for the initial
relationships. Group membership cannot reference a tenant membership or group outside
its persisted identity-scope key.

The schema also enforces status values, positive row versions, and the application-key
grammar used by the domain foundation.

These database constraints complement server authorization. They do not replace RBAC.

## Migration Concurrency

`PostgreSqlSchemaMigrator` uses a PostgreSQL advisory transaction lock before applying
embedded migrations. Concurrent API instances therefore serialize schema migration work
inside the destination database instead of relying on a process-local mutex.

Migration records are written to `identity_access.schema_migrations` in the same
transaction as each migration batch.

Migration execution is not automatically enabled on API startup in this version.

## Local Provisioning

With PostgreSQL client tools available, create the local development database using:

```powershell
psql -d postgres -f .\scripts\postgresql\create-default-database.sql
```

Then apply the initial schema:

```powershell
.\scripts\postgresql\apply-default-schema.ps1
```

Standard PostgreSQL environment variables or local authentication configuration should
be used for `psql`. Credentials must not be committed to the repository.

The API connection secret can be supplied through:

```text
IDENTITY_ACCESS_POSTGRES_DEFAULT
```

with a connection string whose database is `generic_identity_access_default`.

## Current Scope

The user store is the first concrete persistence adapter and proves the row-version
contract. Tenant, membership, and group tables are created now so their relational
boundaries are explicit, while their full mutation repositories remain follow-up work.

No claim of live PostgreSQL validation is made until the integration suite is executed
against a real PostgreSQL instance.
