# Identity & Access TypeScript Client

Version 0.7.0 keeps the class-based connector stable while hardening the copyable Next.js administration module with server-confirmed mutations, dialogs, functional states, and single-file CSS ownership.

Runtime components remain class-based. The primary runtime classes are:

```text
IdentityAccessClient
IdentityAuthorizationContext
IdentityAccessAdminUiBuilder
```

The decorator surface remains deliberately thin:

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

## Password login and local session

```typescript
const session = await client.passwordLogin({
  clientId: "admin-web",
  loginIdentifier: "user@example.test",
  password,
  redirectUri: "https://app.example.test/callback",
});

await client.validateSession(session);
```

`IdentityLocalSession` is directly usable as an `IdentitySessionCredential`. No global current-session state is stored inside `IdentityAccessClient`.

Logout is explicit:

```typescript
await client.logout(
  session,
  "https://app.example.test/signed-out",
);
```

Registered redirect and post-logout redirect URIs remain validated by the server.

## OIDC Authorization Code + PKCE

The connector performs the authorization endpoint through an already authenticated local session. PKCE uses cryptographically random 32-byte input and S256.

```typescript
const authorization = await client.authorizeOidc(session, {
  clientId: "admin-web",
  redirectUri: session.redirectUri,
});

const tokens = await client.exchangeAuthorizationCode(authorization);
```

`authorizeOidc(...)` uses `redirect: "manual"`. It never follows the authorization redirect automatically. The returned `Location` target and `state` are validated before the one-time authorization code is exposed.

The connector sends only the public-client fields required by the backend:

```text
client_id
response_type=code
scope=openid
state
nonce
code_challenge
code_challenge_method=S256
```

No client secret exists or is accepted by the client surface.

## Refresh-token rotation

```typescript
const refreshed = await client.refreshOidcTokens(
  "admin-web",
  tokens.refreshToken,
);
```

A successful refresh returns a replacement refresh token. The caller must discard the presented refresh token immediately. The client never retains or retries an old refresh token automatically.

The current server contract returns:

```text
authorization_code grant
    -> access token
    -> ID token
    -> refresh token

refresh_token grant
    -> access token
    -> rotated refresh token
    -> no new ID token
```

Stable OAuth/OIDC errors are surfaced through `IdentityAccessClientError.protocolCode`, while `temporarily_unavailable` remains a technical `unavailable` failure.

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

## Typed administration API

Administration remains on the primary `IdentityAccessClient`; no parallel runtime client class is introduced.
Every administration operation receives an explicit trusted boundary and credential instead of reading global mutable state.

```typescript
const context = {
  identityScopeId,
  applicationKey: "admin-app",
  credential: { kind: "bearer", accessToken },
};

const user = await client.createUser(context, {
  displayName: "Alice",
});

const updated = await client.updateUser(context, user.userId, {
  displayName: "Alice Updated",
  status: 1,
  expectedVersion: user.version,
});
```

Tenant-scoped operations receive the tenant explicitly:

```typescript
const tenantContext = {
  ...context,
  tenantId,
};

const group = await client.createGroup(tenantContext, {
  displayName: "Billing Team",
});

await client.addPolicyStatement(tenantContext, policyId, {
  modelVersion: 3,
  resource: "billing",
  feature: "*",
  action: "refund",
});
```

The typed surface covers the administration routes currently exposed by the backend:

```text
users
 tenants
 tenant memberships
 groups + group memberships
 policies + statements + scoped bindings
 resource scopes + scope types
 bulk session revocation
 identity-scope authority groups/members/policies/statements/bindings
```

`GET` operations that map to an API `404` return `null`. Remove operations return `true` for `204` and `false` for `404`. Mutable records preserve explicit `expectedVersion` optimistic concurrency.

Whole-segment capability wildcard patterns remain supported exactly where the backend policy model supports them. TypeScript does not evaluate wildcard authority locally.

The backend currently has persistence contracts for application security-model versions and declared capabilities, but it does not yet expose direct MVC administration routes for creating/listing those objects. This client therefore does not invent such endpoints. Existing scope-type routes under a security-model version are typed.

## Validation

```sh
npm install --ignore-scripts --no-audit --no-fund --package-lock=false
npm test
npm run typecheck
npm pack
```

The repository-level `scripts/verify.ps1` performs dependency bootstrap automatically when the local TypeScript compiler is absent. The dependency is pinned exactly in `package.json`; bootstrap does not run package lifecycle scripts and does not create or update a package lockfile.

Runtime code has no third-party npm runtime dependency. HTTPS is required except exact loopback development hosts. Requests are non-cacheable and bounded by timeout/cancellation. Authorization redirects are handled manually and validated. Raw passwords, session tokens, authorization codes, PKCE verifiers, access tokens, ID tokens, and refresh tokens are never retained in public errors.

Direct administration routes for application security-model versions/capability declarations remain a follow-up backend/API decision because they are not currently exposed by the .NET API. MFA, TOTP, recovery codes, and passkeys/WebAuthn remain later optional work.

## Administration UI composition

`IdentityAccessAdminUiBuilder` now produces stable route metadata for the reusable administration sections. Scope-level and tenant-level visibility are evaluated through separate `IdentityAuthorizationContext` instances when required. UI filtering remains presentation-only; every HTTP operation is still authorized by the .NET API.

Core collection pages can use bounded reads:

```typescript
const users = await client.listUsers(context, { offset: 0, limit: 50 });
const tenants = await client.listTenants(context, { limit: 50 });
const groups = await client.listGroups(tenantContext, { limit: 50 });
const policies = await client.listPolicies(tenantContext, { limit: 50 });
```

The server accepts a maximum list size of 200 records per request.

## Next.js functional administration hardening

The copyable `examples/nextjs/admin` module keeps protected reads and mutations on the server. Framework-required Server Action functions are thin adapters over the class-based `IdentityAccessAdminMutationService`; they do not call `IdentityAccessClient` directly.

Implemented functional flows include users, tenants, tenant memberships, groups, policies, resource scopes, identity-scope authority creation, and explicit user/client session revocation. Destructive revocation requires the confirmation text `REVOKE`. Paths are revalidated only after the API confirms success.

The module owns exactly one custom stylesheet:

```text
examples/nextjs/admin/styles/identity-access-admin.css
```

CSS Modules, component-local style files, `<style>` blocks, and React inline style objects are rejected by the source-consistency gate. The premium design increment must evolve the same file.
