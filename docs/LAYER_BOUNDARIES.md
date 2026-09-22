# Layer Boundaries

Identity Access separates application contracts, infrastructure implementations, HTTP
transport concerns, and external authorization integration.

## API

`IdentityAccess.Api` owns:

- the ASP.NET Core composition root;
- MVC controllers and HTTP transport contracts;
- HTTP filters and API-specific authorization metadata;
- OpenAPI configuration;
- HTTP problem handling.

The API does not implement password hashing, session token generation, authentication
client configuration parsing, PostgreSQL stores, database connection management, or
routing-provider parsing.

## Authentication Infrastructure

`IdentityAccess.Infrastructure.Authentication` owns concrete local-authentication
infrastructure:

- ASP.NET Core password hashing;
- cryptographic opaque-session token generation and hashing;
- trusted authentication-client configuration parsing;
- authentication infrastructure registration.

Concrete implementation types are internal. The supported public integration surface is
the registration extension.

## PostgreSQL Infrastructure

`IdentityAccess.Infrastructure.PostgreSql` owns PostgreSQL implementation details.

Concrete stores, connection factories, schema migrators, secret resolvers, and
configuration options are internal implementation types.

The supported public surface is limited to:

- PostgreSQL infrastructure registration;
- stable PostgreSQL storage failure contracts required across the API boundary.

Application code depends on storage interfaces, not PostgreSQL store classes.

## Configuration Routing Infrastructure

`IdentityAccess.Infrastructure.ConfigurationRouting` owns file-based routing parsing and
the immutable routing provider.

The provider implementation is internal.

The public integration surface consists of:

- routing infrastructure registration;
- stable configuration failure contracts.

Application code depends on `IDatabaseRouteResolver` and
`IAuthenticationDirectoryLocator`, not on the concrete provider.

## Test Visibility

Infrastructure assemblies expose internals to `IdentityAccess.Tests` only so contract
tests can validate implementation invariants without promoting implementation types to
the supported public API.

This test visibility does not change runtime dependency direction or the public package
surface.

## Design Rule

Application and domain projects define contracts and semantics.

Infrastructure assemblies implement those contracts.

The API composes features but does not contain infrastructure implementations.
