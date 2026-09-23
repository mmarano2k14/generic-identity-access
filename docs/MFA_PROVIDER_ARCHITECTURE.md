# MFA Provider Architecture

## Purpose

The MFA subsystem is provider-neutral. The generic core owns policy, provider registration metadata,
authenticator lifecycle metadata, routing, authorization, concurrency, and audit. Concrete mechanisms
such as TOTP, recovery codes, and WebAuthn/passkeys are separate providers.

```text
Generic MFA Core
    |
    +-- MfaPolicy
    +-- AuthenticationFactorProviderRegistry
    +-- UserAuthenticator metadata
    +-- administration API / TypeScript client
    +-- PostgreSQL generic tables
    |
    +--------------------+--------------------+--------------------+
    |                    |                    |
    v                    v                    v
 TOTP provider       Recovery provider    WebAuthn provider
 implemented         implemented         later releases
```

## Core invariants

1. The core never calculates a TOTP value.
2. The core never parses or verifies a WebAuthn assertion.
3. The core never stores recovery-code material.
4. Generic `user_authenticators` rows contain lifecycle metadata only.
5. Provider secret or credential material lives in provider-owned storage contracts/tables.
6. A provider is identified by a stable `AuthenticationFactorProviderKey`.
7. Duplicate provider keys fail during registry construction.
8. MFA policy may reference only providers registered in the current host.
9. A disabled policy has no allowed providers.
10. An optional or required policy must allow at least one provider.
11. Provider registration never grants authorization.
12. UI visibility never replaces server-side authorization.

## Policy

`MfaPolicyMode` currently supports:

```text
Disabled
Optional
Required
```

The policy is scoped by:

```text
IdentityScopeId + ApplicationKey
```

Provider allow-list rows are normalized into `mfa_policy_providers` rather than serialized into a
provider-specific JSON document.

## Provider contract

All providers implement the minimal base contract:

```csharp
IAuthenticationFactorProvider
    -> AuthenticationFactorProviderDescriptor
```

The descriptor declares:

```text
provider key
human-readable display name
capabilities: Enrollment / Verification / Recovery
```

Operational enrollment and verification interfaces are intentionally not forced into the base
contract. Provider-specific releases add capability-specific contracts without expanding a giant
provider interface that every implementation must fake.

## Authenticator metadata

Generic metadata stores:

```text
identity scope
user
provider key
authenticator id
display name
lifecycle status
created / confirmed / last-used / revoked timestamps
row version
```

It deliberately does not contain:

```text
TOTP secret
WebAuthn public/private provider payload
recovery code or recovery-code hash
arbitrary provider JSON/blob
```

Provider-specific schema owns those values and may use stronger provider-specific invariants.

## Persistence

MFA is optional at host level. The provider registry may exist with zero concrete providers, and local
authentication does not require MFA administration persistence to be present. If one generic MFA
persistence store is registered, both policy and authenticator stores are required so the optional
administration feature cannot start in a partially configured state.

Migration `0014_mfa_provider_foundation.sql` adds:

```text
identity_access.mfa_policies
identity_access.mfa_policy_providers
identity_access.user_authenticators
```

All three participate in the transactional security-mutation ledger. The generic authenticator
ledger key contains only `identity_scope_id + authenticator_id`.

## Administration surface

Protected administration routes:

```text
GET  /api/v1/identity-scopes/{scope}/applications/{app}/mfa/providers
GET  /api/v1/identity-scopes/{scope}/applications/{app}/mfa/policy
POST /api/v1/identity-scopes/{scope}/applications/{app}/mfa/policy
PUT  /api/v1/identity-scopes/{scope}/applications/{app}/mfa/policy
GET  /api/v1/identity-scopes/{scope}/applications/{app}/mfa/users/{user}/authenticators
DELETE /api/v1/identity-scopes/{scope}/applications/{app}/mfa/users/{user}/authenticators/{authenticator}?expectedVersion=N
```

Capabilities:

```text
identity-access / mfa-policy        / read|write
identity-access / mfa-authenticator / read|write
```

## TypeScript

The class-composed client exposes:

```typescript
client.administration.mfa.listProviders(...)
client.administration.mfa.getPolicy(...)
client.administration.mfa.createPolicy(...)
client.administration.mfa.updatePolicy(...)
client.administration.mfa.listAuthenticators(...)
client.administration.mfa.revokeAuthenticator(...)
```

The root `IdentityAccessClient` remains a composition facade.

## Forward roadmap

```text
0.43.x  generic MFA foundation + provider registry
0.44.0  TOTP provider implemented as a separate provider project
0.45.0  recovery provider implemented as a separate provider project
0.46.x  WebAuthn/passkey registration provider work
0.47.x  WebAuthn/passkey authentication provider work
0.48.x  integration / administration / security hardening
```

The provider releases extend the generic foundation rather than modifying its ownership model.

## Concrete provider status

The TOTP provider is documented separately in `TOTP_PROVIDER.md`. Its provider-owned encrypted secret storage and replay state do not alter the generic authenticator schema.

The recovery-code provider is documented in `RECOVERY_PROVIDER.md`. Raw codes are returned only at generation time; only provider-owned SHA-256 hashes and consumption timestamps are persisted.
