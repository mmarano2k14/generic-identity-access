# @generic-identity/auth

Framework-neutral authentication, authorization and administration SDK for Generic Identity.

## Architecture

```text
@generic-identity/auth
        |
        +--> @generic-identity/contracts
        |
        +--> @identity-access/client   (compatibility transport)
```

The compatibility client remains the proven transport implementation. The public categorized SDK composes it rather than creating duplicate authentication, administration or authorization behavior.

## Public categories

The root client exposes:

```ts
identity.authentication
identity.authorization
identity.administration
identity.account
identity.directory
identity.organizations
identity.accessControl
identity.applicationSecurity
identity.security
```

Application Security provides categorized access to registered security models, manifest registration, scope types, capability catalogs and the effective administration context.

Security Operations provides categorized access to server-backed session revocation, MFA administration, authenticator lifecycle metadata and security audit.

## Security invariants

Authorization decisions remain server-side through the existing .NET/RBAC boundary. The SDK does not calculate permissions locally, does not treat UI visibility as authority and does not manufacture public TRN grant strings.

The structured Application Security permission reference validates registered model coordinates only. It is not a credential, access context or authorization result.

## Ownership exclusions

This package does not own React components, Next.js integration, database routing internals, PostgreSQL/Redis implementation details, a second RBAC engine or consumer-specific business semantics.
