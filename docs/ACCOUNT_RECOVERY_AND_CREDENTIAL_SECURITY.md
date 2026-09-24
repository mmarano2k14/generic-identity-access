# Account Recovery and Credential Security Hardening

## Scope

Version `0.50.0` closes the planned backend/security sequence before the administration UI refinement phase. It adds a controlled self-service password-change path and recovery-code-backed account recovery without introducing a second reset-token subsystem or an external delivery channel.

The increment uses the existing trusted boundaries:

```text
registered authentication client
        |
        +-- active local session + current password -> self-service password change
        |
        +-- login identifier + active recovery code -> account recovery
```

Both successful paths replace the password and invalidate existing session continuity.

## Self-service password change

The authenticated endpoint is:

```text
POST /api/v1/authentication/clients/{clientId}/credentials/password/change
```

The caller supplies the exact local-session provenance in the existing session headers plus the current and replacement passwords in the request body. The service:

1. revalidates the persisted session;
2. evaluates current session assurance;
3. when the application MFA policy is `Required`, requires fresh MFA within `SensitiveOperationMfaMaxAgeMinutes`;
4. re-resolves the registered authentication directory and exact scope/application binding;
5. verifies the current password;
6. rejects direct reuse of the current password;
7. replaces the password using optimistic concurrency;
8. revokes every active local session and unrevoked OIDC refresh token for the subject.

The successful password mutation and revocations are one PostgreSQL statement. A stale credential row version cannot revoke sessions because both revocation CTEs depend on the successful password update.

## Recovery-code-backed password reset

The pre-authentication recovery endpoint is:

```text
POST /api/v1/authentication/clients/{clientId}/recovery/password
```

The request contains:

```text
login identifier
recovery code
new password
```

No authenticator identifier is required from the user. The provider resolves the one active recovery-code authenticator already enforced by the provider schema.

The recovery flow deliberately reuses the existing high-entropy single-use recovery codes instead of adding an emailed/SMS reset token. It:

1. resolves only the registered authentication client and its trusted directory route;
2. locates the password credential by normalized login identifier;
3. requires the current MFA policy to allow the recovery provider;
4. rejects direct reuse of the current password;
5. SHA-256 hashes the supplied recovery code in memory;
6. locks the user, password credential, active recovery authenticator, set, and matching code row;
7. consumes the code exactly once;
8. replaces the password and clears lockout state;
9. revokes all active local sessions;
10. revokes all unrevoked OIDC refresh tokens with `account_recovery` as the categorical reason;
11. commits the mutations atomically.

Unknown accounts, wrong/consumed codes, unavailable authenticators, and inactive accounts do not receive distinct public account-specific errors. Raw recovery codes and passwords are never written to audit records.

## Refresh-token revocation reasons

Migration `0020_credential_security_hardening.sql` expands the bounded refresh-token revocation-reason constraint to:

```text
reuse_detected
credential_changed
account_recovery
```

These are categorical reasons only; no credential material is stored in the field.

## Password replacement rules

Password replacement currently enforces:

```text
minimum length: 12 characters
maximum length: 256 characters
direct current-password reuse: rejected
```

This increment does not claim historical password-reuse prevention across older password versions because no password-history store is introduced.

## Enumeration and disclosure boundary

Recovery requests never scan all configured databases. The registered client determines the authentication context and the trusted route. Invalid account/recovery-factor states converge on the same public failure response. Unknown credential paths still perform password/recovery hashing work to reduce trivial timing differences.

The service does not claim that application-level network timing is perfectly indistinguishable under every deployment topology; rate limiting, abuse detection, edge controls, and notification policy remain deployment responsibilities.

## Audit

New semantic audit categories cover:

```text
PasswordChangeRejected
PasswordRecoverySucceeded
PasswordRecoveryFailed
```

New categorical reasons include:

```text
RecentAuthenticationRequired
PasswordReuseRejected
```

Audit payloads contain identifiers and categories only, never raw passwords, password hashes, session tokens, refresh tokens, or recovery codes.

## Configuration

`IdentityAccess:Authentication:SensitiveOperationMfaMaxAgeMinutes` controls the maximum MFA age accepted for sensitive credential operations when the current application policy requires MFA.

Default:

```text
10 minutes
```

Allowed range:

```text
1 through 60 minutes
```

This setting is independent from the OIDC `MfaMaxAgeMinutes` policy.

## Validation

The source-consistency gate is:

```powershell
.\scripts\verify-credential-security-source-consistency.ps1
```

After migrations through `0020` are applied, the live schema gate is:

```powershell
$env:PGPASSWORD = "<password>"
.\scripts\postgresql\verify-credential-security-hardening.ps1
Remove-Item Env:PGPASSWORD
```

The complete repository gate remains:

```powershell
.\scripts\verify.ps1
```

## Explicit boundary after 0.50.0

`0.50.0` is the final planned backend/security milestone in the current Identity Access foundation sequence. Once it is GREEN, the next planned work is the reusable administration/login/security UI and UX improvement phase. Any defect discovered by the repository gate remains a `0.50.x` corrective patch rather than a new architectural backend milestone.
