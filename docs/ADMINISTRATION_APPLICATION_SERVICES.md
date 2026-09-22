# Administration Application Services

**Source version: 0.13.0. Date: September 21, 2026.**

Version 0.13.0 moves administration orchestration out of ASP.NET Core controllers and into
application-layer services.

## Services

Two application contracts are introduced:

- `IDirectoryAdministrationService`
- `IPolicyAdministrationService`

Their implementations own routing plus persistence orchestration for the existing
administration use cases.

Controllers remain responsible for HTTP concerns only:

- route/body binding;
- Swagger/OpenAPI metadata;
- HTTP status mapping;
- fail-closed handling when administration services are unavailable.

## Directory administration

`DirectoryAdministrationService` centralizes:

- user read/create/update;
- tenant read/create/update;
- tenant membership read/find/create/update;
- group read/create/update;
- group-member list/add/remove.

Every operation resolves exactly one database route and reuses that immutable route for
all persistence calls in that operation.

Group membership creation performs the current structural checks in one place: the group
and tenant membership are loaded through the same route, tenant boundaries are checked,
and the domain factory enforces active-state requirements.

## Policy administration

`PolicyAdministrationService` centralizes:

- policy read/create/update;
- statement list/add/remove;
- group-policy binding list/add/remove.

Policy bindings load both the group and policy through the same operation route before the
domain binding is created. Wildcard evaluation is not performed by this service.

## Concurrency and cancellation

Optimistic concurrency remains implemented by the persistence layer and exposed to the
HTTP layer as `409 Conflict` for stale mutable records.

All methods introduced by these application contracts require an explicit
`CancellationToken`. No optional cancellation-token parameter is introduced by this version.

The services hold no per-request mutable state. They depend only on stateless routing and
store contracts, preserving concurrent operation independence.

## Registration

The API registers the administration services only when both routing and the required
persistence contracts are registered. An unconfigured host still starts for diagnostics,
but administration endpoints remain unavailable/fail-closed.

## Security boundary

This version does not authenticate callers and does not weaken
`RequireAdministrationCapability`.

Authentication, login forms, OIDC, redirect URI validation, sessions, MFA, and account
recovery remain separate work.

## Database

No PostgreSQL migration is introduced in version 0.13.0.
