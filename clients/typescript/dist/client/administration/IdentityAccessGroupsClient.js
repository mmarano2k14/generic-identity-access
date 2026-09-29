import { IdentityAccessAdministrationCodec } from "../IdentityAccessAdministrationCodec.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";
export class IdentityAccessGroupsClient {
    #admin;
    constructor(admin) {
        this.#admin = admin;
    }
    async list(context, options, signal) {
        const path = `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/groups${IdentityAccessPathBuilder.administrationListQuery(options)}`;
        return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.groupRecord), signal);
    }
    async listTemplates(context, options, signal) {
        const path = `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/groups/templates${IdentityAccessPathBuilder.administrationListQuery(options)}`;
        return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.groupRecord), signal);
    }
    async listTemplateScopeRequirements(context, sourceTenantIdValue, sourceGroupIdValue, signal) {
        const path = `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/groups/templates/${IdentityAccessValueCodec.uuid(sourceTenantIdValue)}/${IdentityAccessValueCodec.uuid(sourceGroupIdValue)}/scope-requirements`;
        return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.groupTemplateResourceScopeRequirement), signal);
    }
    async get(context, groupIdValue, signal) {
        const path = `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/groups/${IdentityAccessValueCodec.uuid(groupIdValue)}`;
        return this.#admin.getNullable(path, context, (value) => IdentityAccessAdministrationCodec.groupRecord(value), signal);
    }
    async create(context, request, signal) {
        const path = `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/groups`;
        return this.#admin.post(path, context, {
            groupId: IdentityAccessValueCodec.optionalUuidOrEmpty(request.groupId),
            displayName: IdentityAccessValueCodec.nonEmpty(request.displayName),
            status: IdentityAccessValueCodec.lifecycleStatus(request.status ?? 1),
        }, (value) => IdentityAccessAdministrationCodec.groupRecord(value), signal);
    }
    async createFromTemplate(context, request, signal) {
        const path = `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/groups/from-template`;
        return this.#admin.post(path, context, {
            sourceTenantId: IdentityAccessValueCodec.uuid(request.sourceTenantId),
            sourceGroupId: IdentityAccessValueCodec.uuid(request.sourceGroupId),
            groupId: IdentityAccessValueCodec.optionalUuidOrEmpty(request.groupId),
            resourceScopeMappings: (request.resourceScopeMappings ?? []).map((mapping) => ({
                sourceResourceScopeId: IdentityAccessValueCodec.uuid(mapping.sourceResourceScopeId),
                targetResourceScopeId: IdentityAccessValueCodec.uuid(mapping.targetResourceScopeId),
            })),
        }, (value) => IdentityAccessAdministrationCodec.groupRecord(value), signal);
    }
    async updateReusable(context, sourceTenantIdValue, groupIdValue, request, signal) {
        const path = `${IdentityAccessPathBuilder.administrationBasePath(context)}/reusable-groups/${IdentityAccessValueCodec.uuid(sourceTenantIdValue)}/${IdentityAccessValueCodec.uuid(groupIdValue)}`;
        return this.#admin.put(path, context, {
            displayName: IdentityAccessValueCodec.nonEmpty(request.displayName),
            status: IdentityAccessValueCodec.lifecycleStatus(request.status),
            isTemplate: IdentityAccessValueCodec.boolean(request.isTemplate),
            expectedVersion: IdentityAccessValueCodec.version(request.expectedVersion),
        }, (value) => IdentityAccessAdministrationCodec.groupRecord(value), signal);
    }
    async update(context, groupIdValue, request, signal) {
        const path = `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/groups/${IdentityAccessValueCodec.uuid(groupIdValue)}`;
        return this.#admin.put(path, context, {
            displayName: IdentityAccessValueCodec.nonEmpty(request.displayName),
            status: IdentityAccessValueCodec.lifecycleStatus(request.status),
            expectedVersion: IdentityAccessValueCodec.version(request.expectedVersion),
        }, (value) => IdentityAccessAdministrationCodec.groupRecord(value), signal);
    }
    async listMembers(context, groupIdValue, signal) {
        const path = `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/groups/${IdentityAccessValueCodec.uuid(groupIdValue)}/members`;
        return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.groupMemberRecord), signal);
    }
    async addMember(context, groupIdValue, tenantMembershipIdValue, signal) {
        const path = `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/groups/${IdentityAccessValueCodec.uuid(groupIdValue)}/members`;
        return this.#admin.post(path, context, {
            tenantMembershipId: IdentityAccessValueCodec.uuid(tenantMembershipIdValue),
        }, (value) => IdentityAccessAdministrationCodec.groupMemberRecord(value), signal);
    }
    async removeMember(context, groupIdValue, tenantMembershipIdValue, signal) {
        const path = `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/groups/${IdentityAccessValueCodec.uuid(groupIdValue)}/members/${IdentityAccessValueCodec.uuid(tenantMembershipIdValue)}`;
        return this.#admin.delete(path, context, signal);
    }
}
