import { IdentityAccessAdministrationCodec } from "../IdentityAccessAdministrationCodec.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";
/** Manages the application-aware ResourceScope associated with an Organization. */
export class IdentityAccessOrganizationResourceScopeLinksClient {
    #admin;
    constructor(admin) {
        this.#admin = admin;
    }
    async get(context, organizationIdValue, signal) {
        const path = this.#path(context, organizationIdValue);
        return this.#admin.getNullable(path, context, IdentityAccessAdministrationCodec.organizationResourceScopeLinkRecord, signal);
    }
    async create(context, organizationIdValue, resourceScopeIdValue, signal) {
        return this.#admin.post(this.#path(context, organizationIdValue), context, { resourceScopeId: IdentityAccessValueCodec.uuid(resourceScopeIdValue) }, IdentityAccessAdministrationCodec.organizationResourceScopeLinkRecord, signal);
    }
    async update(context, organizationIdValue, resourceScopeIdValue, expectedRowVersion, signal) {
        return this.#admin.put(this.#path(context, organizationIdValue), context, {
            resourceScopeId: IdentityAccessValueCodec.uuid(resourceScopeIdValue),
            expectedRowVersion: IdentityAccessValueCodec.version(expectedRowVersion),
        }, IdentityAccessAdministrationCodec.organizationResourceScopeLinkRecord, signal);
    }
    async remove(context, organizationIdValue, expectedRowVersion, signal) {
        const version = IdentityAccessValueCodec.version(expectedRowVersion);
        return this.#admin.delete(`${this.#path(context, organizationIdValue)}?expectedRowVersion=${version}`, context, signal);
    }
    #path(context, organizationIdValue) {
        const organizationId = IdentityAccessValueCodec.uuid(organizationIdValue);
        return `${IdentityAccessPathBuilder.organizationDirectoryPath(context)}/${organizationId}/resource-scope-link`;
    }
}
