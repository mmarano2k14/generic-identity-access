# Next.js Integration and Runnable Administration Host

The Next.js example demonstrates server-only use of the class-based TypeScript connector and now includes a runnable administration host under `examples/nextjs/admin`.

The Next.js server calls the Identity Access HTTP API. It never connects directly to PostgreSQL and never receives routing secrets from the browser.

## Class-based integration

`identity-access.ts` exposes `IdentityAccessServerConnector`, a server-only holder for `IdentityAccessClient`.

The reusable connector supports password login, local-session validation/logout, OIDC Authorization Code + PKCE, token exchange, refresh-token rotation, server-delegated authorization, and typed administration routes.

Keep password/session/token handling in server-only code.

## Runnable administration host

`examples/nextjs/admin` now contains its own pinned `package.json`, TypeScript/Next.js configuration, root layout, login route, OIDC callback target, HTTP-only cookie lifecycle, and the premium multi-page administration workspace.

From the repository root, start the API first:

```powershell
dotnet run --project src\IdentityAccess.Api --launch-profile http
```

Then:

```powershell
cd examples\nextjs\admin
Copy-Item .env.local.example .env.local
# set real registered Identity Access values in .env.local
npm install --package-lock=false
npm run dev
```

Open `http://127.0.0.1:3000/login`.

The configured server-side authentication client must register `http://127.0.0.1:3000/auth/callback` exactly (or the exact alternate URI configured in `.env.local`) and allow OIDC scope `openid`.

## Security boundary

The runnable host performs password login, OIDC Authorization Code + PKCE, and token exchange only on the Next.js server. The browser receives only HTTP-only session cookies. Access/refresh/local-session tokens are never passed to Client Components.

Permission-filtered navigation from `IdentityAccessAdminUiBuilder` is presentation only; every protected API call is still authorized server-side.
