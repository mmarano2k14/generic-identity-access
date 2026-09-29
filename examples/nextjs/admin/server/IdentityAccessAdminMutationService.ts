import "server-only";
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

  public async createPasswordCredential(formData: FormData): Promise<void> {
    await this.#request.client.administration.credentials.create(
      this.#request.administrationContext,
      IdentityAccessAdminMutationService.requiredText(formData, "userId", 64),
      {
        loginIdentifier: IdentityAccessAdminMutationService.requiredText(formData, "loginIdentifier", 320),
        password: IdentityAccessAdminMutationService.requiredSecret(formData, "password"),
      },
    );
  }

  public async changePasswordCredential(formData: FormData): Promise<void> {
    await this.#request.client.administration.credentials.changePassword(
      this.#request.administrationContext,
      IdentityAccessAdminMutationService.requiredText(formData, "userId", 64),
      {
        loginIdentifier: IdentityAccessAdminMutationService.requiredText(formData, "loginIdentifier", 320),
        password: IdentityAccessAdminMutationService.requiredSecret(formData, "password"),
        expectedVersion: IdentityAccessAdminMutationService.positiveInteger(formData, "expectedVersion"),
      },
    );
  }

  public async createTenant(formData: FormData): Promise<void> {
    await this.#request.client.administration.tenants.create(this.#request.administrationContext, {
      displayName: IdentityAccessAdminMutationService.requiredText(formData, "displayName", 200),
      status: IdentityAccessAdminMutationService.lifecycleStatus(formData, "status"),
    });
  }

  public async createTenantMembership(formData: FormData): Promise<void> {
    const tenantId = IdentityAccessAdminMutationService.requiredText(formData, "tenantId", 64);
    const context = this.#request.tenantContextFor(tenantId);
    const userId = IdentityAccessAdminMutationService.requiredText(formData, "userId", 64);

    const status = IdentityAccessAdminMutationService.lifecycleStatus(formData, "status");
    if (this.#request.effectiveContext.tenantVisibility !== "scope-wide") {
      const loginIdentifier = IdentityAccessAdminMutationService.requiredText(formData, "loginIdentifier", 320);
      const candidate = await this.#request.client.administration.membershipCandidates.findByLogin(context, loginIdentifier);
      if (candidate === null || candidate.userId !== userId || candidate.userStatus !== 1 || candidate.existingMembershipId !== undefined) {
        throw new Error("The exact login is not eligible for a new membership in this tenant.");
      }
      await this.#request.client.administration.membershipCandidates.createMembershipByLogin(
        context,
        loginIdentifier,
        status,
      );
      return;
    }

    await this.#request.client.administration.memberships.create(context, {
      userId,
      status,
    });
  }

  public async createGroup(formData: FormData): Promise<void> {
    await this.#request.client.administration.groups.create(this.#tenantContext(formData), {
      displayName: IdentityAccessAdminMutationService.requiredText(formData, "displayName", 200),
      status: IdentityAccessAdminMutationService.lifecycleStatus(formData, "status"),
    });
  }

  public async createGroupFromTemplate(formData: FormData): Promise<void> {
    const source = IdentityAccessAdminMutationService.requiredText(formData, "sourceGroup", 160);
    const separator = source.indexOf(":");
    if (separator <= 0) throw new Error("Invalid reusable group selection.");

    const resourceScopeMappings: Array<{
      sourceResourceScopeId: string;
      targetResourceScopeId: string;
    }> = [];
    for (const [name, value] of formData.entries()) {
      if (!name.startsWith("resourceScopeMapping:")) continue;
      const sourceResourceScopeId = name.slice("resourceScopeMapping:".length).trim();
      const targetResourceScopeId = typeof value === "string" ? value.trim() : "";
      if (!sourceResourceScopeId || !targetResourceScopeId) {
        throw new Error("Every scoped template binding requires a target resource scope.");
      }
      resourceScopeMappings.push({ sourceResourceScopeId, targetResourceScopeId });
    }

    await this.#request.client.administration.groups.createFromTemplate(
      this.#tenantContext(formData),
      {
        sourceTenantId: source.slice(0, separator),
        sourceGroupId: source.slice(separator + 1),
        resourceScopeMappings,
      },
    );
  }

  public async createResourceScope(formData: FormData): Promise<void> {
    const parentResourceScopeId = IdentityAccessAdminMutationService.optionalText(formData, "parentResourceScopeId", 64);
    await this.#request.client.administration.resourceScopes.create(this.#tenantContext(formData), {
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
    const tenantId = IdentityAccessAdminMutationService.requiredText(formData, "tenantId", 64);
    await this.#request.client.administration.memberships.update(
      this.#request.tenantContextFor(tenantId),
      IdentityAccessAdminMutationService.requiredText(formData, "membershipId", 64),
      {
        status: IdentityAccessAdminMutationService.lifecycleStatus(formData, "status"),
        expectedVersion: IdentityAccessAdminMutationService.positiveInteger(formData, "expectedVersion"),
      },
    );
  }

  public async updateGroup(formData: FormData): Promise<void> {
    await this.#request.client.administration.groups.update(
      this.#tenantContext(formData),
      IdentityAccessAdminMutationService.requiredText(formData, "groupId", 64),
      {
        displayName: IdentityAccessAdminMutationService.requiredText(formData, "displayName", 200),
        status: IdentityAccessAdminMutationService.lifecycleStatus(formData, "status"),
        expectedVersion: IdentityAccessAdminMutationService.positiveInteger(formData, "expectedVersion"),
      },
    );
  }

  public async updateReusableGroup(formData: FormData): Promise<void> {
    await this.#request.client.administration.groups.updateReusable(
      this.#request.administrationContext,
      IdentityAccessAdminMutationService.requiredText(formData, "tenantId", 64),
      IdentityAccessAdminMutationService.requiredText(formData, "groupId", 64),
      {
        displayName: IdentityAccessAdminMutationService.requiredText(formData, "displayName", 200),
        status: IdentityAccessAdminMutationService.lifecycleStatus(formData, "status"),
        isTemplate: IdentityAccessAdminMutationService.checkbox(formData, "isTemplate"),
        expectedVersion: IdentityAccessAdminMutationService.positiveInteger(formData, "expectedVersion"),
      },
    );
  }

  public async addGroupMember(formData: FormData): Promise<void> {
    await this.#request.client.administration.groups.addMember(
      this.#tenantContext(formData),
      IdentityAccessAdminMutationService.requiredText(formData, "groupId", 64),
      IdentityAccessAdminMutationService.requiredText(formData, "tenantMembershipId", 64),
    );
  }

  public async removeGroupMember(formData: FormData): Promise<void> {
    IdentityAccessAdminMutationService.requireLiteralConfirmation(formData, "REMOVE");
    await this.#request.client.administration.groups.removeMember(
      this.#tenantContext(formData),
      IdentityAccessAdminMutationService.requiredText(formData, "groupId", 64),
      IdentityAccessAdminMutationService.requiredText(formData, "tenantMembershipId", 64),
    );
  }

  public async replaceTenantMemberGroups(formData: FormData): Promise<void> {
    const context = this.#tenantContext(formData);
    const tenantMembershipId = IdentityAccessAdminMutationService.requiredText(formData, "tenantMembershipId", 64);
    const selections = IdentityAccessAdminMutationService.textList(formData, "groupSelection", 160);

    const [groups, assignments] = await Promise.all([
      this.#request.client.administration.groups.list(context, { limit: 100 }),
      this.#request.client.administration.tenantGroupAssignments.list(context),
    ]);

    const groupsById = new Map(groups.map((group) => [group.groupId, group]));
    const desiredGroupIds = new Set<string>();
    for (const selection of selections) {
      const separator = selection.indexOf(":");
      if (separator <= 0 || selection.slice(0, separator) !== "group") throw new Error("Invalid group selection.");
      const group = groupsById.get(selection.slice(separator + 1));
      if (!group || group.status !== 1) throw new Error("Selected tenant group is unavailable.");
      desiredGroupIds.add(group.groupId);
    }

    const currentGroupIds = new Set(assignments
      .filter((assignment) => assignment.tenantMembershipId === tenantMembershipId)
      .map((assignment) => assignment.groupId));

    for (const groupId of currentGroupIds) {
      if (!desiredGroupIds.has(groupId)) await this.#request.client.administration.groups.removeMember(context, groupId, tenantMembershipId);
    }
    for (const groupId of desiredGroupIds) {
      if (!currentGroupIds.has(groupId)) await this.#request.client.administration.groups.addMember(context, groupId, tenantMembershipId);
    }
  }

  public async addManagedGroupPolicyBinding(formData: FormData): Promise<void> {
    const resourceScopeId = IdentityAccessAdminMutationService.optionalText(formData, "resourceScopeId", 64);
    await this.#request.client.administration.managedPolicyBindings.add(
      this.#tenantContext(formData),
      IdentityAccessAdminMutationService.requiredText(formData, "groupId", 64),
      {
        policyId: IdentityAccessAdminMutationService.requiredText(formData, "managedPolicyId", 64),
        ...(resourceScopeId === undefined ? {} : { resourceScopeId }),
        includeDescendants: IdentityAccessAdminMutationService.checkbox(formData, "includeDescendants"),
      },
    );
  }

  public async removeManagedGroupPolicyBinding(formData: FormData): Promise<void> {
    IdentityAccessAdminMutationService.requireLiteralConfirmation(formData, "REMOVE");
    const resourceScopeId = IdentityAccessAdminMutationService.optionalText(formData, "resourceScopeId", 64);
    await this.#request.client.administration.managedPolicyBindings.remove(
      this.#tenantContext(formData),
      IdentityAccessAdminMutationService.requiredText(formData, "groupId", 64),
      IdentityAccessAdminMutationService.requiredText(formData, "managedPolicyId", 64),
      IdentityAccessAdminMutationService.positiveInteger(formData, "policyVersion"),
      resourceScopeId,
    );
  }

  public async updateResourceScope(formData: FormData): Promise<void> {
    const parentResourceScopeId = IdentityAccessAdminMutationService.optionalText(formData, "parentResourceScopeId", 64);
    await this.#request.client.administration.resourceScopes.update(
      this.#tenantContext(formData),
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

  #tenantContext(formData: FormData) {
    const tenantId = IdentityAccessAdminMutationService.requiredText(formData, "tenantId", 64);
    return this.#request.tenantContextFor(tenantId);
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

  private static requiredSecret(formData: FormData, field: string): string {
    const value = formData.get(field);
    if (typeof value !== "string" || value.length < 12 || value.length > 256) {
      throw new Error(`Invalid administration input: ${field} must contain between 12 and 256 characters.`);
    }
    return value;
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

}
