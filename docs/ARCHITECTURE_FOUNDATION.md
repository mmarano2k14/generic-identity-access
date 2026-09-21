# Identity & Access Subproject Foundations

**Date: September 21, 2026. Source version described below: 0.1.0.**

> Historical document for the initial foundation. Version 0.2.0 adds file-based routing and
> clarifies its granularity in `ROUTING_CONFIGURATION.md`. The current validation status
> is documented in `VALIDATION.md`. References to “next contract” and “not executed”
> below describe the initial delivery, not the complete current version.

## Confirmed Decisions

The server is a shared logical ASP.NET Core API. The target storage is PostgreSQL,
with multiple databases possible and placement resolved server-side. An application
may use multiple destinations; a destination may host multiple tenants when isolation
constraints allow it. Neither a single global database nor one database per application
is required.

Each consuming application embeds its own UI module and client. The application context
comes from the integration and does not constitute a permission. No screen asks the user
to choose between projects. Selecting an authorized tenant is a separate concern.

The source architecture files provided for reference are not modified by this delivery.

## Code Actually Created

| Project | Current responsibility |
|---|---|
| `IdentityAccess.Domain` | Immutable identity references, User/Tenant/Group states, memberships, and structural checks |
| `IdentityAccess.Contracts` | Public profile and diagnostic DTOs, with no secrets or physical destination details |
| `IdentityAccess.Application` | Explicit profile projections and foundation-status description |
| `IdentityAccess.Api` | Host composition, local diagnostics, and refusal to start in Production |
| `IdentityAccess.Tests` | Tests for models, projections, contracts, and host behavior; not executed here |

Dependencies flow from the API to Application/Contracts and from Application to
Domain/Contracts.

The domain and contract projects have no external package dependency. No reference to
the runtime engine, Redis, Npgsql, or EF Core is introduced in this first increment.

The TypeScript client calls diagnostic HTTP endpoints. The Next.js example imports
`server-only`.

It exposes no sessions, tokens, RBAC evaluator, or access-context rotation.

## Logical Identity Scope

`IdentityScopeId` represents the identity scope already required by the roadmap.

A user reference is `(IdentityScopeId, UserId)` and a tenant reference is
`(IdentityScopeId, TenantId)`. A group additionally includes the application context and
its `GroupId`.

This makes it possible to represent two distinct accounts carrying the same local
identifier without merging them. A reference remains stable when physical placement
changes.

Email is not used as the immutable technical identifier.

**This reference shape is a foundation-contract proposal, not the silent selection of a
global directory or an SSO policy.** The operational rules for assigning scopes, sharing
a directory, and producing the OIDC subject remain to be defined. No token or SQL table
materializes those decisions yet.

Application keys in the code are case-sensitive and are not implicitly normalized:
1 to 64 characters, starting with a lowercase letter, followed by lowercase letters,
digits, or hyphens.

This constraint is explicit and testable; it is not the TRN grammar.

## Memberships and Groups

A user may belong to multiple tenants within the same identity scope.

The membership constructor rejects mismatched scopes.

Structural addition to a group rejects a different tenant, including when two scopes
reuse the same local `TenantId`. It requires both the group and the tenant membership
to be active.

The account status is separate from the tenant-membership status.

Domain objects are immutable; no endpoint allows these states to be modified yet.

Group membership carries its application context. It does not grant rights in another
application.

A `UserGroup` is never the runtime engine `TenantGroupId`.

Permissions, their bindings, and exact resource scopes are not yet implemented.

Construction checks are necessary but insufficient for production operation:
the administrator must still be authorized, current account and tenant state must be
checked, SQL constraints must be enforced, concurrent mutations must be handled,
administrative operations must be audited, and revocation must be supported.

A domain instance created by a caller is never a trusted context.

## Public Profiles

Projections expose only references, display name, and the relevant status.

They do not expose credentials, MFA factors, refresh tokens, or connection information.

The mapper is not an authorization layer: the future read service must authorize every
scope and subject before returning a projection, including batch reads.

No login email, secret, or recovery mechanism is added before account uniqueness and
storage rules are defined.

These are explicit missing features, not demonstration storage presented as real
authentication.

## PostgreSQL and Routing: Next Boundary

No database schema is finalized in this increment.

The next contract must represent the route requested by a server-side operation,
a registered destination, a revision, and its administrative status.

The initial provider will be file-based, with an optional SQL catalog behind the same
contract.

Secret references remain server-side.

A missing, ambiguous, or disabled route must block the operation. There is no fallback
to a default database.

An operation must preserve its destination and route version for its lifetime.

Co-location requirements for atomic operations will be defined before table families
are split.

Foreign keys and local transactions will not be presented as cross-database guarantees.

The blocking choices remain visible:
shared or separate directories, placement of multi-tenant users, directory localization
before authentication, the relationship between an account and the OIDC subject,
and the initial routing unit.

This repository does not hide a global-database decision inside configuration defaults.

## Security and Existing Compatibility

There is no mock `Allow`, predefined administrator, shared password, anonymous account
creation, or custom cryptographic mechanism.

`RequireCapability`, `IAuthorizationEngine`, and `X-Access-Context` are not reimplemented
from names alone.

The future adapter must preserve the guarantees actually present in the existing .NET
code: rehydration, isolation of concurrent processing, rotation enabled or disabled,
expiration, revocation, and in-flight protection.

Compatibility is not achieved simply by adding an Allow/Deny endpoint.

## Delivery Limitation

This delivery starts the model-and-boundary pack. It does not close the RBAC
compatibility gate, does not provide a migrated database, and does not constitute an
identity service ready for production.

Execution of the .NET build and tests still remains to be obtained.

