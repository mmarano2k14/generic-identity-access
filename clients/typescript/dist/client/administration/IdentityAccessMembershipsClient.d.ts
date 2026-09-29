import type { IdentityAdministrationListOptions, IdentityCreateTenantMembershipRequest, IdentityTenantAdministrationContext, IdentityTenantMembershipRecord, IdentityUpdateTenantMembershipRequest } from "../../admin-contracts.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";
export declare class IdentityAccessMembershipsClient {
    #private;
    constructor(admin: IdentityAccessAdministrationTransport);
    list(context: IdentityTenantAdministrationContext, options?: IdentityAdministrationListOptions, signal?: AbortSignal): Promise<readonly IdentityTenantMembershipRecord[]>;
    get(context: IdentityTenantAdministrationContext, membershipIdValue: string, signal?: AbortSignal): Promise<IdentityTenantMembershipRecord | null>;
    findByUser(context: IdentityTenantAdministrationContext, userIdValue: string, signal?: AbortSignal): Promise<IdentityTenantMembershipRecord | null>;
    create(context: IdentityTenantAdministrationContext, request: IdentityCreateTenantMembershipRequest, signal?: AbortSignal): Promise<IdentityTenantMembershipRecord>;
    update(context: IdentityTenantAdministrationContext, membershipIdValue: string, request: IdentityUpdateTenantMembershipRequest, signal?: AbortSignal): Promise<IdentityTenantMembershipRecord>;
}
