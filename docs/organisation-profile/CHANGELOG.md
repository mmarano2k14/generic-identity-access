# OrganisationProfile changelog

## 0.8.0 — Qualification and hardening

### Added

- Dedicated live `OrganisationProfile.QualificationProbe` for adversarial concurrency and deterministic-version qualification.
- Concurrent complete-set override race proving exactly one successful RowVersion mutation and one stale-write rejection.
- Concurrent identical effective-resolution race proving single-version idempotent publication.
- Semantic `A -> B -> A` replay qualification proving append-only history plus deterministic content-hash reproducibility.
- Explicit stale-resolution, cross-tenant Organization reference, disabled-profile, and lifecycle-only semantic-stability checks.
- Full integrated `.NET` test execution in the OrganisationProfile verification gate.
- Pack 8 source-hardening gate protecting lock/hash/immutability markers and the unchanged three-migration baseline.
- Browser-evidence validator for the ten Pack 7 browser scenarios.
- Disposable PostgreSQL backup/restore qualification across `identity_access`, `organization_directory`, and `organisation_profile`.
- Release-candidate gate combining automated verification, browser evidence, and backup/restore proof.
- Backup/restore recovery-boundary documentation, including the external historical Domain Registry dependency.

### Database

- No migration is introduced in Pack 8.
- Existing migration checksum enforcement and immutable effective-version constraints remain authoritative.

### Deferred

- Provider Registry integration remains outside the OrganisationProfile core.

## 0.7.0 — Administration UI

### Added

- Application-facing OrganisationProfile workspace outside the `/identity` administration navigation.
- Read-only Organization context loaded from Organization Directory.
- Focused `OrganisationProfilePanel` composition with separate template, domain, semantic-version, and lifecycle components.
- Profile creation without duplicating Organization identity.
- Protected template selection from active definitions and published immutable versions.
- Protected domain override selection derived from published immutable template-version references.
- Server-side revalidation of submitted template/domain foreign references before calling Pack 6 mutation endpoints.
- Historical override protection that prevents the complete-set editor from silently dropping an Enable reference that is no longer selectable.
- Immutable effective-version history display and deterministic resolution action.
- Pack 7 UI architecture tests and verification gate.

### Architecture

- OrganisationProfile UI remains outside Identity Access navigation.
- Organization identity, hierarchy, memberships, ResourceScopes, authorization, and provider configuration remain external authorities.
- No god UI component is introduced.

### Database

- No migration is introduced in Pack 7.

### Deferred

- Full qualification and hardening remain Pack 8.
- Provider Registry integration remains outside the OrganisationProfile core.

## 0.6.1 — Pack 6 integration closure

### Fixed

- Preserved the existing `ApiProblems` helper surface while adding `OrganisationProfileAdministrationUnavailable()`.
- Retained `UnprocessableEntity(...)`.
- Retained `OrganizationMembershipAdministrationUnavailable()`.
- Retained `OrganizationResourceScopeLinkAdministrationUnavailable()`.
- Prevented the Pack 6 API integration from regressing pre-existing Organization Directory controllers.

### Validation

- Integrated solution build and test suite are GREEN on the validated development baseline.
- TypeScript client typecheck and tests are GREEN.
- Pack 6 source-contract markers remain present after the merge.

## 0.6.0 — HTTP API and TypeScript SDK

### Added

- Focused ASP.NET Core controllers for mutable profiles, domain overrides, immutable effective versions, template definitions, and template-version publication.
- Controller-level `RequireAdministrationCapability` enforcement using the Pack 5 security catalog.
- Tenant-boundary validation for every profile-id based tenant route.
- Semantic mutation audit emission using Pack 5 event identifiers.
- Stable HTTP problem mapping for OrganisationProfile concurrency, lifecycle, catalog, immutability, and Domain Registry failures.
- `OrganisationProfileDefinitionService` for active Organization validation, template-pin selection validation, profile lifecycle, and optimistic concurrency.
- Optional API feature handles so OrganisationProfile routes fail with 503 rather than controller activation failure when persistence is not configured.
- TypeScript package version `0.26.0`.
- Five focused TypeScript OrganisationProfile administration clients.
- Focused TypeScript DTO codec and route builders.
- Architecture/source qualification for API, tenant-boundary, security, audit, and SDK separation.

### Changed

- Template Draft creation and publication now raise a stable template-inactive application exception instead of raw `InvalidOperationException`.
- Pack verification now runs TypeScript typecheck and client tests.

### Database

- No migration is introduced in Pack 6.

### Deferred

- Administration UI remains Pack 7.
- Provider Registry integration remains outside the OrganisationProfile core.

## 0.5.1 — API host regression recovery

### Fixed

- Restored the current `IdentityAccess.Api.Http.ApiProblems` helper surface required by existing Organization Directory controllers.
- Restored `UnprocessableEntity(...)`.
- Restored `OrganizationMembershipAdministrationUnavailable()`.
- Restored `OrganizationResourceScopeLinkAdministrationUnavailable()`.
- Added a Pack 5 security-integration gate and architecture regression test protecting these pre-existing host helpers from future integration regressions.
- The `CS0006` test-project metadata error disappears once `IdentityAccess.Api` compiles successfully; it is a downstream symptom, not a separate defect.

## 0.5.0 — Identity Access host security integration

### Added

- Existing ASP.NET Core host registration through `AddOrganisationProfile()`.
- Explicit API project references to OrganisationProfile Domain, Application, and PostgreSQL infrastructure.
- `SystemOrganisationProfileClock` for hosted application services.
- Fail-closed `UnavailableDomainRegistryReader` fallback registered with `TryAddSingleton`.
- Four OrganisationProfile feature segments under the established `identity-access` administration resource.
- `organisation-profile`, `organisation-profile-template`, `organisation-profile-domain-override`, and `organisation-profile-effective-version` read/write capabilities.
- Administration security manifest model version 4.
- Append-only OrganisationProfile security audit event identifiers 79 through 91.
- Best-effort `IOrganisationProfileSecurityAuditWriter` bridge through the existing routed Identity Access audit pipeline.
- Security integration source/manifest qualification gate.
- Identity Access architecture tests protecting capability grammar, manifest versioning, audit IDs, host composition, and reusable-project dependency direction.

### Architecture

- OrganisationProfile reusable projects remain independent from Identity Access.
- Identity Access remains the external authentication/authorization/audit host.
- OrganisationProfile capabilities extend the established `identity-access` administration resource with distinct feature segments, matching Organization Directory integration.
- Missing Domain Registry integration fails closed.
- Pack 5 adds no HTTP controllers and no database migration.

### Deferred

- HTTP API routes and controller-level capability enforcement/audit emission remain Pack 6.
- TypeScript SDK follows the HTTP contract in Pack 6.
- Administration UI remains Pack 7.

## 0.4.2 — Source scan excludes generated build artifacts

### Fixed

- `verify-composition-separation.ps1` now excludes `bin` and `obj` directories from the one-type-per-source-file scan.
- Generated SDK files such as `.NETCoreApp,Version=v10.0.AssemblyAttributes.cs` are no longer interpreted as authored OrganisationProfile source files.
- The repository source-layout invariant continues to apply to authored C# files only.

## 0.4.1 — Source layout qualification fix

### Fixed

- Removed nested persistence record declarations rejected by repository `SourceLayoutTests`.
- Moved template-version row state to `OrganisationProfileTemplateVersionRow.cs`.
- Moved effective-version row state to `OrganisationProfileVersionRow.cs`.
- Moved effective-version writer lock state to `OrganisationProfileLockState.cs`.
- Moved latest semantic-version identity to `OrganisationProfileLatestVersionIdentity.cs`.
- Updated reader/writer references without changing persistence or runtime behavior.
- Strengthened the Pack 4 source-separation gate to require exactly one declared type per C# source file, filename/type matching, and block-scoped namespaces.

## 0.4.0 — Domain composition and immutable effective profile versions

### Added

- Narrow `IDomainRegistryReader` boundary with Published/Retired domain-version states.
- `OrganisationProfileDomainRegistryValidator` separating new-selection policy from historical resolution policy.
- Pure `OrganisationProfileCompositionResolver` for template + override composition.
- Deterministic `OrganisationProfileEffectiveContentHasher`.
- Focused `OrganisationProfileDomainOverrideService`.
- Focused `OrganisationProfileCompositionService`.
- Durable `organisation_profile.organisation_profile_domain_overrides`.
- Immutable `organisation_profile.organisation_profile_versions` and version-domain snapshots.
- Parent-profile RowVersion advancement when the complete override set changes.
- Profile-specific serialization of effective snapshot publication with stale-profile rejection.
- Idempotent effective snapshot publication when current semantic content is unchanged.
- New semantic profile version when effective content changes or later reverts.
- Database immutability triggers for effective version rows and effective version-domain rows.
- Live composition probe covering override replacement, stale-write rejection, Published-vs-Retired Domain Registry rules, deterministic resolution, snapshot idempotency, version history, and historical resolution.
- Pack 3 template publication now validates exact domain versions through the external Domain Registry boundary before publishing.

### Architecture

- Domain Registry persistence remains external to OrganisationProfile.
- Template publication requires Published domain versions.
- New Organization-specific Enable overrides require Published domain versions.
- Effective resolution accepts both Published and Retired historical domain versions.
- No `DomainRegistry` implementation project dependency is introduced.
- No composition manager/god service is introduced.

### Deferred

- HTTP API and external administration authorization remain later packs.
- TypeScript SDK and administration UI remain later packs.
- Provider Registry integration remains explicitly outside the OrganisationProfile core.

## 0.3.0 — Template catalog and immutable publication

### Added

- Reusable template-definition persistence and lifecycle.
- Draft template versions with complete domain-composition replacement.
- Deterministic SHA-256 publication hash over template key/version and key-ordered version-pinned domains.
- Immutable Published content and explicit Published-to-Retired lifecycle.
- `organisation_profile` tables for templates, versions, and version domains.
- Published-only profile pin enforcement and foreign-key integrity to template versions.
- Focused definition, draft, and publication services instead of a template manager.
- Separate live template-catalog probe and schema verification.

### Changed

- The profile persistence probe no longer creates an arbitrary pre-catalog template pin.
- `IdentityAccess.sln` now includes `OrganisationProfile.TemplateCatalogProbe`.

### Deferred

- Domain Registry compatibility validation and effective profile resolution remain Pack 4.
- API, authorization integration, TypeScript, and UI remain later packs.

## 0.2.1 — Namespace/type collision qualification fix

### Fixed

- Added an explicit `OrganisationProfileAggregate` alias for the `OrganisationProfile.Domain.OrganisationProfile` aggregate in executable probe projects.
- Prevented the root `OrganisationProfile` namespace from being resolved instead of the aggregate type inside `OrganisationProfile.*` probe namespaces.
- Applied the same alias rule to both the foundation probe and PostgreSQL probe to prevent the collision from recurring during Pack 2 qualification.

## 0.2.0 — PostgreSQL lifecycle persistence

### Added

- `OrganisationProfile.Infrastructure.PostgreSql` with direct Npgsql persistence.
- `organisation_profile` schema ownership and checksum-protected migration metadata.
- `organisation_profile.organisation_profiles` lifecycle table.
- One-profile-per-Organization durable uniqueness.
- Same-database foreign-key integrity to `organization_directory.organizations`.
- Optimistic-concurrency update semantics with stale-write translation.
- PostgreSQL implementation of the narrow `IOrganizationReferenceReader`.
- Live PostgreSQL probe covering create/read/list/update/disable, template pin persistence, duplicate-Organization rejection, stale-write rejection, and cleanup.
- Integrated `.NET` solution entries for Domain, Application, PostgreSQL infrastructure, foundation probe, and PostgreSQL probe.
- PowerShell schema-apply, migration-integrity, schema-validation, store-probe, and Pack 2 verification gates.

### Deferred

- Profile-template catalog tables and publication lifecycle remain Pack 3.
- Domain composition persistence and deterministic resolver remain Pack 4.
- API, security integration, TypeScript, and UI remain later packs.

## 0.1.0 — Foundation and contract freeze

### Added

- module-owned `OrganisationProfile` domain foundation.
- Stable `OrganisationProfileId` and external `OrganizationReference`.
- Explicit profile lifecycle and optimistic-concurrency row-version contract.
- Reusable profile-template key, version, status, and immutable published-version concepts.
- Version-pinned domain selections and explicit enable/disable override semantics.
- Distinct semantic `OrganisationProfileVersionNumber`.
- Deterministic effective-profile and published-template read models with duplicate-domain rejection and stable key ordering.
- Narrow `IOrganizationReferenceReader` integration boundary without Organization Directory implementation dependencies.
- Framework-free executable foundation probe.
- Source-layout, dependency-boundary, forbidden-concept, and anti-god-service verification.
- Architecture and invariant documentation.

### Excluded

- PostgreSQL persistence.
- Identity Access authorization.
- API endpoints.
- TypeScript SDK.
- OrganisationProfile UI.
- Provider credentials or provider-specific configuration.
- Domain Registry persistence or domain business rules.
