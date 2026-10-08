# @generic-identity/next

Reusable Next.js integration boundary for Generic Identity.

## Responsibility

The package provides:

- a client-component boundary over `IdentityProvider`;
- server-only opaque-session cookie coordination;
- server-side session validation helpers;
- server-side capability evaluation through the existing authorization/RBAC path;
- reusable server-only administration form mutation adapters and bounded failure presentation;
- consumer-owned route-map helpers;
- shared React page re-exports by functional category.

Application Security is available through `@generic-identity/next/application-security`.

Security Operations is available through `@generic-identity/next/security-operations`.

## Ownership boundary

This package owns Next.js integration mechanics only. It does not own authentication semantics, authorization semantics, TRN parsing, group/policy rules, MFA policy, application routes, navigation or branding.

Credentials remain server-managed. The package does not use browser storage for session secrets.


## Application Security GOLDEN parity adapters

`@generic-identity/next/server` exports
`registerNextApplicationSecurityManifestFromForm(...)` and
`addNextApplicationScopeTypeFromForm(...)`, plus the JSON file parser
`parseNextApplicationSecurityManifestFile(...)`. These call only the categorized
`session.client.applicationSecurity` SDK and preserve server-side authorization.

The security manifest is always an uploaded **project-owned JSON** file, not a
UI capability builder. See `docs/shared-identity/APPLICATION_SECURITY.md` for
validation and live qualification requirements.
