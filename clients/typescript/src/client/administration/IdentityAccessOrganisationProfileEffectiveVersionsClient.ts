import type {
  IdentityAdministrationListOptions,
  IdentityEffectiveOrganisationProfileRecord,
  IdentityResolveOrganisationProfileRequest,
  IdentityTenantAdministrationContext,
} from "../../admin-contracts.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";
import { IdentityAccessOrganisationProfileCodec } from "./IdentityAccessOrganisationProfileCodec.js";

/** Immutable OrganisationProfile semantic-version query and resolution client. */
export class IdentityAccessOrganisationProfileEffectiveVersionsClient {
  readonly #admin: IdentityAccessAdministrationTransport;

  public constructor(admin: IdentityAccessAdministrationTransport) {
    this.#admin = admin;
  }

  public async list(
    context: IdentityTenantAdministrationContext,
    organisationProfileIdValue: string,
    options?: IdentityAdministrationListOptions,
    signal?: AbortSignal,
  ): Promise<readonly IdentityEffectiveOrganisationProfileRecord[]> {
    const path =
      `${this.#basePath(context, organisationProfileIdValue)}${IdentityAccessPathBuilder.administrationListQuery(options)}`;

    return this.#admin.get(
      path,
      context,
      (value) => IdentityAccessValueCodec.array(value, IdentityAccessOrganisationProfileCodec.effectiveProfileRecord),
      signal,
    );
  }

  public async get(
    context: IdentityTenantAdministrationContext,
    organisationProfileIdValue: string,
    version: number,
    signal?: AbortSignal,
  ): Promise<IdentityEffectiveOrganisationProfileRecord | null> {
    const path =
      `${this.#basePath(context, organisationProfileIdValue)}/${IdentityAccessValueCodec.positiveInteger(version)}`;

    return this.#admin.getNullable(
      path,
      context,
      IdentityAccessOrganisationProfileCodec.effectiveProfileRecord,
      signal,
    );
  }

  public async resolve(
    context: IdentityTenantAdministrationContext,
    organisationProfileIdValue: string,
    request: IdentityResolveOrganisationProfileRequest,
    signal?: AbortSignal,
  ): Promise<IdentityEffectiveOrganisationProfileRecord> {
    const path =
      `${this.#basePath(context, organisationProfileIdValue)}/resolve`;

    return this.#admin.postJsonAction(
      path,
      context,
      {
        expectedRowVersion: IdentityAccessValueCodec.version(request.expectedRowVersion),
      },
      IdentityAccessOrganisationProfileCodec.effectiveProfileRecord,
      signal,
    );
  }

  #basePath(
    context: IdentityTenantAdministrationContext,
    organisationProfileIdValue: string,
  ): string {
    return `${IdentityAccessPathBuilder.organisationProfilesPath(context)}/${IdentityAccessValueCodec.uuid(organisationProfileIdValue)}/effective-versions`;
  }
}
