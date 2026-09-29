import type { IdentityAdministrationContext, IdentitySecurityAuditQuery, IdentitySecurityAuditRecord } from "../../admin-contracts.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";
/** Read-only security-audit administration client. */
export declare class IdentityAccessSecurityAuditClient {
    #private;
    constructor(admin: IdentityAccessAdministrationTransport);
    list(context: IdentityAdministrationContext, query?: IdentitySecurityAuditQuery, signal?: AbortSignal): Promise<readonly IdentitySecurityAuditRecord[]>;
}
