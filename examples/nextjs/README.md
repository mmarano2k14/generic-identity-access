# Next.js Server Integration Example

This example demonstrates server-only use of the class-based TypeScript connector from a consuming Next.js application.

The Next.js server calls the Identity Access HTTP API. It never connects directly to PostgreSQL and never receives routing secrets.

Build the package from `clients/typescript`:

```powershell
npm test
npm run typecheck
npm pack
```

Install the generated archive together with `server-only`:

```powershell
npm install "<path-to>/identity-access-client-0.4.0.tgz" server-only
```

Configure only server-side deployment state:

```text
IDENTITY_ACCESS_API_BASE_URL=http://127.0.0.1:5080
```

Never expose this through a `NEXT_PUBLIC_` variable.

Example:

```typescript
const identity = new IdentityAccessServerConnector();
const info = await identity.client.info();
```

The connector is class-based. `IdentityAccessClient` now supports password login, local-session validation/logout, OIDC Authorization Code + PKCE, token exchange, and refresh-token rotation. Keep password/session/token handling in server-only code. The complete typed administration CRUD surface remains a later `0.42.x` increment.
