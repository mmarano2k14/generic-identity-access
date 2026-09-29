import type { IdentityAdministrationListOptions, IdentityCreateResourceScopeRequest, IdentityResourceScopeRecord, IdentityTenantAdministrationContext, IdentityUpdateResourceScopeRequest } from "../../admin-contracts.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";
export declare class IdentityAccessResourceScopesClient {
    #private;
    constructor(admin: IdentityAccessAdministrationTransport);
    list(context: IdentityTenantAdministrationContext, options?: IdentityAdministrationListOptions, signal?: AbortSignal): Promise<readonly IdentityResourceScopeRecord[]>;
    get(context: IdentityTenantAdministrationContext, resourceScopeIdValue: string, signal?: AbortSignal): Promise<IdentityResourceScopeRecord | null>;
    create(context: IdentityTenantAdministrationContext, request: IdentityCreateResourceScopeRequest, signal?: AbortSignal): Promise<IdentityResourceScopeRecord>;
    update(context: IdentityTenantAdministrationContext, resourceScopeIdValue: string, request: IdentityUpdateResourceScopeRequest, signal?: AbortSignal): Promise<IdentityResourceScopeRecord>;
}
