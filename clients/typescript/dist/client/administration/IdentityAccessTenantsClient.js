import { IdentityAccessAdministrationCodec } from "../IdentityAccessAdministrationCodec.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";
export class IdentityAccessTenantsClient {
    #admin;
    constructor(admin) {
        this.#admin = admin;
    }
    async list(context, options, signal) {
        const path = `${IdentityAccessPathBuilder.administrationBasePath(context)}/tenants${IdentityAccessPathBuilder.administrationListQuery(options)}`;
        return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.tenantRecord), signal);
    }
    async get(context, tenantIdValue, signal) {
        const path = `${IdentityAccessPathBuilder.administrationBasePath(context)}/tenants/${IdentityAccessValueCodec.uuid(tenantIdValue)}`;
        return this.#admin.getNullable(path, context, (value) => IdentityAccessAdministrationCodec.tenantRecord(value), signal);
    }
    async create(context, request, signal) {
        const path = `${IdentityAccessPathBuilder.administrationBasePath(context)}/tenants`;
        return this.#admin.post(path, context, {
            tenantId: IdentityAccessValueCodec.optionalUuidOrEmpty(request.tenantId),
            displayName: IdentityAccessValueCodec.nonEmpty(request.displayName),
            status: IdentityAccessValueCodec.lifecycleStatus(request.status ?? 1),
        }, (value) => IdentityAccessAdministrationCodec.tenantRecord(value), signal);
    }
    async update(context, tenantIdValue, request, signal) {
        const path = `${IdentityAccessPathBuilder.administrationBasePath(context)}/tenants/${IdentityAccessValueCodec.uuid(tenantIdValue)}`;
        return this.#admin.put(path, context, {
            displayName: IdentityAccessValueCodec.nonEmpty(request.displayName),
            status: IdentityAccessValueCodec.lifecycleStatus(request.status),
            expectedVersion: IdentityAccessValueCodec.version(request.expectedVersion),
        }, (value) => IdentityAccessAdministrationCodec.tenantRecord(value), signal);
    }
}
