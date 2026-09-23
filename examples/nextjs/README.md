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
npm install "<path-to>/identity-access-client-0.5.0.tgz" server-only
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

The connector is class-based. `IdentityAccessClient` supports password login, local-session validation/logout, OIDC Authorization Code + PKCE, token exchange, refresh-token rotation, server-delegated authorization, and the typed administration routes currently exposed by the .NET API. Keep password/session/token handling in server-only code.

## Administration module foundation

A multi-page App Router foundation is available under `examples/nextjs/admin`.

It demonstrates:

- permission-filtered navigation from `IdentityAccessAdminUiBuilder`;
- Server Components for protected data loading;
- a Client Component for local presentation filtering;
- per-request server-only credential handling through a class;
- separate user, tenant, membership, group, policy, resource-scope, session, and scope-authority pages;
- no browser exposure of Bearer credentials or routing/storage internals.

The administration example intentionally uses minimal markup. Visual design, responsive layout, forms, dialogs, skeletons, and polished interaction states are a later UI-design increment so presentation can evolve without changing the security boundary.
