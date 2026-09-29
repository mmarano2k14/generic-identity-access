# PostgreSQL Schema and Concurrency

## Ownership

Generic Organization Directory owns the PostgreSQL schema:

```text
organization_directory
```

The initial migration creates only durable Organization identity and hierarchy. Membership and authorization-scope linkage persistence are introduced by later migrations.

## Migration metadata

Applied migrations are recorded in:

```text
organization_directory.schema_migrations
```

Each record stores:

```text
version
name
SHA-256 checksum
applied_at
```

Already-applied migration files are immutable. Filename or normalized SHA-256 changes fail migration integrity validation.

## Organizations

`organization_directory.organizations` uses the composite identity:

```text
identity_scope_id
tenant_id
organization_id
```

Tenant-local organization keys are unique through:

```text
(identity_scope_id, tenant_id, organization_key)
```

The parent foreign key includes `identity_scope_id` and `tenant_id`, so a parent cannot cross tenant boundaries. `ON DELETE RESTRICT` prevents deleting a parent that still has children.

## Hierarchy cycles

A PostgreSQL trigger evaluates the ancestry chain before parent mutations. Direct self-parenting and deep cycles such as:

```text
A -> B -> C -> A
```

are rejected with a constraint-class SQLSTATE. Application persistence translates hierarchy violations into a stable hierarchy-conflict exception.

## Optimistic concurrency

New rows begin at:

```text
row_version = 1
```

Updates use:

```text
WHERE ... AND row_version = expected
```

and atomically increment the durable version. A row that still exists at a different version raises `OrganizationConcurrencyException`; a missing row is reported as not found.

Delete uses the same version precondition. Public destructive API operations are not exposed by the current host; delete exists at the persistence boundary for controlled lifecycle, cleanup and qualification work.

## Failure mapping

```text
Duplicate tenant-local key
  -> OrganizationKeyConflictException

Cross-tenant parent / cycle / child-protected delete
  -> OrganizationHierarchyConflictException

Stale row version
  -> OrganizationConcurrencyException
```

## Qualification

Apply schema:

```powershell
.\scripts\postgresql\apply-default-schema.ps1
```

Then run:

```powershell
.\scripts\postgresql\verify-migration-integrity.ps1
.\scripts\postgresql\verify-organizations.ps1
```

The organization validator runs inside a transaction and rolls back its fixtures.
