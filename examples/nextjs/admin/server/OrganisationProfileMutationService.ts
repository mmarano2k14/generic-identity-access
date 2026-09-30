import "server-only";
import type {
  IdentityOrganisationProfileDomainOverrideRecord,
} from "@identity-access/client";
import { IdentityAccessAdminRequest } from "./IdentityAccessAdminRequest";
import {
  OrganisationProfileWorkspaceService,
  type OrganisationProfileWorkspace,
} from "./OrganisationProfileWorkspaceService";

/** Owns only OrganisationProfile workspace mutations; Organization identity is never mutated here. */
export class OrganisationProfileMutationService {
  readonly #request: IdentityAccessAdminRequest;

  public constructor(request: IdentityAccessAdminRequest) {
    this.#request = request;
  }

  public async create(formData: FormData): Promise<void> {
    const workspace = await this.#workspace(formData);
    if (workspace.organization === null) {
      throw new Error("Invalid administration input: organization is not readable in this tenant.");
    }
    if (workspace.profile !== null) {
      throw new Error("Invalid administration input: this Organization already has an OrganisationProfile.");
    }

    const template = this.#validatedTemplateSelection(
      formData,
      workspace,
      "templateSelection",
      true,
    );

    await this.#request.client.administration.organisationProfiles.create(
      this.#request.tenantContextFor(workspace.tenantId),
      {
        organizationId: workspace.organization.organizationId,
        ...(template === undefined
          ? {}
          : {
              templateKey: template.templateKey,
              templateVersion: template.templateVersion,
            }),
      },
    );
  }

  public async setTemplate(formData: FormData): Promise<void> {
    const workspace = await this.#workspaceWithProfile(formData);
    const template = this.#validatedTemplateSelection(
      formData,
      workspace,
      "templateSelection",
      true,
    );

    await this.#request.client.administration.organisationProfiles.setTemplate(
      this.#request.tenantContextFor(workspace.tenantId),
      workspace.profile!.organisationProfileId,
      {
        ...(template === undefined
          ? {}
          : {
              templateKey: template.templateKey,
              templateVersion: template.templateVersion,
            }),
        expectedRowVersion:
          OrganisationProfileMutationService.positiveInteger(
            formData,
            "expectedRowVersion",
          ),
      },
    );
  }

  public async replaceOverrides(formData: FormData): Promise<void> {
    const workspace = await this.#workspaceWithProfile(formData);
    const allowedByDomain = new Map(
      workspace.domainChoices.map((choice) => [choice.domainKey, choice]),
    );

    const overrides: IdentityOrganisationProfileDomainOverrideRecord[] = [];

    for (const [field, rawValue] of formData.entries()) {
      if (!field.startsWith("domain:")) continue;
      if (typeof rawValue !== "string") {
        throw new Error("Invalid administration input: domain override selection is invalid.");
      }

      const domainKey = field.slice("domain:".length).trim().toLowerCase();
      const choice = allowedByDomain.get(domainKey);
      if (choice === undefined) {
        throw new Error("Invalid administration input: domain override references an unavailable catalog entry.");
      }

      if (rawValue === "inherit") continue;

      if (rawValue === "disable") {
        overrides.push({ domainKey, operation: 2 });
        continue;
      }

      if (rawValue.startsWith("enable:")) {
        const version = Number(rawValue.slice("enable:".length));
        if (!Number.isSafeInteger(version) || version <= 0 || !choice.publishedVersions.includes(version)) {
          throw new Error("Invalid administration input: enabled domain version is not a published selectable reference.");
        }

        overrides.push({
          domainKey,
          domainVersion: version,
          operation: 1,
        });
        continue;
      }

      throw new Error("Invalid administration input: domain override operation is invalid.");
    }

    await this.#request.client.administration.organisationProfileDomainOverrides.replace(
      this.#request.tenantContextFor(workspace.tenantId),
      workspace.profile!.organisationProfileId,
      {
        expectedRowVersion:
          OrganisationProfileMutationService.positiveInteger(
            formData,
            "expectedRowVersion",
          ),
        overrides,
      },
    );
  }

  public async resolveEffectiveVersion(formData: FormData): Promise<void> {
    const workspace = await this.#workspaceWithProfile(formData);

    await this.#request.client.administration.organisationProfileEffectiveVersions.resolve(
      this.#request.tenantContextFor(workspace.tenantId),
      workspace.profile!.organisationProfileId,
      {
        expectedRowVersion:
          OrganisationProfileMutationService.positiveInteger(
            formData,
            "expectedRowVersion",
          ),
      },
    );
  }

  public async enable(formData: FormData): Promise<void> {
    await this.#lifecycle(formData, "enable");
  }

  public async disable(formData: FormData): Promise<void> {
    await this.#lifecycle(formData, "disable");
  }

  async #lifecycle(formData: FormData, action: "enable" | "disable"): Promise<void> {
    const workspace = await this.#workspaceWithProfile(formData);
    const context = this.#request.tenantContextFor(workspace.tenantId);
    const expectedRowVersion = OrganisationProfileMutationService.positiveInteger(
      formData,
      "expectedRowVersion",
    );

    if (action === "enable") {
      await this.#request.client.administration.organisationProfiles.enable(
        context,
        workspace.profile!.organisationProfileId,
        expectedRowVersion,
      );
      return;
    }

    await this.#request.client.administration.organisationProfiles.disable(
      context,
      workspace.profile!.organisationProfileId,
      expectedRowVersion,
    );
  }

  async #workspace(formData: FormData): Promise<OrganisationProfileWorkspace> {
    const tenantId = OrganisationProfileMutationService.requiredText(
      formData,
      "tenantId",
      64,
    );
    const organizationId = OrganisationProfileMutationService.requiredText(
      formData,
      "organizationId",
      64,
    );

    return new OrganisationProfileWorkspaceService(this.#request).load(
      tenantId,
      organizationId,
    );
  }

  async #workspaceWithProfile(formData: FormData): Promise<OrganisationProfileWorkspace> {
    const workspace = await this.#workspace(formData);
    if (workspace.profile === null) {
      throw new Error("Invalid administration input: OrganisationProfile is not available for this Organization.");
    }

    const submittedProfileId = OrganisationProfileMutationService.requiredText(
      formData,
      "organisationProfileId",
      64,
    ).toLowerCase();

    if (workspace.profile.organisationProfileId !== submittedProfileId) {
      throw new Error("Invalid administration input: OrganisationProfile does not belong to the selected Organization context.");
    }

    return workspace;
  }

  #validatedTemplateSelection(
    formData: FormData,
    workspace: OrganisationProfileWorkspace,
    field: string,
    allowEmpty: boolean,
  ): { readonly templateKey: string; readonly templateVersion: number } | undefined {
    const raw = formData.get(field);
    if ((raw === null || raw === "") && allowEmpty) return undefined;
    if (typeof raw !== "string") {
      throw new Error("Invalid administration input: template selection is invalid.");
    }

    const separator = raw.lastIndexOf("@");
    if (separator <= 0 || separator === raw.length - 1) {
      throw new Error("Invalid administration input: template selection is invalid.");
    }

    const templateKey = raw.slice(0, separator).trim().toLowerCase();
    const templateVersion = Number(raw.slice(separator + 1));

    if (!Number.isSafeInteger(templateVersion) || templateVersion <= 0) {
      throw new Error("Invalid administration input: template version is invalid.");
    }

    const selectable = workspace.templateChoices.some(
      (choice) =>
        choice.templateKey === templateKey &&
        choice.templateVersion === templateVersion,
    );

    if (!selectable) {
      throw new Error("Invalid administration input: template selection is not an active published catalog reference.");
    }

    return { templateKey, templateVersion };
  }

  private static requiredText(
    formData: FormData,
    field: string,
    maxLength: number,
  ): string {
    const value = formData.get(field);
    if (typeof value !== "string") {
      throw new Error(`Invalid administration input: ${field} is required.`);
    }

    const normalized = value.trim();
    if (!normalized || normalized.length > maxLength) {
      throw new Error(
        `Invalid administration input: ${field} must contain between 1 and ${maxLength} characters.`,
      );
    }

    return normalized;
  }

  private static positiveInteger(formData: FormData, field: string): number {
    const text = OrganisationProfileMutationService.requiredText(
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
