# Administration UI and authentication UX

Version `0.51.0` starts the reusable UI/UX refinement phase after the backend/security sequence closed at `0.50.0`.

The administration host remains a Next.js App Router application consuming the class-based TypeScript client. UI refinement does not change the authorization boundary: protected reads and mutations continue to be enforced by the .NET API and external RBAC integration.

## Design ownership

The administration module owns one custom stylesheet only:

```text
examples/nextjs/admin/styles/identity-access-admin.css
```

CSS Modules, component-local `<style>` blocks, and inline React style objects remain prohibited. Light/dark tokens, responsive behavior, authentication surfaces, focus treatment, dialogs, tables, and workspace chrome stay centralized.

## Navigation behavior

The desktop administration workspace keeps a persistent left navigation rail. Route visibility is still produced by `IdentityAccessAdminUiBuilder` from the trusted administration context.

`AdminNavigationActiveLink` adds current-route presentation only. It does not make an authorization decision and it does not expose hidden routes. The active route uses `aria-current="page"`.

At narrow viewport widths the permanent rail is removed from layout and the top bar exposes a compact native-details navigation surface. The mobile surface receives the same already-filtered navigation entries as the desktop rail. No second route catalogue is introduced.

A skip-to-content link provides direct keyboard access to the main administration region.

## Authentication surface

The sign-in page remains server coordinated:

```text
browser form
    -> Next.js Server Action
    -> IdentityAccessHostSessionService
    -> TypeScript authentication client
    -> password-login
    -> OIDC Authorization Code + PKCE
    -> HTTP-only host cookies
```

The password visibility toggle changes presentation only. Password values remain form-local and are submitted only to the server action.

## Account recovery surface

`/recovery` is a public host route over the recovery-code password replacement contract added in `0.50.0`.

The route asks for:

```text
login identifier
recovery code
new password
new password confirmation
```

The browser never supplies a recovery authenticator identifier. The host calls `IdentityAccessAuthenticationClient.recoverPasswordWithCode(...)`, and the backend resolves the one active recovery authenticator for the account.

The UI preserves the backend anti-enumeration boundary. Invalid account, authenticator, and code conditions are presented as the same generic recovery rejection. Successful recovery redirects to sign-in with a non-secret success marker.

Recovery proofs and passwords are not placed in URLs, client-readable cookies, or browser configuration.

## Responsive and accessibility invariants

- visible keyboard focus remains available through the shared design system;
- active navigation exposes `aria-current`;
- the main workspace exposes a skip target;
- password visibility controls have explicit accessible labels and pressed state;
- native form semantics and Server Actions remain intact;
- reduced-motion preferences remain respected;
- dark mode remains driven by the user-agent color-scheme preference;
- mobile navigation is a presentation concern only and receives server-filtered entries.

## Validation

The primary repository verification chain continues to run `scripts/verify-typescript-source-consistency.ps1`, TypeScript tests/typecheck, and the production Next.js build.

The source-consistency gate pins the new navigation/recovery surfaces and rejects reintroduction of additional custom CSS files, inline styles, browser-exposed Identity Access configuration, or direct client-side security calls.
