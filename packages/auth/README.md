# `@generic-identity/auth`

Framework-neutral authentication and authorization SDK boundary for Generic Identity.

## Pack 3 status

Pack 3 activates this package as an **additive facade over the proven existing TypeScript client**.

```text
@generic-identity/auth
        |
        +--> @generic-identity/contracts
        |
        +--> @identity-access/client   (temporary extraction bridge)
```

The legacy client remains the runtime implementation in this pack. No source file is moved or deleted.

## Public responsibilities

```text
createIdentityClient
signIn
signOut
validateSession
isAllowed
createAuthorizationContext
RequireCapability
shared client error class/code
session credentials
bearer credentials
password-login/session/MFA authentication contracts
```

The package is framework-neutral and must not depend on React or Next.js.

## Authorization invariant

`isAllowed` and authorization contexts always delegate to the existing server-side .NET/RBAC boundary. This package does not parse TRNs and does not calculate permissions locally.

## Authentication invariant

Login, logout, session validation, password changes/recovery and MFA step-up delegate to the existing proven authentication client.

## Deliberately not invented in Pack 3

The roadmap mentions future convenience surfaces such as `getCurrentUser()` and `getEffectivePermissions()`. The current proven TypeScript client does not expose those operations, so Pack 3 does **not** fabricate new server semantics for them. They can be added only after a real backing contract/API exists.

## Ownership exclusions

This package does not own:

- React components or hooks;
- Next.js integration;
- administration pages;
- Generic Organization Directory;
- OrganisationProfile;
- PostgreSQL/Redis implementation details;
- a second RBAC, permission engine or TRN parser.

## Migration rule

The temporary dependency on `@identity-access/client` must be removed only by a later explicit source migration with `MOVED` / `DELETED` manifests and a full GREEN verification before cleanup.
