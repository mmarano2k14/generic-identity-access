import type {
  IdentityOrganisationProfileDomainOverrideRecord,
  IdentityReplaceOrganisationProfileDomainOverridesRequest,
  IdentityOrganisationProfileRecord,
  IdentityTenantAdministrationContext,
} from "../../admin-contracts.js";
import { IdentityAccessClientError } from "../../errors.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec, type IdentityJsonObject } from "../IdentityAccessValueCodec.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";
import { IdentityAccessOrganisationProfileCodec } from "./IdentityAccessOrganisationProfileCodec.js";

/** Organization-specific OrganisationProfile domain override client. */
export class IdentityAccessOrganisationProfileDomainOverridesClient {
  readonly #admin: IdentityAccessAdministrationTransport;

  public constructor(admin: IdentityAccessAdministrationTransport) {
    this.#admin = admin;
  }

  public async list(
    context: IdentityTenantAdministrationContext,
    organisationProfileIdValue: string,
    signal?: AbortSignal,
  ): Promise<readonly IdentityOrganisationProfileDomainOverrideRecord[]> {
    const path = this.#path(context, organisationProfileIdValue);

    return this.#admin.get(
      path,
      context,
      (value) => IdentityAccessValueCodec.array(value, IdentityAccessOrganisationProfileCodec.domainOverride),
      signal,
    );
  }

  public async replace(
    context: IdentityTenantAdministrationContext,
    organisationProfileIdValue: string,
    request: IdentityReplaceOrganisationProfileDomainOverridesRequest,
    signal?: AbortSignal,
  ): Promise<IdentityOrganisationProfileRecord> {
    const path = this.#path(context, organisationProfileIdValue);

    return this.#admin.put(
      path,
      context,
      {
        expectedRowVersion: IdentityAccessValueCodec.version(request.expectedRowVersion),
        overrides: request.overrides.map(
          IdentityAccessOrganisationProfileDomainOverridesClient.overrideBody,
        ),
      },
      IdentityAccessOrganisationProfileCodec.profileRecord,
      signal,
    );
  }

  #path(
    context: IdentityTenantAdministrationContext,
    organisationProfileIdValue: string,
  ): string {
    return `${IdentityAccessPathBuilder.organisationProfilesPath(context)}/${IdentityAccessValueCodec.uuid(organisationProfileIdValue)}/domain-overrides`;
  }

  private static overrideBody(
    value: IdentityOrganisationProfileDomainOverrideRecord,
  ): IdentityJsonObject {
    const domainKey = IdentityAccessValueCodec.slug(value.domainKey);

    if (value.operation === 1) {
      if (value.domainVersion === undefined) {
        throw new IdentityAccessClientError("configuration");
      }

      return {
        domainKey,
        domainVersion: IdentityAccessValueCodec.positiveInteger(value.domainVersion),
        operation: 1,
      };
    }

    if (value.operation === 2) {
      if (value.domainVersion !== undefined) {
        throw new IdentityAccessClientError("configuration");
      }

      return {
        domainKey,
        domainVersion: null,
        operation: 2,
      };
    }

    throw new IdentityAccessClientError("configuration");
  }
}
