# Shared Identity Organizations - Organizations SDK/UI

## Objective

Promote the already-proven Generic Organization Directory backend and legacy TypeScript client into the categorized public Generic Identity SDK and shared UI without moving ownership into application-specific semantics.

## Boundary

This delivery covers only generic organization identity and belonging:

```text
Organization
Organization hierarchy
OrganizationMembership
Organization <-> ResourceScope link
```

It does not include OrganisationProfile, business domains, providers, business rules, or consumer-specific semantics.

## Public SDK

```text
GenericIdentityClient.organizations
  organizations
    list
    get
    tree
    children
    create
    update
    enable
    disable

  memberships
    listForOrganization
    listForTenantMembership
    get
    add
    activate
    suspend
    remove

  resourceScopeLinks
    get
    create
    update
    remove
```

## Shared UI

```text
OrganizationsPage
OrganizationDetailsPage
OrganizationTreePage
OrganizationMembershipsPage
OrganizationForm
OrganizationMembershipForm
OrganizationResourceScopeLinkForm
```

## Public subpaths

```text
@generic-identity/contracts/organizations
@generic-identity/auth/organizations
@generic-identity/react/organizations
@generic-identity/next/organizations
```

## Compatibility

The existing `authentication`, `authorization`, `administration`, `account`, and `directory` surfaces remain unchanged. The Organization category is additive.

## Version

The four Generic Identity public packages advance together to `1.2.0`.

## Non-goals

- no backend endpoint redesign;
- no database migration;
- no Organization authorization semantics rewrite;
- no OrganisationProfile promotion into Generic Identity;
- no consumer-specific route or naming policy.
