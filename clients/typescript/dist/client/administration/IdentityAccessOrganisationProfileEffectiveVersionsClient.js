import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";
import { IdentityAccessOrganisationProfileCodec } from "./IdentityAccessOrganisationProfileCodec.js";
/** Immutable OrganisationProfile semantic-version query and resolution client. */
export class IdentityAccessOrganisationProfileEffectiveVersionsClient {
    #admin;
    constructor(admin) {
        this.#admin = admin;
    }
    async list(context, organisationProfileIdValue, options, signal) {
        const path = `${this.#basePath(context, organisationProfileIdValue)}${IdentityAccessPathBuilder.administrationListQuery(options)}`;
        return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessOrganisationProfileCodec.effectiveProfileRecord), signal);
    }
    async get(context, organisationProfileIdValue, version, signal) {
        const path = `${this.#basePath(context, organisationProfileIdValue)}/${IdentityAccessValueCodec.positiveInteger(version)}`;
        return this.#admin.getNullable(path, context, IdentityAccessOrganisationProfileCodec.effectiveProfileRecord, signal);
    }
    async resolve(context, organisationProfileIdValue, request, signal) {
        const path = `${this.#basePath(context, organisationProfileIdValue)}/resolve`;
        return this.#admin.postJsonAction(path, context, {
            expectedRowVersion: IdentityAccessValueCodec.version(request.expectedRowVersion),
        }, IdentityAccessOrganisationProfileCodec.effectiveProfileRecord, signal);
    }
    #basePath(context, organisationProfileIdValue) {
        return `${IdentityAccessPathBuilder.organisationProfilesPath(context)}/${IdentityAccessValueCodec.uuid(organisationProfileIdValue)}/effective-versions`;
    }
}
