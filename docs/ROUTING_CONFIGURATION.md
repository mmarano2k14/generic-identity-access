# Server-Side Routing Configuration

## Purpose

The routing layer maps a trusted application/identity context to a registered PostgreSQL destination. Routing is an infrastructure concern and does not participate in identity or authorization semantics.

## Routing Unit

The current routing unit is an identity directory:

```text
(ApplicationKey, IdentityScopeId, identity-directory)
                         |
                         v
             registered destination
```

An identity scope may contain multiple tenants. The routing layer does not split one directory by table or user.

## Configuration Contract

The configuration document contains:

```text
schemaVersion
revision
routingProvider
placementGranularity
destinations
routes
authenticationContexts
```

Supported values include:

```text
routingProvider      = configuration
placementGranularity = identity-scope
dataSet              = identity-directory
```

A destination contains:

```text
key
provider
connectionSecretRef
state
```

A route contains:

```text
applicationKey
identityScopeId
dataSet
destinationKey
version
state
```

An authentication context contains:

```text
key
applicationKey
identityScopeId
state
```

The configuration parser rejects unknown fields, duplicate JSON properties, invalid states, invalid identifiers, null collections, malformed UUIDs, and contradictory placement for the same identity scope.

## No Fallback

Resolution is exact.

The following conditions fail explicitly:

- missing route;
- disabled route;
- disabled destination;
- unknown destination;
- duplicate route;
- contradictory destination assignment;
- unsupported provider;
- invalid configuration.

The resolver does not select a default database after a failure and does not probe another destination to keep an operation running.

## Bootstrap Directory Location

`IAuthenticationDirectoryLocator` may locate a registered directory before authentication by using a trusted application context and registered authentication-context key.

Directory location is not authentication and does not prove tenant membership or authorization.

The locator does not search an email address across all databases and does not accept a connection string from HTTP input.

## Secrets

Routing configuration stores opaque secret references rather than plaintext connection strings.

Example:

```text
env:IDENTITY_ACCESS_POSTGRES_DEFAULT
```

Secret references remain server-side and are never public response fields.

## Loading and Route Stability

The configuration file is loaded and validated before activation. The active routing snapshot is immutable.

An operation retains its `ResolvedDatabaseRoute` for the complete logical operation instead of resolving again between reads and writes.

Changing a route does not migrate data. Data movement requires a separate controlled procedure.

## Local Activation

Example PowerShell configuration:

```powershell
$env:IdentityAccess__Routing__Provider = "configuration"
$env:IdentityAccess__Routing__FilePath = (Resolve-Path .\config\routing.example.json).Path

dotnet run --project src\IdentityAccess.Api --launch-profile http
```

Remove overrides after use:

```powershell
Remove-Item Env:IdentityAccess__Routing__Provider -ErrorAction SilentlyContinue
Remove-Item Env:IdentityAccess__Routing__FilePath -ErrorAction SilentlyContinue
```

## Operational Invariants

1. Clients never provide connection strings.
2. Missing or disabled routes fail closed.
3. A route does not grant authorization.
4. Route selection remains stable for an operation.
5. Different identity scopes remain logically distinct when sharing one database.
6. Route configuration changes do not implicitly migrate data.
7. Secrets are resolved only by trusted server infrastructure.
