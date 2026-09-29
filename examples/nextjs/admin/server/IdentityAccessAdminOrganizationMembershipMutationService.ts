import "server-only";
import { IdentityAccessAdminRequest } from "./IdentityAccessAdminRequest";

/** Owns only explicit OrganizationMembership reconciliation for one tenant member. */
export class IdentityAccessAdminOrganizationMembershipMutationService {
  readonly #request: IdentityAccessAdminRequest;

  public constructor(request: IdentityAccessAdminRequest) {
    this.#request = request;
  }

  public async replaceTenantMemberOrganizations(
    formData: FormData,
  ): Promise<void> {
    const tenantId =
      IdentityAccessAdminOrganizationMembershipMutationService.requiredText(
        formData,
        "tenantId",
        64,
      );
    const context = this.#request.tenantContextFor(tenantId);
    const tenantMembershipId =
      IdentityAccessAdminOrganizationMembershipMutationService.requiredText(
        formData,
        "tenantMembershipId",
        64,
      );
    const selections =
      IdentityAccessAdminOrganizationMembershipMutationService.textList(
        formData,
        "organizationSelection",
        160,
      );

    const [organizations, currentMemberships] = await Promise.all([
      this.#request.client.administration.organizations.list(
        context,
        { limit: 200 },
      ),
      this.#request.client.administration.organizationMemberships.listForTenantMembership(
        context,
        tenantMembershipId,
        { limit: 200 },
      ),
    ]);

    const organizationsById = new Map(
      organizations.map((organization) => [
        organization.organizationId,
        organization,
      ]),
    );
    const currentByOrganizationId = new Map(
      currentMemberships.map((membership) => [
        membership.organizationId,
        membership,
      ]),
    );
    const desiredOrganizationIds = new Set<string>();

    for (const selection of selections) {
      const separator = selection.indexOf(":");
      if (
        separator <= 0 ||
        selection.slice(0, separator) !== "organization"
      ) {
        throw new Error("Invalid Organization selection.");
      }

      const organizationId = selection.slice(separator + 1);
      const organization = organizationsById.get(organizationId);
      if (!organization) {
        throw new Error(
          "Selected Organization is unavailable in this tenant.",
        );
      }

      const current = currentByOrganizationId.get(organizationId);
      if (
        (!current || current.status !== 1) &&
        organization.status !== 1
      ) {
        throw new Error(
          "A new or reactivated Organization membership requires an active Organization.",
        );
      }

      desiredOrganizationIds.add(organizationId);
    }

    for (const membership of currentMemberships) {
      if (!desiredOrganizationIds.has(membership.organizationId)) {
        await this.#request.client.administration.organizationMemberships.remove(
          context,
          membership.organizationId,
          tenantMembershipId,
          membership.rowVersion,
        );
      } else if (membership.status !== 1) {
        await this.#request.client.administration.organizationMemberships.activate(
          context,
          membership.organizationId,
          tenantMembershipId,
          membership.rowVersion,
        );
      }
    }

    for (const organizationId of desiredOrganizationIds) {
      if (!currentByOrganizationId.has(organizationId)) {
        await this.#request.client.administration.organizationMemberships.add(
          context,
          organizationId,
          tenantMembershipId,
        );
      }
    }
  }

  private static requiredText(
    formData: FormData,
    field: string,
    maxLength: number,
  ): string {
    const value = formData.get(field);
    if (typeof value !== "string") {
      throw new Error(
        `Invalid administration input: ${field} is required.`,
      );
    }

    const normalized = value.trim();
    if (!normalized || normalized.length > maxLength) {
      throw new Error(
        `Invalid administration input: ${field} must contain between 1 and ${maxLength} characters.`,
      );
    }

    return normalized;
  }

  private static textList(
    formData: FormData,
    field: string,
    maxLength: number,
  ): readonly string[] {
    return formData.getAll(field).map((value) => {
      if (typeof value !== "string") {
        throw new Error(
          `Invalid administration input: ${field} is invalid.`,
        );
      }

      const normalized = value.trim();
      if (!normalized || normalized.length > maxLength) {
        throw new Error(
          `Invalid administration input: ${field} contains an invalid value.`,
        );
      }

      return normalized;
    });
  }
}
