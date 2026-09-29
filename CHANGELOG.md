# 0.2.0 - PostgreSQL organization persistence

- Added checksum-protected PostgreSQL migration metadata under the owned `organization_directory` schema.
- Added immutable migration `0001_organizations.sql` with tenant-isolated organization identity, tenant-local key uniqueness and parent foreign keys.
- Added database-enforced deep hierarchy-cycle rejection and parent deletion protection.
- Added `IOrganizationStore` and the Npgsql-backed `PostgreSqlOrganizationStore` with get, tenant-local key lookup, bounded listing, create, optimistic update and controlled delete persistence operations.
- Added stable duplicate-key, hierarchy-conflict and stale-row concurrency failure contracts without leaking PostgreSQL exceptions into the application boundary.
- Added migration application and integrity scripts plus transactional organization hierarchy/concurrency qualification.
- Added a live Npgsql persistence probe covering create/read/list/update/stale-write/cycle/delete behavior through `PostgreSqlOrganizationStore`.
- Added PostgreSQL schema/concurrency documentation and expanded repository validation guidance.
- OrganizationMembership persistence, resource-scope-link persistence, administration API, real Identity Access integration and UI remain intentionally outside this version.

# 0.1.0 - Organization identity foundation and contracts

- Created the Generic Organization Directory repository baseline on .NET 10 with separate Domain, Contracts, Application, API, PostgreSQL infrastructure, and test projects.
- Added stable identity-scope, tenant, organization, tenant-membership, resource-scope, application, scope-type, and security-model-version value objects with explicit validation.
- Added immutable `Organization` state with canonical organization keys, application-defined organization types, tenant-local parent validation, lifecycle status, timestamps, and optimistic concurrency versioning.
- Added explicit `OrganizationMembership` as an organizational-belonging relationship over an external tenant membership; the domain does not treat organization membership as an authorization grant.
- Added application-aware `OrganizationResourceScopeLink` so external authorization scope linkage remains separate from organization identity and can vary by application security model.
- Added transport-neutral organization, membership, and resource-scope-link contracts without consumer-specific business semantics.
- Added a minimal ASP.NET Core host with liveness and service-information endpoints while deferring organization administration routes until persistence/application services are implemented.
- Added source-layout and domain-independence gates plus focused domain tests for key syntax, parent isolation, membership isolation, resource-scope isolation, and concurrency invariants.
- Added architecture, validation, implementation-roadmap, README, and standard verification documentation.
- PostgreSQL migrations, hierarchy-cycle persistence validation, real Identity Access integration, administration authorization, TypeScript client, and administration UI remain intentionally outside this version.
