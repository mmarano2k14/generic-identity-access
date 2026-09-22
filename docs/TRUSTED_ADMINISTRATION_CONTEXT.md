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

`Subject`, `Application`, `ClientId`, and `AuthenticationContextKey` come from a validated
server session and registered authentication client state.

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

This baseline establishes trusted authentication only.

Capability authorization remains fail-closed:

```text
trusted AdministrationRequestContext
        |
        +-- identity established
        |
        +-- capability authorization unavailable
                |
                -> HTTP 503
```

The next authorization integration connects the trusted context to:

```text
IdentityAuthorizationService
        |
IRbacAuthorizationAdapter
        |
external RBAC engine
```

Missing or invalid administration session credentials return HTTP 401.

A valid authenticated context does not by itself grant any administrative capability.
