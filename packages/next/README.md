# @generic-identity/next

Reusable Next.js integration boundary for Generic Identity.

## Responsibility

The package provides:

- a client-component boundary over `IdentityProvider`;
- server-only opaque-session cookie coordination;
- server-side session validation helpers;
- server-side capability evaluation through the existing authorization/RBAC path;
- consumer-owned route-map helpers;
- shared React page re-exports by functional category.

Application Security is available through `@generic-identity/next/application-security`.

Security Operations is available through `@generic-identity/next/security-operations`.

## Ownership boundary

This package owns Next.js integration mechanics only. It does not own authentication semantics, authorization semantics, TRN parsing, group/policy rules, MFA policy, application routes, navigation or branding.

Credentials remain server-managed. The package does not use browser storage for session secrets.
