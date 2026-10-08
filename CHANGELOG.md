# Generic Identity 1.5.0 — Administration Integration Qualification

**Date:** 2026-10-07

- Completed reusable Next.js administration workflows and React presentation surfaces for tenants, users, memberships, organizations, groups, managed policies, resource scopes, delegated authority, security audit, and session security.
- Added bounded, server-backed entity reference autocomplete for users, tenants, tenant memberships, managed policies, resource scopes, authority groups, and authority policies.
- Added application-security manifest upload and scope-type administration workflows while preserving server-side model validation and registration authority.
- Added complete user credential administration, membership reconciliation, organization/resource-scope linking, managed-policy publication, delegated-authority membership/policy/binding administration, and optimistic-concurrency handling through reusable server workflows.
- Clarified the separation between tenant Groups / Managed Policies and identity-scope Delegated Authority. Administrative authority is not duplicated into tenant authorization catalogs.
- Expanded Security Audit with bounded filters, summary metrics, correlation navigation, and secret-safe event presentation.
- Reworked Sessions administration around bounded security evidence plus server-confirmed user/client revocation. The SDK does not infer an active-session inventory because no administrative session-list endpoint exists.
- Completed provider-neutral MFA administration composition for provider discovery, application policy, effective user MFA state, authenticator metadata, standard revocation, and recovery revocation. Dedicated live functional acceptance for MFA remains pending.
- Added source and behavior qualification for the administration workflows and updated consumer-integration validation to reflect the current canonical routes and package boundaries.
- Preserved the aligned public package version at `1.5.0`; no backend schema migration, RBAC bypass, authentication shortcut, or speculative backend API was introduced by this integration work.

# Generic Identity 1.5.0 — Security Operations

- Promoted existing session-revocation, MFA-administration, authenticator-lifecycle and security-audit capabilities into the categorized `identity.security` SDK surface.
- Added reusable React/Next.js Security Operations forms and audit presentation without moving privileged mutations into the browser.
- Preserved internal-only TOTP enrollment, WebAuthn registration and recovery-code generation as explicitly unavailable public APIs rather than inventing endpoints.
- Advanced the four aligned Generic Identity public packages to `1.5.0`.
- No backend endpoint, database migration, MFA/session semantic, RBAC semantic, source move or source deletion was introduced.

# Generic Identity 1.4.0 — Application Security

- Promoted the existing application security model, manifest registration, scope type, capability catalog and effective administration context surfaces into the categorized public SDK.
- Added reusable React/Next.js Application Security pages and forms while keeping routes and server actions consumer-owned.
- Added a structured permission-reference contract that validates declared namespaces/capabilities without manufacturing TRN strings or performing authorization.
- Advanced the four aligned Generic Identity public packages to `1.4.0`.
- Updated living SDK documentation to use named implementation milestones instead of numbered delivery labels.
- No backend endpoint, database migration, RBAC semantic, source move or source deletion was introduced.

# Shared Identity Integration — Access Control — Delegated-authority import correction

- Corrected the Access Control auth SDK import list so the active delegated-authority interfaces resolve `IdentityCreatePolicyRequest`, `IdentityUpdatePolicyRequest`, and `IdentityAddPolicyStatementRequest`.
- This is a compile-time correction only; no runtime, backend, database, or RBAC behavior changed.
- No files moved or deleted.

# Shared Identity Integration — Access Control — Delegated-authority contract completion

- Restored the generic create/update policy request contracts required by the active identity-scope delegated-authority surface.
- These request contracts are not the retired tenant-policy administration composition; they are reused by the active scope-authority client.
- Contracts/auth typechecks were re-run after the compatibility convergence.
- No source files moved or deleted; no backend/database/RBAC behavior changed.

# Shared Identity Integration — Access Control — Access-control compatibility convergence

- Reconciles Access Control with the already-closed managed-policy compatibility boundary.
- Keeps retired tenant-policy administration uncomposed and unexported.
- Accepts `RETIRED_COMPATIBILITY` in the SDK Inventory and Category Contract inventory status model.
- Keeps legacy tenant-policy React files only as inert `export {};` tombstones; no source files are deleted.
- Fixes `GroupAccessPage` to render only active managed-policy bindings.
- Fixes the Access Control source gate so `ManagedPolicyBindingForm` is not falsely rejected by the substring `PolicyBindingForm`.
- Validation-only/compatibility convergence; no database migration, backend behavior change, or RBAC bypass.

# Shared Identity Integration — Access Control — Managed-policy compatibility closure

- Corrected Access Control so the categorized Access Control SDK does not reactivate the retired tenant-policy administration model.
- Removed `IdentityAccessPoliciesClient` from administration composition and removed the accidental public legacy binding exports.
- Removed `tenantPolicies` from `GenericIdentityAccessControlClient`; active authorization administration remains managed-policy based.
- Converted Access Control tenant-policy pages/forms into inert compatibility tombstones without deleting files.
- Updated the feature inventory with explicit `RETIRED_COMPATIBILITY` status and taught the SDK Inventory and Category Contract validator that status.
- Updated GroupAccessPage to display active managed-policy bindings only.
- Validation-only/public-surface correction; backend persistence and RBAC semantics are unchanged.
- No files moved or deleted.

# Shared Identity Integration — Access Control SDK/UI

- Added the categorized `accessControl` public SDK surface for groups, tenant policies, managed policies, managed-policy bindings, resource scopes, delegated identity-scope authority and authorization evaluation.
- Promoted the existing tenant-policy client into the proven legacy administration client so the categorized SDK can reuse it without a second implementation.
- Added generic Access Control forms and pages while keeping routes and server actions consumer-owned.
- Updated the full SDK feature matrix: all backend-supported Access Control rows are now complete; effective-permission listing and permission explanation remain explicitly backend-missing.
- Advanced the four public Generic Identity packages together to `1.3.0`.
- No backend schema, RBAC semantics, files, or migrations were removed or moved.

# Shared Identity Integration — Organizations — PowerShell version-alignment gate

- Fixed the Organizations source validator under Windows PowerShell StrictMode when all four public package versions collapse to one scalar pipeline result.
- Materializes the unique version values into an explicit array before reading `.Count`.
- Validation-only change; Organizations SDK/UI source, runtime behavior, package versions, backend, database and RBAC behavior are unchanged.
- No files moved or deleted.

# Shared Identity Integration — Organizations SDK/UI

- Promoted Generic Organization Directory contracts and the proven Organization clients into a categorized public `organizations` SDK surface.
- Added complete Organization CRUD, hierarchy, enable/disable, membership lifecycle, tenant-membership lookup, and Organization-to-ResourceScope link surfaces.
- Added shared Organization list, detail, hierarchy, membership and edit/link UI components without introducing application-specific business semantics.
- Added public `organizations` subpaths across contracts, auth, React and Next.js packages and advanced the aligned package version to 1.2.0.
- Kept OrganisationProfile outside the Generic Identity SDK boundary; no backend, RBAC, database or authorization semantics changed.
- No files moved or deleted.

# Shared Identity Integration — Account and Directory SDK/UI

- Added consumer-neutral categorized Account and Directory public SDK surfaces over the proven backend/client operations.
- Promoted users and tenants to full CRUD, tenant memberships to full lifecycle, membership candidate lookup/add-by-login, tenant user projections and tenant group assignment reads.
- Added shared Password, Authentication Step-Up, Tenants, Tenant Details, Memberships and Membership Candidate React views.
- Preserved the existing authentication/authorization/administration compatibility surfaces with no backend or database migration.
- Advanced the four Generic Identity public packages additively to 1.1.0 and made release qualification derive the aligned shared package version dynamically.

# Shared Identity Integration - SDK Inventory and Category Contract - full SDK inventory and category contract freeze

- Froze a consumer-neutral seven-category model for the complete Generic Identity SDK: Account & Authentication, Directory, Organizations, Access Control, Application Security, Security Operations, and Protocol & Diagnostics.
- Added human-readable and machine-readable feature matrices grounded in the current API controllers, proven TypeScript client, public Generic Identity packages, and shared React pages.
- Classified incomplete surfaces explicitly as SDK, UI, backend API, or backend-contract gaps instead of inventing missing endpoints.
- Kept OrganisationProfile outside the categorized Generic Identity SDK boundary.
- Added a consumer-neutral naming gate for Generic Identity public/runtime source and the new SDK Inventory and Category Contract artifacts.
- No runtime behavior, package version, database schema, authentication, authorization or RBAC behavior changed. No files moved or deleted.

# Shared Identity Integration - Release Qualification - release packaging and versioning

- Froze `@generic-identity/contracts`, `@generic-identity/auth`, `@generic-identity/react` and `@generic-identity/next` at `1.0.0` for the first stable Shared Identity package boundary.
- Kept repository package manifests private to prevent accidental publication while staged release artifacts are explicitly made public and receive exact semver dependencies.
- Made the package artifact builder self-sufficient on clean checkouts and staged the transitional `@identity-access/client` package as a publishable compatibility dependency.
- Added release-artifact checksum, packed-manifest, dependency-boundary and forbidden-content qualification plus a generated `release-manifest.json`.
- Release Qualification qualifies artifacts only; it does not publish to a registry.
- No files moved or deleted; no database migration or authentication/RBAC behavior changed.

# Shared Identity Integration — Runtime and Security Qualification — consumer development administrator security-model registration

- Fixed the local consumer application administrator authority bootstrap to register the derived consumer application security model before inserting registration-backed RBAC namespaces.
- Computes the same deterministic security-manifest fingerprint shape used by the development admin bootstrap, but pins the local consumer application key and model version supplied to the consumer grant script.
- Keeps the existing fail-closed registration conflict behavior: an existing registration with a different fingerprint is rejected instead of silently rewritten.
- Strengthened source and post-bootstrap verification to require a matching application security model registration.
- The previous failed bootstrap used a single PostgreSQL transaction, so its partial inserts are rolled back and require no cleanup.
- No files moved or deleted; no database migration or production authorization behavior changed.

# Shared Identity Integration — Runtime and Security Qualification — consumer development administrator authority

- Added an explicit idempotent local-development bootstrap for the `admin` user in the `consumer-app` application context.
- Composes the shared Generic Identity administration capability set into the consumer development application model without copying authorization logic into the consumer application.
- Grants `identity-access/*/*` through the existing identity-scope administration group/policy/binding model; no authorization bypass is introduced.
- Keeps `admin-web` authority unchanged and separate.
- No files moved or deleted; no database migration or production authorization behavior changed.

# Shared Identity Integration — Runtime and Security Qualification — self-contained local package boundary

- Replaced `@generic-identity/contracts` source-relative escapes into `clients/typescript/src` with package-boundary type re-exports from `@identity-access/client`.
- Added repository-only TypeScript path mapping so the contracts package still typechecks against the proven local client without runtime coupling.
- Updated the local artifact builder so the packed contracts artifact declares the exact legacy client dependency required by the transitional local-consumer package graph.
- Bumped the four Generic Identity local artifact versions to `0.2.1` so npm cannot reuse stale `0.2.0` tarballs when the package contents change.
- Strengthened source gates to reject cross-package relative source imports and preserve package-boundary consumption.
- No files moved or deleted; no backend, database, RBAC, authentication, session, MFA or authorization behavior changed.

# Shared Identity Integration — Runtime and Security Qualification — Node 26 npm CLI spawning

- Replaced direct `npm.cmd` child-process spawning in the local package artifact builder with `node <npm-cli.js>` execution resolved from `npm_execpath` or the Node installation.
- Fixes Windows Node.js 26 `spawnSync npm.cmd EINVAL` while preserving the same npm build and artifact-generation commands and output artifacts.
- Strengthened the source gate so future changes cannot reintroduce direct `.cmd` spawning.
- No files moved or deleted; no backend, database, RBAC, authentication, session, MFA or authorization behavior changed.

# Shared Identity Integration — Runtime and Security Qualification — local artifact source gate

- Fixed the local-package artifact source validator to detect the actual local npm artifact command invocation instead of searching for an obsolete literal command fragment.
- Validation-only change; the local package builder and generated artifacts are unchanged.
- No files moved or deleted; no backend, database, RBAC, authentication, session, MFA or authorization behavior changed.

# Shared Identity Integration — Runtime and Security Qualification — Turbopack local package artifacts

- Added a generated local npm-package artifact builder for the legacy client and the four Generic Identity public packages.
- Rewrites only staged package manifests so local `file:` development dependencies become normal version dependencies inside the generated tarballs.
- Keeps repository source authoritative and untouched; generated tarballs are consumer artifacts, not copied source-of-truth code.
- Enables consumers to install real package copies inside their own `node_modules`, allowing Turbopack to keep its root constrained to the consumer project.
- No files moved or deleted; no backend, database, RBAC, authentication, session, MFA or authorization behavior changed.

# Shared Identity Integration — Runtime and Security Qualification — React Server/Client boundaries

- Added explicit `"use client"` boundaries to Generic Identity React modules that create/read React context or use client hooks, including the shared authorization gate and visual override primitives.
- Changed the Next.js shared-page entrypoint to re-export only `@generic-identity/react/pages` instead of the broad React package barrel, preventing Server Components from traversing unrelated client-only hooks.
- Narrowed `NextIdentityProvider` imports to the provider and visual public subpaths.
- Added source gates that prevent removal of these App Router/RSC boundaries.
- No files moved or deleted; no backend, database, RBAC, authentication, session, MFA or authorization behavior changed.

# Shared Identity Integration — Runtime and Security Qualification — linked auth development dependencies

- Added deterministic restore of `@generic-identity/auth` local file dependencies during repository verification so external source-linked Next.js consumers can resolve `@identity-access/client` and `@generic-identity/contracts` from the real auth package path.
- Kept the existing legacy TypeScript client as the runtime bridge and ensured its `dist` output is built before the auth package dependency restore.
- No files moved or deleted; no backend, database, RBAC, authentication, session, MFA or authorization behavior changed.

# Shared Identity Integration — Runtime and Security Qualification — linked raw-source module specifiers

- Normalized relative imports/exports inside `@generic-identity/contracts`, `@generic-identity/auth`, `@generic-identity/react` and `@generic-identity/next` from emitted `.js` specifiers to extensionless source specifiers so Turbopack can resolve the linked TypeScript source before Release Qualification emits publishable JavaScript artifacts.
- Switched the raw-source contracts, auth and React package typechecks to `ESNext` + `Bundler`, matching the already-qualified Next package and the actual pre-publication linked-source consumption model.
- Updated historical source gates and added a closure gate that rejects relative `.js` specifiers inside linked raw-source packages.
- No files moved or deleted; no backend, database, RBAC, authentication or session behavior changed.

# Shared Identity Integration — Runtime and Security Qualification — consumer local development client

- Added a second local authentication client, `consumer-web`, without changing or replacing the existing `admin-web` client.
- Added trusted local routing and authentication-context entries for application key `consumer-app` on the existing development identity scope and PostgreSQL destination.
- Added a source gate for the consumer local development client configuration.
- No files moved or deleted; no database migration or production authentication behavior changed.

# Shared Identity Integration — Consumer Bridge — Consumer administration bridge

- Added a read-focused Generic Identity administration facade for shared users, groups, managed policies and MFA page data without copying endpoint logic into consumers.
- Added Next.js server helpers that construct trusted administration contexts from the existing HTTP-only session credential.
- Added runtime source export conditions for local pre-publication `file:` consumption of the auth, React and Next packages.
- Added passive tenant-user contracts required by consumer-facing user lists.
- Kept the existing legacy TypeScript client as the runtime implementation bridge; no second authentication, authorization or administration engine was introduced.
- No files moved or deleted; no backend/runtime behavior or database schema changed.

# Shared Identity Integration - Next.js Integration Fix 02

- Enabled `skipLibCheck` for the standalone `@generic-identity/next` package typecheck so Generic Identity source is checked strictly without re-typechecking Next.js own declaration files.
- Kept `strict`, `exactOptionalPropertyTypes`, `noEmit`, ESNext modules and Bundler resolution unchanged.
- Updated the Next.js Integration source gate to require this library-package compatibility setting.
- No runtime, backend, database, move, or delete changes.

# Shared Identity Integration - Next.js Integration Fix 01

- Switched `@generic-identity/next` TypeScript resolution from `NodeNext` to `ESNext` + `Bundler` so Next.js package subpaths such as `next/headers` resolve during standalone package typechecking.
- Updated the Next.js Integration source gate to lock the supported Next.js TypeScript resolution mode.
- No runtime, backend, database, move, or delete changes.

# Shared Identity Integration — Next.js Integration

- Activated private `@generic-identity/next` as the reusable Next.js-specific integration boundary.
- Added an explicit Next.js client-component wrapper over the shared React `IdentityProvider`.
- Added server-only opaque-session cookie coordination using HTTP-only, SameSite=Lax cookies and server-side session validation.
- Added server-side capability evaluation and protected-page helpers that delegate to the existing Generic Identity auth/RBAC path and preserve DENY versus technical failure semantics.
- Added consumer-owned route-map helpers and shared Identity page re-exports without imposing public URLs or navigation.
- Declared Next.js, React and React DOM as peer dependencies to avoid duplicate framework instances in consuming applications.
- Kept the existing `examples/nextjs/admin` host untouched; consumer migration is deferred to the dedicated integration milestone.
- No files moved or deleted; no backend/runtime behavior or database schema changed.

# Shared Identity Integration — Theme and Component Overrides — Theming and visual component overrides

- Fixed the Theme and Component Overrides verification script for Windows PowerShell 5.1 by removing C-style escaped quote literals and non-ASCII parser-sensitive markers.
- Fixed historical React Foundation, Shared React Pages and Theme and Component Overrides source gates so later monotonic `@generic-identity/react` version bumps do not invalidate already-qualified earlier milestones.
- Added the first public Generic Identity theme contract with documented `--gi-*` CSS custom properties, stable `gi-*` classes and `data-gi-*` hooks.
- Added `IdentityThemeRoot` and exported `@generic-identity/react/theme.css` for default styling without forcing a consumer design system.
- Added optional `IdentityProvider.components` visual overrides for Button, Input, Panel and Table rendering.
- Added `IdentityButton` and `IdentityInput` primitives and migrated the shared sign-in/recovery forms to those primitives without changing authentication behavior.
- Made `IdentityPanel` and `IdentityTable` visual implementations replaceable while retaining safe default rendering when no provider is present.
- Added theme/component-override consumer compile coverage and Theme and Component Overrides architecture/source gates.
- Bumped the private `@generic-identity/react` extraction package to `0.2.0`; publication remains deferred.
- No files moved or deleted; no backend/runtime behavior or database schema changed.

# Shared Identity Integration — Shared React Pages — Shared React pages

- Added presentation-only shared Identity pages for sign-in, recovery, account/profile, users, groups, policies, sessions, MFA and security composition.
- Added reusable semantic page primitives (`IdentityPageFrame`, `IdentityPanel`, `IdentityTable`, `IdentityStatus`, `IdentityEmptyState`) with stable `gi-*` and `data-gi-*` styling hooks.
- Kept Next.js routing, server sessions, protected mutations and consumer branding outside the shared React package.
- Recorded current Next.js host pages as future cleanup candidates only; no source was moved or deleted.

# Shared Identity Integration — React Foundation — React foundation

- Activated private `@generic-identity/react` over the shared contracts and auth packages.
- Added `IdentityProvider`, React hooks and `RequireCapability` as a UI-only authorization gate backed by the existing server authorization path.
- Kept React/React DOM as peer dependencies and prohibited Next.js coupling in the React package.
- Added source/type gates and corrected the React Foundation source marker so harmless TypeScript line wrapping does not fail validation.
- No existing administration host integration was replaced.

# Shared Identity Integration — Authentication and Authorization SDK — Authentication and authorization SDK

- Activated private `@generic-identity/auth` as a framework-neutral facade over the proven TypeScript authentication and server-backed authorization implementation.
- Added shared client/session/authentication helpers, authorization context, capability evaluation and common client error translation.
- Preserved the distinction between authorization DENY and technical failure.
- Kept `@identity-access/client` as the temporary authoritative runtime bridge; no second RBAC or authentication system was introduced.
- Added only missing public type exports to the legacy client; no implementation was moved.

# Shared Identity Integration — Public Contracts — Public contracts

- Activated private `@generic-identity/contracts` as a passive type-only boundary for shared identity, administration, authorization, policies, sessions, MFA, security manifests and public errors.
- Kept secrets, token-bearing requests, PostgreSQL/Redis internals, Organization Directory and OrganisationProfile contracts outside the shared package.
- Added consumer typecheck and source-boundary validation while leaving existing runtime behavior unchanged.

# Shared Identity Integration — Baseline and Structure — Baseline and structure gates

- Froze the existing GREEN repository structure before extraction work.
- Reserved `packages/contracts`, `packages/auth`, `packages/react` and `packages/next` boundaries without moving existing implementation.
- Added shared-identity structure/integration verification to the existing repository verification entry point.
- Explicitly kept the existing TypeScript client, Next.js administration host and server projects authoritative.
- No runtime behavior, database schema, source relocation or deletion was introduced.

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
