import { IdentityAccessAdministrationCodec } from "../IdentityAccessAdministrationCodec.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";
export class IdentityAccessScopeAuthorityClient {
    #admin;
    constructor(admin) {
        this.#admin = admin;
    }
    async listGroups(context, options, signal) {
        const path = `${IdentityAccessPathBuilder.scopeAuthorityPath(context)}/groups${IdentityAccessPathBuilder.administrationListQuery(options)}`;
        return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.scopeAuthorityGroupRecord), signal);
    }
    async getGroup(context, groupIdValue, signal) {
        const path = `${IdentityAccessPathBuilder.scopeAuthorityPath(context)}/groups/${IdentityAccessValueCodec.uuid(groupIdValue)}`;
        return this.#admin.getNullable(path, context, (value) => IdentityAccessAdministrationCodec.scopeAuthorityGroupRecord(value), signal);
    }
    async createGroup(context, request, signal) {
        const path = `${IdentityAccessPathBuilder.scopeAuthorityPath(context)}/groups`;
        return this.#admin.post(path, context, {
            groupId: IdentityAccessValueCodec.optionalUuidOrEmpty(request.groupId),
            displayName: IdentityAccessValueCodec.nonEmpty(request.displayName),
            status: IdentityAccessValueCodec.lifecycleStatus(request.status ?? 1),
        }, (value) => IdentityAccessAdministrationCodec.scopeAuthorityGroupRecord(value), signal);
    }
    async updateGroup(context, groupIdValue, request, signal) {
        const path = `${IdentityAccessPathBuilder.scopeAuthorityPath(context)}/groups/${IdentityAccessValueCodec.uuid(groupIdValue)}`;
        return this.#admin.put(path, context, {
            displayName: IdentityAccessValueCodec.nonEmpty(request.displayName),
            status: IdentityAccessValueCodec.lifecycleStatus(request.status),
            expectedVersion: IdentityAccessValueCodec.version(request.expectedVersion),
        }, (value) => IdentityAccessAdministrationCodec.scopeAuthorityGroupRecord(value), signal);
    }
    async listMembers(context, groupIdValue, signal) {
        const path = `${IdentityAccessPathBuilder.scopeAuthorityPath(context)}/groups/${IdentityAccessValueCodec.uuid(groupIdValue)}/members`;
        return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.scopeAuthorityMemberRecord), signal);
    }
    async addMember(context, groupIdValue, userIdValue, signal) {
        const path = `${IdentityAccessPathBuilder.scopeAuthorityPath(context)}/groups/${IdentityAccessValueCodec.uuid(groupIdValue)}/members`;
        return this.#admin.post(path, context, { userId: IdentityAccessValueCodec.uuid(userIdValue) }, (value) => IdentityAccessAdministrationCodec.scopeAuthorityMemberRecord(value), signal);
    }
    async removeMember(context, groupIdValue, userIdValue, signal) {
        const path = `${IdentityAccessPathBuilder.scopeAuthorityPath(context)}/groups/${IdentityAccessValueCodec.uuid(groupIdValue)}/members/${IdentityAccessValueCodec.uuid(userIdValue)}`;
        return this.#admin.delete(path, context, signal);
    }
    async listPolicies(context, options, signal) {
        const path = `${IdentityAccessPathBuilder.scopeAuthorityPath(context)}/policies${IdentityAccessPathBuilder.administrationListQuery(options)}`;
        return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.policyRecord), signal);
    }
    async getPolicy(context, policyIdValue, signal) {
        const path = `${IdentityAccessPathBuilder.scopeAuthorityPath(context)}/policies/${IdentityAccessValueCodec.uuid(policyIdValue)}`;
        return this.#admin.getNullable(path, context, (value) => IdentityAccessAdministrationCodec.policyRecord(value), signal);
    }
    async createPolicy(context, request, signal) {
        const path = `${IdentityAccessPathBuilder.scopeAuthorityPath(context)}/policies`;
        return this.#admin.post(path, context, {
            policyId: IdentityAccessValueCodec.optionalUuidOrEmpty(request.policyId),
            displayName: IdentityAccessValueCodec.nonEmpty(request.displayName),
            status: IdentityAccessValueCodec.lifecycleStatus(request.status ?? 1),
        }, (value) => IdentityAccessAdministrationCodec.policyRecord(value), signal);
    }
    async updatePolicy(context, policyIdValue, request, signal) {
        const path = `${IdentityAccessPathBuilder.scopeAuthorityPath(context)}/policies/${IdentityAccessValueCodec.uuid(policyIdValue)}`;
        return this.#admin.put(path, context, {
            displayName: IdentityAccessValueCodec.nonEmpty(request.displayName),
            status: IdentityAccessValueCodec.lifecycleStatus(request.status),
            expectedVersion: IdentityAccessValueCodec.version(request.expectedVersion),
        }, (value) => IdentityAccessAdministrationCodec.policyRecord(value), signal);
    }
    async listPolicyStatements(context, policyIdValue, signal) {
        const path = `${IdentityAccessPathBuilder.scopeAuthorityPath(context)}/policies/${IdentityAccessValueCodec.uuid(policyIdValue)}/statements`;
        return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.policyStatementRecord), signal);
    }
    async addPolicyStatement(context, policyIdValue, request, signal) {
        const path = `${IdentityAccessPathBuilder.scopeAuthorityPath(context)}/policies/${IdentityAccessValueCodec.uuid(policyIdValue)}/statements`;
        return this.#admin.post(path, context, IdentityAccessAdministrationCodec.policyStatementBody(request), (value) => IdentityAccessAdministrationCodec.policyStatementRecord(value), signal);
    }
    async removePolicyStatement(context, policyIdValue, statementIdValue, signal) {
        const path = `${IdentityAccessPathBuilder.scopeAuthorityPath(context)}/policies/${IdentityAccessValueCodec.uuid(policyIdValue)}/statements/${IdentityAccessValueCodec.uuid(statementIdValue)}`;
        return this.#admin.delete(path, context, signal);
    }
    async listPolicyBindings(context, groupIdValue, signal) {
        const path = `${IdentityAccessPathBuilder.scopeAuthorityPath(context)}/groups/${IdentityAccessValueCodec.uuid(groupIdValue)}/policy-bindings`;
        return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.scopeAuthorityPolicyBindingRecord), signal);
    }
    async addPolicyBinding(context, groupIdValue, policyIdValue, signal) {
        const path = `${IdentityAccessPathBuilder.scopeAuthorityPath(context)}/groups/${IdentityAccessValueCodec.uuid(groupIdValue)}/policy-bindings`;
        return this.#admin.post(path, context, { policyId: IdentityAccessValueCodec.uuid(policyIdValue) }, (value) => IdentityAccessAdministrationCodec.scopeAuthorityPolicyBindingRecord(value), signal);
    }
    async removePolicyBinding(context, groupIdValue, policyIdValue, signal) {
        const path = `${IdentityAccessPathBuilder.scopeAuthorityPath(context)}/groups/${IdentityAccessValueCodec.uuid(groupIdValue)}/policy-bindings/${IdentityAccessValueCodec.uuid(policyIdValue)}`;
        return this.#admin.delete(path, context, signal);
    }
}
