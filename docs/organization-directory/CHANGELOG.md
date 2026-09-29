# Organization Directory changelog

## 0.8.2 — Focused ResourceScope-link action contract alignment

### Fixed

- Aligned `linkOrganizationResourceScopeAction`, `relinkOrganizationResourceScopeAction`, and `unlinkOrganizationResourceScopeAction` with the focused `IdentityAccessAdminOrganizationScopeLinkMutationService` contract.
- Server actions now call the service's `link`, `relink`, and `unlink` methods directly.
- Added an architecture regression test preventing action and focused-service method names from drifting apart.

## 0.8.0 — Separation hardening and qualification

### Changed

- Split Organization Directory mutations out of the broad `IdentityAccessAdminMutationService` into focused definition/lifecycle, membership-reconciliation, and ResourceScope-link mutation services.
- Split Organization Directory reads out of `IdentityAccessAdminMembershipOverviewService` into a focused `IdentityAccessAdminOrganizationOverviewService`.
- Refactored the Organization Directory UI composition into focused create, hierarchy-table, row-action, and ResourceScope-link components while keeping the feature embedded in Identity Membership.
- Kept the generic Identity Access mutation/read services free from Organization-specific client calls.
- Corrected the Organization Directory schema-bootstrap diagnostic path.

### Added

- Architecture regression tests for reusable project layering and focused API-controller dependencies.
- UI/server separation tests that prevent Organization operations from regressing into broad god services.
- A dedicated source-separation qualification gate with bounded focused-component checks.
- A PostgreSQL qualification orchestrator covering migration integrity, hierarchy, memberships, ResourceScope links, and the live store probe.
- A top-level Organization Directory qualification runner that reuses existing repository/module gates without replacing central verification scripts.
- `docs/organization-directory/QUALIFICATION.md` with full and partial qualification semantics.

### Architecture

- Organization Directory remains inside the existing Identity Access repository, API, database, and Membership UI.
- No separate `/identity/organizations` administration application is introduced.
- Organization membership remains belonging only; authorization continues through managed policies, ResourceScopes, and external RBAC.

## 0.7.1 — Protected ResourceScope reference selection

### Fixed

- Replaced editable Organization ResourceScope raw select fields with the shared server-backed `AdminEntityAutocomplete`.
- Removed preloading of the tenant ResourceScope catalog into the Membership page solely for link/relink forms.
- ResourceScope link and relink forms now submit only the stable selected `resourceScopeId`; the server continues to validate tenant/application ownership and active status.
- Added an architecture regression test preventing Organization ResourceScope administration from falling back to raw select fields or browser-preloaded scope catalogs.

## 0.7.0 — TypeScript connector and integrated Identity Membership UI

### Added

- Class-based TypeScript clients for Organizations, OrganizationMemberships and Organization-to-ResourceScope links.
- Organization hierarchy, lifecycle, membership and ResourceScope-link administration inside the existing `/identity/memberships` workspace.
- Member-centric Organization assignment using explicit OrganizationMembership reconciliation.
- Tenant Organization directory management with generic type, parent hierarchy and lifecycle state.
- ResourceScope link/relink/unlink controls that reuse existing Identity Access ResourceScopes.
- Client transport tests and architecture guards for the integrated UI boundary.

### Architecture

- No second Next.js application, host, project selector, or `/identity/organizations` application is introduced.
- Organization administration remains part of the existing Identity Access admin host and is embedded in Identity Membership.
- OrganizationMembership remains belonging only; GroupMembership, Managed Policies, ResourceScopes and RBAC remain the authorization path.
- Browser components never receive or evaluate authorization tokens; server actions and the .NET API remain authoritative.

## 0.6.2 — Development bootstrap PowerShell compatibility

### Fixed

- Removed the unsupported `ConvertFrom-Json -Depth` argument from the development administrator bootstrap for Windows PowerShell 5.1 compatibility.
- Replaced .NET-only `SHA256.HashData` and `Convert.ToHexString` calls with framework-compatible SHA-256 fingerprint generation.
- Made the bootstrap derive `ModelVersion` from the selected security manifest when `-ModelVersion` is omitted.
- Preserved fail-closed validation when an explicit `-ModelVersion` does not match the manifest.

## 0.6.0 — Administration security and audit integration

### Added

- Published administration capabilities for `organization`, `organization-membership` and `organization-scope-link`.
- Dedicated Organization ResourceScope-link authorization separate from ordinary Organization definition administration.
- Administration security manifest version 3 with immutable capability-catalog evolution.
- Best-effort semantic security audit events for Organization lifecycle, OrganizationMembership lifecycle and ResourceScope-link mutations.
- Organization Directory audit events routed through the existing Identity Access security-audit pipeline.
- Source-consistency and architecture tests that protect capability metadata, fail-closed controller authorization and audit wiring.

### Security

- All Organization Directory administration remains server-authorized through `RequireAdministrationCapability`.
- ResourceScope-link administration no longer borrows generic `organization/write`; it uses the dedicated `organization-scope-link` capability.
- Semantic audit failure never converts a successful primary mutation into a failed business operation, matching the existing Identity Access audit contract.
- The security manifest is advanced from model version 2 to model version 3 rather than mutating an already registered immutable model.
- Existing model-version-2 ResourceScopes remain valid durable resources; model version 3 only evolves the administration capability catalog.

## 0.5.0 — Identity Access ResourceScope integration

### Added

- Durable application-aware Organization-to-ResourceScope linkage.
- Foreign-key integrity to `identity_access.resource_scopes` without changing Identity Access-owned schema.
- Authoritative ResourceScope metadata reads from Identity Access, including type, security-model version and active status.
- Create, read, relink and remove ResourceScope-link administration through the existing `IdentityAccess.Api`.
- One Organization-to-scope mapping per application and one Organization owner per ResourceScope/application boundary.
- Optimistic concurrency for link replacement and removal.
- Transactional PostgreSQL validation for Identity Access ResourceScope compatibility.
- Application tests for missing/inactive ResourceScopes, inactive Organizations, uniqueness and relinking.

### Security

- Organization Directory never creates authorization grants from Organization membership or Organization hierarchy.
- Link creation and replacement require an existing active Identity Access ResourceScope in the same Identity Scope, Tenant and application.
- `scope_type` and security-model version are read from Identity Access rather than trusted from caller input.
- ResourceScope linkage reuses existing `organization` administration authorization; delegated fine-grained security policy remains a later security pack.

## 0.4.0 — Explicit organization membership

### Added

- Durable `organization_directory.organization_memberships` persistence.
- Foreign-key validation against both tenant-local Organizations and existing `identity_access.tenant_memberships`.
- Explicit `OrganizationMembership` lifecycle with Active/Suspended state and optimistic concurrency.
- Organization-centric and tenant-member-centric membership reads.
- OrganizationMembership administration endpoints hosted by the existing `IdentityAccess.Api`.
- Dedicated `identity-access / organization-membership / read|write` administration capability metadata.
- Live PostgreSQL membership validation and Npgsql store probe coverage.
- Application-service tests covering active/inactive tenant membership, inactive organizations, duplicate membership, lifecycle and removal.

### Security

- Organization membership establishes belonging only; it does not grant Managed Policies, GroupMemberships or RBAC authority.
- New or reactivated OrganizationMemberships require a currently active Identity Access TenantMembership.
- Cross-tenant membership is blocked in the domain model and by PostgreSQL foreign keys.
- Removing OrganizationMembership never deletes the Identity Access User or TenantMembership.

## 0.3.3 — API compile compatibility

### Fixed

- Replaced the invalid nullable-`Guid` pattern in organization creation with explicit safe pattern matching.
- Restored the shared `ApiProblems.UnprocessableEntity(...)` helper required by resource-scope-aware group-template cloning.
- Preserved the Pack 3 `OrganizationAdministrationUnavailable()` problem response in the same HTTP problem catalog.
- Prevented downstream `IdentityAccess.Api.dll` metadata-file errors caused by API compilation failures.

## 0.3.2 — Parent identifier nullable conversion

### Fixed

- Unwrap the validated nullable `OrganizationId?` before constructing a parent `OrganizationReference`.
- Restore compilation of `OrganizationAdministrationService.ResolveAndValidateParentAsync`.
- Prevent downstream metadata-file errors caused by the Organization Directory application project failing to compile.

## 0.3.1 — Hierarchy tree key typing

### Fixed

- Removed nullable `OrganizationId?` dictionary keys from organization tree materialization.
- Materialize root organizations separately and index only non-root children by non-null `OrganizationId`.
- Restored compatibility with the `notnull` generic constraint used by `Dictionary<TKey, TValue>` / `Enumerable.ToDictionary`.

## 0.3.0 — Organization administration API and hierarchy

### Added

- Organization lifecycle application service with create, read, update, enable and disable operations.
- Tenant-local direct-child and bounded full-tree hierarchy reads.
- Application-level parent existence checks and descendant-cycle prevention before persistence.
- Organization administration endpoints hosted by the existing `IdentityAccess.Api`.
- Dedicated `identity-access / organization / read|write` administration capability metadata.
- Fail-closed Organization Directory feature registration when shared PostgreSQL is not configured.
- Stable RFC 7807 mappings for organization concurrency, hierarchy, identity, key and missing-reference failures.
- Application-service and API architecture tests.

### Changed

- Pack 3 does not introduce a second web host; Organization Directory remains integrated into `IdentityAccess.Api`.
- Organization keys remain immutable through the public update contract.
- Organization deletion is intentionally not exposed; lifecycle uses enable/disable operations.
- Delegated tenant assignment for the new organization capability remains a later security-administration concern; scope-wide wildcard administration can exercise the API during this pack.

## 0.2.1 — PostgreSQL hierarchy-delete error translation

### Fixed

- Translate PostgreSQL `restrict_violation` (`SQLSTATE 23001`) raised by the tenant-local parent `ON DELETE RESTRICT` constraint into `OrganizationHierarchyConflictException`.
- Keep parent-with-children deletion as an expected domain conflict instead of leaking a raw `PostgresException` from the persistence layer.
- Restore the live PostgreSQL store probe expectation for protected parent deletion.

## 0.2.0 — Shared PostgreSQL persistence

- Integrated Organization Directory projects into `IdentityAccess.sln`.
- Moved persistence to the existing `generic_identity_access_default` database.
- Kept independent schema ownership under `organization_directory`.
- Added `organizations` persistence, optimistic concurrency and hierarchy cycle protection.
- Added cross-schema FK to `identity_access.tenants`.
- Added independent Organization Directory migration checksums.
- Added live Npgsql store qualification using an existing active Identity Access tenant.
- Moved module scripts under `scripts/organization-directory` to avoid collisions.
- Removed the separate Organization Directory API host from the integrated plan; Pack 3 will use `IdentityAccess.Api`.

## 0.1.0 — Foundation and contracts

- Added generic Organization, OrganizationMembership and resource-scope-link contracts.
- Added application-agnostic hierarchy and lifecycle invariants.
