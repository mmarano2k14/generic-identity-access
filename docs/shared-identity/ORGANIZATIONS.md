# Generic Identity — Organizations

**Current package family:** `1.5.0`  
**Boundary:** generic organization identity and belonging only

## Scope

The categorized Organizations SDK covers:

```text
Organization lifecycle
Organization hierarchy
OrganizationMembership lifecycle
Organization <-> ResourceScope linkage
```

It does not absorb OrganisationProfile, business domains, providers, or consumer-specific semantics.

## Public SDK

```text
GenericIdentityClient.organizations
  organizations
    list / get / tree / children
    create / update / enable / disable

  memberships
    listForOrganization
    listForTenantMembership
    get / add / activate / suspend / remove

  resourceScopeLinks
    get / create / update / remove
```

## Reusable administration workflow

The Next.js integration composes tenant visibility, hierarchy, lifecycle, memberships, and ResourceScope linking through a reusable server workspace and mutation helpers.

Relation fields use server-backed autocomplete:

```text
tenant membership -> tenant-membership reference
resource scope     -> resource-scope reference
```

ResourceScope links preserve tenant isolation and backend optimistic concurrency. Organization membership remains organizational belonging and never becomes an authorization grant.

## Shared presentation

```text
OrganizationsPage
OrganizationDetailsPage
OrganizationTreePage
OrganizationMembershipsPage
OrganizationForm
OrganizationMembershipForm
OrganizationResourceScopeLinkForm
```

Routes, Server Actions, navigation, and consumer semantics remain application-owned.
