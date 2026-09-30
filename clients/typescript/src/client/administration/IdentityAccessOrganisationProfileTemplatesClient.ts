import type {
  IdentityAdministrationContext,
  IdentityAdministrationListOptions,
  IdentityCreateOrganisationProfileTemplateRequest,
  IdentityOrganisationProfileTemplateRecord,
  IdentityUpdateOrganisationProfileTemplateRequest,
} from "../../admin-contracts.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";
import { IdentityAccessOrganisationProfileCodec } from "./IdentityAccessOrganisationProfileCodec.js";

/** Reusable OrganisationProfile template-definition client. */
export class IdentityAccessOrganisationProfileTemplatesClient {
  readonly #admin: IdentityAccessAdministrationTransport;

  public constructor(admin: IdentityAccessAdministrationTransport) {
    this.#admin = admin;
  }

  public async list(
    context: IdentityAdministrationContext,
    options?: IdentityAdministrationListOptions,
    signal?: AbortSignal,
  ): Promise<readonly IdentityOrganisationProfileTemplateRecord[]> {
    const path =
      `${IdentityAccessPathBuilder.organisationProfileTemplatesPath(context)}${IdentityAccessPathBuilder.administrationListQuery(options)}`;

    return this.#admin.get(
      path,
      context,
      (value) => IdentityAccessValueCodec.array(value, IdentityAccessOrganisationProfileCodec.templateRecord),
      signal,
    );
  }

  public async get(
    context: IdentityAdministrationContext,
    templateKeyValue: string,
    signal?: AbortSignal,
  ): Promise<IdentityOrganisationProfileTemplateRecord | null> {
    const path =
      `${IdentityAccessPathBuilder.organisationProfileTemplatesPath(context)}/${IdentityAccessValueCodec.slug(templateKeyValue)}`;

    return this.#admin.getNullable(
      path,
      context,
      IdentityAccessOrganisationProfileCodec.templateRecord,
      signal,
    );
  }

  public async create(
    context: IdentityAdministrationContext,
    request: IdentityCreateOrganisationProfileTemplateRequest,
    signal?: AbortSignal,
  ): Promise<IdentityOrganisationProfileTemplateRecord> {
    return this.#admin.post(
      IdentityAccessPathBuilder.organisationProfileTemplatesPath(context),
      context,
      {
        templateKey: IdentityAccessValueCodec.slug(request.templateKey),
        displayName: IdentityAccessValueCodec.nonEmpty(request.displayName),
      },
      IdentityAccessOrganisationProfileCodec.templateRecord,
      signal,
    );
  }

  public async update(
    context: IdentityAdministrationContext,
    templateKeyValue: string,
    request: IdentityUpdateOrganisationProfileTemplateRequest,
    signal?: AbortSignal,
  ): Promise<IdentityOrganisationProfileTemplateRecord> {
    const path =
      `${IdentityAccessPathBuilder.organisationProfileTemplatesPath(context)}/${IdentityAccessValueCodec.slug(templateKeyValue)}`;

    return this.#admin.put(
      path,
      context,
      {
        displayName: IdentityAccessValueCodec.nonEmpty(request.displayName),
        expectedRowVersion: IdentityAccessValueCodec.version(request.expectedRowVersion),
      },
      IdentityAccessOrganisationProfileCodec.templateRecord,
      signal,
    );
  }

  public async enable(
    context: IdentityAdministrationContext,
    templateKeyValue: string,
    expectedRowVersion: number,
    signal?: AbortSignal,
  ): Promise<IdentityOrganisationProfileTemplateRecord> {
    return this.#lifecycle(context, templateKeyValue, "enable", expectedRowVersion, signal);
  }

  public async disable(
    context: IdentityAdministrationContext,
    templateKeyValue: string,
    expectedRowVersion: number,
    signal?: AbortSignal,
  ): Promise<IdentityOrganisationProfileTemplateRecord> {
    return this.#lifecycle(context, templateKeyValue, "disable", expectedRowVersion, signal);
  }

  async #lifecycle(
    context: IdentityAdministrationContext,
    templateKeyValue: string,
    action: "enable" | "disable",
    expectedRowVersion: number,
    signal?: AbortSignal,
  ): Promise<IdentityOrganisationProfileTemplateRecord> {
    const path =
      `${IdentityAccessPathBuilder.organisationProfileTemplatesPath(context)}/${IdentityAccessValueCodec.slug(templateKeyValue)}/${action}`;

    return this.#admin.postJsonAction(
      path,
      context,
      { expectedRowVersion: IdentityAccessValueCodec.version(expectedRowVersion) },
      IdentityAccessOrganisationProfileCodec.templateRecord,
      signal,
    );
  }
}
