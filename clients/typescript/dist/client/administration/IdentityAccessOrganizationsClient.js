import { IdentityAccessAdministrationCodec } from "../IdentityAccessAdministrationCodec.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";
/** Tenant-local Organization Directory client hosted by the common Identity Access API. */
export class IdentityAccessOrganizationsClient {
    #admin;
    constructor(admin) {
        this.#admin = admin;
    }
    async list(context, options, signal) {
        const path = `${IdentityAccessPathBuilder.organizationDirectoryPath(context)}${IdentityAccessPathBuilder.administrationListQuery(options)}`;
        return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.organizationRecord), signal);
    }
    async get(context, organizationIdValue, signal) {
        const path = `${IdentityAccessPathBuilder.organizationDirectoryPath(context)}/${IdentityAccessValueCodec.uuid(organizationIdValue)}`;
        return this.#admin.getNullable(path, context, IdentityAccessAdministrationCodec.organizationRecord, signal);
    }
    async tree(context, signal) {
        const path = `${IdentityAccessPathBuilder.organizationDirectoryPath(context)}/tree`;
        return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.organizationTreeNodeRecord), signal);
    }
    async children(context, organizationIdValue, signal) {
        const path = `${IdentityAccessPathBuilder.organizationDirectoryPath(context)}/${IdentityAccessValueCodec.uuid(organizationIdValue)}/children`;
        return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.organizationRecord), signal);
    }
    async create(context, request, signal) {
        const path = IdentityAccessPathBuilder.organizationDirectoryPath(context);
        return this.#admin.post(path, context, {
            organizationId: IdentityAccessValueCodec.optionalUuidOrEmpty(request.organizationId),
            organizationKey: IdentityAccessValueCodec.slug(request.organizationKey),
            displayName: IdentityAccessValueCodec.nonEmpty(request.displayName),
            organizationType: IdentityAccessValueCodec.slug(request.organizationType),
            parentOrganizationId: request.parentOrganizationId === undefined
                ? null
                : IdentityAccessValueCodec.uuid(request.parentOrganizationId),
        }, IdentityAccessAdministrationCodec.organizationRecord, signal);
    }
    async update(context, organizationIdValue, request, signal) {
        const path = `${IdentityAccessPathBuilder.organizationDirectoryPath(context)}/${IdentityAccessValueCodec.uuid(organizationIdValue)}`;
        return this.#admin.put(path, context, {
            displayName: IdentityAccessValueCodec.nonEmpty(request.displayName),
            organizationType: IdentityAccessValueCodec.slug(request.organizationType),
            parentOrganizationId: request.parentOrganizationId === undefined
                ? null
                : IdentityAccessValueCodec.uuid(request.parentOrganizationId),
            expectedRowVersion: IdentityAccessValueCodec.version(request.expectedRowVersion),
        }, IdentityAccessAdministrationCodec.organizationRecord, signal);
    }
    async enable(context, organizationIdValue, expectedRowVersion, signal) {
        return this.#lifecycle(context, organizationIdValue, "enable", expectedRowVersion, signal);
    }
    async disable(context, organizationIdValue, expectedRowVersion, signal) {
        return this.#lifecycle(context, organizationIdValue, "disable", expectedRowVersion, signal);
    }
    async #lifecycle(context, organizationIdValue, action, expectedRowVersion, signal) {
        const organizationId = IdentityAccessValueCodec.uuid(organizationIdValue);
        const path = `${IdentityAccessPathBuilder.organizationDirectoryPath(context)}/${organizationId}/${action}`;
        return this.#admin.postJsonAction(path, context, { expectedRowVersion: IdentityAccessValueCodec.version(expectedRowVersion) }, IdentityAccessAdministrationCodec.organizationRecord, signal);
    }
}
