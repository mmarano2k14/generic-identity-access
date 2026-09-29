import type { IdentityAdministrationContext, IdentityCreateMfaPolicyRequest, IdentityMfaPolicyRecord, IdentityMfaProviderRecord, IdentityMfaUserSecurityState, IdentityUpdateMfaPolicyRequest, IdentityUserAuthenticatorRecord } from "../../admin-contracts.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";
/** Provider-neutral MFA administration client. */
export declare class IdentityAccessMfaClient {
    #private;
    constructor(admin: IdentityAccessAdministrationTransport);
    listProviders(context: IdentityAdministrationContext, signal?: AbortSignal): Promise<readonly IdentityMfaProviderRecord[]>;
    getPolicy(context: IdentityAdministrationContext, signal?: AbortSignal): Promise<IdentityMfaPolicyRecord | null>;
    createPolicy(context: IdentityAdministrationContext, request: IdentityCreateMfaPolicyRequest, signal?: AbortSignal): Promise<IdentityMfaPolicyRecord>;
    updatePolicy(context: IdentityAdministrationContext, request: IdentityUpdateMfaPolicyRequest, signal?: AbortSignal): Promise<IdentityMfaPolicyRecord>;
    listAuthenticators(context: IdentityAdministrationContext, userIdValue: string, signal?: AbortSignal): Promise<readonly IdentityUserAuthenticatorRecord[]>;
    getUserSecurityState(context: IdentityAdministrationContext, userIdValue: string, signal?: AbortSignal): Promise<IdentityMfaUserSecurityState>;
    revokeAuthenticator(context: IdentityAdministrationContext, userIdValue: string, authenticatorIdValue: string, expectedVersionValue: number, signal?: AbortSignal): Promise<IdentityUserAuthenticatorRecord>;
    revokeAuthenticatorForRecovery(context: IdentityAdministrationContext, userIdValue: string, authenticatorIdValue: string, expectedVersionValue: number, signal?: AbortSignal): Promise<IdentityUserAuthenticatorRecord>;
    private static providerRecord;
    private static providerCapability;
    private static policyRecord;
    private static userSecurityState;
    private static authenticatorRecord;
    private static optionalTimestamp;
    private static policyMode;
    private static authenticatorStatus;
}
