# Users Administration Integration

**Date:** 2026-10-07  
**Status:** Qualified

## Purpose

This document defines the reusable Generic Identity user-administration integration surface and the boundaries that consuming applications must preserve.

The reference administration implementation establishes the supported behavior. Consuming applications own routing, navigation, branding, page composition, and presentation, while Generic Identity owns identity contracts, server-side administration workflows, authorization requirements, validation, concurrency, and credential-security semantics.

## Supported workflows

| Administration workflow | Generic Identity SDK surface | Consumer responsibility |
|---|---|---|
| Scope-wide user list and selected-user detail | `loadNextUsersDirectory()` using `directory.users.list/get` | Route composition and presentation |
| Membership-limited user list and selected-user detail | `loadNextUsersDirectory()` using `directory.tenantUsers.list` | Tenant-context presentation without scope-wide fallback |
| Authorized-tenant aggregation | Bounded `loadNextUsersDirectory()` fan-out | Present tenant and membership provenance clearly |
| Create user | `createNextUserFromForm()` | Host-owned Server Action binding and UX |
| Update user, lifecycle and optimistic concurrency | `updateNextUserFromForm()` | Preserve `userId` and `expectedVersion` |
| Secret-free credential metadata | `loadNextUserCredentialMetadata()` | Render metadata only |
| Create or change password credential | `createNextPasswordCredentialFromForm()`, `changeNextPasswordCredentialFromForm()` | Secure form composition and error presentation |
| Access assignment provenance | `loadNextUserAccessInsight()` | Present provenance without interpreting it as an authorization verdict |
| Cross-navigation | Stable user and tenant identifiers | Consumer-owned routes and navigation |

## Security and authorization boundaries

- Scope-wide user mutations require scope-wide effective visibility and backend RBAC authorization.
- A hidden or disabled browser control is not an authorization boundary.
- Membership-limited subjects must not call the unrestricted `directory.users` list or detail operations.
- Query-supplied tenant identifiers represent requested context only. The server remains authoritative for tenant visibility and access.
- Cross-tenant aggregation is intentionally bounded and must never be presented as an unbounded export.
- Access insight represents assignment provenance. It does not replace authorization evaluation or wildcard processing.
- Password hashes, plaintext credentials, refresh tokens, session tokens, and MFA secrets must never be returned to or rendered by administration UI.
- Credential changes rely on the Identity API for session and refresh-continuity behavior.
- Optimistic concurrency must preserve `expectedVersion`; stale writes must remain explicit conflicts.
- React function-action forms must not force an HTML `method` attribute.
- Consuming applications must use the Generic Identity public packages rather than importing the compatibility transport directly.
- Consuming applications must not implement a parallel RBAC engine or local TRN-generation path.

## Relevant SDK surface

### Next.js server integration

```text
loadNextUsersDirectory
loadNextUserCredentialMetadata
loadNextUserAccessInsight
createNextUserFromForm
updateNextUserFromForm
createNextPasswordCredentialFromForm
changeNextPasswordCredentialFromForm
```

### React administration components

```text
UserForm
PasswordCredentialForm
UserDetailsPage
TenantLinkedUsersPage
UserAccessInsightPanel
UserPasswordCredentialPanel
```

## Behavioral requirements

### Scope-wide administration

A scope-wide administrator may:

```text
list users
select a user
create a user
update a user
change lifecycle state
inspect credential metadata
create/change a password credential
inspect access assignment provenance
```

Every mutation remains server-authorized.

### Membership-limited administration

A membership-limited administrator:

```text
sees only tenant-linked user projections
does not receive unrestricted scope-wide user reads
cannot escape effective tenant visibility through URL or form input
remains subject to tenant-local capability checks
```

### Authorized-tenant aggregation

The aggregated tenant view may contain the same User ID more than once when that identity has multiple tenant memberships.

Rows must therefore preserve concrete relationship identity:

```text
tenantId
membershipId
userId
```

This view is bounded and must communicate that bound to consumers.

## Concurrency

User updates use optimistic concurrency.

Expected behavior:

```text
read record version
submit expectedVersion
backend accepts current version
or
backend returns conflict for stale version
```

The consuming application must not silently retry a stale mutation as an overwrite.

## Credential handling

Administration surfaces may expose only credential metadata required for lifecycle management.

Never expose:

```text
password hashes
plaintext passwords after submission
refresh tokens
session tokens
MFA secrets
provider-private enrollment material
```

Password input values remain transient form input and are not persisted in browser route state.

## Qualification

A compliant consumer integration should verify:

1. Scope-wide subjects can use supported user-administration operations.
2. Membership-limited subjects cannot trigger unrestricted user reads.
3. User creation produces a stable user identity without creating an implicit tenant membership.
4. User updates preserve optimistic concurrency.
5. Credential metadata remains secret-free.
6. Password creation/change never returns credential material.
7. Access insight remains provenance-only and is not presented as an authorization verdict.
8. Unauthorized tenant context is rejected server-side.
9. React form actions render without action/method hydration warnings.
10. Cross-navigation uses stable identifiers without changing authorization semantics.

## Non-goals

This integration does not define:

```text
consumer-specific navigation
consumer-specific visual design
a second authorization engine
TRN construction in the consumer
unbounded identity export
permission explanation semantics not supported by the backend
provider-specific MFA enrollment
session inventory semantics not exposed by the backend
```
