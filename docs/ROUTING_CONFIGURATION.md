# Server-Side Routing by Configuration

**Source version: 0.2.0. Date: September 21, 2026.**

## Delivered Scope

This version adds the resolution contract, the file-based provider, strict document
validation, and a directory-location registry used before authentication.

It does not create PostgreSQL connections, connection pools, schemas, accounts, or
permissions.

`IdentityAccess.Application.Routing` owns the server-side contracts.

`IdentityAccess.Infrastructure.ConfigurationRouting` implements them using standard
.NET libraries. The public contracts project and the TypeScript client remain unchanged.

No additional NuGet package is required for this provider.

## Explicit Implementation Choice: One Directory per Identity Scope

The roadmap left the initial routing granularity open. This version adopts a deliberately
limited unit without arbitrarily splitting tables:

```text
(ApplicationKey, IdentityScopeId, identity-directory)
                         |
                         v
           registered PostgreSQL destination
```

`IdentityScopeId` is the logical authority of a directory and does not contain a database
name.

The foundation user reference remains `(IdentityScopeId, UserId)`. A directory may contain
multiple tenants. In this first contract, their identity data remains co-located; routing
is not performed per table or per user.

This choice does not create a real transaction boundary yet. PostgreSQL constraints and
transactions will be implemented when storage is connected.

| Representable placement | Interpretation |
|---|---|
| Application B / scope C -> destination A | One destination hosts multiple distinct scopes |
| Application B / scope A -> destination A, with an explicit route | Two applications locate the same directory; no implicit SSO or permission |

The same scope cannot point to different destinations depending on the application,
including through disabled routes. The complete configuration is rejected in that case.

Two different scopes using the same database do not become the same identity.

SQL-level isolation between those scopes remains to be implemented and tested.

**Important limitation:** this version does not distribute tenants from the same directory
across multiple databases. It does not merge accounts from different scopes.

Sharing an account across physically separated tenants would require an explicit evolution
of the authority and consistency model, not a simple configuration-file change.

This provider makes no decision regarding SSO, email uniqueness, or OIDC subject semantics.

## File Format

The complete `config/routing.example.json` file is an inactive example. It contains only
fictitious identifiers and secret references.

Required fields are:

| Field | Contract |
|---|---|
| `schemaVersion` | Exact string `1` |
| `revision` | Strictly positive integer for the document |
| `routingProvider` | Exact string `configuration`; no mixing with a catalog |
| `placementGranularity` | Exact string `identity-scope` |
| `destinations` | 1 to 256 registered PostgreSQL destinations |
| `routes` | 1 to 4,096 exact routes |
| `authenticationContexts` | 0 to 4,096 explicit bootstrap registrations |

Each destination contains `key`, `provider`, `connectionSecretRef`, and `state`.

Each route contains `applicationKey`, `identityScopeId`, `dataSet`, `destinationKey`,
`version`, and `state`.

Each bootstrap context contains `key`, `applicationKey`, `identityScopeId`, and `state`.

The only accepted state values are `active` and `disabled`.

A route version is a strictly positive integer.

The only accepted dataset is `identity-directory`.

The scope is a non-empty UUID in D format.

Keys use the foundation `ApplicationKey` grammar:

- 1 to 64 characters;
- first character must be a lowercase letter;
- remaining characters may contain lowercase letters, digits, or hyphens.

The document is case-sensitive, limited to 1 MiB, and limited to a depth of 16.

Unknown properties, missing required properties, duplicate JSON properties, comments,
trailing commas, null lists, and null elements are rejected.

Duplicate-property detection also traverses nested objects and compares decoded property
names; escaped notation cannot bypass this validation.

Earlier architecture JSON examples were illustrative and were not a published file format.

They must not be loaded unchanged: `tenantId` is not part of the `identity-scope` contract
for this increment.

The provider rejects unknown routing granularity instead of silently converting it into
another model.

## Resolution and No Fallback

`IDatabaseRouteResolver.ResolveAsync` performs an exact lookup using the application,
identity scope, and dataset.

It returns an immutable snapshot containing:

- the destination;
- the PostgreSQL provider;
- the configuration revision;
- the route version;
- a server-only secret reference.

| Situation | Result |
|---|---|
| Route missing | `DatabaseRouteFailure.RouteNotFound` |
| Route disabled | `DatabaseRouteFailure.RouteDisabled` |
| Destination disabled | `DatabaseRouteFailure.DestinationDisabled` |
| Duplicate route | File rejected before activation |
| Unknown destination | File rejected before activation |
| Contradictory placement for the same scope | File rejected before activation |
| Cancellation requested | `OperationCanceledException`, not a business denial |

There is no wildcard, default route, implicit priority, or "last entry wins" policy.

The availability of another destination is never consulted in order to change the identity
boundary.

## Bootstrap: Location, Not Authentication

`IAuthenticationDirectoryLocator.LocateAsync` receives an expected application and a
registered context key.

It verifies the exact registration, the application match, and the states of the context,
its route, and its destination.

The lookup identifies one directory without using an email address, scanning every
database, or converting an HTTP value into a connection string.

The result is not a session, an authenticated principal, or a validated membership.

The expected application must come from a trusted server-side integration.

This provider does not itself validate a host, OIDC client, or header.

A trusted HTTP/OIDC adapter still needs to be implemented; no public bootstrap endpoint is
added here.

Constructing a .NET request does not authenticate a caller.

## Secrets and Diagnostics

A `ConnectionSecretReference` is an opaque `scheme:identifier` value, for example:

```text
env:IDENTITY_ACCESS_POSTGRES_A
```

It does not yet resolve the environment variable.

There is no password or connection-string reader in this increment.

Plaintext connection parameters must not appear in either the configuration file or the
registry.

The reference constructor rejects forms containing spaces, `;`, `=`, `?`, or
connection-string fragments.

Shape validation cannot determine whether an operator pasted a secret value into an
identifier, so configuration content remains an operational responsibility.

Controlled error messages do not include rejected input, file paths, secret references,
or original parsing exceptions.

`ToString` and JSON representations of the secret reference do not reveal its value.

Routing results remain server-side types and must not be returned as public DTOs.

Existing endpoints expose only foundation diagnostics.

There is no endpoint for listing destinations or bootstrap registrations.

Resolving a route is never an RBAC decision and does not grant permission to read a
profile.

## Loading and In-Flight Operations

A single file is selected at startup.

Parsing and all validation complete before service registration.

Activated dictionaries are immutable.

The resolver and directory locator use the exact same provider instance.

There is no file watcher, hot reload, or atomic replacement across instances in this
increment.

Changing the file does not affect an already running API.

Applying a change therefore requires a controlled restart of all relevant instances.

A disable operation written to disk has no immediate effect on an active instance.

The revision and route version describe the loaded snapshot.

This version does not maintain a durable revision counter and does not protect against
configuration rollback across restarts.

Deployment must verify the active versions.

A future catalog must define its own publication, revocation, and propagation protocol.

An operation must retain its `ResolvedDatabaseRoute` until completion instead of resolving
again between a read and a write.

The contract preserves this object, but no storage context currently enforces this rule for
SQL requests.

**Changing a route does not migrate data.**

A storage cutover must control data transfer, schema compatibility, writes, and in-flight
operations.

No hot data movement or mid-operation revocation mechanism is delivered here.

## Local API Activation

From the solution root, in the terminal where the API will be started:

```powershell
$env:IdentityAccess__Routing__Provider = "configuration"
$env:IdentityAccess__Routing__FilePath = (Resolve-Path .\config\routing.example.json).Path

dotnet run --project src/IdentityAccess.Api --launch-profile http
```

The path is server-side configuration and is never a parameter received from a browser.

A relative path is resolved from the API content root.

Using the absolute path above avoids ambiguity between the solution directory and the host
directory.

The `none` mode delivered in `appsettings.json` leaves the resolver unregistered.

A configured path while using `none`, an unknown provider, or a conflicting source in the
`IdentityAccess:Routing` section causes startup failure instead of being ignored.

In `configuration` mode, a missing or invalid file also causes startup failure.

After valid activation, `/health/ready` still returns **503** with `ready: false`.

Only the `database-routing` blocker is removed.

The following blockers remain:

- `postgresql-persistence`;
- `authentication`;
- `rbac-integration`.

All three public service-configuration indicators remain `false`.

To remove the terminal overrides after stopping the API:

```powershell
Remove-Item Env:IdentityAccess__Routing__Provider -ErrorAction SilentlyContinue
Remove-Item Env:IdentityAccess__Routing__FilePath -ErrorAction SilentlyContinue
```

The host remains limited to `Development` and `Testing` environments.

Enabling routing does not make this increment suitable for use as a production security
service.

## Remaining Work

The next storage increment requires:

- effective secret resolution;
- Npgsql;
- bounded connection pools per destination;
- operation-bound database connections;
- tests against at least two PostgreSQL databases.

Migrations and the persistent directory remain separate work items.

RBAC compatibility still requires the existing engine sources and contracts, including:

- TRN handling;
- `RequireCapability`;
- `IAuthorizationEngine`;
- accessors;
- rotation;
- rehydration;
- in-flight protection.

This routing provider does not simulate any of them.

Scope reference: Identity & Access roadmap v1, sections 11, 12, and 18.

The implementation choices and limitations above define version 0.2.0 without
retroactively changing the historical status of the source architecture document.
