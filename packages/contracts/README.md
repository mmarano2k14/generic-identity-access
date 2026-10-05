# @generic-identity/contracts

Passive public TypeScript contracts for the Generic Identity boundary.

## Responsibility

This package exposes stable contract types that consumers can compile against without depending on Generic Identity backend implementation details.

The authoritative compatibility bridge currently reuses proven contract declarations from `@identity-access/client`. That dependency is transitional and intentionally type-oriented; moving source ownership is a separate migration decision.

## Public categories

Focused subpath exports are available for:

```text
administration
authorization
identity
mfa
policies
security-manifest
session
account
directory
organizations
access-control
application-security
security-operations
errors
```

Application Security includes registered model metadata, manifest contracts, scope-type contracts, effective administration context metadata and the structured `IdentityApplicationSecurityPermissionReference` type.

Security Operations includes non-secret session-revocation, MFA policy/provider/authenticator metadata and security-audit contracts.

## Exclusions

This package does not expose credentials, tokens, PostgreSQL/Redis internals, React/Next.js behavior or authorization evaluation logic.

## Runtime behavior

None. The package is contract-only.
