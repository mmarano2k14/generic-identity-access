# Validation

## Standard .NET Verification

Run the complete .NET verification from the repository root:

```powershell
.\scripts\verify.ps1
```

The script performs restore, Release build, and .NET tests with exit-code checking.

Equivalent commands:

```powershell
dotnet restore IdentityAccess.sln
dotnet build IdentityAccess.sln -c Release --no-restore
dotnet test IdentityAccess.sln -c Release --no-build --no-restore
```

## TypeScript Client

```powershell
cd clients\typescript
npm install
npm test
npm run typecheck
```

The TypeScript build output under `clients/typescript/dist` is generated content and is not
part of the source manifest.

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
.\scripts\postgresql\verify-permission-persistence.ps1
.\scripts\postgresql\verify-assigned-capability-projection.ps1
.\scripts\postgresql\verify-rbac-capability-alignment.ps1
.\scripts\postgresql\verify-wildcard-policy-patterns.ps1
.\scripts\postgresql\verify-authentication-foundation.ps1
.\scripts\postgresql\verify-resource-scope-hierarchy.ps1
.\scripts\postgresql\verify-atomic-mutations.ps1
.\scripts\postgresql\verify-security-audit.ps1
```

`verify-migration-integrity.ps1` should run immediately after schema application so later
live tests execute against a migration set whose names and SHA-256 checksums have already
been validated.

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
