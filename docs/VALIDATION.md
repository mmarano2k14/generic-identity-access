# Validation

## Standard repository verification

From the repository root:

```powershell
.\scripts\verify.ps1 -Configuration Release
```

The verification performs:

```text
dotnet --info
dotnet restore
dotnet build
dotnet test
```

## PostgreSQL qualification

Create the local database if required:

```powershell
psql -U postgres -d postgres -f .\scripts\postgresql\create-default-database.sql
```

Apply schema and validate:

```powershell
.\scripts\postgresql\apply-default-schema.ps1
.\scripts\postgresql\verify-migration-integrity.ps1
.\scripts\postgresql\verify-organizations.ps1

$env:ORGANIZATION_DIRECTORY_POSTGRES_DEFAULT = "Host=127.0.0.1;Port=5432;Database=generic_organization_directory_default;Username=postgres;Password=<password>"
.\scripts\postgresql\verify-store.ps1
```

The PostgreSQL validation proves:

- migration filename/checksum integrity;
- tenant-local organization key uniqueness;
- tenant-local parent foreign keys;
- direct and deep cycle protection;
- protected parent deletion semantics;
- positive durable row versions;
- an optimistic-concurrency update path.

## Domain and source invariants

Repository tests continue to prove:

- canonical organization keys are validated;
- organizations cannot parent themselves;
- parent references cannot cross tenant boundaries;
- negative concurrency versions are rejected;
- organization membership cannot cross identity-scope or tenant boundaries;
- resource-scope linkage cannot cross identity-scope or tenant boundaries;
- the domain contains no consumer-specific business semantics;
- every C# source file follows the source-layout convention.

Backup/restore, OrganizationMembership persistence, real Identity Access integration and administration authorization remain later qualification increments.
