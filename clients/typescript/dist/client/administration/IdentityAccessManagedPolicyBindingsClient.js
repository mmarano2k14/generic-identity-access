import { IdentityAccessAdministrationCodec } from "../IdentityAccessAdministrationCodec.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";
/** Tenant-scoped client for attaching shared managed-policy versions to groups. */
export class IdentityAccessManagedPolicyBindingsClient {
    #admin;
    constructor(admin) {
        this.#admin = admin;
    }
    async listAvailablePolicies(context, options, signal) {
        const path = `${this.basePath(context)}/available-policies${IdentityAccessPathBuilder.administrationListQuery(options)}`;
        return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.managedPolicyRecord), signal);
    }
    async list(context, groupIdValue, signal) {
        const path = `${this.basePath(context)}/groups/${IdentityAccessValueCodec.uuid(groupIdValue)}`;
        return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.managedGroupPolicyBindingRecord), signal);
    }
    async add(context, groupIdValue, request, signal) {
        const path = `${this.basePath(context)}/groups/${IdentityAccessValueCodec.uuid(groupIdValue)}`;
        return this.#admin.post(path, context, {
            policyId: IdentityAccessValueCodec.uuid(request.policyId),
            policyVersion: request.policyVersion === undefined
                ? null
                : IdentityAccessValueCodec.positiveInteger(request.policyVersion),
            resourceScopeId: request.resourceScopeId === undefined
                ? null
                : IdentityAccessValueCodec.uuid(request.resourceScopeId),
            includeDescendants: request.includeDescendants ?? false,
        }, (value) => IdentityAccessAdministrationCodec.managedGroupPolicyBindingRecord(value), signal);
    }
    async remove(context, groupIdValue, policyIdValue, policyVersion, resourceScopeIdValue, signal) {
        const query = resourceScopeIdValue === undefined
            ? ""
            : `?resourceScopeId=${encodeURIComponent(IdentityAccessValueCodec.uuid(resourceScopeIdValue))}`;
        const path = `${this.basePath(context)}/groups/${IdentityAccessValueCodec.uuid(groupIdValue)}/${IdentityAccessValueCodec.uuid(policyIdValue)}/versions/${IdentityAccessValueCodec.positiveInteger(policyVersion)}${query}`;
        return this.#admin.delete(path, context, signal);
    }
    basePath(context) {
        return `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/managed-policy-bindings`;
    }
}
