# Membership Administration Integration

**Date:** 2026-10-07  
**Status:** Qualified

## Purpose

This document defines the Generic Identity membership-administration integration surface.

The SDK composes tenant-scoped membership lifecycle operations, membership candidate resolution, group assignment reconciliation, and organization assignment reconciliation while preserving the backend as the authority for authorization, lifecycle validation, and concurrency.

Consuming applications own routing and presentation. They must reuse the Generic Identity workflows rather than reproduce membership semantics locally.

## Reused transport

The integration composes the existing typed clients:

```text
client.directory.memberships.list/get/findByUser/create/update
client.directory.membershipCandidates.findByLogin/createMembershipByLogin
client.directory.tenantUsers.list
client.directory.tenantGroupAssignments.list
client.accessControl.groups.list/addMember/removeMember
client.organizations.organizations.list
client.organizations.memberships.listForTenantMembership/add/activate/remove
isAllowedOnServer()
```

## Public SDK surface

### `@generic-identity/next/server`

```text
loadNextMembershipWorkspace
createNextTenantMembershipFromForm
findNextTenantMembershipCandidateFromForm
updateNextTenantMembershipFromForm
replaceNextTenantMemberGroupsFromForm
replaceNextTenantMemberOrganizationsFromForm
```

### `@generic-identity/react/directory`

```text
MembershipForm
MembershipCandidateForm
MembershipCandidateLookupPanel
MemberGroupAssignmentsForm
MemberOrganizationAssignmentsForm
MembershipsPage
```

`MembershipsPage` supports optional resolved member display names while preserving stable membership and user identifiers.

## Access and safety rules

- Tenant selection is constrained by the effective identity context.
- Scope-wide administrators may create a membership using a stable User ID.
- Membership-limited administrators use exact-login candidate lookup and creation.
- Candidate eligibility is revalidated at mutation time.
- Inactive or already-associated candidates are rejected according to backend rules.
- Group and organization reads are capability protected.
- Absence of read permission must not be interpreted as an empty grant.
- Bulk replacement is blocked when a source collection reaches its safe paging bound.
- Group replacement must not create membership in an inactive group.
- Organization replacement preserves row-version concurrency for removal and reactivation.
- Organization membership represents organizational belonging, not an authorization grant.
- Multi-request relationship reconciliation is not an HTTP-level transaction. A later request may fail after an earlier operation has succeeded.
- After a partial failure, the client must reload current state before retrying.

## Candidate lookup

Membership candidate lookup is intentionally server-mediated.

The consuming application should provide a protected reusable interaction that:

```text
accepts exact login input
performs server-side candidate resolution
returns only safe candidate metadata
revalidates candidate eligibility during creation
does not encode login identifiers in page URLs
```

The browser must not treat candidate preview as authorization to create a membership.

## Assignment reconciliation

### Group assignments

Group replacement reconciles the desired set of tenant-local group memberships.

The server remains authoritative for:

```text
tenant scope
group lifecycle
membership lifecycle
capability checks
relationship validity
```

### Organization assignments

Organization replacement reconciles organization membership relationships attached to the tenant membership.

The integration must preserve:

```text
organization lifecycle
tenant isolation
rowVersion concurrency
activation/reactivation semantics
removal semantics
```

Organization assignment does not itself imply authorization.

## React Server Action compatibility

Forms using React or Next.js function actions must not force an explicit HTML `method` attribute.

This preserves the expected React action behavior and avoids hydration/action warnings.

## Error and partial-application behavior

The integration must distinguish:

```text
validation failure
authorization failure
optimistic concurrency conflict
dependency/transport failure
partial relationship reconciliation
```

A partial reconciliation must never be presented as an all-or-nothing transaction.

The correct recovery behavior is:

```text
report safe failure
reload authoritative state
allow the administrator to decide whether to retry
```

## Qualification

A compliant consumer integration should verify:

1. Membership overview resolves authorized tenant context correctly.
2. Stable User ID membership creation works for scope-wide administrators.
3. Exact-login candidate lookup works for membership-limited administration.
4. Candidate eligibility is revalidated at mutation time.
5. Membership updates preserve optimistic concurrency.
6. Group assignment reconciliation respects lifecycle and authorization.
7. Organization assignment reconciliation preserves row versions.
8. Organization membership is never presented as an authorization grant.
9. Bounded collections do not silently perform unsafe bulk replacement.
10. Partial failures require authoritative reload before retry.
11. Function-action forms render without explicit method/action conflicts.
12. Direct attempts to escape the effective tenant boundary remain forbidden server-side.

## Non-goals

This integration does not define:

```text
consumer-specific routes
consumer-specific visual design
a local RBAC implementation
implicit permission grants from organization membership
cross-request transaction semantics
unbounded bulk assignment
browser-authoritative membership eligibility
```
