# Validation

## Standard .NET Verification

Run the complete .NET verification from the repository root:

```powershell
.\scripts\verify.ps1
```

The script performs TypeScript source-consistency, restores the pinned TypeScript development dependency when the local compiler is absent, runs client build/tests/typecheck, restores the pinned runnable Next.js administration-host dependencies when absent, runs host typecheck plus `next build`, then performs .NET restore, Release build, and .NET tests with exit-code checking.

Equivalent commands:

```powershell
dotnet restore IdentityAccess.sln
dotnet build IdentityAccess.sln -c Release --no-restore
dotnet test IdentityAccess.sln -c Release --no-build --no-restore
```

## TypeScript Client

```powershell
.\scripts\verify-typescript-source-consistency.ps1
cd clients\typescript
npm install --ignore-scripts --no-audit --no-fund --package-lock=false
npm test
npm run typecheck
```

The TypeScript build output under `clients/typescript/dist` is generated content and is not
part of the source manifest.

The TypeScript source-consistency gate also pins the class-only runtime architecture, typed administration method set, bounded core list APIs, route-aware `IdentityAccessAdminUiBuilder`, explicit tenant authorization context, and the runnable Next.js administration host. It verifies the class-based server-only login/OIDC lifecycle, HTTP-only cookie posture, pinned host framework versions, single-file CSS ownership, and the absence of `NEXT_PUBLIC_` security configuration. It rejects reintroduction of a functional client factory, a parallel administration runtime client class, or client-secret support.

For the Next.js administration module it additionally requires server-only class-based mutation orchestration, thin Server Action adapters, destructive session-revocation confirmation, loading/error state files, a real `/identity` overview, structured record-detail presentation, and exactly one custom stylesheet at `examples/nextjs/admin/styles/identity-access-admin.css`. CSS Modules, component-local style blocks, React inline style objects, and raw JSON record dumps are rejected. The central stylesheet must retain shared design tokens, automatic dark-mode support, and reduced-motion handling.

The same gate pins the `0.58.0` failure boundary: mutation services must not own transport-error presentation, Server Actions delegate safe classification to `IdentityAccessAdminFailurePresentation`, structured action state distinguishes recovery guidance, stale `409` responses require current-state reload, invalid protected sessions route back to sign-in, and session investigation must not infer state when audit evidence is technically unavailable.

Version `0.59.0` extends the same source-consistency gate for the final administration presentation boundary. It requires route-aware compact-navigation disclosure state, automatic mobile-menu close after navigation, a focusable skip target, mutation-dialog trigger focus restoration, explicit shared-field hint relationships, pending-state semantics on public authentication forms, and single-stylesheet forced-colors, increased-contrast, dynamic-viewport, and main-focus treatment.

## External RBAC Compatibility

Build the supported external RBAC distribution first, then run:

```powershell
.\scripts\verify-multiplexed-rbac.ps1 `
  -ReferenceDirectory "<path-to-external-rbac-release-directory>"
```

This suite validates exact and wildcard decisions against the external engine instead of
duplicating wildcard logic locally.

## PostgreSQL Verification

Set the PostgreSQL administrator password only for the current shell when required:

```powershell
$env:PGPASSWORD = "<postgres-password>"
```

Recommended sequence:

```powershell
.\scripts\postgresql\apply-default-schema.ps1
.\scripts\postgresql\verify-migration-integrity.ps1
.\scripts\postgresql\verify-default-database.ps1
.\scripts\postgresql\verify-directory-persistence.ps1
.\scripts\postgresql\verify-assigned-capability-projection.ps1
.\scripts\postgresql\verify-rbac-capability-alignment.ps1
.\scripts\postgresql\verify-authentication-foundation.ps1
.\scripts\postgresql\verify-resource-scope-hierarchy.ps1
.\scripts\postgresql\verify-atomic-mutations.ps1
.\scripts\postgresql\verify-security-audit.ps1
```

`verify-migration-integrity.ps1` should run immediately after schema application so later
live tests execute against a migration set whose names and SHA-256 checksums have already
been validated.

`verify-assigned-capability-projection.ps1` requires the managed-policy schema introduced by
migrations `0023` through `0025`. It performs a table preflight and reports the missing managed
tables with an instruction to run `apply-default-schema.ps1` when the local database has not
yet been advanced. The projection verifier does not apply schema mutations itself.

The historical `verify-permission-persistence.ps1` and `verify-wildcard-policy-patterns.ps1`
scripts remain in the repository only to preserve validation coverage for the retained legacy
schema. They are no longer production-qualification gates because runtime authorization and
administration are managed-policy only. Wildcard compatibility of the external RBAC engine is
validated by `verify-multiplexed-rbac.ps1`; managed policy statements themselves are selected
from concrete registered capabilities.

`verify-atomic-mutations.ps1` uses rollback-scoped fixtures and must not leave test rows in
the database.

`verify-security-audit.ps1` verifies the durable audit schema and rejects obvious
credential-, token-, connection-, or secret-bearing audit columns.

Remove the temporary password environment variable after validation:

```powershell
Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
```

## Validation Rules

- A test is reported as passed only after execution.
- Static source inspection is not a substitute for a .NET build.
- In-memory routing tests are not a substitute for live PostgreSQL verification.
- Compatibility tests against local emulation are not a substitute for the external RBAC suite.
- A successful login alone does not demonstrate tenant isolation or authorization correctness.
- A route resolution result does not demonstrate authorization.
- Migration integrity validation is required before relying on live PostgreSQL behavior.
- Test failures must not be converted into skipped or ignored production gates without documented justification.


## Identity-Scope Administration Authority

After migration application:

```powershell
$env:PGPASSWORD = "<postgres-password>"
.\scripts\postgresql\verify-identity-scope-authority.ps1
Remove-Item Env:PGPASSWORD
```

The validation transaction verifies active scope-administration grant projection and
fail-closed behavior when an authority group is disabled.


## Authorization Source Consistency

Before compiling the administration authorization boundary after an overlay or repository
migration:

```powershell
.\scripts\verify-authorization-source-consistency.ps1
```

This gate verifies that the API authorization helpers, project references, failure-code
surface, public authorization constructors, identity-scope authority contracts, and current
route test fixture are present as one coherent source set.


## Identity-Scope Authority Administration

```powershell
$env:PGPASSWORD = "<postgres-password>"
.\scripts\postgresql\verify-identity-scope-authority-administration.ps1
Remove-Item Env:PGPASSWORD
```

This transaction verifies atomic active-user/group membership creation and active
group/policy binding creation.


## Transactional Security Mutation Ledger

After migration application:

```powershell
$env:PGPASSWORD = "<postgres-password>"
.\scripts\postgresql\verify-transactional-security-audit.ps1
Remove-Item Env:PGPASSWORD
```

The fixture validates transaction co-commit, actor/correlation propagation, secret-safe
record keys, and append-only ledger behavior.


## External RBAC Compatibility Preflight

The external RBAC compatibility suite now performs a contract preflight before wildcard
tests.

The preflight validates:

```text
required binary presence
assembly identity
assembly SHA-256 fingerprints
required external types
required constructors
required writable properties
required methods and return types
```

A contract mismatch is a technical failure and must not be interpreted as authorization
denial.

The adapter binding is process-pinned after the first preflight; changing external binaries
requires a process restart.


## OIDC Source Consistency

The standard repository verification invokes:

```powershell
.\scripts\verify-oidc-source-consistency.ps1
```

This gate protects the OIDC contract, PKCE S256-only policy, authorization-code and rotating
refresh-token storage, active signing-key selection, multi-key JWKS publication, legacy
single-key compatibility, discovery/token controllers, family replay revocation, local-session
linkage, and migration-0012/0013 transactional-audit triggers from partial overlays.


### OIDC Signing-Key Rotation

The .NET test suite validates that:

```text
new JWTs are signed only by the configured active private key
JWT kid equals ActiveSigningKeyId
JWKS exposes the active key and retained validation keys
retained keys may be public-only
duplicate kid values fail closed
missing active-key membership fails closed
public-only active keys fail closed
legacy single-key configuration remains supported
legacy and multi-key configuration cannot be mixed
```

Signing-key rotation is process-pinned. A configuration or PEM change requires a process
restart; static source validation is not evidence that a production key rollover has been
operationally completed.

### OIDC Bearer Access-Token Validation

The .NET test suite validates that:

```text
valid RS256 access tokens bind to the current registered client/application
retained public keys validate access tokens issued before signing-key rotation
unknown kid values fail closed with no fallback key
tampered signatures fail
issuer and audience mismatches fail
expired tokens fail
client/application mismatches fail
Bearer credentials are not merged with IdentitySession headers
current local session/user state is revalidated for every Bearer administration request
revoked/expired/inactive session state invalidates Bearer authentication
identity-scope route mismatch invalidates Bearer authentication
unexpected validation/storage failures remain technical unavailability
```

Bearer validation introduces no migration beyond `0013`. Current session continuity uses the
existing `user_sessions` + `users` state and the trusted authentication-directory route.

## OIDC Authorization Code + PKCE and Refresh-Token Rotation

After migrations through 0013 are applied:

```powershell
$env:PGPASSWORD = "<postgres-password>"

.\scripts\postgresql\verify-oidc-authorization-code.ps1
.\scripts\postgresql\verify-oidc-refresh-token.ps1

Remove-Item Env:PGPASSWORD
```

The live fixtures validate one-time code consumption, current-session enforcement,
revoked-session rejection, refresh-token rotation, absolute family lifetime preservation,
consumed-token replay family revocation, and secret-safe transactional-ledger capture.

## Production Qualification

Version 0.41.0 adds a consolidated release-candidate gate:

```powershell
$env:PGPASSWORD = "<postgres-password>"

.\scripts\verify-production-qualification.ps1 `
  -RbacReferenceDirectory "D:\Dev\Personal\multiplexed-rbac\implementations\dotnet\src\Multiplexed.Rbac.Core\bin\Release\net10.0"

Remove-Item Env:PGPASSWORD
```

Run it against a dedicated qualification database, not a live production database. The gate runs
repository verification, external RBAC compatibility, all current PostgreSQL validation scripts,
and a disposable `pg_dump` / `pg_restore` cycle by default.

For fast local iteration only:

```powershell
.\scripts\verify-production-qualification.ps1 `
  -RbacReferenceDirectory "<path>" `
  -SkipBackupRestore
```

Using `-SkipBackupRestore` is explicitly partial qualification and must not be reported as restore
validation.

Backup/restore can also be run independently:

```powershell
.\scripts\postgresql\verify-backup-restore.ps1
```

See `docs/PRODUCTION_QUALIFICATION.md` for prerequisites, cleanup behavior, restart/secret
operations, and the distinction between repository qualification and deployment-specific evidence.

## Generic MFA provider foundation

Static provider-boundary validation is included in the primary repository gate:

```powershell
.\scripts\verify-mfa-source-consistency.ps1
```

Migration `0014_mfa_provider_foundation.sql` must remain provider-neutral and transactionally audited.
The generic schema must not acquire TOTP secrets, WebAuthn provider payloads, recovery-code material,
private keys, or arbitrary provider blobs. Concrete provider releases add their own validation.

After applying the schema, validate the live PostgreSQL foundation with:

```powershell
$env:PGPASSWORD = "<password>"
.\scripts\postgresql\verify-mfa-provider-foundation.ps1
Remove-Item Env:PGPASSWORD
```

## TOTP provider validation

Version `0.44.0` adds `scripts/verify-totp-source-consistency.ps1` to the primary repository verification chain. It pins the concrete provider project, RFC 6238 implementation markers, provider-owned migration `0015_totp_provider.sql`, row-lock replay boundary, API host registration, and focused tests.

After applying PostgreSQL migrations, `scripts/postgresql/verify-totp-provider.ps1` checks the provider table, protected-secret storage type, generic-schema separation, and transactional mutation-trigger coverage.

## Recovery-code provider validation

Version `0.45.0` adds `scripts/verify-recovery-source-consistency.ps1` to the primary repository verification chain. The gate pins the separate provider project, high-entropy generator, SHA-256 hash-only persistence, provider-owned migration `0016_recovery_provider.sql`, active-set replacement, row-lock single-use consumption, API host registration, and focused tests.

After applying PostgreSQL migrations, run:

```powershell
$env:PGPASSWORD = "<password>"
.\scripts\postgresql\verify-recovery-provider.ps1
Remove-Item Env:PGPASSWORD
```

The live schema gate verifies the provider tables, `bytea` hash storage, absence of raw/protected code columns, absence of recovery-specific columns from the generic authenticator table, transactional mutation-trigger coverage, and the one-active-recovery-set partial unique index.


## WebAuthn registration provider validation

Version `0.46.0` adds `scripts/verify-webauthn-registration-source-consistency.ps1` to the primary repository verification chain. The gate pins the separate provider project, `webauthn.create` client-data validation, `none` attestation profile, ES256/P-256 COSE public-key validation, hash-only challenge persistence, row-lock single-use challenge consumption, API host registration, and focused tests.

After applying PostgreSQL migrations, run:

```powershell
$env:PGPASSWORD = "<password>"
.\scripts\postgresql\verify-webauthn-registration.ps1
Remove-Item Env:PGPASSWORD
```

The live schema gate verifies the registration-challenge and credential tables, `bytea` challenge-hash/public-key storage, absence of private-key columns, generic authenticator separation, and transactional mutation-trigger coverage.

## WebAuthn authentication provider validation

Version `0.47.0` adds `scripts/verify-webauthn-authentication-source-consistency.ps1` to the primary repository verification chain. The gate pins `webauthn.get` client-data validation, ES256 assertion-signature verification, RP ID and UP/UV checks, stable backup eligibility, signature-counter replay handling, atomic challenge consumption, provider Verification capability, migration `0018_webauthn_authentication.sql`, and focused tests.

After applying PostgreSQL migrations, run:

```powershell
$env:PGPASSWORD = "<password>"
.\scripts\postgresql\verify-webauthn-authentication.ps1
Remove-Item Env:PGPASSWORD
```

The live schema gate verifies hash-only authentication-challenge persistence, user binding, mutation-ledger coverage, and absence of raw challenge or private-key material.


## MFA integration and hardening validation

Version `0.48.0` adds `scripts/verify-mfa-integration-source-consistency.ps1` to the primary repository verification chain. The gate pins provider-policy enforcement across TOTP, recovery codes and both WebAuthn ceremonies, provider-neutral effective user MFA state, row-locked normal authenticator revocation, explicit lost-factor recovery revocation with session invalidation, API/TypeScript surfaces, Next.js state inspection, and focused regression tests.

Run the complete gate with:

```powershell
.\scripts\verify.ps1
```

The repository does not claim session-level MFA assurance, password-login step-up, or OIDC MFA/ACR enforcement in this increment. Those protocol interactions require a separately validated session-assurance contract.

## MFA session assurance and OIDC integration validation

Version `0.49.0` adds `scripts/verify-mfa-session-assurance-source-consistency.ps1` to the primary repository verification chain. The gate pins durable local-session assurance, row-locked exact-session upgrades, TOTP/recovery/WebAuthn step-up endpoints, Required-policy OIDC freshness enforcement, `interaction_required`, signed `auth_time`/`acr`/`amr` claims, pinned refresh assurance, TypeScript client support, migration `0019_session_authentication_assurance.sql`, and focused tests.

After applying PostgreSQL migrations, run:

```powershell
$env:PGPASSWORD = "<password>"
.\scripts\postgresql\verify-session-assurance.ps1
Remove-Item Env:PGPASSWORD
```

The complete repository gate remains:

```powershell
.\scripts\verify.ps1
```

`0.49.0` does not claim a generic declarative step-up requirement for every application operation. The session-assurance contract is reusable by such boundaries, while each consuming application still defines which non-OIDC operations require recent MFA.

## Account recovery and credential-security validation

Version `0.50.0` adds `scripts/verify-credential-security-source-consistency.ps1` to the primary repository verification chain. The gate pins recent-MFA self-service password replacement, current-password reuse rejection, recovery-code-backed password reset, generic public recovery failures, local-session and refresh-token invalidation, migration `0020_credential_security_hardening.sql`, TypeScript client support, and focused source/schema contract tests.

After applying PostgreSQL migrations, run:

```powershell
$env:PGPASSWORD = "<password>"
.\scripts\postgresql\verify-credential-security-hardening.ps1
Remove-Item Env:PGPASSWORD
```

The complete repository gate remains:

```powershell
.\scripts\verify.ps1
```

The `0.50.0` verification contract covers credential-security behavior that later administration and authentication surfaces must preserve. A repository result is not GREEN when these backend guarantees fail.


## 0.51.0 administration UI/UX gate

Version `0.51.0` keeps administration UI validation inside the existing TypeScript/Next.js verification chain rather than creating a parallel frontend gate.

`scripts/verify-typescript-source-consistency.ps1` now additionally requires:

- active-route navigation through `AdminNavigationActiveLink` with `aria-current`;
- current-workspace top-bar context through `AdminCurrentSection`;
- compact mobile navigation in the protected administration layout;
- a skip target for keyboard navigation;
- a dedicated public `/recovery` page, recovery form, and Server Action;
- host-side use of `IdentityAccessAuthenticationClient.recoverPasswordWithCode(...)`;
- no browser-supplied recovery authenticator identifier;
- the single shared stylesheet to own active-route, mobile-navigation, password-visibility, and recovery presentation.

The normal `scripts/verify.ps1` path continues to run the TypeScript source-consistency gate, client tests/typecheck, and a production Next.js build. No frontend-only success should be described as repository GREEN until the complete verification command succeeds.


## 0.60.0 application security-manifest validation

Version `0.60.0` adds `scripts/verify-security-catalog-source-consistency.ps1` to the primary repository verification chain. The gate pins the following architectural invariants:

- `CapabilityKey` remains exactly `resource / feature / action`;
- RBAC project and namespace remain context and are not added to the capability key;
- `RbacTrnCompiler` retains `trn:{project}:{namespace}:{resource}:{feature}:{action}`;
- manifest-backed catalog registration persists project, namespaces, normalized fingerprint, and concrete capabilities;
- catalog registration does not evaluate RBAC authority;
- the application-security controller remains protected through the centralized administration-capability boundary.

The TypeScript source-consistency gate additionally requires the focused security-model list/get/register client, separate Policy Builder read and mutation services, catalog-backed statement selection, and a read-only Security Models workspace. It rejects free-text exact capability coordinates in the Policy Builder and rejects browser-side authorization evaluation.

Migration `0021_application_security_manifest_catalog.sql` extends the PostgreSQL model with manifest registration provenance and allowed RBAC namespaces. The complete Windows/.NET verification remains:

```powershell
.\scripts\verify.ps1
```

A source-consistency pass or TypeScript-only pass is not a substitute for the complete .NET 10 and live-environment validation configured by the repository.


Version `0.60.5` extends TypeScript source-consistency validation for relationship reference UX. The gate requires the shared `AdminEntityAutocomplete`, the focused `IdentityAccessAdminEntityReferencePresentation` mapper, and autocomplete usage for the covered editable foreign identifiers while allowing opaque technical IDs to remain text input. It also pins the new bounded membership and identity-scope authority list client methods used to populate those selectors.

## 0.60.6 administration reference search validation

Version `0.60.6` pins server-backed relationship autocomplete behavior. The TypeScript source-consistency gate requires a three-character minimum, bounded results, debounce/cancellation, a focused server-only search class, the thin `/api/identity/entity-references` route, and explicit Tenant + User selection for tenant-membership creation. It rejects preloaded `options={...}` usage on `AdminEntityAutocomplete` and raw editable administrable foreign-ID fields.

The public TypeScript client exposes optional bounded `search` on administration list options; client tests verify query encoding and reject terms shorter than three or longer than 128 characters before transport. PostgreSQL migration `0022_administration_search_indexes.sql` supports the display-name and resource external-ID prefix searches. Complete Windows/.NET verification remains `./scripts/verify.ps1`; TypeScript-only validation is not repository GREEN.
## Group-as-Template and tenant-group qualification

Version `0.64.0` preserves checksum-protected migrations `0026_group_templates.sql` and `0027_group_template_flag_foundation.sql`, then adds append-only migration `0028_simplify_group_templates.sql`. Historical migrations must remain byte-compatible with databases that have already recorded their checksums. Run:

```powershell
$env:PGPASSFILE = (Resolve-Path .\scripts\postgresql\.pgpass.local).Path
psql -U postgres -d generic_identity_access_default -c "SELECT current_database(), current_user;"
.\scripts\postgresql\apply-default-schema.ps1
.\scripts\postgresql\verify-group-as-template.ps1
.\scripts\verify-group-as-template-source-consistency.ps1
```

The PostgreSQL gate proves that `identity_access.group_templates`, `origin`, and `template_id` are retired and that `user_groups.is_template` is present and constrained. Migration `0028` removes only the known artificial development template instances through explicit relationship cleanup and fails rather than silently discarding unexpected legacy global template definitions.

The source gate pins the unified `UserGroup` model, explicit `Create from template` path, target-tenant delegation checks, managed-policy-only cloning, absence of membership cloning, scope-authorized reusable-definition mutation, unified TypeScript group client, and removal of the separate `Available group templates` UI catalogue.

## Administration end-to-end qualification

The complete administration qualification combines repository verification, external RBAC compatibility, live PostgreSQL validation, backup/restore validation, and real-browser evidence.

Run:

```powershell
.\scripts\verify-administration-qualification.ps1 `
  -RbacReferenceDirectory 'D:\Dev\Personal\multiplexed-rbac\implementations\dotnet\src\Multiplexed.Rbac.Core\bin\Release\net10.0' `
  -Configuration Release
```

Before the browser portion continues, keep the API running on `http://127.0.0.1:5080` and the Next.js administration host running on `http://127.0.0.1:3000`. Use `127.0.0.1` consistently with the registered OIDC callback.

The browser evidence covers OIDC sign-in, tenant/member counts, empty-tenant creation, one-table `Template Yes/No` group presentation, `Create from template`, safe member addition, `Manage groups`, tenant isolation, identity-scope-only reusable-definition mutation, managed authorization ALLOW/DENY, Sessions, MFA, and logout.

A run using `-SkipBackupRestore` or `-SkipBrowserQualification` is partial and must not be described as complete qualification.

## Unified reusable-group model qualification

The current group model requires browser evidence that:

- Groups are presented through one real-group list with `Template Yes/No`;
- `Create from template` creates a normal target-tenant group;
- compatible managed-policy bindings are copied;
- source memberships are not copied;
- only identity-scope administration can promote, demote, or mutate reusable group definitions;
- tenant delegation remains bounded by the caller's authority in the target tenant.

The full repository gate remains:

```powershell
.\scripts\verify.ps1 -Configuration Release
```

Then run the PostgreSQL gates and `scripts/verify-administration-qualification.ps1` against the migrated local database. A source-only, frontend-only, or TypeScript-only pass is not a complete repository result.
