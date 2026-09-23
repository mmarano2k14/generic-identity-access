"use server";

import { revalidatePath } from "next/cache";
import type { AdminActionState } from "../../contracts/AdminActionState";
import { IdentityAccessAdminMutationService } from "../../server/IdentityAccessAdminMutationService";
import { IdentityAccessAdminRequest } from "../../server/IdentityAccessAdminRequest";

export async function createUserAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/users", "User created.", (service) => service.createUser(formData));
}

export async function createTenantAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/tenants", "Tenant created.", (service) => service.createTenant(formData));
}

export async function createTenantMembershipAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/memberships", "Tenant membership created.", (service) => service.createTenantMembership(formData));
}

export async function createGroupAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/groups", "Group created.", (service) => service.createGroup(formData));
}

export async function createPolicyAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/policies", "Policy created.", (service) => service.createPolicy(formData));
}

export async function createResourceScopeAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/resource-scopes", "Resource scope created.", (service) => service.createResourceScope(formData));
}

export async function createScopeAuthorityGroupAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/authority", "Scope authority group created.", (service) => service.createScopeAuthorityGroup(formData));
}

export async function createScopeAuthorityPolicyAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute("/identity/authority", "Scope authority policy created.", (service) => service.createScopeAuthorityPolicy(formData));
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
