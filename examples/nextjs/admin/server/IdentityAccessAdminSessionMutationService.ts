import "server-only";
import { IdentityAccessAdminRequest } from "./IdentityAccessAdminRequest";

/** Coordinates only destructive session-security mutations through existing typed client contracts. */
export class IdentityAccessAdminSessionMutationService {
  readonly #request: IdentityAccessAdminRequest;

  public constructor(request: IdentityAccessAdminRequest) {
    this.#request = request;
  }

  public async revokeUserSessions(formData: FormData): Promise<number> {
    IdentityAccessAdminSessionMutationService.#requireConfirmation(formData);
    const result = await this.#request.client.administration.sessions.revokeUser(
      this.#request.administrationContext,
      IdentityAccessAdminSessionMutationService.#requiredText(formData, "userId", 64),
    );
    return result.revokedCount;
  }

  public async revokeClientSessions(formData: FormData): Promise<number> {
    IdentityAccessAdminSessionMutationService.#requireConfirmation(formData);
    const result = await this.#request.client.administration.sessions.revokeClient(
      this.#request.administrationContext,
      IdentityAccessAdminSessionMutationService.#requiredText(formData, "clientId", 128),
    );
    return result.revokedCount;
  }

  static #requiredText(formData: FormData, field: string, maxLength: number): string {
    const value = formData.get(field);
    if (typeof value !== "string") throw new Error(`Invalid session administration input: ${field} is required.`);
    const normalized = value.trim();
    if (!normalized || normalized.length > maxLength) {
      throw new Error(`Invalid session administration input: ${field} must contain between 1 and ${maxLength} characters.`);
    }
    return normalized;
  }

  static #requireConfirmation(formData: FormData): void {
    if (IdentityAccessAdminSessionMutationService.#requiredText(formData, "confirmation", 16) !== "REVOKE") {
      throw new Error("Invalid session administration input: confirmation must equal REVOKE.");
    }
  }
}
