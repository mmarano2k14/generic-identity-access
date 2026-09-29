import type { IdentityAuthenticationAssurance, IdentityLocalSession, IdentityLogoutResult, IdentityPasswordLoginRequest, IdentityRecoveryPasswordResetRequest, IdentitySelfServicePasswordChangeRequest, IdentitySessionCredential, IdentitySessionValidationResult, IdentityWebAuthnAuthenticationOptions, IdentityWebAuthnAuthenticationResponse } from "../contracts.js";
import { IdentityAccessHttpTransport } from "./IdentityAccessHttpTransport.js";
/** Local password/session authentication lifecycle and session-bound MFA step-up. */
export declare class IdentityAccessAuthenticationClient {
    #private;
    constructor(transport: IdentityAccessHttpTransport);
    passwordLogin(request: IdentityPasswordLoginRequest, signal?: AbortSignal): Promise<IdentityLocalSession>;
    changePassword(request: IdentitySelfServicePasswordChangeRequest, signal?: AbortSignal): Promise<void>;
    recoverPasswordWithCode(request: IdentityRecoveryPasswordResetRequest, signal?: AbortSignal): Promise<void>;
    validateSession(credential: IdentitySessionCredential, signal?: AbortSignal): Promise<IdentitySessionValidationResult>;
    verifyTotp(credential: IdentitySessionCredential, authenticatorId: string, code: string, signal?: AbortSignal): Promise<IdentityAuthenticationAssurance>;
    verifyRecoveryCode(credential: IdentitySessionCredential, authenticatorId: string, code: string, signal?: AbortSignal): Promise<IdentityAuthenticationAssurance>;
    beginWebAuthnStepUp(credential: IdentitySessionCredential, signal?: AbortSignal): Promise<IdentityWebAuthnAuthenticationOptions>;
    completeWebAuthnStepUp(credential: IdentitySessionCredential, response: IdentityWebAuthnAuthenticationResponse, signal?: AbortSignal): Promise<IdentityAuthenticationAssurance>;
    logout(credential: IdentitySessionCredential, postLogoutRedirectUri?: string, signal?: AbortSignal): Promise<IdentityLogoutResult>;
    private verifyFactor;
    private static normalizeCredential;
    private static sessionHeaders;
    private static decodeAssurance;
}
