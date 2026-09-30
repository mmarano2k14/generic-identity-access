# OrganisationProfile Pack 4 — Domain Composition and Effective Resolution

## Scope

Pack 4 turns a profile/template pin into deterministic effective semantic content.

```text
Published/Retired template version
              +
Organization-specific overrides
              ↓
OrganisationProfileCompositionResolver
              ↓
effective version-pinned domains
              ↓
Domain Registry resolution policy
              ↓
stable content hash
              ↓
immutable OrganisationProfileVersion
```

## Responsibility split

```text
OrganisationProfileCompositionResolver
    pure template + override merge

OrganisationProfileDomainRegistryValidator
    external domain version policy

OrganisationProfileDomainOverrideService
    mutable Organization-specific override set

OrganisationProfileCompositionService
    orchestration + immutable snapshot publication

OrganisationProfileEffectiveContentHasher
    deterministic semantic identity
```

Persistence is also separated:

```text
PostgreSqlOrganisationProfileDomainOverrideStore

PostgreSqlOrganisationProfileVersionStore
    ├── PostgreSqlOrganisationProfileVersionReader
    └── PostgreSqlOrganisationProfileVersionWriter
```

No `OrganisationProfileCompositionManager` is introduced.

## Override semantics

Template example:

```text
commerce@4
inventory@3
finance@5
```

Overrides:

```text
Disable inventory
Enable manufacturing@2
```

Effective result:

```text
commerce@4
finance@5
manufacturing@2
```

Output is always sorted by `DomainKey`.

Duplicate overrides are rejected.

## Domain Registry policy

New selection requires:

```text
Published
```

Historical resolution accepts:

```text
Published
Retired
```

This means a domain version may be retired from new use without breaking an already pinned historical OrganisationProfile.

## Effective hash

Canonical effective content contains:

```text
organisation-profile-effective-content/v1
template=<template-key>@<version>
domain=<domain-key>@<version>
...
```

or:

```text
template=none
```

when the profile has no template.

The hash excludes RowVersion and timestamps.

Therefore persistence/concurrency metadata does not change semantic identity.

## Effective semantic version

`OrganisationProfileVersionNumber` remains separate from mutable `RowVersion`.

```text
RowVersion
    protects current mutable profile configuration

OrganisationProfileVersionNumber
    identifies one immutable resolved semantic snapshot
```

If content is unchanged, resolving again returns the existing latest semantic version.

If content changes, a new version is appended.

If content later returns to an earlier configuration, a new version is still appended so temporal history remains explicit.

## PostgreSQL

Migration:

```text
0003_effective_composition.sql
```

Tables:

```text
organisation_profile.organisation_profile_domain_overrides

organisation_profile.organisation_profile_versions

organisation_profile.organisation_profile_version_domains
```

The override set is mutable.

Effective version rows and their domain rows are append-only through the application contract and protected against SQL `UPDATE`.

## Concurrency

Replacing overrides:

```text
lock parent profile
validate expected RowVersion
replace complete override set
increment parent RowVersion
commit
```

Publishing an effective semantic snapshot:

```text
lock parent profile
revalidate RowVersion + template pin + active status
compare latest content hash
append only when semantic content changed
commit
```

This prevents an effective snapshot from being persisted from stale profile configuration.

## Apply

```powershell
.\scripts\organisation-profile\postgresql\apply-schema.ps1
```

## Verify

```powershell
.\scripts\organisation-profile\verify.ps1 -Configuration Release
```

Expected Pack 4 lines include:

```text
OrganisationProfile effective composition schema validation: GREEN
OrganisationProfile effective composition probe: GREEN
OrganisationProfile Pack 4 verification: GREEN
```
