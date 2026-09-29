import type { IdentityAdministrationContext, IdentityAdministrationListOptions, IdentityCreateUserRequest, IdentityUpdateUserRequest, IdentityUserRecord } from "../../admin-contracts.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";
export declare class IdentityAccessUsersClient {
    #private;
    constructor(admin: IdentityAccessAdministrationTransport);
    list(context: IdentityAdministrationContext, options?: IdentityAdministrationListOptions, signal?: AbortSignal): Promise<readonly IdentityUserRecord[]>;
    get(context: IdentityAdministrationContext, userIdValue: string, signal?: AbortSignal): Promise<IdentityUserRecord | null>;
    create(context: IdentityAdministrationContext, request: IdentityCreateUserRequest, signal?: AbortSignal): Promise<IdentityUserRecord>;
    update(context: IdentityAdministrationContext, userIdValue: string, request: IdentityUpdateUserRequest, signal?: AbortSignal): Promise<IdentityUserRecord>;
}
