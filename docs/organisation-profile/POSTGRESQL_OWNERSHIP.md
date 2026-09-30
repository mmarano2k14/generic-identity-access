# OrganisationProfile PostgreSQL Ownership

The OrganisationProfile module owns its own PostgreSQL schema:

```text
organisation_profile
```

Pack 2 migrations will be created under this schema.

The module must not create tables in:

```text
identity_access
organization_directory
```

Those schemas remain owned by their respective modules.

The initial Pack 2 table family is expected to begin with:

```text
organisation_profile.organisation_profiles
organisation_profile.schema_migrations
```

Later packs may add template/version/composition tables under the same `organisation_profile` schema.

External identities are referenced by stable identifiers. The profile module does not duplicate external authority or move external tables into its schema.


## Pack 2 implementation

Pack 2 materializes the reserved ownership:

```text
organisation_profile.schema_migrations
organisation_profile.organisation_profiles
```

The profile table references `organization_directory.organizations` through a same-database foreign key while ownership of Organization identity remains external.

Template/version catalog tables remain deferred to Pack 3.
