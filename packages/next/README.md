# `@generic-identity/next`

Reusable Next.js integration for Generic Identity.

Pack 7 activates this package without switching the existing administration host. It adds:

- a client-component boundary over `IdentityProvider`;
- server-only opaque-session cookie coordination;
- server-side authentication/session validation helpers;
- server-side capability evaluation that delegates to the existing authorization/RBAC path;
- consumer-owned route-map helpers;
- re-exports of the shared Identity pages.

## Ownership boundary

This package owns Next.js integration mechanics only. It does not own authentication semantics, authorization semantics, TRN parsing, group/policy rules, MFA rules, application routes, application navigation or product branding.

Session credentials are kept in HTTP-only cookies and are read only from server code. The package does not use `localStorage` or `sessionStorage` for credentials.

The current `examples/nextjs/admin` host remains untouched in Pack 7. Consumer migration belongs to the dedicated integration pack after this package is qualified.
