import type { IdentityAdministrationListOptions, IdentityOrganizationMembershipRecord, IdentityTenantAdministrationContext } from "../../admin-contracts.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";
/** Explicit OrganizationMembership client. Belonging never implies authorization. */
export declare class IdentityAccessOrganizationMembershipsClient {
    #private;
    constructor(admin: IdentityAccessAdministrationTransport);
    listForOrganization(context: IdentityTenantAdministrationContext, organizationIdValue: string, options?: IdentityAdministrationListOptions, signal?: AbortSignal): Promise<readonly IdentityOrganizationMembershipRecord[]>;
    listForTenantMembership(context: IdentityTenantAdministrationContext, tenantMembershipIdValue: string, options?: IdentityAdministrationListOptions, signal?: AbortSignal): Promise<readonly IdentityOrganizationMembershipRecord[]>;
    get(context: IdentityTenantAdministrationContext, organizationIdValue: string, tenantMembershipIdValue: string, signal?: AbortSignal): Promise<IdentityOrganizationMembershipRecord | null>;
    add(context: IdentityTenantAdministrationContext, organizationIdValue: string, tenantMembershipIdValue: string, signal?: AbortSignal): Promise<IdentityOrganizationMembershipRecord>;
    activate(context: IdentityTenantAdministrationContext, organizationIdValue: string, tenantMembershipIdValue: string, expectedRowVersion: number, signal?: AbortSignal): Promise<IdentityOrganizationMembershipRecord>;
    suspend(context: IdentityTenantAdministrationContext, organizationIdValue: string, tenantMembershipIdValue: string, expectedRowVersion: number, signal?: AbortSignal): Promise<IdentityOrganizationMembershipRecord>;
    remove(context: IdentityTenantAdministrationContext, organizationIdValue: string, tenantMembershipIdValue: string, expectedRowVersion: number, signal?: AbortSignal): Promise<boolean>;
}
