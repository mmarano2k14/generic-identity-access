# Shared Identity Integration - Pack 10 - release packaging and versioning

- Froze `@generic-identity/contracts`, `@generic-identity/auth`, `@generic-identity/react` and `@generic-identity/next` at `1.0.0` for the first stable Shared Identity package boundary.
- Kept repository package manifests private to prevent accidental publication while staged release artifacts are explicitly made public and receive exact semver dependencies.
- Made the package artifact builder self-sufficient on clean checkouts and staged the transitional `@identity-access/client` package as a publishable compatibility dependency.
- Added release-artifact checksum, packed-manifest, dependency-boundary and forbidden-content qualification plus a generated `release-manifest.json`.
- Pack 10 qualifies artifacts only; it does not publish to a registry.
- No files moved or deleted; no database migration or authentication/RBAC behavior changed.

# Shared Identity Integration - Pack 9 Fix 07.1 - MAGELLAN dev admin security-model registration

- Fixed the local MAGELLAN administrator authority bootstrap to register the derived MAGELLAN application security model before inserting registration-backed RBAC namespaces.
- Computes the same deterministic security-manifest fingerprint shape used by the development admin bootstrap, but pins the local consumer application key and model version supplied to the MAGELLAN grant script.
- Keeps the existing fail-closed registration conflict behavior: an existing registration with a different fingerprint is rejected instead of silently rewritten.
- Strengthened source and post-bootstrap verification to require a matching application security model registration.
- The previous failed bootstrap used a single PostgreSQL transaction, so its partial inserts are rolled back and require no cleanup.
- No files moved or deleted; no database migration or production authorization behavior changed.

# Shared Identity Integration - Pack 9 Fix 07 - MAGELLAN development administrator authority

- Added an explicit idempotent local-development bootstrap for the `admin` user in the `magellan` application context.
- Composes the shared Generic Identity administration capability set into the MAGELLAN development application model without copying authorization logic into MAGELLAN.
- Grants `identity-access/*/*` through the existing identity-scope administration group/policy/binding model; no authorization bypass is introduced.
- Keeps `admin-web` authority unchanged and separate.
- No files moved or deleted; no database migration or production authorization behavior changed.

# Shared Identity Integration - Pack 9 Fix 06A.3 - self-contained local package boundary

- Replaced `@generic-identity/contracts` source-relative escapes into `clients/typescript/src` with package-boundary type re-exports from `@identity-access/client`.
- Added repository-only TypeScript path mapping so the contracts package still typechecks against the proven local client without runtime coupling.
- Updated the local artifact builder so the packed contracts artifact declares the exact legacy client dependency required by the transitional local-consumer package graph.
- Bumped the four Generic Identity local artifact versions to `0.2.1` so npm cannot reuse stale `0.2.0` tarballs when the package contents change.
- Strengthened source gates to reject cross-package relative source imports and preserve package-boundary consumption.
- No files moved or deleted; no backend, database, RBAC, authentication, session, MFA or authorization behavior changed.

# Shared Identity Integration - Pack 9 Fix 06A.2 - Node 26 npm CLI spawning

- Replaced direct `npm.cmd` child-process spawning in the local package artifact builder with `node <npm-cli.js>` execution resolved from `npm_execpath` or the Node installation.
- Fixes Windows Node.js 26 `spawnSync npm.cmd EINVAL` while preserving the same npm build/pack commands and output artifacts.
- Strengthened the source gate so future changes cannot reintroduce direct `.cmd` spawning.
- No files moved or deleted; no backend, database, RBAC, authentication, session, MFA or authorization behavior changed.

# Shared Identity Integration - Pack 9 Fix 06A.1 - local artifact source gate

- Fixed the local-package artifact source validator to detect the actual `runNpm(["pack", ...])` builder call instead of searching for a non-existent `npm", "pack` text fragment.
- Validation-only change; the local package builder and generated artifacts are unchanged.
- No files moved or deleted; no backend, database, RBAC, authentication, session, MFA or authorization behavior changed.

# Shared Identity Integration - Pack 9 Fix 06A - Turbopack local package artifacts

- Added a generated local npm-package artifact builder for the legacy client and the four Generic Identity public packages.
- Rewrites only staged package manifests so local `file:` development dependencies become normal version dependencies inside the generated tarballs.
- Keeps repository source authoritative and untouched; generated tarballs are consumer artifacts, not copied source-of-truth code.
- Enables consumers to install real package copies inside their own `node_modules`, allowing Turbopack to keep its root constrained to the consumer project.
- No files moved or deleted; no backend, database, RBAC, authentication, session, MFA or authorization behavior changed.

# Shared Identity Integration - Pack 9 Fix 05 - React Server/Client boundaries

- Added explicit `"use client"` boundaries to Generic Identity React modules that create/read React context or use client hooks, including the shared authorization gate and visual override primitives.
- Changed the Next.js shared-page entrypoint to re-export only `@generic-identity/react/pages` instead of the broad React package barrel, preventing Server Components from traversing unrelated client-only hooks.
- Narrowed `NextIdentityProvider` imports to the provider and visual public subpaths.
- Added source gates that prevent removal of these App Router/RSC boundaries.
- No files moved or deleted; no backend, database, RBAC, authentication, session, MFA or authorization behavior changed.

# Shared Identity Integration - Pack 9 Fix 04 - linked auth development dependencies

- Added deterministic restore of `@generic-identity/auth` local file dependencies during repository verification so external source-linked Next.js consumers can resolve `@identity-access/client` and `@generic-identity/contracts` from the real auth package path.
- Kept the existing legacy TypeScript client as the runtime bridge and ensured its `dist` output is built before the auth package dependency restore.
- No files moved or deleted; no backend, database, RBAC, authentication, session, MFA or authorization behavior changed.

# Shared Identity Integration - Pack 9 Fix 03 - linked raw-source module specifiers

- Normalized relative imports/exports inside `@generic-identity/contracts`, `@generic-identity/auth`, `@generic-identity/react` and `@generic-identity/next` from emitted `.js` specifiers to extensionless source specifiers so Turbopack can resolve the linked TypeScript source before Pack 10 emits publishable JavaScript artifacts.
- Switched the raw-source contracts, auth and React package typechecks to `ESNext` + `Bundler`, matching the already-qualified Next package and the actual pre-publication linked-source consumption model.
- Updated historical source gates and added a closure gate that rejects relative `.js` specifiers inside linked raw-source packages.
- No files moved or deleted; no backend, database, RBAC, authentication or session behavior changed.

# Shared Identity Integration - Pack 9 Fix 01 - MAGELLAN local development client

- Added a second local authentication client, `magellan-ux`, without changing or replacing the existing `admin-web` client.
- Added trusted local routing and authentication-context entries for application key `magellan` on the existing development identity scope and PostgreSQL destination.
- Added a source gate for the MAGELLAN local development client configuration.
- No files moved or deleted; no database migration or production authentication behavior changed.

# Shared Identity Integration — Pack 7 Closure — Consumer administration bridge

- Added a read-focused Generic Identity administration facade for shared users, groups, managed policies and MFA page data without copying endpoint logic into consumers.
- Added Next.js server helpers that construct trusted administration contexts from the existing HTTP-only session credential.
- Added runtime source export conditions for local pre-publication `file:` consumption of the auth, React and Next packages.
- Added passive tenant-user contracts required by consumer-facing user lists.
- Kept the existing legacy TypeScript client as the runtime implementation bridge; no second authentication, authorization or administration engine was introduced.
- No files moved or deleted; no backend/runtime behavior or database schema changed.

# Shared Identity Integration - Pack 7 Fix 02

- Enabled `skipLibCheck` for the standalone `@generic-identity/next` package typecheck so Generic Identity source is checked strictly without re-typechecking Next.js own declaration files.
- Kept `strict`, `exactOptionalPropertyTypes`, `noEmit`, ESNext modules and Bundler resolution unchanged.
- Updated the Pack 7 source gate to require this library-package compatibility setting.
- No runtime, backend, database, move, or delete changes.

# Shared Identity Integration - Pack 7 Fix 01

- Switched `@generic-identity/next` TypeScript resolution from `NodeNext` to `ESNext` + `Bundler` so Next.js package subpaths such as `next/headers` resolve during standalone package typechecking.
- Updated the Pack 7 source gate to lock the supported Next.js TypeScript resolution mode.
- No runtime, backend, database, move, or delete changes.

# Shared Identity Integration - Pack 7 - Next.js integration package

- Activated private `@generic-identity/next` as the reusable Next.js-specific integration boundary.
- Added an explicit Next.js client-component wrapper over the shared React `IdentityProvider`.
- Added server-only opaque-session cookie coordination using HTTP-only, SameSite=Lax cookies and server-side session validation.
- Added server-side capability evaluation and protected-page helpers that delegate to the existing Generic Identity auth/RBAC path and preserve DENY versus technical failure semantics.
- Added consumer-owned route-map helpers and shared Identity page re-exports without imposing public URLs or navigation.
- Declared Next.js, React and React DOM as peer dependencies to avoid duplicate framework instances in consuming applications.
- Kept the existing `examples/nextjs/admin` host untouched; consumer migration is deferred to the dedicated integration pack.
- No files moved or deleted; no backend/runtime behavior or database schema changed.

# Shared Identity Integration — Pack 6 — Theming and visual component overrides

- Fixed the Pack 6 verification script for Windows PowerShell 5.1 by removing C-style escaped quote literals and non-ASCII parser-sensitive markers.
- Fixed historical Pack 4/5/6 source gates so later monotonic `@generic-identity/react` version bumps do not invalidate already-qualified earlier packs.
- Added the first public Generic Identity theme contract with documented `--gi-*` CSS custom properties, stable `gi-*` classes and `data-gi-*` hooks.
- Added `IdentityThemeRoot` and exported `@generic-identity/react/theme.css` for default styling without forcing a consumer design system.
- Added optional `IdentityProvider.components` visual overrides for Button, Input, Panel and Table rendering.
- Added `IdentityButton` and `IdentityInput` primitives and migrated the shared sign-in/recovery forms to those primitives without changing authentication behavior.
- Made `IdentityPanel` and `IdentityTable` visual implementations replaceable while retaining safe default rendering when no provider is present.
- Added theme/component-override consumer compile coverage and Pack 6 architecture/source gates.
- Bumped the private `@generic-identity/react` extraction package to `0.2.0`; publication remains deferred.
- No files moved or deleted; no backend/runtime behavior or database schema changed.

# Shared Identity Integration — Pack 5 — Shared React pages

- Added presentation-only shared Identity pages for sign-in, recovery, account/profile, users, groups, policies, sessions, MFA and security composition.
- Added reusable semantic page primitives (`IdentityPageFrame`, `IdentityPanel`, `IdentityTable`, `IdentityStatus`, `IdentityEmptyState`) with stable `gi-*` and `data-gi-*` styling hooks.
- Kept Next.js routing, server sessions, protected mutations and consumer branding outside the shared React package.
- Recorded current Next.js host pages as future cleanup candidates only; no source was moved or deleted.

# Shared Identity Integration — Pack 4 — React foundation

- Activated private `@generic-identity/react` over the shared contracts and auth packages.
- Added `IdentityProvider`, React hooks and `RequireCapability` as a UI-only authorization gate backed by the existing server authorization path.
- Kept React/React DOM as peer dependencies and prohibited Next.js coupling in the React package.
- Added source/type gates and corrected the Pack 4 source marker so harmless TypeScript line wrapping does not fail validation.
- No existing administration host integration was replaced.

# Shared Identity Integration — Pack 3 — Authentication and authorization SDK

- Activated private `@generic-identity/auth` as a framework-neutral facade over the proven TypeScript authentication and server-backed authorization implementation.
- Added shared client/session/authentication helpers, authorization context, capability evaluation and common client error translation.
- Preserved the distinction between authorization DENY and technical failure.
- Kept `@identity-access/client` as the temporary authoritative runtime bridge; no second RBAC or authentication system was introduced.
- Added only missing public type exports to the legacy client; no implementation was moved.

# Shared Identity Integration — Pack 2 — Public contracts

- Activated private `@generic-identity/contracts` as a passive type-only boundary for shared identity, administration, authorization, policies, sessions, MFA, security manifests and public errors.
- Kept secrets, token-bearing requests, PostgreSQL/Redis internals, Organization Directory and OrganisationProfile contracts outside the shared package.
- Added consumer typecheck and source-boundary validation while leaving existing runtime behavior unchanged.

# Shared Identity Integration — Pack 1 — Baseline and structure gates

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
