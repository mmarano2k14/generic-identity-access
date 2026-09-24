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
/identity/resource-scopes
/identity/mfa
/identity/sessions
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
IDENTITY_ACCESS_TENANT_ID=<uuid>
IDENTITY_ACCESS_OIDC_CLIENT_ID=<registered-public-client-id>
IDENTITY_ACCESS_OIDC_REDIRECT_URI=http://127.0.0.1:3000/auth/callback
IDENTITY_ACCESS_BEARER_COOKIE_NAME=identity_access_bearer
```

All values are server-only. Never introduce `NEXT_PUBLIC_` equivalents for Identity Access security configuration.

The host resolves request-bound cookies before reading these environment values. This keeps the protected administration routes request-time rendered and allows `next build` to complete without requiring live Identity Access runtime configuration. The values are required when the host actually serves login or protected administration requests.

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
# edit .env.local with the real identity scope / application / tenant / OIDC client values
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
