import "server-only";

import {
  GenericIdentityClientError,
  type IdentityAdministrationContext,
  type IdentityTenantAdministrationContext,
} from "@generic-identity/auth";
import type {
  IdentityEffectiveAdministrationContext,
  IdentityOrganizationMembershipRecord,
  IdentityOrganizationRecord,
  IdentityOrganizationResourceScopeLinkRecord,
} from "@generic-identity/contracts";
import { createNextTenantAdministrationContext } from "./administration";
import { isAllowedOnServer } from "./authorization";
import { NextIdentityServerSession } from "./session";

const ORGANIZATION_LIMIT = 200;
const MEMBERSHIP_LIMIT = 200;

export interface NextOrganizationWorkspaceQuery {
  readonly tenantId?: string;
  readonly organizationId?: string;
  readonly defaultTenantId?: string;
}

export interface NextOrganizationWorkspaceTenant {
  readonly tenantId: string;
  readonly displayName: string;
  readonly status?: 1 | 2;
  readonly version?: number;
}

export interface NextOrganizationWorkspacePermissions {
  readonly canReadOrganizations: boolean;
  readonly canWriteOrganizations: boolean;
  readonly canReadTenantMemberships: boolean;
  readonly canReadOrganizationMemberships: boolean;
  readonly canWriteOrganizationMemberships: boolean;
  readonly canReadOrganizationScopeLinks: boolean;
  readonly canWriteOrganizationScopeLinks: boolean;
  readonly canReadResourceScopes: boolean;
}

export interface NextOrganizationWorkspace {
  readonly scopeWide: boolean;
  readonly tenants: readonly NextOrganizationWorkspaceTenant[];
  readonly selectedTenantId?: string;
  /** Trusted server-only tenant context. */
  readonly tenantContext?: IdentityTenantAdministrationContext;
  readonly selectedTenant?: NextOrganizationWorkspaceTenant;
  readonly organizations: readonly IdentityOrganizationRecord[];
  readonly selectedOrganization: IdentityOrganizationRecord | null;
  readonly children: readonly IdentityOrganizationRecord[];
  readonly memberships: readonly IdentityOrganizationMembershipRecord[];
  readonly resourceScopeLink: IdentityOrganizationResourceScopeLinkRecord | null;
  readonly permissions: NextOrganizationWorkspacePermissions;
  readonly organizationsTruncated: boolean;
  readonly membershipsTruncated: boolean;
}

const noPermissions: NextOrganizationWorkspacePermissions = {
  canReadOrganizations: false,
  canWriteOrganizations: false,
  canReadTenantMemberships: false,
  canReadOrganizationMemberships: false,
  canWriteOrganizationMemberships: false,
  canReadOrganizationScopeLinks: false,
  canWriteOrganizationScopeLinks: false,
  canReadResourceScopes: false,
};

/**
 * Composes the Organization Directory GOLDEN workspace. Tenant query values are
 * requested context only; effective visibility and every capability are checked
 * before protected collections are fetched.
 */
export async function loadNextOrganizationWorkspace(
  session: NextIdentityServerSession,
  administration: IdentityAdministrationContext,
  effective: IdentityEffectiveAdministrationContext,
  query: NextOrganizationWorkspaceQuery = {},
): Promise<NextOrganizationWorkspace> {
  if (
    effective.identityScopeId !== administration.identityScopeId
    || effective.applicationKey !== administration.applicationKey
  ) throw new GenericIdentityClientError("configuration");

  const scopeWide = effective.tenantVisibility === "scope-wide";
  const requestedTenantId = query.tenantId?.trim().toLowerCase()
    || query.defaultTenantId?.trim().toLowerCase();

  let tenants: readonly NextOrganizationWorkspaceTenant[];
  let selectedTenantId: string | undefined;
  let selectedTenant: NextOrganizationWorkspaceTenant | undefined;

  if (scopeWide) {
    selectedTenantId = requestedTenantId || undefined;
    if (selectedTenantId) {
      const record = await session.client.directory.tenants.get(administration, selectedTenantId);
      if (!record) throw new GenericIdentityClientError("forbidden", 403);
      selectedTenant = {
        tenantId: record.tenantId,
        displayName: record.displayName,
        status: record.status,
        version: record.version,
      };
      tenants = [selectedTenant];
    } else tenants = [];
  } else {
    tenants = effective.activeTenantMemberships.map((membership) => ({
      tenantId: membership.tenantId,
      displayName: membership.tenantId,
    }));
    selectedTenantId = requestedTenantId || (tenants.length === 1 ? tenants[0]?.tenantId : undefined);
    selectedTenant = selectedTenantId
      ? tenants.find((candidate) => candidate.tenantId === selectedTenantId)
      : undefined;
    if (selectedTenantId && !selectedTenant) throw new GenericIdentityClientError("forbidden", 403);
  }

  const empty = {
    scopeWide,
    tenants,
    organizations: [] as readonly IdentityOrganizationRecord[],
    selectedOrganization: null,
    children: [] as readonly IdentityOrganizationRecord[],
    memberships: [] as readonly IdentityOrganizationMembershipRecord[],
    resourceScopeLink: null,
    permissions: noPermissions,
    organizationsTruncated: false,
    membershipsTruncated: false,
  };
  if (!selectedTenantId || !selectedTenant) return empty;

  const tenantContext = createNextTenantAdministrationContext(
    session,
    administration.identityScopeId,
    administration.applicationKey,
    selectedTenantId,
  );
  const boundary = {
    identityScopeId: administration.identityScopeId,
    applicationKey: administration.applicationKey,
    tenantId: selectedTenantId,
  };
  const capability = (feature: string, action: string) =>
    isAllowedOnServer(session, boundary, { resource: "identity-access", feature, action });

  const [
    canReadOrganizations,
    canWriteOrganizations,
    canReadTenantMemberships,
    canReadOrganizationMemberships,
    canWriteOrganizationMemberships,
    canReadOrganizationScopeLinks,
    canWriteOrganizationScopeLinks,
    canReadResourceScopes,
  ] = await Promise.all([
    capability("organization", "read"),
    capability("organization", "write"),
    capability("tenant-membership", "read"),
    capability("organization-membership", "read"),
    capability("organization-membership", "write"),
    capability("organization-scope-link", "read"),
    capability("organization-scope-link", "write"),
    capability("resource-scope", "read"),
  ]);
  const permissions: NextOrganizationWorkspacePermissions = {
    canReadOrganizations,
    canWriteOrganizations,
    canReadTenantMemberships,
    canReadOrganizationMemberships,
    canWriteOrganizationMemberships,
    canReadOrganizationScopeLinks,
    canWriteOrganizationScopeLinks,
    canReadResourceScopes,
  };

  if (!canReadOrganizations) {
    return { ...empty, selectedTenantId, selectedTenant, tenantContext, permissions };
  }

  const organizations = await session.client.organizations.organizations.list(
    tenantContext,
    { limit: ORGANIZATION_LIMIT },
  );
  const requestedOrganizationId = query.organizationId?.trim().toLowerCase();
  const selectedOrganization = requestedOrganizationId
    ? organizations.find((organization) => organization.organizationId === requestedOrganizationId) ?? null
    : null;
  if (requestedOrganizationId && !selectedOrganization) {
    throw new GenericIdentityClientError("forbidden", 403);
  }

  const [memberships, resourceScopeLink] = selectedOrganization
    ? await Promise.all([
        canReadOrganizationMemberships
          ? session.client.organizations.memberships.listForOrganization(
              tenantContext,
              selectedOrganization.organizationId,
              { limit: MEMBERSHIP_LIMIT },
            )
          : Promise.resolve([] as readonly IdentityOrganizationMembershipRecord[]),
        canReadOrganizationScopeLinks
          ? session.client.organizations.resourceScopeLinks.get(
              tenantContext,
              selectedOrganization.organizationId,
            )
          : Promise.resolve(null),
      ])
    : [[], null] as const;

  const children = selectedOrganization
    ? organizations.filter((organization) => organization.parentOrganizationId === selectedOrganization.organizationId)
    : [];

  return {
    scopeWide,
    tenants,
    selectedTenantId,
    tenantContext,
    selectedTenant,
    organizations,
    selectedOrganization,
    children,
    memberships,
    resourceScopeLink,
    permissions,
    organizationsTruncated: organizations.length >= ORGANIZATION_LIMIT,
    membershipsTruncated: memberships.length >= MEMBERSHIP_LIMIT,
  };
}
