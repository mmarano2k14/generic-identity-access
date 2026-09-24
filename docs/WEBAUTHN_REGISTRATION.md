# WebAuthn / Passkey Registration Provider

Version `0.46.0` adds the registration half of the WebAuthn provider as the separate `IdentityAccess.Mfa.WebAuthn` project.

The generic MFA core remains provider-neutral. WebAuthn registration owns its challenge ceremony, protocol validation and public credential persistence. Authentication/assertion verification is implemented separately and documented in `WEBAUTHN_AUTHENTICATION.md`.

## Initial registration profile

The initial profile is intentionally conservative:

- `navigator.credentials.create()` semantics with `type = webauthn.create`;
- 32-byte cryptographic challenges with SHA-256 hash-only durable challenge storage;
- exact configured origin validation;
- RP ID hash validation;
- user presence required;
- user verification required;
- discoverable credential / resident key required;
- attestation conveyance `none`;
- `fmt = none` attestation validation;
- ES256 (`alg = -7`) with P-256 COSE public keys;
- no authenticator extensions in this increment;
- one-time, expiring registration challenge consumption.

The service does not implement a custom cryptographic signature protocol. Registration parses the WebAuthn structures required to validate the `none` attestation profile and validates the returned COSE public key as a P-256 public key.

## Persistent data

Migration `0017_webauthn_registration.sql` adds provider-owned tables:

```text
identity_access.webauthn_registration_challenges
identity_access.webauthn_credentials
```

The challenge table persists only a SHA-256 challenge hash, application binding, expiry and consumption state. The raw challenge is returned to the trusted caller and is not stored.

The credential table persists public registration material:

```text
credential_id
COSE public key
COSE algorithm
AAGUID
initial signature counter
backup eligibility / backup state
opaque user handle
```

A WebAuthn private key never enters Identity Access and is never persisted by the server. Private-key custody remains with the authenticator/passkey provider.

## Generic authenticator lifecycle

Registration creates a generic `UserAuthenticator` in `Pending` state. A valid and atomically accepted registration response transitions it to `Active` and sets `confirmed_at`.

The challenge row and generic authenticator row are locked together during completion. Replayed or expired challenges cannot activate a second credential. Credential identifiers are unique within an identity scope.

## User handle

The browser-facing WebAuthn user handle is a fixed 32-byte SHA-256 value derived from the identity scope identifier and user identifier. It is opaque, stable for the same scoped user, contains no login name or e-mail address, and is persisted with the public credential for later assertion validation.

## Configuration

The provider is disabled by default. When enabled, configure a relying-party identifier, display name and explicit allowed origins.

```json
{
  "IdentityAccess": {
    "Mfa": {
      "WebAuthn": {
        "Enabled": "true",
        "RelyingPartyId": "example.test",
        "RelyingPartyName": "Generic Identity Access",
        "AllowedOrigins": [
          "https://login.example.test"
        ],
        "ChallengeLifetimeSeconds": "300"
      }
    }
  }
}
```

Allowed origins must use HTTPS, except loopback/localhost HTTP origins for local development. Each origin host must equal or be a subdomain of the configured RP ID.

## Deliberate boundary for 0.46.0

This increment does not implement:

- signature-counter mutation after authentication;
- password-login or OIDC MFA orchestration;
- step-up authentication;
- lost-factor administration;
- attestation trust-chain formats other than `none`;
- WebAuthn extensions.

Those concerns remain separate so registration can be validated before authentication is added.
