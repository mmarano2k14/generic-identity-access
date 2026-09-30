# OrganisationProfile Pack 5 — Organization + Identity Access Security Integration

## Scope

Pack 5 integrates the generic module into the existing ASP.NET Core host while preserving the module boundary.

Added host composition:

```text
builder.AddOrganizationDirectory();
builder.AddOrganisationProfile();
```

Added explicit API project references:

```text
OrganisationProfile.Domain
OrganisationProfile.Application
OrganisationProfile.Infrastructure.PostgreSql
```

## Registered module services

When shared PostgreSQL is configured:

```text
IOrganizationReferenceReader
IOrganisationProfileStore
IOrganisationProfileTemplateStore
IOrganisationProfileTemplateVersionStore
IOrganisationProfileDomainOverrideStore
IOrganisationProfileVersionStore

IOrganisationProfileClock

OrganisationProfileDomainRegistryValidator
OrganisationProfileTemplateContentHasher
OrganisationProfileEffectiveContentHasher
OrganisationProfileCompositionResolver

OrganisationProfileTemplateDefinitionService
OrganisationProfileTemplateDraftService
OrganisationProfileTemplatePublicationService
OrganisationProfileDomainOverrideService
OrganisationProfileCompositionService
```

## Security

Authorization resource:

```text
identity-access
```

OrganisationProfile features:

```text
organisation-profile
organisation-profile-template
organisation-profile-domain-override
organisation-profile-effective-version
```

Actions:

```text
read
write
```

Administration manifest:

```text
modelVersion = 4
```

## Audit

Pack 5 appends audit event identifiers 79 through 91 and registers:

```text
IOrganisationProfileSecurityAuditWriter
    ↓
OrganisationProfileSecurityAuditWriter
    ↓
existing Identity Access routed audit sink
```

No audit payload accepts arbitrary secrets or provider configuration.

## Domain Registry

A missing concrete Domain Registry integration fails closed through:

```text
UnavailableDomainRegistryReader
```

It is registered with `TryAddSingleton`, allowing a real external reader to be supplied without changing OrganisationProfile core projects.

## Verification

```powershell
.\scripts\organisation-profile\verify.ps1 -Configuration Release
```

Expected new gate:

```text
OrganisationProfile security integration validation: GREEN
```

Expected final line:

```text
OrganisationProfile Pack 5 verification: GREEN
```

## No migration

Pack 5 adds no PostgreSQL migration.

Pack 4 schema remains the current OrganisationProfile database schema.
