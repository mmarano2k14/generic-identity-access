import type { IdentityAdministrationContext, IdentityCreateGroupFromTemplateRequest, IdentityCreateGroupRequest, IdentityGroupMemberRecord, IdentityGroupRecord, IdentityGroupTemplateResourceScopeRequirement, IdentityTenantAdministrationContext, IdentityAdministrationListOptions, IdentityUpdateGroupRequest, IdentityUpdateReusableGroupRequest } from "../../admin-contracts.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";
export declare class IdentityAccessGroupsClient {
    #private;
    constructor(admin: IdentityAccessAdministrationTransport);
    list(context: IdentityTenantAdministrationContext, options?: IdentityAdministrationListOptions, signal?: AbortSignal): Promise<readonly IdentityGroupRecord[]>;
    listTemplates(context: IdentityTenantAdministrationContext, options?: IdentityAdministrationListOptions, signal?: AbortSignal): Promise<readonly IdentityGroupRecord[]>;
    listTemplateScopeRequirements(context: IdentityTenantAdministrationContext, sourceTenantIdValue: string, sourceGroupIdValue: string, signal?: AbortSignal): Promise<readonly IdentityGroupTemplateResourceScopeRequirement[]>;
    get(context: IdentityTenantAdministrationContext, groupIdValue: string, signal?: AbortSignal): Promise<IdentityGroupRecord | null>;
    create(context: IdentityTenantAdministrationContext, request: IdentityCreateGroupRequest, signal?: AbortSignal): Promise<IdentityGroupRecord>;
    createFromTemplate(context: IdentityTenantAdministrationContext, request: IdentityCreateGroupFromTemplateRequest, signal?: AbortSignal): Promise<IdentityGroupRecord>;
    updateReusable(context: IdentityAdministrationContext, sourceTenantIdValue: string, groupIdValue: string, request: IdentityUpdateReusableGroupRequest, signal?: AbortSignal): Promise<IdentityGroupRecord>;
    update(context: IdentityTenantAdministrationContext, groupIdValue: string, request: IdentityUpdateGroupRequest, signal?: AbortSignal): Promise<IdentityGroupRecord>;
    listMembers(context: IdentityTenantAdministrationContext, groupIdValue: string, signal?: AbortSignal): Promise<readonly IdentityGroupMemberRecord[]>;
    addMember(context: IdentityTenantAdministrationContext, groupIdValue: string, tenantMembershipIdValue: string, signal?: AbortSignal): Promise<IdentityGroupMemberRecord>;
    removeMember(context: IdentityTenantAdministrationContext, groupIdValue: string, tenantMembershipIdValue: string, signal?: AbortSignal): Promise<boolean>;
}
