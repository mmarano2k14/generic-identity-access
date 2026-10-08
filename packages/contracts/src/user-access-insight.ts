/**
 * Read-only provenance of a user's assigned tenant access. This is NOT an
 * authorization decision and MUST NOT be used as an allow/deny result.
 */
import type { IdentityTenantMembershipRecord } from "./directory/index";

export interface IdentityUserAccessStatement {
  readonly statementId: string;
  readonly modelVersion: number;
  readonly resource: string;
  readonly feature: string;
  readonly action: string;
}

export interface IdentityUserAccessBinding {
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
  readonly statements: readonly IdentityUserAccessStatement[];
}

export interface IdentityUserAccessGroup {
  readonly groupId: string;
  readonly groupName: string;
  readonly groupStatus: number;
  readonly lifecycleReady: boolean;
  readonly bindings: readonly IdentityUserAccessBinding[];
}

export interface IdentityUserAccessInsight {
  readonly membership: IdentityTenantMembershipRecord | null;
  readonly groups: readonly IdentityUserAccessGroup[];
  readonly scannedGroupCount: number;
  readonly scanBoundReached: boolean;
  readonly assignmentCount: number;
  readonly statementCount: number;
  readonly lifecycleReadyAssignmentCount: number;
}

/** Row in a membership-backed multi-tenant collection. One user may occur several times. */
export interface IdentityTenantLinkedUserRow {
  readonly tenantId: string;
  readonly tenantDisplayName: string;
  readonly membershipId: string;
  readonly userId: string;
  readonly displayName: string;
  readonly status: 1 | 2;
  readonly version: number;
}
