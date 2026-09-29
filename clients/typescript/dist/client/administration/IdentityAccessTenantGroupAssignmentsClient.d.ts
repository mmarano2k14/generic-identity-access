import type { IdentityTenantAdministrationContext, IdentityTenantGroupAssignmentRecord } from "../../admin-contracts.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";
/** Aggregate tenant read surface for group assignments. */
export declare class IdentityAccessTenantGroupAssignmentsClient {
    #private;
    constructor(admin: IdentityAccessAdministrationTransport);
    list(context: IdentityTenantAdministrationContext, signal?: AbortSignal): Promise<readonly IdentityTenantGroupAssignmentRecord[]>;
}
