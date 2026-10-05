# Shared Identity Public Contracts — Public Contracts

## Objective

Activate `@generic-identity/contracts` as the first public Shared Identity package boundary without moving or deleting proven source.

Public Contracts is deliberately additive.

## Architecture decision

The new package is a type-only facade over the existing proven contract declarations:

```text
clients/typescript/src/contracts.ts
clients/typescript/src/admin-contracts.ts
clients/typescript/src/errors.ts
                  |
                  v
packages/contracts/src/index.ts
                  |
                  v
@generic-identity/contracts
```

This is an extraction bridge, not the final publication layout.

The bridge preserves the current implementation while establishing the package name and the first consumer-facing boundary. Contract ownership may move later only through an explicit migration with a deletion manifest and a second full verification.

## Package structure

The public facade is split by responsibility from the first active milestone:

```text
packages/contracts/src/
├── administration.ts
├── authorization.ts
├── errors.ts
├── identity.ts
├── mfa.ts
├── policies.ts
├── security-manifest.ts
├── session.ts
└── index.ts
```

This prevents a single contract file from becoming a future god-module while keeping one root import available.

## Included public families

```text
User
Tenant
TenantMembership
Group / GroupMember
ManagedPolicy / PolicyVersion / PolicyStatement
ManagedGroupPolicyBinding
CapabilityRequirement
ApplicationSecurityManifest
AuthorizationBoundary
AuthorizationEvaluationResponse
SessionValidation metadata
MFA provider/policy/authenticator metadata
ResourceScope
IdentityAccessErrorCode
```

Public Contracts preserves the existing proven TypeScript type names. It does not invent a second semantic model or rename established contracts merely for aesthetics.

## Security boundary

The passive package explicitly excludes secret-bearing material such as:

```text
IdentityAccessCredential
IdentityBearerCredential
IdentitySessionCredential
IdentityLocalSession
IdentityOidcTokenSet
password-bearing requests
```

Authentication material remains behind the existing client/authentication boundary.

## Ownership boundary

The package also excludes:

```text
Generic Organization Directory contracts
OrganisationProfile contracts
```

Those established subsystems remain independently owned.

## Compatibility strategy

The current TypeScript client remains:

```text
@identity-access/client
```

and the current Next.js administration host continues consuming it directly.

No existing import is redirected in Public Contracts.

This gives the repository two surfaces temporarily:

```text
legacy proven client       -> remains authoritative for behavior
public contracts package  -> new passive consumer boundary
```

There is no duplicate implementation because the new package only re-exports existing type declarations.

## Exit criteria

Public Contracts is complete when:

- Baseline and Structure structure validation remains GREEN;
- `@generic-identity/contracts` exists and is private;
- it has zero runtime dependencies;
- it exports only passive non-secret Generic Identity contracts;
- it does not export Organization Directory or OrganisationProfile contracts;
- a compile-only consumer imports the new package name successfully;
- the existing TypeScript client tests and typecheck remain GREEN;
- the existing Next.js host remains GREEN;
- the complete .NET solution remains GREEN;
- no source file is moved or deleted.
