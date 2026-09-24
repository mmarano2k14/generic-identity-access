# Authentication Session Lifecycle

Local authentication sessions are bound to the current account lifecycle rather than only
to the state captured at login time.

## Session Issuance

Session issuance is performed through:

```text
IAuthenticationSessionStore.CreateForActiveUserAsync
```

The PostgreSQL implementation inserts the session with an `INSERT ... SELECT` predicate
that requires the current user row to be active.

This closes the race where a user could be suspended after a separate user-state read but
before session insertion.

## Session Validation

Session validation joins the current `users` row and requires:

```text
UserStatus.Active
```

in addition to the existing session checks:

- identity scope;
- session id;
- client id;
- token hash;
- not revoked;
- not expired.

A session that was valid when issued becomes invalid immediately after the persisted user
is suspended.


## Authentication assurance

Version `0.49.0` extends the durable session with non-secret authentication assurance. New password sessions begin as `PasswordOnly` with `pwd`. Successful TOTP, recovery-code, or WebAuthn step-up can atomically upgrade the exact active session to `MultiFactor` with a verification timestamp and method references.

The upgrade is serialized with `FOR UPDATE` and revalidates subject, client, application, authentication context, current user status, expiry, and revocation before mutation. Raw authentication proofs are never persisted in session assurance.

OIDC consumes this state through `IAuthenticationAssuranceService`; see `MFA_SESSION_ASSURANCE_OIDC.md`.

## User Suspension

Updating a user to a non-active status revokes active subject sessions in the same
PostgreSQL command as the user status update.

Reactivating the user does not restore previously revoked sessions.

## Password Changes

Password change updates the credential and revokes all active sessions for the subject in the same PostgreSQL statement. Version `0.50.0` also revokes every unrevoked OIDC refresh token for the subject with categorical reason `credential_changed`.

A stale credential version does not revoke sessions or refresh tokens because both revocation paths are conditional on a successful credential update. Self-service change re-authenticates the current password and requires recent MFA when the current application policy is `Required`.

Recovery-code account recovery performs code consumption, password replacement, lockout reset, local-session revocation, and refresh-token revocation with reason `account_recovery` inside one PostgreSQL transaction.

## Administrative Revocation

`ISessionAdministrationService` supports:

```text
revoke all sessions for a user
revoke all sessions for a registered client
```

The HTTP administration API exposes:

```text
DELETE /api/v1/identity-scopes/{identityScopeId}/applications/{applicationKey}/sessions/users/{userId}

DELETE /api/v1/identity-scopes/{identityScopeId}/applications/{applicationKey}/sessions/clients/{clientId}
```

Both routes require the `identity-access / session / write` administration capability.

User-wide revocation applies across registered clients and applications inside the resolved
identity scope.

Client-wide revocation is limited to the requested application route and validates that the
client is registered for that application.

## Concurrency

The lifecycle guarantees do not rely on process-local locks.

PostgreSQL predicates and mutation statements remain authoritative under concurrent:

- login versus user suspension;
- password change versus active sessions;
- session validation versus account suspension;
- bulk revocation versus session validation.

## Verification

The repository includes:

```powershell
.\scripts\postgresql\verify-session-lifecycle.ps1
```

The live SQL fixture runs inside a transaction and rolls back all validation data.
