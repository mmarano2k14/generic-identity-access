# OrganisationProfile Pack 1 — Foundation and Contract Freeze

## Scope

This pack introduces only additive OrganisationProfile foundation files.

It does not modify root solution files, central package-management files, existing Identity Access code, or Generic Organization Directory code.

## Projects

```text
src/
├── OrganisationProfile.Domain
└── OrganisationProfile.Application

tests/
└── OrganisationProfile.FoundationProbe
```

No external NuGet package is required by the foundation probe.

## Frozen contracts

```text
OrganisationProfileId
OrganizationReference
OrganisationProfileStatus

OrganisationProfileTemplateKey
OrganisationProfileTemplateVersionNumber
OrganisationProfileTemplatePin
OrganisationProfileTemplateStatus
OrganisationProfileTemplateVersionStatus

DomainKey
DomainVersion
OrganisationProfileDomainSelection
OrganisationProfileDomainOverride
OrganisationProfileDomainOverrideOperation

OrganisationProfileContentHash
OrganisationProfileVersionNumber

OrganisationProfile
OrganisationProfileTemplate
PublishedOrganisationProfileTemplateVersion
EffectiveOrganisationProfile

IOrganizationReferenceReader
OrganizationReferenceState
IOrganisationProfileClock
```

## Exit gate

From the host repository root:

```powershell
.\scripts\organisation-profile\verify-foundation.ps1 -Configuration Release
```

Expected final output:

```text
OrganisationProfile Pack 1 verification: GREEN
```

## Deliberately deferred

Pack 2 owns PostgreSQL persistence.

Pack 3 owns template catalog lifecycle and immutable publication services.

Pack 4 owns deterministic domain composition resolution and the Domain Registry reader.

No persistence or API shape is prematurely frozen here.
