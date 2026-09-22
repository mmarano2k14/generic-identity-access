# Trusted Administration Context

Administrative HTTP operations require a server-validated identity context before
capability authorization can execute.

## Context Shape

```csharp
public sealed record AdministrationRequestContext
{
    public SubjectReference Subject { get; }
    public Guid SessionId { get; }
    public string ClientId { get; }
    public ApplicationKey Application { get; }
    public string AuthenticationContextKey { get; }
    public DateTimeOffset ExpiresAt { get; }
}
```

The context intentionally does not contain:

```text
raw session token
tenant id
resource scope id
permissions
capabilities
database destination
connection information
```

`Subject`, `Application`, `ClientId`, and `AuthenticationContextKey` come from either a validated
opaque local session or a validated OIDC access token rebound to current registered-client state.
Bearer authentication additionally revalidates the referenced current local session/user state.

Tenant and resource identifiers remain authorization targets from the current route; they
are not authentication claims.

## Local Session HTTP Transport

The local-session administration bridge uses:

```text
Authorization: IdentitySession <opaque-session-token>
X-Identity-Access-Client: <registered-client-id>
X-Identity-Access-Session: <session-guid>
```

The request does not supply trusted user, identity-scope, or application claims.

The server validates the session through `ILocalAuthenticationService`, which resolves the
registered client and current identity directory.

## OIDC Bearer HTTP Transport

When OIDC is enabled, protected administration endpoints also accept:

```text
Authorization: Bearer <RS256-access-token>
```

The token is validated against the process-pinned OIDC public key ring, issuer, audience, lifetime,
subject, session, client, scope, identity-scope, and application claims. The `client_id` is rebound
to the current server registration so `AuthenticationContextKey` is never accepted as a caller JWT
claim. The referenced `sid` must still identify an active, unexpired local session for the same
active user/client/application/context.

A Bearer request containing `X-Identity-Access-Client` or `X-Identity-Access-Session` is rejected;
the two credential transports are not merged.

## Request Boundary Matching

After authentication succeeds, the administration security layer compares:

```text
validated Subject.IdentityScopeId
        ==
route identityScopeId
```

and:

```text
validated Application
        ==
route applicationKey
```

A mismatch is denied before capability authorization.

## Per-Request Storage

The trusted context is attached to ASP.NET Core `HttpContext.Features` for the lifetime of
the current request.

There is no global mutable current user, current tenant, or current database state.

## Authorization State

Trusted authentication feeds the existing capability authorization pipeline:

```text
IdentitySession OR Bearer
        |
trusted AdministrationRequestContext
        |
IdentityAuthorizationService
        |
IRbacAuthorizationAdapter
        |
external RBAC engine
```

Missing or invalid administration credentials return HTTP 401. An authenticated
identity-scope/application boundary mismatch or RBAC denial returns HTTP 403. Technical
authentication/authorization unavailability returns HTTP 503.

A valid authenticated context does not by itself grant any administrative capability.
