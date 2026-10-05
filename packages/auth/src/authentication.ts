import type {
  IdentityLocalSession,
  IdentityLogoutResult,
  IdentityPasswordLoginRequest,
  IdentityRecoveryPasswordResetRequest,
  IdentitySelfServicePasswordChangeRequest,
  IdentitySessionCredential,
  IdentityWebAuthnAuthenticationOptions,
  IdentityWebAuthnAuthenticationResponse,
} from "@identity-access/client";
import type {
  IdentityAuthenticationAssurance,
  IdentitySessionValidationResult,
} from "@generic-identity/contracts";

/**
 * Secret-bearing authentication contracts belong to the auth boundary rather
 * than the passive contracts package. During the Authentication and Authorization SDK milestone, their proven source remains
 * the existing TypeScript client.
 */
export type {
  IdentityLocalSession,
  IdentityLogoutResult,
  IdentityPasswordLoginRequest,
  IdentityRecoveryPasswordResetRequest,
  IdentitySelfServicePasswordChangeRequest,
  IdentitySessionCredential,
  IdentityWebAuthnAuthenticationOptions,
  IdentityWebAuthnAuthenticationResponse,
} from "@identity-access/client";

/** Framework-neutral authentication/session surface exposed by the shared SDK. */
export interface GenericIdentityAuthenticationClient {
  passwordLogin(
    request: IdentityPasswordLoginRequest,
    signal?: AbortSignal,
  ): Promise<IdentityLocalSession>;

  changePassword(
    request: IdentitySelfServicePasswordChangeRequest,
    signal?: AbortSignal,
  ): Promise<void>;

  recoverPasswordWithCode(
    request: IdentityRecoveryPasswordResetRequest,
    signal?: AbortSignal,
  ): Promise<void>;

  validateSession(
    credential: IdentitySessionCredential,
    signal?: AbortSignal,
  ): Promise<IdentitySessionValidationResult>;

  verifyTotp(
    credential: IdentitySessionCredential,
    authenticatorId: string,
    code: string,
    signal?: AbortSignal,
  ): Promise<IdentityAuthenticationAssurance>;

  verifyRecoveryCode(
    credential: IdentitySessionCredential,
    authenticatorId: string,
    code: string,
    signal?: AbortSignal,
  ): Promise<IdentityAuthenticationAssurance>;

  beginWebAuthnStepUp(
    credential: IdentitySessionCredential,
    signal?: AbortSignal,
  ): Promise<IdentityWebAuthnAuthenticationOptions>;

  completeWebAuthnStepUp(
    credential: IdentitySessionCredential,
    response: IdentityWebAuthnAuthenticationResponse,
    signal?: AbortSignal,
  ): Promise<IdentityAuthenticationAssurance>;

  logout(
    credential: IdentitySessionCredential,
    postLogoutRedirectUri?: string,
    signal?: AbortSignal,
  ): Promise<IdentityLogoutResult>;
}

type AuthenticationClientOwner = Readonly<{
  authentication: GenericIdentityAuthenticationClient;
}>;

/** Stable convenience name for the proven password-login flow. */
export function signIn(
  client: AuthenticationClientOwner,
  request: IdentityPasswordLoginRequest,
  signal?: AbortSignal,
): Promise<IdentityLocalSession> {
  return client.authentication.passwordLogin(request, signal);
}

/** Stable convenience name for the proven logout flow. */
export function signOut(
  client: AuthenticationClientOwner,
  credential: IdentitySessionCredential,
  postLogoutRedirectUri?: string,
  signal?: AbortSignal,
): Promise<IdentityLogoutResult> {
  return client.authentication.logout(credential, postLogoutRedirectUri, signal);
}

/** Validates the current opaque session through the server-side authority. */
export function validateSession(
  client: AuthenticationClientOwner,
  credential: IdentitySessionCredential,
  signal?: AbortSignal,
): Promise<IdentitySessionValidationResult> {
  return client.authentication.validateSession(credential, signal);
}
