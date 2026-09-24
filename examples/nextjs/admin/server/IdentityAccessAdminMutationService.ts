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

  public async updateUser(formData: FormData): Promise<void> {
    await this.#request.client.administration.users.update(
      this.#request.administrationContext,
      IdentityAccessAdminMutationService.requiredText(formData, "userId", 64),
      {
        displayName: IdentityAccessAdminMutationService.requiredText(formData, "displayName", 200),
        status: IdentityAccessAdminMutationService.lifecycleStatus(formData, "status"),
        expectedVersion: IdentityAccessAdminMutationService.positiveInteger(formData, "expectedVersion"),
      },
    );
  }

  public async updateTenant(formData: FormData): Promise<void> {
    await this.#request.client.administration.tenants.update(
      this.#request.administrationContext,
      IdentityAccessAdminMutationService.requiredText(formData, "tenantId", 64),
      {
        displayName: IdentityAccessAdminMutationService.requiredText(formData, "displayName", 200),
        status: IdentityAccessAdminMutationService.lifecycleStatus(formData, "status"),
        expectedVersion: IdentityAccessAdminMutationService.positiveInteger(formData, "expectedVersion"),
      },
    );
  }

  public async updateTenantMembership(formData: FormData): Promise<void> {
    await this.#request.client.administration.memberships.update(
      this.#request.tenantContext(),
      IdentityAccessAdminMutationService.requiredText(formData, "membershipId", 64),
      {
        status: IdentityAccessAdminMutationService.lifecycleStatus(formData, "status"),
        expectedVersion: IdentityAccessAdminMutationService.positiveInteger(formData, "expectedVersion"),
      },
    );
  }

  public async updateGroup(formData: FormData): Promise<void> {
    await this.#request.client.administration.groups.update(
      this.#request.tenantContext(),
      IdentityAccessAdminMutationService.requiredText(formData, "groupId", 64),
      {
        displayName: IdentityAccessAdminMutationService.requiredText(formData, "displayName", 200),
        status: IdentityAccessAdminMutationService.lifecycleStatus(formData, "status"),
        expectedVersion: IdentityAccessAdminMutationService.positiveInteger(formData, "expectedVersion"),
      },
    );
  }

  public async addGroupMember(formData: FormData): Promise<void> {
    await this.#request.client.administration.groups.addMember(
      this.#request.tenantContext(),
      IdentityAccessAdminMutationService.requiredText(formData, "groupId", 64),
      IdentityAccessAdminMutationService.requiredText(formData, "tenantMembershipId", 64),
    );
  }

  public async removeGroupMember(formData: FormData): Promise<void> {
    IdentityAccessAdminMutationService.requireLiteralConfirmation(formData, "REMOVE");
    await this.#request.client.administration.groups.removeMember(
      this.#request.tenantContext(),
      IdentityAccessAdminMutationService.requiredText(formData, "groupId", 64),
      IdentityAccessAdminMutationService.requiredText(formData, "tenantMembershipId", 64),
    );
  }

  public async updatePolicy(formData: FormData): Promise<void> {
    await this.#request.client.administration.policies.update(
      this.#request.tenantContext(),
      IdentityAccessAdminMutationService.requiredText(formData, "policyId", 64),
      {
        displayName: IdentityAccessAdminMutationService.requiredText(formData, "displayName", 200),
        status: IdentityAccessAdminMutationService.lifecycleStatus(formData, "status"),
        expectedVersion: IdentityAccessAdminMutationService.positiveInteger(formData, "expectedVersion"),
      },
    );
  }

  public async addPolicyStatement(formData: FormData): Promise<void> {
    await this.#request.client.administration.policies.addStatement(
      this.#request.tenantContext(),
      IdentityAccessAdminMutationService.requiredText(formData, "policyId", 64),
      {
        modelVersion: IdentityAccessAdminMutationService.positiveInteger(formData, "modelVersion"),
        resource: IdentityAccessAdminMutationService.requiredText(formData, "resource", 128),
        feature: IdentityAccessAdminMutationService.requiredText(formData, "feature", 128),
        action: IdentityAccessAdminMutationService.requiredText(formData, "action", 128),
      },
    );
  }

  public async removePolicyStatement(formData: FormData): Promise<void> {
    IdentityAccessAdminMutationService.requireLiteralConfirmation(formData, "REMOVE");
    await this.#request.client.administration.policies.removeStatement(
      this.#request.tenantContext(),
      IdentityAccessAdminMutationService.requiredText(formData, "policyId", 64),
      IdentityAccessAdminMutationService.requiredText(formData, "statementId", 64),
    );
  }

  public async addGroupPolicyBinding(formData: FormData): Promise<void> {
    const resourceScopeId = IdentityAccessAdminMutationService.optionalText(formData, "resourceScopeId", 64);
    await this.#request.client.administration.policies.addBinding(
      this.#request.tenantContext(),
      IdentityAccessAdminMutationService.requiredText(formData, "groupId", 64),
      {
        policyId: IdentityAccessAdminMutationService.requiredText(formData, "policyId", 64),
        ...(resourceScopeId === undefined ? {} : { resourceScopeId }),
        includeDescendants: IdentityAccessAdminMutationService.checkbox(formData, "includeDescendants"),
      },
    );
  }

  public async removeGroupPolicyBinding(formData: FormData): Promise<void> {
    IdentityAccessAdminMutationService.requireLiteralConfirmation(formData, "REMOVE");
    const resourceScopeId = IdentityAccessAdminMutationService.optionalText(formData, "resourceScopeId", 64);
    await this.#request.client.administration.policies.removeBinding(
      this.#request.tenantContext(),
      IdentityAccessAdminMutationService.requiredText(formData, "groupId", 64),
      IdentityAccessAdminMutationService.requiredText(formData, "policyId", 64),
      resourceScopeId,
    );
  }

  public async updateResourceScope(formData: FormData): Promise<void> {
    const parentResourceScopeId = IdentityAccessAdminMutationService.optionalText(formData, "parentResourceScopeId", 64);
    await this.#request.client.administration.resourceScopes.update(
      this.#request.tenantContext(),
      IdentityAccessAdminMutationService.requiredText(formData, "resourceScopeId", 64),
      {
        modelVersion: IdentityAccessAdminMutationService.positiveInteger(formData, "modelVersion"),
        scopeType: IdentityAccessAdminMutationService.requiredText(formData, "scopeType", 128),
        externalResourceId: IdentityAccessAdminMutationService.requiredText(formData, "externalResourceId", 256),
        displayName: IdentityAccessAdminMutationService.requiredText(formData, "displayName", 200),
        ...(parentResourceScopeId === undefined ? {} : { parentResourceScopeId }),
        status: IdentityAccessAdminMutationService.lifecycleStatus(formData, "status"),
        expectedVersion: IdentityAccessAdminMutationService.positiveInteger(formData, "expectedVersion"),
      },
    );
  }

  public async updateScopeAuthorityGroup(formData: FormData): Promise<void> {
    await this.#request.client.administration.scopeAuthority.updateGroup(
      this.#request.administrationContext,
      IdentityAccessAdminMutationService.requiredText(formData, "groupId", 64),
      {
        displayName: IdentityAccessAdminMutationService.requiredText(formData, "displayName", 200),
        status: IdentityAccessAdminMutationService.lifecycleStatus(formData, "status"),
        expectedVersion: IdentityAccessAdminMutationService.positiveInteger(formData, "expectedVersion"),
      },
    );
  }

  public async addScopeAuthorityMember(formData: FormData): Promise<void> {
    await this.#request.client.administration.scopeAuthority.addMember(
      this.#request.administrationContext,
      IdentityAccessAdminMutationService.requiredText(formData, "groupId", 64),
      IdentityAccessAdminMutationService.requiredText(formData, "userId", 64),
    );
  }

  public async removeScopeAuthorityMember(formData: FormData): Promise<void> {
    IdentityAccessAdminMutationService.requireLiteralConfirmation(formData, "REMOVE");
    await this.#request.client.administration.scopeAuthority.removeMember(
      this.#request.administrationContext,
      IdentityAccessAdminMutationService.requiredText(formData, "groupId", 64),
      IdentityAccessAdminMutationService.requiredText(formData, "userId", 64),
    );
  }

  public async updateScopeAuthorityPolicy(formData: FormData): Promise<void> {
    await this.#request.client.administration.scopeAuthority.updatePolicy(
      this.#request.administrationContext,
      IdentityAccessAdminMutationService.requiredText(formData, "policyId", 64),
      {
        displayName: IdentityAccessAdminMutationService.requiredText(formData, "displayName", 200),
        status: IdentityAccessAdminMutationService.lifecycleStatus(formData, "status"),
        expectedVersion: IdentityAccessAdminMutationService.positiveInteger(formData, "expectedVersion"),
      },
    );
  }

  public async addScopeAuthorityPolicyStatement(formData: FormData): Promise<void> {
    await this.#request.client.administration.scopeAuthority.addPolicyStatement(
      this.#request.administrationContext,
      IdentityAccessAdminMutationService.requiredText(formData, "policyId", 64),
      {
        modelVersion: IdentityAccessAdminMutationService.positiveInteger(formData, "modelVersion"),
        resource: IdentityAccessAdminMutationService.requiredText(formData, "resource", 128),
        feature: IdentityAccessAdminMutationService.requiredText(formData, "feature", 128),
        action: IdentityAccessAdminMutationService.requiredText(formData, "action", 128),
      },
    );
  }

  public async removeScopeAuthorityPolicyStatement(formData: FormData): Promise<void> {
    IdentityAccessAdminMutationService.requireLiteralConfirmation(formData, "REMOVE");
    await this.#request.client.administration.scopeAuthority.removePolicyStatement(
      this.#request.administrationContext,
      IdentityAccessAdminMutationService.requiredText(formData, "policyId", 64),
      IdentityAccessAdminMutationService.requiredText(formData, "statementId", 64),
    );
  }

  public async addScopeAuthorityPolicyBinding(formData: FormData): Promise<void> {
    await this.#request.client.administration.scopeAuthority.addPolicyBinding(
      this.#request.administrationContext,
      IdentityAccessAdminMutationService.requiredText(formData, "groupId", 64),
      IdentityAccessAdminMutationService.requiredText(formData, "policyId", 64),
    );
  }

  public async removeScopeAuthorityPolicyBinding(formData: FormData): Promise<void> {
    IdentityAccessAdminMutationService.requireLiteralConfirmation(formData, "REMOVE");
    await this.#request.client.administration.scopeAuthority.removePolicyBinding(
      this.#request.administrationContext,
      IdentityAccessAdminMutationService.requiredText(formData, "groupId", 64),
      IdentityAccessAdminMutationService.requiredText(formData, "policyId", 64),
    );
  }

  public async createMfaPolicy(formData: FormData): Promise<void> {
    await this.#request.client.administration.mfa.createPolicy(this.#request.administrationContext, {
      mode: IdentityAccessAdminMutationService.mfaPolicyMode(formData, "mode"),
      allowedProviders: IdentityAccessAdminMutationService.textList(formData, "allowedProviders", 128),
    });
  }

  public async updateMfaPolicy(formData: FormData): Promise<void> {
    await this.#request.client.administration.mfa.updatePolicy(this.#request.administrationContext, {
      mode: IdentityAccessAdminMutationService.mfaPolicyMode(formData, "mode"),
      allowedProviders: IdentityAccessAdminMutationService.textList(formData, "allowedProviders", 128),
      expectedVersion: IdentityAccessAdminMutationService.positiveInteger(formData, "expectedVersion"),
    });
  }

  public async revokeMfaAuthenticator(formData: FormData): Promise<void> {
    IdentityAccessAdminMutationService.requireLiteralConfirmation(formData, "REVOKE");
    await this.#request.client.administration.mfa.revokeAuthenticator(
      this.#request.administrationContext,
      IdentityAccessAdminMutationService.requiredText(formData, "userId", 64),
      IdentityAccessAdminMutationService.requiredText(formData, "authenticatorId", 64),
      IdentityAccessAdminMutationService.positiveInteger(formData, "expectedVersion"),
    );
  }

  public async recoveryRevokeMfaAuthenticator(formData: FormData): Promise<void> {
    IdentityAccessAdminMutationService.requireLiteralConfirmation(formData, "REVOKE");
    await this.#request.client.administration.mfa.revokeAuthenticatorForRecovery(
      this.#request.administrationContext,
      IdentityAccessAdminMutationService.requiredText(formData, "userId", 64),
      IdentityAccessAdminMutationService.requiredText(formData, "authenticatorId", 64),
      IdentityAccessAdminMutationService.positiveInteger(formData, "expectedVersion"),
    );
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

  private static checkbox(formData: FormData, field: string): boolean {
    const raw = formData.get(field);
    return raw === "on" || raw === "true" || raw === "1";
  }

  private static textList(formData: FormData, field: string, maxLength: number): readonly string[] {
    return formData.getAll(field).map((value) => {
      if (typeof value !== "string") throw new Error(`Invalid administration input: ${field} is invalid.`);
      const normalized = value.trim();
      if (!normalized || normalized.length > maxLength) throw new Error(`Invalid administration input: ${field} contains an invalid value.`);
      return normalized;
    });
  }

  private static mfaPolicyMode(formData: FormData, field: string): 1 | 2 | 3 {
    const raw = formData.get(field);
    if (raw === "1") return 1;
    if (raw === "2") return 2;
    if (raw === "3") return 3;
    throw new Error(`Invalid administration input: ${field} must be Disabled, Optional, or Required.`);
  }

  private static requireLiteralConfirmation(formData: FormData, expected: string): void {
    if (formData.get("confirmation") !== expected) {
      throw new Error(`Invalid administration input: type ${expected} to confirm this security-sensitive operation.`);
    }
  }

  private static requireConfirmation(formData: FormData): void {
    IdentityAccessAdminMutationService.requireLiteralConfirmation(formData, "REVOKE");
  }
}
