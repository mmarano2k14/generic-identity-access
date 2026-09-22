# Cancellation Contracts

All asynchronous production I/O contracts require an explicit `CancellationToken`.

## Rule

Public and internal server-side I/O APIs use:

```csharp
CancellationToken cancellationToken
```

They do not expose:

```csharp
CancellationToken cancellationToken = default
```

This applies to routing, secret resolution, database connections, schema migration, and
persistent directory stores.

## Rationale

Cancellation is part of the operation contract. Requiring the caller to provide the token
makes propagation visible at every boundary and prevents a deep infrastructure call from
silently detaching from the lifetime of the request or operation that initiated it.

The token belongs to the current logical operation only. Cancellation of one operation must
not mutate or cancel unrelated concurrent work.

## API Boundary

ASP.NET Core controllers receive the request cancellation token and propagate it through
application services, routing, persistence, authentication, and authorization operations.

Background or explicitly detached work must supply a token representing its own lifetime.
It must not rely on an omitted default token.

## Enforcement

The architecture test `CancellationTokenContractTests` scans production C# source and fails
when an optional `CancellationToken = default` parameter is introduced.
