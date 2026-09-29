import "server-only";
import { IdentityAccessAdminRequest } from "./IdentityAccessAdminRequest";

/** Owns only Organization definition and lifecycle mutations. */
export class IdentityAccessAdminOrganizationMutationService {
  readonly #request: IdentityAccessAdminRequest;

  public constructor(request: IdentityAccessAdminRequest) {
    this.#request = request;
  }

  public async createOrganization(formData: FormData): Promise<void> {
    const parentOrganizationId =
      IdentityAccessAdminOrganizationMutationService.optionalText(
        formData,
        "parentOrganizationId",
        64,
      );

    await this.#request.client.administration.organizations.create(
      this.#tenantContext(formData),
      {
        organizationKey:
          IdentityAccessAdminOrganizationMutationService.requiredText(
            formData,
            "organizationKey",
            64,
          ),
        displayName:
          IdentityAccessAdminOrganizationMutationService.requiredText(
            formData,
            "displayName",
            200,
          ),
        organizationType:
          IdentityAccessAdminOrganizationMutationService.requiredText(
            formData,
            "organizationType",
            64,
          ),
        ...(parentOrganizationId === undefined
          ? {}
          : { parentOrganizationId }),
      },
    );
  }

  public async updateOrganization(formData: FormData): Promise<void> {
    const parentOrganizationId =
      IdentityAccessAdminOrganizationMutationService.optionalText(
        formData,
        "parentOrganizationId",
        64,
      );

    await this.#request.client.administration.organizations.update(
      this.#tenantContext(formData),
      IdentityAccessAdminOrganizationMutationService.requiredText(
        formData,
        "organizationId",
        64,
      ),
      {
        displayName:
          IdentityAccessAdminOrganizationMutationService.requiredText(
            formData,
            "displayName",
            200,
          ),
        organizationType:
          IdentityAccessAdminOrganizationMutationService.requiredText(
            formData,
            "organizationType",
            64,
          ),
        ...(parentOrganizationId === undefined
          ? {}
          : { parentOrganizationId }),
        expectedRowVersion:
          IdentityAccessAdminOrganizationMutationService.positiveInteger(
            formData,
            "expectedRowVersion",
          ),
      },
    );
  }

  public async enableOrganization(formData: FormData): Promise<void> {
    await this.#request.client.administration.organizations.enable(
      this.#tenantContext(formData),
      IdentityAccessAdminOrganizationMutationService.requiredText(
        formData,
        "organizationId",
        64,
      ),
      IdentityAccessAdminOrganizationMutationService.positiveInteger(
        formData,
        "expectedRowVersion",
      ),
    );
  }

  public async disableOrganization(formData: FormData): Promise<void> {
    await this.#request.client.administration.organizations.disable(
      this.#tenantContext(formData),
      IdentityAccessAdminOrganizationMutationService.requiredText(
        formData,
        "organizationId",
        64,
      ),
      IdentityAccessAdminOrganizationMutationService.positiveInteger(
        formData,
        "expectedRowVersion",
      ),
    );
  }

  #tenantContext(formData: FormData) {
    const tenantId =
      IdentityAccessAdminOrganizationMutationService.requiredText(
        formData,
        "tenantId",
        64,
      );

    return this.#request.tenantContextFor(tenantId);
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

  private static optionalText(
    formData: FormData,
    field: string,
    maxLength: number,
  ): string | undefined {
    const value = formData.get(field);
    if (value === null || value === "") return undefined;
    if (typeof value !== "string") {
      throw new Error(
        `Invalid administration input: ${field} is invalid.`,
      );
    }

    const normalized = value.trim();
    if (!normalized) return undefined;
    if (normalized.length > maxLength) {
      throw new Error(
        `Invalid administration input: ${field} is too long.`,
      );
    }

    return normalized;
  }

  private static positiveInteger(
    formData: FormData,
    field: string,
  ): number {
    const text =
      IdentityAccessAdminOrganizationMutationService.requiredText(
        formData,
        field,
        16,
      );
    const value = Number(text);

    if (!Number.isSafeInteger(value) || value <= 0) {
      throw new Error(
        `Invalid administration input: ${field} must be a positive integer.`,
      );
    }

    return value;
  }
}
