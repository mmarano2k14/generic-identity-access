# 0.42.2 - TypeScript verification dependency bootstrap correction

- Added repository verification bootstrap for the pinned TypeScript development dependency when the local `node_modules/.bin/tsc.cmd` compiler is absent on a clean workstation.
- Runs the bootstrap with package lifecycle scripts, audit, and funding output disabled and without creating or mutating a package lockfile.
- Preserved the existing TypeScript build, test, strict type-check, .NET restore/build/test, authorization, OIDC, and source-consistency gates unchanged after dependency restoration.
- Added regression coverage that pins the clean-workstation TypeScript dependency-restore behavior in `scripts/verify.ps1`.
- Updated TypeScript and repository validation documentation with the explicit dependency-bootstrap command.
- Changed no TypeScript connector runtime behavior, .NET API behavior, PostgreSQL schema, migration sequence, OIDC protocol behavior, or authorization semantics.

# 0.42.1 - Repository verification PowerShell gate correction

- Corrected the repository verification runner so nested PowerShell source-consistency scripts are evaluated through PowerShell success state instead of reading an uninitialized native-process `$LASTEXITCODE`.
- Preserved `$LASTEXITCODE` checks for native `npm` and `dotnet` processes where the variable is defined by process execution.
- Added regression coverage pinning the distinction between PowerShell-script gates and native-process exit-code gates under strict mode.
- Changed no TypeScript connector runtime behavior, .NET API behavior, PostgreSQL schema, migration sequence, OIDC protocol behavior, or authorization semantics.

# 0.42.0 - TypeScript class connector and authorization-context foundation

- Replaced the legacy functional `createIdentityAccessClient(...)` implementation with the class-based `IdentityAccessClient` runtime API.
- Added `IdentityAuthorizationContext` with an async `isAllowed(resource, feature, action)` contract that delegates every decision to the trusted .NET administration authorization boundary.
- Added three capability-evaluation HTTP routes for identity-scope, tenant, and resource-scope boundaries without duplicating wildcard/TRN evaluation outside the existing authorization services.
- Preserved authorization semantics for external runtimes: explicit RBAC denial returns `allowed=false`, unauthenticated requests remain 401, trusted-boundary mismatch remains 403, and technical authorization failure remains 503.
- Added `RequireCapability(...)` TypeScript decorator metadata for declarative capability requirements while keeping authorization execution server-side.
- Added the class-based `IdentityAccessAdminUiBuilder` with optional permission-aware presentation filtering; UI visibility does not replace server-side authorization.
- Added strict Bearer and `IdentitySession` credential construction with no mixed authentication provenance.
- Extended TypeScript error mapping so 401, 403, and 503 remain distinct from generic HTTP failures.
- Replaced the function-based Next.js diagnostic helper with a class-based server-only connector example.
- Added a TypeScript source-consistency gate that pins the three required runtime classes, `RequireCapability`, `isAllowed`, and removal of the legacy functional factory.
- Integrated TypeScript source consistency, build, tests, and strict type checking into the standard repository verification script.
- Executed the TypeScript suite successfully with 33 passed tests and strict type checking in the generation environment.
- Changed no PostgreSQL schema or migration sequence; migration `0013` remains the latest migration.
- Kept MFA, TOTP, passkeys/WebAuthn, recovery codes, and step-up authentication outside this increment.

# 0.41.0 - Production qualification and operational hardening

- Added a consolidated production-qualification runner covering repository verification, external RBAC compatibility, PostgreSQL persistence/security gates, OIDC live database gates, and disposable backup/restore qualification.
- Added executable PostgreSQL backup/restore qualification using a randomly named scratch database, migration-checksum revalidation, restored-schema structural checks, and guaranteed cleanup unless explicit retention is requested.
- Added restored-database checks for required security tables, valid migration checksums, validated constraints, valid indexes, and absence of forbidden raw-secret columns.
- Hardened PostgreSQL data-source initialization so a failed lazy registration is removed instead of poisoning a destination until process restart.
- Removed connection-string parser exceptions from the public `InvalidConnectionString` failure path so raw secret values cannot leak through inner-exception serialization or broad exception logging.
- Added regression coverage proving invalid connection-string initialization can be retried, leaves no registered destination behind, and does not disclose the secret value.
- Added architecture coverage that pins the production-qualification and backup/restore gate composition.
- Added production runbook guidance for qualification databases, restart-controlled PostgreSQL secret rotation, OIDC signing-key rollover, restore evidence, and deployment-specific evidence that remains outside repository qualification.
- Changed no PostgreSQL schema or migration sequence; migration `0013` remains the latest migration.
- Kept MFA, TOTP, passkeys/WebAuthn, recovery codes, and step-up authentication outside this release as optional later work.

# 0.40.1 - Bearer administration test fixture layout correction

- Extracted Bearer administration validator test doubles into one top-level type per source file so architecture source-layout validation can enforce filename/type parity.
- Extracted OIDC access-token session test doubles into one top-level type per source file without changing test behavior.
- Preserved the 0.40.0 Bearer access-token validation runtime, current-session continuity, administration authentication semantics, OIDC key-ring behavior, and RBAC integration unchanged.
- Changed no production runtime behavior, PostgreSQL schema, migration sequence, authentication protocol contract, or public API surface.

# 0.40.0 - OIDC Bearer access-token validation for administration APIs

- Added strict RS256 Bearer access-token validation for protected administration APIs while preserving the existing `IdentitySession` transport.
- Added process-pinned public-key validation by exact JWT `kid` with no fallback key, including retained-key continuity across controlled OIDC signing-key rotation.
- Added exact JWT header and access-token claim validation for algorithm/type, signature, issuer, audience, expiry, issuance time, token id, logical subject, session id, identity scope, registered client, scope, and application binding.
- Rebound the validated `client_id` to the current trusted server registration so authentication-context provenance is never accepted from a caller JWT claim.
- Added current local-session and active-user revalidation for every Bearer administration request through the trusted authentication-directory route.
- Added session-reference persistence validation that checks current session revocation/expiry, current Active user state, client, application, and identity-scope placement without requiring or reconstructing the opaque local-session token.
- Rejected requests that mix Bearer authentication with `X-Identity-Access-Client` or `X-Identity-Access-Session` local-session provenance headers.
- Preserved administration HTTP semantics: invalid Bearer/session state maps to unauthenticated, authenticated route/RBAC mismatch remains denied, and validation/storage failures remain technical unavailability.
- Added typed OIDC access-token validation failure categories without exposing those categories as a public token-validation oracle.
- Added unit and architecture coverage for valid tokens, tampering, unknown `kid`, retained-key validation, issuer/audience/expiry enforcement, client/application binding, current-session continuity, transport separation, and technical-failure handling.
- Extended OIDC source-consistency validation and security documentation for Bearer authentication and session continuity.
- Changed no PostgreSQL schema or migration sequence; migration `0013` remains the latest migration.
- Kept MFA, TOTP, passkeys/WebAuthn, recovery codes, and step-up authentication outside this release as optional later work.

# 0.39.0 - OIDC signing-key rotation and multi-key JWKS lifecycle

- Added a process-pinned RSA signing-key ring with one explicit active `kid` for new RS256 access-token and ID-token signatures.
- Extended JWKS publication to expose the active key together with retained validation keys so previously issued JWTs can remain verifiable across controlled key rollover.
- Allowed retained signing-key entries to use public-only RSA PEM material while requiring the active key to contain private material.
- Added fail-closed validation for missing active-key membership, duplicate key identifiers, unavailable/invalid PEM files, RSA keys below 2048 bits, and oversized key rings.
- Added `ActiveSigningKeyId` plus `SigningKeys` trusted-server configuration and preserved `SigningKeyId` / `SigningKeyPemPath` as a backward-compatible one-key configuration form.
- Rejected mixed legacy and multi-key signing configuration to prevent ambiguous startup authority.
- Kept signing-key activation restart-controlled; PEM/configuration changes do not mutate the process-pinned signing state in place.
- Added signing-key ring tests covering active-key signatures, multi-key JWKS projection, public-only retained keys, duplicate `kid` rejection, unknown active-key rejection, and configuration compatibility.
- Extended OIDC source-consistency validation and protocol documentation for active-key selection and multi-key JWKS publication.
- Changed no PostgreSQL schema, migration sequence, refresh-token family semantics, authentication transport, external RBAC behavior, or multi-database routing behavior.

# 0.38.1 - OIDC refresh-token PostgreSQL validation fixture correction

- Corrected a malformed UUID family identifier in the live PostgreSQL refresh-token validation fixture.
- Added a source-level regression test that parses every UUID-like literal in the refresh-token PostgreSQL validation fixture before live database execution.
- Preserved the 0.38.0 refresh-token runtime implementation, mandatory rotation, family replay revocation, local-session continuity, token issuance behavior, database schema, and migration sequence unchanged.
- Changed no authentication transport, external RBAC behavior, multi-database routing behavior, or production persistence contract.

# 0.38.0 - OIDC refresh-token rotation and protocol session continuity

- Added opaque 256-bit refresh tokens with SHA-256-only persistence and a configurable absolute family lifetime.
- Added migration `0013_oidc_refresh_token_rotation.sql` with refresh-token family lineage, single-use state, replay revocation state, current-session linkage, and secret-safe transactional security-mutation ledger coverage.
- Added mandatory refresh-token rotation with family-level mutation serialization so replay of a consumed token revokes the entire family without exposing replay details to the public client.
- Revalidated the source local session and current active user during initial family creation and every refresh rotation.
- Extended `/connect/token` with the public-client `refresh_token` grant while rejecting unexpected form fields, client secrets, password grants, and scope escalation.
- Preserved the original family scope, session, application, authentication context, authentication time, and absolute expiry across rotations.
- Added access-token-only refresh issuance; authorization-code exchange still returns an RS256 access token and ID token, while refresh returns a new access token and rotated refresh token without inventing new nonce semantics.
- Extended OIDC discovery to advertise `authorization_code` and `refresh_token` grants.
- Added typed semantic security-audit events for refresh-family creation, rotation, reuse detection, and family revocation without logging raw tokens or token hashes.
- Added refresh-token cryptography, orchestration, persistence-contract, replay, session-eligibility, source-consistency, and live PostgreSQL validation coverage.
- Updated OIDC, architecture, failure-code, validation, and repository documentation for refresh-token continuity.
- Changed no client-secret policy, PKCE S256 requirement, redirect-URI matching rules, local-session transport, external RBAC behavior, or multi-database routing ownership.

# 0.37.1 - OIDC test fixture and API regression correction

- Corrected a malformed UUID fixture in the RS256 token issuer test.
- Removed `/connect/authorize` and `/connect/token` from the legacy `Unimplemented_identity_endpoints_are_not_exposed` test because both routes are implemented by the OIDC protocol surface.
- Preserved HTTP method semantics: `/connect/token` is a POST endpoint, so a GET request may correctly resolve to `405 Method Not Allowed` rather than `404 Not Found`.
- Changed no runtime OIDC behavior, token claims, PKCE validation, signing behavior, database schema, migrations, authentication transport, authorization behavior, or RBAC integration.

# 0.37.0 - OAuth 2.0 / OpenID Connect Authorization Code + PKCE

- Added a strict public-client OAuth 2.0 Authorization Code + OpenID Connect protocol surface with mandatory PKCE S256.
- Added exact registered redirect URI validation, mandatory `state` and `nonce`, `response_type=code`, and the initial `openid` scope only.
- Restricted the configured issuer to an origin-only HTTPS URI, or HTTP loopback origin for development, so discovery URLs always match the actual root-mounted protocol endpoints.
- Added `/.well-known/openid-configuration`, `/.well-known/jwks.json`, `/connect/authorize`, and `/connect/token` MVC endpoints.
- Kept browser/login UI outside the protocol layer; the authorization endpoint consumes an already validated local `IdentitySession` session.
- Added opaque 256-bit authorization codes with SHA-256-only persistence and migration `0012_oidc_authorization_code_pkce.sql`.
- Made authorization-code creation itself conditional on the source session remaining active and the current user remaining active in the same PostgreSQL `INSERT ... SELECT`, closing the validation-to-persistence revocation race.
- Added atomic single-use code redemption requiring exact client/redirect/PKCE challenge, unexpired unconsumed code, active source session, and current active user state.
- Added transactional security-mutation ledger coverage for OIDC authorization-code creation and consumption using only `identity_scope_id` and `code_id` as ledger keys.
- Added RS256 access-token and ID-token issuance using a trusted server-side RSA PEM key, configurable `kid`, process-pinned key material, and JWKS public-key projection.
- Added ID-token claims `iss`, `sub`, `aud`, `exp`, `iat`, `auth_time`, `nonce`, `sid`, and `at_hash`.
- Added access-token claims for issuer, logical subject, audience, expiry, issuance, token id, client, scope, session, identity scope, and application.
- Extended validated local-session context with the original authentication time required for the OIDC `auth_time` claim.
- Extended immutable authentication-client registration with explicit OIDC enablement and allowed OIDC scopes; this release supports only `openid`.
- Added typed OIDC authorization/token failure categories and OAuth wire-error mapping.
- Added cryptographic PKCE/JWT tests, protocol orchestration tests, OIDC client/configuration tests, PostgreSQL code-store contract tests, and a live PostgreSQL single-use/revocation validation.
- Added `verify-oidc-source-consistency.ps1` and integrated it into the standard repository verification flow.
- Added a development RSA signing-key generation utility and professional OIDC protocol documentation.
- Preserved the existing two-argument authentication registration overload while adding the explicit content-root overload required for deterministic signing-key path resolution.
- Added the default `secrets/` directory to `.gitignore`.
- Added no IdentityServer or equivalent external OIDC server dependency.
- Refresh tokens, key rotation, MFA protocol signaling, userinfo, device flow, client credentials, and dynamic client registration remain outside this release.

# 0.36.0 - External RBAC integration hardening

- Added a process-pinned compatibility binding for the runtime-only external RBAC distribution.
- Added `IMultiplexedRbacCompatibilityProbe` and a neutral `MultiplexedRbacCompatibilityReport` exposing compatibility state, typed failure code, safe diagnostic detail, assembly versions, and SHA-256 fingerprints without leaking external types.
- Added typed `ExternalLoadFailed` and `ExternalContractMismatch` RBAC failure categories while preserving existing failure-code values.
- Centralized all external reflection discovery and contract validation in `MultiplexedRbacBindingLoader`.
- Validated required external assembly identities, configured load paths, types, public constructors, writable properties, methods, method return types, namespace collection compatibility, and engine constructor shape before authorization is enabled.
- Pinned the validated binding through a thread-safe lazy instance so authorization requests no longer rediscover reflection members per call and cannot silently hot-swap an external distribution during process lifetime.
- Added startup compatibility preflight when administration authorization is enabled; incompatible or unloadable external distributions now fail startup instead of producing a partially functional authorization boundary.
- Added deterministic external execution-context cleanup through `finally`, with cleanup failures remaining technical invocation failures.
- Preserved project/namespace grant filtering while leaving all wildcard meaning and final allow/deny evaluation exclusively to the external RBAC engine.
- Added unit, architecture, and real external-integration tests for missing distributions, invalid binary images, process-pinned preflight results, binary fingerprints, reflection-binding centralization, context cleanup, and startup preflight.
- Updated external RBAC, dependency, validation, and typed-failure documentation.
- Changed no tenant/scope grant semantics, TRN wire format, wildcard semantics, authentication transport, database schema, or external RBAC engine implementation.

# 0.35.0 - Transactional security mutation ledger

- Added migration `0011_transactional_security_mutation_ledger.sql` with an append-only PostgreSQL ledger for security-sensitive persistence mutations.
- Added database triggers that capture INSERT, UPDATE, and DELETE operations inside the same transaction as users, tenants, memberships, groups, policies, credentials, sessions, resource scopes, application security models/capabilities, and identity-scope authority state.
- Added safe explicit record-key capture per table; complete rows, password hashes, session-token hashes, login identifiers, connection strings, secret references, and arbitrary payloads are excluded.
- Added request Activity tags for trusted administration actor provenance and server-generated correlation identifiers.
- Added PostgreSQL connection-session propagation for correlation id, actor identity scope, user, session, client, application, and authentication-context identifiers, overwriting all audit settings on every pooled connection checkout.
- Added append-only enforcement preventing UPDATE or DELETE of transactional mutation-ledger records.
- Preserved semantic `security_events` as best-effort domain enrichment while making the transactional mutation ledger authoritative for persistence-change durability.
- Added live PostgreSQL validation proving mutation/ledger co-commit, rollback symmetry, actor/correlation propagation, credential secret exclusion, and append-only behavior.
- Added architecture and PostgreSQL contract tests plus professional transactional-audit documentation.
- Integrated the authorization source-consistency gate into the normal `scripts/verify.ps1` verification flow.
- Added XML comments to new public audit-provenance contracts.
- Changed no authorization wildcard semantics, authentication token format, redirect-URI rules, tenant/scope authority model, or external RBAC behavior.

# 0.34.0 - Identity-scope authority administration API

- Added strong domain types for identity-scope administration groups, direct user memberships, policies, statements, and group-policy bindings.
- Added application storage contracts and PostgreSQL implementations for scope-authority lifecycle management.
- Added atomic member creation requiring an active administration group and active user in one PostgreSQL statement.
- Added atomic group-policy binding creation requiring an active administration group and active policy in one PostgreSQL statement.
- Added `IIdentityScopeAuthorityAdministrationService` with optimistic-concurrency group/policy mutations and audited membership, statement, and binding changes.
- Added MVC/Swagger endpoints for scope-authority groups, memberships, policies, statements, and bindings.
- Added dedicated RBAC capabilities for authority-group, membership, policy, statement, and binding administration.
- Preserved explicit first-administrator bootstrap as the root-of-trust mechanism; routine lifecycle management is now performed through the RBAC-protected API.
- Added typed security-audit event categories for all scope-authority administration mutations.
- Added live PostgreSQL administration-invariant validation, architecture tests, storage contract tests, and professional documentation.
- Changed no database schema beyond existing migration 0010, external RBAC wildcard behavior, session-token format, authentication transport, or tenant policy model.

# 0.33.5 - Authorization source-consistency gate alignment

- Updated `verify-authorization-source-consistency.ps1` to follow the one-type-per-file authorization test layout introduced in 0.33.4.
- Moved the immutable-route fixture assertions from `IdentityScopeAuthorizationServiceTests.cs` to `IdentityScopeAuthorizationTestRouteResolver.cs`, where the route is now constructed.
- Added the six split authorization test-helper files to the consistency gate's required-source set.
- Preserved the test class check that verifies the identity-scope authorization test still uses the dedicated route resolver.
- Changed no production runtime code, authorization behavior, database schema, migration history, HTTP behavior, TRN format, wildcard semantics, or external RBAC integration.

# 0.33.4 - Authorization test source-layout correction

- Split identity-scope authorization test doubles into dedicated source files so every C# source file declares at most one type.
- Split administration RBAC authorizer test doubles into dedicated source files with scenario-specific names to avoid future type-name collisions.
- Preserved file-name/type-name alignment and block-scoped namespaces for all new test helper types.
- Kept the existing source-layout architecture gate unchanged so the one-type-per-file rule remains enforced for tests and production code.
- Changed no production runtime code, authorization decisions, database schema, migration history, TRN shape, wildcard behavior, HTTP behavior, or external RBAC integration.

# 0.33.3 - Cumulative administration authorization consistency repair

- Reissued the complete administration authorization and identity-scope authority source delta from the last stable 0.31.1 baseline so the API security boundary cannot remain partially overlaid across 0.32.x and 0.33.x.
- Includes the required authorization helper types, request-boundary and target resolvers, stable administration failure codes, API project references, tenant/scope authorization orchestration, PostgreSQL identity-scope grant projection, migration 0010, and the 0.33.1/0.33.2 build corrections as one coherent source set.
- Added `verify-authorization-source-consistency.ps1` to fail before compilation when required authorization files, project references, failure-code members, constructor corrections, or current route fixtures are missing.
- Updated validation documentation with the source-consistency gate.
- Changed no authorization policy semantics, database schema beyond the already-defined migration 0010, TRN format, wildcard behavior, authentication transport, or external RBAC behavior.

# 0.33.2 - Authorization build and project-reference correction

- Corrected the identity-scope authorization test fixture to construct `ResolvedDatabaseRoute` with the complete immutable placement snapshot required by the current routing contract.
- Rewrote the API project reference groups explicitly so the current repair delta carries and reloads direct references to `IdentityAccess.Authorization`, `IdentityAccess.Rbac`, and `IdentityAccess.Rbac.MultiplexedAdapter`.
- Marked the three direct authorization project references with explicit `ReferenceOutputAssembly="true"` semantics.
- Added an architecture regression test protecting the API authorization project graph.
- Added a route-constructor regression test preventing authorization fixtures from drifting to obsolete `ResolvedDatabaseRoute` constructor shapes.
- Preserved the 0.33.1 public-constructor accessibility correction and kept `CapabilityGrantAuthorizationEvaluator` internal.
- Changed no runtime authorization decisions, grant projection, database schema, TRN shape, wildcard semantics, HTTP routes, or external RBAC behavior.

# 0.33.1 - Authorization constructor accessibility correction

- Removed the internal `CapabilityGrantAuthorizationEvaluator` from the public constructors of tenant and identity-scope authorization services.
- Kept the shared evaluator internal and instantiated it from public `RbacTrnCompiler` and `IRbacAuthorizationAdapter` constructor dependencies.
- Removed internal evaluator registration from the API composition root.
- Restored the identity-scope authorization unit test to the supported public constructor surface.
- Added an architecture regression test preventing public authorization constructors from exposing internal types from the authorization assembly.
- Changed no authorization decisions, grant projection, database schema, TRN format, wildcard semantics, HTTP behavior, or external RBAC integration.

# 0.33.0 - Identity-scope administration authority

- Added a dedicated identity-scope administration authority model so scope-wide operations no longer depend on or borrow authority from an arbitrary tenant.
- Added scope administration groups, direct user memberships, policies, policy statements, and group-policy bindings in migration `0010_identity_scope_administration_authority.sql`.
- Reused application security-model pinning and capability-pattern validation for scope administration policies.
- Added `IIdentityScopeAssignedCapabilityReader` with PostgreSQL projection from active users, active scope groups, active policies, and bound statements.
- Added `IdentityScopeAuthorizationRequest` and `IIdentityScopeAuthorizationService`.
- Extracted shared TRN materialization and external RBAC delegation into `CapabilityGrantAuthorizationEvaluator` for both tenant and identity-scope authorization.
- Updated administration RBAC enforcement to select tenant/resource authorization when `tenantId` is present and dedicated identity-scope authorization otherwise.
- Preserved complete separation between tenant authority and identity-scope authority.
- Added an explicit PostgreSQL bootstrap utility for provisioning the first scope administrator without inferring authority from tenant membership.
- Added live PostgreSQL authority validation, unit tests, architecture tests, and professional documentation.
- Added XML comments to new public production and test contracts.
- Changed no local-session token format, authentication transport, tenant policy semantics, resource-scope semantics, or external RBAC wildcard behavior.

# 0.32.0 - Tenant-scoped administration RBAC enforcement

- Connected trusted administration sessions to `IdentityAuthorizationService` and the configured external RBAC adapter for tenant-scoped administration routes.
- Added strict server-side authorization configuration for RBAC project, namespace, provider, and external binary reference directory.
- Added fail-fast validation for required routing/grant-projection services and external RBAC binaries when authorization is enabled.
- Added route-derived tenant and optional resource-scope authorization targets without accepting caller-provided authentication claims.
- Preserved exact-scope and descendant resource binding behavior by passing route `resourceScopeId` into the existing authorization request.
- Kept identity-scope-only administration operations fail-closed instead of borrowing authority from an arbitrary tenant.
- Added explicit administration failure categories for unavailable authentication, unsupported authorization target, and technical authorization failure.
- Preserved `Denied` versus `TechnicalFailure` semantics through the HTTP authorization bridge.
- Kept all wildcard evaluation exclusively in the external RBAC engine.
- Updated readiness to report capability authorization available only when the RBAC-backed authorizer is configured.
- Added unit and architecture tests for trusted-context translation, tenant/resource target mapping, denial, technical failure, and identity-scope fail-closed behavior.
- Added professional administration RBAC authorization documentation.
- Changed no database schema, policy storage model, session-token format, redirect-URI rules, or external RBAC wildcard semantics.

# 0.31.1 - Administration authentication route fixture correction

- Corrected the malformed user GUID used by the protected administration-route integration test.
- Added explicit GUID parsing assertions so future fixture changes cannot silently fall through the MVC `{guid}` route constraint.
- Restored the test to the intended administration authentication boundary, where HTTP 401 or fail-closed HTTP 503 is evaluated.
- Changed no runtime code, authentication behavior, trusted-context semantics, authorization behavior, database schema, or external RBAC integration.

# 0.31.0 - Trusted authenticated administration context

- Added `AuthenticatedSessionContext` so local-session validation returns server-validated subject, client, application, authentication-context, session, and expiry provenance without exposing the raw session token.
- Added `AdministrationRequestContext` as the per-request trusted identity object for administrative HTTP operations.
- Added a local-session administration context resolver using `Authorization: IdentitySession`, `X-Identity-Access-Client`, and `X-Identity-Access-Session`.
- Prevented caller-supplied user, identity-scope, application, tenant, resource-scope, permission, or capability values from becoming trusted authentication state.
- Added request-boundary matching between the validated session identity scope/application and route identity scope/application.
- Added an explicit unauthenticated administration access decision mapped to HTTP 401.
- Attached validated administration context to `HttpContext.Features` rather than global mutable state.
- Kept capability authorization fail-closed and explicitly unavailable until the identity authorization service is connected.
- Updated readiness diagnostics so authenticated-context availability is not mistaken for capability-authorization readiness.
- Added architecture and HTTP-boundary tests plus professional trusted-context documentation.
- Added XML comments to new public security and authentication contracts.
- Changed no database schema, password/session-token format, redirect-URI rules, wildcard semantics, or external RBAC behavior.

# 0.30.0 - Authentication session lifecycle hardening

- Bound session issuance to current active-user state with an atomic PostgreSQL `INSERT ... SELECT` predicate.
- Bound session validation to current active-user state so suspension invalidates already-issued sessions.
- Revoked active sessions atomically when a user is updated to a non-active state.
- Revoked active subject sessions atomically when a password credential is changed successfully.
- Added subject-wide and registered-client-wide session revocation contracts and administration endpoints.
- Added the `identity-access / session / write` administration capability.
- Added security audit events for user-wide and client-wide session revocation.
- Removed the separate user-directory read from local login session issuance.
- Added architecture, PostgreSQL contract, Swagger, and live PostgreSQL session-lifecycle verification.
- Added professional session lifecycle documentation.
- Added XML comments to all new public production and test contracts.
- Changed no database schema, session-token format, redirect-URI rules, wildcard semantics, or external RBAC behavior.

# 0.29.0 - Contract alignment and repository hygiene

- Aligned the TypeScript service-information contract with the .NET API by adding `databaseRoutingConfigured`.
- Updated TypeScript diagnostic fixtures and smoke validation to use the current `configuration` readiness stage and blocker set.
- Removed unused user, tenant, and group profile transport contracts and the unused directory profile mapper.
- Removed the resulting `IdentityAccess.Application` dependency on `IdentityAccess.Contracts`.
- Added architecture tests that protect the Application-to-Contracts boundary and prevent removed profile DTOs from returning.
- Regenerated `SOURCE_MANIFEST.sha256` as a source-only manifest that excludes generated build output such as `dist`, `bin`, `obj`, `node_modules`, `.next`, `.vs`, `artifacts`, and `TestResults`.
- Updated validation documentation with migration-integrity, atomic-mutation, and security-audit PostgreSQL gates.
- Updated TypeScript client package metadata to describe its current diagnostics-only role.
- Changed no database schema, authentication semantics, authorization semantics, wildcard evaluation, routing behavior, or public HTTP routes.

# 0.28.0 - Structured observability, correlation and security audit

- Added server-generated HTTP correlation identifiers and structured request-completion logging scopes.
- Added typed security audit event, outcome, reason, and writer contracts in the application layer.
- Added durable PostgreSQL security audit persistence with correlation identifiers and secret-safe categorical fields.
- Added migration `0009_security_audit.sql` with audit lookup indexes.
- Added audit emission for directory, policy, resource-scope, credential, password-login, and session-revocation events.
- Kept audit sink failures non-authoritative for completed primary operations while logging persistence failures server-side.
- Added security audit schema verification and architecture tests that reject obvious credential or token fields in audit contracts.
- Added HTTP correlation integration tests and professional observability/security audit documentation.
- Added XML comments to all new production and test types.
- Changed no authorization wildcard semantics, routing format, session-token format, redirect-URI rules, or public HTTP route structure.

# 0.27.0 - Dynamic health, readiness and runtime version diagnostics

- Replaced the stale application-layer `FoundationStatus` diagnostic object with runtime diagnostics derived from the actual host service graph.
- Added ASP.NET Core tagged health checks for liveness and readiness while preserving controller-based HTTP endpoints and Swagger documentation.
- Added explicit database-routing configuration state to the service information contract.
- Made readiness blockers dynamic for routing, PostgreSQL persistence, local authentication, and administration authorization.
- Derived the reported module version from the running API assembly informational version instead of a hardcoded value.
- Updated health and routing API tests to reflect dynamic capability state.
- Added architecture tests that prevent `FoundationStatus` from returning and require tagged health-check registration.
- Added professional health and runtime diagnostics documentation.
- Added XML comments to all new production and test contracts.
- Changed no database schema, routing format, authentication behavior, authorization semantics, wildcard evaluation, or public administration routes.

# 0.26.0 - Infrastructure layer boundaries

- Added `IdentityAccess.Infrastructure.Authentication` for password hashing, session-token cryptography, authentication-client configuration, and authentication service registration.
- Moved PostgreSQL and configuration-routing registration into their owning infrastructure assemblies.
- Internalized PostgreSQL store implementations, connection management, schema migration, storage options, and environment secret resolution.
- Internalized the concrete configuration routing provider.
- Added test-only internal visibility for PostgreSQL and configuration-routing contract tests without expanding the supported public surface.
- Aligned API Security namespaces with their folder boundary.
- Added architecture tests that protect infrastructure public surfaces and prevent authentication infrastructure from returning to the HTTP assembly.
- Added professional layer-boundary documentation and updated the repository structure.
- Added XML comments to all new public and test contracts.
- Changed no database schema, routing format, authentication semantics, authorization semantics, wildcard evaluation, or HTTP routes.

# 0.25.3 - Atomic validation fixture UUID correction

- Corrected malformed UUID literals in the PostgreSQL atomic-mutation validation fixture.
- Added a regression test that validates UUID literals used by the live PostgreSQL fixture.
- Changed no runtime code, migration SQL, database schema, atomic mutation implementation, routing, authentication, authorization, or RBAC behavior.

# 0.25.2 - PostgreSQL migration script robustness

- Corrected PowerShell interpolation of migration version values followed by a colon.
- Added null-safe PostgreSQL text-command handling for empty query output.
- Made migration verification detect an uninitialized checksum column and instruct the operator to run schema application first.
- Kept schema application responsible for creating the checksum column before any checksum query is executed.
- Added unknown-applied-version validation to both schema application and integrity verification paths.
- Changed no runtime checksum algorithm, migration SQL, atomic mutation behavior, routing, authentication, authorization, or RBAC behavior.

# 0.25.1 - Migration checksum test correction

- Corrected migration checksum tests to use actual CRLF and LF characters instead of literal backslash escape sequences.
- Corrected the independent reference checksum implementation to normalize real carriage-return and line-feed characters.
- Changed no runtime checksum algorithm, database schema, migration SQL, atomic mutation logic, routing, authentication, authorization, or RBAC behavior.

# 0.25.0 - Atomic mutations and migration integrity

- Added atomic group-membership creation that validates active group and tenant-membership state inside the insert statement.
- Added atomic group-policy binding creation that validates active group, policy, and optional resource-scope state inside the insert statement.
- Added atomic credential creation and password update that verify subject existence inside the mutation statement.
- Removed read-then-write pre-validation from group membership, policy binding, and credential administration orchestration.
- Added SHA-256 checksums to PostgreSQL migration metadata with line-ending normalization shared by runtime and PowerShell migration paths.
- Added migration-name, checksum, and unknown-version integrity validation.
- Added live PostgreSQL validation scripts for migration integrity and atomic mutation predicates.
- Added architecture and migration-integrity tests.
- Added XML comments to all new production and test contracts and implementations.
- Changed no external RBAC wildcard semantics, routing format, session-token format, or public HTTP routes.

# 0.24.1 - Authentication problem-response reference fix

- Corrected the password-login fallback branch to use the centralized `ApiProblems.AuthenticationUnavailable()` response.
- Added an architecture regression test that detects references to removed controller-local HTTP problem helpers.
- Changed no authentication decision semantics, routing, persistence, authorization, wildcard, or database behavior.

# 0.24.0 - Central HTTP problem and exception handling

- Added `ApiProblems` as the single source for standardized RFC 7807 problem responses used by the HTTP layer.
- Added `ApiExceptionHandler` for centralized mapping of invalid request values, optimistic-concurrency conflicts, database-route failures, and PostgreSQL storage failures.
- Removed controller-local `IdentityConcurrencyException` handling.
- Removed direct `ProblemDetails` construction from controllers and authorization filters.
- Preserved fail-closed feature-unavailable and administration-authorization responses through centralized problem factories.
- Added architecture tests that prevent controller-local problem construction and concurrency-exception translation.
- Added professional HTTP error-handling documentation.
- Added XML comments to all new production and test types.
- Changed no domain, routing, persistence schema, authentication, wildcard, or external RBAC semantics.

# 0.23.0 - Explicit controller dependency injection

- Removed `IServiceProvider` from all MVC controller constructors.
- Replaced controller-level service location with explicit `OptionalFeature<TService>` dependencies.
- Added a single API composition-root registration for optional directory, policy, resource-scope, authentication, and credential services.
- Preserved fail-closed behavior when optional services are not configured.
- Added architecture tests that reject `IServiceProvider`, `GetService`, and `RequestServices` usage in controller source.
- Added tests for available and unavailable optional feature handles.
- Added professional dependency-injection documentation.
- Added XML comments to the new production and test types.
- Changed no routing, persistence, authentication, authorization, wildcard, or database behavior.

# 0.22.0 - Typed failure contracts

- Replaced free-form authorization, RBAC, authentication, and administration failure-code strings with subsystem-specific enums.
- Preserved external RBAC diagnostic detail separately from the stable typed RBAC failure category.
- Preserved the originating typed RBAC failure when identity authorization reports an RBAC technical failure.
- Updated authentication HTTP mapping to compare typed authentication failure codes.
- Added architecture tests that protect result contracts from regressing to free-form string failure codes.
- Added professional failure-code contract documentation.
- Added XML comments to all new production and test types introduced by this release.
- Changed no database schema, routing format, wildcard authorization semantics, session-token format, or redirect-URI rules.

# 0.21.0 - Administration capability catalog and documentation policy

- Centralized administration capability resource, feature, and action segments in `IdentityAccessAdministrationCapabilities`.
- Replaced repeated authorization string literals in administration controllers with catalog constants.
- Added architecture tests that validate catalog segments and reject duplicated raw administration capability literals in controllers.
- Kept XML documentation generation for production assemblies while making missing XML comments non-blocking.
- Documented the repository policy that XML comments remain expected quality guidance rather than a compiler requirement.
- Added XML comments to the new production catalog and architecture tests.
- Changed no authorization semantics, wildcard evaluation, persistence schema, routing contract, or external RBAC behavior.

# 0.20.0 - Key and identifier validation consolidation

- Centralized canonical lowercase slug mechanics used by domain key value objects.
- Preserved distinct semantic types while removing duplicated key-validation loops.
- Reused the application-key grammar for authentication context-key validation.
- Added `RbacContextKey` as the single RBAC project/namespace canonicalization contract.
- Aligned authorization requests and TRN compilation on the same RBAC context rules.
- Added consistency tests covering canonical keys, capability normalization, authentication context keys, and RBAC context bounds.
- Added professional key and identifier contract documentation.
- Changed no database schema, routing format, wildcard authorization semantics, or external RBAC evaluation behavior.

# 0.19.0 - Explicit cancellation contracts

- Removed optional `CancellationToken = default` parameters from production routing, persistence, secret-resolution, connection, and migration contracts.
- Updated corresponding infrastructure implementations and test doubles to require explicit cancellation tokens.
- Added an architecture test that rejects optional cancellation tokens in production C# source.
- Added repository documentation for cancellation propagation and operation independence.
- Changed no routing semantics, persistence schema, authentication behavior, authorization behavior, or RBAC behavior.

# 0.18.3 - Correct test-project XML documentation isolation

- Moved conditional XML documentation settings from `Directory.Build.props` to `Directory.Build.targets`.
- Ensured `IsTestProject` is evaluated after project-local properties are available.
- Disabled XML documentation generation and `CS1591` enforcement for test projects only.
- Preserved XML documentation generation and warnings-as-errors for production projects.
- Kept both test projects explicitly marked with `IsTestProject=true`.
- Changed no runtime API, routing, persistence, authentication, authorization, or RBAC behavior.

# 0.18.2 - Production-only XML documentation enforcement

- Restricted XML documentation generation to non-test .NET projects.
- Kept missing production public API documentation as a build failure through `TreatWarningsAsErrors`.
- Disabled XML documentation generation for test projects and suppressed `CS1591` there.
- Removed repository-wide `.editorconfig` enforcement of `CS1591` so test fixtures are not treated as public product API.
- Confirmed both test projects explicitly declare `IsTestProject=true`.
- Changed no runtime API, persistence, authentication, authorization, routing, or RBAC behavior.

# 0.18.1 - Public enum documentation correction

- Added XML documentation for every `AdministrationAccessDecision` enum member.
- Expanded the enum to one documented member per line so compiler-enforced CS1591 validation covers the public API clearly.
- Preserved all authorization semantics and API contracts.

# 0.18.0 - Public API documentation

- Added XML documentation for externally visible production types and members.
- Enabled XML documentation generation across all .NET projects.
- Kept compiler warnings as build errors so undocumented public API additions fail normal builds.
- Added repository documentation for public contract documentation standards.
- Preserved block-scoped namespaces and one-type-per-file source layout rules.

# 0.17.3 - Repository documentation normalization

- Replaced internal development-process documentation with current architecture, validation, dependency, and source-layout references.
- Standardized repository documentation on professional English terminology.
- Consolidated validation guidance into `docs/VALIDATION.md`.
- Replaced the historical foundation document with `docs/ARCHITECTURE.md`.
- Removed transitional documentation references from the public README and changelog.
- Changed no API, persistence, authentication, authorization, routing, or RBAC behavior.

# 0.17.2 - Block-scoped namespace standardization

- Standardized every C# namespace under `src/` and `tests/` on block-scoped syntax.
- Added an architecture test that rejects file-scoped namespaces.
- Preserved one declared C# type per file and file/type-name alignment.
- Repaired positional record extraction introduced by the previous source-layout transformation.
- Confirmed a single `PasswordLoginResult` declaration in the packaged source.
- Added no functional API, persistence, routing, authentication, or authorization behavior.

# 0.17.1 - One-type-per-file source layout

- Rebuilt the source-layout refactor from the validated 0.16.0 baseline.
- Preserved positional-record parameter attributes and complete record declarations when splitting controller contracts.
- Moved nested infrastructure helper records into dedicated internal files without changing behavior.
- Consolidated repeated PostgreSQL test connection guards into one dedicated test-support type.
- Moved authorization test doubles into dedicated files and preserved their original behavior.
- Enforced one declared C# type per file and file-name/type-name alignment across production and test source.
- Added an architecture regression test for source-layout rules.
- Added no functional API, database, authentication, authorization, routing, or RBAC behavior change.

# 0.16.0 - RBAC compatibility consolidation

- Removed the obsolete Application-layer RBAC compatibility implementation.
- Removed the duplicate Application-layer `RbacTrnCompiler`.
- Removed the local `RbacScopeConformanceEvaluator`; wildcard authorization remains exclusively owned by the external RBAC engine.
- Removed the obsolete `RbacPermissionPattern` and `RbacTrnContext` compatibility types.
- Removed the obsolete local wildcard compatibility unit suite and historical compatibility document.
- Retained `IdentityAccess.Rbac.RbacTrnCompiler` as the single TRN materialization implementation.
- Added focused compiler contract tests and an architecture regression test that prevents RBAC compiler/wildcard-evaluator ownership from returning to the Application layer.
- Changed no intended HTTP, persistence, authentication, resource-scope, policy, session, or authorization-decision behavior.

# 0.15.3 - Authentication availability gate

- Added an MVC authorization filter that fails local-authentication endpoints closed before request-body model binding when authentication is disabled.
- Preserved HTTP `503 Service Unavailable` as the explicit disabled-authentication contract.
- Added a regression test proving malformed or incomplete login bodies cannot bypass or alter the disabled-authentication availability result.
- Kept local authentication, routing, PostgreSQL, RBAC, session, and redirect validation behavior unchanged when authentication is enabled.

# 0.15.2 - Test-host routing configuration isolation

- Normalized blank routing file paths to an absent value before provider validation.
- Changed shared API test-host setup to mask ambient routing file-path environment variables with an explicit empty host setting.
- Updated routing test hosts to isolate `FilePath` from developer-machine environment state.
- Added a regression test proving `Provider=none` with an empty file path remains unconfigured instead of producing `ConflictingProviderSelection`.
- Added no production routing fallback and no database, authorization, authentication, or resource-scope behavior change.

# 0.15.1 - Deterministic disabled-authentication test host

- Pinned routing, PostgreSQL, and local authentication to disabled values in the shared `WebApplicationFactory` test host.
- Replaced the late test configuration override with host settings so minimal-hosting startup observes the intended test configuration.
- Prevented ambient developer-machine `IdentityAccess__*` environment variables from accidentally enabling local authentication in the disabled-authentication API test.
- Added an explicit assertion that `ILocalAuthenticationService` is absent before validating the fail-closed HTTP response.
- Changed no production authentication, routing, PostgreSQL, resource-scope, RBAC, or API behavior.

# 0.15.0 - Generic resource scope hierarchy and scoped policy bindings

- Added versioned application-defined resource scope types with explicit parent-type relationships.
- Added tenant-linked hierarchical resource scopes without hard-coding application business concepts.
- Added optimistic concurrency and PostgreSQL persistence for resource scope instances.
- Extended group-policy bindings with tenant-wide, exact-scope, and include-descendants targets.
- Preserved pre-existing unscoped bindings as tenant-wide authorization assignments.
- Added scope-aware assigned-capability projection before external RBAC wildcard evaluation.
- Kept resource scope identity separate from the external TRN wire format.
- Added MVC/Swagger administration controllers for scope types and resource scopes.
- Added PostgreSQL migration `0008_resource_scope_hierarchy.sql` and live hierarchy validation.
- Kept request cancellation tokens explicit on all new application and storage contracts.
- Added no application-specific organization, business, department, project, site, or environment types to the core.

# 0.14.1 - Authentication schema verification script fix

- Fixed PostgreSQL dollar-quoted `DO $$ ... $$;` handling in the PowerShell authentication schema verification script.
- Replaced invalid backslash escaping with a literal PowerShell here-string so `psql` receives valid PostgreSQL syntax.
- No authentication contract, database schema, API, routing, or security semantics changed.

# 0.14.0 - Local authentication foundation

- Added persistent password credentials separated from public user profiles.
- Added ASP.NET Core Identity password hashing through an internal adapter; plaintext passwords are not persisted.
- Added deterministic per-scope login-identifier normalization and uniqueness.
- Added failed-attempt tracking and bounded lockout state.
- Added opaque 256-bit session tokens with SHA-256-only persistence and explicit session revocation.
- Added server-registered authentication clients with exact login and post-logout redirect URI validation.
- Added controller-based password login, session validation, logout and protected credential administration endpoints.
- Bound sessions to identity scope, client, application and authentication context without introducing implicit SSO.
- Added migration `0007_local_authentication_foundation.sql`.
- Preserved fail-closed administration authorization; this version does not expose OIDC protocol endpoints.
- Required explicit cancellation tokens across all new authentication application contracts.

# 0.13.0 - Administration application services

- Added application-layer directory and policy administration services.
- Moved routing and persistence orchestration out of MVC controllers.
- Centralized group-membership and group-policy-binding structural validation paths.
- Preserved one immutable server-resolved database route per administration operation.
- Preserved optimistic concurrency and existing HTTP `409 Conflict` mapping.
- Required explicit cancellation tokens on all new application administration contracts.
- Registered administration services only when routing and the required stores are available.
- Preserved fail-closed controller authorization and diagnostic startup when storage is unconfigured.
- Added no authentication, OIDC, login, redirect URI, session, MFA, or recovery behavior.
- Added no PostgreSQL migration.

# 0.12.2 - MVC action binding correction

- Reworked `GroupMembersController` dependency resolution to use a private dependency holder instead of a tuple.
- Added explicit `[FromRoute]` and `[FromBody]` binding metadata to the group-membership administration actions.
- Marked internal controller helpers as `[NonAction]` to keep them outside MVC action discovery.
- Preserved request cancellation propagation, fail-closed administration authorization, routing, persistence, and HTTP contracts.

# 0.12.1 - Nullable flow correction

- Corrected nullable-flow annotations in directory administration controllers after fail-closed dependency resolution.
- No API contract, routing, persistence, authorization, or schema behavior changed.

# 0.12.0 - Directory administration controllers

- Added MVC controllers for users, tenants, tenant memberships, groups, and group memberships.
- Extended Swagger/OpenAPI with the persisted directory administration surface.
- Preserved fail-closed administration authorization and server-side route resolution.
- Added HTTP `409 Conflict` mapping for optimistic-concurrency failures.
- Propagated required request cancellation tokens through every new controller action.
- Added no authentication, OIDC, MFA, redirect URI, session, recovery, or database migration behavior.

# 0.11.0 - Controller API and Swagger

- Replaced Minimal API mappings with ASP.NET Core MVC controllers.
- Added Swagger/OpenAPI generation and Swagger UI.
- Added controller-based policy, policy-statement and group-policy-binding administration routes.
- Added a fail-closed administration authorization filter and default unavailable authorizer; no administrative mutation is exposed without an explicit trusted authorizer.
- Preserved server-side route resolution and PostgreSQL placement boundaries.
- Mapped policy optimistic-concurrency conflicts to HTTP 409.
- Kept cancellation explicit on all new asynchronous HTTP operations.
- Added controller, Swagger and fail-closed HTTP tests.

## 0.10.0 - 2026-09-21

### Added

- Added persisted `CapabilityPattern` support for the six whole-segment wildcard forms validated against the external RBAC engine.
- Added PostgreSQL migration `0006_persistent_wildcard_policy_patterns.sql`.
- Added database-side validation that exact statements reference a declared capability and wildcard statements match at least one declared capability in the pinned model.
- Added wildcard policy persistence and TRN-materialization tests without adding local wildcard evaluation.
- Added `verify-wildcard-policy-patterns.ps1` for live PostgreSQL validation.

### Changed

- Policy statements and assigned grants now carry a capability pattern instead of assuming every grant is concrete.
- TRN materialization accepts concrete and wildcard grant patterns while final authorization remains delegated to the external RBAC adapter.

### Compatibility

- `CapabilityKey` remains the concrete request/action type.
- Existing constructors accepting `CapabilityKey` remain available and are converted to concrete `CapabilityPattern` values.
- No external RBAC source or wildcard evaluator is copied into this repository.

## 0.9.0 - 2026-09-21

### Added

- Added `IdentityAccess.Authorization` as a neutral orchestration layer between routed identity data and the RBAC adapter.
- Added `IIdentityAuthorizationService`, `IdentityAuthorizationRequest`, and three-state authorization results.
- Added operation-scoped orchestration that resolves one immutable route, reads current assigned capabilities, materializes candidate TRNs, and delegates the final decision to `IRbacAuthorizationAdapter`.
- Added provenance validation that rejects cross-subject, cross-tenant, or cross-application grants before invoking RBAC.
- Added tests for allow, deny, technical failure, cancellation propagation, route stability, duplicate grant materialization, provenance integrity, and concurrent operation independence.

### Behavior

- Only an explicit RBAC denial becomes `Denied`. Routing, storage, projection, materialization, or adapter failures remain `TechnicalFailure`.
- Cancellation is propagated and is not converted to an authorization result.
- The service contains no mutable current-user, current-tenant, current-route, or current-permission state.
- Wildcard matching remains exclusively owned by the external RBAC engine; this service does not evaluate wildcard semantics.

### Limitations

- Persisted policy statements still represent concrete `CapabilityKey` assignments. Persisted wildcard policy patterns are not introduced in this version.
- No public HTTP authorization endpoint is added. Authentication and trusted application-context wiring remain separate work.

### Validation

- The external RBAC wildcard compatibility suite from the preceding increment was validated successfully against the real external engine.

# Changelog

## 0.8.0 - 2026-09-21

### Added

- Repository-owned neutral RBAC request and decision contracts in `IdentityAccess.Rbac`.
- Anti-corruption adapter in `IdentityAccess.Rbac.MultiplexedAdapter` with no compile-time dependency on the external RBAC projects.
- Canonical TRN materialization using `trn:{project}:{namespace}:{resource}:{feature}:{action}` without performing authorization locally.
- Dedicated external-engine integration suite covering exact grants, all six supported wildcard shapes, negative matches, partial wildcard rejection, context isolation and concurrent calls.
- External compatibility runner accepting an explicit RBAC `net10.0` output directory.
- Fifth append-only PostgreSQL migration aligning persisted capability columns with `resource / feature / action` terminology.

### Changed

- `CapabilityKey` now represents `Resource`, `Feature`, and `Action`; project and authorization namespace remain integration-context values.
- Local wildcard evaluation was removed. Wildcard semantics remain authoritative in the external RBAC `AuthorizationIndex` and `TrnAuthorizationEngine`.
- `Allowed`, `Denied`, and `TechnicalFailure` are distinct adapter outcomes.
- All adapter operations require an explicit non-optional `CancellationToken`.
- Source version advanced to 0.8.0.

### Security

- Domain and application projects do not reference external RBAC assemblies.
- Candidate TRNs are filtered to the requested project and namespace before external-context construction; wildcard meaning is not interpreted by the adapter.
- Missing or incompatible external RBAC binaries produce a technical failure instead of a business denial.
- Cancellation remains cancellation and is never converted to Allow/Deny.

### Concurrency

- Each adapter call creates an independent external authorization scope and execution-context instance.
- No mutable current-user, current-tenant, current-namespace or authorization cache is owned by the generic identity core.
- Dedicated external-engine tests execute concurrent contexts to detect cross-request permission contamination.

### Validation

- Main repository tests validate the adapter boundary without requiring external RBAC binaries.
- The wildcard compatibility matrix is intentionally executed by `scripts/verify-multiplexed-rbac.ps1` against the real external RBAC binaries.
- TypeScript validation remains unchanged from the preceding increment.

## 0.7.0 - 2026-09-21

### Added

- Provenance-preserving assigned-capability read contract for one subject, tenant and application boundary.
- PostgreSQL assignment projection across active users, memberships, groups, policies and policy statements.
- Fourth append-only migration with indexes supporting assignment traversal.
- Live PostgreSQL validation that excludes suspended group and policy paths.
- Explicit non-optional cancellation token on the new assignment-reader contract.

### Security

- Assigned capability rows are structural provenance only and are never returned as Allow/Deny decisions.
- No TRN grammar, wildcard, deny, inheritance or resource-level authorization behavior is invented.
- Scope, tenant and application boundaries are validated before opening a database connection.

### Concurrency

- Every projection call retains one immutable route snapshot and opens an independent logical connection.
- No mutable current-subject, current-tenant, current-application or current-database state is introduced.
- Duplicate capability keys retain separate provenance instead of being merged across assignment paths.

### Validation

- Added .NET contract and registration tests for assignment provenance and routing-boundary enforcement.
- Added transactional PostgreSQL validation for active versus suspended assignment paths.
- .NET execution must be validated in an environment with the .NET 10 SDK.

## 0.6.0 - 2026-09-21

### Added

- Versioned application security-model and capability domain contracts.
- Tenant/application-scoped permission policies with optimistic `row_version` concurrency.
- Immutable policy statements referencing declared capability-model versions.
- Exact group-to-policy binding contracts and PostgreSQL persistence.
- Third append-only migration for application models, capabilities, policies, statements and bindings.
- Live PostgreSQL validation for cross-scope policy independence and stale policy writes.
- Explicit cancellation-token requirements on the new permission/policy persistence contracts.

### Changed

- Server and package source version advanced to 0.6.0.
- PostgreSQL API registration now includes the permission/policy persistence stores.

### Security

- Capability structures are not treated as TRNs and stored bindings are not treated as authorization decisions.
- No wildcard, deny, inheritance or TRN grammar is invented before the actual RBAC engine contract is integrated.
- Scope, tenant and application boundaries are preserved in relational keys and domain validation.

### Concurrency

- Policy metadata uses compare-and-increment row versions.
- Every new asynchronous store operation receives and propagates an explicit `CancellationToken`.
- No mutable process-global authorization or database context is introduced.

### Validation

- TypeScript suite remains green with 26 passed tests and strict type checking.

## 0.5.0 - 2026-09-21

### Added

- PostgreSQL persistence stores for tenants, tenant memberships, user groups and group-membership edges.
- Operation-scoped application contracts for the complete initial directory persistence surface.
- Structural rehydration for persisted group-membership edges without treating persistence as authorization.
- Second append-only migration adding scope-leading directory query indexes.
- Live PostgreSQL validation for equal local identifiers across independent identity scopes.
- PostgreSQL scripts with explicit configurable database user, defaulting to `postgres`.

### Changed

- Server and package source version advanced to 0.5.0.
- The local schema script now applies all SQL migrations in lexical order.
- PostgreSQL API registration now includes all initial directory stores.

### Concurrency

- User, tenant, tenant-membership and user-group updates use optimistic `row_version` checks.
- Every store operation retains the supplied immutable route and opens an independent logical connection.
- Equal local identifiers in different identity scopes remain independent in keys, predicates and relational constraints.
- Group-membership edges are exact scoped inserts/deletes and do not use process-global mutable state.

### Security

- Persistence contracts remain server-side infrastructure and do not create public CRUD or authentication endpoints.
- Restoring a persisted group edge is explicitly not an authorization decision.
- Database placement remains resolved by the routing layer; stores cannot silently choose a different destination.

### Validation

- Additional .NET tests cover cross-scope rejection, store registration, persisted-edge restoration and embedded migration presence.
- A PostgreSQL transactional validation script covers full directory scope independence without leaving fixture data behind.
- .NET execution must be validated in an environment with the .NET 10 SDK.

## 0.4.0 - 2026-09-21

### Added

- Initial owned PostgreSQL schema under `identity_access` for users, tenants, tenant memberships, user groups and group memberships.
- Embedded append-only migration execution with a PostgreSQL advisory transaction lock.
- Explicit `row_version` optimistic-concurrency tokens on mutable identity records.
- First persistent user-directory adapter with scope-bound reads, creates and conditional updates.
- `IdentityConcurrencyException` for stale writes without exposing record or storage identifiers.
- Generic local development database convention: `generic_identity_access_default`.
- Local provisioning and SQL validation scripts for identity-scope isolation and stale-write rejection.
- Server registration for the schema migrator and user directory store when PostgreSQL infrastructure is explicitly enabled.

### Changed

- Server source version advanced to 0.4.0.
- The routing example now points generic application scopes to the configured `identity-default` destination.

### Concurrency

- Every persistence operation receives an immutable route snapshot and opens its own logical Npgsql connection.
- No mutable process-global current database, tenant or user state is introduced.
- Rows in different identity scopes remain independent even when local identifiers are equal.
- Stale writes fail through compare-and-increment `row_version` semantics instead of silently overwriting a committed update.
- Migration execution is serialized inside PostgreSQL rather than through a process-local lock.

### Compatibility

- `generic_identity_access_default` is a local configured destination and never a fallback route.
- Npgsql pooling remains bounded per destination; pooled physical connections do not carry application operation state.
- Database constraints complement, but do not replace, future RBAC enforcement.

### Limitations

- Full tenant, membership and group mutation repositories are not yet implemented.
- Schema migration is not automatically executed during API startup.
- Live PostgreSQL validation must be executed in an environment containing PostgreSQL client/server components.
- Authentication, OIDC, MFA and RBAC integration remain outside this version.

### Validation

- .NET restore, compilation and tests require execution in an environment with the .NET 10 SDK.
- The provided PostgreSQL validation script tests same-local-ID scope independence and stale-version rejection against `generic_identity_access_default`.

## 0.3.0 - 2026-09-21

### Added

- PostgreSQL infrastructure project using Npgsql 10.0.3.
- Server-only connection-secret resolver contract and environment-backed `env:` implementation.
- Bounded Npgsql data-source pools created lazily per registered destination.
- Operation-bound connection factory accepting an immutable resolved route snapshot.
- Fail-closed detection when one destination is rebound to a different secret reference in-process.
- Explicit PostgreSQL infrastructure configuration with bounded pool and timeout validation.
- Unit and API registration tests for configuration and secret-resolution behavior.

### Changed

- Server source version advanced to 0.3.0.
- The API may register PostgreSQL connection infrastructure explicitly; it remains disabled by default.

### Compatibility

- Routing remains authoritative for physical placement.
- Connection strings remain server-only and are resolved only after a trusted route snapshot exists.
- Enabling connection infrastructure does not imply authentication, authorization, schema availability or production readiness.

### Limitations

- No schema, migrations, repositories or persistent identity directory are added in this version.
- Only `env:` secret references are implemented; other secret stores require explicit adapters.
- No live PostgreSQL integration result is claimed until the two-database validation harness is executed.

### Validation

- .NET restore, compilation and tests require execution in an environment with the .NET 10 SDK and NuGet access.

## 0.2.0 - 2026-09-21

### Added

- Server-side routing contracts for application, identity scope and identity-directory placement.
- Immutable resolved destinations with configuration revision, route version and opaque secret references.
- Configuration-backed route resolver and registered pre-authentication directory locator.
- Strict JSON configuration validation, including duplicate properties, required fields,
  unsupported fields, bounded input size and catalogue cardinality limits.
- Destination and route uniqueness checks, referential validation and rejection of
  conflicting physical authorities for one identity scope.
- Explicit route, destination and authentication-context administrative disabling without fallback.
- Optional API startup registration from one server-selected file, with fail-fast validation.
- Synthetic configuration showing one application across two destinations and distinct scopes
  sharing one destination without implicit account merging.
- 87 additional declared .NET test cases for contracts, configuration, routing, bootstrap and API integration.

### Changed

- Server source version advanced to 0.2.0; public diagnostic contract version remains v1.
- Readiness removes only the database-routing blocker after successful file activation.
- Existing API tests explicitly disable externally selected routing configuration.
- Documentation records initial whole-directory placement and startup-only configuration lifetime.

### Compatibility

- Public DTOs and TypeScript client sources remain unchanged.
- Existing NuGet dependency versions remain unchanged; the routing provider adds no package dependencies.
- Physical placement remains separate from subject identity, tenant membership and authorization.
- External authorization-engine behavior, TRN grammar and context-rotation semantics are not modified or simulated.

### Limitations

- Routing does not open PostgreSQL connections, resolve secret values or provide persistence.
- Tenant data within one identity scope is not split across physical identity databases.
- Bootstrap lookup does not authenticate users or establish trust in HTTP or OIDC clients.
- Configuration changes require a controlled restart; no live reload or cross-instance propagation is provided.
- Route changes do not migrate data or provide durable configuration rollback protection.
- Authentication, OIDC, MFA, RBAC integration and production readiness remain unimplemented.

### Validation

- TypeScript compilation and strict type checking passed.
- 26 TypeScript transport tests passed with no skipped tests, including a local Node HTTP fixture.
- XML/JSON parsing, project references, dependency preservation and C# lexical delimiter checks passed.
- .NET restore, build, tests and live API smoke were not executed because the SDK was unavailable.
- PostgreSQL integration and Next.js application build were not executed.


## 0.1.0 - 2026-09-21

### Added

- Independent .NET 10 solution with domain, public contracts, application and API projects.
- Immutable scoped subject, tenant and application-specific group references.
- Structural membership validation and separate account, tenant, group and membership states.
- Explicit profile projections without credential or physical storage fields.
- Local diagnostic endpoints with non-ready service status and production startup rejection.
- TypeScript diagnostic HTTP transport with response validation, per-request cancellation,
  bounded timeout, redirect rejection and sanitized error categories.
- Server-only Next.js integration example, local verification scripts and contract smoke command.
- Domain, profile and API test sources plus executable TypeScript transport tests.

### Compatibility

- No direct or transitive source project reference to consuming application assemblies.
- No dependency on the .NET SDK or CLR contracts in the TypeScript package.
- No upper Node.js version restriction; TypeScript is compiled before execution.
- Application, identity scope, tenant and physical storage placement remain separate concepts.

### Limitations

- PostgreSQL routing, connections, migrations and persistence are not implemented.
- Authentication, OIDC, MFA, permissions, TRN generation and existing RBAC integration are not implemented.
- Scope assignment, account sharing, pre-authentication bootstrap and multi-database placement remain open decisions.
- The diagnostic client does not provide sessions, token refresh or access-context rotation.
- No production readiness or security qualification is asserted.

### Validation

- TypeScript compilation passed with compiler 5.8.3.
- 26 TypeScript transport tests passed on Node.js 22.16.0, with no skipped tests.
- One passing test used a real local HTTP fixture hosted by Node.js, not the .NET API.
- .NET restore, compilation, tests and live API smoke validation were not executed because the SDK was unavailable.
- Next.js application build and PostgreSQL integration were not executed.

## 0.8.1 - 2026-09-21

### Changed
- Replaced the local wildcard conformance evaluator with an explicit external-RBAC adapter boundary.
- Added `IdentityAccess.Rbac` as the repository-owned neutral authorization contract surface.
- Added `IdentityAccess.Rbac.MultiplexedAdapter` as an anti-corruption adapter with no compile-time dependency on external RBAC assemblies.
- Kept wildcard resolution authoritative in the external `TrnAuthorizationEngine` and `AuthorizationIndex`.
- Added a separate integration suite that executes exact and wildcard authorization against supplied external RBAC binaries.
- Preserved `Allowed`, `Denied`, and `TechnicalFailure` as distinct adapter outcomes.
- Required explicit `CancellationToken` parameters on authorization adapter calls.

### Compatibility
- `IdentityAccess.Domain`, `IdentityAccess.Application`, and `IdentityAccess.Rbac` do not reference `Multiplexed.*` assemblies.
- External RBAC binaries remain outside this repository and are supplied only to the dedicated compatibility test runner.


### 0.8.0 integration fix 2

- Corrected the reflection adapter to materialize the external `NamespaceEntry.Trns` property using its actual collection type instead of assigning a `List<string>` unconditionally.
- Preserved wildcard evaluation exclusively in the external RBAC engine.
### 0.10.1

- Made the local PostgreSQL migration runner track applied migration versions in `identity_access.schema_migrations` and skip them on subsequent runs.
- Made `0003_permission_policy_foundation.sql` compatible with both the pre-alignment `capability_namespace/resource/action` shape and the aligned `capability_resource/feature/action` shape when rerun against an existing development database.
- Prevented the local schema application workflow from failing after the RBAC capability-column alignment migration has already been applied.
