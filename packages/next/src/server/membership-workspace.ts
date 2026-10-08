import "server-only";

import {
  GenericIdentityClientError,
  type IdentityAdministrationContext,
  type IdentityTenantAdministrationContext,
} from "@generic-identity/auth";
import type {
  IdentityEffectiveAdministrationContext,
  IdentityGroupRecord,
  IdentityOrganizationMembershipRecord,
  IdentityOrganizationRecord,
  IdentityTenantGroupAssignmentRecord,
  IdentityTenantMembershipRecord,
  IdentityTenantUserRecord,
} from "@generic-identity/contracts";
import { isAllowedOnServer } from "./authorization";
import { createNextTenantAdministrationContext } from "./administration";
import { NextIdentityServerSession } from "./session";

const MEMBER_LIMIT = 200;
const GROUP_LIMIT = 100;
const ORGANIZATION_LIMIT = 200;

export interface NextMembershipWorkspaceQuery {
  readonly tenantId?: string;
  readonly membershipId?: string;
  readonly defaultTenantId?: string;
}

export interface NextMembershipWorkspaceTenant {
  readonly tenantId: string;
  readonly displayName: string;
  readonly status?: 1 | 2;
  readonly version?: number;
}

export interface NextMembershipWorkspacePermissions {
  readonly canReadMembers: boolean;
  readonly canWriteMembers: boolean;
  readonly canReadGroups: boolean;
  readonly canReadGroupAssignments: boolean;
  readonly canWriteGroupAssignments: boolean;
  readonly canReadOrganizations: boolean;
  readonly canReadOrganizationMemberships: boolean;
  readonly canWriteOrganizationMemberships: boolean;
}

export interface NextSelectedTenantMember {
  readonly membershipId: string;
  readonly userId: string;
  readonly displayName: string;
  readonly version: number;
  readonly status: 1 | 2;
}

export interface NextMembershipWorkspace {
  readonly scopeWide: boolean;
  readonly tenants: readonly NextMembershipWorkspaceTenant[];
  readonly selectedTenantId?: string;
  /** Internal trusted context. Never forward to Client Components. */
  readonly tenantContext?: IdentityTenantAdministrationContext;
  readonly selectedTenant?: NextMembershipWorkspaceTenant;
  readonly members: readonly IdentityTenantUserRecord[];
  readonly ownMembership: IdentityTenantMembershipRecord | null;
  readonly selectedMember: NextSelectedTenantMember | null;
  readonly groups: readonly IdentityGroupRecord[];
  readonly assignments: readonly IdentityTenantGroupAssignmentRecord[];
  readonly organizations: readonly IdentityOrganizationRecord[];
  readonly memberOrganizations: readonly IdentityOrganizationMembershipRecord[];
  readonly permissions: NextMembershipWorkspacePermissions;
  readonly membersTruncated: boolean;
  readonly groupsTruncated: boolean;
  readonly organizationsTruncated: boolean;
}

const noPermissions: NextMembershipWorkspacePermissions = {
  canReadMembers: false,
  canWriteMembers: false,
  canReadGroups: false,
  canReadGroupAssignments: false,
  canWriteGroupAssignments: false,
  canReadOrganizations: false,
  canReadOrganizationMemberships: false,
  canWriteOrganizationMemberships: false,
};

/**
 * Composes the GOLDEN Memberships workspace without granting access from a
 * route/query parameter. Authorization runs on the .NET boundary and every
 * tenant-facing read is gated independently.
 */
export async function loadNextMembershipWorkspace(
  session: NextIdentityServerSession,
  administration: IdentityAdministrationContext,
  effective: IdentityEffectiveAdministrationContext,
  query: NextMembershipWorkspaceQuery = {},
): Promise<NextMembershipWorkspace> {
  if (
    effective.identityScopeId !== administration.identityScopeId ||
    effective.applicationKey !== administration.applicationKey
  ) throw new GenericIdentityClientError("configuration");

  const scopeWide = effective.tenantVisibility === "scope-wide";
  const request = query.tenantId?.trim().toLowerCase() || query.defaultTenantId?.trim().toLowerCase();

  let tenants: readonly NextMembershipWorkspaceTenant[];
  let selectedTenantId: string | undefined;
  let selectedTenant: NextMembershipWorkspaceTenant | undefined;

  if (scopeWide) {
    // Scope-wide consumers use the shared server-backed tenant autocomplete.
    // Only the selected tenant is resolved here; no tenant catalog is preloaded.
    selectedTenantId = request || undefined;
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
    } else {
      tenants = [];
    }
  } else {
    tenants = effective.activeTenantMemberships.map((member) => ({
      tenantId: member.tenantId, displayName: member.tenantId,
    }));
    selectedTenantId = request || (tenants.length === 1 ? tenants[0]?.tenantId : undefined);
    selectedTenant = selectedTenantId
      ? tenants.find((candidate) => candidate.tenantId === selectedTenantId)
      : undefined;
    if (selectedTenantId && !selectedTenant) {
      throw new GenericIdentityClientError("forbidden", 403);
    }
  }

  const base = {
    scopeWide, tenants, members: [] as readonly IdentityTenantUserRecord[],
    ownMembership: null, selectedMember: null,
    groups: [] as readonly IdentityGroupRecord[],
    assignments: [] as readonly IdentityTenantGroupAssignmentRecord[],
    organizations: [] as readonly IdentityOrganizationRecord[],
    memberOrganizations: [] as readonly IdentityOrganizationMembershipRecord[],
    permissions: noPermissions,
    membersTruncated: false, groupsTruncated: false, organizationsTruncated: false,
  };
  if (!selectedTenantId || !selectedTenant) return base;

  const tenantContext = createNextTenantAdministrationContext(
    session, administration.identityScopeId, administration.applicationKey, selectedTenantId,
  );
  const boundary = {
    identityScopeId: administration.identityScopeId,
    applicationKey: administration.applicationKey,
    tenantId: selectedTenantId,
  };
  const capability = (feature: string, action: string) =>
    isAllowedOnServer(session, boundary, { resource: "identity-access", feature, action });

  const [
    canReadMembers, canWriteMembers, canReadGroups,
    canReadGroupAssignments, canWriteGroupAssignments,
    canReadOrganizations, canReadOrganizationMemberships, canWriteOrganizationMemberships,
  ] = await Promise.all([
    capability("tenant-membership", "read"),
    capability("tenant-membership", "write"),
    capability("group", "read"),
    capability("group-membership", "read"),
    capability("group-membership", "write"),
    capability("organization", "read"),
    capability("organization-membership", "read"),
    capability("organization-membership", "write"),
  ]);

  const permissions = {
    canReadMembers, canWriteMembers, canReadGroups,
    canReadGroupAssignments, canWriteGroupAssignments,
    canReadOrganizations, canReadOrganizationMemberships, canWriteOrganizationMemberships,
  };
  const [members, ownMembership, groups, assignments, organizations] = await Promise.all([
    canReadMembers
      ? session.client.directory.tenantUsers.list(tenantContext, { limit: MEMBER_LIMIT })
      : Promise.resolve([] as readonly IdentityTenantUserRecord[]),
    !scopeWide
      ? session.client.directory.memberships.findByUser(tenantContext, effective.userId)
      : Promise.resolve(null),
    canReadGroups
      ? session.client.accessControl.groups.list(tenantContext, { limit: GROUP_LIMIT })
      : Promise.resolve([] as readonly IdentityGroupRecord[]),
    canReadGroupAssignments
      ? session.client.directory.tenantGroupAssignments.list(tenantContext)
      : Promise.resolve([] as readonly IdentityTenantGroupAssignmentRecord[]),
    canReadOrganizations
      ? session.client.organizations.organizations.list(tenantContext, { limit: ORGANIZATION_LIMIT })
      : Promise.resolve([] as readonly IdentityOrganizationRecord[]),
  ]);

  const requestedMembership = query.membershipId?.trim();
  const selectedRecord = requestedMembership
    ? members.find((item) => item.membershipId === requestedMembership)
    : undefined;
  const selectedMember: NextSelectedTenantMember | null = selectedRecord
    ? {
      membershipId: selectedRecord.membershipId,
      userId: selectedRecord.userId,
      displayName: selectedRecord.displayName,
      version: selectedRecord.membershipVersion,
      status: selectedRecord.membershipStatus,
    }
    : requestedMembership && ownMembership?.membershipId === requestedMembership
      ? {
        membershipId: ownMembership.membershipId,
        userId: ownMembership.userId,
        displayName: "Current user",
        version: ownMembership.version,
        status: ownMembership.status,
      }
      : null;

  if (requestedMembership && selectedMember === null) {
    throw new GenericIdentityClientError("forbidden", 403);
  }

  const memberOrganizations = selectedMember && canReadOrganizations && canReadOrganizationMemberships
    ? await session.client.organizations.memberships.listForTenantMembership(
      tenantContext, selectedMember.membershipId, { limit: ORGANIZATION_LIMIT },
    )
    : [];

  return {
    scopeWide, tenants, selectedTenantId, selectedTenant, tenantContext,
    members, ownMembership, selectedMember, groups, assignments,
    organizations, memberOrganizations, permissions,
    membersTruncated: members.length >= MEMBER_LIMIT,
    groupsTruncated: groups.length >= GROUP_LIMIT,
    organizationsTruncated: organizations.length >= ORGANIZATION_LIMIT || memberOrganizations.length >= ORGANIZATION_LIMIT,
  };
}
