import type { IdentityAdministrationContext } from "@identity-access/client";
import type {
  IdentityCreateMfaPolicyRequest,
  IdentityMfaPolicyRecord,
  IdentityMfaProviderRecord,
  IdentityMfaUserSecurityState,
  IdentitySecurityAuditQuery,
  IdentitySecurityAuditRecord,
  IdentitySessionRevocationResult,
  IdentityUpdateMfaPolicyRequest,
  IdentityUserAuthenticatorRecord,
} from "@generic-identity/contracts/security-operations";

/** Administrative session-revocation operations backed by the existing server authority. */
export interface GenericIdentitySecuritySessionsClient {
  revokeUser(
    context: IdentityAdministrationContext,
    userId: string,
    signal?: AbortSignal,
  ): Promise<IdentitySessionRevocationResult>;

  revokeClient(
    context: IdentityAdministrationContext,
    clientId: string,
    signal?: AbortSignal,
  ): Promise<IdentitySessionRevocationResult>;
}

/** Provider-neutral MFA administration over the existing Identity backend. */
export interface GenericIdentitySecurityMfaClient {
  listProviders(
    context: IdentityAdministrationContext,
    signal?: AbortSignal,
  ): Promise<readonly IdentityMfaProviderRecord[]>;

  getPolicy(
    context: IdentityAdministrationContext,
    signal?: AbortSignal,
  ): Promise<IdentityMfaPolicyRecord | null>;

  createPolicy(
    context: IdentityAdministrationContext,
    request: IdentityCreateMfaPolicyRequest,
    signal?: AbortSignal,
  ): Promise<IdentityMfaPolicyRecord>;

  updatePolicy(
    context: IdentityAdministrationContext,
    request: IdentityUpdateMfaPolicyRequest,
    signal?: AbortSignal,
  ): Promise<IdentityMfaPolicyRecord>;

  listAuthenticators(
    context: IdentityAdministrationContext,
    userId: string,
    signal?: AbortSignal,
  ): Promise<readonly IdentityUserAuthenticatorRecord[]>;

  getUserSecurityState(
    context: IdentityAdministrationContext,
    userId: string,
    signal?: AbortSignal,
  ): Promise<IdentityMfaUserSecurityState>;

  revokeAuthenticator(
    context: IdentityAdministrationContext,
    userId: string,
    authenticatorId: string,
    expectedVersion: number,
    signal?: AbortSignal,
  ): Promise<IdentityUserAuthenticatorRecord>;

  revokeAuthenticatorForRecovery(
    context: IdentityAdministrationContext,
    userId: string,
    authenticatorId: string,
    expectedVersion: number,
    signal?: AbortSignal,
  ): Promise<IdentityUserAuthenticatorRecord>;
}

/** Read-only administrative security-audit surface. */
export interface GenericIdentitySecurityAuditClient {
  list(
    context: IdentityAdministrationContext,
    query?: IdentitySecurityAuditQuery,
    signal?: AbortSignal,
  ): Promise<readonly IdentitySecurityAuditRecord[]>;
}

/**
 * Categorized Security Operations facade.
 *
 * The facade delegates to the proven compatibility transport; it does not
 * implement session, MFA, authenticator or audit semantics locally.
 */
export interface GenericIdentitySecurityOperationsClient {
  readonly sessions: GenericIdentitySecuritySessionsClient;
  readonly mfa: GenericIdentitySecurityMfaClient;
  readonly audit: GenericIdentitySecurityAuditClient;
}

export function createGenericIdentitySecurityOperationsClient(
  sessions: GenericIdentitySecuritySessionsClient,
  mfa: GenericIdentitySecurityMfaClient,
  audit: GenericIdentitySecurityAuditClient,
): GenericIdentitySecurityOperationsClient {
  return { sessions, mfa, audit };
}
