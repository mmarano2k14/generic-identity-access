"use server";

import { revalidatePath } from "next/cache";
import type { AdminActionState } from "../../contracts/AdminActionState";
import { IdentityAccessAdminFailurePresentation } from "../../server/IdentityAccessAdminFailurePresentation";
import { IdentityAccessAdminManagedPolicyMutationService } from "../../server/IdentityAccessAdminManagedPolicyMutationService";
import { IdentityAccessAdminMutationService } from "../../server/IdentityAccessAdminMutationService";
import { IdentityAccessAdminRequest } from "../../server/IdentityAccessAdminRequest";
import { IdentityAccessAdminSecurityModelFailurePresentation } from "../../server/IdentityAccessAdminSecurityModelFailurePresentation";
import { IdentityAccessAdminSecurityModelMutationService } from "../../server/IdentityAccessAdminSecurityModelMutationService";

export async function createUserAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/users", "User created.", (service) => service.createUser(formData));
}

export async function updateUserAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/users", "User updated.", (service) => service.updateUser(formData));
}

export async function createTenantAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute(["/identity/tenants", "/identity/memberships"], "Tenant created.", (service) => service.createTenant(formData));
}

export async function updateTenantAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute(["/identity/tenants", "/identity/memberships"], "Tenant updated.", (service) => service.updateTenant(formData));
}

export async function createTenantMembershipAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute(["/identity/memberships", "/identity/tenants"], "Tenant membership created.", (service) => service.createTenantMembership(formData));
}

export async function updateTenantMembershipAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute(["/identity/memberships", "/identity/tenants"], "Tenant membership updated.", (service) => service.updateTenantMembership(formData));
}

export async function createGroupAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/groups", "Group created.", (service) => service.createGroup(formData));
}

export async function createGroupFromTemplateAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/groups", "Group created from template.", (service) => service.createGroupFromTemplate(formData));
}

export async function updateGroupAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/groups", "Group updated.", (service) => service.updateGroup(formData));
}

export async function updateReusableGroupAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/groups", "Group updated.", (service) => service.updateReusableGroup(formData));
}

export async function addGroupMemberAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/groups", "Group member added.", (service) => service.addGroupMember(formData));
}

export async function removeGroupMemberAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/groups", "Group member removed.", (service) => service.removeGroupMember(formData));
}

export async function replaceTenantMemberGroupsAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute(["/identity/memberships", "/identity/groups"], "Group assignments updated.", (service) => service.replaceTenantMemberGroups(formData));
}

export async function createManagedPolicyAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return executeManagedPolicy("Managed policy created.", (service) => service.createPolicy(formData));
}

export async function updateManagedPolicyAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return executeManagedPolicy("Managed policy updated.", (service) => service.updatePolicy(formData));
}

export async function createManagedPolicyVersionAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return executeManagedPolicy("Managed policy draft version created.", (service) => service.createVersion(formData));
}

export async function publishManagedPolicyVersionAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return executeManagedPolicy("Managed policy version published.", (service) => service.publishVersion(formData));
}

export async function addManagedPolicyStatementAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return executeManagedPolicy("Managed policy statement added.", (service) => service.addStatementFromCatalog(formData));
}

export async function removeManagedPolicyStatementAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return executeManagedPolicy("Managed policy statement removed.", (service) => service.removeStatement(formData));
}

export async function registerSecurityModelManifestAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  try {
    const request = await IdentityAccessAdminRequest.fromCurrentRequest();
    const service = new IdentityAccessAdminSecurityModelMutationService(request);
    const message = await service.registerManifest(formData);
    revalidatePath("/identity/security-models");
    revalidatePath("/identity/policies");
    return { status: "success", message };
  } catch (error) {
    return { status: "error", failure: IdentityAccessAdminSecurityModelFailurePresentation.fromRegistration(error) };
  }
}

export async function addManagedGroupPolicyBindingAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/groups", "Managed policy binding added.", (service) => service.addManagedGroupPolicyBinding(formData));
}

export async function removeManagedGroupPolicyBindingAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/groups", "Managed policy binding removed.", (service) => service.removeManagedGroupPolicyBinding(formData));
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

async function execute(
  revalidationPath: string | readonly string[],
  defaultSuccessMessage: string,
  operation: (service: IdentityAccessAdminMutationService) => Promise<void | string>,
): Promise<AdminActionState> {
  try {
    const request = await IdentityAccessAdminRequest.fromCurrentRequest();
    const service = new IdentityAccessAdminMutationService(request);
    const result = await operation(service);
    const paths = typeof revalidationPath === "string" ? [revalidationPath] : revalidationPath;
    for (const path of paths) revalidatePath(path);
    return { status: "success", message: typeof result === "string" ? result : defaultSuccessMessage };
  } catch (error) {
    return { status: "error", failure: IdentityAccessAdminFailurePresentation.fromMutation(error) };
  }
}

async function executeManagedPolicy(
  defaultSuccessMessage: string,
  operation: (service: IdentityAccessAdminManagedPolicyMutationService) => Promise<void | string>,
): Promise<AdminActionState> {
  try {
    const request = await IdentityAccessAdminRequest.fromCurrentRequest();
    const service = new IdentityAccessAdminManagedPolicyMutationService(request);
    const result = await operation(service);
    revalidatePath("/identity/policies");
    revalidatePath("/identity/groups");
    return { status: "success", message: typeof result === "string" ? result : defaultSuccessMessage };
  } catch (error) {
    return { status: "error", failure: IdentityAccessAdminFailurePresentation.fromMutation(error) };
  }
}
