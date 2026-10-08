import "server-only";

import type { IdentityAdministrationContext, IdentityTenantAdministrationContext } from "@generic-identity/auth";
import type {
  IdentityGroupRecord,
  IdentityManagedGroupPolicyBindingRecord,
  IdentityManagedPolicyRecord,
  IdentityManagedPolicyStatementRecord,
  IdentityManagedPolicyVersionRecord,
  IdentityResourceScopeRecord,
  IdentityTenantMembershipRecord,
  IdentityUserRecord,
} from "@generic-identity/contracts";
import type {
  IdentityUserAccessBinding,
  IdentityUserAccessGroup,
  IdentityUserAccessInsight,
} from "@generic-identity/contracts";
import { NextIdentityServerSession } from "./session";

const GROUP_SCAN_LIMIT = 100;

/**
 * Composes user→tenant membership→groups→managed policy bindings→statements,
 * mirroring the GOLDEN Identity Admin diagnostic. This never performs RBAC
 * evaluation, nor does it infer allow/deny from presence of assignments.
 *
 * The tenant context must be validated against effective visibility by the
 * caller. Individual SDK operations also enforce their own .NET RBAC checks.
 */
export async function loadNextUserAccessInsight(
  session: NextIdentityServerSession,
  administration: IdentityAdministrationContext,
  tenant: IdentityTenantAdministrationContext,
  user: IdentityUserRecord,
): Promise<IdentityUserAccessInsight> {
  if (tenant.identityScopeId !== administration.identityScopeId ||
      tenant.applicationKey !== administration.applicationKey) {
    throw new Error("A user access diagnostic cannot cross administration boundaries.");
  }

  const membership = await session.client.directory.memberships.findByUser(tenant, user.userId);
  if (membership === null) return emptyInsight(null);

  const groups = await session.client.accessControl.groups.list(tenant, { limit: GROUP_SCAN_LIMIT });
  const assignments = await Promise.all(groups.map(async (group) => ({
    group,
    members: await session.client.accessControl.groups.listMembers(tenant, group.groupId),
  })));
  const assignedGroups = assignments
    .filter(({ members }) => members.some((member) => member.tenantMembershipId === membership.membershipId))
    .map(({ group }) => group);

  if (assignedGroups.length === 0) return {
    ...emptyInsight(membership),
    scannedGroupCount: groups.length,
    scanBoundReached: groups.length === GROUP_SCAN_LIMIT,
  };

  const bindingsByGroup = await Promise.all(assignedGroups.map(async (group) => ({
    groupId: group.groupId,
    bindings: await session.client.accessControl.managedPolicyBindings.list(tenant, group.groupId),
  })));
  const allBindings = bindingsByGroup.flatMap((entry) => entry.bindings);
  const policyIds = [...new Set(allBindings.map((binding) => binding.policyId))];
  const policyVersions = [...new Map(allBindings.map((binding) => [
    versionKey(binding.policyId, binding.policyVersion),
    { policyId: binding.policyId, policyVersion: binding.policyVersion },
  ] as const)).values()];
  const scopeIds = [...new Set(allBindings.flatMap((binding) =>
    binding.resourceScopeId === undefined ? [] : [binding.resourceScopeId]))];

  const [policies, versions, scopes, statements] = await Promise.all([
    Promise.all(policyIds.map(async (policyId) =>
      session.client.accessControl.managedPolicies.get(administration, policyId))),
    Promise.all(policyVersions.map(async ({ policyId, policyVersion }) => ({
      policyId, policyVersion,
      version: await session.client.accessControl.managedPolicies.getVersion(
        administration, policyId, policyVersion,
      ),
    }))),
    Promise.all(scopeIds.map(async (scopeId) =>
      session.client.accessControl.resourceScopes.get(tenant, scopeId))),
    Promise.all(policyVersions.map(async ({ policyId, policyVersion }) => ({
      policyId, policyVersion,
      statements: await session.client.accessControl.managedPolicies.listStatements(
        administration, policyId, policyVersion,
      ),
    }))),
  ]);

  const policyMap = new Map<string, IdentityManagedPolicyRecord>(policies.flatMap((p) =>
    p === null ? [] : [[p.policyId, p] as const]));
  const versionMap = new Map<string, IdentityManagedPolicyVersionRecord>(versions.flatMap((item) =>
    item.version === null ? [] : [[versionKey(item.policyId, item.policyVersion), item.version] as const]));
  const scopeMap = new Map<string, IdentityResourceScopeRecord>(scopes.flatMap((scope) =>
    scope === null ? [] : [[scope.resourceScopeId, scope] as const]));
  const statementMap = new Map<string, readonly IdentityManagedPolicyStatementRecord[]>(
    statements.map((item) => [versionKey(item.policyId, item.policyVersion), item.statements]),
  );
  const bindingMap = new Map<string, readonly IdentityManagedGroupPolicyBindingRecord[]>(
    bindingsByGroup.map((item) => [item.groupId, item.bindings]),
  );

  let assignmentCount = 0;
  let statementCount = 0;
  let lifecycleReadyAssignmentCount = 0;

  const projectedGroups: IdentityUserAccessGroup[] = assignedGroups.map((group) => {
    const projectedBindings = (bindingMap.get(group.groupId) ?? []).map((binding): IdentityUserAccessBinding => {
      const policy = policyMap.get(binding.policyId);
      const key = versionKey(binding.policyId, binding.policyVersion);
      const version = versionMap.get(key);
      const scope = binding.resourceScopeId === undefined ? undefined : scopeMap.get(binding.resourceScopeId);
      const blockers = lifecycleBlockers(user, membership, group, policy, version, scope, binding);
      const policyStatements = statementMap.get(key) ?? [];
      assignmentCount++;
      statementCount += policyStatements.length;
      if (blockers.length === 0) lifecycleReadyAssignmentCount++;
      return {
        policyId: binding.policyId,
        policyVersion: binding.policyVersion,
        policyName: policy?.displayName ?? binding.policyId,
        policyStatus: policy?.status,
        policyVersionPublishedAt: version?.publishedAt,
        resourceScopeId: binding.resourceScopeId,
        resourceScopeName: scope?.displayName,
        resourceScopeStatus: scope?.status,
        includeDescendants: binding.includeDescendants,
        lifecycleReady: blockers.length === 0,
        blockers,
        statements: policyStatements.map((statement) => ({
          statementId: statement.statementId,
          modelVersion: statement.modelVersion,
          resource: statement.resource,
          feature: statement.feature,
          action: statement.action,
        })),
      };
    });
    return {
      groupId: group.groupId,
      groupName: group.displayName,
      groupStatus: group.status,
      lifecycleReady: user.status === 1 && membership.status === 1 && group.status === 1,
      bindings: projectedBindings,
    };
  });

  return {
    membership, groups: projectedGroups,
    scannedGroupCount: groups.length,
    scanBoundReached: groups.length === GROUP_SCAN_LIMIT,
    assignmentCount, statementCount, lifecycleReadyAssignmentCount,
  };
}

function emptyInsight(membership: IdentityTenantMembershipRecord | null): IdentityUserAccessInsight {
  return {
    membership, groups: [], scannedGroupCount: 0, scanBoundReached: false,
    assignmentCount: 0, statementCount: 0, lifecycleReadyAssignmentCount: 0,
  };
}

function lifecycleBlockers(
  user: IdentityUserRecord,
  membership: IdentityTenantMembershipRecord,
  group: IdentityGroupRecord,
  policy: IdentityManagedPolicyRecord | undefined,
  version: IdentityManagedPolicyVersionRecord | undefined,
  scope: IdentityResourceScopeRecord | undefined,
  binding: IdentityManagedGroupPolicyBindingRecord,
): readonly string[] {
  const blockers: string[] = [];
  if (user.status !== 1) blockers.push("user inactive");
  if (membership.status !== 1) blockers.push("tenant membership inactive");
  if (group.status !== 1) blockers.push("group inactive");
  if (policy === undefined) blockers.push("managed policy record unavailable");
  else if (policy.status !== 1) blockers.push("managed policy inactive");
  if (version === undefined) blockers.push("managed policy version unavailable");
  else if (version.publishedAt === undefined) blockers.push("managed policy version unpublished");
  if (binding.resourceScopeId !== undefined) {
    if (scope === undefined) blockers.push("resource scope record unavailable");
    else if (scope.status !== 1) blockers.push("resource scope inactive");
  }
  return blockers;
}

function versionKey(policyId: string, policyVersion: number): string {
  return `${policyId}:v${policyVersion}`;
}
