import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";
import { IdentityAccessOrganisationProfileCodec } from "./IdentityAccessOrganisationProfileCodec.js";
/** Reusable OrganisationProfile template-definition client. */
export class IdentityAccessOrganisationProfileTemplatesClient {
    #admin;
    constructor(admin) {
        this.#admin = admin;
    }
    async list(context, options, signal) {
        const path = `${IdentityAccessPathBuilder.organisationProfileTemplatesPath(context)}${IdentityAccessPathBuilder.administrationListQuery(options)}`;
        return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessOrganisationProfileCodec.templateRecord), signal);
    }
    async get(context, templateKeyValue, signal) {
        const path = `${IdentityAccessPathBuilder.organisationProfileTemplatesPath(context)}/${IdentityAccessValueCodec.slug(templateKeyValue)}`;
        return this.#admin.getNullable(path, context, IdentityAccessOrganisationProfileCodec.templateRecord, signal);
    }
    async create(context, request, signal) {
        return this.#admin.post(IdentityAccessPathBuilder.organisationProfileTemplatesPath(context), context, {
            templateKey: IdentityAccessValueCodec.slug(request.templateKey),
            displayName: IdentityAccessValueCodec.nonEmpty(request.displayName),
        }, IdentityAccessOrganisationProfileCodec.templateRecord, signal);
    }
    async update(context, templateKeyValue, request, signal) {
        const path = `${IdentityAccessPathBuilder.organisationProfileTemplatesPath(context)}/${IdentityAccessValueCodec.slug(templateKeyValue)}`;
        return this.#admin.put(path, context, {
            displayName: IdentityAccessValueCodec.nonEmpty(request.displayName),
            expectedRowVersion: IdentityAccessValueCodec.version(request.expectedRowVersion),
        }, IdentityAccessOrganisationProfileCodec.templateRecord, signal);
    }
    async enable(context, templateKeyValue, expectedRowVersion, signal) {
        return this.#lifecycle(context, templateKeyValue, "enable", expectedRowVersion, signal);
    }
    async disable(context, templateKeyValue, expectedRowVersion, signal) {
        return this.#lifecycle(context, templateKeyValue, "disable", expectedRowVersion, signal);
    }
    async #lifecycle(context, templateKeyValue, action, expectedRowVersion, signal) {
        const path = `${IdentityAccessPathBuilder.organisationProfileTemplatesPath(context)}/${IdentityAccessValueCodec.slug(templateKeyValue)}/${action}`;
        return this.#admin.postJsonAction(path, context, { expectedRowVersion: IdentityAccessValueCodec.version(expectedRowVersion) }, IdentityAccessOrganisationProfileCodec.templateRecord, signal);
    }
}
