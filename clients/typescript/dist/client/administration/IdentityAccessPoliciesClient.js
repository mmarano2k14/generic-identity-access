import { IdentityAccessAdministrationCodec } from "../IdentityAccessAdministrationCodec.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";
export class IdentityAccessPoliciesClient {
    #admin;
    constructor(admin) {
        this.#admin = admin;
    }
    async list(context, options, signal) {
        const path = `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/policies${IdentityAccessPathBuilder.administrationListQuery(options)}`;
        return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.policyRecord), signal);
    }
    async get(context, policyIdValue, signal) {
        const path = `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/policies/${IdentityAccessValueCodec.uuid(policyIdValue)}`;
        return this.#admin.getNullable(path, context, (value) => IdentityAccessAdministrationCodec.policyRecord(value), signal);
    }
    async create(context, request, signal) {
        const path = `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/policies`;
        return this.#admin.post(path, context, {
            policyId: IdentityAccessValueCodec.optionalUuidOrEmpty(request.policyId),
            displayName: IdentityAccessValueCodec.nonEmpty(request.displayName),
            status: IdentityAccessValueCodec.lifecycleStatus(request.status ?? 1),
        }, (value) => IdentityAccessAdministrationCodec.policyRecord(value), signal);
    }
    async update(context, policyIdValue, request, signal) {
        const path = `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/policies/${IdentityAccessValueCodec.uuid(policyIdValue)}`;
        return this.#admin.put(path, context, {
            displayName: IdentityAccessValueCodec.nonEmpty(request.displayName),
            status: IdentityAccessValueCodec.lifecycleStatus(request.status),
            expectedVersion: IdentityAccessValueCodec.version(request.expectedVersion),
        }, (value) => IdentityAccessAdministrationCodec.policyRecord(value), signal);
    }
    async listStatements(context, policyIdValue, signal) {
        const path = `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/policies/${IdentityAccessValueCodec.uuid(policyIdValue)}/statements`;
        return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.policyStatementRecord), signal);
    }
    async addStatement(context, policyIdValue, request, signal) {
        const path = `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/policies/${IdentityAccessValueCodec.uuid(policyIdValue)}/statements`;
        return this.#admin.post(path, context, IdentityAccessAdministrationCodec.policyStatementBody(request), (value) => IdentityAccessAdministrationCodec.policyStatementRecord(value), signal);
    }
    async removeStatement(context, policyIdValue, statementIdValue, signal) {
        const path = `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/policies/${IdentityAccessValueCodec.uuid(policyIdValue)}/statements/${IdentityAccessValueCodec.uuid(statementIdValue)}`;
        return this.#admin.delete(path, context, signal);
    }
    async listBindings(context, groupIdValue, signal) {
        const path = `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/groups/${IdentityAccessValueCodec.uuid(groupIdValue)}/policy-bindings`;
        return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.groupPolicyBindingRecord), signal);
    }
    async addBinding(context, groupIdValue, request, signal) {
        const path = `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/groups/${IdentityAccessValueCodec.uuid(groupIdValue)}/policy-bindings`;
        return this.#admin.post(path, context, {
            policyId: IdentityAccessValueCodec.uuid(request.policyId),
            resourceScopeId: request.resourceScopeId === undefined ? null : IdentityAccessValueCodec.uuid(request.resourceScopeId),
            includeDescendants: request.includeDescendants ?? false,
        }, (value) => IdentityAccessAdministrationCodec.groupPolicyBindingRecord(value), signal);
    }
    async removeBinding(context, groupIdValue, policyIdValue, resourceScopeIdValue, signal) {
        const query = resourceScopeIdValue === undefined
            ? ""
            : `?resourceScopeId=${encodeURIComponent(IdentityAccessValueCodec.uuid(resourceScopeIdValue))}`;
        const path = `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/groups/${IdentityAccessValueCodec.uuid(groupIdValue)}/policy-bindings/${IdentityAccessValueCodec.uuid(policyIdValue)}${query}`;
        return this.#admin.delete(path, context, signal);
    }
}
