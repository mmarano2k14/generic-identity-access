# Generic Identity — Membership Administration

**Current package family:** `1.5.0`  
**Status:** reusable administration workflow qualified

## Scope

Membership administration composes tenant membership lifecycle, candidate lookup, tenant group assignments, and Organization assignments without changing the authorization meaning of any relation.

Supported behavior includes:

```text
tenant membership overview
membership candidate lookup by login
membership creation
membership update with concurrency
group assignment reconciliation
Organization assignment reconciliation
server-backed tenant and relation selection
```

## Security and authorization rules

- tenant context is validated against the effective administration context;
- scope-wide and membership-limited administrators follow different server-authorized paths;
- candidate eligibility is revalidated before mutation;
- missing read capability is not interpreted as an empty collection;
- inactive groups are never silently introduced during reconciliation;
- Organization membership expresses belonging and does not grant authorization;
- each underlying HTTP mutation remains independently authoritative.

Bulk reconciliation can be partially applied if a later request fails. Consumers must reload authoritative state before retrying rather than assuming a cross-request transaction.

## Reusable components

```text
loadNextMembershipWorkspace
createNextTenantMembershipFromForm
findNextTenantMembershipCandidateFromForm
updateNextTenantMembershipFromForm
replaceNextTenantMemberGroupsFromForm
replaceNextTenantMemberOrganizationsFromForm
MembershipsPage
MembershipForm
MembershipCandidateLookupPanel
MemberGroupAssignmentsForm
MemberOrganizationAssignmentsForm
```
