import type {
  IdentityAdministrationContext,
  IdentityCreateOrganisationProfileTemplateVersionRequest,
  IdentityOrganisationProfileTemplateVersionRecord,
  IdentityReplaceOrganisationProfileTemplateDomainsRequest,
} from "../../admin-contracts.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec, type IdentityJsonObject } from "../IdentityAccessValueCodec.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";
import { IdentityAccessOrganisationProfileCodec } from "./IdentityAccessOrganisationProfileCodec.js";

/** Draft/publication lifecycle client for reusable OrganisationProfile template versions. */
export class IdentityAccessOrganisationProfileTemplateVersionsClient {
  readonly #admin: IdentityAccessAdministrationTransport;

  public constructor(admin: IdentityAccessAdministrationTransport) {
    this.#admin = admin;
  }

  public async list(
    context: IdentityAdministrationContext,
    templateKeyValue: string,
    signal?: AbortSignal,
  ): Promise<readonly IdentityOrganisationProfileTemplateVersionRecord[]> {
    return this.#admin.get(
      this.#basePath(context, templateKeyValue),
      context,
      (value) => IdentityAccessValueCodec.array(value, IdentityAccessOrganisationProfileCodec.templateVersionRecord),
      signal,
    );
  }

  public async get(
    context: IdentityAdministrationContext,
    templateKeyValue: string,
    version: number,
    signal?: AbortSignal,
  ): Promise<IdentityOrganisationProfileTemplateVersionRecord | null> {
    const path =
      `${this.#basePath(context, templateKeyValue)}/${IdentityAccessValueCodec.positiveInteger(version)}`;

    return this.#admin.getNullable(
      path,
      context,
      IdentityAccessOrganisationProfileCodec.templateVersionRecord,
      signal,
    );
  }

  public async createDraft(
    context: IdentityAdministrationContext,
    templateKeyValue: string,
    request: IdentityCreateOrganisationProfileTemplateVersionRequest,
    signal?: AbortSignal,
  ): Promise<IdentityOrganisationProfileTemplateVersionRecord> {
    return this.#admin.post(
      this.#basePath(context, templateKeyValue),
      context,
      {
        templateVersion: IdentityAccessValueCodec.positiveInteger(request.templateVersion),
        domains: request.domains.map(
          IdentityAccessOrganisationProfileTemplateVersionsClient.domainSelectionBody,
        ),
      },
      IdentityAccessOrganisationProfileCodec.templateVersionRecord,
      signal,
    );
  }

  public async replaceDomains(
    context: IdentityAdministrationContext,
    templateKeyValue: string,
    version: number,
    request: IdentityReplaceOrganisationProfileTemplateDomainsRequest,
    signal?: AbortSignal,
  ): Promise<IdentityOrganisationProfileTemplateVersionRecord> {
    const path =
      `${this.#basePath(context, templateKeyValue)}/${IdentityAccessValueCodec.positiveInteger(version)}/domains`;

    return this.#admin.put(
      path,
      context,
      {
        expectedRowVersion: IdentityAccessValueCodec.version(request.expectedRowVersion),
        domains: request.domains.map(
          IdentityAccessOrganisationProfileTemplateVersionsClient.domainSelectionBody,
        ),
      },
      IdentityAccessOrganisationProfileCodec.templateVersionRecord,
      signal,
    );
  }

  public async publish(
    context: IdentityAdministrationContext,
    templateKeyValue: string,
    version: number,
    expectedRowVersion: number,
    signal?: AbortSignal,
  ): Promise<IdentityOrganisationProfileTemplateVersionRecord> {
    return this.#publicationAction(
      context,
      templateKeyValue,
      version,
      "publish",
      expectedRowVersion,
      signal,
    );
  }

  public async retire(
    context: IdentityAdministrationContext,
    templateKeyValue: string,
    version: number,
    expectedRowVersion: number,
    signal?: AbortSignal,
  ): Promise<IdentityOrganisationProfileTemplateVersionRecord> {
    return this.#publicationAction(
      context,
      templateKeyValue,
      version,
      "retire",
      expectedRowVersion,
      signal,
    );
  }

  #basePath(
    context: IdentityAdministrationContext,
    templateKeyValue: string,
  ): string {
    return `${IdentityAccessPathBuilder.organisationProfileTemplatesPath(context)}/${IdentityAccessValueCodec.slug(templateKeyValue)}/versions`;
  }

  async #publicationAction(
    context: IdentityAdministrationContext,
    templateKeyValue: string,
    version: number,
    action: "publish" | "retire",
    expectedRowVersion: number,
    signal?: AbortSignal,
  ): Promise<IdentityOrganisationProfileTemplateVersionRecord> {
    const path =
      `${this.#basePath(context, templateKeyValue)}/${IdentityAccessValueCodec.positiveInteger(version)}/${action}`;

    return this.#admin.postJsonAction(
      path,
      context,
      { expectedRowVersion: IdentityAccessValueCodec.version(expectedRowVersion) },
      IdentityAccessOrganisationProfileCodec.templateVersionRecord,
      signal,
    );
  }

  private static domainSelectionBody(
    value: { readonly domainKey: string; readonly domainVersion: number },
  ): IdentityJsonObject {
    return {
      domainKey: IdentityAccessValueCodec.slug(value.domainKey),
      domainVersion: IdentityAccessValueCodec.positiveInteger(value.domainVersion),
    };
  }
}
