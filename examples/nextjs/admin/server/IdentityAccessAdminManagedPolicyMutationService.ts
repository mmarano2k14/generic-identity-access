import "server-only";
import { IdentityAccessAdminRequest } from "./IdentityAccessAdminRequest";

/** Server-only mutations for the shared managed-policy catalog. */
export class IdentityAccessAdminManagedPolicyMutationService {
  readonly #request: IdentityAccessAdminRequest;

  public constructor(request: IdentityAccessAdminRequest) {
    this.#request = request;
  }

  public async createPolicy(formData: FormData): Promise<void> {
    await this.#request.client.administration.managedPolicies.create(this.#request.administrationContext, {
      policyKey: IdentityAccessAdminManagedPolicyMutationService.requiredText(formData, "policyKey", 128),
      displayName: IdentityAccessAdminManagedPolicyMutationService.requiredText(formData, "displayName", 200),
      status: IdentityAccessAdminManagedPolicyMutationService.lifecycleStatus(formData, "status"),
    });
  }

  public async updatePolicy(formData: FormData): Promise<void> {
    await this.#request.client.administration.managedPolicies.update(
      this.#request.administrationContext,
      IdentityAccessAdminManagedPolicyMutationService.requiredText(formData, "policyId", 64),
      {
        policyKey: IdentityAccessAdminManagedPolicyMutationService.requiredText(formData, "policyKey", 128),
        displayName: IdentityAccessAdminManagedPolicyMutationService.requiredText(formData, "displayName", 200),
        status: IdentityAccessAdminManagedPolicyMutationService.lifecycleStatus(formData, "status"),
        expectedVersion: IdentityAccessAdminManagedPolicyMutationService.positiveInteger(formData, "expectedVersion"),
      },
    );
  }

  public async createVersion(formData: FormData): Promise<void> {
    await this.#request.client.administration.managedPolicies.createVersion(
      this.#request.administrationContext,
      IdentityAccessAdminManagedPolicyMutationService.requiredText(formData, "policyId", 64),
      {
        policyVersion: IdentityAccessAdminManagedPolicyMutationService.positiveInteger(formData, "policyVersion"),
        modelVersion: IdentityAccessAdminManagedPolicyMutationService.positiveInteger(formData, "modelVersion"),
      },
    );
  }

  public async publishVersion(formData: FormData): Promise<void> {
    await this.#request.client.administration.managedPolicies.publishVersion(
      this.#request.administrationContext,
      IdentityAccessAdminManagedPolicyMutationService.requiredText(formData, "policyId", 64),
      IdentityAccessAdminManagedPolicyMutationService.positiveInteger(formData, "policyVersion"),
      { makeDefault: IdentityAccessAdminManagedPolicyMutationService.checkbox(formData, "makeDefault") },
    );
  }

  public async addStatementFromCatalog(formData: FormData): Promise<void> {
    const selected = IdentityAccessAdminManagedPolicyMutationService.requiredText(formData, "capability", 256);
    const parts = selected.split("|");
    if (parts.length !== 4) throw new Error("Invalid administration input: capability selection is invalid.");

    const selectedModelVersion = Number(parts[0]);
    if (!Number.isSafeInteger(selectedModelVersion) || selectedModelVersion <= 0) {
      throw new Error("Invalid administration input: capability model version is invalid.");
    }

    const policyId = IdentityAccessAdminManagedPolicyMutationService.requiredText(formData, "policyId", 64);
    const policyVersion = IdentityAccessAdminManagedPolicyMutationService.positiveInteger(formData, "policyVersion");
    const version = await this.#request.client.administration.managedPolicies.getVersion(
      this.#request.administrationContext,
      policyId,
      policyVersion,
    );
    if (version === null) throw new Error("Invalid administration input: managed policy version was not found.");
    if (version.publishedAt !== undefined) throw new Error("Invalid administration input: published policy versions are immutable.");
    if (version.modelVersion !== selectedModelVersion) {
      throw new Error("Invalid administration input: capability does not belong to the policy version security model.");
    }

    await this.#request.client.administration.managedPolicies.addStatement(
      this.#request.administrationContext,
      policyId,
      policyVersion,
      {
        resource: parts[1]!,
        feature: parts[2]!,
        action: parts[3]!,
      },
    );
  }

  public async removeStatement(formData: FormData): Promise<void> {
    IdentityAccessAdminManagedPolicyMutationService.requireLiteralConfirmation(formData, "REMOVE");
    await this.#request.client.administration.managedPolicies.removeStatement(
      this.#request.administrationContext,
      IdentityAccessAdminManagedPolicyMutationService.requiredText(formData, "policyId", 64),
      IdentityAccessAdminManagedPolicyMutationService.positiveInteger(formData, "policyVersion"),
      IdentityAccessAdminManagedPolicyMutationService.requiredText(formData, "statementId", 64),
    );
  }

  private static requiredText(formData: FormData, field: string, maxLength: number): string {
    const value = formData.get(field);
    if (typeof value !== "string") throw new Error(`Invalid administration input: ${field} is required.`);
    const normalized = value.trim();
    if (!normalized || normalized.length > maxLength) {
      throw new Error(`Invalid administration input: ${field} must contain between 1 and ${maxLength} characters.`);
    }
    return normalized;
  }

  private static positiveInteger(formData: FormData, field: string): number {
    const text = IdentityAccessAdminManagedPolicyMutationService.requiredText(formData, field, 16);
    const value = Number(text);
    if (!Number.isSafeInteger(value) || value <= 0) {
      throw new Error(`Invalid administration input: ${field} must be a positive integer.`);
    }
    return value;
  }

  private static lifecycleStatus(formData: FormData, field: string): 1 | 2 {
    const raw = formData.get(field);
    if (raw === null || raw === "" || raw === "1") return 1;
    if (raw === "2") return 2;
    throw new Error(`Invalid administration input: ${field} must be Active or Inactive.`);
  }

  private static checkbox(formData: FormData, field: string): boolean {
    const raw = formData.get(field);
    return raw === "on" || raw === "true" || raw === "1";
  }

  private static requireLiteralConfirmation(formData: FormData, expected: string): void {
    if (IdentityAccessAdminManagedPolicyMutationService.requiredText(formData, "confirmation", 32) !== expected) {
      throw new Error(`Invalid administration input: confirmation must be ${expected}.`);
    }
  }
}
