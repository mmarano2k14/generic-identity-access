# OrganisationProfile — Foundation Architecture

## Ownership

`OrganisationProfile` is owned by OrganisationProfile.

It enriches an existing `Organization` from Generic Organization Directory with OrganisationProfile semantic composition. It does not own Organization identity, membership, authentication, ResourceScope authority, or RBAC.

```text
Identity Access
    ↓
Organization Directory
    ↓
Organization
    ↓
OrganisationProfile
    ↓
version-pinned domain composition
```

## Naming

The canonical OrganisationProfile concept is:

```text
OrganisationProfile
```

Historical `BusinessProfile` terminology is not used in source code.

The upstream generic identity remains:

```text
Organization
```

## Initial cardinality

Pack 1 freezes the initial cardinality:

```text
Organization 1 ── 0..1 OrganisationProfile
```

An Organization may exist without a profile.

An OrganisationProfile always references exactly one Organization.

The durable uniqueness constraint is implemented in Pack 2:

```text
IdentityScopeId + TenantId + OrganizationId
```

## Template pin

A profile may pin:

```text
TemplateKey
+
TemplateVersion
```

Example:

```text
ecommerce-standard@3
```

Published template versions are immutable. Mutation creates a different version.

## Domain pinning

Every effective domain is explicitly versioned:

```text
commerce@4
inventory@3
finance@5
```

Durable or replay-sensitive behavior must never resolve an unqualified `latest` domain version.

## Override semantics

Organization-specific composition uses explicit overrides:

```text
Enable(domainKey, domainVersion)
Disable(domainKey)
```

`Enable` requires a version.

`Disable` carries no version.

Pack 4 will implement the deterministic resolver. Pack 1 freezes the contract only.

## Version identities

Two independent versions are intentionally distinct:

```text
RowVersion
    optimistic concurrency

OrganisationProfileVersionNumber
    immutable semantic/effective profile snapshot
```

These concepts must not be merged.

## Integration boundary

Pack 1 does not reference Generic Organization Directory assemblies.

Instead it defines:

```text
IOrganizationReferenceReader
```

with only the external state required by the profile lifecycle.

This preserves the option for in-process, HTTP, database-backed, or other trusted adapters later without moving Organization ownership into OrganisationProfile.

## Explicit exclusions

The foundation does not contain:

```text
Provider credentials
Provider secrets
Provider DTOs
Identity Access authorization logic
Organization membership
Business entities
Domain business rules
Runtime execution
```

Those belong to their own subsystems.

## Anti-god-class rule

The subsystem will evolve through focused responsibilities:

```text
profile lifecycle
template lifecycle
publication
composition resolution
persistence
security integration
API/SDK
UI
```

No single `OrganisationProfileManager` owns the complete subsystem.

## Template catalog and publication

Reusable composition is versioned independently from concrete Organizations:

```text
Template Definition
        ↓
Draft Version
        ↓
Published immutable content
        ↓
Retired historical version
```

Published content identity includes the template key, explicit template version, and key-ordered `DomainKey@DomainVersion` composition.

A concrete profile pin therefore identifies one exact published semantic template version and never an implicit latest version.

Definition lifecycle, draft editing, publication, and content hashing remain separate responsibilities.

Domain Registry validation remains external to this catalog and is introduced in Pack 4.

## Effective domain composition

Pack 4 introduces a deterministic effective-composition layer without moving Domain Registry ownership into this module.

```text
Template version
      +
Profile override set
      ↓
OrganisationProfileCompositionResolver
      ↓
exact DomainKey@DomainVersion set
      ↓
Domain Registry validation
      ↓
effective content hash
      ↓
immutable semantic profile version
```

The Domain Registry boundary distinguishes new selection from historical resolution:

```text
Published
    selectable + resolvable

Retired
    not selectable
    still resolvable
```

This allows deprecation without rewriting historical profile semantics.

Mutable profile configuration and immutable semantic history remain separate:

```text
organisation_profiles
organisation_profile_domain_overrides
        ↓ resolve
organisation_profile_versions
organisation_profile_version_domains
```

`RowVersion` protects mutable configuration.

`OrganisationProfileVersionNumber` identifies immutable resolved semantics.

No Domain Registry implementation dependency is introduced and no god composition service owns registry persistence, template persistence, overrides, hashing, and snapshot persistence together.

## Identity Access host security integration

OrganisationProfile remains independent from Identity Access at the reusable-project layer.

The existing Identity Access API host is the composition boundary:

```text
IdentityAccess.Api
    ├── OrganisationProfile registration
    ├── external RBAC capability metadata
    └── best-effort routed security audit bridge
```

Authorization follows the existing integrated administration resource:

```text
identity-access
```

with distinct OrganisationProfile feature segments:

```text
organisation-profile
organisation-profile-template
organisation-profile-domain-override
organisation-profile-effective-version
```

The administration manifest version containing these capabilities is:

```text
modelVersion 4
```

The host provides a fail-closed `IDomainRegistryReader` fallback so missing Domain Registry integration cannot silently make a domain version selectable.

HTTP endpoints remain outside Pack 5. Pack 6 consumes these frozen authorization and audit boundaries.

## HTTP administration boundary

Pack 6 exposes OrganisationProfile through focused controllers in the existing Identity Access host.

Tenant profile routes validate the durable profile's `IdentityScopeId` and `TenantId` before any profile-id based read or mutation proceeds.

Controller responsibilities remain separated:

```text
profile definition
domain overrides
effective semantic versions
template definitions
template-version lifecycle
```

The HTTP layer does not implement a second authorization engine. Every endpoint uses the existing administration authorization filter and the capability coordinates frozen in Pack 5.

The TypeScript SDK mirrors the same separation with five focused clients rather than one large OrganisationProfile client.

## Pack 7 UI boundary

The reference OrganisationProfile workspace is mounted outside the Identity Access administration navigation.

```text
Existing Organization
        ↓ read-only reference
OrganisationProfile workspace
        ├── Template selection
        ├── Domain overrides
        ├── Effective versions
        └── Profile lifecycle
```

Organization identity, OrganizationMembership, ResourceScope linkage, authorization grants, provider connections, and provider credentials are not owned by this UI.

The UI composition remains focused:

```text
OrganisationProfilePanel
├── ProfileTemplateSelector
├── DomainCompositionPanel
├── ProfileVersionPanel
└── ProfileLifecycleActions
```

New template and domain foreign references are selected only from protected catalog reads and are revalidated server-side before mutation.

## Pack 8 qualification boundary

Pack 8 does not add a new runtime responsibility or persistence model. It qualifies the existing architecture under adversarial execution and recovery conditions.

The additional qualification path is:

```text
integrated build + tests
        ↓
source/architecture gates
        ↓
PostgreSQL migration/schema gates
        ↓
concurrent mutation + snapshot probe
        ↓
browser evidence
        ↓
disposable backup/restore proof
        ↓
release-candidate qualification
```

Concurrency remains governed by the existing optimistic `RowVersion` contract plus focused PostgreSQL row locking. Pack 8 does not introduce a distributed lock service or a second version authority.

Semantic version history remains append-only. Returning to an earlier effective composition produces a new semantic version while reproducing the same deterministic content hash.

For shared-database recovery qualification, the logical restore set includes the externally owned `identity_access` and `organization_directory` schemas together with `organisation_profile`, because the profile schema contains durable foreign references to those authorities. This recovery grouping does not change subsystem ownership.

Historical Domain Registry data remains externally owned and must remain available for replay of pinned domain versions once that subsystem has durable persistence.
