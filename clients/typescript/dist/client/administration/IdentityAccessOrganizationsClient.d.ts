import type { IdentityAdministrationListOptions, IdentityCreateOrganizationRequest, IdentityOrganizationRecord, IdentityOrganizationTreeNodeRecord, IdentityTenantAdministrationContext, IdentityUpdateOrganizationRequest } from "../../admin-contracts.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";
/** Tenant-local Organization Directory client hosted by the common Identity Access API. */
export declare class IdentityAccessOrganizationsClient {
    #private;
    constructor(admin: IdentityAccessAdministrationTransport);
    list(context: IdentityTenantAdministrationContext, options?: IdentityAdministrationListOptions, signal?: AbortSignal): Promise<readonly IdentityOrganizationRecord[]>;
    get(context: IdentityTenantAdministrationContext, organizationIdValue: string, signal?: AbortSignal): Promise<IdentityOrganizationRecord | null>;
    tree(context: IdentityTenantAdministrationContext, signal?: AbortSignal): Promise<readonly IdentityOrganizationTreeNodeRecord[]>;
    children(context: IdentityTenantAdministrationContext, organizationIdValue: string, signal?: AbortSignal): Promise<readonly IdentityOrganizationRecord[]>;
    create(context: IdentityTenantAdministrationContext, request: IdentityCreateOrganizationRequest, signal?: AbortSignal): Promise<IdentityOrganizationRecord>;
    update(context: IdentityTenantAdministrationContext, organizationIdValue: string, request: IdentityUpdateOrganizationRequest, signal?: AbortSignal): Promise<IdentityOrganizationRecord>;
    enable(context: IdentityTenantAdministrationContext, organizationIdValue: string, expectedRowVersion: number, signal?: AbortSignal): Promise<IdentityOrganizationRecord>;
    disable(context: IdentityTenantAdministrationContext, organizationIdValue: string, expectedRowVersion: number, signal?: AbortSignal): Promise<IdentityOrganizationRecord>;
}
