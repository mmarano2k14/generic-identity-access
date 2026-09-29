import { IdentityAccessAdministrationCodec } from "../IdentityAccessAdministrationCodec.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";
/** Reusable group-template administration plus tenant-scoped read/instantiate operations. */
export class IdentityAccessGroupTemplatesClient {
    #admin;
    constructor(admin) {
        this.#admin = admin;
    }
    async list(context, options, signal) {
        const path = `${this.globalBasePath(context)}${IdentityAccessPathBuilder.administrationListQuery(options)}`;
        return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.groupTemplateRecord), signal);
    }
    async listAvailable(context, options, signal) {
        const path = `${this.tenantBasePath(context)}${IdentityAccessPathBuilder.administrationListQuery(options)}`;
        return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.groupTemplateRecord), signal);
    }
    async create(context, request, signal) {
        return this.#admin.post(this.globalBasePath(context), context, {
            templateId: IdentityAccessValueCodec.optionalUuidOrEmpty(request.templateId),
            displayName: IdentityAccessValueCodec.nonEmpty(request.displayName),
            status: IdentityAccessValueCodec.lifecycleStatus(request.status ?? 1),
        }, (value) => IdentityAccessAdministrationCodec.groupTemplateRecord(value), signal);
    }
    async update(context, templateIdValue, request, signal) {
        return this.#admin.put(`${this.globalBasePath(context)}/${IdentityAccessValueCodec.uuid(templateIdValue)}`, context, {
            displayName: IdentityAccessValueCodec.nonEmpty(request.displayName),
            status: IdentityAccessValueCodec.lifecycleStatus(request.status),
            expectedVersion: IdentityAccessValueCodec.version(request.expectedVersion),
        }, (value) => IdentityAccessAdministrationCodec.groupTemplateRecord(value), signal);
    }
    async instantiate(context, templateIdValue, groupIdValue, signal) {
        return this.#admin.post(`${this.tenantBasePath(context)}/${IdentityAccessValueCodec.uuid(templateIdValue)}/instantiate`, context, { groupId: IdentityAccessValueCodec.optionalUuidOrEmpty(groupIdValue) }, (value) => IdentityAccessAdministrationCodec.groupRecord(value), signal);
    }
    globalBasePath(context) {
        return `${IdentityAccessPathBuilder.administrationBasePath(context)}/group-templates`;
    }
    tenantBasePath(context) {
        return `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/group-templates`;
    }
}
