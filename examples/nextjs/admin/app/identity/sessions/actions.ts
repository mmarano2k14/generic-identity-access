"use server";

import { revalidatePath } from "next/cache";
import type { AdminActionState } from "../../../contracts/AdminActionState";
import { IdentityAccessAdminFailurePresentation } from "../../../server/IdentityAccessAdminFailurePresentation";
import { IdentityAccessAdminRequest } from "../../../server/IdentityAccessAdminRequest";
import { IdentityAccessAdminSessionMutationService } from "../../../server/IdentityAccessAdminSessionMutationService";

export async function revokeUserSessionsAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute(async (service) => {
    const count = await service.revokeUserSessions(formData);
    return `${count} session${count === 1 ? "" : "s"} revoked.`;
  });
}

export async function revokeClientSessionsAction(_state: AdminActionState, formData: FormData): Promise<AdminActionState> {
  return execute(async (service) => {
    const count = await service.revokeClientSessions(formData);
    return `${count} session${count === 1 ? "" : "s"} revoked.`;
  });
}

async function execute(
  operation: (service: IdentityAccessAdminSessionMutationService) => Promise<string>,
): Promise<AdminActionState> {
  try {
    const request = await IdentityAccessAdminRequest.fromCurrentRequest();
    const service = new IdentityAccessAdminSessionMutationService(request);
    const message = await operation(service);
    revalidatePath("/identity/sessions");
    return { status: "success", message };
  } catch (error) {
    return { status: "error", failure: IdentityAccessAdminFailurePresentation.fromMutation(error) };
  }
}
