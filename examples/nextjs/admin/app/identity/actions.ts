"use server";

import { revalidatePath } from "next/cache";
import type { AdminActionState } from "../../contracts/AdminActionState";
import { IdentityAccessAdminMutationService } from "../../server/IdentityAccessAdminMutationService";
import { IdentityAccessAdminRequest } from "../../server/IdentityAccessAdminRequest";

export async function createUserAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/users", "User created.", (service) => service.createUser(formData));
}

export async function updateUserAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/users", "User updated.", (service) => service.updateUser(formData));
}

export async function createTenantAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/tenants", "Tenant created.", (service) => service.createTenant(formData));
}

export async function updateTenantAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/tenants", "Tenant updated.", (service) => service.updateTenant(formData));
}

export async function createTenantMembershipAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/memberships", "Tenant membership created.", (service) => service.createTenantMembership(formData));
}

export async function updateTenantMembershipAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/memberships", "Tenant membership updated.", (service) => service.updateTenantMembership(formData));
}

export async function createGroupAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/groups", "Group created.", (service) => service.createGroup(formData));
}

export async function updateGroupAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/groups", "Group updated.", (service) => service.updateGroup(formData));
}

export async function addGroupMemberAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/groups", "Group member added.", (service) => service.addGroupMember(formData));
}

export async function removeGroupMemberAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/groups", "Group member removed.", (service) => service.removeGroupMember(formData));
}

export async function createPolicyAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/policies", "Policy created.", (service) => service.createPolicy(formData));
}

export async function updatePolicyAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/policies", "Policy updated.", (service) => service.updatePolicy(formData));
}

export async function addPolicyStatementAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/policies", "Policy statement added.", (service) => service.addPolicyStatement(formData));
}

export async function removePolicyStatementAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/policies", "Policy statement removed.", (service) => service.removePolicyStatement(formData));
}

export async function addGroupPolicyBindingAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/groups", "Policy binding added.", (service) => service.addGroupPolicyBinding(formData));
}

export async function removeGroupPolicyBindingAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/groups", "Policy binding removed.", (service) => service.removeGroupPolicyBinding(formData));
}

export async function createResourceScopeAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/resource-scopes", "Resource scope created.", (service) => service.createResourceScope(formData));
}

export async function updateResourceScopeAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/resource-scopes", "Resource scope updated.", (service) => service.updateResourceScope(formData));
}

export async function createScopeAuthorityGroupAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/authority", "Scope authority group created.", (service) => service.createScopeAuthorityGroup(formData));
}

export async function updateScopeAuthorityGroupAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/authority", "Scope authority group updated.", (service) => service.updateScopeAuthorityGroup(formData));
}

export async function addScopeAuthorityMemberAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/authority", "Scope authority member added.", (service) => service.addScopeAuthorityMember(formData));
}

export async function removeScopeAuthorityMemberAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/authority", "Scope authority member removed.", (service) => service.removeScopeAuthorityMember(formData));
}

export async function createScopeAuthorityPolicyAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/authority", "Scope authority policy created.", (service) => service.createScopeAuthorityPolicy(formData));
}

export async function updateScopeAuthorityPolicyAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/authority", "Scope authority policy updated.", (service) => service.updateScopeAuthorityPolicy(formData));
}

export async function addScopeAuthorityPolicyStatementAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/authority", "Scope authority policy statement added.", (service) => service.addScopeAuthorityPolicyStatement(formData));
}

export async function removeScopeAuthorityPolicyStatementAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/authority", "Scope authority policy statement removed.", (service) => service.removeScopeAuthorityPolicyStatement(formData));
}

export async function addScopeAuthorityPolicyBindingAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/authority", "Scope authority policy binding added.", (service) => service.addScopeAuthorityPolicyBinding(formData));
}

export async function removeScopeAuthorityPolicyBindingAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/authority", "Scope authority policy binding removed.", (service) => service.removeScopeAuthorityPolicyBinding(formData));
}

export async function createMfaPolicyAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/mfa", "MFA policy created.", (service) => service.createMfaPolicy(formData));
}

export async function updateMfaPolicyAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/mfa", "MFA policy updated.", (service) => service.updateMfaPolicy(formData));
}

export async function revokeMfaAuthenticatorAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/mfa", "Authenticator revoked.", (service) => service.revokeMfaAuthenticator(formData));
}

export async function recoveryRevokeMfaAuthenticatorAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/mfa", "Lost authenticator revoked and sessions contained.", (service) => service.recoveryRevokeMfaAuthenticator(formData));
}

export async function revokeUserSessionsAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/sessions", "User sessions revoked.", async (service) => {
    const count = await service.revokeUserSessions(formData);
    return `${count} session${count === 1 ? "" : "s"} revoked.`;
  });
}

export async function revokeClientSessionsAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/sessions", "Client sessions revoked.", async (service) => {
    const count = await service.revokeClientSessions(formData);
    return `${count} session${count === 1 ? "" : "s"} revoked.`;
  });
}

async function execute(
  revalidationPath: string,
  defaultSuccessMessage: string,
  operation: (service: IdentityAccessAdminMutationService) => Promise<void | string>,
): Promise<AdminActionState> {
  try {
    const request = await IdentityAccessAdminRequest.fromCurrentRequest();
    const service = new IdentityAccessAdminMutationService(request);
    const result = await operation(service);
    revalidatePath(revalidationPath);
    return { status: "success", message: typeof result === "string" ? result : defaultSuccessMessage };
  } catch (error) {
    return { status: "error", message: IdentityAccessAdminMutationService.publicErrorMessage(error) };
  }
}
