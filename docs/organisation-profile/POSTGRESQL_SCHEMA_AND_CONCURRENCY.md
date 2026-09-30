# OrganisationProfile PostgreSQL Schema and Concurrency

## Owned schema

```text
organisation_profile
```

This module does not create tables in:

```text
identity_access
organization_directory
```

## Migration metadata

```text
organisation_profile.schema_migrations
```

Migration files are immutable after application and are protected by SHA-256 checksum verification.

## Pack 2 table

```text
organisation_profile.organisation_profiles
```

Columns:

```text
organisation_profile_id
identity_scope_id
tenant_id
organization_id

template_key
template_version

status
row_version

created_at
updated_at
```

## Identity and uniqueness

Primary identity:

```text
organisation_profile_id
```

Initial business cardinality:

```text
(identity_scope_id, tenant_id, organization_id)
    UNIQUE
```

Therefore one Organization can have at most one OrganisationProfile.

## Organization reference integrity

The shared-database deployment uses:

```text
FOREIGN KEY
(identity_scope_id, tenant_id, organization_id)
REFERENCES organization_directory.organizations
```

with `ON DELETE RESTRICT`.

The application layer still exposes a narrow `IOrganizationReferenceReader`; the shared-database FK is an infrastructure integrity mechanism, not a transfer of Organization ownership.

## Template pin

Pack 2 persists only the already-frozen pin:

```text
template_key?
template_version?
```

Both fields are null together or populated together.

When populated:

```text
template_key      stable lowercase slug
template_version  > 0
```

No template table is created yet. Template catalog persistence belongs to Pack 3.

## Optimistic concurrency

New domain instances use:

```text
RowVersion = 0
```

PostgreSQL persists them as:

```text
row_version = 1
```

Every successful update performs:

```text
WHERE row_version = expected
SET row_version = row_version + 1
```

A stale update raises:

```text
OrganisationProfileConcurrencyException
```

`RowVersion` remains distinct from future immutable semantic `OrganisationProfileVersionNumber`.

## Lifecycle

Pack 2 supports durable:

```text
Active
Disabled
```

No hard-delete API is introduced.

This preserves stable profile identity and leaves destructive data lifecycle policy for a later explicit design decision.
