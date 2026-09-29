import type { IdentityOrganizationResourceScopeLinkRecord, IdentityTenantAdministrationContext } from "../../admin-contracts.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";
/** Manages the application-aware ResourceScope associated with an Organization. */
export declare class IdentityAccessOrganizationResourceScopeLinksClient {
    #private;
    constructor(admin: IdentityAccessAdministrationTransport);
    get(context: IdentityTenantAdministrationContext, organizationIdValue: string, signal?: AbortSignal): Promise<IdentityOrganizationResourceScopeLinkRecord | null>;
    create(context: IdentityTenantAdministrationContext, organizationIdValue: string, resourceScopeIdValue: string, signal?: AbortSignal): Promise<IdentityOrganizationResourceScopeLinkRecord>;
    update(context: IdentityTenantAdministrationContext, organizationIdValue: string, resourceScopeIdValue: string, expectedRowVersion: number, signal?: AbortSignal): Promise<IdentityOrganizationResourceScopeLinkRecord>;
    remove(context: IdentityTenantAdministrationContext, organizationIdValue: string, expectedRowVersion: number, signal?: AbortSignal): Promise<boolean>;
}
