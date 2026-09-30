import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";
import { IdentityAccessOrganisationProfileCodec } from "./IdentityAccessOrganisationProfileCodec.js";
/** Draft/publication lifecycle client for reusable OrganisationProfile template versions. */
export class IdentityAccessOrganisationProfileTemplateVersionsClient {
    #admin;
    constructor(admin) {
        this.#admin = admin;
    }
    async list(context, templateKeyValue, signal) {
        return this.#admin.get(this.#basePath(context, templateKeyValue), context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessOrganisationProfileCodec.templateVersionRecord), signal);
    }
    async get(context, templateKeyValue, version, signal) {
        const path = `${this.#basePath(context, templateKeyValue)}/${IdentityAccessValueCodec.positiveInteger(version)}`;
        return this.#admin.getNullable(path, context, IdentityAccessOrganisationProfileCodec.templateVersionRecord, signal);
    }
    async createDraft(context, templateKeyValue, request, signal) {
        return this.#admin.post(this.#basePath(context, templateKeyValue), context, {
            templateVersion: IdentityAccessValueCodec.positiveInteger(request.templateVersion),
            domains: request.domains.map(IdentityAccessOrganisationProfileTemplateVersionsClient.domainSelectionBody),
        }, IdentityAccessOrganisationProfileCodec.templateVersionRecord, signal);
    }
    async replaceDomains(context, templateKeyValue, version, request, signal) {
        const path = `${this.#basePath(context, templateKeyValue)}/${IdentityAccessValueCodec.positiveInteger(version)}/domains`;
        return this.#admin.put(path, context, {
            expectedRowVersion: IdentityAccessValueCodec.version(request.expectedRowVersion),
            domains: request.domains.map(IdentityAccessOrganisationProfileTemplateVersionsClient.domainSelectionBody),
        }, IdentityAccessOrganisationProfileCodec.templateVersionRecord, signal);
    }
    async publish(context, templateKeyValue, version, expectedRowVersion, signal) {
        return this.#publicationAction(context, templateKeyValue, version, "publish", expectedRowVersion, signal);
    }
    async retire(context, templateKeyValue, version, expectedRowVersion, signal) {
        return this.#publicationAction(context, templateKeyValue, version, "retire", expectedRowVersion, signal);
    }
    #basePath(context, templateKeyValue) {
        return `${IdentityAccessPathBuilder.organisationProfileTemplatesPath(context)}/${IdentityAccessValueCodec.slug(templateKeyValue)}/versions`;
    }
    async #publicationAction(context, templateKeyValue, version, action, expectedRowVersion, signal) {
        const path = `${this.#basePath(context, templateKeyValue)}/${IdentityAccessValueCodec.positiveInteger(version)}/${action}`;
        return this.#admin.postJsonAction(path, context, { expectedRowVersion: IdentityAccessValueCodec.version(expectedRowVersion) }, IdentityAccessOrganisationProfileCodec.templateVersionRecord, signal);
    }
    static domainSelectionBody(value) {
        return {
            domainKey: IdentityAccessValueCodec.slug(value.domainKey),
            domainVersion: IdentityAccessValueCodec.positiveInteger(value.domainVersion),
        };
    }
}
