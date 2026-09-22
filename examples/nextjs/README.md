# Next.js Server Integration Example

This example demonstrates server-side use of the TypeScript client from a consuming Next.js application.

The Next.js server calls the Identity & Access HTTP API. It does not connect directly to PostgreSQL and does not receive database routing secrets.

## Build the Local Client Package

From `clients/typescript`:

```powershell
npm install
npm test
npm pack
```

Install the generated package into the consuming Next.js application together with `server-only`:

```powershell
npm install "<path-to>/identity-access-client-0.1.0.tgz" server-only
```

Configure the API base URL only in server-side configuration:

```text
IDENTITY_ACCESS_API_BASE_URL=http://127.0.0.1:5080
```

Do not expose this setting through a `NEXT_PUBLIC_` variable.

Example:

```typescript
const info = await identityAccessDiagnostics().info();
```

The `server-only` import protects the integration module from accidental use in client components.

This example currently demonstrates diagnostics only. It does not implement Next.js login state, route guards, OIDC, MFA, or access-context management.
