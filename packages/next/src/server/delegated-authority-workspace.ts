import "server-only";

import type { IdentityAdministrationContext } from "@generic-identity/auth";
import type {
  IdentityScopeAuthorityGroupRecord,
  IdentityScopeAuthorityMemberRecord,
  IdentityScopeAuthorityPolicyBindingRecord,
  IdentityScopeAuthorityPolicyRecord,
  IdentityScopeAuthorityPolicyStatementRecord,
  IdentityUserRecord,
} from "@generic-identity/contracts";
import { isAllowedOnServer } from "./authorization";
import { NextIdentityServerSession } from "./session";

const AUTHORITY_LIMIT = 50;

export interface NextDelegatedAuthorityWorkspaceQuery {
  readonly groupId?: string;
  readonly policyId?: string;
}

export interface NextDelegatedAuthorityWorkspacePermissions {
  readonly canReadGroups: boolean;
  readonly canWriteGroups: boolean;
  readonly canReadMemberships: boolean;
  readonly canWriteMemberships: boolean;
  readonly canReadPolicies: boolean;
  readonly canWritePolicies: boolean;
  readonly canReadStatements: boolean;
  readonly canWriteStatements: boolean;
  readonly canReadBindings: boolean;
  readonly canWriteBindings: boolean;
  readonly canReadUsers: boolean;
}

export interface NextDelegatedAuthorityWorkspace {
  readonly groups: readonly IdentityScopeAuthorityGroupRecord[];
  readonly policies: readonly IdentityScopeAuthorityPolicyRecord[];
  readonly selectedGroup: IdentityScopeAuthorityGroupRecord | null;
  readonly selectedPolicy: IdentityScopeAuthorityPolicyRecord | null;
  readonly members: readonly IdentityScopeAuthorityMemberRecord[];
  readonly statements: readonly IdentityScopeAuthorityPolicyStatementRecord[];
  readonly bindings: readonly IdentityScopeAuthorityPolicyBindingRecord[];
  readonly memberUsers: readonly IdentityUserRecord[];
  readonly boundPolicies: readonly IdentityScopeAuthorityPolicyRecord[];
  readonly permissions: NextDelegatedAuthorityWorkspacePermissions;
  readonly groupsTruncated: boolean;
  readonly policiesTruncated: boolean;
}

/**
 * Composes the tenant-free identity-scope authority workspace.
 *
 * Scope-authority groups/policies are deliberately separate from tenant Groups
 * and Managed Policies. A Super Admin grant never causes those catalogs to be
 * merged; it merely grants the relevant scope-authority capabilities.
 */
export async function loadNextDelegatedAuthorityWorkspace(
  session: NextIdentityServerSession,
  context: IdentityAdministrationContext,
  query: NextDelegatedAuthorityWorkspaceQuery = {},
): Promise<NextDelegatedAuthorityWorkspace> {
  const boundary = {
    identityScopeId: context.identityScopeId,
    applicationKey: context.applicationKey,
  };
  const capability = (feature: string, action: string) =>
    isAllowedOnServer(session, boundary, { resource: "identity-access", feature, action });

  const [
    canReadGroups,
    canWriteGroups,
    canReadMemberships,
    canWriteMemberships,
    canReadPolicies,
    canWritePolicies,
    canReadStatements,
    canWriteStatements,
    canReadBindings,
    canWriteBindings,
    canReadUsers,
  ] = await Promise.all([
    capability("scope-authority-group", "read"),
    capability("scope-authority-group", "write"),
    capability("scope-authority-membership", "read"),
    capability("scope-authority-membership", "write"),
    capability("scope-authority-policy", "read"),
    capability("scope-authority-policy", "write"),
    capability("scope-authority-statement", "read"),
    capability("scope-authority-statement", "write"),
    capability("scope-authority-binding", "read"),
    capability("scope-authority-binding", "write"),
    capability("user", "read"),
  ]);

  const permissions: NextDelegatedAuthorityWorkspacePermissions = {
    canReadGroups,
    canWriteGroups,
    canReadMemberships,
    canWriteMemberships,
    canReadPolicies,
    canWritePolicies,
    canReadStatements,
    canWriteStatements,
    canReadBindings,
    canWriteBindings,
    canReadUsers,
  };

  const [groups, policies] = await Promise.all([
    canReadGroups
      ? session.client.accessControl.delegatedAuthority.listGroups(context, { limit: AUTHORITY_LIMIT })
      : Promise.resolve([] as readonly IdentityScopeAuthorityGroupRecord[]),
    canReadPolicies
      ? session.client.accessControl.delegatedAuthority.listPolicies(context, { limit: AUTHORITY_LIMIT })
      : Promise.resolve([] as readonly IdentityScopeAuthorityPolicyRecord[]),
  ]);

  const requestedGroupId = query.groupId?.trim().toLowerCase();
  const requestedPolicyId = query.policyId?.trim().toLowerCase();

  const [selectedGroup, selectedPolicy] = await Promise.all([
    requestedGroupId && canReadGroups
      ? groups.find((group) => group.groupId === requestedGroupId)
        ?? session.client.accessControl.delegatedAuthority.getGroup(context, requestedGroupId)
      : Promise.resolve(null),
    requestedPolicyId && canReadPolicies
      ? policies.find((policy) => policy.policyId === requestedPolicyId)
        ?? session.client.accessControl.delegatedAuthority.getPolicy(context, requestedPolicyId)
      : Promise.resolve(null),
  ]);

  const [members, statements, bindings] = await Promise.all([
    selectedGroup && canReadMemberships
      ? session.client.accessControl.delegatedAuthority.listMembers(context, selectedGroup.groupId)
      : Promise.resolve([] as readonly IdentityScopeAuthorityMemberRecord[]),
    selectedPolicy && canReadStatements
      ? session.client.accessControl.delegatedAuthority.listPolicyStatements(context, selectedPolicy.policyId)
      : Promise.resolve([] as readonly IdentityScopeAuthorityPolicyStatementRecord[]),
    selectedGroup && canReadBindings
      ? session.client.accessControl.delegatedAuthority.listPolicyBindings(context, selectedGroup.groupId)
      : Promise.resolve([] as readonly IdentityScopeAuthorityPolicyBindingRecord[]),
  ]);

  const [memberUsers, boundPolicies] = await Promise.all([
    canReadUsers
      ? Promise.all(members.map((member) => session.client.directory.users.get(context, member.userId)))
          .then((records) => records.filter((record): record is IdentityUserRecord => record !== null))
      : Promise.resolve([] as readonly IdentityUserRecord[]),
    canReadPolicies
      ? Promise.all(bindings.map((binding) => (
          policies.find((policy) => policy.policyId === binding.policyId)
          ?? session.client.accessControl.delegatedAuthority.getPolicy(context, binding.policyId)
        ))).then((records) => records.filter((record): record is IdentityScopeAuthorityPolicyRecord => record !== null))
      : Promise.resolve([] as readonly IdentityScopeAuthorityPolicyRecord[]),
  ]);

  return {
    groups,
    policies,
    selectedGroup,
    selectedPolicy,
    members,
    statements,
    bindings,
    memberUsers,
    boundPolicies,
    permissions,
    groupsTruncated: groups.length >= AUTHORITY_LIMIT,
    policiesTruncated: policies.length >= AUTHORITY_LIMIT,
  };
}
