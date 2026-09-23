import "server-only";
import { IdentityAccessClientError } from "@identity-access/client";
import { IdentityAccessAdminRequest } from "./IdentityAccessAdminRequest";

/**
 * Server-only mutation coordinator for the copyable Next.js administration module.
 * Form parsing, validation, trusted context construction, and API mutations stay outside Client Components.
 */
export class IdentityAccessAdminMutationService {
  readonly #request: IdentityAccessAdminRequest;

  public constructor(request: IdentityAccessAdminRequest) {
    this.#request = request;
  }

  public async createUser(formData: FormData): Promise<void> {
    await this.#request.client.administration.users.create(this.#request.administrationContext, {
      displayName: IdentityAccessAdminMutationService.requiredText(formData, "displayName", 200),
      status: IdentityAccessAdminMutationService.lifecycleStatus(formData, "status"),
    });
  }

  public async createTenant(formData: FormData): Promise<void> {
    await this.#request.client.administration.tenants.create(this.#request.administrationContext, {
      displayName: IdentityAccessAdminMutationService.requiredText(formData, "displayName", 200),
      status: IdentityAccessAdminMutationService.lifecycleStatus(formData, "status"),
    });
  }

  public async createTenantMembership(formData: FormData): Promise<void> {
    await this.#request.client.administration.memberships.create(this.#request.tenantContext(), {
      userId: IdentityAccessAdminMutationService.requiredText(formData, "userId", 64),
      status: IdentityAccessAdminMutationService.lifecycleStatus(formData, "status"),
    });
  }

  public async createGroup(formData: FormData): Promise<void> {
    await this.#request.client.administration.groups.create(this.#request.tenantContext(), {
      displayName: IdentityAccessAdminMutationService.requiredText(formData, "displayName", 200),
      status: IdentityAccessAdminMutationService.lifecycleStatus(formData, "status"),
    });
  }

  public async createPolicy(formData: FormData): Promise<void> {
    await this.#request.client.administration.policies.create(this.#request.tenantContext(), {
      displayName: IdentityAccessAdminMutationService.requiredText(formData, "displayName", 200),
      status: IdentityAccessAdminMutationService.lifecycleStatus(formData, "status"),
    });
  }

  public async createResourceScope(formData: FormData): Promise<void> {
    const parentResourceScopeId = IdentityAccessAdminMutationService.optionalText(formData, "parentResourceScopeId", 64);
    await this.#request.client.administration.resourceScopes.create(this.#request.tenantContext(), {
      modelVersion: IdentityAccessAdminMutationService.positiveInteger(formData, "modelVersion"),
      scopeType: IdentityAccessAdminMutationService.requiredText(formData, "scopeType", 128),
      externalResourceId: IdentityAccessAdminMutationService.requiredText(formData, "externalResourceId", 256),
      displayName: IdentityAccessAdminMutationService.requiredText(formData, "displayName", 200),
      ...(parentResourceScopeId === undefined ? {} : { parentResourceScopeId }),
      status: IdentityAccessAdminMutationService.lifecycleStatus(formData, "status"),
    });
  }

  public async createScopeAuthorityGroup(formData: FormData): Promise<void> {
    await this.#request.client.administration.scopeAuthority.createGroup(this.#request.administrationContext, {
      displayName: IdentityAccessAdminMutationService.requiredText(formData, "displayName", 200),
      status: IdentityAccessAdminMutationService.lifecycleStatus(formData, "status"),
    });
  }

  public async createScopeAuthorityPolicy(formData: FormData): Promise<void> {
    await this.#request.client.administration.scopeAuthority.createPolicy(this.#request.administrationContext, {
      displayName: IdentityAccessAdminMutationService.requiredText(formData, "displayName", 200),
      status: IdentityAccessAdminMutationService.lifecycleStatus(formData, "status"),
    });
  }

  public async revokeUserSessions(formData: FormData): Promise<number> {
    IdentityAccessAdminMutationService.requireConfirmation(formData);
    const result = await this.#request.client.administration.sessions.revokeUser(
      this.#request.administrationContext,
      IdentityAccessAdminMutationService.requiredText(formData, "userId", 64),
    );
    return result.revokedCount;
  }

  public async revokeClientSessions(formData: FormData): Promise<number> {
    IdentityAccessAdminMutationService.requireConfirmation(formData);
    const result = await this.#request.client.administration.sessions.revokeClient(
      this.#request.administrationContext,
      IdentityAccessAdminMutationService.requiredText(formData, "clientId", 128),
    );
    return result.revokedCount;
  }

  public static publicErrorMessage(error: unknown): string {
    if (error instanceof IdentityAccessClientError) {
      switch (error.code) {
        case "unauthenticated": return "Authentication is required. Sign in again and retry.";
        case "forbidden": return "The current administrator is not allowed to perform this operation.";
        case "unavailable": return "Identity Access is temporarily unavailable. No change was confirmed.";
        case "protocol": return "The requested identity operation was rejected.";
        default: return "The identity operation could not be completed.";
      }
    }
    if (error instanceof Error && error.message.startsWith("Invalid administration input:")) return error.message;
    return "The identity operation could not be completed.";
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

  private static optionalText(formData: FormData, field: string, maxLength: number): string | undefined {
    const value = formData.get(field);
    if (value === null || value === "") return undefined;
    if (typeof value !== "string") throw new Error(`Invalid administration input: ${field} is invalid.`);
    const normalized = value.trim();
    if (!normalized) return undefined;
    if (normalized.length > maxLength) throw new Error(`Invalid administration input: ${field} is too long.`);
    return normalized;
  }

  private static positiveInteger(formData: FormData, field: string): number {
    const text = IdentityAccessAdminMutationService.requiredText(formData, field, 16);
    const value = Number(text);
    if (!Number.isSafeInteger(value) || value <= 0) throw new Error(`Invalid administration input: ${field} must be a positive integer.`);
    return value;
  }

  private static lifecycleStatus(formData: FormData, field: string): 1 | 2 {
    const raw = formData.get(field);
    if (raw === null || raw === "" || raw === "1") return 1;
    if (raw === "2") return 2;
    throw new Error(`Invalid administration input: ${field} must be Active or Inactive.`);
  }

  private static requireConfirmation(formData: FormData): void {
    if (formData.get("confirmation") !== "REVOKE") {
      throw new Error("Invalid administration input: type REVOKE to confirm this security-sensitive operation.");
    }
  }
}
