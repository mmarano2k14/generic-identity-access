import type {
  IdentityGroupRecord,
  IdentityPolicyRecord,
  IdentityResourceScopeRecord,
  IdentityScopeAuthorityGroupRecord,
  IdentityScopeAuthorityPolicyRecord,
  IdentityTenantMembershipRecord,
  IdentityTenantRecord,
  IdentityUserRecord,
} from "@identity-access/client";
import type { AdminEntityReferenceOption } from "../contracts/AdminEntityReferenceOption";

/** Maps typed administration records to the shared identifier/display-name autocomplete model. */
export class IdentityAccessAdminEntityReferencePresentation {
  public static users(records: readonly IdentityUserRecord[]): readonly AdminEntityReferenceOption[] {
    return records.map((record) => ({
      id: record.userId,
      displayName: record.displayName,
      description: record.status === 1 ? "Active user" : "Inactive user",
    }));
  }

  public static tenants(records: readonly IdentityTenantRecord[]): readonly AdminEntityReferenceOption[] {
    return records.map((record) => ({
      id: record.tenantId,
      displayName: record.displayName,
      description: record.status === 1 ? "Active tenant" : "Inactive tenant",
    }));
  }

  public static tenantMemberships(
    memberships: readonly IdentityTenantMembershipRecord[],
    users: readonly IdentityUserRecord[],
  ): readonly AdminEntityReferenceOption[] {
    const usersById = new Map(users.map((user) => [user.userId, user]));
    return memberships.map((membership) => {
      const user = usersById.get(membership.userId);
      return {
        id: membership.membershipId,
        displayName: user?.displayName ?? membership.userId,
        description: `Tenant membership · ${membership.userId}`,
        keywords: [membership.userId],
      };
    });
  }

  public static groups(records: readonly IdentityGroupRecord[]): readonly AdminEntityReferenceOption[] {
    return records.map((record) => ({
      id: record.groupId,
      displayName: record.displayName,
      description: record.status === 1 ? "Active group" : "Inactive group",
    }));
  }

  public static policies(records: readonly IdentityPolicyRecord[]): readonly AdminEntityReferenceOption[] {
    return records.map((record) => ({
      id: record.policyId,
      displayName: record.displayName,
      description: record.status === 1 ? "Active policy" : "Inactive policy",
    }));
  }

  public static resourceScopes(records: readonly IdentityResourceScopeRecord[]): readonly AdminEntityReferenceOption[] {
    return records.map((record) => ({
      id: record.resourceScopeId,
      displayName: record.displayName,
      description: `${record.scopeType} · ${record.externalResourceId}`,
      keywords: [record.scopeType, record.externalResourceId],
    }));
  }

  public static scopeAuthorityGroups(records: readonly IdentityScopeAuthorityGroupRecord[]): readonly AdminEntityReferenceOption[] {
    return records.map((record) => ({
      id: record.groupId,
      displayName: record.displayName,
      description: record.status === 1 ? "Active authority group" : "Inactive authority group",
    }));
  }

  public static scopeAuthorityPolicies(records: readonly IdentityScopeAuthorityPolicyRecord[]): readonly AdminEntityReferenceOption[] {
    return records.map((record) => ({
      id: record.policyId,
      displayName: record.displayName,
      description: record.status === 1 ? "Active authority policy" : "Inactive authority policy",
    }));
  }
}
