import type {
  IdentityAdministrationListOptions,
  IdentityCreateOrganisationProfileRequest,
  IdentityOrganisationProfileRecord,
  IdentitySetOrganisationProfileTemplateRequest,
  IdentityTenantAdministrationContext,
} from "../../admin-contracts.js";
import { IdentityAccessClientError } from "../../errors.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec, type IdentityJsonObject } from "../IdentityAccessValueCodec.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";
import { IdentityAccessOrganisationProfileCodec } from "./IdentityAccessOrganisationProfileCodec.js";

/** Mutable tenant-local OrganisationProfile definition client. */
export class IdentityAccessOrganisationProfilesClient {
  readonly #admin: IdentityAccessAdministrationTransport;

  public constructor(admin: IdentityAccessAdministrationTransport) {
    this.#admin = admin;
  }

  public async list(
    context: IdentityTenantAdministrationContext,
    options?: IdentityAdministrationListOptions,
    signal?: AbortSignal,
  ): Promise<readonly IdentityOrganisationProfileRecord[]> {
    const path =
      `${IdentityAccessPathBuilder.organisationProfilesPath(context)}${IdentityAccessPathBuilder.administrationListQuery(options)}`;

    return this.#admin.get(
      path,
      context,
      (value) => IdentityAccessValueCodec.array(value, IdentityAccessOrganisationProfileCodec.profileRecord),
      signal,
    );
  }

  public async get(
    context: IdentityTenantAdministrationContext,
    organisationProfileIdValue: string,
    signal?: AbortSignal,
  ): Promise<IdentityOrganisationProfileRecord | null> {
    const path =
      `${IdentityAccessPathBuilder.organisationProfilesPath(context)}/${IdentityAccessValueCodec.uuid(organisationProfileIdValue)}`;

    return this.#admin.getNullable(
      path,
      context,
      IdentityAccessOrganisationProfileCodec.profileRecord,
      signal,
    );
  }

  public async getByOrganization(
    context: IdentityTenantAdministrationContext,
    organizationIdValue: string,
    signal?: AbortSignal,
  ): Promise<IdentityOrganisationProfileRecord | null> {
    const path =
      `${IdentityAccessPathBuilder.organisationProfilesPath(context)}/by-organization/${IdentityAccessValueCodec.uuid(organizationIdValue)}`;

    return this.#admin.getNullable(
      path,
      context,
      IdentityAccessOrganisationProfileCodec.profileRecord,
      signal,
    );
  }

  public async create(
    context: IdentityTenantAdministrationContext,
    request: IdentityCreateOrganisationProfileRequest,
    signal?: AbortSignal,
  ): Promise<IdentityOrganisationProfileRecord> {
    const body: IdentityJsonObject = {
      organizationId: IdentityAccessValueCodec.uuid(request.organizationId),
      ...(
        request.organisationProfileId === undefined
          ? {}
          : { organisationProfileId: IdentityAccessValueCodec.uuid(request.organisationProfileId) }
      ),
      ...IdentityAccessOrganisationProfilesClient.templatePinBody(
        request.templateKey,
        request.templateVersion,
      ),
    };

    return this.#admin.post(
      IdentityAccessPathBuilder.organisationProfilesPath(context),
      context,
      body,
      IdentityAccessOrganisationProfileCodec.profileRecord,
      signal,
    );
  }

  public async setTemplate(
    context: IdentityTenantAdministrationContext,
    organisationProfileIdValue: string,
    request: IdentitySetOrganisationProfileTemplateRequest,
    signal?: AbortSignal,
  ): Promise<IdentityOrganisationProfileRecord> {
    const path =
      `${IdentityAccessPathBuilder.organisationProfilesPath(context)}/${IdentityAccessValueCodec.uuid(organisationProfileIdValue)}/template`;

    const templateFields =
      request.templateKey === undefined && request.templateVersion === undefined
        ? { templateKey: null, templateVersion: null }
        : IdentityAccessOrganisationProfilesClient.templatePinBody(
            request.templateKey,
            request.templateVersion,
          );

    return this.#admin.put(
      path,
      context,
      {
        ...templateFields,
        expectedRowVersion: IdentityAccessValueCodec.version(request.expectedRowVersion),
      },
      IdentityAccessOrganisationProfileCodec.profileRecord,
      signal,
    );
  }

  public async enable(
    context: IdentityTenantAdministrationContext,
    organisationProfileIdValue: string,
    expectedRowVersion: number,
    signal?: AbortSignal,
  ): Promise<IdentityOrganisationProfileRecord> {
    return this.#lifecycle(
      context,
      organisationProfileIdValue,
      "enable",
      expectedRowVersion,
      signal,
    );
  }

  public async disable(
    context: IdentityTenantAdministrationContext,
    organisationProfileIdValue: string,
    expectedRowVersion: number,
    signal?: AbortSignal,
  ): Promise<IdentityOrganisationProfileRecord> {
    return this.#lifecycle(
      context,
      organisationProfileIdValue,
      "disable",
      expectedRowVersion,
      signal,
    );
  }

  async #lifecycle(
    context: IdentityTenantAdministrationContext,
    organisationProfileIdValue: string,
    action: "enable" | "disable",
    expectedRowVersion: number,
    signal?: AbortSignal,
  ): Promise<IdentityOrganisationProfileRecord> {
    const path =
      `${IdentityAccessPathBuilder.organisationProfilesPath(context)}/${IdentityAccessValueCodec.uuid(organisationProfileIdValue)}/${action}`;

    return this.#admin.postJsonAction(
      path,
      context,
      { expectedRowVersion: IdentityAccessValueCodec.version(expectedRowVersion) },
      IdentityAccessOrganisationProfileCodec.profileRecord,
      signal,
    );
  }

  private static templatePinBody(
    templateKey: string | undefined,
    templateVersion: number | undefined,
  ): IdentityJsonObject {
    if (templateKey === undefined && templateVersion === undefined) {
      return {};
    }

    if (templateKey === undefined || templateVersion === undefined) {
      throw new IdentityAccessClientError("configuration");
    }

    return {
      templateKey: IdentityAccessValueCodec.slug(templateKey),
      templateVersion: IdentityAccessValueCodec.positiveInteger(templateVersion),
    };
  }
}
