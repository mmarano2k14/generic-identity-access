# TOTP Provider

Version `0.44.0` adds the first concrete authentication-factor provider while preserving the provider-neutral MFA core.

## Boundary

`IdentityAccess.Mfa.Totp` owns TOTP generation, enrollment, verification, secret protection and provider persistence. The generic `identity_access.user_authenticators` table continues to contain lifecycle metadata only. Raw or encrypted TOTP material is not added to the generic authenticator row.

Provider-owned durable state is stored in `identity_access.totp_authenticators`. The secret is protected with ASP.NET Core Data Protection before persistence. The table stores the protected payload, the pinned algorithm parameters, and the last accepted time step used for replay prevention.

## Cryptographic profile

The initial profile is intentionally narrow:

- RFC 6238 time-based one-time passwords;
- HMAC-SHA1;
- 6 decimal digits;
- 30-second period;
- 20-byte cryptographically random enrollment secret;
- configurable verification window from 0 to 2 adjacent steps, default 1.

The parameters are persisted per authenticator even though this release accepts only the initial fixed profile. A future provider version can migrate explicitly instead of silently changing the meaning of existing authenticators.

## Enrollment

Enrollment creates the generic Pending authenticator and provider-owned protected secret in one PostgreSQL transaction. The returned Base32 secret and `otpauth://` URI are intended to be shown only during enrollment and must not be logged or persisted by the caller.

The first valid code confirms the authenticator. Confirmation consumes its accepted time step and atomically transitions the generic authenticator to Active.

## Verification and replay protection

A successful verification is not only a mathematical TOTP match. The provider opens a PostgreSQL transaction, locks the generic and provider rows, and accepts the proof only when its time step is strictly newer than `last_accepted_time_step`.

The accepted time step and generic `last_used_at` are updated in the same transaction. Two concurrent requests presenting the same valid code therefore cannot both succeed.

## Configuration

The provider is disabled by default.

```json
{
  "IdentityAccess": {
    "Mfa": {
      "Totp": {
        "Enabled": "true",
        "Issuer": "Generic Identity Access",
        "AllowedClockSkewSteps": "1"
      }
    }
  }
}
```

TOTP registration fails closed when the identity routing, database connection, security audit, generic MFA provider registry, or shared time provider services are unavailable.

## Verification

Primary repository verification remains:

```powershell
.\scripts\verify.ps1
```

The PostgreSQL schema can additionally be checked after applying migrations:

```powershell
.\scripts\postgresql\verify-totp-provider.ps1
```

The release is not considered GREEN until the repository verification is executed in the target .NET 10 environment.
