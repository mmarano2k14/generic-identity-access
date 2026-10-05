# Shared Identity Authentication and Authorization SDK — Authentication and Authorization SDK

## Objective

Activate `@generic-identity/auth` as the framework-neutral authentication and authorization SDK boundary without moving or deleting the proven TypeScript implementation.

Authentication and Authorization SDK remains deliberately additive.

## Architecture decision

The package is a narrow public facade over the existing runtime client:

```text
@generic-identity/contracts
          ^
          |
@generic-identity/auth
          |
          +---- temporary extraction bridge ----> @identity-access/client
                                                   existing proven implementation
```

The new package advertises only authentication and authorization responsibilities. It does not advertise the legacy client administration, system or OIDC surfaces.

## Public structure

```text
packages/auth/
├── package.json
├── tsconfig.json
├── src/
│   ├── authentication.ts
│   ├── authorization.ts
│   ├── authorization-context.ts
│   ├── client.ts
│   ├── errors.ts
│   └── index.ts
└── test/
    └── consumer-auth.ts
```

## Public surface

```text
createIdentityClient
signIn
signOut
validateSession
isAllowed
createAuthorizationContext
RequireCapability
GenericIdentityClientError
```

Authentication/MFA methods remain available through the narrow `client.authentication` interface. Authorization remains available through the narrow `client.authorization` interface.

## No second security implementation

The facade delegates to the proven client:

```text
signIn             -> authentication.passwordLogin
signOut            -> authentication.logout
validateSession    -> authentication.validateSession
isAllowed          -> authorization.evaluate
AuthorizationContext -> existing server-backed authorization context
```

It does not:

- evaluate permissions locally;
- parse or synthesize TRNs;
- transform technical failures into ALLOW;
- create another session model;
- implement another MFA engine.

## Existing behavior deliberately preserved

`clients/typescript` remains the authoritative runtime implementation during Authentication and Authorization SDK.

The only existing TypeScript source expansion is the export of two already-existing authentication request types required by the new public facade:

```text
IdentityRecoveryPasswordResetRequest
IdentitySelfServicePasswordChangeRequest
```

Their declarations are not moved or duplicated.

## Deliberately deferred

The target roadmap lists convenience functions such as:

```text
getCurrentUser
getCurrentSession
getEffectivePermissions
```

The current proven TypeScript runtime has session validation but no backed `getCurrentUser` or effective-permission retrieval operation. Authentication and Authorization SDK therefore does not invent those semantics. A later milestone may expose them only after a real backing API/contract is identified.

## Compatibility strategy

No existing consumer import is redirected.

```text
examples/nextjs/admin -> @identity-access/client   (unchanged)
@generic-identity/auth -> extraction facade        (new)
```

React and Next.js remain reserved for later milestones.

## Exit criteria

Authentication and Authorization SDK is complete when:

- Baseline and Structure and Public Contracts gates remain GREEN;
- `@generic-identity/auth` is active and private;
- it depends on `@generic-identity/contracts` and only the explicit temporary legacy runtime bridge;
- it has no React or Next.js dependency;
- it contains no direct fetch/storage/TRN implementation;
- a consumer compiles against the new package name;
- the existing TypeScript client tests/typecheck remain GREEN;
- the existing Next.js host remains unchanged;
- the complete .NET verification remains GREEN;
- no source file is moved or deleted.
