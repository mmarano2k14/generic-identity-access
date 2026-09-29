"use server";

import { revalidatePath } from "next/cache";
import type { AdminActionState } from "../../../contracts/AdminActionState";
import { IdentityAccessAdminFailurePresentation } from "../../../server/IdentityAccessAdminFailurePresentation";
import { IdentityAccessAdminRequest } from "../../../server/IdentityAccessAdminRequest";
import { IdentityAccessAdminSecurityModelMutationService } from "../../../server/IdentityAccessAdminSecurityModelMutationService";

export async function addScopeTypeAction(
  modelVersion: number,
  _state: AdminActionState,
  formData: FormData,
): Promise<AdminActionState> {
  try {
    const request = await IdentityAccessAdminRequest.fromCurrentRequest();
    const service = new IdentityAccessAdminSecurityModelMutationService(request);
    const message = await service.addScopeType(modelVersion, formData);
    revalidatePath("/identity/security-models");
    revalidatePath("/identity/resource-scopes");
    return { status: "success", message };
  } catch (error) {
    return { status: "error", failure: IdentityAccessAdminFailurePresentation.fromMutation(error) };
  }
}
