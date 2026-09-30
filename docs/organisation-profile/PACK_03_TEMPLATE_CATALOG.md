# OrganisationProfile Pack 3 — Template Catalog and Immutable Publication

## Responsibility split

```text
OrganisationProfileTemplateDefinitionService
    template metadata lifecycle

OrganisationProfileTemplateDraftService
    Draft version creation and composition replacement

OrganisationProfileTemplatePublicationService
    publish and retire

OrganisationProfileTemplateContentHasher
    deterministic content identity
```

No `OrganisationProfileTemplateManager` is introduced.

## Database

Migration:

```text
0002_template_catalog.sql
```

Tables:

```text
organisation_profile.organisation_profile_templates
organisation_profile.organisation_profile_template_versions
organisation_profile.organisation_profile_template_domains
```

A profile pin now references a real template version.

New or changed pins require:

```text
status = Published
```

Existing historical pins remain valid when a Published version becomes Retired.

## Publication

Canonical hash input:

```text
organisation-profile-template-content/v1
template=<key>
version=<version>
domain=<key>@<version>
...
```

Domains are sorted by stable key before hashing.

Published domain content and published hash cannot be updated.

## Pack boundary

Pack 3 accepts syntactically valid version-pinned domains.

It does not yet verify those domains against a Domain Registry. That external compatibility boundary belongs to Pack 4.

## Apply

```powershell
.\scripts\organisation-profile\postgresql\apply-schema.ps1
```

If durable profiles contain pre-catalog template pins from Pack 2 manual experimentation, migration `0002` stops explicitly. Clear those unqualified pins before applying the real template catalog.

## Verify

```powershell
.\scripts\organisation-profile\verify.ps1 -Configuration Release
```

Expected:

```text
OrganisationProfile template catalog schema validation: GREEN
OrganisationProfile template catalog probe: GREEN
OrganisationProfile Pack 3 verification: GREEN
```
