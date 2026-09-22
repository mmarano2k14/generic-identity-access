# Identity & Access TypeScript Client

Version 0.3.0 starts the class-based connector line for the Generic Identity Access API.
Runtime components are classes. The legacy `createIdentityAccessClient(...)` factory is removed.

Primary runtime classes:

```text
IdentityAccessClient
IdentityAuthorizationContext
IdentityAccessAdminUiBuilder
```

The decorator surface is deliberately thin:

```typescript
@RequireCapability("replay", "execution", "run")
```

It stores capability metadata only. Permission evaluation always returns to the .NET Identity Access API and the configured external RBAC engine.

## Diagnostics

```typescript
import { IdentityAccessClient } from "@identity-access/client";

const client = new IdentityAccessClient({
  baseUrl: "http://127.0.0.1:5080",
  timeoutMs: 10000,
});

const info = await client.info();
const live = await client.liveness();
const readiness = await client.readiness();
```

## Authorization context

The TypeScript equivalent of:

```csharp
if (!_auth.IsAllowed("billing", "invoice", "refund"))
{
    return;
}
```

is:

```typescript
const allowed = await auth.isAllowed("billing", "invoice", "refund");
if (!allowed) {
  return;
}
```

`IdentityAuthorizationContext` supports either a Bearer access token or the existing local `IdentitySession` credential transport. The two provenance forms are never mixed.

## Declarative capability metadata

```typescript
export class ReplayExecutionHandler {
  @RequireCapability("replay", "execution", "run")
  public async run(): Promise<void> {
  }
}
```

The decorator does not authorize locally. `IdentityAuthorizationContext.isAllowedFor(...)` resolves the metadata and calls the .NET authorization endpoint.

## Administration UI builder

```typescript
const definition = await new IdentityAccessAdminUiBuilder(auth)
  .withUsers()
  .withGroups()
  .withPolicies()
  .buildVisible();
```

Visibility filtering is presentation behavior only. Every real administration operation remains protected server-side.

## Validation

```sh
npm install --ignore-scripts --no-audit --no-fund --package-lock=false
npm test
npm run typecheck
npm pack
```

The repository-level `scripts/verify.ps1` performs this dependency bootstrap automatically when the local TypeScript compiler is absent. The dependency is pinned exactly in `package.json`; the bootstrap does not run package lifecycle scripts and does not create or update a lockfile.

Runtime code has no third-party npm runtime dependency. HTTPS is required except exact loopback development hosts. Redirects are rejected, cache storage is disabled, requests are bounded by timeout/cancellation, and raw credentials are never retained in public errors.

Authentication/OIDC token lifecycle and the complete typed administration API remain follow-up increments in the `0.42.x` connector line.
