import type { IdentityAdministrationContext, IdentityAdministrationListOptions, IdentityCreateTenantRequest, IdentityTenantRecord, IdentityUpdateTenantRequest } from "../../admin-contracts.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";
export declare class IdentityAccessTenantsClient {
    #private;
    constructor(admin: IdentityAccessAdministrationTransport);
    list(context: IdentityAdministrationContext, options?: IdentityAdministrationListOptions, signal?: AbortSignal): Promise<readonly IdentityTenantRecord[]>;
    get(context: IdentityAdministrationContext, tenantIdValue: string, signal?: AbortSignal): Promise<IdentityTenantRecord | null>;
    create(context: IdentityAdministrationContext, request: IdentityCreateTenantRequest, signal?: AbortSignal): Promise<IdentityTenantRecord>;
    update(context: IdentityAdministrationContext, tenantIdValue: string, request: IdentityUpdateTenantRequest, signal?: AbortSignal): Promise<IdentityTenantRecord>;
}
