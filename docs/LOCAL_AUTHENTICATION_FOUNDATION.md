# Local Authentication Foundation

**Source version: 0.14.0. Date: September 21, 2026.**

Version 0.14.0 introduces the first real local-account authentication path. It does not
introduce OpenID Connect yet.

## Scope

The increment adds:

- password credentials stored separately from public user profiles;
- ASP.NET Core Identity password hashing through an adapter;
- deterministic login-identifier normalization within one identity scope;
- failed-attempt tracking and time-bounded lockout;
- opaque random sessions whose raw tokens are never stored in PostgreSQL;
- exact server-registered login redirect URIs;
- exact server-registered post-logout redirect URIs;
- session validation and revocation contracts;
- controller-based login, validation, logout and credential administration endpoints.

## Client registration and redirect URIs

Authentication clients are server configuration, not caller authority.

A configured client binds:

```text
client id
  -> application key
  -> authentication context key
  -> exact login redirect URIs
  -> exact post-logout redirect URIs
```

Redirect matching is intentionally exact. The registration contract rejects:

- wildcard hosts or paths;
- URI fragments;
- embedded user information;
- non-HTTPS remote redirects;
- duplicate redirect entries.

HTTP is accepted only for loopback addresses used during local development.

A login request cannot provide an arbitrary destination and cause the API to redirect or
return it as trusted. The supplied value must exactly match a registered URI.

Example configuration when authentication is enabled:

```json
{
  "IdentityAccess": {
    "Authentication": {
      "Enabled": "true",
      "SessionLifetimeMinutes": "60",
      "LockoutAttempts": "5",
      "LockoutMinutes": "15",
      "SensitiveOperationMfaMaxAgeMinutes": "10",
      "Clients": [
        {
          "ClientId": "web-client",
          "ApplicationKey": "app-a",
          "AuthenticationContextKey": "app-a-primary",
          "RedirectUris": [
            "http://127.0.0.1:3000/auth/callback"
          ],
          "PostLogoutRedirectUris": [
            "http://127.0.0.1:3000/"
          ]
        }
      ]
    }
  }
}
```

Enabling authentication also requires configured routing and PostgreSQL persistence.
Startup fails rather than silently downgrading to another authentication source.

## Password credentials

`identity_access.password_credentials` stores:

- identity scope;
- user identifier;
- display login identifier;
- normalized lookup identifier;
- password hash;
- failed-attempt count;
- optional lockout expiry;
- optimistic row version.

The user UUID remains the immutable account identity. A login identifier is only a login
lookup key and can be changed without redefining the user.

Password hashes are produced by ASP.NET Core Identity's `PasswordHasher<TUser>` adapter.
No plaintext password is stored.

Unknown login identifiers execute a dummy password-hash verification path before returning
the generic invalid-credentials result.

## Sessions

Successful password authentication creates a random 256-bit opaque session token.

Only its SHA-256 hash is stored in `identity_access.user_sessions`.

The raw token is returned once to the trusted caller together with:

- session id;
- user id;
- expiry;
- validated redirect URI.

The session is bound to:

```text
identity scope
client id
application key
authentication context key
user
```

A session issued for one client is not an implicit SSO session for another client.

The raw session token is a credential. It must not be stored in browser local storage,
application logs, URLs, analytics, or database columns. The intended browser integration
is a server-side/BFF flow where a trusted application server stores the credential in an
appropriate protected HTTP-only session mechanism.

## HTTP surface

```text
POST /api/v1/authentication/clients/{clientId}/password-login
POST /api/v1/authentication/clients/{clientId}/sessions/validate
POST /api/v1/authentication/clients/{clientId}/logout
POST /api/v1/authentication/clients/{clientId}/credentials/password/change
POST /api/v1/authentication/clients/{clientId}/recovery/password
```

Credential administration is protected by the existing fail-closed administration
boundary:

```text
GET  /api/v1/identity-scopes/{scope}/applications/{app}/users/{user}/password-credential
POST /api/v1/identity-scopes/{scope}/applications/{app}/users/{user}/password-credential
PUT  /api/v1/identity-scopes/{scope}/applications/{app}/users/{user}/password-credential
```

## Security behavior

- invalid login identifier and invalid password produce the same public 401 response;
- suspended users cannot create sessions;
- session tokens are random and stored only as hashes;
- session validation requires the registered client context and the exact token hash;
- logout revokes the exact session only;
- authentication routing uses the registered authentication context and never scans every database;
- operation-local state is not held in global mutable current-user fields;
- all asynchronous application contracts require explicit cancellation tokens;
- self-service password change verifies the current password and, when MFA is Required, recent session MFA assurance;
- successful password change or recovery invalidates existing local sessions and OIDC refresh-token continuity;
- recovery-code account recovery returns the same public failure for account/factor/proof rejection states.

## Deliberate limitations

This version is not OpenID Connect and does not expose `/connect/authorize` or
`/connect/token`.

It does not yet install an ASP.NET Core authentication handler for administrative
controllers and does not replace the existing fail-closed administration authorizer.

The original 0.14.0 increment did not implement MFA, passkeys, recovery codes, password reset, refresh tokens, or OIDC. Later increments now provide those capabilities. The current credential-security extension is documented in `ACCOUNT_RECOVERY_AND_CREDENTIAL_SECURITY.md`. External identity providers, email/SMS delivery channels, and password-history storage remain outside this foundation.
