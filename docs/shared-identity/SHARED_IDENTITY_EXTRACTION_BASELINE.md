# Shared Identity Extraction Baseline

## Status

Baseline and Structure establishes a non-functional-change baseline for extracting reusable Identity client and UI surfaces from the current repository.

No runtime implementation is moved in this delivery.
No existing public API is replaced in this delivery.
No database migration is introduced in this delivery.

## Current repository inventory

The baseline contains three established server-side areas under `src/`:

- Identity Access and authentication/authorization infrastructure;
- Generic Organization Directory;
- OrganisationProfile.

The current solution contains the following production projects:

```text
IdentityAccess.Api
IdentityAccess.Application
IdentityAccess.Authorization
IdentityAccess.Contracts
IdentityAccess.Domain
IdentityAccess.Infrastructure.Authentication
IdentityAccess.Infrastructure.ConfigurationRouting
IdentityAccess.Infrastructure.PostgreSql
IdentityAccess.Mfa.Recovery
IdentityAccess.Mfa.Totp
IdentityAccess.Mfa.WebAuthn
IdentityAccess.Rbac
IdentityAccess.Rbac.MultiplexedAdapter

OrganizationDirectory.Application
OrganizationDirectory.Contracts
OrganizationDirectory.Domain
OrganizationDirectory.Infrastructure.PostgreSql

OrganisationProfile.Application
OrganisationProfile.Domain
OrganisationProfile.Infrastructure.PostgreSql
```

The baseline also contains dedicated test/probe projects for Identity Access, Organization Directory and OrganisationProfile.

## Existing TypeScript client

The current reusable client remains authoritative at:

```text
clients/typescript
```

Current package identity:

```text
@identity-access/client
version 0.26.0
private: true
```

The package already contains focused class-based responsibilities for:

- system health and service information;
- authentication;
- OIDC;
- authorization;
- administration;
- typed administration sub-clients;
- `IdentityAuthorizationContext`;
- `RequireCapability` metadata;
- public contracts and stable client errors.

Baseline and Structure does not split this package. Later milestones will expose new public package boundaries around proven behavior before any legacy source is removed.

## Existing Next.js administration host

The current runnable consumer remains at:

```text
examples/nextjs/admin
```

Current package identity:

```text
@identity-access/admin-host
version 0.64.0
```

Current framework pins:

```text
Next.js      16.3.6
React        19.3.0
React DOM    19.3.0
TypeScript    5.8.3
```

It currently consumes the TypeScript client through:

```text
@identity-access/client = file:../../../clients/typescript
```

The host currently contains local administration pages, server-side integration services and the single administration stylesheet. Those files are extraction candidates, not disposable code.

## Protected existing subsystems

The shared Identity extraction must not absorb or casually relocate the following established subsystems:

```text
src/OrganizationDirectory.*
src/OrganisationProfile.*
tests/OrganizationDirectory.*
tests/OrganisationProfile.*
scripts/organization-directory/
scripts/organisation-profile/
docs/organization-directory/
docs/organisation-profile/
```

They remain independently owned boundaries while the shared Identity packages are introduced.

## Target public package structure

Baseline and Structure reserves the following package boundaries without adding runtime implementation:

```text
packages/
├── contracts/
├── auth/
├── react/
└── next/
```

Required dependency direction:

```text
contracts
   ↑
 auth
   ↑
 react
   ↑
 next
```

More explicitly:

```text
contracts -> no internal package dependency
auth      -> contracts only
react     -> contracts + auth
next      -> contracts + auth + react
```

Forbidden dependency direction:

```text
contracts -> auth/react/next
auth      -> react/next
react     -> next
```

## Extraction strategy

Every later extraction follows this order:

```text
1. Add the new public destination.
2. Adapt consumers to the new public destination.
3. Build and test.
4. Prove the old path has no remaining consumer.
5. Delete the old path only in a later explicit cleanup step.
6. Build and test again.
```

A ZIP overlay is never treated as a deletion mechanism. Any change requiring deletion must provide an explicit deletion list and cleanup command/script.

## Implementation sequence

The planned sequence is:

```text
1. Baseline and structure gates
2. Public contracts
3. Auth / Authorization SDK
4. React foundation
5. Shared Identity pages
6. Theme and visual overrides
7. Next.js integration
8. Consumer application integration
9. Security and end-to-end qualification
10. Packaging, versioning and release qualification
```

## Baseline and Structure exit criteria

Baseline and Structure is complete when:

- all current source locations still exist;
- the existing TypeScript client is unchanged and still authoritative;
- the existing Next.js administration host still consumes the existing client;
- the four future package boundaries exist only as documentation-only reservations;
- no source file has been moved or deleted;
- the shared-identity structure gate is part of the repository verification entry point;
- the complete pre-existing verification remains GREEN on the developer environment.
