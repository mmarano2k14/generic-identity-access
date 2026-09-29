import type { IdentityAdministrationContext, IdentitySessionRevocationResult } from "../../admin-contracts.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";
export declare class IdentityAccessSessionsClient {
    #private;
    constructor(admin: IdentityAccessAdministrationTransport);
    revokeUser(context: IdentityAdministrationContext, userIdValue: string, signal?: AbortSignal): Promise<IdentitySessionRevocationResult>;
    revokeClient(context: IdentityAdministrationContext, clientIdValue: string, signal?: AbortSignal): Promise<IdentitySessionRevocationResult>;
}
