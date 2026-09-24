# MFA Integration and Hardening

## Scope

Version `0.48.0` integrates the provider-neutral MFA policy with the concrete TOTP, recovery-code,
and WebAuthn providers and hardens generic authenticator administration.

This increment does not claim end-to-end password-login or OIDC step-up authentication. Session-level
MFA assurance and protocol interaction remain separate work because the current session model does not
persist a verified factor/assurance state.

## Provider policy enforcement

Concrete provider operations now evaluate the current generic MFA policy on the already resolved
identity database route before enrollment or verification work begins.

The guard is fail-closed:

```text
no policy configured -> reject provider operation
MFA Disabled         -> reject provider operation
provider not allowed -> reject provider operation
provider allowed     -> continue provider operation
```

The policy guard receives the server-resolved route. It rejects scope/application mismatches and does
not perform a second routing decision.

This integration applies to:

```text
TOTP enrollment / confirmation / verification
Recovery-code generation / replacement / verification
WebAuthn registration begin / complete
WebAuthn authentication begin / complete
```

Provider registration still does not grant authorization. Administration RBAC and resource/scope
checks remain independent.

## Effective user MFA state

Administration exposes a provider-neutral effective state derived from:

```text
current application MFA policy
+ active generic UserAuthenticator records
+ registered provider capabilities
```

The response identifies whether the policy is configured/required, whether active verification, primary, and recovery factors exist, which allowed providers are active, and whether the current enrollment set satisfies a Required policy. Recovery-capable providers remain fallback mechanisms and do not satisfy a Required policy on their own.

The state never returns provider-owned secret material.

Protected endpoint:

```text
GET /api/v1/identity-scopes/{scope}/applications/{app}/mfa/users/{user}/state
```

The TypeScript administration client exposes the same operation through
`administration.mfa.getUserSecurityState(...)`, and the reusable Next.js administration page can
inspect this state by stable user ID.

## Required-policy revocation invariant

Normal authenticator revocation is hardened against removing the final active primary verification factor when the application policy is `Required`. A recovery-capable provider does not count as that remaining primary factor.

The PostgreSQL store locks all generic authenticator rows for the target user with `FOR UPDATE`, then
checks target version and the remaining eligible active factors before changing lifecycle state.
Concurrent normal revocations for the same user therefore serialize at the generic authenticator
boundary.

The operation distinguishes:

```text
Succeeded
NotFound
VersionConflict
WouldViolateRequiredMfa
```

A policy-compliance failure is returned by the administration API as HTTP `409` rather than silently
weakening the configured requirement.

Policy updates are stored separately and are not claimed to share the same transaction as an
authenticator revocation. Deployment and future orchestration work must not overstate this boundary.

## Lost-factor recovery revocation

An explicit account-recovery administration path may revoke a lost authenticator even when it is the
last verification factor:

```text
POST /api/v1/identity-scopes/{scope}/applications/{app}/mfa/users/{user}/authenticators/{authenticator}/recovery-revoke?expectedVersion=N
```

This path is deliberately distinct from normal revocation. After the authenticator mutation succeeds,
it revokes active local sessions for the user through `ISessionAdministrationService`.

The authenticator mutation and session revocation are sequential security operations, not one
distributed transaction. The implementation therefore does not claim cross-store atomicity.

The TypeScript client exposes this operation as
`administration.mfa.revokeAuthenticatorForRecovery(...)`.

## Provider-owned state after generic revocation

Generic revocation does not delete provider-owned security records. Concrete verification paths read
or lock the generic authenticator lifecycle and reject non-active authenticators. Provider-specific
rows therefore remain available for audit/forensics without remaining usable as active factors.

## Validation

The repository verification chain includes:

```powershell
.\scripts\verify-mfa-integration-source-consistency.ps1
```

The gate pins:

- policy-guard registration and use by all concrete providers;
- provider-neutral effective user state;
- row-locked generic revocation protection;
- explicit recovery revocation plus user-session revocation;
- API and TypeScript administration surfaces;
- Next.js effective-state inspection;
- focused policy/state tests.

The complete repository gate remains:

```powershell
.\scripts\verify.ps1
```

A release is not considered GREEN until that command succeeds in the target .NET 10 environment.
