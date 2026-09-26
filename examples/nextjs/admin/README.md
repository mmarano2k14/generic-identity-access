# Runnable Next.js Administration Host

This directory is both the reusable App Router administration module and a runnable local Next.js host for Generic Identity Access.

The host preserves the repository security boundaries:

- **Server Components** load protected data and construct permission-aware navigation.
- **Client Components** handle local filtering, dialog state, pending state, and safe presentation feedback only.
- framework-required **Server Action functions** are thin adapters; durable logic lives in TypeScript classes.
- `IdentityAccessHostSessionService` owns the server-only password -> local session -> OIDC Authorization Code + PKCE -> token exchange lifecycle.
- `IdentityAccessAdminRequest` resolves the trusted administration boundary and the HTTP-only Bearer credential for each request.
- `IdentityAccessClient`, `IdentityAuthorizationContext`, and `IdentityAccessAdminUiBuilder` remain the reusable connector classes.
- no Client Component receives passwords after form submission, access tokens, refresh tokens, local session tokens, database routes, connection information, RBAC internals, or secret references.
- `/identity/security-models` registers complete project-owned JSON manifests and inspects their immutable registered projections; it does not expose free-text capability-coordinate authoring, and policy statement creation selects exact registered capabilities.
- relationship selectors use a shared server-backed autocomplete: no collection is preloaded into the browser; searches start after three characters, are debounced/cancelled, and return at most 20 authorized records.
- tenant-membership creation selects both the tenant and user explicitly instead of relying on an implicit configured tenant.
- tenant-member lookup and display resolve through tenant-constrained server reads; the host does not merge tenant memberships with the identity-scope user directory.
- tenant-scoped workspaces resolve their tenant from the trusted effective administration context; scope-wide operators may request a tenant through server-backed selection, single-membership subjects resolve automatically, and multi-membership subjects can choose only from their active membership set.
- membership-limited `/identity/users` reads use the tenant-constrained user projection and never fall back to the identity-scope directory.
- tenant IDs carried in query strings or hidden form fields are requested context only; every read and mutation reconstructs a trusted tenant context and is re-authorized by the API.

## Runtime versions

The standalone host pins its framework/runtime versions in `package.json` rather than depending on floating tags.

```text
Next.js     16.3.6
React       19.3.0
React DOM   19.3.0
TypeScript  5.8.3
```

## Routes

```text
/                  -> redirects to /login or /identity
/login             -> real server-side Identity Access login
/recovery          -> recovery-code-backed password replacement
/auth/callback     -> registered OIDC redirect target; normal host flow captures it server-side
/identity          -> premium administration overview
/identity/users
/identity/tenants
/identity/memberships
/identity/groups
/identity/policies
/identity/security-models
/identity/resource-scopes
/identity/mfa
/identity/sessions
/identity/security-audit
/identity/authority
```

## Authentication flow

The browser posts credentials to a Server Action. The action delegates to the class-based session service:

```text
Browser form
    |
    v
loginAction (thin Next.js adapter)
    |
    v
IdentityAccessHostSessionService
    |
    +--> passwordLogin()
    |       -> opaque local session
    |
    +--> authorizeOidc()
    |       -> Authorization Code + PKCE S256
    |
    +--> exchangeAuthorizationCode()
            -> access / ID / refresh token
    |
    v
HTTP-only SameSite=Lax cookies
    |
    v
/identity
```

The OIDC authorization redirect is handled with `redirect: "manual"` by `IdentityAccessClient`, so the browser normally never navigates through `/auth/callback`. The route exists because the redirect URI must still be an exact registered URI.

`Sign out` attempts to revoke the persisted local session, then clears the local HTTP-only host cookies.

`/recovery` remains pre-authentication and delegates through `IdentityAccessHostSessionService` to the existing recovery-code password replacement contract. It accepts no authenticator identifier from the browser, keeps the recovery proof out of URLs, and redirects back to sign-in only after successful replacement.

## Configuration

Copy the checked-in example:

```powershell
Copy-Item .env.local.example .env.local
```

Configure:

```text
IDENTITY_ACCESS_API_BASE_URL=http://127.0.0.1:5080
IDENTITY_ACCESS_IDENTITY_SCOPE_ID=<uuid>
IDENTITY_ACCESS_APPLICATION_KEY=<registered-application-key>
IDENTITY_ACCESS_OIDC_CLIENT_ID=<registered-public-client-id>
IDENTITY_ACCESS_OIDC_REDIRECT_URI=http://127.0.0.1:3000/auth/callback
IDENTITY_ACCESS_BEARER_COOKIE_NAME=identity_access_bearer
```

All values are server-only. Never introduce `NEXT_PUBLIC_` equivalents for Identity Access security configuration.

The host resolves request-bound cookies before reading these environment values. The tenant boundary is resolved from the authenticated subject by the Identity Access API and is never configured as a fixed runtime environment tenant. This keeps protected administration routes request-time rendered and allows `next build` to complete without requiring live Identity Access runtime configuration. The remaining values are required when the host actually serves login or protected administration requests.

The configured OIDC client must be enabled server-side, allow `openid`, and register the redirect URI **exactly** as configured by the host. The host does not create demo users, bypass authentication, or bootstrap authority automatically.

## Run locally

Start the .NET API separately from the repository root:

```powershell
dotnet run --project src\IdentityAccess.Api --launch-profile http
```

Then start the host:

```powershell
cd examples\nextjs\admin
Copy-Item .env.local.example .env.local
# edit .env.local with the real identity scope / application / OIDC client values
npm install --package-lock=false
npm run dev
```

Open:

```text
http://127.0.0.1:3000/login
```

Production build check:

```powershell
npm run verify
```

The repository-wide `scripts/verify.ps1` also restores the pinned host dependencies when needed, type-checks the host, and runs `next build`.

## CSS ownership rule

All custom CSS for login and administration lives in exactly one file:

```text
styles/identity-access-admin.css
```

Do not add CSS Modules, per-component stylesheets, `<style>` blocks, or inline style objects. Premium light/dark tokens, responsive behavior, focus states, reduced-motion handling, tables, dialogs, active/mobile navigation, login, recovery, and page composition all evolve inside this same file.

## Administration behavior

The protected pages include server-confirmed creation flows, local filtering, loading/error/empty states, security-sensitive session revocation with explicit confirmation, a real overview dashboard, structured detail surfaces, grouped active-route navigation, compact mobile navigation, and premium responsive presentation. Mutation paths are revalidated only after the server confirms success; the UI does not assume optimistic authorization or mutation success.

UI visibility remains presentation filtering only. The .NET API and external RBAC integration remain the final authorization authority.


## Security model registration

`/identity/security-models` exposes a protected **Register manifest** action. Select the complete JSON manifest produced and versioned by the consuming project. Registration is server-confirmed, requires the existing `security-model / write` administration capability, and revalidates both the Security Models and Policies workspaces on success.

The host intentionally does not provide separate browser fields for RBAC project, namespace, resource, feature, action, or model version. Those values remain owned by the project manifest. Reusing a model version with different normalized semantics is surfaced as an immutable-version conflict; the project must increment `modelVersion` and register the new manifest.

## Security audit workspace

`/identity/security-audit` is a server-rendered, read-only view over the authorized audit API. Query normalization, API loading, presentation mapping, and visible-window summary aggregation are separate classes. The browser receives only the already-authorized categorical audit records needed for rendering.

The page supports exact user, tenant, outcome, and correlation filters plus bounded 25/50/100/200-event windows. It never exposes database routes, tokens, secrets, arbitrary payloads, or an RBAC evaluator.

## Sessions and security operations workspace

`/identity/sessions` composes the existing authorized security-audit read path with the existing user-wide and registered-client-wide session revocation operations. Query normalization, audit loading, presentation, summary aggregation, and mutations are separate class responsibilities; Server Actions stay thin.

The current backend does not expose an administration session-list contract, so the page intentionally does not invent an active-session inventory. Missing audit evidence is not interpreted as proof that a session is active, expired, or revoked. Investigation can be narrowed by user, client, outcome, and bounded result window, with links to related user, MFA, audit, and correlation contexts.

Containment actions require explicit `REVOKE` confirmation and are considered successful only after the server confirms the operation. The workspace never renders raw session/access/refresh tokens, token hashes, password material, MFA secrets, routing secrets, or connection material.

## Production failure behavior

The administration host classifies protected-operation failures server-side before any error state reaches a Client Component. Mutation services do not own transport/error formatting. `IdentityAccessAdminFailurePresentation` maps the typed client error plus bounded host validation failures into safe UI categories and recovery guidance.

Important behavior:

- `401` routes an invalid protected administration context back to sign-in;
- `403` remains an authorization refusal and is never relabeled as a technical outage;
- `409` is presented as stale optimistic-concurrency state and requires reload before retry;
- `503` remains a technical dependency failure;
- timeout, transport, cancellation, and invalid-protocol responses never manufacture mutation success;
- when the final mutation outcome cannot be proven from the response, the UI instructs the administrator to reload current state before retrying;
- raw exception details, response bodies, credentials, tokens, connection material, and routing secrets are never included in action state.

The sessions workspace additionally degrades its bounded security-audit evidence independently from containment. If audit evidence is technically unavailable, no active/revoked/expired state is inferred, while the existing separately authorized user/client revocation controls remain visible.
## Accessibility and responsive behavior

The administration host keeps accessibility state in presentation-only components. The desktop and compact navigation surfaces receive the same server-filtered entries; the mobile disclosure exposes its expanded state and closes after route navigation without becoming a second route catalogue.

Keyboard users can skip directly to a focusable main workspace. Mutation dialogs restore focus to their trigger after controlled close. Shared form hints are explicitly associated with their inputs, and login/recovery forms expose pending state while server actions execute.

The single stylesheet includes reduced-motion, automatic dark mode, increased-contrast, forced-colors, dynamic viewport-height, and narrow-screen composition rules. These behaviors change presentation only and do not alter the server-side authorization boundary.

## Relationship reference selectors

Administration relationships use one shared `AdminEntityAutocomplete` component instead of editable raw foreign-key fields. The selector searches the authorized bounded record set by display name and stable ID, renders both for operator confirmation, and submits only the stable identifier expected by the existing mutation contract.

The durable mapping logic remains outside React in `IdentityAccessAdminEntityReferencePresentation`. Existing typed administration clients are reused for lookup reads; only missing bounded list contracts are added. Opaque technical values such as `clientId`, `correlationId`, and provider/external resource identifiers remain text input because they do not identify selectable Identity Access records.

## Managed policy catalog administration

`/identity/policies` is the shared managed-policy catalog workspace. It is identity-scope/application scoped and intentionally does not accept tenant selection. Identity-scope administrators can create reusable policy definitions, create draft versions pinned to registered security-model versions, edit draft capability statements through the manifest-backed catalog, publish immutable versions, and select a published default version.

Tenant administrators do not own policy definitions. They consume active shared policies through the tenant-scoped managed binding workflow under Groups. The host no longer lists or removes legacy tenant-owned policy bindings; only managed bindings are active.
