import "server-only";
import { IdentityAccessAdminRequest } from "./IdentityAccessAdminRequest";

/** Owns only Organization-to-ResourceScope link mutations. */
export class IdentityAccessAdminOrganizationScopeLinkMutationService {
  readonly #request: IdentityAccessAdminRequest;

  public constructor(request: IdentityAccessAdminRequest) {
    this.#request = request;
  }

  public async link(formData: FormData): Promise<void> {
    await this.#request.client.administration.organizationResourceScopeLinks.create(
      this.#tenantContext(formData),
      IdentityAccessAdminOrganizationScopeLinkMutationService.requiredText(
        formData,
        "organizationId",
        64,
      ),
      IdentityAccessAdminOrganizationScopeLinkMutationService.requiredText(
        formData,
        "resourceScopeId",
        64,
      ),
    );
  }

  public async relink(formData: FormData): Promise<void> {
    await this.#request.client.administration.organizationResourceScopeLinks.update(
      this.#tenantContext(formData),
      IdentityAccessAdminOrganizationScopeLinkMutationService.requiredText(
        formData,
        "organizationId",
        64,
      ),
      IdentityAccessAdminOrganizationScopeLinkMutationService.requiredText(
        formData,
        "resourceScopeId",
        64,
      ),
      IdentityAccessAdminOrganizationScopeLinkMutationService.positiveInteger(
        formData,
        "expectedRowVersion",
      ),
    );
  }

  public async unlink(formData: FormData): Promise<void> {
    IdentityAccessAdminOrganizationScopeLinkMutationService.requireLiteralConfirmation(
      formData,
      "REMOVE",
    );

    await this.#request.client.administration.organizationResourceScopeLinks.remove(
      this.#tenantContext(formData),
      IdentityAccessAdminOrganizationScopeLinkMutationService.requiredText(
        formData,
        "organizationId",
        64,
      ),
      IdentityAccessAdminOrganizationScopeLinkMutationService.positiveInteger(
        formData,
        "expectedRowVersion",
      ),
    );
  }

  #tenantContext(formData: FormData) {
    const tenantId =
      IdentityAccessAdminOrganizationScopeLinkMutationService.requiredText(
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

  private static positiveInteger(
    formData: FormData,
    field: string,
  ): number {
    const text =
      IdentityAccessAdminOrganizationScopeLinkMutationService.requiredText(
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

  private static requireLiteralConfirmation(
    formData: FormData,
    expected: string,
  ): void {
    if (formData.get("confirmation") !== expected) {
      throw new Error(
        `Invalid administration input: type ${expected} to confirm this security-sensitive operation.`,
      );
    }
  }
}
