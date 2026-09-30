"use server";

import { revalidatePath } from "next/cache";
import type { AdminActionState } from "../../../../contracts/AdminActionState";
import { IdentityAccessAdminFailurePresentation } from "../../../../server/IdentityAccessAdminFailurePresentation";
import { IdentityAccessAdminRequest } from "../../../../server/IdentityAccessAdminRequest";
import { OrganisationProfileMutationService } from "../../../../server/OrganisationProfileMutationService";

export async function createOrganisationProfileAction(
  _state: AdminActionState,
  formData: FormData,
): Promise<AdminActionState> {
  return execute(formData, "OrganisationProfile created.", (service) => service.create(formData));
}

export async function setOrganisationProfileTemplateAction(
  _state: AdminActionState,
  formData: FormData,
): Promise<AdminActionState> {
  return execute(formData, "Template selection updated.", (service) => service.setTemplate(formData));
}

export async function replaceOrganisationProfileDomainOverridesAction(
  _state: AdminActionState,
  formData: FormData,
): Promise<AdminActionState> {
  return execute(formData, "Domain overrides updated.", (service) => service.replaceOverrides(formData));
}

export async function resolveOrganisationProfileEffectiveVersionAction(
  _state: AdminActionState,
  formData: FormData,
): Promise<AdminActionState> {
  return execute(formData, "Effective profile version resolved.", (service) => service.resolveEffectiveVersion(formData));
}

export async function enableOrganisationProfileAction(
  _state: AdminActionState,
  formData: FormData,
): Promise<AdminActionState> {
  return execute(formData, "OrganisationProfile enabled.", (service) => service.enable(formData));
}

export async function disableOrganisationProfileAction(
  _state: AdminActionState,
  formData: FormData,
): Promise<AdminActionState> {
  return execute(formData, "OrganisationProfile disabled.", (service) => service.disable(formData));
}

async function execute(
  formData: FormData,
  successMessage: string,
  operation: (service: OrganisationProfileMutationService) => Promise<void>,
): Promise<AdminActionState> {
  try {
    const request = await IdentityAccessAdminRequest.fromCurrentRequest();
    await operation(new OrganisationProfileMutationService(request));

    const organizationId = requiredRouteValue(formData, "organizationId");
    revalidatePath(`/organisations/${encodeURIComponent(organizationId)}/profile`);

    return { status: "success", message: successMessage };
  } catch (error) {
    return {
      status: "error",
      failure: IdentityAccessAdminFailurePresentation.fromMutation(error),
    };
  }
}

function requiredRouteValue(formData: FormData, field: string): string {
  const value = formData.get(field);
  if (typeof value !== "string" || !value.trim() || value.length > 128) {
    throw new Error(`Invalid administration input: ${field} is required.`);
  }
  return value.trim().toLowerCase();
}
