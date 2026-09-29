import { IdentityAccessAdministrationCodec } from "../IdentityAccessAdministrationCodec.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";
export class IdentityAccessResourceScopesClient {
    #admin;
    constructor(admin) {
        this.#admin = admin;
    }
    async list(context, options, signal) {
        const path = `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/resource-scopes${IdentityAccessPathBuilder.administrationListQuery(options)}`;
        return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.resourceScopeRecord), signal);
    }
    async get(context, resourceScopeIdValue, signal) {
        const path = `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/resource-scopes/${IdentityAccessValueCodec.uuid(resourceScopeIdValue)}`;
        return this.#admin.getNullable(path, context, (value) => IdentityAccessAdministrationCodec.resourceScopeRecord(value), signal);
    }
    async create(context, request, signal) {
        const path = `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/resource-scopes`;
        return this.#admin.post(path, context, IdentityAccessAdministrationCodec.resourceScopeBody(request, false), (value) => IdentityAccessAdministrationCodec.resourceScopeRecord(value), signal);
    }
    async update(context, resourceScopeIdValue, request, signal) {
        const path = `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/resource-scopes/${IdentityAccessValueCodec.uuid(resourceScopeIdValue)}`;
        return this.#admin.put(path, context, IdentityAccessAdministrationCodec.resourceScopeBody(request, true), (value) => IdentityAccessAdministrationCodec.resourceScopeRecord(value), signal);
    }
}
