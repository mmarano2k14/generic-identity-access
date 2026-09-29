import type { IdentityTenantAdministrationContext, IdentityTenantUserListOptions, IdentityTenantUserRecord } from "../../admin-contracts.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";
/** Reads tenant-constrained user projections without exposing the scope-wide directory. */
export declare class IdentityAccessTenantUsersClient {
    #private;
    constructor(admin: IdentityAccessAdministrationTransport);
    list(context: IdentityTenantAdministrationContext, options?: IdentityTenantUserListOptions, signal?: AbortSignal): Promise<readonly IdentityTenantUserRecord[]>;
}
