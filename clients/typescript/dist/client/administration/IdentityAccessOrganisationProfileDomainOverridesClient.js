import { IdentityAccessClientError } from "../../errors.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";
import { IdentityAccessOrganisationProfileCodec } from "./IdentityAccessOrganisationProfileCodec.js";
/** Organization-specific OrganisationProfile domain override client. */
export class IdentityAccessOrganisationProfileDomainOverridesClient {
    #admin;
    constructor(admin) {
        this.#admin = admin;
    }
    async list(context, organisationProfileIdValue, signal) {
        const path = this.#path(context, organisationProfileIdValue);
        return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessOrganisationProfileCodec.domainOverride), signal);
    }
    async replace(context, organisationProfileIdValue, request, signal) {
        const path = this.#path(context, organisationProfileIdValue);
        return this.#admin.put(path, context, {
            expectedRowVersion: IdentityAccessValueCodec.version(request.expectedRowVersion),
            overrides: request.overrides.map(IdentityAccessOrganisationProfileDomainOverridesClient.overrideBody),
        }, IdentityAccessOrganisationProfileCodec.profileRecord, signal);
    }
    #path(context, organisationProfileIdValue) {
        return `${IdentityAccessPathBuilder.organisationProfilesPath(context)}/${IdentityAccessValueCodec.uuid(organisationProfileIdValue)}/domain-overrides`;
    }
    static overrideBody(value) {
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
