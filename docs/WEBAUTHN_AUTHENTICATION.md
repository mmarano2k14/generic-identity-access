# WebAuthn / Passkey Authentication Provider

Version `0.47.0` adds assertion authentication to the existing `IdentityAccess.Mfa.WebAuthn` provider.

The generic MFA core remains provider-neutral. WebAuthn owns browser challenge construction, client-data validation, authenticator-data validation, ES256 assertion verification, signature-counter handling, backup-state tracking, and provider-owned persistence.

## Boundary

```text
Generic MFA Core
        |
        v
WebAuthn Provider
        |
        +-- Registration (0.46.x)
        |
        +-- Authentication (0.47.0)
                |
                +-- hash-only challenge state
                +-- credential lookup
                +-- clientDataJSON validation
                +-- authenticatorData validation
                +-- RP ID hash validation
                +-- UP / UV enforcement
                +-- ES256 assertion verification
                +-- signature-counter mutation
                +-- backup-state mutation
                +-- one-time challenge consumption
```

WebAuthn private keys remain entirely outside Identity Access. Authentication uses only the credential identifier and public COSE key persisted during registration.

## Public service

`IWebAuthnAuthenticationService` exposes two operations:

```text
BeginAuthenticationAsync(...)
CompleteAuthenticationAsync(...)
```

`BeginAuthenticationAsync(...)` creates a cryptographically random 32-byte challenge, persists only its SHA-256 hash, binds the challenge to identity scope, user, application and expiry, and returns browser-facing request options.

The returned options require user verification and list the currently active credential identifiers for the user. The server does not trust the browser list when completing an assertion; credential ownership is checked again against provider-owned persistence.

## Assertion verification profile

The initial authentication profile requires:

- `clientDataJSON.type == "webauthn.get"`;
- challenge binding through SHA-256 and fixed-time comparison;
- an exact configured origin;
- `crossOrigin == false` when present;
- no `topOrigin` field;
- exact RP ID hash binding;
- User Presence (`UP`) set;
- User Verification (`UV`) set;
- no attested credential data in an assertion;
- no authenticator extensions in this initial profile;
- stable Backup Eligibility (`BE`) matching the registered credential;
- Backup State (`BS`) only when the credential is backup eligible;
- ES256 / P-256 public-key signature verification using the registered COSE public key.

The optional response `userHandle`, when supplied by the browser, must match the opaque 32-byte user handle persisted during registration.

## Signature counter

The credential's durable `sign_count` is updated only after a cryptographically valid assertion and successful transactional state mutation.

The initial policy is conservative:

```text
stored = 0 and asserted = 0
    -> accepted as a counterless authenticator profile

stored != 0 or asserted != 0
    and asserted <= stored
    -> replay / counter regression

asserted > stored
    -> accepted and persisted
```

A non-increasing non-zero counter is surfaced as replay detection. This is a provider security signal and is not silently converted into success.

## Backup state

Backup eligibility is treated as a stable credential property and must match the value recorded at registration. Backup state may change over time and is updated after a successful assertion.

## Atomic completion

The PostgreSQL completion path locks:

```text
webauthn_authentication_challenges
user_authenticators
webauthn_credentials
```

in the same transaction using `FOR UPDATE`.

The transaction rechecks application binding, expiry, challenge consumption, active authenticator state, backup eligibility and signature-counter monotonicity before it:

1. updates `user_authenticators.last_used_at`;
2. updates `webauthn_credentials.sign_count` and `backup_state`;
3. marks the authentication challenge consumed.

A challenge can therefore succeed at most once under concurrency.

## PostgreSQL migration

Migration `0018_webauthn_authentication.sql` adds:

```text
identity_access.webauthn_authentication_challenges
```

Only the SHA-256 challenge hash is stored. The raw challenge is returned to the caller and is never persisted.

No private key, provider secret, raw challenge or assertion signature is stored by this migration.

## Provider capability

The WebAuthn provider now declares:

```text
Enrollment | Verification
```

Registration and authentication remain provider-specific contracts rather than capabilities implemented inside the generic MFA core.

## Configuration

Authentication reuses the WebAuthn provider configuration introduced for registration:

```json
{
  "IdentityAccess": {
    "Mfa": {
      "WebAuthn": {
        "Enabled": "true",
        "RelyingPartyId": "example.com",
        "RelyingPartyName": "Example Identity",
        "AllowedOrigins": [
          "https://login.example.com"
        ],
        "ChallengeLifetimeSeconds": "300"
      }
    }
  }
}
```

The provider remains disabled by default.

## Validation

Repository gate:

```powershell
.\scripts\verify-webauthn-authentication-source-consistency.ps1
```

Full verification:

```powershell
.\scripts\verify.ps1
```

PostgreSQL:

```powershell
$env:PGPASSWORD = "<local-postgres-password>"
.\scripts\postgresql\apply-default-schema.ps1
.\scripts\postgresql\verify-webauthn-authentication.ps1
Remove-Item Env:PGPASSWORD
```

## Deliberately deferred

This increment does not integrate WebAuthn into password-login orchestration, OIDC token issuance, step-up policy enforcement, lost-factor administration, or browser UI. Those belong to the integration and hardening increment rather than assertion cryptography itself.
