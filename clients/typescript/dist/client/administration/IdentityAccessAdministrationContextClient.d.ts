import type { IdentityAdministrationContext, IdentityEffectiveAdministrationContext } from "../../admin-contracts.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";
/** Reads the server-trusted effective administration context for the current credential. */
export declare class IdentityAccessAdministrationContextClient {
    #private;
    constructor(admin: IdentityAccessAdministrationTransport);
    get(context: IdentityAdministrationContext, signal?: AbortSignal): Promise<IdentityEffectiveAdministrationContext>;
}
