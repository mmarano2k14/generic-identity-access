import { IdentityAccessClientError } from "../../errors.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";
import { IdentityAccessOrganisationProfileCodec } from "./IdentityAccessOrganisationProfileCodec.js";
/** Mutable tenant-local OrganisationProfile definition client. */
export class IdentityAccessOrganisationProfilesClient {
    #admin;
    constructor(admin) {
        this.#admin = admin;
    }
    async list(context, options, signal) {
        const path = `${IdentityAccessPathBuilder.organisationProfilesPath(context)}${IdentityAccessPathBuilder.administrationListQuery(options)}`;
        return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessOrganisationProfileCodec.profileRecord), signal);
    }
    async get(context, organisationProfileIdValue, signal) {
        const path = `${IdentityAccessPathBuilder.organisationProfilesPath(context)}/${IdentityAccessValueCodec.uuid(organisationProfileIdValue)}`;
        return this.#admin.getNullable(path, context, IdentityAccessOrganisationProfileCodec.profileRecord, signal);
    }
    async getByOrganization(context, organizationIdValue, signal) {
        const path = `${IdentityAccessPathBuilder.organisationProfilesPath(context)}/by-organization/${IdentityAccessValueCodec.uuid(organizationIdValue)}`;
        return this.#admin.getNullable(path, context, IdentityAccessOrganisationProfileCodec.profileRecord, signal);
    }
    async create(context, request, signal) {
        const body = {
            organizationId: IdentityAccessValueCodec.uuid(request.organizationId),
            ...(request.organisationProfileId === undefined
                ? {}
                : { organisationProfileId: IdentityAccessValueCodec.uuid(request.organisationProfileId) }),
            ...IdentityAccessOrganisationProfilesClient.templatePinBody(request.templateKey, request.templateVersion),
        };
        return this.#admin.post(IdentityAccessPathBuilder.organisationProfilesPath(context), context, body, IdentityAccessOrganisationProfileCodec.profileRecord, signal);
    }
    async setTemplate(context, organisationProfileIdValue, request, signal) {
        const path = `${IdentityAccessPathBuilder.organisationProfilesPath(context)}/${IdentityAccessValueCodec.uuid(organisationProfileIdValue)}/template`;
        const templateFields = request.templateKey === undefined && request.templateVersion === undefined
            ? { templateKey: null, templateVersion: null }
            : IdentityAccessOrganisationProfilesClient.templatePinBody(request.templateKey, request.templateVersion);
        return this.#admin.put(path, context, {
            ...templateFields,
            expectedRowVersion: IdentityAccessValueCodec.version(request.expectedRowVersion),
        }, IdentityAccessOrganisationProfileCodec.profileRecord, signal);
    }
    async enable(context, organisationProfileIdValue, expectedRowVersion, signal) {
        return this.#lifecycle(context, organisationProfileIdValue, "enable", expectedRowVersion, signal);
    }
    async disable(context, organisationProfileIdValue, expectedRowVersion, signal) {
        return this.#lifecycle(context, organisationProfileIdValue, "disable", expectedRowVersion, signal);
    }
    async #lifecycle(context, organisationProfileIdValue, action, expectedRowVersion, signal) {
        const path = `${IdentityAccessPathBuilder.organisationProfilesPath(context)}/${IdentityAccessValueCodec.uuid(organisationProfileIdValue)}/${action}`;
        return this.#admin.postJsonAction(path, context, { expectedRowVersion: IdentityAccessValueCodec.version(expectedRowVersion) }, IdentityAccessOrganisationProfileCodec.profileRecord, signal);
    }
    static templatePinBody(templateKey, templateVersion) {
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
