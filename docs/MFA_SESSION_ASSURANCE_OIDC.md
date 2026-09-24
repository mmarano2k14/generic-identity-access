# MFA Session Assurance and OIDC Integration

## Scope

Version `0.49.0` records authentication assurance on the existing local session and carries that
assurance through the OAuth 2.0 / OpenID Connect authorization-code and refresh-token lifecycle.

The increment does not add a new MFA provider. TOTP, recovery codes, and WebAuthn remain independent
providers behind the generic MFA core.

## Session assurance

A newly password-authenticated local session starts with:

```text
level       = PasswordOnly
methods     = [pwd]
verified_at = password authentication time
acr         = urn:generic-identity-access:acr:password
```

After a concrete factor verifies successfully for the exact active local session, the session is
upgraded atomically:

```text
level       = MultiFactor
methods     = pwd + mfa + concrete factor method(s)
verified_at = most recent successful factor verification
acr         = urn:generic-identity-access:acr:mfa
```

Concrete method references used by this release are:

```text
TOTP       -> otp
Recovery   -> recovery
WebAuthn   -> pop
```

The session contains method references only. Passwords, TOTP secrets, recovery codes, WebAuthn
private keys, assertion signatures, and raw MFA proofs are never stored in session assurance state.

## Exact-session step-up

Authenticated local-session MFA endpoints bind a successful provider verification to the exact
session presented through the existing `IdentitySession` transport.

```text
POST /api/v1/authentication/clients/{clientId}/mfa/totp/{authenticatorId}/verify
POST /api/v1/authentication/clients/{clientId}/mfa/recovery/{authenticatorId}/verify
POST /api/v1/authentication/clients/{clientId}/mfa/webauthn/options
POST /api/v1/authentication/clients/{clientId}/mfa/webauthn/complete
```

A successful factor proof is not enough by itself. Before assurance is persisted, the server
re-resolves the trusted authentication directory and row-locks the session while checking the exact
identity scope, user, session id, client id, application, authentication-context key, session
lifetime, revocation state, and current active-user state.

An MFA proof for one user or session cannot upgrade another session.

## MFA policy and OIDC

OIDC authorization evaluates current persisted session assurance against the current generic MFA
policy.

```text
Disabled / Optional policy -> password-only assurance may authorize
Required policy            -> recent MultiFactor assurance is required
```

`IdentityAccess:Authentication:Oidc:MfaMaxAgeMinutes` controls the maximum age accepted for Required
MFA during authorization. The default is 15 minutes and the accepted configuration range is 1 to
1440 minutes.

If a valid local session exists but Required MFA is missing or stale, authorization fails with the
OIDC protocol error:

```text
interaction_required
```

The TypeScript OIDC client preserves this as a typed OIDC protocol error so the consuming application
can run a session-bound step-up operation and retry authorization.

## OIDC assurance pinning

When authorization succeeds, the current assurance is pinned into the durable authorization-code
grant and then into the refresh-token family.

ID tokens and access tokens include:

```text
auth_time
acr
amr
```

Refresh-token rotation preserves the assurance pinned to the family. A later factor verification on
the local session does not silently upgrade an already issued refresh-token family. A fresh OIDC
authorization is required when the caller needs a newly elevated OIDC grant.

Bearer validation reconstructs assurance from the signed claims and revalidates the source local
session. The current session must still carry at least the token's assurance level, verification time,
and method references. Session revocation or current-user ineligibility continues to invalidate
Bearer use through the existing session-continuity boundary.

## Persistence

Migration `0019_session_authentication_assurance.sql` adds bounded assurance state to:

```text
identity_access.user_sessions
identity_access.oidc_authorization_codes
identity_access.oidc_refresh_tokens
```

Existing rows are conservatively backfilled as password-only. Session rows additionally persist the
assurance verification timestamp. Authorization-code and refresh-token rows reuse their pinned
`authenticated_at` value as the assurance verification time.

The migration stores no authentication proof or provider secret.

## Public API and TypeScript client

Password-login and session-validation responses expose non-secret assurance metadata. The TypeScript
class client exposes:

```typescript
client.authentication.verifyTotp(...)
client.authentication.verifyRecoveryCode(...)
client.authentication.beginWebAuthnStepUp(...)
client.authentication.completeWebAuthnStepUp(...)
```

All step-up calls use the existing local-session header/token provenance and do not accept database
routing information from the caller.

## Security boundaries and limitations

This increment establishes durable session assurance and OIDC MFA enforcement. It does not claim a
generic declarative step-up attribute for every application controller or business operation. Such
operations can evaluate the session assurance contract, but which non-OIDC application actions
require recent MFA remains an application/security-policy decision.

Recovery authentication is recorded distinctly as `recovery`. It establishes multi-factor session
assurance only after the recovery provider has successfully consumed a valid single-use recovery
code under its own policy and storage guarantees.

## Validation

The repository verification chain includes:

```powershell
.\scripts\verify-mfa-session-assurance-source-consistency.ps1
```

After PostgreSQL migrations are applied, the live schema gate is:

```powershell
.\scripts\postgresql\verify-session-assurance.ps1
```

The complete repository gate remains:

```powershell
.\scripts\verify.ps1
```

The release is not considered GREEN until the complete gate succeeds in the target .NET 10
environment.
