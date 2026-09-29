import type { IdentityAdministrationContext, IdentityAdministrationListOptions, IdentityCreateGroupTemplateRequest, IdentityGroupRecord, IdentityGroupTemplateRecord, IdentityTenantAdministrationContext, IdentityUpdateGroupTemplateRequest } from "../../admin-contracts.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";
/** Reusable group-template administration plus tenant-scoped read/instantiate operations. */
export declare class IdentityAccessGroupTemplatesClient {
    #private;
    constructor(admin: IdentityAccessAdministrationTransport);
    list(context: IdentityAdministrationContext, options?: IdentityAdministrationListOptions, signal?: AbortSignal): Promise<readonly IdentityGroupTemplateRecord[]>;
    listAvailable(context: IdentityTenantAdministrationContext, options?: IdentityAdministrationListOptions, signal?: AbortSignal): Promise<readonly IdentityGroupTemplateRecord[]>;
    create(context: IdentityAdministrationContext, request: IdentityCreateGroupTemplateRequest, signal?: AbortSignal): Promise<IdentityGroupTemplateRecord>;
    update(context: IdentityAdministrationContext, templateIdValue: string, request: IdentityUpdateGroupTemplateRequest, signal?: AbortSignal): Promise<IdentityGroupTemplateRecord>;
    instantiate(context: IdentityTenantAdministrationContext, templateIdValue: string, groupIdValue?: string, signal?: AbortSignal): Promise<IdentityGroupRecord>;
    private globalBasePath;
    private tenantBasePath;
}
