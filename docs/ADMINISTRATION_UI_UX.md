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

## 0.52.0 administration CRUD behavior

The administration workspace now exposes mutation controls where the typed backend already defines them. Server Components still load trusted data, and Client Components remain limited to presentation, filtering, dialogs, and form interaction.

Lifecycle-managed records support create and edit:

```text
users
tenants
tenant memberships
groups
policies
resource scopes
scope-authority groups
scope-authority policies
MFA policy
```

These stable records are not given a synthetic hard-delete operation. When the API models lifecycle state, the UI exposes `Active` / `Inactive` editing instead of inventing a destructive endpoint.

Relationship records that have explicit backend removal contracts expose add/remove controls:

```text
group members
group policy bindings
policy statements
scope-authority members
scope-authority policy statements
scope-authority policy bindings
MFA authenticators (revocation lifecycle)
```

Destructive relationship mutations require an explicit confirmation phrase in the server-side mutation service. The UI never treats hiding a row or closing a dialog as proof that a mutation succeeded.

All mutable versioned records carry the currently observed `expectedVersion` back to the API. A stale browser view therefore remains subject to optimistic-concurrency rejection instead of overwriting a newer mutation.

The MFA administration page can create or edit provider-neutral policy from the installed provider registry and can invoke either normal authenticator revocation or the separate lost-factor recovery revocation path. Provider-owned secret material is never rendered into forms.

The group and scope-authority management surfaces deliberately distinguish durable object identity from removable relationship edges. Removing a membership or policy-binding edge does not delete the referenced user, group, policy, tenant membership, or resource scope.
## 0.53.0 administration visual and interaction polish

The administration data model and security boundaries are unchanged. This increment concentrates on dense collection usability and mutation clarity.

`AdminEntityTable` now provides presentation-only search, lifecycle-status filtering, optional sorting, a one-action reset, and an `aria-live` visible-result count. These controls operate only on rows already returned by the server. They do not expand the server query, discover hidden records, or make an authorization decision.

Table semantics are strengthened with a caption, explicit column scopes, complete identifier tooltips, and per-cell data labels. At narrow widths the same semantic table rows are presented as compact record cards so identifiers, lifecycle state, versions, and actions remain readable without horizontal navigation.

`AdminMutationDialog` now binds its native dialog to explicit accessible title/description identifiers and exposes `aria-busy` while a Server Action is pending. Security-sensitive dialogs render a dedicated warning panel that explains the mutation is still re-authorized server-side. The existing server-side confirmation phrase remains authoritative for destructive relationship/session mutations.

The polish remains dependency-free and continues to use the single shared stylesheet. No backend endpoint, database migration, RBAC behavior, OIDC behavior, MFA-provider contract, or public TypeScript client contract changes are part of this increment.
