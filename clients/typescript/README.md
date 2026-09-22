# Identity & Access TypeScript Client

Version 0.4.0 extends the class-based connector with local authentication and the OIDC Authorization Code + PKCE token lifecycle.

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

## Validation

```sh
npm install --ignore-scripts --no-audit --no-fund --package-lock=false
npm test
npm run typecheck
npm pack
```

The repository-level `scripts/verify.ps1` performs dependency bootstrap automatically when the local TypeScript compiler is absent. The dependency is pinned exactly in `package.json`; bootstrap does not run package lifecycle scripts and does not create or update a package lockfile.

Runtime code has no third-party npm runtime dependency. HTTPS is required except exact loopback development hosts. Requests are non-cacheable and bounded by timeout/cancellation. Authorization redirects are handled manually and validated. Raw passwords, session tokens, authorization codes, PKCE verifiers, access tokens, ID tokens, and refresh tokens are never retained in public errors.

The complete typed administration CRUD API remains a follow-up increment in the `0.42.x` connector line. MFA, TOTP, recovery codes, and passkeys/WebAuthn remain later optional work.
