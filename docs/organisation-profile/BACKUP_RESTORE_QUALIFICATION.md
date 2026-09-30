# OrganisationProfile Backup and Restore Qualification

## Recovery boundary

OrganisationProfile owns its own schema, but its durable rows reference externally owned identity and organization records.

For the current shared-PostgreSQL deployment, a coherent recovery set therefore includes:

```text
identity_access
organization_directory
organisation_profile
```

This does not transfer ownership of Tenant or Organization data to OrganisationProfile. It only reflects restore ordering and foreign-key integrity.

## Why the profile schema alone is insufficient

`organisation_profile.organisation_profiles` references the existing Organization identity using:

```text
identity_scope_id
tenant_id
organization_id
```

The Organization Directory in turn references Identity Access Tenant and membership/resource-scope state.

An empty target database containing only OrganisationProfile tables would therefore not represent a valid recovery environment.

## Domain Registry implication

OrganisationProfile stores explicit domain keys and versions, but does not own Domain Registry persistence.

A historical effective profile can identify its pinned domain versions after restore, but deterministic replay also requires those historical Domain Registry versions to remain available from the external Domain Registry authority.

Once that subsystem is implemented, its owner must define and qualify its own backup/restore policy. Pack 8 does not fabricate a Domain Registry backup format.

## Qualification script

Run:

```powershell
.\scripts\organisation-profile\postgresql\verify-backup-restore.ps1
```

Prerequisites:

```text
psql
pg_dump
pg_restore
CREATE DATABASE privilege on the development PostgreSQL instance
DROP DATABASE privilege for the disposable target
```

The source database is selected through the same environment used by the other OrganisationProfile PostgreSQL gates.

Supported configuration includes:

```text
ORGANISATION_PROFILE_POSTGRES_DEFAULT
IDENTITY_ACCESS_POSTGRES_DEFAULT
ORGANISATION_PROFILE_POSTGRES_DATABASE
IDENTITY_ACCESS_POSTGRES_DATABASE
ORGANISATION_PROFILE_POSTGRES_USER
IDENTITY_ACCESS_POSTGRES_USER
PGPASSWORD / PGPASSFILE
```

## Safety behavior

The restore target is randomized with the prefix:

```text
organisation_profile_restore_
```

The script refuses to reuse an existing target name.

The temporary dump is deleted by default. Use `-KeepArtifact` only when the artifact must be inspected manually.

A qualification dump may contain account, security, and other sensitive development data from the included schemas. It must never be committed to source control or treated as a shareable build artifact.

## Integrity checks after restore

The restored database must preserve the same OrganisationProfile counts for:

```text
schema migration records
profiles
template definitions
template versions
effective profile versions
effective version domains
```

It must also pass the existing restored-database checks for:

```text
migration checksums
base profile schema
template catalog schema
effective composition schema
immutable semantic-version constraints/triggers
```

## Operational limitation

This qualification proves logical dump/restore compatibility for the selected schemas in the tested environment.

It does not by itself define:

```text
production RPO
production RTO
point-in-time recovery
WAL retention
cross-region replication
backup encryption/key custody
secret-store recovery
Domain Registry recovery
provider credential recovery
```

Those remain deployment and subsystem-owner responsibilities.
