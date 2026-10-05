# OrganisationProfile PostgreSQL Persistence — PostgreSQL Lifecycle Persistence

## Scope

PostgreSQL Persistence adds:

```text
OrganisationProfile.Infrastructure.PostgreSql
OrganisationProfile.PostgreSqlProbe
```

and integrates all OrganisationProfile Foundation/2 projects into:

```text
IdentityAccess.sln
```

No new root package version is required because the repository already centrally manages `Npgsql`.

## Migration

Apply:

```powershell
.\scripts\organisation-profile\postgresql\apply-schema.ps1
```

This creates:

```text
organisation_profile.schema_migrations
organisation_profile.organisation_profiles
```

## Connection configuration

The scripts/probe first accept:

```text
ORGANISATION_PROFILE_POSTGRES_DEFAULT
```

and fall back to the existing integrated environment variable:

```text
IDENTITY_ACCESS_POSTGRES_DEFAULT
```

Example:

```powershell
$env:IDENTITY_ACCESS_POSTGRES_DEFAULT = "Host=127.0.0.1;Port=5432;Database=generic_identity_access_default;Username=postgres;Password=sa"
```

## Verification

Apply the schema first:

```powershell
.\scripts\organisation-profile\postgresql\apply-schema.ps1
```

Then run the complete PostgreSQL Persistence gate:

```powershell
.\scripts\organisation-profile\verify.ps1 -Configuration Release
```

The gate covers:

```text
solution restore
solution build
foundation probe
migration checksum integrity
schema constraints
live PostgreSQL persistence probe
optimistic concurrency
one-profile-per-Organization uniqueness
Organization reference FK
template-pin persistence
lifecycle persistence
probe cleanup
```

Expected final output:

```text
OrganisationProfile PostgreSQL Persistence verification: GREEN
```

## Partial source/build verification

When PostgreSQL is intentionally unavailable:

```powershell
.\scripts\organisation-profile\verify.ps1 `
    -Configuration Release `
    -SkipPostgreSql
```

This is explicitly partial.
