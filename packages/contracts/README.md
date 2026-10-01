# @generic-identity/contracts

Pack 2 activates the first shared package boundary as a **type-only public contract bridge**.

The goal is to let future consumers compile against a stable Generic Identity package name without moving or rewriting the proven TypeScript client yet.

## Current source strategy

The authoritative contract declarations still live in the established client under:

```text
clients/typescript/src/contracts.ts
clients/typescript/src/admin-contracts.ts
clients/typescript/src/errors.ts
```

`packages/contracts/src/index.ts` re-exports a curated, passive subset of those existing types.

This is intentional and temporary. It avoids a dangerous source move while consumers and package boundaries are still being qualified. A later explicit extraction/cleanup pack may transfer ownership only after all consumers have migrated and the old path has no remaining dependency.

## Module structure

```text
src/
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

The root export is convenient for consumers, while focused subpath exports keep the contract surface from becoming a single monolithic file.

## Public responsibility

The Pack 2 surface includes passive contracts for:

- users;
- tenants;
- tenant memberships;
- groups and group membership;
- managed permission policies and bindings;
- capabilities;
- application security manifests;
- authorization boundaries and decisions;
- non-secret session validation metadata;
- MFA provider/policy/authenticator metadata;
- resource scopes;
- stable public error codes.

## Explicit exclusions

The package does **not** export:

- access tokens;
- session tokens;
- password requests or credentials;
- OIDC token sets;
- PostgreSQL or routing implementation details;
- Redis/Lua internals;
- React or Next.js;
- Generic Organization Directory contracts;
- OrganisationProfile contracts.

## Runtime behavior

There is none.

The package is type-only in Pack 2 and emits no runtime JavaScript. Authentication and authorization behavior remains in the existing proven client until Pack 3 introduces the framework-neutral auth/authorization facade.

## Validation

The repository verification executes:

```powershell
.\scripts\shared-identity\verify-pack-02-contracts.ps1
```

and then typechecks the package with the already-pinned TypeScript compiler from `clients/typescript`.
