# Recovery-Code Authentication-Factor Provider

## Purpose

`IdentityAccess.Mfa.Recovery` is the concrete recovery-code provider for the generic MFA subsystem.
It is separate from the provider-neutral core and owns generation, one-time disclosure, hash-only
persistence, atomic consumption, set regeneration, and provider audit behavior.

```text
Generic MFA Core
    |
    +-- provider registry
    +-- policy
    +-- generic authenticator metadata
    |
    v
Recovery provider
    |
    +-- high-entropy code generation
    +-- one-time disclosure
    +-- SHA-256 code hashing
    +-- provider-owned PostgreSQL tables
    +-- atomic single-use consumption
    +-- active-set replacement
```

## Security profile

The initial provider profile is intentionally fixed:

```text
characters per canonical code: 16
alphabet size:                  32
entropy per code:               80 bits
human display:                  XXXX-XXXX-XXXX-XXXX
hash algorithm:                 SHA-256
code count:                     10 by default, configurable from 6 through 20
```

The alphabet excludes visually ambiguous `I`, `O`, `0`, and `1`. Verification is case-insensitive
and accepts the display hyphens as optional separators.

Recovery codes are already high-entropy random values. Only SHA-256 hashes of their canonical form
are persisted. Raw codes are returned only from `GenerateOrReplaceAsync(...)`; there is no provider
operation that reads raw codes back from storage.

## Generic authenticator boundary

The generic `identity_access.user_authenticators` row stores only:

```text
identity scope
user
provider = recovery
authenticator id
display name
Active / Revoked lifecycle
timestamps
row version
```

It never stores:

```text
raw recovery code
recovery-code hash
recovery-code list
provider JSON/blob
```

Provider-owned persistence is migration `0016_recovery_provider.sql`:

```text
identity_access.recovery_code_sets
identity_access.recovery_codes
```

`recovery_codes.code_hash` is `bytea` constrained to 32 bytes. No raw or reversibly protected code
column exists.

## Generation and replacement

`GenerateOrReplaceAsync(...)` creates an immediately active recovery-code authenticator because the
server itself generated the proof set; there is no external first-code confirmation step.

The PostgreSQL store locks the user row before changing recovery state. Within one transaction it:

1. revokes every currently active `recovery` authenticator for that user;
2. inserts the new active generic authenticator;
3. inserts the provider-owned set metadata;
4. inserts only SHA-256 hashes of the generated codes;
5. commits the replacement as one unit.

A provider-specific partial unique index also permits only one active recovery-code authenticator per
`identity_scope_id + user_id`.

Regeneration therefore invalidates every code from the old active set before the new set is visible.
Old rows remain available for audit/history until their generic authenticator is deleted according to
future retention policy.

## Verification and single-use consumption

Verification canonicalizes and hashes the supplied proof in memory. The raw proof is never written to
the database or security audit.

The store acquires row locks for the selected active set and then the matching recovery-code row.
Consumption succeeds only while `consumed_at IS NULL`. The same transaction:

```text
sets recovery_codes.consumed_at
increments recovery_code_sets.row_version
updates user_authenticators.last_used_at
commits
```

A second concurrent or later attempt with the same code observes the consumed row and returns
`AlreadyConsumed`; it cannot succeed a second time.

## Provider capabilities

The provider descriptor declares:

```text
Enrollment
Verification
Recovery
```

`Recovery` identifies a proof suitable for recovery flows. Version `0.50.0` uses the active recovery-code set for password recovery without adding a second reset-token format.

## Account recovery integration

Version `0.50.0` adds recovery-code-backed password replacement through the registered authentication-client route. The public request supplies only the login identifier, one recovery code, and the new password; the server resolves the user's one active recovery authenticator.

The PostgreSQL mutation locks the active set and matching code, consumes the code once, replaces the password, clears lockout state, revokes active local sessions, and revokes unrevoked OIDC refresh tokens in the same transaction. Invalid account, authenticator, and proof states are collapsed into one public recovery rejection.

See `ACCOUNT_RECOVERY_AND_CREDENTIAL_SECURITY.md`.

## Server registration

The API host registers the provider from:

```text
IdentityAccess:Mfa:Recovery
```

Example:

```json
{
  "IdentityAccess": {
    "Mfa": {
      "Recovery": {
        "Enabled": "true",
        "CodeCount": "10"
      }
    }
  }
}
```

The provider is disabled by default. When enabled, it requires the generic provider registry, route
resolver, PostgreSQL connection factory, semantic audit writer, and `TimeProvider` to have already
been registered.

## Audit

Semantic audit records only categorical metadata and identifiers. It never records a recovery code or
code hash.

The provider reuses the provider-neutral semantic categories already owned by the MFA core. Generation or replacement records `UserAuthenticatorEnrollmentConfirmed`, successful consumption records `AuthenticationFactorVerificationSucceeded`, and invalid proofs use `AuthenticationFactorVerificationFailed`. Reuse of an already consumed code uses the existing `AuthenticationFactorReplayDetected` reason. The `RecoveryCodeSet` result separately tells the trusted caller whether an older active set was replaced.

The provider tables also participate in the transactional security-mutation ledger.

## Validation

Primary repository verification includes:

```powershell
.\scripts\verify-recovery-source-consistency.ps1
```

After applying migrations through `0016`:

```powershell
$env:PGPASSWORD = "<password>"
.\scripts\postgresql\verify-recovery-provider.ps1
Remove-Item Env:PGPASSWORD
```

The full repository gate remains:

```powershell
.\scripts\verify.ps1
```

## Original 0.45.0 non-goals

This increment does not add:

```text
email/SMS recovery delivery
password-history storage
browser exposure of raw recovery-code storage
```

Later increments delivered MFA/OIDC/WebAuthn integration. The remaining entries above are still outside the provider boundary.
