import type { IdentityTenantAdministrationContext, IdentityTenantMembershipCandidateRecord, IdentityTenantMembershipRecord, IdentityMembershipStatus } from "../../admin-contracts.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";
/** Exact-login candidate lookup for controlled tenant membership creation. */
export declare class IdentityAccessMembershipCandidatesClient {
    #private;
    constructor(admin: IdentityAccessAdministrationTransport);
    findByLogin(context: IdentityTenantAdministrationContext, loginIdentifier: string, signal?: AbortSignal): Promise<IdentityTenantMembershipCandidateRecord | null>;
    createMembershipByLogin(context: IdentityTenantAdministrationContext, loginIdentifier: string, status?: IdentityMembershipStatus, signal?: AbortSignal): Promise<IdentityTenantMembershipRecord>;
}
