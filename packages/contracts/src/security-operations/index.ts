/**
 * Passive contracts for Generic Identity Security Operations.
 *
 * These shapes describe administrative session revocation, MFA policy/provider
 * metadata, authenticator lifecycle metadata and security-audit reads. Secrets,
 * session tokens and provider-private enrollment material are intentionally
 * excluded from this contract category.
 */
export type {
  IdentityAuthenticatorStatus,
  IdentityCreateMfaPolicyRequest,
  IdentityMfaPolicyMode,
  IdentityMfaPolicyRecord,
  IdentityMfaProviderCapability,
  IdentityMfaProviderRecord,
  IdentityMfaUserSecurityState,
  IdentitySecurityAuditOutcome,
  IdentitySecurityAuditQuery,
  IdentitySecurityAuditRecord,
  IdentitySessionRevocationResult,
  IdentityUpdateMfaPolicyRequest,
  IdentityUserAuthenticatorRecord,
} from "@identity-access/client";
