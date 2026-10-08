import "server-only";

import {
  GenericIdentityClientError,
  type IdentityAdministrationContext,
  type IdentityTenantAdministrationContext,
} from "@generic-identity/auth";
import type {
  IdentityEffectiveAdministrationContext,
  IdentityGroupMemberRecord,
  IdentityGroupRecord,
  IdentityGroupTemplateResourceScopeRequirement,
  IdentityManagedGroupPolicyBindingRecord,
  IdentityResourceScopeRecord,
} from "@generic-identity/contracts";
import { createNextTenantAdministrationContext } from "./administration";
import { isAllowedOnServer } from "./authorization";
import { NextIdentityServerSession } from "./session";

const TENANT_PAGE_SIZE = 200;
const MAXIMUM_AGGREGATE_TENANTS = 1000;
const TENANT_FANOUT_CONCURRENCY = 8;
const GROUP_LIMIT = 50;
const TEMPLATE_LIMIT = 100;
const RESOURCE_SCOPE_LIMIT = 200;

export interface NextGroupWorkspaceQuery {
  readonly tenantId?: string;
  readonly tenantView?: string;
  readonly groupId?: string;
  readonly defaultTenantId?: string;
}

export interface NextGroupWorkspaceTenant {
  readonly tenantId: string;
  readonly displayName: string;
}

export interface NextGroupAggregateRecord {
  readonly tenant: NextGroupWorkspaceTenant;
  readonly group: IdentityGroupRecord;
}

export interface NextReusableGroupTemplate {
  readonly group: IdentityGroupRecord;
  readonly requirements: readonly IdentityGroupTemplateResourceScopeRequirement[];
}

export interface NextGroupMemberView {
  readonly record: IdentityGroupMemberRecord;
  readonly displayName?: string;
}

export interface NextManagedGroupPolicyBindingView {
  readonly record: IdentityManagedGroupPolicyBindingRecord;
  readonly policyDisplayName?: string;
  readonly resourceScopeDisplayName?: string;
}

export interface NextGroupWorkspacePermissions {
  readonly canReadGroups: boolean;
  readonly canWriteGroups: boolean;
  readonly canReadGroupMemberships: boolean;
  readonly canWriteGroupMemberships: boolean;
  readonly canReadPolicyBindings: boolean;
  readonly canWritePolicyBindings: boolean;
  readonly canReadManagedPolicies: boolean;
  readonly canReadResourceScopes: boolean;
  readonly canReadTenantMemberships: boolean;
  readonly canManageReusableDefinitions: boolean;
}

export interface NextGroupWorkspace {
  readonly scopeWide: boolean;
  readonly allTenants: boolean;
  readonly tenants: readonly NextGroupWorkspaceTenant[];
  readonly selectedTenantId?: string;
  readonly selectedTenant?: NextGroupWorkspaceTenant;
  /** Trusted server-only tenant context. */
  readonly tenantContext?: IdentityTenantAdministrationContext;
  readonly aggregateGroups: readonly NextGroupAggregateRecord[];
  readonly groups: readonly IdentityGroupRecord[];
  readonly selectedGroup: IdentityGroupRecord | null;
  readonly members: readonly NextGroupMemberView[];
  readonly managedPolicyBindings: readonly NextManagedGroupPolicyBindingView[];
  readonly reusableTemplates: readonly NextReusableGroupTemplate[];
  readonly targetResourceScopes: readonly IdentityResourceScopeRecord[];
  readonly permissions: NextGroupWorkspacePermissions;
  readonly groupsTruncated: boolean;
}

const noPermissions: NextGroupWorkspacePermissions = {
  canReadGroups: false,
  canWriteGroups: false,
  canReadGroupMemberships: false,
  canWriteGroupMemberships: false,
  canReadPolicyBindings: false,
  canWritePolicyBindings: false,
  canReadManagedPolicies: false,
  canReadResourceScopes: false,
  canReadTenantMemberships: false,
  canManageReusableDefinitions: false,
};

/**
 * Composes the Groups GOLDEN workspace. Requested tenant/group query values are
 * never authority: effective visibility and capability checks precede protected
 * reads, and aggregate mode remains read-only until one concrete tenant is open.
 */
export async function loadNextGroupWorkspace(
  session: NextIdentityServerSession,
  administration: IdentityAdministrationContext,
  effective: IdentityEffectiveAdministrationContext,
  query: NextGroupWorkspaceQuery = {},
): Promise<NextGroupWorkspace> {
  assertEffectiveContext(administration, effective);

  const scopeWide = effective.tenantVisibility === "scope-wide";
  const allTenants = query.tenantView === "all";

  if (allTenants) {
    const tenants = await listAuthorizedTenantTargets(session, administration, effective);
    const aggregateGroups = await loadAggregateGroups(session, administration, tenants);
    return {
      scopeWide,
      allTenants: true,
      tenants,
      aggregateGroups,
      groups: [],
      selectedGroup: null,
      members: [],
      managedPolicyBindings: [],
      reusableTemplates: [],
      targetResourceScopes: [],
      permissions: noPermissions,
      groupsTruncated: false,
    };
  }

  const requestedTenantId = query.tenantId?.trim().toLowerCase()
    || query.defaultTenantId?.trim().toLowerCase();
  const tenantResolution = await resolveTenant(
    session,
    administration,
    effective,
    requestedTenantId,
  );

  const empty: NextGroupWorkspace = {
    scopeWide,
    allTenants: false,
    tenants: tenantResolution.tenants,
    ...(tenantResolution.selectedTenantId === undefined ? {} : { selectedTenantId: tenantResolution.selectedTenantId }),
    ...(tenantResolution.selectedTenant === undefined ? {} : { selectedTenant: tenantResolution.selectedTenant }),
    aggregateGroups: [],
    groups: [],
    selectedGroup: null,
    members: [],
    managedPolicyBindings: [],
    reusableTemplates: [],
    targetResourceScopes: [],
    permissions: noPermissions,
    groupsTruncated: false,
  };

  if (!tenantResolution.selectedTenantId || !tenantResolution.selectedTenant) return empty;

  const tenantContext = createNextTenantAdministrationContext(
    session,
    administration.identityScopeId,
    administration.applicationKey,
    tenantResolution.selectedTenantId,
  );
  const tenantBoundary = {
    identityScopeId: administration.identityScopeId,
    applicationKey: administration.applicationKey,
    tenantId: tenantResolution.selectedTenantId,
  };
  const tenantCapability = (feature: string, action: string) =>
    isAllowedOnServer(session, tenantBoundary, { resource: "identity-access", feature, action });
  const scopeCapability = (feature: string, action: string) =>
    isAllowedOnServer(
      session,
      { identityScopeId: administration.identityScopeId, applicationKey: administration.applicationKey },
      { resource: "identity-access", feature, action },
    );

  const [
    canReadGroups,
    canWriteGroups,
    canReadGroupMemberships,
    canWriteGroupMemberships,
    canReadPolicyBindings,
    canWritePolicyBindings,
    canReadManagedPolicies,
    canReadResourceScopes,
    canReadTenantMemberships,
    canManageReusableDefinitions,
  ] = await Promise.all([
    tenantCapability("group", "read"),
    tenantCapability("group", "write"),
    tenantCapability("group-membership", "read"),
    tenantCapability("group-membership", "write"),
    tenantCapability("policy-binding", "read"),
    tenantCapability("policy-binding", "write"),
    tenantCapability("policy", "read"),
    tenantCapability("resource-scope", "read"),
    tenantCapability("tenant-membership", "read"),
    scopeWide ? scopeCapability("group", "write") : Promise.resolve(false),
  ]);
  const permissions: NextGroupWorkspacePermissions = {
    canReadGroups,
    canWriteGroups,
    canReadGroupMemberships,
    canWriteGroupMemberships,
    canReadPolicyBindings,
    canWritePolicyBindings,
    canReadManagedPolicies,
    canReadResourceScopes,
    canReadTenantMemberships,
    canManageReusableDefinitions,
  };

  if (!canReadGroups) {
    if (query.groupId?.trim()) throw new GenericIdentityClientError("forbidden", 403);
    return { ...empty, tenantContext, permissions };
  }

  const [groups, reusableGroups, targetResourceScopes] = await Promise.all([
    session.client.accessControl.groups.list(tenantContext, { limit: GROUP_LIMIT }),
    session.client.accessControl.groups.listTemplates(tenantContext, { limit: TEMPLATE_LIMIT }),
    canReadResourceScopes
      ? session.client.accessControl.resourceScopes.list(tenantContext, { limit: RESOURCE_SCOPE_LIMIT })
      : Promise.resolve([] as readonly IdentityResourceScopeRecord[]),
  ]);

  const reusableTemplates = await Promise.all(reusableGroups.map(async (group) => ({
    group,
    requirements: await session.client.accessControl.groups.listTemplateScopeRequirements(
      tenantContext,
      group.tenantId,
      group.groupId,
    ),
  })));

  const requestedGroupId = query.groupId?.trim().toLowerCase();
  const selectedGroup = requestedGroupId
    ? groups.find((group) => group.groupId === requestedGroupId)
      ?? await session.client.accessControl.groups.get(tenantContext, requestedGroupId)
    : null;
  if (requestedGroupId && !selectedGroup) throw new GenericIdentityClientError("forbidden", 403);

  const [memberRecords, bindingRecords] = selectedGroup
    ? await Promise.all([
        canReadGroupMemberships
          ? session.client.accessControl.groups.listMembers(tenantContext, selectedGroup.groupId)
          : Promise.resolve([] as readonly IdentityGroupMemberRecord[]),
        canReadPolicyBindings
          ? session.client.accessControl.managedPolicyBindings.list(tenantContext, selectedGroup.groupId)
          : Promise.resolve([] as readonly IdentityManagedGroupPolicyBindingRecord[]),
      ])
    : [[], []] as const;

  const members: NextGroupMemberView[] = await Promise.all(memberRecords.map(async (record) => {
    if (!canReadTenantMemberships) return { record };
    const matches = await session.client.directory.tenantUsers.list(tenantContext, {
      search: record.tenantMembershipId,
      limit: 20,
    });
    const match = matches.find((candidate) => candidate.membershipId === record.tenantMembershipId);
    return { record, ...(match ? { displayName: match.displayName } : {}) };
  }));

  const managedPolicyBindings: NextManagedGroupPolicyBindingView[] = await Promise.all(bindingRecords.map(async (record) => {
    const [policy, scope] = await Promise.all([
      canReadManagedPolicies
        ? findManagedPolicy(session, tenantContext, record.policyId)
        : Promise.resolve(undefined),
      canReadResourceScopes && record.resourceScopeId
        ? session.client.accessControl.resourceScopes.get(tenantContext, record.resourceScopeId)
        : Promise.resolve(null),
    ]);
    return {
      record,
      ...(policy ? { policyDisplayName: policy.displayName } : {}),
      ...(scope ? { resourceScopeDisplayName: scope.displayName } : {}),
    };
  }));

  return {
    scopeWide,
    allTenants: false,
    tenants: tenantResolution.tenants,
    selectedTenantId: tenantResolution.selectedTenantId,
    selectedTenant: tenantResolution.selectedTenant,
    tenantContext,
    aggregateGroups: [],
    groups,
    selectedGroup,
    members,
    managedPolicyBindings,
    reusableTemplates,
    targetResourceScopes,
    permissions,
    groupsTruncated: groups.length >= GROUP_LIMIT,
  };
}

async function findManagedPolicy(
  session: NextIdentityServerSession,
  context: IdentityTenantAdministrationContext,
  policyId: string,
) {
  const matches = await session.client.accessControl.managedPolicyBindings.listAvailablePolicies(
    context,
    { search: policyId, limit: 20 },
  );
  return matches.find((policy) => policy.policyId === policyId);
}

async function resolveTenant(
  session: NextIdentityServerSession,
  administration: IdentityAdministrationContext,
  effective: IdentityEffectiveAdministrationContext,
  requestedTenantId?: string,
): Promise<{
  readonly tenants: readonly NextGroupWorkspaceTenant[];
  readonly selectedTenantId?: string;
  readonly selectedTenant?: NextGroupWorkspaceTenant;
}> {
  if (effective.tenantVisibility === "scope-wide") {
    if (!requestedTenantId) return { tenants: [] };
    const record = await session.client.directory.tenants.get(administration, requestedTenantId);
    if (!record) throw new GenericIdentityClientError("forbidden", 403);
    const selectedTenant = { tenantId: record.tenantId, displayName: record.displayName };
    return { tenants: [selectedTenant], selectedTenantId: record.tenantId, selectedTenant };
  }

  const tenants = [...new Map(effective.activeTenantMemberships.map((membership) => [
    membership.tenantId,
    { tenantId: membership.tenantId, displayName: membership.tenantId },
  ])).values()].sort((left, right) => left.tenantId.localeCompare(right.tenantId));
  const selectedTenantId = requestedTenantId || (tenants.length === 1 ? tenants[0]?.tenantId : undefined);
  const selectedTenant = selectedTenantId
    ? tenants.find((tenant) => tenant.tenantId === selectedTenantId)
    : undefined;
  if (selectedTenantId && !selectedTenant) throw new GenericIdentityClientError("forbidden", 403);
  return {
    tenants,
    ...(selectedTenantId === undefined ? {} : { selectedTenantId }),
    ...(selectedTenant === undefined ? {} : { selectedTenant }),
  };
}

async function listAuthorizedTenantTargets(
  session: NextIdentityServerSession,
  administration: IdentityAdministrationContext,
  effective: IdentityEffectiveAdministrationContext,
): Promise<readonly NextGroupWorkspaceTenant[]> {
  if (effective.tenantVisibility === "membership-limited") {
    return [...new Map(effective.activeTenantMemberships.map((membership) => [
      membership.tenantId,
      { tenantId: membership.tenantId, displayName: membership.tenantId },
    ])).values()].sort((left, right) => left.tenantId.localeCompare(right.tenantId));
  }

  const targets: NextGroupWorkspaceTenant[] = [];
  for (let offset = 0; offset < MAXIMUM_AGGREGATE_TENANTS; offset += TENANT_PAGE_SIZE) {
    const tenants = await session.client.directory.tenants.list(administration, { offset, limit: TENANT_PAGE_SIZE });
    targets.push(...tenants.map((tenant) => ({ tenantId: tenant.tenantId, displayName: tenant.displayName })));
    if (tenants.length < TENANT_PAGE_SIZE) return targets.sort(compareTenant);
  }
  const overflow = await session.client.directory.tenants.list(administration, {
    offset: MAXIMUM_AGGREGATE_TENANTS,
    limit: 1,
  });
  if (overflow.length > 0) {
    throw new Error("The aggregate tenant view exceeds the supported administration UI boundary. Select a tenant explicitly.");
  }
  return targets.sort(compareTenant);
}

async function loadAggregateGroups(
  session: NextIdentityServerSession,
  administration: IdentityAdministrationContext,
  tenants: readonly NextGroupWorkspaceTenant[],
): Promise<readonly NextGroupAggregateRecord[]> {
  const result: NextGroupAggregateRecord[] = [];
  for (let offset = 0; offset < tenants.length; offset += TENANT_FANOUT_CONCURRENCY) {
    const slice = tenants.slice(offset, offset + TENANT_FANOUT_CONCURRENCY);
    const batches = await Promise.all(slice.map(async (tenant) => {
      const context = createNextTenantAdministrationContext(
        session,
        administration.identityScopeId,
        administration.applicationKey,
        tenant.tenantId,
      );
      try {
        const groups = await session.client.accessControl.groups.list(context, { limit: GROUP_LIMIT });
        return groups.map((group) => ({ tenant, group }));
      } catch (error) {
        if (error instanceof GenericIdentityClientError && error.code === "forbidden") return [];
        throw error;
      }
    }));
    result.push(...batches.flat());
  }
  return result;
}

function assertEffectiveContext(
  administration: IdentityAdministrationContext,
  effective: IdentityEffectiveAdministrationContext,
): void {
  if (
    administration.identityScopeId !== effective.identityScopeId
    || administration.applicationKey !== effective.applicationKey
  ) throw new GenericIdentityClientError("configuration");
}

function compareTenant(left: NextGroupWorkspaceTenant, right: NextGroupWorkspaceTenant): number {
  return left.displayName.localeCompare(right.displayName) || left.tenantId.localeCompare(right.tenantId);
}
