# Generic Identity — Next.js Integration

`@generic-identity/next` is the reusable Next.js-specific integration boundary for Generic Identity.

## Responsibilities

The package provides:

```text
server-only opaque-session coordination
trusted administration-context construction
server-side session validation
server-backed capability evaluation
protected-page helpers
bounded entity reference search
administration workspace loaders
server mutation workflows
shared React administration exports
consumer-owned route integration helpers
```

## Current administration workflow coverage

The server package now contains reusable workflows for:

```text
Tenants
Users
Memberships
Organizations
Groups
Managed Policies
Resource Scopes
Delegated Authority
Application Security
MFA administration
Security Audit
Session security and containment
```

These workflows compose the categorized SDK and preserve backend authorization/concurrency semantics. They do not move privileged mutations into browser code.

## Consumer ownership

A consumer application owns:

```text
public URL structure
navigation
branding and shell layout
Server Action entrypoints
route revalidation
consumer-specific application capabilities
```

A consumer must not duplicate Identity RBAC, TRN construction, database routing, authentication, MFA, or session semantics.

## React / Next.js boundary

React and React DOM remain peer dependencies. Shared client components retain explicit client boundaries, while server-only integration code stays under the Next.js server surface.

## Compatibility

The legacy TypeScript transport remains the runtime bridge during the package transition. New consumer source should import `@generic-identity/*` public surfaces rather than the compatibility transport directly.
