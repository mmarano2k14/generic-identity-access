import "server-only";
import type {
  IdentityGroupRecord,
  IdentityManagedGroupPolicyBindingRecord,
  IdentityManagedPolicyRecord,
  IdentityManagedPolicyStatementRecord,
  IdentityManagedPolicyVersionRecord,
  IdentityResourceScopeRecord,
  IdentityTenantAdministrationContext,
  IdentityTenantMembershipRecord,
  IdentityUserRecord,
} from "@identity-access/client";
import { IdentityAccessAdminRequest } from "./IdentityAccessAdminRequest";

export interface IdentityAccessAdminAccessInsightStatement {
  readonly statementId: string;
  readonly modelVersion: number;
  readonly resource: string;
  readonly feature: string;
  readonly action: string;
}

export interface IdentityAccessAdminAccessInsightBinding {
  readonly policyId: string;
  readonly policyVersion: number;
  readonly policyName: string;
  readonly policyStatus: number | undefined;
  readonly policyVersionPublishedAt: string | undefined;
  readonly resourceScopeId: string | undefined;
  readonly resourceScopeName: string | undefined;
  readonly resourceScopeStatus: number | undefined;
  readonly includeDescendants: boolean;
  readonly lifecycleReady: boolean;
  readonly blockers: readonly string[];
  readonly statements: readonly IdentityAccessAdminAccessInsightStatement[];
}

export interface IdentityAccessAdminAccessInsightGroup {
  readonly groupId: string;
  readonly groupName: string;
  readonly groupStatus: number;
  readonly lifecycleReady: boolean;
  readonly bindings: readonly IdentityAccessAdminAccessInsightBinding[];
}

export interface IdentityAccessAdminAccessInsight {
  readonly membership: IdentityTenantMembershipRecord | null;
  readonly groups: readonly IdentityAccessAdminAccessInsightGroup[];
  readonly scannedGroupCount: number;
  readonly scanBoundReached: boolean;
  readonly assignmentCount: number;
  readonly statementCount: number;
  readonly lifecycleReadyAssignmentCount: number;
}

/**
 * Server-only diagnostic composition over the managed-policy administration APIs.
 * It explains assignment provenance and lifecycle blockers without evaluating wildcard matching
 * or producing an allow/deny result for the selected user.
 */
export class IdentityAccessAdminAccessInsightService {
  static readonly #groupScanLimit = 100;
  readonly #request: IdentityAccessAdminRequest;

  public constructor(request: IdentityAccessAdminRequest) {
    this.#request = request;
  }

  public async inspectUser(
    user: IdentityUserRecord,
    context: IdentityTenantAdministrationContext,
  ): Promise<IdentityAccessAdminAccessInsight> {
    const membership = await this.#request.client.administration.memberships.findByUser(context, user.userId);
    if (membership === null) {
      return {
        membership: null,
        groups: [],
        scannedGroupCount: 0,
        scanBoundReached: false,
        assignmentCount: 0,
        statementCount: 0,
        lifecycleReadyAssignmentCount: 0,
      };
    }

    const groups = await this.#request.client.administration.groups.list(
      context,
      { limit: IdentityAccessAdminAccessInsightService.#groupScanLimit },
    );

    const membershipsByGroup = await Promise.all(groups.map(async (group) => ({
      group,
      members: await this.#request.client.administration.groups.listMembers(context, group.groupId),
    })));

    const assignedGroups = membershipsByGroup
      .filter(({ members }) => members.some((member) => member.tenantMembershipId === membership.membershipId))
      .map(({ group }) => group);

    if (assignedGroups.length === 0) {
      return {
        membership,
        groups: [],
        scannedGroupCount: groups.length,
        scanBoundReached: groups.length === IdentityAccessAdminAccessInsightService.#groupScanLimit,
        assignmentCount: 0,
        statementCount: 0,
        lifecycleReadyAssignmentCount: 0,
      };
    }

    const bindingsByGroup = await Promise.all(assignedGroups.map(async (group) => ({
      groupId: group.groupId,
      bindings: await this.#request.client.administration.managedPolicyBindings.list(context, group.groupId),
    })));

    const allBindings = bindingsByGroup.flatMap(({ bindings }) => bindings);
    const policyIds = [...new Set(allBindings.map((binding) => binding.policyId))];
    const policyVersions = [...new Map(allBindings.map((binding) => [
      `${binding.policyId}:v${binding.policyVersion}`,
      { policyId: binding.policyId, policyVersion: binding.policyVersion },
    ])).values()];
    const scopeIds = [...new Set(allBindings.flatMap((binding) => binding.resourceScopeId === undefined ? [] : [binding.resourceScopeId]))];
    const [policies, versions, scopes, statements] = await Promise.all([
      Promise.all(policyIds.map(async (policyId) =>
        this.#request.client.administration.managedPolicies.get(this.#request.administrationContext, policyId))),
      Promise.all(policyVersions.map(async ({ policyId, policyVersion }) => ({
        policyId,
        policyVersion,
        version: await this.#request.client.administration.managedPolicies.getVersion(
          this.#request.administrationContext,
          policyId,
          policyVersion,
        ),
      }))),
      Promise.all(scopeIds.map(async (scopeId) => this.#request.client.administration.resourceScopes.get(context, scopeId))),
      Promise.all(policyVersions.map(async ({ policyId, policyVersion }) => ({
        policyId,
        policyVersion,
        statements: await this.#request.client.administration.managedPolicies.listStatements(
          this.#request.administrationContext,
          policyId,
          policyVersion,
        ),
      }))),
    ]);

    const policyMap = new Map<string, IdentityManagedPolicyRecord>(policies.flatMap((policy) =>
      policy === null ? [] : [[policy.policyId, policy]]));
    const versionMap = new Map<string, IdentityManagedPolicyVersionRecord>(versions.flatMap((entry) =>
      entry.version === null ? [] : [[this.#versionKey(entry.policyId, entry.policyVersion), entry.version]]));
    const scopeMap = new Map<string, IdentityResourceScopeRecord>(scopes.flatMap((scope) =>
      scope === null ? [] : [[scope.resourceScopeId, scope]]));
    const statementMap = new Map<string, readonly IdentityManagedPolicyStatementRecord[]>(
      statements.map((entry) => [this.#versionKey(entry.policyId, entry.policyVersion), entry.statements]),
    );
    const bindingMap = new Map<string, readonly IdentityManagedGroupPolicyBindingRecord[]>(
      bindingsByGroup.map((entry) => [entry.groupId, entry.bindings]),
    );

    let assignmentCount = 0;
    let statementCount = 0;
    let lifecycleReadyAssignmentCount = 0;

    const projectedGroups: IdentityAccessAdminAccessInsightGroup[] = assignedGroups.map((group) => {
      const groupBindings = bindingMap.get(group.groupId) ?? [];
      const projectedBindings = groupBindings.map((binding): IdentityAccessAdminAccessInsightBinding => {
        const policy = policyMap.get(binding.policyId);
        const versionKey = this.#versionKey(binding.policyId, binding.policyVersion);
        const version = versionMap.get(versionKey);
        const scope = binding.resourceScopeId === undefined ? undefined : scopeMap.get(binding.resourceScopeId);
        const blockers = this.#lifecycleBlockers(user, membership, group, policy, version, scope, binding);
        const policyStatements = statementMap.get(versionKey) ?? [];

        assignmentCount += 1;
        statementCount += policyStatements.length;
        if (blockers.length === 0) lifecycleReadyAssignmentCount += 1;

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
      membership,
      groups: projectedGroups,
      scannedGroupCount: groups.length,
      scanBoundReached: groups.length === IdentityAccessAdminAccessInsightService.#groupScanLimit,
      assignmentCount,
      statementCount,
      lifecycleReadyAssignmentCount,
    };
  }

  #lifecycleBlockers(
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

  #versionKey(policyId: string, policyVersion: number): string {
    return `${policyId}:v${policyVersion}`;
  }
}
