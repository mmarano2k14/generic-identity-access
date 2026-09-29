import { IdentityAccessAdministrationCodec } from "../IdentityAccessAdministrationCodec.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";
export class IdentityAccessSecurityModelsClient {
    #admin;
    constructor(admin) {
        this.#admin = admin;
    }
    async list(context, signal) {
        const path = `${IdentityAccessPathBuilder.administrationBasePath(context)}/security-models`;
        return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, (entry) => IdentityAccessAdministrationCodec.applicationSecurityModelSummaryRecord(entry)), signal);
    }
    async get(context, modelVersionValue, signal) {
        const modelVersion = IdentityAccessValueCodec.positiveInteger(modelVersionValue);
        const path = `${IdentityAccessPathBuilder.administrationBasePath(context)}/security-models/${modelVersion}`;
        return this.#admin.getNullable(path, context, (value) => IdentityAccessAdministrationCodec.applicationSecurityModelRecord(value), signal);
    }
    async registerManifest(context, request, signal) {
        const modelVersion = IdentityAccessValueCodec.positiveInteger(request.modelVersion);
        const path = `${IdentityAccessPathBuilder.administrationBasePath(context)}/security-models/${modelVersion}`;
        return this.#admin.put(path, context, IdentityAccessAdministrationCodec.applicationSecurityManifestBody(request), (value) => IdentityAccessAdministrationCodec.applicationSecurityModelRecord(value), signal);
    }
    async listScopeTypes(context, modelVersionValue, signal) {
        const modelVersion = IdentityAccessValueCodec.positiveInteger(modelVersionValue);
        const path = `${IdentityAccessPathBuilder.administrationBasePath(context)}/security-models/${modelVersion}/scope-types`;
        return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.scopeTypeRecord), signal);
    }
    async addScopeType(context, modelVersionValue, request, signal) {
        const modelVersion = IdentityAccessValueCodec.positiveInteger(modelVersionValue);
        const path = `${IdentityAccessPathBuilder.administrationBasePath(context)}/security-models/${modelVersion}/scope-types`;
        return this.#admin.post(path, context, {
            key: IdentityAccessValueCodec.slug(request.key),
            displayName: IdentityAccessValueCodec.nonEmpty(request.displayName),
            parentKey: request.parentKey === undefined ? null : IdentityAccessValueCodec.slug(request.parentKey),
            canAttachToTenant: IdentityAccessValueCodec.boolean(request.canAttachToTenant),
        }, (value) => IdentityAccessAdministrationCodec.scopeTypeRecord(value), signal);
    }
}
