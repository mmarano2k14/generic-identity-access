# OrganisationProfile Security Integration

## Boundary

OrganisationProfile remains a reusable generic module.

The reusable projects:

```text
OrganisationProfile.Domain
OrganisationProfile.Application
OrganisationProfile.Infrastructure.PostgreSql
```

do not reference Identity Access projects.

Identity Access is the host-side authorization and audit integration layer:

```text
OrganisationProfile
        ↑
IdentityAccess.Api composition root
        ├── external RBAC capability metadata
        └── routed security audit bridge
```

## Authorization catalog

OrganisationProfile follows the established integrated-administration model already used by Organization Directory.

The authorization resource remains:

```text
identity-access
```

and OrganisationProfile owns four distinct feature segments:

```text
organisation-profile
organisation-profile-template
organisation-profile-domain-override
organisation-profile-effective-version
```

Each feature currently publishes:

```text
read
write
```

`effective-version/write` means requesting deterministic resolution/snapshot publication. It does not allow mutation of an already persisted immutable semantic version.

## Administration security manifest

The existing administration application manifest advances from:

```text
modelVersion 3
```

to:

```text
modelVersion 4
```

The previous `identity-access` resource remains present and unchanged semantically.

Four OrganisationProfile features are appended to the existing:

```text
identity-access
```

resource.

No existing capability identifier is renamed or removed. Existing `identity-access:*:*` administrative grants therefore retain their established wildcard semantics while the new features remain individually grantable.

## Audit vocabulary

Security audit event identifiers are append-only.

Security Integration reserves:

```text
79  OrganisationProfileCreated
80  OrganisationProfileUpdated
81  OrganisationProfileStatusChanged
82  OrganisationProfileTemplatePinChanged
83  OrganisationProfileDomainOverridesReplaced
84  OrganisationProfileEffectiveVersionResolved
85  OrganisationProfileTemplateCreated
86  OrganisationProfileTemplateUpdated
87  OrganisationProfileTemplateStatusChanged
88  OrganisationProfileTemplateDraftCreated
89  OrganisationProfileTemplateDraftCompositionReplaced
90  OrganisationProfileTemplateVersionPublished
91  OrganisationProfileTemplateVersionRetired
```

The host bridge writes through the existing:

```text
IDatabaseRouteResolver
ISecurityAuditWriter
SecurityAuditEvent
```

pipeline.

Audit remains best-effort and never replaces a successful primary mutation result.

## Domain Registry behavior

The API host registers a fail-closed fallback:

```text
UnavailableDomainRegistryReader
```

only when no concrete `IDomainRegistryReader` has already been registered.

The fallback returns no domain version as available.

Therefore a missing Domain Registry integration cannot silently authorize or publish a domain version.

## PostgreSQL composition

OrganisationProfile continues to use the shared PostgreSQL deployment.

The API host uses:

```text
TryAddSingleton<NpgsqlDataSource>
```

so it reuses the existing shared data source when Organization Directory already registered it.

No new database schema or migration is introduced by Security Integration.

## HTTP boundary

Security Integration intentionally does not add OrganisationProfile HTTP controllers.

It freezes:

```text
host registration
authorization resource/features
security manifest version
audit event vocabulary
audit bridge
```

HTTP API and TypeScript SDK can then expose API routes using these already-versioned boundaries rather than inventing authorization and audit semantics inside controllers.
