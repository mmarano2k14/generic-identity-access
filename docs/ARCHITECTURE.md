# Architecture

## Purpose

Generic Identity & Access provides a reusable identity, authentication, authorization, and access-control service for applications that require strong multi-tenant boundaries without coupling logical identity to physical storage placement.

## Core Boundaries

```text
Consuming Application
        |
        v
ASP.NET Core API
        |
        +------------------+------------------+
        |                  |                  |
        v                  v                  v
Directory             Authorization       Authentication
        |                  |                  |
        v                  v                  v
Routing / Stores      RBAC Adapter      Credentials / Sessions
        |                  |                  |
        v                  v                  v
PostgreSQL          External RBAC       PostgreSQL
```

The service owns identity-directory data, memberships, groups, policies, resource scopes, local credentials, sessions, and its own routing metadata.

It does not become a generic SQL proxy and does not own application business data.

## Identity

Logical identity is independent from database placement.

A user is identified by:

```text
IdentityScopeId + UserId
```

A tenant is identified by:

```text
IdentityScopeId + TenantId
```

The same local UUID in two identity scopes does not represent the same logical subject.

Email and login identifiers are not immutable technical identities.

## Tenancy and Membership

A user may belong to multiple tenants within the supported identity model.

Account status and tenant-membership status are separate concerns. Group membership requires a valid tenant membership and remains scoped to the tenant and application context.

## Resource Scopes

Applications may define generic hierarchical resource types such as organization, business unit, department, project, environment, or any other application-specific scope.

The core does not hardcode these business concepts.

A tenant may be linked to one or more root resource scopes. Policies may target the tenant, an exact resource scope, or a resource scope and its descendants.

## Authorization

The authorization model separates:

```text
Group Membership
      |
      v
Policy Binding
      |
      v
Permission Policy
      |
      v
Policy Statements
      |
      v
Assigned Capability Grants
      |
      v
TRN Materialization
      |
      v
External RBAC Engine
```

Identity Access determines which grants are structurally applicable. The external RBAC engine remains the authority for wildcard matching and final Allow / Deny evaluation.

A technical failure is not converted into an authorization denial.

## Authentication

Local authentication is scoped through registered clients and trusted authentication contexts.

The current implementation supports password credentials, lockout, opaque sessions, session validation, logout, and registered redirect URI validation.

Registered redirect URIs are matched exactly. Client input cannot introduce a new trusted redirect destination.

## PostgreSQL Placement

Database placement is resolved by trusted server configuration.

```text
Application + Identity Scope
          |
          v
Database Route Resolver
          |
          v
Resolved Database Route
          |
          v
PostgreSQL Destination
```

A route is infrastructure metadata, not identity and not authorization.

One resolved route is retained for the logical operation. A route is never silently replaced by another destination after a failure.

## Concurrency

Mutable persisted records use explicit optimistic concurrency through row versions.

Stale writes fail rather than silently overwriting newer state.

Request state is operation-local. The architecture does not use global mutable current-user, current-tenant, or current-database variables.

## API Boundary

The API uses ASP.NET Core MVC controllers and OpenAPI.

Controllers expose transport concerns. Application services own orchestration. Domain and persistence concerns remain outside controllers.

Administrative operations fail closed when trusted authorization is unavailable.

## Security Invariants

1. Physical database placement never defines logical identity.
2. Client input never selects a PostgreSQL connection string.
3. A database route never grants permission.
4. A TRN is not a credential.
5. Resource-scope applicability is resolved before external RBAC evaluation.
6. Wildcard authorization belongs to the external RBAC engine.
7. Technical authorization failures remain distinct from explicit denials.
8. Mutable records reject stale writes.
9. Concurrent requests do not share mutable operation state.
10. Credentials, session tokens, and connection secrets are not exposed through public diagnostic or administration contracts.
11. Redirect destinations are registered and validated server-side.
12. Authentication and administrative authorization fail closed.


## Assembly Boundaries

Infrastructure is split by responsibility:

```text
IdentityAccess.Infrastructure.Authentication
IdentityAccess.Infrastructure.ConfigurationRouting
IdentityAccess.Infrastructure.PostgreSql
```

Concrete infrastructure implementations are internal. Composition uses each
infrastructure assembly's public registration surface, while application services depend
on interfaces owned by the application layer.

The HTTP API does not own password hashing, token generation, PostgreSQL store
implementations, or routing-provider implementations.

See `docs/LAYER_BOUNDARIES.md`.


## OIDC Protocol Boundary

OIDC protocol orchestration is layered over the existing local authentication contracts:

```text
registered public client
        |
validated local session
        |
authorization request
        |
one-time PKCE-bound code
        |
atomic PostgreSQL consume
        |
initial refresh-token family
        |
RS256 access token + ID token + opaque refresh token
        |
refresh_token grant
        |
family-serialized atomic rotation
        |
RS256 access token + rotated opaque refresh token
```

The protocol layer does not own browser login UI and does not accept password credentials or
client secrets at the token endpoint.

Authorization codes and refresh tokens are opaque; PostgreSQL stores only their SHA-256
hashes. Refresh families have an absolute expiry and remain bound to the source local session,
current active user, public client, application, and authentication context.

Current account and source-session state are rechecked during code redemption, initial family
creation, and every refresh rotation. Reuse of a consumed refresh token revokes the entire
family while the public protocol still returns only `invalid_grant`. Refresh grants issue a new
access token and rotated refresh token but no new ID token in the current protocol design.

JWT signing uses a process-pinned RSA key ring. One explicit active `kid` signs new access and
ID tokens, while all configured public keys remain published through JWKS for validation
continuity across controlled restarts. Retired keys may be configured as public-only material;
the active key must contain private material. Signing-key configuration is server-owned and is
not mutable through protocol requests.
