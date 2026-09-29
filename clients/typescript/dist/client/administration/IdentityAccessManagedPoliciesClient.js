import { IdentityAccessAdministrationCodec } from "../IdentityAccessAdministrationCodec.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";
/** Administration client for the tenant-independent managed-policy catalog. */
export class IdentityAccessManagedPoliciesClient {
    #admin;
    constructor(admin) {
        this.#admin = admin;
    }
    async list(context, options, signal) {
        const path = `${this.basePath(context)}${IdentityAccessPathBuilder.administrationListQuery(options)}`;
        return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.managedPolicyRecord), signal);
    }
    async get(context, policyIdValue, signal) {
        return this.#admin.getNullable(`${this.basePath(context)}/${IdentityAccessValueCodec.uuid(policyIdValue)}`, context, (value) => IdentityAccessAdministrationCodec.managedPolicyRecord(value), signal);
    }
    async create(context, request, signal) {
        return this.#admin.post(this.basePath(context), context, {
            policyId: IdentityAccessValueCodec.optionalUuidOrEmpty(request.policyId),
            policyKey: IdentityAccessValueCodec.managedPolicyKey(request.policyKey),
            displayName: IdentityAccessValueCodec.nonEmpty(request.displayName),
            status: IdentityAccessValueCodec.lifecycleStatus(request.status ?? 1),
        }, (value) => IdentityAccessAdministrationCodec.managedPolicyRecord(value), signal);
    }
    async update(context, policyIdValue, request, signal) {
        return this.#admin.put(`${this.basePath(context)}/${IdentityAccessValueCodec.uuid(policyIdValue)}`, context, {
            policyKey: IdentityAccessValueCodec.managedPolicyKey(request.policyKey),
            displayName: IdentityAccessValueCodec.nonEmpty(request.displayName),
            status: IdentityAccessValueCodec.lifecycleStatus(request.status),
            expectedVersion: IdentityAccessValueCodec.version(request.expectedVersion),
        }, (value) => IdentityAccessAdministrationCodec.managedPolicyRecord(value), signal);
    }
    async listVersions(context, policyIdValue, signal) {
        return this.#admin.get(`${this.basePath(context)}/${IdentityAccessValueCodec.uuid(policyIdValue)}/versions`, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.managedPolicyVersionRecord), signal);
    }
    async getVersion(context, policyIdValue, policyVersion, signal) {
        return this.#admin.getNullable(`${this.basePath(context)}/${IdentityAccessValueCodec.uuid(policyIdValue)}/versions/${IdentityAccessValueCodec.positiveInteger(policyVersion)}`, context, (value) => IdentityAccessAdministrationCodec.managedPolicyVersionRecord(value), signal);
    }
    async createVersion(context, policyIdValue, request, signal) {
        return this.#admin.post(`${this.basePath(context)}/${IdentityAccessValueCodec.uuid(policyIdValue)}/versions`, context, {
            policyVersion: IdentityAccessValueCodec.positiveInteger(request.policyVersion),
            modelVersion: IdentityAccessValueCodec.positiveInteger(request.modelVersion),
        }, (value) => IdentityAccessAdministrationCodec.managedPolicyVersionRecord(value), signal);
    }
    async publishVersion(context, policyIdValue, policyVersion, request = {}, signal) {
        return this.#admin.put(`${this.basePath(context)}/${IdentityAccessValueCodec.uuid(policyIdValue)}/versions/${IdentityAccessValueCodec.positiveInteger(policyVersion)}/publish`, context, { makeDefault: IdentityAccessValueCodec.boolean(request.makeDefault ?? false) }, (value) => IdentityAccessAdministrationCodec.managedPolicyVersionRecord(value), signal);
    }
    async listStatements(context, policyIdValue, policyVersion, signal) {
        return this.#admin.get(`${this.versionPath(context, policyIdValue, policyVersion)}/statements`, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.managedPolicyStatementRecord), signal);
    }
    async addStatement(context, policyIdValue, policyVersion, request, signal) {
        return this.#admin.post(`${this.versionPath(context, policyIdValue, policyVersion)}/statements`, context, {
            statementId: IdentityAccessValueCodec.optionalUuidOrEmpty(request.statementId),
            resource: IdentityAccessValueCodec.capabilityPatternSegment(request.resource),
            feature: IdentityAccessValueCodec.capabilityPatternSegment(request.feature),
            action: IdentityAccessValueCodec.capabilityPatternSegment(request.action),
        }, (value) => IdentityAccessAdministrationCodec.managedPolicyStatementRecord(value), signal);
    }
    async removeStatement(context, policyIdValue, policyVersion, statementIdValue, signal) {
        return this.#admin.delete(`${this.versionPath(context, policyIdValue, policyVersion)}/statements/${IdentityAccessValueCodec.uuid(statementIdValue)}`, context, signal);
    }
    basePath(context) {
        return `${IdentityAccessPathBuilder.administrationBasePath(context)}/managed-policies`;
    }
    versionPath(context, policyIdValue, policyVersion) {
        return `${this.basePath(context)}/${IdentityAccessValueCodec.uuid(policyIdValue)}/versions/${IdentityAccessValueCodec.positiveInteger(policyVersion)}`;
    }
}
