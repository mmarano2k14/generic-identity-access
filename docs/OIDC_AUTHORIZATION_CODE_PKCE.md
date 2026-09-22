# OAuth 2.0 / OpenID Connect Authorization Code + PKCE, Refresh-Token Rotation, Signing-Key Rotation, and Bearer API Validation

Identity Access provides a strict first-party OpenID Connect provider foundation built on
the existing local authentication, multi-database routing, session lifecycle, transactional
audit, and client-registration contracts.

This release implements:

```text
OAuth 2.0 Authorization Code
OpenID Connect
PKCE S256
public clients only
RS256 access tokens
RS256 ID tokens
rotating opaque refresh tokens
refresh-token family replay detection
local-session-bound refresh continuity
process-pinned RSA signing-key rotation
multi-key JWKS publication
administration API Bearer access-token validation
current local-session/user revalidation for Bearer access
OIDC discovery
JWKS
```

It intentionally does not implement:

```text
implicit flow
resource-owner password grant
client credentials
client secrets
device authorization
dynamic client registration
userinfo
MFA protocol signaling
passkeys / WebAuthn
step-up authentication
```

These exclusions keep the first protocol surface narrow and fail-closed.

## Endpoints

```text
GET  /.well-known/openid-configuration
GET  /.well-known/jwks.json
GET  /connect/authorize
POST /connect/token
```

The token endpoint consumes:

```text
application/x-www-form-urlencoded
```

## Issuer Boundary

The configured issuer is origin-only:

```text
https://identity.example.test
```

A path-bearing issuer such as:

```text
https://identity.example.test/issuer
```

is rejected because this release exposes protocol endpoints at the host root and does not
configure an ASP.NET Core path base. This keeps discovery URLs and actual HTTP routes
identical.

## Registered Public Clients

OIDC is opt-in per server-registered authentication client.

Example trusted configuration:

```json
{
  "IdentityAccess": {
    "Authentication": {
      "Enabled": "true",
      "Clients": {
        "admin-web": {
          "ClientId": "admin-web",
          "ApplicationKey": "app-a",
          "AuthenticationContextKey": "app-a-primary",
          "RedirectUris": [
            "https://client.example.test/callback"
          ],
          "PostLogoutRedirectUris": [
            "https://client.example.test/"
          ],
          "OidcEnabled": "true",
          "AllowedOidcScopes": [
            "openid"
          ]
        }
      },
      "Oidc": {
        "Enabled": "true",
        "Issuer": "https://identity.example.test",
        "AccessTokenAudience": "identity-access-api",
        "AuthorizationCodeLifetimeSeconds": "120",
        "AccessTokenLifetimeMinutes": "15",
        "IdTokenLifetimeMinutes": "15",
        "RefreshTokenLifetimeDays": "30",
        "ActiveSigningKeyId": "identity-key-2",
        "SigningKeys": [
          {
            "KeyId": "identity-key-2",
            "PemPath": "secrets/oidc-signing-key-2-private.pem"
          },
          {
            "KeyId": "identity-key-1",
            "PemPath": "secrets/oidc-signing-key-1-public.pem"
          }
        ]
      }
    }
  }
}
```

OIDC-enabled clients are public clients. There is no client-secret field.

The current protocol release supports only:

```text
scope = openid
```

Additional claims/scopes must be introduced explicitly rather than silently accepted.

## Redirect URI Security

Authorization and token requests must use an exact registered redirect URI.

The existing client registration rules continue to reject:

```text
wildcard redirect URIs
URI fragments
userinfo
non-loopback HTTP redirect URIs
```

HTTPS is required except for loopback HTTP development URIs.

A request with an unknown client or unregistered redirect URI is never redirected to the
caller-supplied URI.

## Authorization Request

The authorization endpoint accepts:

```text
client_id
redirect_uri
response_type=code
scope=openid
state
nonce
code_challenge
code_challenge_method=S256
```

`state` is required and must contain 8 to 512 non-control characters.

`nonce` is required and must contain 8 to 256 non-control characters.

PKCE is mandatory:

```text
code_challenge_method = S256
code_challenge = BASE64URL(SHA256(code_verifier))
```

`plain` PKCE is not supported.

## Resource-Owner Authentication

The authorization endpoint does not implement a login page.

It consumes an already validated local Identity Access session using the existing session
transport:

```text
Authorization: IdentitySession <opaque-session-token>
X-Identity-Access-Session: <session-guid>
```

The OIDC `client_id` is used to validate the local session against the same registered
client.

A host UI or BFF is responsible for authenticating the user first and then initiating the
authorization request.

This separation keeps:

```text
browser/UI authentication experience
```

independent from:

```text
OAuth/OIDC protocol issuance
```

A missing or invalid local session produces:

```text
error=login_required
```

only after the client and redirect URI have been validated.

## Authorization Code Storage

The authorization code is a cryptographically random 256-bit opaque value.

The database stores only:

```text
SHA-256(code)
```

The raw code is returned once to the registered redirect URI and is never persisted.

Migration `0012_oidc_authorization_code_pkce.sql` adds:

```text
identity_access.oidc_authorization_codes
```

Each record binds the code to:

```text
identity scope
user
source session
client
application
authentication context
exact redirect URI
scope
PKCE S256 challenge
nonce
authentication time
issue time
expiry
consumption time
```

## Current Session State at Code Issuance

Authorization-code persistence uses `INSERT ... SELECT` against the current source session
and user state.

A code is created only while:

```text
source session is not revoked
source session is unexpired
source session still matches client/application/authentication context
current user status is Active
```

This closes the race where a logout or suspension occurs after initial session validation
but before code persistence.

## Single-Use and Current Account State

Token exchange consumes the code with one PostgreSQL statement.

The consume predicate requires:

```text
code hash matches
client matches
redirect URI matches
PKCE challenge matches
code is unconsumed
code is unexpired
source session is not revoked
source session is unexpired
source session client/application/context still match
current user status is Active
```

Successful redemption sets:

```text
consumed_at
```

atomically.

Replay therefore returns:

```text
invalid_grant
```

If the user is suspended or the source local session is revoked after authorization but
before token exchange, the code cannot be redeemed.

## Refresh-Token Family Creation

A successful authorization-code exchange also creates an opaque refresh-token family.

Refresh tokens are generated from 32 random bytes and encoded as base64url values. PostgreSQL
stores only:

```text
SHA-256(refresh_token)
```

Migration `0013_oidc_refresh_token_rotation.sql` adds:

```text
identity_access.oidc_refresh_tokens
```

The initial family member binds the refresh chain to:

```text
identity scope
user
source local session (sid)
public client
application
authentication context
scope
original authentication time
absolute family expiry
```

Initial family creation uses current `user_sessions` and `users` state and succeeds only
while the source session is active/unexpired and the user is Active. Token responses are not
returned when that persistence predicate fails.

The default absolute family lifetime is 30 days and is configured with:

```text
RefreshTokenLifetimeDays
```

The value must be between 1 and 365 days. Rotation does not extend the family expiry.

## Refresh Grant and Mandatory Rotation

The token endpoint accepts a refresh grant with exactly:

```text
client_id
grant_type=refresh_token
refresh_token
```

The endpoint rejects unexpected form parameters. In particular, the refresh flow has no
client secret, password grant, or scope override. The original `openid` scope is preserved
from the family.

Every successful refresh is single-use and atomic. PostgreSQL serializes mutations for the
family before deciding whether the presented token can rotate. A valid current token causes:

```text
old token consumed_at = now
replacement token inserted
sequence_number = previous + 1
parent_token_id = previous token id
same family id
same absolute expires_at
```

The current local session and user are rechecked on every rotation. A revoked or expired
session, inactive user, expired refresh family, wrong public client, unknown token, or already
revoked family returns the same public protocol failure:

```text
invalid_grant
```

## Refresh-Token Replay Detection

Presenting a previously consumed refresh token is treated as family reuse. While holding the
family mutation lock, the store revokes every non-revoked member of that family and records:

```text
revoked_at = now
revocation_reason = reuse_detected
```

The public client still receives only:

```text
invalid_grant
```

Replay detection details are retained only in internal semantic audit; they are not exposed
as a distinct public OAuth error.

## Tokens

Authorization-code exchange issues:

```text
access_token
id_token
refresh_token
token_type = Bearer
expires_in
scope = openid
```

Refresh-token exchange issues:

```text
access_token
refresh_token
token_type = Bearer
expires_in
scope = openid
```

The refresh response deliberately does not issue a new ID token in this release. This avoids
inventing new `nonce` semantics during refresh; the original `auth_time` remains preserved in
the refresh-family record for future protocol evolution.

Both JWTs use:

```text
alg = RS256
kid = configured signing-key id
```

The access token includes:

```text
iss
sub
aud
exp
iat
jti
client_id
scope
sid
identity_scope_id
application_key
```

The ID token includes:

```text
iss
sub
aud
exp
iat
auth_time
nonce
sid
at_hash
```

The OIDC subject is based on the full logical user identity:

```text
IdentityScopeId + UserId
```

rather than `UserId` alone.

## Administration API Bearer Validation

Protected administration endpoints accept the existing local-session transport and, when OIDC
is enabled, the standard access-token transport:

```text
Authorization: Bearer <access-token>
```

Bearer authentication is not a second authorization engine. It establishes the same trusted
`AdministrationRequestContext` that the existing capability/TRN/RBAC pipeline consumes.

The Bearer validator is process-pinned to the same public signing-key ring exposed through JWKS.
It requires the exact access-token contract issued by this provider and validates:

```text
JWT structure with no duplicate header/claim names
alg = RS256
typ = JWT
kid resolves to one process-pinned published key
RS256 signature
iss = configured canonical issuer
aud = configured access-token audience
exp is in the future
iat is not in the future
exp > iat
encoded lifetime does not exceed configured AccessTokenLifetimeMinutes
jti is a non-empty canonical token UUID
sid is a non-empty local-session UUID
identity_scope_id is a non-empty UUID
sub = identity_scope_id:user_id
client_id resolves to a current registered OIDC client
scope is currently allowed for that client
application_key matches the server-registered client application
```

The authentication-context key is not trusted from a JWT claim. It is rebound from the current
server-side `client_id` registration after signature/claim validation.

After cryptographic validation, the API revalidates the JWT `sid` against the current
`user_sessions` and `users` state through the registered authentication-directory route. Bearer
access therefore fails when the source session is revoked or expired, when the current user is no
longer Active, when the session/client/application/context binding changed, or when the route no
longer resolves to the token identity scope. The raw opaque local-session token is not required or
reconstructed for this continuity check.

Bearer requests must not also supply the legacy local-session provenance headers:

```text
X-Identity-Access-Client
X-Identity-Access-Session
```

Mixing the two authentication transports is rejected instead of merging credentials.

HTTP semantics remain aligned with the existing administration boundary:

```text
401  missing/invalid/expired Bearer token or ineligible current session/user
403  authenticated token crosses the requested identity-scope/application boundary or RBAC denies
503  token/session validation or authorization fails technically
```

A valid access token still grants no administration capability by itself. The current RBAC decision
is evaluated after authentication for every protected operation.

## Signing-Key Ring and Rotation

OIDC uses a process-pinned RSA signing-key ring. Exactly one configured `kid` is active for
new signatures while every configured key is published through JWKS.

Minimum RSA key size:

```text
2048 bits
```

The active key must contain RSA private-key material. Retained keys used only to validate JWTs
issued before a rotation may contain public-key material only. This allows old private material
to be removed from the Identity Access process while its public key remains available to token
validators.

The active signing key is selected by:

```text
ActiveSigningKeyId
```

All entries are declared under:

```text
SigningKeys[]
  KeyId
  PemPath
```

Key identifiers are exact, case-sensitive JWT `kid` values and must be unique. The key ring is
bounded to 16 published keys. Startup fails closed when the active key is absent, its PEM does
not contain private RSA material, a key is below 2048 bits, a key file is unavailable, or the
ring contains duplicate identifiers.

Signing operations serialize access to the active process-pinned RSA instance so concurrent
token issuance does not depend on cryptographic provider instance thread-safety. Retained keys
are reduced to public JWK material after startup and are not used for signing.

JWKS publishes every configured public key. Newly issued JWT headers always contain the active
`kid`. Rotation is therefore an operator-controlled, restart-based lifecycle rather than an
in-place replacement of a PEM file. Multi-instance deployments should use a staged rollover so
every instance can publish the new public key before any instance starts signing with it:

```text
Stage 1 - pre-publish
1. Add the new key to SigningKeys on every provider instance.
2. Keep ActiveSigningKeyId on the current key.
3. Restart/roll all instances and verify JWKS exposes both keys everywhere.

Stage 2 - activate
4. Change ActiveSigningKeyId to the new key while retaining the previous key.
5. Restart/roll all instances. Mixed old/new signers remain verifiable because both JWKS keys
   were pre-published in Stage 1.

Stage 3 - retire
6. After every instance signs with the new key, the previous entry may be replaced by public-only
   PEM material on a later restart.
7. Remove the previous key only after previously issued JWTs and validator/JWKS caches can no
   longer require it.
```

Refresh tokens are opaque and are not signed with this key ring, so signing-key rotation does
not invalidate a refresh-token family by itself. New access tokens produced by refresh use the
currently active signing key after restart.

The legacy single-key configuration remains supported as a compatibility form:

```text
SigningKeyId
SigningKeyPemPath
```

It is treated as a one-key ring. Legacy fields cannot be mixed with `SigningKeys`, and
`ActiveSigningKeyId` is required when the multi-key form is used.

For local development:

```powershell
.\scripts\authentication\create-dev-oidc-signing-key.ps1
```

Private signing keys must never be committed to source control.

## Discovery

The discovery document advertises only implemented capabilities:

```text
response_types_supported = ["code"]
response_modes_supported = ["query"]
grant_types_supported = ["authorization_code", "refresh_token"]
subject_types_supported = ["public"]
id_token_signing_alg_values_supported = ["RS256"]
scopes_supported = ["openid"]
token_endpoint_auth_methods_supported = ["none"]
code_challenge_methods_supported = ["S256"]
```

## Transactional Audit

Authorization-code insert and consume mutations participate in the transactional security
mutation ledger.

The ledger stores only:

```text
identity_scope_id
code_id
```

as the authorization-code record key.

It never stores:

```text
raw authorization code
code hash
PKCE challenge
PKCE verifier
nonce
redirect URI
token
```

Refresh-token insert, consume, replacement, and family-revocation mutations also participate
in the transactional ledger. Refresh rows expose only:

```text
identity_scope_id
token_id
```

as ledger keys. Raw refresh tokens and refresh-token hashes are never ledger keys or semantic
audit payloads.

Semantic audit records:

```text
OidcAuthorizationCodeIssued
OidcAuthorizationCodeRedeemed
OidcRefreshTokenFamilyCreated
OidcRefreshTokenRotated
OidcRefreshTokenReuseDetected
OidcRefreshTokenFamilyRevoked
```

## Error Semantics

Authorization endpoint errors use registered redirect URIs only after redirect validation.

Examples:

```text
unsupported_response_type
invalid_scope
login_required
invalid_request
temporarily_unavailable
```

The token endpoint returns:

```text
invalid_request
invalid_client
invalid_grant
unsupported_grant_type
temporarily_unavailable
```

A protocol error never becomes authorization success.

## Validation

After applying migrations through 0013:

```powershell
$env:PGPASSWORD = "<postgres-password>"

.\scripts\postgresql\apply-default-schema.ps1
.\scripts\postgresql\verify-migration-integrity.ps1
.\scripts\postgresql\verify-oidc-authorization-code.ps1
.\scripts\postgresql\verify-oidc-refresh-token.ps1

Remove-Item Env:PGPASSWORD
```

The live fixture validates:

```text
single-use code consumption
active-session requirement
transactional-ledger coverage
secret-safe ledger keys
revoked-session rejection
refresh-token rotation
consumed-token replay family revocation
absolute family expiry preservation
local-session-bound refresh eligibility
```
