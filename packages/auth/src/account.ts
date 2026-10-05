import type {
  IdentityAdministrationContext,
  IdentityChangePasswordCredentialRequest,
  IdentityCreatePasswordCredentialRequest,
  IdentityPasswordCredentialMetadataRecord,
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
import type { GenericIdentityAuthenticationClient } from "./authentication";

/** Administrative password-credential lifecycle for an existing user. */
export interface GenericIdentityPasswordCredentialsClient {
  get(
    context: IdentityAdministrationContext,
    userId: string,
    signal?: AbortSignal,
  ): Promise<IdentityPasswordCredentialMetadataRecord | null>;

  create(
    context: IdentityAdministrationContext,
    userId: string,
    request: IdentityCreatePasswordCredentialRequest,
    signal?: AbortSignal,
  ): Promise<IdentityPasswordCredentialMetadataRecord>;

  changePassword(
    context: IdentityAdministrationContext,
    userId: string,
    request: IdentityChangePasswordCredentialRequest,
    signal?: AbortSignal,
  ): Promise<IdentityPasswordCredentialMetadataRecord>;
}

/**
 * Categorized account facade. It delegates to the proven authentication and
 * administration transports and does not introduce a second account system.
 */
export interface GenericIdentityAccountClient {
  readonly passwordCredentials: GenericIdentityPasswordCredentialsClient;

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
}

export function createGenericIdentityAccountClient(
  authentication: GenericIdentityAuthenticationClient,
  passwordCredentials: GenericIdentityPasswordCredentialsClient,
): GenericIdentityAccountClient {
  return {
    passwordCredentials,
    changePassword: (request, signal) => authentication.changePassword(request, signal),
    recoverPasswordWithCode: (request, signal) => authentication.recoverPasswordWithCode(request, signal),
    validateSession: (credential, signal) => authentication.validateSession(credential, signal),
    verifyTotp: (credential, authenticatorId, code, signal) =>
      authentication.verifyTotp(credential, authenticatorId, code, signal),
    verifyRecoveryCode: (credential, authenticatorId, code, signal) =>
      authentication.verifyRecoveryCode(credential, authenticatorId, code, signal),
    beginWebAuthnStepUp: (credential, signal) => authentication.beginWebAuthnStepUp(credential, signal),
    completeWebAuthnStepUp: (credential, response, signal) =>
      authentication.completeWebAuthnStepUp(credential, response, signal),
  };
}

export type { IdentityLocalSession } from "@identity-access/client";
