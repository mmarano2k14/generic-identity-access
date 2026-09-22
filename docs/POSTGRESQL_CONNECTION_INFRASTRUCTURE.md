# PostgreSQL Connection Infrastructure

**Source version: 0.3.0. Date: September 21, 2026.**

## Scope

This version connects the routing layer to reusable PostgreSQL connection infrastructure.
It does not introduce an identity schema, migrations, repositories, authentication, OIDC,
MFA, or RBAC decisions.

The routing layer still selects the physical destination. The storage layer receives an
already resolved `ResolvedDatabaseRoute` and never selects another destination.

## Secret Resolution

Connection strings remain outside routing files. A destination carries an opaque reference
such as:

```text
env:IDENTITY_ACCESS_POSTGRES_A
```

The first implementation supports only the `env:` scheme. Unsupported schemes and missing
values fail closed with sanitized error codes. Secret values are never placed in public DTOs
or diagnostic output.

## Bounded Pools

Each destination receives at most one Npgsql data source in the process. Data sources are
created lazily and use bounded pooling.

Default limits:

| Setting | Default |
|---|---:|
| Minimum pool size | 0 |
| Maximum pool size | 20 |
| Connection timeout | 10 seconds |
| Command timeout | 30 seconds |

A destination cannot silently switch to another secret reference during the same process
lifetime. Such a mismatch fails with `DestinationDefinitionChanged`.

## Operation Boundary

Callers resolve a route once and keep the returned snapshot for the complete logical
operation. The connection factory opens a connection from that snapshot:

```text
server operation
      |
      v
ResolvedDatabaseRoute
      |
      v
IIdentityDatabaseConnectionFactory
      |
      v
bounded NpgsqlDataSource for the registered destination
      |
      v
PostgreSQL connection
```

The factory does not rerun routing between reads and writes.

## API Configuration

PostgreSQL connection infrastructure is disabled by default:

```json
{
  "IdentityAccess": {
    "PostgreSql": {
      "Enabled": "false"
    }
  }
}
```

Local activation may use environment variables:

```powershell
$env:IdentityAccess__PostgreSql__Enabled = "true"
$env:IdentityAccess__PostgreSql__MaximumPoolSize = "20"
$env:IdentityAccess__PostgreSql__ConnectionTimeoutSeconds = "10"
$env:IdentityAccess__PostgreSql__CommandTimeoutSeconds = "30"
```

Enabling this infrastructure does not change readiness to green because no persistent
identity schema or authentication/authorization implementation exists yet.

## Security Boundaries

- Clients never provide connection strings.
- Route resolution is not authorization.
- Secret resolution occurs only on the server.
- Missing secrets fail closed.
- Npgsql errors are wrapped in sanitized storage failures.
- Cancellation remains cancellation and is not converted into a business denial.
- Pooling is bounded per destination.
- A destination cannot be rebound silently inside a running process.

## Remaining Work

The next storage increment must add an explicit schema and migration ownership model,
then validate isolation against at least two real PostgreSQL databases. Persistent directory
operations must preserve the existing identity-scope boundary and route snapshot for each
operation.
