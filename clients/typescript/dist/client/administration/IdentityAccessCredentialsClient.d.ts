import type { IdentityAdministrationContext, IdentityChangePasswordCredentialRequest, IdentityCreatePasswordCredentialRequest, IdentityPasswordCredentialMetadataRecord } from "../../admin-contracts.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";
/** Password-credential administration without exposing password hashes or stored secret material. */
export declare class IdentityAccessCredentialsClient {
    #private;
    constructor(admin: IdentityAccessAdministrationTransport);
    get(context: IdentityAdministrationContext, userIdValue: string, signal?: AbortSignal): Promise<IdentityPasswordCredentialMetadataRecord | null>;
    create(context: IdentityAdministrationContext, userIdValue: string, request: IdentityCreatePasswordCredentialRequest, signal?: AbortSignal): Promise<IdentityPasswordCredentialMetadataRecord>;
    changePassword(context: IdentityAdministrationContext, userIdValue: string, request: IdentityChangePasswordCredentialRequest, signal?: AbortSignal): Promise<IdentityPasswordCredentialMetadataRecord>;
}
