import type {
  IdentityMfaPolicyRecord,
  IdentitySecurityAuditRecord,
  IdentitySessionRevocationResult,
} from "@generic-identity/contracts/security-operations";

export function acceptSecurityOperationsContracts(
  policy: IdentityMfaPolicyRecord,
  audit: IdentitySecurityAuditRecord,
  revocation: IdentitySessionRevocationResult,
): void {
  void policy;
  void audit;
  void revocation;
}
