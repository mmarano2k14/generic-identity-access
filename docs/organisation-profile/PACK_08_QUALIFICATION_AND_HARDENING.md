# OrganisationProfile Pack 8 — Qualification and hardening

## Scope

Pack 8 closes the initial OrganisationProfile core by qualifying the already implemented boundaries under integration, concurrency, persistence, and recovery pressure.

It introduces no new business capability and no database migration.

The qualification target is the existing surface:

```text
profile lifecycle
profile template catalog
immutable template publication
domain override composition
immutable effective versions
Organization reference boundary
Identity Access authorization integration
HTTP API
TypeScript SDK
administration UI
PostgreSQL persistence
```

Provider integration remains outside this core pack.

## Automated qualification

Run:

```powershell
.\scripts\organisation-profile\verify.ps1 -Configuration Release
```

This gate now executes:

```text
solution restore
solution build
complete .NET test suite
foundation probe
composition/source separation
security integration
HTTP API + TypeScript SDK qualification
administration UI typecheck/build qualification
Pack 8 source hardening checks
migration checksum integrity
PostgreSQL schema gates
PostgreSQL lifecycle persistence probe
template catalog probe
effective composition probe
adversarial qualification probe
```

A run with `-SkipPostgreSql` is explicitly partial and is not a release-candidate qualification.

## Adversarial concurrency

The Pack 8 qualification probe exercises two concurrent complete-set domain override mutations using the same parent profile `RowVersion`.

Required result:

```text
one mutation commits
one mutation is rejected as stale
profile RowVersion advances exactly once
persisted override set is complete
```

The probe also starts two concurrent effective-profile resolutions against the same unchanged profile state.

Required result:

```text
both callers resolve the same content hash
both callers observe the same semantic version
only one durable semantic version exists
```

The existing `FOR UPDATE` serialization in the focused persistence writers remains the mechanism under qualification; Pack 8 does not introduce a second concurrency system.

## Version reproducibility

The qualification sequence creates three semantic states:

```text
A -> B -> A
```

Required durable history:

```text
version 1 -> hash A
version 2 -> hash B
version 3 -> hash A
```

This proves two different properties at the same time:

1. semantic history is append-only and is not rewritten when content reverts;
2. equivalent effective content reproduces the same deterministic hash.

Resolving unchanged version-3 state again must return version 3 rather than append version 4.

Template content hashing is also qualified with reversed input ordering and must produce the same published hash.

## Stale and cross-tenant rejection

Pack 8 explicitly verifies:

```text
stale profile RowVersion cannot resolve a new semantic snapshot
cross-tenant OrganizationReference cannot be used to create a profile
disabled OrganisationProfile cannot publish a new effective snapshot
```

Lifecycle-only disable/re-enable operations must not create a new semantic version when effective content did not change.

## Migration integrity

Pack 8 adds no migration.

The expected OrganisationProfile migration set remains:

```text
0001_organisation_profiles.sql
0002_template_catalog.sql
0003_effective_composition.sql
```

The existing migration ledger and SHA-256 checksum gate remain authoritative. A changed historical migration, unknown applied version, or missing applied version fails qualification.

## Browser evidence

The manual Pack 7 browser flow remains the real UI behavior gate:

```powershell
.\scripts\organisation-profile\verify-administration-ui-browser.ps1
```

The generated JSON evidence is no longer accepted only by convention. Pack 8 adds:

```powershell
.\scripts\organisation-profile\verify-browser-evidence.ps1 `
  -EvidencePath <path>
```

The validator requires all ten browser scenarios to exist and be true.

## Backup and restore

Pack 8 adds a disposable-database restore qualification:

```powershell
.\scripts\organisation-profile\postgresql\verify-backup-restore.ps1
```

The qualification backup intentionally includes:

```text
identity_access
organization_directory
organisation_profile
```

because OrganisationProfile durable references depend on external Tenant and Organization authority. Restoring only `organisation_profile` into an empty database is not a valid recovery proof for the shared-database deployment.

The script:

```text
creates a custom-format pg_dump
inspects the dump contents
creates a randomized disposable database
restores the three dependent schemas
compares source/restored OrganisationProfile row counts
reruns migration/schema/template/effective validation on the restored database
drops the disposable database
removes the temporary dump by default
```

The script requires PostgreSQL client tools and permission to create/drop a disposable database.

See `BACKUP_RESTORE_QUALIFICATION.md` for operational implications.

## Release-candidate gate

A release-candidate qualification combines the automated gate, browser evidence, and backup/restore proof:

```powershell
.\scripts\organisation-profile\verify-release-candidate.ps1 `
  -Configuration Release `
  -BrowserEvidencePath .\artifacts\qualification\organisation-profile-ui-<timestamp>.json
```

Success ends with:

```text
OrganisationProfile Pack 8 release-candidate qualification: GREEN
```

Using `-SkipBackupRestore` deliberately produces only a partial result and never prints the release-candidate GREEN line.

## Exit criteria

Pack 8 is closed only when the target development/release environment proves:

```text
integrated build                         GREEN
integrated .NET tests                    GREEN
TypeScript SDK tests/typecheck           GREEN
administration UI production build       GREEN
migration integrity                      GREEN
profile PostgreSQL probe                 GREEN
template publication probe               GREEN
effective composition probe              GREEN
concurrent writer qualification          GREEN
concurrent snapshot idempotency           GREEN
version reproducibility                  GREEN
cross-tenant rejection                   GREEN
browser evidence                         GREEN
backup/restore qualification             GREEN
```

No ignored, manually bypassed, or skipped check is described as a full release-candidate qualification.
